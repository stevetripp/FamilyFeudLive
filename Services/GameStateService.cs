using System.Text.Json;
using FamilyFeudLive.Models;

namespace FamilyFeudLive.Services;

public class GameStateService
{
    private readonly object _lock = new();

    public Team Team1 { get; set; } = new() { Name = "Team Red", Color = "#ff4d6d", Score = 0 };
    public Team Team2 { get; set; } = new() { Name = "Team Blue", Color = "#00b4d8", Score = 0 };

    public List<Question> Questions { get; set; } = new();
    public int CurrentRoundIndex { get; private set; } = 0;

    public int Strikes { get; private set; } = 0;
    public BuzzerState Buzzer { get; private set; } = new();

    // Event triggers for sound/animation synchronization
    public long LastBuzzerEventId { get; private set; }
    public long LastDingEventId { get; private set; }
    public long LastStrikeEventId { get; private set; }
    public long LastDuplicateEventId { get; private set; }
    public long LastWinEventId { get; private set; }

    public FastMoneyState FastMoney { get; set; } = new();
    private CancellationTokenSource? _fastMoneyTimerCts;

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

    public string GetTeamColor(string? teamName)
    {
        if (string.IsNullOrWhiteSpace(teamName)) return Team1.Color;

        if (string.Equals(teamName, Team1.Name, StringComparison.OrdinalIgnoreCase) ||
            teamName == "1" || string.Equals(teamName, "team1", StringComparison.OrdinalIgnoreCase) ||
            teamName.Contains("red", StringComparison.OrdinalIgnoreCase))
        {
            return Team1.Color;
        }

        if (string.Equals(teamName, Team2.Name, StringComparison.OrdinalIgnoreCase) ||
            teamName == "2" || string.Equals(teamName, "team2", StringComparison.OrdinalIgnoreCase) ||
            teamName.Contains("blue", StringComparison.OrdinalIgnoreCase))
        {
            return Team2.Color;
        }

        return Team1.Color;
    }

    public GameStateService()
    {
        LoadDefaultQuestions();
        LoadDefaultFastMoneyQuestions();
    }

    public void InitFastMoneyPlayerSlots()
    {
        FastMoney.Player1Answers.Clear();
        FastMoney.Player2Answers.Clear();
        int count = Math.Max(5, FastMoney.Questions.Count);
        for (int i = 0; i < count; i++)
        {
            FastMoney.Player1Answers.Add(new FastMoneyPlayerAnswer { QuestionIndex = i });
            FastMoney.Player2Answers.Add(new FastMoneyPlayerAnswer { QuestionIndex = i });
        }
    }

