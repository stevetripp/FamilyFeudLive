using System.Text.Json;
using FamilyFeudLive.Models;

namespace FamilyFeudLive.Services;

public class GameStateService
{
    private readonly object _lock = new();

    public Team Team1 { get; set; } = new() { Name = "Team Red", Color = "#e63946", Score = 0 };
    public Team Team2 { get; set; } = new() { Name = "Team Blue", Color = "#1d3557", Score = 0 };

    public List<Question> Questions { get; set; } = new();
    public int CurrentRoundIndex { get; private set; } = 0;

    public int Strikes { get; private set; } = 0;
    public BuzzerState Buzzer { get; private set; } = new();

    // Event triggers for sound/animation synchronization
    public long LastBuzzerEventId { get; private set; }
    public long LastDingEventId { get; private set; }
    public long LastStrikeEventId { get; private set; }

    public event Action? OnChange;

    public Question CurrentQuestion =>
        Questions.Count > 0 && CurrentRoundIndex >= 0 && CurrentRoundIndex < Questions.Count
            ? Questions[CurrentRoundIndex]
            : new Question { Text = "No questions available" };

    public int RoundBank
    {
        get
        {
            var revealedSum = CurrentQuestion.Answers
                .Where(a => a.IsRevealed)
                .Sum(a => a.Points);
            return revealedSum * CurrentQuestion.Multiplier;
        }
    }

    public GameStateService()
    {
        LoadDefaultQuestions();
    }

    private void LoadDefaultQuestions()
    {
        Questions = new List<Question>
        {
            new Question
            {
                Id = 1,
                Multiplier = 1,
                Text = "Name something you might find under the sofa cushions.",
                Answers = new List<AnswerItem>
                {
                    new() { Id = 1, Text = "Loose Change / Coins", Points = 38 },
                    new() { Id = 2, Text = "TV Remote", Points = 26 },
                    new() { Id = 3, Text = "Food Crumbs / Popcorn", Points = 15 },
                    new() { Id = 4, Text = "Missing Sock", Points = 9 },
                    new() { Id = 5, Text = "Cell Phone", Points = 5 },
                    new() { Id = 6, Text = "Pet Toys / Hair", Points = 4 },
                    new() { Id = 7, Text = "Dust Bunnies", Points = 3 }
                }
            },
            new Question
            {
                Id = 2,
                Multiplier = 1,
                Text = "Name an excuse people give for being late to work.",
                Answers = new List<AnswerItem>
                {
                    new() { Id = 1, Text = "Traffic / Roadwork", Points = 44 },
                    new() { Id = 2, Text = "Overslept / Alarm Failed", Points = 27 },
                    new() { Id = 3, Text = "Car Trouble / Flat Tire", Points = 13 },
                    new() { Id = 4, Text = "Bad Weather", Points = 7 },
                    new() { Id = 5, Text = "Kids / School Dropoff", Points = 5 },
                    new() { Id = 6, Text = "Sick / Doctor Appointment", Points = 4 }
                }
            },
            new Question
            {
                Id = 3,
                Multiplier = 2, // Double points round
                Text = "Name something people always forget to pack for vacation.",
                Answers = new List<AnswerItem>
                {
                    new() { Id = 1, Text = "Toothbrush / Toothpaste", Points = 35 },
                    new() { Id = 2, Text = "Phone Charger", Points = 28 },
                    new() { Id = 3, Text = "Sunscreen", Points = 14 },
                    new() { Id = 4, Text = "Underwear / Socks", Points = 10 },
                    new() { Id = 5, Text = "Swimsuit", Points = 6 },
                    new() { Id = 6, Text = "Sunglasses", Points = 4 },
                    new() { Id = 7, Text = "Medications", Points = 3 }
                }
            },
            new Question
            {
                Id = 4,
                Multiplier = 2,
                Text = "Name a food people love that smells terrible.",
                Answers = new List<AnswerItem>
                {
                    new() { Id = 1, Text = "Stinky Cheese / Blue Cheese", Points = 41 },
                    new() { Id = 2, Text = "Fish / Seafood", Points = 23 },
                    new() { Id = 3, Text = "Garlic", Points = 16 },
                    new() { Id = 4, Text = "Durian Fruit", Points = 9 },
                    new() { Id = 5, Text = "Onions", Points = 6 },
                    new() { Id = 6, Text = "Eggs", Points = 5 }
                }
            },
            new Question
            {
                Id = 5,
                Multiplier = 3, // Triple points round
                Text = "Name something you dread doing on Sunday night.",
                Answers = new List<AnswerItem>
                {
                    new() { Id = 1, Text = "Thinking About Work / Monday", Points = 42 },
                    new() { Id = 2, Text = "Setting the Alarm Clock", Points = 24 },
                    new() { Id = 3, Text = "Laundry / Ironing Clothes", Points = 15 },
                    new() { Id = 4, Text = "Meal Prep / Cooking", Points = 9 },
                    new() { Id = 5, Text = "Homework / Studying", Points = 6 },
                    new() { Id = 6, Text = "Going to Sleep Early", Points = 4 }
                }
            }
        };
    }

