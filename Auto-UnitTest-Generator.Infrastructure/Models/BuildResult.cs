namespace Auto_UnitTest_Generator.Infrastructure.Models;

public class BuildResult
{
    public bool Success { get; set; }
    public string Output { get; set; } = string.Empty;
    public List<string> Errors { get; set; } = new();
}


