using OpenAI.Chat;
using Auto_UnitTest_Generator.Infrastructure.Configuration;

namespace Auto_UnitTest_Generator.Infrastructure.AI;

public class OpenAiClient : IAiClient
{
    private readonly ChatClient _chatClient;
    private readonly string _model;

    public OpenAiClient(AiConfiguration config)
    {
        if (string.IsNullOrWhiteSpace(config.ApiKey))
        {
            throw new ArgumentException("OpenAI API key is required.", nameof(config.ApiKey));
        }

        _model = config.Model;
        _chatClient = new ChatClient(model: _model, apiKey: config.ApiKey);
    }

    public async Task<string> GenerateCompletionAsync(string prompt, double temperature = 0.2)
    {
        try
        {
            var chatCompletion = await _chatClient.CompleteChatAsync(
                new ChatMessage[]
                {
                    new SystemChatMessage("You are an expert C# software engineer specializing in unit testing and test-driven development."),
                    new UserChatMessage(prompt)
                },
                new ChatCompletionOptions
                {
                    Temperature = (float)temperature
                });

            return chatCompletion.Value.Content[0].Text;
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException($"Failed to generate completion from OpenAI: {ex.Message}", ex);
        }
    }
}


