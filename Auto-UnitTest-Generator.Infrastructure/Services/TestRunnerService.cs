using System.Diagnostics;
using System.Text;
using System.Text.RegularExpressions;
using Auto_UnitTest_Generator.Infrastructure.Models;

namespace Auto_UnitTest_Generator.Infrastructure.Services;

public class TestRunnerService
{
    public async Task<TestExecutionResult> RunTestsAsync(string projectPath)
    {
        var result = new TestExecutionResult();

        try
        {
            var projectDirectory = Path.GetDirectoryName(projectPath);
            if (string.IsNullOrEmpty(projectDirectory))
            {
                result.Success = false;
                result.FailureMessages.Add("Invalid project path");
                return result;
            }

            var processInfo = new ProcessStartInfo
            {
                FileName = "dotnet",
                Arguments = $"test \"{projectPath}\" --no-build --verbosity normal",
                WorkingDirectory = projectDirectory,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true
            };

            var output = new StringBuilder();

            using var process = new Process { StartInfo = processInfo };
            
            process.OutputDataReceived += (sender, e) =>
            {
                if (!string.IsNullOrEmpty(e.Data))
                {
                    output.AppendLine(e.Data);
                }
            };

            process.ErrorDataReceived += (sender, e) =>
            {
                if (!string.IsNullOrEmpty(e.Data))
                {
                    output.AppendLine(e.Data);
                }
            };

            process.Start();
            process.BeginOutputReadLine();
            process.BeginErrorReadLine();
            await process.WaitForExitAsync();

            result.Output = output.ToString();
            result.Success = process.ExitCode == 0;

            // Parse test results
            ParseTestResults(result);
        }
        catch (Exception ex)
        {
            result.Success = false;
            result.FailureMessages.Add($"Test execution failed: {ex.Message}");
        }

        return result;
    }

    private void ParseTestResults(TestExecutionResult result)
    {
        var output = result.Output;

        // Parse total tests
        var totalMatch = Regex.Match(output, @"Total tests:\s*(\d+)", RegexOptions.IgnoreCase);
        if (totalMatch.Success)
        {
            result.TotalTests = int.Parse(totalMatch.Groups[1].Value);
        }

        // Parse passed tests
        var passedMatch = Regex.Match(output, @"Passed:\s*(\d+)", RegexOptions.IgnoreCase);
        if (passedMatch.Success)
        {
            result.PassedTests = int.Parse(passedMatch.Groups[1].Value);
        }

        // Parse failed tests
        var failedMatch = Regex.Match(output, @"Failed:\s*(\d+)", RegexOptions.IgnoreCase);
        if (failedMatch.Success)
        {
            result.FailedTests = int.Parse(failedMatch.Groups[1].Value);
        }

        // Extract failure messages
        if (result.FailedTests > 0)
        {
            var lines = output.Split('\n');
            var inFailureSection = false;

            foreach (var line in lines)
            {
                if (line.Contains("Failed!") || line.Contains("Error Message:") || line.Contains("Stack Trace:"))
                {
                    inFailureSection = true;
                }

                if (inFailureSection && !string.IsNullOrWhiteSpace(line))
                {
                    result.FailureMessages.Add(line.Trim());
                }

                if (line.Contains("Test Run Failed."))
                {
                    inFailureSection = false;
                }
            }
        }
    }
}


