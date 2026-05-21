namespace Auto_UnitTest_Generator.Infrastructure.Configuration;

public class ToolConfiguration
{
    public AiConfiguration Ai { get; set; } = new();
    public TestFrameworkConfiguration TestFramework { get; set; } = new();
    public CoverageConfiguration Coverage { get; set; } = new();
    public ReviewerConfiguration Reviewer { get; set; } = new();
    public GenerationConfiguration Generation { get; set; } = new();
}


