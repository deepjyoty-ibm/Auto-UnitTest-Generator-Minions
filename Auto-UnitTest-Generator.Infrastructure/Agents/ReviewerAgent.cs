using Auto_UnitTest_Generator.Infrastructure.AI;
using Auto_UnitTest_Generator.Infrastructure.Configuration;
using Auto_UnitTest_Generator.Infrastructure.Constants;
using Auto_UnitTest_Generator.Infrastructure.Helpers;
using Auto_UnitTest_Generator.Infrastructure.Models;

namespace Auto_UnitTest_Generator.Infrastructure.Agents;

public class ReviewerAgent : IAgent
{
    private readonly IAiClient _aiClient;
    private readonly ToolConfiguration _config;

    public ReviewerAgent(IAiClient aiClient, ToolConfiguration config)
    {
        _aiClient = aiClient;
        _config = config;
    }

    public async Task<string> ExecuteAsync(string input)
    {
        // This is a simple wrapper for consistency
        throw new NotImplementedException("Use ReviewTestsAsync instead");
    }

    public async Task<ReviewResult> ReviewTestsAsync(string sourceCode, string generatedTests)
    {
        var prompt = PromptConstants.BuildReviewerPrompt(
            sourceCode,
            generatedTests,
            _config.TestFramework.Type
        );

        var response = await _aiClient.GenerateCompletionAsync(prompt, 0.1); // Lower temperature for consistency
        response = response.Replace("```json", "").Replace("```", "").Trim();
        // Extract JSON from response
        var reviewResult = JsonExtractionHelper.ExtractJson<ReviewResult>(response);

        if (reviewResult == null)
        {
            // If JSON extraction fails, assume there are issues
            return new ReviewResult
            {
                IsApproved = false,
                Issues = new List<string> { "Failed to parse review response" },
                FixInstructions = new List<string> { "Regenerate tests with proper structure" }
            };
        }

        return reviewResult;
    }
}


