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
}
