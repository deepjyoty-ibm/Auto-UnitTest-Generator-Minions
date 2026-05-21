namespace Auto_UnitTest_Generator.Infrastructure.Models;

public class ReviewResult
{
    public bool IsApproved { get; set; }
    public List<string> Issues { get; set; } = new();
    public List<string> FixInstructions { get; set; } = new();
}


