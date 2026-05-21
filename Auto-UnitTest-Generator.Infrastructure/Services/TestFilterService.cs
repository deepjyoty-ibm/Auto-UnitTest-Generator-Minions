using System.Text;
using System.Text.RegularExpressions;
using Auto_UnitTest_Generator.Infrastructure.Models;

namespace Auto_UnitTest_Generator.Infrastructure.Services;

/// <summary>
/// Service to filter test code and remove failing tests while keeping passing ones.
/// </summary>
public class TestFilterService
{
    /// <summary>
    /// Extracts failing test method names from test execution output.
    /// </summary>
    public List<string> ExtractFailingTestNames(TestExecutionResult testResult)
    {
        var failingTests = new List<string>();
        
        if (testResult.FailedTests == 0 || string.IsNullOrEmpty(testResult.Output))
        {
            return failingTests;
        }

        // Pattern to match test method names in xUnit output
        // Example: "   SampleConsoleApp.Tests.Services.UserServiceTests.RegisterUserAsync_WithInvalidEmail_ThrowsArgumentException"
        var testNamePattern = @"^\s+([A-Za-z_][A-Za-z0-9_\.]*\.([A-Za-z_][A-Za-z0-9_]*))\s*\[FAIL\]";
        var simplePattern = @"Failed\s+([A-Za-z_][A-Za-z0-9_\.]*\.([A-Za-z_][A-Za-z0-9_]*))";
        
        var lines = testResult.Output.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);
        
        foreach (var line in lines)
        {
            // Try detailed pattern first
            var match = Regex.Match(line, testNamePattern);
            if (match.Success)
            {
                var fullName = match.Groups[1].Value;
                var methodName = match.Groups[2].Value;
                failingTests.Add(methodName);
                continue;
            }
            
            // Try simple pattern
            match = Regex.Match(line, simplePattern);
            if (match.Success)
            {
                var methodName = match.Groups[2].Value;
                failingTests.Add(methodName);
            }
        }
        
        // Also check failure messages for test names
        foreach (var message in testResult.FailureMessages)
        {
            var match = Regex.Match(message, @"([A-Za-z_][A-Za-z0-9_]*)\s+\[FAIL\]");
            if (match.Success)
            {
                var testName = match.Groups[1].Value;
                if (!failingTests.Contains(testName))
                {
                    failingTests.Add(testName);
                }
            }
        }
        
        return failingTests.Distinct().ToList();
    }

    /// <summary>
    /// Removes failing test methods from the test code, keeping only passing tests.
    /// </summary>
    public string RemoveFailingTests(string testCode, List<string> failingTestNames)
    {
        if (string.IsNullOrEmpty(testCode) || failingTestNames == null || failingTestNames.Count == 0)
        {
            return testCode;
        }

        var lines = testCode.Split(new[] { "\r\n", "\r", "\n" }, StringSplitOptions.None);
        var result = new StringBuilder();
        var skipMode = false;
        var bracketCount = 0;
        var currentTestName = string.Empty;

        for (int i = 0; i < lines.Length; i++)
        {
            var line = lines[i];
            var trimmedLine = line.Trim();

            // Check if this line starts a test method
            if (!skipMode && (trimmedLine.StartsWith("[Fact]") || trimmedLine.StartsWith("[Theory]")))
            {
                // Look ahead to find the method name
                for (int j = i + 1; j < Math.Min(i + 5, lines.Length); j++)
                {
                    var nextLine = lines[j].Trim();
                    var methodMatch = Regex.Match(nextLine, @"public\s+(?:async\s+)?(?:Task|void)\s+([A-Za-z_][A-Za-z0-9_]*)\s*\(");
                    if (methodMatch.Success)
                    {
                        currentTestName = methodMatch.Groups[1].Value;
                        
                        // Check if this test is in the failing list
                        if (failingTestNames.Any(ft => ft.Equals(currentTestName, StringComparison.OrdinalIgnoreCase)))
                        {
                            skipMode = true;
                            bracketCount = 0;
                        }
                        break;
                    }
                }
            }

            if (skipMode)
            {
                // Count opening and closing braces to know when the method ends
                bracketCount += line.Count(c => c == '{');
                bracketCount -= line.Count(c => c == '}');

                // When bracket count returns to 0, we've exited the method
                if (bracketCount <= 0 && line.Contains('}'))
                {
                    skipMode = false;
                    currentTestName = string.Empty;
                    continue; // Skip the closing brace line
                }
                continue; // Skip all lines in failing test
            }

            result.AppendLine(line);
        }

        return result.ToString();
    }

    /// <summary>
    /// Counts the number of test methods in the code.
    /// </summary>
    public int CountTestMethods(string testCode)
    {
        if (string.IsNullOrEmpty(testCode))
        {
            return 0;
        }

        var factCount = Regex.Matches(testCode, @"\[Fact\]").Count;
        var theoryCount = Regex.Matches(testCode, @"\[Theory\]").Count;
        
        return factCount + theoryCount;
    }
}

