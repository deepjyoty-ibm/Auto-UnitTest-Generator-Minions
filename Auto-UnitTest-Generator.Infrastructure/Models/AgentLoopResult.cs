namespace Auto_UnitTest_Generator.Infrastructure.Models;

public class AgentLoopResult
{
    public bool Success { get; set; }
    public int Attempts { get; set; }
    public string GeneratedTestCode { get; set; } = string.Empty;
    public double FinalCoverage { get; set; }
    public List<string> IterationLog { get; set; } = new();
    public List<string> CodeQualityWarnings { get; set; } = new();
    public string? ErrorMessage { get; set; }
}