    public int ImportQuestionsFromJson(string json, bool replaceExisting = false)
    {
        lock (_lock)
        {
            var options = new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            };

            var imported = JsonSerializer.Deserialize<List<JsonImportQuestion>>(json, options);
            if (imported == null || imported.Count == 0)
            {
                return 0;
            }

            if (replaceExisting)
            {
                Questions.Clear();
                CurrentRoundIndex = 0;
                Strikes = 0;
                ResetBuzzer();
            }

            int startId = Questions.Count > 0 ? Questions.Max(q => q.Id) + 1 : 1;
            int addedCount = 0;

            foreach (var item in imported)
            {
                var qText = !string.IsNullOrWhiteSpace(item.Question) ? item.Question : item.Text;
                if (string.IsNullOrWhiteSpace(qText)) continue;

                // Multiplier: user specified, or progressive Feud ladder (1x for first 2 rounds, 2x for next 2, 3x for 5+)
                int totalRoundsBefore = Questions.Count;
                int defaultMultiplier = totalRoundsBefore < 2 ? 1 : (totalRoundsBefore < 4 ? 2 : 3);
                int multiplier = item.Multiplier > 0 && item.Multiplier != 1 ? item.Multiplier : defaultMultiplier;

                var question = new Question
                {
                    Id = startId++,
                    Text = qText.Trim(),
                    Multiplier = multiplier,
                    Answers = new List<AnswerItem>()
                };

                if (item.Answers != null)
                {
                    int answerId = 1;
                    foreach (var a in item.Answers)
                    {
                        var aText = !string.IsNullOrWhiteSpace(a.Answer) ? a.Answer : a.Text;
                        if (string.IsNullOrWhiteSpace(aText)) continue;

                        int points = a.PeopleWithAnswer > 0
                            ? a.PeopleWithAnswer
                            : (a.Points > 0 ? a.Points : (item.PointsPerAnswer > 0 ? item.PointsPerAnswer : 10));

                        question.Answers.Add(new AnswerItem
                        {
                            Id = answerId++,
                            Text = aText.Trim(),
                            Points = points,
                            IsRevealed = false
                        });
                    }
                }

                Questions.Add(question);
                addedCount++;
            }

            NotifyStateChanged();
            return addedCount;
        }
    }

    public bool PressBuzzer(string teamName, string playerName)
    {
        lock (_lock)
        {
            if (!Buzzer.IsActive || Buzzer.IsLocked)
            {
                return false;
            }

            Buzzer.IsLocked = true;
            Buzzer.WinnerTeam = teamName;
            Buzzer.WinnerPlayer = playerName;
            Buzzer.WinnerTimestamp = DateTime.UtcNow;
            LastBuzzerEventId = DateTime.UtcNow.Ticks;

            NotifyStateChanged();
            return true;
        }
    }

    public void ActivateBuzzer()
    {
        lock (_lock)
        {
            Buzzer.IsActive = true;
            Buzzer.IsLocked = false;
            Buzzer.WinnerTeam = null;
            Buzzer.WinnerPlayer = null;
            Buzzer.WinnerTimestamp = null;
            NotifyStateChanged();
        }
    }

    public void ResetBuzzer()
    {
        lock (_lock)
        {
            Buzzer.IsActive = false;
            Buzzer.IsLocked = false;
            Buzzer.WinnerTeam = null;
            Buzzer.WinnerPlayer = null;
            Buzzer.WinnerTimestamp = null;
            NotifyStateChanged();
        }
    }

    public void ToggleAnswer(int answerId)
    {
        lock (_lock)
        {
            var answer = CurrentQuestion.Answers.FirstOrDefault(a => a.Id == answerId);
            if (answer != null)
            {
                answer.IsRevealed = !answer.IsRevealed;
                if (answer.IsRevealed)
                {
                    LastDingEventId = DateTime.UtcNow.Ticks;
                }
                NotifyStateChanged();
            }
        }
    }

    public void RevealAllAnswers()
    {
        lock (_lock)
        {
            foreach (var a in CurrentQuestion.Answers)
            {
                a.IsRevealed = true;
            }
            NotifyStateChanged();
        }
    }

    public void HideAllAnswers()
    {
        lock (_lock)
        {
            foreach (var a in CurrentQuestion.Answers)
            {
                a.IsRevealed = false;
            }
            NotifyStateChanged();
        }
    }

    public void AddStrike()
    {
        lock (_lock)
        {
            if (Strikes < 3)
            {
                Strikes++;
            }
            LastStrikeEventId = DateTime.UtcNow.Ticks;
            NotifyStateChanged();
        }
    }

    public void ClearStrikes()
    {
        lock (_lock)
        {
            Strikes = 0;
            NotifyStateChanged();
        }
    }

    public void AwardBank(int teamNumber)
    {
        lock (_lock)
        {
            var points = RoundBank;
            if (teamNumber == 1)
            {
                Team1.Score += points;
            }
            else if (teamNumber == 2)
            {
                Team2.Score += points;
            }
            NotifyStateChanged();
        }
    }

    public void AdjustScore(int teamNumber, int delta)
    {
        lock (_lock)
        {
            if (teamNumber == 1)
            {
                Team1.Score = Math.Max(0, Team1.Score + delta);
            }
            else if (teamNumber == 2)
            {
                Team2.Score = Math.Max(0, Team2.Score + delta);
            }
            NotifyStateChanged();
        }
    }

    public void SetRound(int index)
    {
        lock (_lock)
        {
            if (index >= 0 && index < Questions.Count)
            {
                CurrentRoundIndex = index;
                Strikes = 0;
                ResetBuzzer();
                NotifyStateChanged();
            }
        }
    }

    public void NextRound()
    {
        if (CurrentRoundIndex + 1 < Questions.Count)
        {
            SetRound(CurrentRoundIndex + 1);
        }
    }

    public void PreviousRound()
    {
        if (CurrentRoundIndex > 0)
        {
            SetRound(CurrentRoundIndex - 1);
        }
    }

    public void ResetGame()
    {
        lock (_lock)
        {
            Team1.Score = 0;
            Team2.Score = 0;
            Strikes = 0;
            CurrentRoundIndex = 0;
            ResetBuzzer();
            foreach (var q in Questions)
            {
                foreach (var a in q.Answers)
                {
                    a.IsRevealed = false;
                }
            }
            NotifyStateChanged();
        }
    }

    public void AddQuestion(Question question)
    {
        lock (_lock)
        {
            question.Id = Questions.Count > 0 ? Questions.Max(q => q.Id) + 1 : 1;
            Questions.Add(question);
            NotifyStateChanged();
        }
    }

    public void UpdateTeams(string team1Name, string team2Name)
    {
        lock (_lock)
        {
            Team1.Name = team1Name;
            Team2.Name = team2Name;
            NotifyStateChanged();
        }
    }

    private void NotifyStateChanged() => OnChange?.Invoke();
}
