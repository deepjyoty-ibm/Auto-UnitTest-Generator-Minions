using Auto_UnitTest_Generator.Infrastructure.AI;
using Auto_UnitTest_Generator.Infrastructure.Configuration;
using Auto_UnitTest_Generator.Infrastructure.Constants;
using Auto_UnitTest_Generator.Infrastructure.Helpers;
using Auto_UnitTest_Generator.Infrastructure.Services;

namespace Auto_UnitTest_Generator.Infrastructure.Agents;

public class FixAgent : IAgent
{
    private readonly IAiClient _aiClient;
    private readonly ToolConfiguration _config;

    public FixAgent(IAiClient aiClient, ToolConfiguration config)
    {
        _aiClient = aiClient;
        _config = config;
    }

    public async Task<string> ExecuteAsync(string input)
    {
        // This is a simple wrapper for consistency
        throw new NotImplementedException("Use FixTestsAsync instead");
    }

    public async Task<string> FixTestsAsync(
        string sourceCode,
        string currentTests,
        List<string> issues,
        List<string> fixInstructions,
        string? buildErrors = null,
        string? testFailures = null)
    {
        var prompt = PromptConstants.BuildFixPrompt(
            sourceCode,
            currentTests,
            issues,
            fixInstructions,
            buildErrors,
            testFailures
        );

        var response = await _aiClient.GenerateCompletionAsync(prompt, _config.Ai.Temperature);

        // Extract code from response
        var code = JsonExtractionHelper.ExtractCodeBlock(response, "csharp");

        // Ensure all required using statements are present
        code = CodeSanitizerService.EnsureRequiredUsings(code);
        code = CodeSanitizerService.RemoveDuplicateUsings(code);

        return code;
    }
}


