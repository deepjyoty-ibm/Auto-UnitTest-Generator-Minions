using Auto_UnitTest_Generator.Infrastructure.Agents;
using Auto_UnitTest_Generator.Infrastructure.Configuration;
using Auto_UnitTest_Generator.Infrastructure.Models;
using System.Linq;
using System.Xml.Linq;

namespace Auto_UnitTest_Generator.Infrastructure.Services;

public class AgentOrchestrator
{
    private readonly GeneratorAgent _generatorAgent;
    private readonly ReviewerAgent _reviewerAgent;
    private readonly FixAgent _fixAgent;
    private readonly FileWriterService _fileWriter;
    private readonly BuildValidator _buildValidator;
    private readonly TestRunnerService _testRunner;
    private readonly CoverageService _coverageService;
    private readonly TestFilterService _testFilterService;
    private readonly CodeQualityAnalyzer _codeQualityAnalyzer;
    private readonly ToolConfiguration _config;

    public AgentOrchestrator(
        GeneratorAgent generatorAgent,
        ReviewerAgent reviewerAgent,
        FixAgent fixAgent,
        FileWriterService fileWriter,
        BuildValidator buildValidator,
        TestRunnerService testRunner,
        CoverageService coverageService,
        TestFilterService testFilterService,
        CodeQualityAnalyzer codeQualityAnalyzer,
        ToolConfiguration config)
    {
        _generatorAgent = generatorAgent;
        _reviewerAgent = reviewerAgent;
        _fixAgent = fixAgent;
        _fileWriter = fileWriter;
        _buildValidator = buildValidator;
        _testRunner = testRunner;
        _coverageService = coverageService;
        _testFilterService = testFilterService;
        _codeQualityAnalyzer = codeQualityAnalyzer;
        _config = config;
    }

    private static void CopyDirectory(string sourceDir, string destinationDir)
    {
        var dir = new DirectoryInfo(sourceDir);
        if (!dir.Exists)
        {
            throw new DirectoryNotFoundException($"Source directory does not exist: {sourceDir}");
        }

        // Exclude common build / tooling folders to avoid duplicate generated files (obj, bin, .vs, .git)
        var excludedDirs = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            "bin",
            "obj",
            ".vs",
            ".git",
            ".vscode"
        };

        Directory.CreateDirectory(destinationDir);

