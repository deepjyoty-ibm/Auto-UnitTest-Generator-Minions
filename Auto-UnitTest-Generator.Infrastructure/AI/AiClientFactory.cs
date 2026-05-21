using Auto_UnitTest_Generator.Infrastructure.Configuration;
using static System.Runtime.InteropServices.JavaScript.JSType;

namespace Auto_UnitTest_Generator.Infrastructure.AI;

public class AiClientFactory
{
    public static IAiClient Create(AiConfiguration config)
    {
        if (config == null)
        {
            throw new ArgumentNullException(nameof(config));
        }

        if (string.IsNullOrWhiteSpace(config.Provider))
        {
            throw new ArgumentException("AI provider is required.", nameof(config.Provider));
        }
        else  if (!ConfigLoader.ValidProviders.Contains(config.Provider.ToLowerInvariant()))
        {
            throw new ArgumentException($"Invalid AI provider: {config.Provider.ToLowerInvariant()}. Valid options: {string.Join(", ", ConfigLoader.ValidProviders)}");
        }

        return config.Provider.ToLowerInvariant() switch
        {
            "openai" => new OpenAiClient(config),
            "groq" => new GroqAiClient(config),
            _ => throw new NotSupportedException($"AI provider '{config.Provider}' is not supported. Supported providers: OpenAI, Groq")
        };
    }
}


