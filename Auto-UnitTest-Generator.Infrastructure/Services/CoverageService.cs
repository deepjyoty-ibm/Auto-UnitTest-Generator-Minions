using System.Diagnostics;
using System.Text;
using System.Linq;
using System.Text.RegularExpressions;
using System.Xml.Linq;
using Auto_UnitTest_Generator.Infrastructure.Models;

namespace Auto_UnitTest_Generator.Infrastructure.Services;

public class CoverageService
{
    // Now accepts optional sourceFilePath to compute coverage for a specific source file when possible
    public async Task<CoverageResult> RunCoverageAsync(string projectPath, int targetCoverage, string? sourceFilePath = null)
    {
        var result = new CoverageResult();

        try
        {
            var projectDirectory = Path.GetDirectoryName(projectPath);
            if (string.IsNullOrEmpty(projectDirectory))
            {
                result.LineCoverage = 0;
                result.BranchCoverage = 0;
                result.MeetsTarget = false;
                return result;
            }

            var processInfo = new ProcessStartInfo
            {
                FileName = "dotnet",
                Arguments = $"test \"{projectPath}\" --collect:\"XPlat Code Coverage\" --no-build",
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

            // Try to find generated coverage files (cobertura / opencover) and parse line coverage
            try
            {
                var coverageFiles = Directory.GetFiles(projectDirectory, "*coverage*.xml", SearchOption.AllDirectories)
                    .Concat(Directory.GetFiles(projectDirectory, "coverage*.xml", SearchOption.AllDirectories))
                    .Concat(Directory.GetFiles(projectDirectory, "*coverage.cobertura.xml", SearchOption.AllDirectories))
                    .Distinct()
                    .ToArray();

                if (coverageFiles.Length > 0)
                {
                    // Prefer cobertura-style files
                    var file = coverageFiles.OrderBy(f => f).First();
                    // Log which file we parsed for easier debugging
                    Console.WriteLine($"  🔍 Coverage file found: {file}");
                    result.Output += "\nCoverageFile: " + file;
                    var doc = XDocument.Load(file);
                    var root = doc.Root;
                    if (root != null)
                    {
                        // If a specific source file was requested, try to compute coverage for that file only
                        if (!string.IsNullOrWhiteSpace(sourceFilePath))
                        {
                            try
                            {
                                var normalizedSourcePath = sourceFilePath.Replace("\\", "/");
                                var sourceFileName = Path.GetFileName(normalizedSourcePath);

                                // Cobertura: classes/class elements with filename attribute
                                var classNodes = doc.Descendants("class")
                                    .Where(x => (string?)x.Attribute("filename") != null)
                                    .ToList();

                                Console.WriteLine("📂 Checking coverage XML Files:");

                                Console.WriteLine($"🎯 Looking for coverage of: {normalizedSourcePath}");

                                IEnumerable<XElement> matched = classNodes.Where(c =>
                                {
                                    var fn = ((string?)c.Attribute("filename") ?? string.Empty)
                                        .Replace("\\", "/");

                                    return fn.Equals(
                                               normalizedSourcePath,
                                               StringComparison.OrdinalIgnoreCase)
                                           || fn.EndsWith(
                                               normalizedSourcePath,
                                               StringComparison.OrdinalIgnoreCase)
                                           || fn.EndsWith(
                                               sourceFileName,
                                               StringComparison.OrdinalIgnoreCase);
                                });

                                Console.WriteLine($"✅ Matched Coverage Files: {matched.Count()}");

                                // Fallback: file/path attribute
                                if (!matched.Any())
                                {
                                    matched = doc.Descendants().Where(e =>
                                    {
                                        var fn = (string?)e.Attribute("file")
                                                 ?? (string?)e.Attribute("path");

                                        if (string.IsNullOrWhiteSpace(fn))
                                        {
                                            return false;
                                        }

                                        fn = fn.Replace("\\", "/");

                                        return fn.Equals(normalizedSourcePath, StringComparison.OrdinalIgnoreCase)
                                               || fn.EndsWith(normalizedSourcePath,StringComparison.OrdinalIgnoreCase)
                                               || fn.EndsWith(sourceFileName,StringComparison.OrdinalIgnoreCase);
                                    });
                                }


                                int totalLines = 0;
                                int coveredLines = 0;

                                foreach (var node in matched)
                                {
                                    // Cobertura lines: node/lines/line elements with hits attribute
                                    var lines = node.Descendants("line").ToList();
                                    if (!lines.Any())
                                    {
                                        // some formats use 'lines' elements or 'Line' nodes
                                        lines = node.Descendants().Where(d => d.Name.LocalName.Equals("line", StringComparison.OrdinalIgnoreCase)).ToList();
                                    }

                                    foreach (var ln in lines)
                                    {
                                        var hitsAttr = (string?)ln.Attribute("hits") ?? (string?)ln.Attribute("count") ?? (string?)ln.Attribute("value");
                                        if (int.TryParse(hitsAttr, out var hits))
                                        {
                                            totalLines++;
                                            if (hits > 0) coveredLines++;
                                        }
                                        else
                                        {
                                            // If no hits attribute, try 'branch' or 'covered' indicators
                                            var coveredAttr = (string?)ln.Attribute("covered") ?? (string?)ln.Attribute("isCovered");
                                            if (!string.IsNullOrEmpty(coveredAttr) && (coveredAttr.Equals("true", StringComparison.OrdinalIgnoreCase) || coveredAttr == "1"))
                                            {
                                                totalLines++;
                                                coveredLines++;
                                            }
                                        }
                                    }
                                }

                                if (totalLines > 0)
                                {
                                    var pct = (coveredLines / (double)totalLines) * 100.0;
                                    result.LineCoverage = pct;
                                    result.BranchCoverage = 0;
                                    result.MeetsTarget = result.LineCoverage >= targetCoverage;
                                    Console.WriteLine($"  🔎 Parsed file line coverage for {sourceFilePath}: {result.LineCoverage:F2}% (target: {targetCoverage}%)");
                                    result.Output += $"\nParsedFileLineCoverage: {result.LineCoverage:F2}%";
                                }
                                else
                                {
                                    // fallback to overall parsing below
                                }
                            }
                            catch
                            {
                                // fallback to overall parsing below
                            }
                        }

                        // If no per-file coverage computed, fall back to overall metrics
                        if (result.LineCoverage <= 0)
                        {
                            double lineCoverage = 0;

                            // Cobertura uses either line-rate or lines-covered/lines-valid
                            var lineRateAttr = root.Attribute("line-rate") ?? root.Attribute("lineRate");
                            if (lineRateAttr != null && double.TryParse(lineRateAttr.Value, out var lr))
                            {
                                lineCoverage = lr * 100.0;
                            }
                            else
                            {
                                var linesCovered = root.Attribute("lines-covered") ?? root.Attribute("linesCovered");
                                var linesValid = root.Attribute("lines-valid") ?? root.Attribute("linesValid");
                                if (linesCovered != null && linesValid != null
                                    && double.TryParse(linesCovered.Value, out var lc)
                                    && double.TryParse(linesValid.Value, out var lv)
                                    && lv > 0)
                                {
                                    lineCoverage = (lc / lv) * 100.0;
                                }
                                else
                                {
                                    // Try common OpenCover summary elements
                                    var summary = doc.Descendants("Summary").FirstOrDefault();
                                    if (summary != null)
                                    {
                                        var visited = summary.Attribute("visitedSequencePoints");
                                        var total = summary.Attribute("numSequencePoints");
                                        if (visited != null && total != null
                                            && double.TryParse(visited.Value, out var vs)
                                            && double.TryParse(total.Value, out var ts)
                                            && ts > 0)
                                        {
                                            lineCoverage = (vs / ts) * 100.0;
                                        }
                                    }
                                }
                            }

                            if (lineCoverage > 0)
                            {
                                result.LineCoverage = lineCoverage;
                                result.BranchCoverage = 0;
                                result.MeetsTarget = result.LineCoverage >= targetCoverage;
                                Console.WriteLine($"  🔎 Parsed line coverage: {result.LineCoverage:F2}% (target: {targetCoverage}%)");
                                result.Output += $"\nParsedLineCoverage: {result.LineCoverage:F2}%";
                            }
                        }
                    }
                }
            }
            catch
            {
                // ignore parsing errors and fall back to stdout parsing
            }

            // If we couldn't determine coverage from files, parse stdout as fallback
            if (result.LineCoverage <= 0)
            {
                // Parse coverage results from output
                ParseCoverageResults(result, targetCoverage);
            }
        }
        catch (Exception ex)
        {
            result.LineCoverage = 0;
            result.BranchCoverage = 0;
            result.MeetsTarget = false;
            result.Output = $"Coverage execution failed: {ex.Message}";
        }

        return result;
    }

    private void ParseCoverageResults(CoverageResult result, int targetCoverage)
    {
        var output = result.Output;

        // Try to parse line coverage percentage
        var lineCoverageMatch = Regex.Match(output, @"Line\s*coverage:\s*(\d+\.?\d*)%", RegexOptions.IgnoreCase);
        if (lineCoverageMatch.Success)
        {
            result.LineCoverage = double.Parse(lineCoverageMatch.Groups[1].Value);
        }
        else
        {
            // Alternative pattern
            var altMatch = Regex.Match(output, @"\|\s*Total\s*\|\s*\d+\.?\d*%\s*\|\s*(\d+\.?\d*)%", RegexOptions.IgnoreCase);
            if (altMatch.Success)
            {
                result.LineCoverage = double.Parse(altMatch.Groups[1].Value);
            }
        }

        // Try to parse branch coverage percentage
        var branchCoverageMatch = Regex.Match(output, @"Branch\s*coverage:\s*(\d+\.?\d*)%", RegexOptions.IgnoreCase);
        if (branchCoverageMatch.Success)
        {
            result.BranchCoverage = double.Parse(branchCoverageMatch.Groups[1].Value);
        }

        // Check if target is met
        result.MeetsTarget = result.LineCoverage >= targetCoverage;

        // If coverage is low, try to identify uncovered areas
        if (!result.MeetsTarget)
        {
            result.UncoveredLines.Add($"Current coverage: {result.LineCoverage:F1}%, Target: {targetCoverage}%");
            result.UncoveredLines.Add("Additional test coverage needed to reach target.");
        }
    }
}