        foreach (var file in dir.GetFiles())
        {
            // Skip files that are generated or not needed for build copy (e.g., *.user)
            if (file.Extension.Equals(".user", StringComparison.OrdinalIgnoreCase)
                || file.Extension.Equals(".suo", StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            var targetFilePath = Path.Combine(destinationDir, file.Name);
            file.CopyTo(targetFilePath, true);
        }

        foreach (var subDir in dir.GetDirectories())
        {
            if (excludedDirs.Contains(subDir.Name))
            {
                continue;
            }

            var newDestination = Path.Combine(destinationDir, subDir.Name);
            CopyDirectory(subDir.FullName, newDestination);
        }
    }

    public async Task<AgentLoopResult> ExecuteAsync(GenerateRequest request)
    {
        var result = new AgentLoopResult
        {
            Success = false,
            Attempts = 0,
            FinalCoverage = 0
        };

        try
        {
            Console.WriteLine("🚀 Starting agent orchestration loop...");
            Console.WriteLine($"Target Coverage: {request.TargetCoverage}%");
            Console.WriteLine($"Max Attempts: {_config.Coverage.MaxAttempts}");
            Console.WriteLine();

            string currentTestCode = string.Empty;

            for (int attempt = 1; attempt <= _config.Coverage.MaxAttempts; attempt++)
            {
                result.Attempts = attempt;
                Console.WriteLine($"📍 Attempt {attempt}/{_config.Coverage.MaxAttempts}");
                result.IterationLog.Add($"======================= Attempt {attempt} =========================");

                // Step 1: Generate or regenerate tests
                Console.WriteLine("  🤖 Generator Agent: Generating tests...");
                if (attempt == 1)
                {
                    // Pass source file path so the generator can include manifest/context information
                    currentTestCode = await _generatorAgent.ExecuteAsync(request.SourceCode!, request.SourceFilePath);
                }
                else
                {
                    // For subsequent attempts, try to improve coverage
                    currentTestCode = await _generatorAgent.GenerateAdditionalTestsAsync(
                        request.SourceCode!,
                        request.SourceFilePath,
                        currentTestCode,
                        result.FinalCoverage,
                        new List<string> { "Increase test coverage to meet target" }
                    );
                }
                result.IterationLog.Add("Generator: Tests generated");

                // Step 2: Review tests
                Console.WriteLine("  👀 Reviewer Agent: Reviewing tests...");
                var reviewResult = await _reviewerAgent.ReviewTestsAsync(request.SourceCode!, currentTestCode);
                result.IterationLog.Add($"Reviewer: Approved={reviewResult.IsApproved}, Issues={reviewResult.Issues.Count}");

                if (!reviewResult.IsApproved && _config.Reviewer.StrictMode)
                {
                    Console.WriteLine($"  ⚠️  Review failed with {reviewResult.Issues.Count} issues");
                    foreach (var issue in reviewResult.Issues.Take(3))
                    {
                        Console.WriteLine($"     - {issue}");
                    }

                    // Step 3: Fix issues
                    Console.WriteLine("  🔧 Fix Agent: Fixing issues...");
                    currentTestCode = await _fixAgent.FixTestsAsync(
                        request.SourceCode!,
                        currentTestCode,
                        reviewResult.Issues,
                        reviewResult.FixInstructions
                    );
                    result.IterationLog.Add("Fix Agent: Issues fixed");
                }

                // Step 4: Save generated tests with backup and build; if build fails revert
                Console.WriteLine("  💾 Saving test file (backup will be kept)...");
                var projectPath = GetTestProjectPath(request.TestFilePath);
                string? originalContent = null;
                try
                {
                    if (File.Exists(request.TestFilePath))
                    {
                        originalContent = await File.ReadAllTextAsync(request.TestFilePath);
                    }
                }
                catch
                {
                    originalContent = null;
                }

                // Write generated tests to real path
                await _fileWriter.WriteFileAsync(request.TestFilePath, currentTestCode);
                result.IterationLog.Add("File saved");

                Console.WriteLine("  🔨 Building project...");
                var buildResult = await _buildValidator.BuildProjectAsync(projectPath);
                result.IterationLog.Add($"Build: Success={buildResult.Success}");

                if (!buildResult.Success)
                {
                    Console.WriteLine($"  ❌ Build failed with {buildResult.Errors.Count} errors");
                    foreach (var error in buildResult.Errors.Take(3))
                    {
                        Console.WriteLine($"     - {error}");
                    }

                    // Fix build errors
                    Console.WriteLine("  🔧 Fix Agent: Fixing build errors...");
                    currentTestCode = await _fixAgent.FixTestsAsync(
                        request.SourceCode!,
                        currentTestCode,
                        buildResult.Errors,
                        new List<string> { "Fix compilation errors" },
                        string.Join("\n", buildResult.Errors)
                    );
                    result.IterationLog.Add("Fix Agent: Build errors are trying to fix");

                    // Save fixes and retry build
                    await _fileWriter.WriteFileAsync(request.TestFilePath, currentTestCode);
                    buildResult = await _buildValidator.BuildProjectAsync(projectPath);
                    result.IterationLog.Add($"Build: Success={buildResult.Success}");
                    if (!buildResult.Success)
                    {
                        Console.WriteLine("  ❌ Build still failing after fix attempt. Reverting changes...");
                        // Restore original content if available, otherwise delete the file

                        try
                        {
                            if (originalContent != null)
                            {
                                await _fileWriter.WriteFileAsync(request.TestFilePath, originalContent);
                                result.IterationLog.Add($"The backup Unitetst file code reverted back");
                            }
                            else if (File.Exists(request.TestFilePath))
                            {
                                File.Delete(request.TestFilePath);
                            }
                        }
                        catch
                        {
                            // ignore restore errors
                        }

                        var test_Result = await _testRunner.RunTestsAsync(projectPath);
                        result.IterationLog.Add($"Tests: Passed={test_Result.PassedTests}, Failed={test_Result.FailedTests}");

                        // proceed to next attempt without overwriting the working tests
                        continue;
                    }

                }
                else
                {
                    Console.WriteLine("  ✅ Build successful");
                }

                // Step 5: Run tests
                Console.WriteLine("  🧪 Running tests...");
                var testResult = await _testRunner.RunTestsAsync(projectPath);
                result.IterationLog.Add($"Tests: Passed={testResult.PassedTests}, Failed={testResult.FailedTests}");

                if (!testResult.Success)
                {
                    Console.WriteLine($"  ⚠️  {testResult.FailedTests} test(s) failed");

                    // Try to fix test failures first
                    Console.WriteLine("  🔧 Fix Agent: Attempting to fix test failures...");
                    var fixedTestCode = await _fixAgent.FixTestsAsync(
                        request.SourceCode!,
                        currentTestCode,
                        new List<string> { "Test failures detected" },
                        new List<string> { "Fix failing tests" },
                        null,
                        string.Join("\n", testResult.FailureMessages.Take(5))
                    );
                    result.IterationLog.Add("Fix Agent: Test failures fix attempted");

                    // Save and rerun
                    await _fileWriter.WriteFileAsync(request.TestFilePath, fixedTestCode);
                    await _buildValidator.BuildProjectAsync(projectPath);
                    var retryTestResult = await _testRunner.RunTestsAsync(projectPath);

                    if (retryTestResult.Success)
                    {
                        // Fix worked, use the fixed code
                        currentTestCode = fixedTestCode;
                        testResult = retryTestResult;
                        Console.WriteLine($"  ✅ All tests passed after fix ({testResult.PassedTests} tests)");
                    }
                    else
                    {
                        Console.WriteLine("  ⚠️  Tests still failing after fix attempt");
                        Console.WriteLine("  🔧 Removing failing tests and keeping only passing ones...");
                        
                        // Extract failing test names
                        var failingTestNames = _testFilterService.ExtractFailingTestNames(retryTestResult);
                        Console.WriteLine($"  📋 Identified {failingTestNames.Count} failing test(s): {string.Join(", ", failingTestNames.Take(3))}");
                        
                        // Remove failing tests from the original code (before fix attempt)
                        var filteredTestCode = _testFilterService.RemoveFailingTests(currentTestCode, failingTestNames);
                        
                        var originalTestCount = _testFilterService.CountTestMethods(currentTestCode);
                        var filteredTestCount = _testFilterService.CountTestMethods(filteredTestCode);
                        
                        Console.WriteLine($"  📊 Test count: {originalTestCount} → {filteredTestCount} (removed {originalTestCount - filteredTestCount} failing test(s))");
                        
                        // Save filtered code and verify it builds and passes
                        await _fileWriter.WriteFileAsync(request.TestFilePath, filteredTestCode);
                        var filterBuildResult = await _buildValidator.BuildProjectAsync(projectPath);
                        
                        if (filterBuildResult.Success)
                        {
                            var filterTestResult = await _testRunner.RunTestsAsync(projectPath);
                            
                            if (filterTestResult.Success || filterTestResult.PassedTests > 0)
                            {
                                currentTestCode = filteredTestCode;
                                testResult = filterTestResult;
                                result.IterationLog.Add($"Filtered: Kept {filteredTestCount} passing tests, removed {originalTestCount - filteredTestCount} failing tests");
                                Console.WriteLine($"  ✅ Kept {testResult.PassedTests} passing test(s)");
                            }
                            else
                            {
                                Console.WriteLine("  ⚠️  Filtered tests still have issues, reverting to previous version");
                                result.IterationLog.Add("Filter failed: reverting");
                                continue;
                            }
                        }
                        else
                        {
                            Console.WriteLine("  ⚠️  Filtered code doesn't build, reverting to previous version");
                            result.IterationLog.Add("Filter build failed: reverting");
                            continue;
                        }
                    }
                }
                else
                {
                    Console.WriteLine($"  ✅ All tests passed ({testResult.PassedTests} tests)");
                }

                // Step 6: Check coverage
                Console.WriteLine("  📊 Checking coverage...");
                var coverageResult = await _coverageService.RunCoverageAsync(
                    projectPath,
                    request.TargetCoverage,
                    request.SourceFilePath
                );
                result.FinalCoverage = coverageResult.LineCoverage;
                result.IterationLog.Add($"Coverage: {coverageResult.LineCoverage:F1}%");

                Console.WriteLine($"  📈 Coverage: {coverageResult.LineCoverage:F1}% (Target: {request.TargetCoverage}%)");

                if (coverageResult.MeetsTarget)
                {
                    Console.WriteLine("  🎉 Target coverage achieved!");
                    result.Success = true;
                    result.GeneratedTestCode = currentTestCode;
                    break;
                }
                else
                {
                    Console.WriteLine($"  📉 Coverage below target. Need {request.TargetCoverage - coverageResult.LineCoverage:F1}% more.");
                }

                Console.WriteLine();
            }

            if (!result.Success)
            {
                Console.WriteLine($"⚠️  Max attempts reached. Final coverage: {result.FinalCoverage:F1}%");
                result.GeneratedTestCode = currentTestCode;
                result.ErrorMessage = $"Failed to reach target coverage of {request.TargetCoverage}% after {_config.Coverage.MaxAttempts} attempts.";
            }
            else
            {
                Console.WriteLine($"✅ Success! Generated tests with {result.FinalCoverage:F1}% coverage in {result.Attempts} attempt(s).");
            }

            // Always do a final coverage measurement on the test file currently on disk so we log the ultimate line coverage
            try
            {
                var finalProjectPath = GetTestProjectPath(request.TestFilePath);
                Console.WriteLine("🔁 Final coverage probe: building and measuring coverage on current test file...");

                var finalBuild = await _buildValidator.BuildProjectAsync(finalProjectPath);
                if (finalBuild.Success)
                {
                    var finalTestResult = await _testRunner.RunTestsAsync(finalProjectPath);
                    var finalCoverage = await _coverageService.RunCoverageAsync(finalProjectPath, request.TargetCoverage, request.SourceFilePath);
                    result.FinalCoverage = finalCoverage.LineCoverage;
                    result.IterationLog.Add($"FinalCoverage: {result.FinalCoverage:F1}%");
                    Console.WriteLine($"  📈 Final Coverage: {result.FinalCoverage:F1}%");
                    
                    // Always analyze code quality to identify potential unreachable code
                    Console.WriteLine("  🔍 Analyzing code quality for potential issues...");
                    var qualityWarnings = _codeQualityAnalyzer.AnalyzeCodeQuality(
                        request.SourceCode!,
                        null,
                        result.FinalCoverage,
                        100 // Check against 100% to find any unreachable code
                    );
                    
                    if (qualityWarnings.Count > 0)
                    {
                        result.CodeQualityWarnings.AddRange(qualityWarnings);
                        result.IterationLog.Add($"CodeQualityWarnings: {qualityWarnings.Count} issue(s) detected");
                    }
                }
                else
                {
                    result.IterationLog.Add("Final build failed; cannot measure coverage.");
                    Console.WriteLine("  ⚠️ Final build failed; coverage not available.");
                }
            }
            catch
            {
                // ignore final probe errors
            }
        }
        catch (Exception ex)
        {
            result.Success = false;
            result.ErrorMessage = $"Orchestration failed: {ex.Message}";
            result.IterationLog.Add($"ERROR: {ex.Message}");
            Console.WriteLine($"❌ Error: {ex.Message}");
        }

        return result;
    }

    private string GetTestProjectPath(string testFilePath)
    {
        // Find the .csproj file in the test file's directory or parent directories
        var fullTestFilePath = Path.GetFullPath(testFilePath);

        var directory = Path.GetDirectoryName(fullTestFilePath);
        while (!string.IsNullOrEmpty(directory))
        {
            var csprojFiles = Directory.GetFiles(directory, "*.csproj");
            if (csprojFiles.Length > 0)
            {
                return Path.GetFullPath(csprojFiles[0]);
            }
            directory = Path.GetDirectoryName(directory);
        }

        throw new FileNotFoundException("Could not find test project file (.csproj)");
    }
}


