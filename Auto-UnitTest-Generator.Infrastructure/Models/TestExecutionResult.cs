namespace Auto_UnitTest_Generator.Infrastructure.Models;

public class TestExecutionResult
{
    public bool Success { get; set; }
    public int TotalTests { get; set; }
    public int PassedTests { get; set; }
    public int FailedTests { get; set; }
    public List<string> FailureMessages { get; set; } = new();
    public string Output { get; set; } = string.Empty;
}


