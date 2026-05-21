namespace Auto_UnitTest_Generator.Infrastructure.Configuration;

public class AiConfiguration
{
    public string Provider { get; set; } = "OpenAI";
    public string ApiKey { get; set; } = "env:OPENAI_API_KEY";
    public string Model { get; set; } = "gpt-4o-mini";
    public double Temperature { get; set; } = 0.2;
}


