namespace Auto_UnitTest_Generator.Infrastructure.Models;

public class GenerateRequest
{
    public required string SourceFilePath { get; set; }
    public required string TestFilePath { get; set; }
    public int TargetCoverage { get; set; }
    public string? SolutionPath { get; set; }
    public string? SourceCode { get; set; }
    public string? Namespace { get; set; }
    public List<string> Dependencies { get; set; } = new();
}


