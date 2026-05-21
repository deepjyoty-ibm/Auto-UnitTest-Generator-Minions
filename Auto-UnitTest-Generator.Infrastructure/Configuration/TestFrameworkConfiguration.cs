namespace Auto_UnitTest_Generator.Infrastructure.Configuration;

public class TestFrameworkConfiguration
{
    public string Type { get; set; } = "xunit";
    public string MockingLibrary { get; set; } = "Moq";
    public string AssertionLibrary { get; set; } = "FluentAssertions";
}


