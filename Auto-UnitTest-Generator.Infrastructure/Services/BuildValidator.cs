using System.Diagnostics;
using System.Text;
using Auto_UnitTest_Generator.Infrastructure.Models;

namespace Auto_UnitTest_Generator.Infrastructure.Services;

public class BuildValidator
{
    public async Task<BuildResult> BuildProjectAsync(string projectPath)
    {
        Console.WriteLine($"Building Project: {projectPath} in BuildValidator");
        var result = new BuildResult();

        try
        {
            var projectDirectory = Path.GetDirectoryName(projectPath);
            if (string.IsNullOrEmpty(projectDirectory))
            {
                result.Success = false;
                result.Errors.Add("Invalid project path");
                return result;
            }

            var processInfo = new ProcessStartInfo
            {
                FileName = "dotnet",
                Arguments = $"build \"{projectPath}\"",
                WorkingDirectory = projectDirectory,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true
            };

            var output = new StringBuilder();
            var errors = new StringBuilder();

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
                    errors.AppendLine(e.Data);
                }
            };

            process.Start();
            process.BeginOutputReadLine();
            process.BeginErrorReadLine();
            await process.WaitForExitAsync();

            result.Output = output.ToString();
            result.Success = process.ExitCode == 0;

            if (!result.Success)
            {
                // If build failed, attempt a restore and rebuild once to resolve ProjectReference/PackageReference paths
                var restoreInfo = new ProcessStartInfo
                {
                    FileName = "dotnet",
                    Arguments = $"restore \"{projectPath}\"",
                    WorkingDirectory = projectDirectory,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    UseShellExecute = false,
                    CreateNoWindow = true
                };

                try
                {
                    using var restoreProcess = new Process { StartInfo = restoreInfo };
                    var restoreOut = new StringBuilder();
                    var restoreErr = new StringBuilder();
                    restoreProcess.OutputDataReceived += (s, e) => { if (!string.IsNullOrEmpty(e.Data)) restoreOut.AppendLine(e.Data); };
                    restoreProcess.ErrorDataReceived += (s, e) => { if (!string.IsNullOrEmpty(e.Data)) restoreErr.AppendLine(e.Data); };
                    restoreProcess.Start();
                    restoreProcess.BeginOutputReadLine();
                    restoreProcess.BeginErrorReadLine();
                    await restoreProcess.WaitForExitAsync();

                    // Try build again once
                    using var retryProcess = new Process { StartInfo = processInfo };
                    var retryOut = new StringBuilder();
                    var retryErr = new StringBuilder();
                    retryProcess.OutputDataReceived += (s, e) => { if (!string.IsNullOrEmpty(e.Data)) retryOut.AppendLine(e.Data); };
                    retryProcess.ErrorDataReceived += (s, e) => { if (!string.IsNullOrEmpty(e.Data)) retryErr.AppendLine(e.Data); };
                    retryProcess.Start();
                    retryProcess.BeginOutputReadLine();
                    retryProcess.BeginErrorReadLine();
                    await retryProcess.WaitForExitAsync();

                    result.Output += "\n--- Restore Output ---\n" + restoreOut.ToString();
                    result.Output += "\n--- Retry Build Output ---\n" + retryOut.ToString();
                    result.Success = retryProcess.ExitCode == 0;

                    if (!result.Success)
                    {
                        var combinedRetry = retryErr.ToString();
                        if (!string.IsNullOrEmpty(combinedRetry)) result.Errors.Add(combinedRetry);
                    }
                }
                catch
                {
                    // ignore restore errors
                }

                var errorOutput = errors.ToString();
                if (!string.IsNullOrEmpty(errorOutput))
                {
                    result.Errors.Add(errorOutput);
                }

                // Parse build errors from output
                var lines = result.Output.Split('\n');
                foreach (var line in lines)
                {
                    if (line.IndexOf("error cs", StringComparison.OrdinalIgnoreCase) >= 0
                        || line.IndexOf("error:", StringComparison.OrdinalIgnoreCase) >= 0
                        || line.IndexOf("warning treated as error", StringComparison.OrdinalIgnoreCase) >= 0)
                    {
                        result.Errors.Add(line.Trim());
                    }
                }

                // If we couldn't parse any specific errors, include the full output and exit code for diagnostics
                if (!result.Errors.Any())
                {
                    var combined = new StringBuilder();
                    combined.AppendLine($"dotnet build exited with code {process.ExitCode}.");
                    if (!string.IsNullOrWhiteSpace(errorOutput))
                    {
                        combined.AppendLine("Standard Error:");
                        combined.AppendLine(errorOutput.Trim());
                    }
                    if (!string.IsNullOrWhiteSpace(result.Output))
                    {
                        combined.AppendLine("Standard Output:");
                        combined.AppendLine(result.Output.Trim());
                    }

                    // Add a truncated combined message to avoid extremely long messages
                    var combinedMsg = combined.ToString();
                    if (combinedMsg.Length > 8000)
                    {
                        combinedMsg = combinedMsg[..8000] + "\n...output truncated...";
                    }

                    result.Errors.Add(combinedMsg);
                }
            }
        }
        catch (Exception ex)
        {
            result.Success = false;
            result.Errors.Add($"Build execution failed: {ex.Message}");
        }

        return result;
    }
}


