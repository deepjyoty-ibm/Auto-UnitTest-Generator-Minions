namespace Auto_UnitTest_Generator.Infrastructure.Models;

public class CoverageResult
{
    public double LineCoverage { get; set; }
    public double BranchCoverage { get; set; }
    public bool MeetsTarget { get; set; }
    public List<string> UncoveredLines { get; set; } = new();
    public List<string> UnreachableCodeWarnings { get; set; } = new();
    public string Output { get; set; } = string.Empty;
}


