namespace Auto_UnitTest_Generator.Infrastructure.Constants;

public static class PromptConstants_Strategy1
{
    public const string GeneratorSystemPrompt = @"You are an expert C# software engineer specializing in unit testing.
Your task is to generate comprehensive, high-quality unit tests.

CRITICAL RULES:
1. Return ONLY raw C# code - NO markdown, NO explanations, NO ```csharp blocks
2. Generate complete, compilable test code
3. Use proper test framework syntax based on configuration
4. Include all necessary using statements
5. Use proper namespace matching the test project
6. Generate realistic mocks for dependencies
7. Cover happy paths, edge cases, and exceptions
8. Use Arrange-Act-Assert pattern
9. Write clear, descriptive test names
10. Aim for high branch coverage";

    public const string ReviewerSystemPrompt = @"You are a strict senior .NET code reviewer specializing in unit test quality.
Your task is to review generated unit tests and identify issues.

Review for:
- Compilation issues
- Invalid using statements or namespaces
- Hallucinated APIs or fake methods
- Weak or missing assertions
- Missing edge cases or exception tests
- Poor mock setup
- Async/await mistakes
- Missing Arrange-Act-Assert structure
- Duplicate or redundant tests
- Poor maintainability
- Bad naming conventions
- Branch coverage gaps
- Flaky test patterns
- Missing verification of mocks

CRITICAL: Return ONLY valid JSON in this exact format:
{
  ""isApproved"": false,
  ""issues"": [""issue1"", ""issue2""],
  ""fixInstructions"": [""instruction1"", ""instruction2""]
}

NO markdown, NO explanations, ONLY STRICT JSON. DO NOT WRAP WITH ```json";

    public const string FixAgentSystemPrompt = @"You are an expert C# software engineer specializing in fixing unit test issues.
Your task is to fix the provided test code based on reviewer feedback and build/test failures.

CRITICAL RULES:
1. Return ONLY raw C# code - NO markdown, NO explanations, NO ```csharp blocks
2. Fix all identified issues
3. Maintain existing test structure where possible
4. Ensure code compiles
5. Fix mock setups and verifications
6. Correct assertion logic
7. Handle async/await properly
8. Use proper exception handling";

    public static string BuildGeneratorPrompt(
        string sourceCode,
        string testFramework,
        string mockingLibrary,
        string assertionLibrary,
        int targetCoverage,
        bool generateAsync,
        bool generateExceptions,
        bool generateEdgeCases)
    {
        var prompt = $@"Generate comprehensive unit tests for the following C# code.

SOURCE CODE:
{sourceCode}

TEST FRAMEWORK: {testFramework}
MOCKING LIBRARY: {mockingLibrary}
ASSERTION LIBRARY: {assertionLibrary}
TARGET COVERAGE: {targetCoverage}%

REQUIREMENTS:
- Use {testFramework} test framework";

        if (testFramework.ToLowerInvariant() == "xunit")
        {
            prompt += "\n- Use [Fact] for tests, [Theory] for parameterized tests";
        }
        else if (testFramework.ToLowerInvariant() == "nunit")
        {
            prompt += "\n- Use [Test] for tests, [TestCase] for parameterized tests";
        }
        else if (testFramework.ToLowerInvariant() == "mstest")
        {
            prompt += "\n- Use [TestMethod] for tests, [DataTestMethod] for parameterized tests";
        }

        prompt += $"\n- Use {mockingLibrary} for mocking dependencies";
        
        if (assertionLibrary.ToLowerInvariant() == "fluentassertions")
        {
            prompt += "\n- Use FluentAssertions for assertions (e.g., result.Should().Be(expected))";
        }
        else
        {
            prompt += "\n- Use built-in assertions (e.g., Assert.Equal(expected, result))";
        }

        if (generateAsync)
        {
            prompt += "\n- Generate async tests for async methods";
        }

        if (generateExceptions)
        {
            prompt += "\n- Generate exception tests for error scenarios";
        }

        if (generateEdgeCases)
        {
            prompt += "\n- Generate edge case tests (null, empty, boundary values)";
        }

        prompt += $@"

IMPORTANT:
- Return ONLY the C# test code
- NO markdown formatting
- NO explanations
- Include all necessary using statements
- Use proper namespace
- Mock all constructor dependencies
- Verify mock invocations
- Aim for {targetCoverage}% coverage";

        return prompt;
    }

    public static string BuildReviewerPrompt(string sourceCode, string generatedTests, string testFramework)
    {
        return $@"Review the following generated unit tests for quality and correctness.

SOURCE CODE:
{sourceCode}

GENERATED TESTS:
{generatedTests}

TEST FRAMEWORK: {testFramework}

Perform a strict review and identify ALL issues. Return ONLY JSON in this format:
{{
  ""isApproved"": true/false,
  ""issues"": [""list of issues""],
  ""fixInstructions"": [""specific fix instructions""]
}}

If tests are perfect, return: {{""isApproved"": true, ""issues"": [], ""fixInstructions"": []}}";
    }

    public static string BuildFixPrompt(
        string sourceCode,
        string currentTests,
        List<string> issues,
        List<string> fixInstructions,
        string? buildErrors = null,
        string? testFailures = null)
    {
        var prompt = $@"Fix the following unit test code based on the identified issues.

SOURCE CODE:
{sourceCode}

CURRENT TESTS:
{currentTests}

ISSUES IDENTIFIED:
{string.Join("\n", issues.Select((issue, i) => $"{i + 1}. {issue}"))}

FIX INSTRUCTIONS:
{string.Join("\n", fixInstructions.Select((fix, i) => $"{i + 1}. {fix}"))}";

        if (!string.IsNullOrWhiteSpace(buildErrors))
        {
            prompt += $@"

BUILD ERRORS:
{buildErrors}";
        }

        if (!string.IsNullOrWhiteSpace(testFailures))
        {
            prompt += $@"

TEST FAILURES:
{testFailures}";
        }

        prompt += @"

Return ONLY the corrected C# test code. NO markdown, NO explanations.";

        return prompt;
    }

    public static string BuildCoverageImprovementPrompt(
        string sourceCode,
        string currentTests,
        double currentCoverage,
        int targetCoverage,
        List<string> uncoveredAreas)
    {
        return $@"Improve test coverage for the following code.

SOURCE CODE:
{sourceCode}

CURRENT TESTS:
{currentTests}

CURRENT COVERAGE: {currentCoverage:F1}%
TARGET COVERAGE: {targetCoverage}%

UNCOVERED AREAS:
{string.Join("\n", uncoveredAreas.Select((area, i) => $"{i + 1}. {area}"))}

Generate additional tests to cover the uncovered areas and reach the target coverage.
Return ONLY the COMPLETE updated test code (including existing tests + new tests).
NO markdown, NO explanations.";
    }
}


