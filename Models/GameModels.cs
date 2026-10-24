namespace FamilyFeudLive.Models;

public class AnswerItem
{
    public int Id { get; set; }
    public string Text { get; set; } = string.Empty;
    public int Points { get; set; }
    public bool IsRevealed { get; set; }
}

public class Question
{
    public int Id { get; set; }
    public string Text { get; set; } = string.Empty;
    public int Multiplier { get; set; } = 1;
    public List<AnswerItem> Answers { get; set; } = new();
}

public class Team
{
    public string Name { get; set; } = string.Empty;
    public string Color { get; set; } = "#dc3545"; // Default red
    public int Score { get; set; }
}

public class BuzzerState
{
    public bool IsActive { get; set; }
    public bool IsLocked { get; set; }
    public string? WinnerTeam { get; set; }
    public string? WinnerPlayer { get; set; }
    public DateTime? WinnerTimestamp { get; set; }
}

public class JsonImportAnswer
{
    public string? Answer { get; set; }
    public string? Text { get; set; }
    public int Points { get; set; }
    public int PeopleWithAnswer { get; set; }
}

public class JsonImportQuestion
{
    public string? Question { get; set; }
    public string? Text { get; set; }
    public int Multiplier { get; set; } = 1;
    public int PointsPerAnswer { get; set; }
    public List<JsonImportAnswer>? Answers { get; set; }
}

public class JsonImportRoot
{
    public List<JsonImportQuestion>? Rounds { get; set; }
    public List<JsonImportQuestion>? Questions { get; set; }
    public List<FastMoneyQuestion>? FastMoneyQuestions { get; set; }
    public List<FastMoneyQuestion>? FastMoneyQuestion { get; set; }
}

public class FastMoneyAnswer
{
    public string Answer { get; set; } = string.Empty;
    public string? Text { get; set; }
    public int Points { get; set; }
}

public class FastMoneyQuestion
{
    public int QuestionId { get; set; }
    public string Question { get; set; } = string.Empty;
    public string? Text { get; set; }
    public bool IsRevealed { get; set; }
    public List<FastMoneyAnswer> Answers { get; set; } = new();
}

public class FastMoneyPlayerAnswer
{
    public int QuestionIndex { get; set; }
    public string AnswerText { get; set; } = string.Empty;
    public int Points { get; set; }
    public bool IsAnswerRevealed { get; set; }
    public bool IsPointsRevealed { get; set; }
    public bool IsDuplicate { get; set; }
}

public class FastMoneyState
{
    public bool IsActive { get; set; }
    public int WinningTeamNumber { get; set; } = 1;
    public string WinningTeamName { get; set; } = string.Empty;
    public int CurrentPlayer { get; set; } = 1;
    public int TimeRemaining { get; set; } = 20;
    public bool IsTimerRunning { get; set; }
    public List<FastMoneyQuestion> Questions { get; set; } = new();
    public List<FastMoneyPlayerAnswer> Player1Answers { get; set; } = new();
    public List<FastMoneyPlayerAnswer> Player2Answers { get; set; } = new();
    public string? DuplicateWarningMessage { get; set; }
    public long DuplicateAlertTimestamp { get; set; }

    public int Player1Total => Player1Answers.Where(a => a.IsPointsRevealed).Sum(a => a.Points);
    public int Player2Total => Player2Answers.Where(a => a.IsPointsRevealed).Sum(a => a.Points);
    public int CombinedRevealedTotal => Player1Total + Player2Total;
    public bool HasWonGrandPrize => CombinedRevealedTotal >= 200;
}
