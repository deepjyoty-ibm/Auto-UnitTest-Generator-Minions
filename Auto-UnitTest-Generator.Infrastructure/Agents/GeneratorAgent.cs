using Auto_UnitTest_Generator.Infrastructure.AI;
using Auto_UnitTest_Generator.Infrastructure.Configuration;
using Auto_UnitTest_Generator.Infrastructure.Constants;
using Auto_UnitTest_Generator.Infrastructure.Helpers;
using Auto_UnitTest_Generator.Infrastructure.Services;

namespace Auto_UnitTest_Generator.Infrastructure.Agents;

public class GeneratorAgent : IAgent
{
    private readonly IAiClient _aiClient;
    private readonly ToolConfiguration _config;

    public GeneratorAgent(IAiClient aiClient, ToolConfiguration config)
    {
        _aiClient = aiClient;
        _config = config;
    }

    public async Task<string> ExecuteAsync(string sourceCode)
    {
        return await ExecuteAsync(sourceCode, null);
    }

    public async Task<string> ExecuteAsync(string sourceCode, string? sourceFilePath)
    {
        // Extract a detailed manifest from the source code (namespace, constructor, methods, csproj refs)
        var manifest = CodeContextExtractor.ExtractDetailedManifest(sourceCode, sourceFilePath ?? string.Empty);

        var prompt = PromptConstants.BuildGeneratorPrompt(
            sourceCode,
            manifest,
            _config.TestFramework.Type,
            _config.TestFramework.MockingLibrary,
            _config.TestFramework.AssertionLibrary,
            _config.Coverage.Target,
            _config.Generation.GenerateAsyncTests,
            _config.Generation.GenerateExceptionTests,
            _config.Generation.GenerateEdgeCases
        );

        // Clamp temperature to deterministic range
        var temperature = Math.Clamp(_config.Ai.Temperature, 0.0, 0.2);
        var response = await _aiClient.GenerateCompletionAsync(prompt, temperature);

        // Extract code from response (remove markdown if present)
        var code = JsonExtractionHelper.ExtractCodeBlock(response, "csharp");

        // Ensure all required using statements are present
        code = CodeSanitizerService.EnsureRequiredUsings(code);
        code = CodeSanitizerService.RemoveDuplicateUsings(code);

        return code;
    }

    public async Task<string> GenerateAdditionalTestsAsync(
        string sourceCode,
        string? sourceFilePath,
        string currentTests,
        double currentCoverage,
        List<string> uncoveredAreas)
    {
        var prompt = PromptConstants.BuildCoverageImprovementPrompt(
            sourceCode,
            currentTests,
            currentCoverage,
            _config.Coverage.Target,
            uncoveredAreas
        );
        // Clamp temperature for deterministic improvements
        var temperature = Math.Clamp(_config.Ai.Temperature, 0.0, 0.2);
        var response = await _aiClient.GenerateCompletionAsync(prompt, temperature);

        // Extract code from response
        var code = JsonExtractionHelper.ExtractCodeBlock(response, "csharp");

        return code;
    }
}