    private void LoadDefaultFastMoneyQuestions()
    {
        FastMoney.Questions = new List<FastMoneyQuestion>
        {
            new()
            {
                QuestionId = 1,
                Question = "Name a fruit you might put in a smoothie.",
                Answers = new List<FastMoneyAnswer>
                {
                    new() { Answer = "Banana", Points = 42 },
                    new() { Answer = "Strawberry", Points = 28 },
                    new() { Answer = "Mango", Points = 12 },
                    new() { Answer = "Blueberry", Points = 8 },
                    new() { Answer = "Peach", Points = 5 },
                    new() { Answer = "Pineapple", Points = 3 },
                    new() { Answer = "Raspberry", Points = 2 }
                }
            },
            new()
            {
                QuestionId = 2,
                Question = "Name something people bring to the beach.",
                Answers = new List<FastMoneyAnswer>
                {
                    new() { Answer = "Towel", Points = 38 },
                    new() { Answer = "Sunscreen", Points = 25 },
                    new() { Answer = "Umbrella", Points = 15 },
                    new() { Answer = "Cooler / Drinks", Points = 10 },
                    new() { Answer = "Chair", Points = 8 },
                    new() { Answer = "Sunglasses", Points = 4 }
                }
            },
            new()
            {
                QuestionId = 3,
                Question = "Name a popular pizza topping.",
                Answers = new List<FastMoneyAnswer>
                {
                    new() { Answer = "Pepperoni", Points = 48 },
                    new() { Answer = "Sausage", Points = 21 },
                    new() { Answer = "Mushrooms", Points = 14 },
                    new() { Answer = "Extra Cheese", Points = 9 },
                    new() { Answer = "Onions", Points = 5 },
                    new() { Answer = "Bacon", Points = 3 }
                }
            },
            new()
            {
                QuestionId = 4,
                Question = "Name something you do first thing in the morning.",
                Answers = new List<FastMoneyAnswer>
                {
                    new() { Answer = "Brush teeth", Points = 39 },
                    new() { Answer = "Stretch / Yawn", Points = 22 },
                    new() { Answer = "Check phone", Points = 18 },
                    new() { Answer = "Drink coffee / water", Points = 12 },
                    new() { Answer = "Use the bathroom", Points = 6 },
                    new() { Answer = "Turn off alarm", Points = 3 }
                }
            },
            new()
            {
                QuestionId = 5,
                Question = "Name a reason people give for being pulled over by the police.",
                Answers = new List<FastMoneyAnswer>
                {
                    new() { Answer = "Speeding", Points = 61 },
                    new() { Answer = "Broken taillight", Points = 16 },
                    new() { Answer = "Running a red light", Points = 11 },
                    new() { Answer = "Expired tags / registration", Points = 6 },
                    new() { Answer = "Texting while driving", Points = 4 },
                    new() { Answer = "Failure to signal", Points = 2 }
                }
            }
        };

        InitFastMoneyPlayerSlots();
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

            List<JsonImportQuestion>? imported = null;
            List<FastMoneyQuestion>? fastMoneyImported = null;

            // 1. Try root object with "Rounds" / "Questions" / "FastMoneyQuestions"
            try
            {
                var root = JsonSerializer.Deserialize<JsonImportRoot>(json, options);
                if (root?.Rounds != null && root.Rounds.Count > 0)
                {
                    imported = root.Rounds;
                }
                else if (root?.Questions != null && root.Questions.Count > 0)
                {
                    imported = root.Questions;
                }

                if (root?.FastMoneyQuestions != null && root.FastMoneyQuestions.Count > 0)
                {
                    fastMoneyImported = root.FastMoneyQuestions;
                }
                else if (root?.FastMoneyQuestion != null && root.FastMoneyQuestion.Count > 0)
                {
                    fastMoneyImported = root.FastMoneyQuestion;
                }
            }
            catch
            {
                // continue to array parsing
            }

            // 2. If not found in root object, try top-level array: [ ... ]
            if (imported == null || imported.Count == 0)
            {
                try
                {
                    imported = JsonSerializer.Deserialize<List<JsonImportQuestion>>(json, options);
                }
                catch
                {
                    // ignore
                }
            }

            // 3. If still nothing, check if top-level array is List<FastMoneyQuestion>
            if ((imported == null || imported.Count == 0) && (fastMoneyImported == null || fastMoneyImported.Count == 0))
            {
                try
                {
                    var fmq = JsonSerializer.Deserialize<List<FastMoneyQuestion>>(json, options);
                    if (fmq != null && fmq.Any(q => q.Answers.Count > 0))
                    {
                        fastMoneyImported = fmq;
                    }
                }
                catch
                {
                    // ignore
                }
            }

            int addedCount = 0;

            if (fastMoneyImported != null && fastMoneyImported.Count > 0)
            {
                foreach (var fmq in fastMoneyImported)
                {
                    fmq.Answers = fmq.Answers.OrderByDescending(a => a.Points).ToList();
                }
                FastMoney.Questions = fastMoneyImported;
                InitFastMoneyPlayerSlots();
                addedCount += fastMoneyImported.Count;
            }

            if (imported != null && imported.Count > 0)
            {
                if (replaceExisting)
                {
                    Questions.Clear();
                    CurrentRoundIndex = 0;
                    Strikes = 0;
                    ResetBuzzer();
                }

                int startId = Questions.Count > 0 ? Questions.Max(q => q.Id) + 1 : 1;

                foreach (var item in imported)
                {
                    var qText = !string.IsNullOrWhiteSpace(item.Question) ? item.Question : item.Text;
                    if (string.IsNullOrWhiteSpace(qText)) continue;

                    int totalRoundsBefore = Questions.Count;
                    int defaultMultiplier = totalRoundsBefore < 2 ? 1 : (totalRoundsBefore < 4 ? 2 : 3);
                    int multiplier = item.Multiplier > 0 ? item.Multiplier : defaultMultiplier;

                    var question = new Question
                    {
                        Id = startId++,
                        Text = qText.Trim(),
                        Multiplier = multiplier,
                        Answers = new List<AnswerItem>()
                    };

                    if (item.Answers != null)
                    {
                        // Order answers from highest rank (#1) to lowest rank by points
                        var validAnswers = item.Answers
                            .Where(a => !string.IsNullOrWhiteSpace(a.Answer) || !string.IsNullOrWhiteSpace(a.Text))
                            .OrderByDescending(a => a.Points > 0 ? a.Points : (a.PeopleWithAnswer > 0 ? a.PeopleWithAnswer : (item.PointsPerAnswer > 0 ? item.PointsPerAnswer : 10)))
                            .ToList();

                        int answerId = 1;
                        foreach (var a in validAnswers)
                        {
                            var aText = !string.IsNullOrWhiteSpace(a.Answer) ? a.Answer : a.Text;
                            int points = a.Points > 0
                                ? a.Points
                                : (a.PeopleWithAnswer > 0 ? a.PeopleWithAnswer : (item.PointsPerAnswer > 0 ? item.PointsPerAnswer : 10));

                            question.Answers.Add(new AnswerItem
                            {
                                Id = answerId++,
                                Text = aText!.Trim(),
                                Points = points,
                                IsRevealed = false
                            });
                        }
                    }

                    Questions.Add(question);
                    addedCount++;
                }
            }

            if (addedCount > 0)
            {
                NotifyStateChanged();
            }
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
            // Ensure answers are ordered from highest rank (#1) to lowest rank
            question.Answers = question.Answers.OrderByDescending(a => a.Points).ToList();
            for (int i = 0; i < question.Answers.Count; i++)
            {
                question.Answers[i].Id = i + 1;
            }
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

    // FAST MONEY METHODS
    public void SetFastMoneyActive(bool active)
    {
        lock (_lock)
        {
            FastMoney.IsActive = active;
            if (active)
            {
                FastMoney.WinningTeamNumber = Team2.Score > Team1.Score ? 2 : 1;
                FastMoney.WinningTeamName = FastMoney.WinningTeamNumber == 1 ? Team1.Name : Team2.Name;
                if (FastMoney.Questions.Count == 0)
                {
                    LoadDefaultFastMoneyQuestions();
                }
            }
            NotifyStateChanged();
        }
    }

    public void SetFastMoneyWinningTeam(int teamNumber)
    {
        lock (_lock)
        {
            FastMoney.WinningTeamNumber = teamNumber;
            FastMoney.WinningTeamName = teamNumber == 1 ? Team1.Name : Team2.Name;
            NotifyStateChanged();
        }
    }

    public void SetFastMoneyPlayer(int playerNumber)
    {
        lock (_lock)
        {
            FastMoney.CurrentPlayer = playerNumber;
            StopFastMoneyTimer();
            FastMoney.TimeRemaining = playerNumber == 1 ? 20 : 25;
            FastMoney.DuplicateWarningMessage = null;
            NotifyStateChanged();
        }
    }

    public void StartFastMoneyTimer()
    {
        lock (_lock)
        {
            if (FastMoney.IsTimerRunning) return;
            FastMoney.IsTimerRunning = true;
            _fastMoneyTimerCts?.Cancel();
            _fastMoneyTimerCts = new CancellationTokenSource();
            var token = _fastMoneyTimerCts.Token;

            _ = Task.Run(async () =>
            {
                while (!token.IsCancellationRequested)
                {
                    try
                    {
                        await Task.Delay(1000, token);
                    }
                    catch (TaskCanceledException)
                    {
                        break;
                    }

                    lock (_lock)
                    {
                        if (!FastMoney.IsTimerRunning || token.IsCancellationRequested) break;

                        if (FastMoney.TimeRemaining > 0)
                        {
                            FastMoney.TimeRemaining--;
                            NotifyStateChanged();

                            if (FastMoney.TimeRemaining == 0)
                            {
                                FastMoney.IsTimerRunning = false;
                                LastBuzzerEventId = DateTime.UtcNow.Ticks;
                                NotifyStateChanged();
                                break;
                            }
                        }
                        else
                        {
                            FastMoney.IsTimerRunning = false;
                            NotifyStateChanged();
                            break;
                        }
                    }
                }
            }, token);

            NotifyStateChanged();
        }
    }

    public void StopFastMoneyTimer()
    {
        lock (_lock)
        {
            FastMoney.IsTimerRunning = false;
            _fastMoneyTimerCts?.Cancel();
            NotifyStateChanged();
        }
    }

    public void ResetFastMoneyTimer(int? seconds = null)
    {
        lock (_lock)
        {
            StopFastMoneyTimer();
            FastMoney.TimeRemaining = seconds ?? (FastMoney.CurrentPlayer == 1 ? 20 : 25);
            NotifyStateChanged();
        }
    }

    public void AdjustFastMoneyTimer(int delta)
    {
        lock (_lock)
        {
            FastMoney.TimeRemaining = Math.Max(0, FastMoney.TimeRemaining + delta);
            NotifyStateChanged();
        }
    }

    public void SetFastMoneyAnswer(int playerNumber, int questionIndex, string answerText, int points)
    {
        lock (_lock)
        {
            var list = playerNumber == 1 ? FastMoney.Player1Answers : FastMoney.Player2Answers;
            if (questionIndex >= 0 && questionIndex < list.Count)
            {
                var slot = list[questionIndex];
                slot.AnswerText = answerText?.Trim() ?? string.Empty;
                slot.Points = points;

                if (playerNumber == 2)
                {
                    var p1Answer = questionIndex < FastMoney.Player1Answers.Count
                        ? FastMoney.Player1Answers[questionIndex].AnswerText
                        : string.Empty;

                    bool isDuplicate = !string.IsNullOrWhiteSpace(slot.AnswerText) &&
                                       !string.IsNullOrWhiteSpace(p1Answer) &&
                                       string.Equals(slot.AnswerText, p1Answer, StringComparison.OrdinalIgnoreCase);

                    slot.IsDuplicate = isDuplicate;
                    if (isDuplicate)
                    {
                        LastDuplicateEventId = DateTime.UtcNow.Ticks;
                        FastMoney.DuplicateWarningMessage = $"DUPLICATE ANSWER: \"{slot.AnswerText.ToUpper()}\"! TRY AGAIN!";
                        FastMoney.DuplicateAlertTimestamp = DateTime.UtcNow.Ticks;
                    }
                    else
                    {
                        FastMoney.DuplicateWarningMessage = null;
                    }
                }

                NotifyStateChanged();
            }
        }
    }

    public void TriggerDuplicateBuzz(string? customMsg = null)
    {
        lock (_lock)
        {
            LastDuplicateEventId = DateTime.UtcNow.Ticks;
            FastMoney.DuplicateWarningMessage = customMsg ?? "DUPLICATE ANSWER! TRY AGAIN!";
            FastMoney.DuplicateAlertTimestamp = DateTime.UtcNow.Ticks;
            NotifyStateChanged();
        }
    }

    public void ClearDuplicateWarning()
    {
        lock (_lock)
        {
            FastMoney.DuplicateWarningMessage = null;
            NotifyStateChanged();
        }
    }

    public void ToggleFastMoneyAnswerReveal(int playerNumber, int questionIndex)
    {
        lock (_lock)
        {
            var list = playerNumber == 1 ? FastMoney.Player1Answers : FastMoney.Player2Answers;
            if (questionIndex >= 0 && questionIndex < list.Count)
            {
                list[questionIndex].IsAnswerRevealed = !list[questionIndex].IsAnswerRevealed;
                if (list[questionIndex].IsAnswerRevealed)
                {
                    LastDingEventId = DateTime.UtcNow.Ticks;
                }
                NotifyStateChanged();
            }
        }
    }

    public void ToggleFastMoneyPointsReveal(int playerNumber, int questionIndex)
    {
        lock (_lock)
        {
            var list = playerNumber == 1 ? FastMoney.Player1Answers : FastMoney.Player2Answers;
            if (questionIndex >= 0 && questionIndex < list.Count)
            {
                list[questionIndex].IsPointsRevealed = !list[questionIndex].IsPointsRevealed;
                if (list[questionIndex].IsPointsRevealed)
                {
                    LastDingEventId = DateTime.UtcNow.Ticks;
                    if (FastMoney.HasWonGrandPrize)
                    {
                        LastWinEventId = DateTime.UtcNow.Ticks;
                    }
                }
                NotifyStateChanged();
            }
        }
    }

    public void RevealAllFastMoney(int playerNumber)
    {
        lock (_lock)
        {
            var list = playerNumber == 1 ? FastMoney.Player1Answers : FastMoney.Player2Answers;
            foreach (var slot in list)
            {
                slot.IsAnswerRevealed = true;
                slot.IsPointsRevealed = true;
            }
            LastDingEventId = DateTime.UtcNow.Ticks;
            if (FastMoney.HasWonGrandPrize)
            {
                LastWinEventId = DateTime.UtcNow.Ticks;
            }
            NotifyStateChanged();
        }
    }

    public void HideAllFastMoney(int playerNumber)
    {
        lock (_lock)
        {
            var list = playerNumber == 1 ? FastMoney.Player1Answers : FastMoney.Player2Answers;
            foreach (var slot in list)
            {
                slot.IsAnswerRevealed = false;
                slot.IsPointsRevealed = false;
            }
            NotifyStateChanged();
        }
    }

    public void ResetFastMoneyRound()
    {
        lock (_lock)
        {
            StopFastMoneyTimer();
            FastMoney.CurrentPlayer = 1;
            FastMoney.TimeRemaining = 20;
            FastMoney.DuplicateWarningMessage = null;
            InitFastMoneyPlayerSlots();
            NotifyStateChanged();
        }
    }

    public int FindFastMoneyPoints(int questionIndex, string answerText)
    {
        if (questionIndex < 0 || questionIndex >= FastMoney.Questions.Count || string.IsNullOrWhiteSpace(answerText))
            return 0;

        var q = FastMoney.Questions[questionIndex];
        var clean = answerText.Trim();

        var exact = q.Answers.FirstOrDefault(a =>
            string.Equals(a.Answer, clean, StringComparison.OrdinalIgnoreCase) ||
            (!string.IsNullOrWhiteSpace(a.Text) && string.Equals(a.Text, clean, StringComparison.OrdinalIgnoreCase)));
        if (exact != null) return exact.Points;

        var partial = q.Answers.FirstOrDefault(a =>
        {
            var text = !string.IsNullOrWhiteSpace(a.Answer) ? a.Answer : a.Text;
            if (string.IsNullOrEmpty(text)) return false;
            var parts = text.Split(new[] { '/', ',' }, StringSplitOptions.RemoveEmptyEntries);
            return parts.Any(p => string.Equals(p.Trim(), clean, StringComparison.OrdinalIgnoreCase)) ||
                   text.Contains(clean, StringComparison.OrdinalIgnoreCase) ||
                   clean.Contains(text, StringComparison.OrdinalIgnoreCase);
        });

        return partial?.Points ?? 0;
    }

    private void NotifyStateChanged() => OnChange?.Invoke();
}
