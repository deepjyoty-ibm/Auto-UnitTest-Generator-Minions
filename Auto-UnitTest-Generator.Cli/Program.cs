using System.CommandLine;
using System.Text;
using Auto_UnitTest_Generator.Infrastructure.Agents;
using Auto_UnitTest_Generator.Infrastructure.AI;
using Auto_UnitTest_Generator.Infrastructure.Configuration;
using Auto_UnitTest_Generator.Infrastructure.Models;
using Auto_UnitTest_Generator.Infrastructure.Services;

namespace Auto_UnitTest_Generator.Cli;

class Program
{
    static async Task<int> Main(string[] args)
    {
        // Enable UTF-8 encoding for emoji support
        Console.OutputEncoding = Encoding.UTF8;
        Console.InputEncoding = Encoding.UTF8;

        var rootCommand = new RootCommand("Auto-UnitTest-Generator - AI-powered unit test generation with multi-agent orchestration");

        // Define options
        var filePathOption = new Option<string>(
            name: "--filepath",
            description: "Path to the source C# file to generate tests for")
        {
            IsRequired = true
        };
        filePathOption.AddAlias("-f");

        var generateFilePathOption = new Option<string>(
            name: "--generatefilepath",
            description: "Path where the generated test file should be saved")
        {
            IsRequired = true
        };
        generateFilePathOption.AddAlias("-g");

        var coverageOption = new Option<int?>(
            name: "--coverage",
            description: "Target code coverage percentage (default: 90)")
        {
            IsRequired = false
        };
        coverageOption.AddAlias("-c");

        var maxAttemptsOption = new Option<int?>(
            name: "--maxattempts",
            description: "Maximum refinement iterations (default: 3)")
        {
            IsRequired = false
        };
        maxAttemptsOption.AddAlias("-m");

        rootCommand.AddOption(filePathOption);
        rootCommand.AddOption(generateFilePathOption);
        rootCommand.AddOption(coverageOption);
        rootCommand.AddOption(maxAttemptsOption);

        rootCommand.SetHandler(async (string filePath, string generateFilePath, int? coverage, int? maxAttempts) =>
        {
            try
            {
                Console.WriteLine("╔════════════════════════════════════════════════════════════╗");
                Console.WriteLine("║        Auto-UnitTest-Generator - AI Test Generation       ║");
                Console.WriteLine("╚════════════════════════════════════════════════════════════╝");
                Console.WriteLine();

                // Step 1: Load configuration
                Console.WriteLine("📋 Loading configuration...");
                var config = ConfigLoader.LoadConfiguration();
                
                // Override coverage if provided via CLI
                if (coverage.HasValue)
                {
                    config.Coverage.Target = coverage.Value;
                }

                // Override max attempts if provided via CLI
                if (maxAttempts.HasValue)
                {
                    config.Coverage.MaxAttempts = maxAttempts.Value;
                }

                // Validate configuration
                ConfigLoader.ValidateConfiguration(config);
                Console.WriteLine($"✅ Configuration loaded successfully");
                Console.WriteLine($"   Provider: {config.Ai.Provider}");
                Console.WriteLine($"   Model: {config.Ai.Model}");
                Console.WriteLine($"   Framework: {config.TestFramework.Type}");
                Console.WriteLine($"   Target Coverage: {config.Coverage.Target}%");
                Console.WriteLine($"   Max Attempts: {config.Coverage.MaxAttempts}");
                Console.WriteLine();

                // Step 2: Validate input file
                Console.WriteLine("📂 Validating input file...");
                var fileReader = new FileReaderService();
                if (!fileReader.FileExists(filePath))
                {
                    Console.WriteLine($"❌ ERROR: Source file not found: {filePath}");
                    Environment.ExitCode = 1;
                    return;
                }

                var sourceCode = await fileReader.ReadFileAsync(filePath);
                Console.WriteLine($"✅ Source file loaded: {filePath}");
                Console.WriteLine($"   Lines: {sourceCode.Split('\n').Length}");
                Console.WriteLine();

                // Step 3: Detect solution context
                Console.WriteLine("🔍 Detecting solution context...");
                var solutionService = new SolutionContextService();
                var solutionPath = solutionService.FindSolutionFile(Directory.GetCurrentDirectory());
                if (solutionPath != null)
                {
                    Console.WriteLine($"✅ Solution found: {Path.GetFileName(solutionPath)}");
                }
                else
                {
                    Console.WriteLine("⚠️  No solution file found in parent directories");
                }
                Console.WriteLine();

                // Step 4: Initialize AI client and agents
                Console.WriteLine("🤖 Initializing AI agents...");
                var aiClient = AiClientFactory.Create(config.Ai);
                var generatorAgent = new GeneratorAgent(aiClient, config);
                var reviewerAgent = new ReviewerAgent(aiClient, config);
                var fixAgent = new FixAgent(aiClient, config);
                Console.WriteLine("✅ Agents initialized");
                Console.WriteLine();

                // Step 5: Initialize services
                var fileWriter = new FileWriterService();
                var buildValidator = new BuildValidator();
                var testRunner = new TestRunnerService();
                var coverageService = new CoverageService();
                var testFilterService = new TestFilterService();
                var codeQualityAnalyzer = new CodeQualityAnalyzer();

                // Step 6: Create orchestrator
                var orchestrator = new AgentOrchestrator(
                    generatorAgent,
                    reviewerAgent,
                    fixAgent,
                    fileWriter,
                    buildValidator,
                    testRunner,
                    coverageService,
                    testFilterService,
                    codeQualityAnalyzer,
                    config
                );

                // Step 7: Build request
                var request = new GenerateRequest
                {
                    SourceFilePath = filePath,
                    TestFilePath = generateFilePath,
                    TargetCoverage = config.Coverage.Target,
                    SolutionPath = solutionPath,
                    SourceCode = sourceCode
                };

                // Ensure the directory for the generated test file exists. Create it if missing.
                var generateDir = Path.GetDirectoryName(generateFilePath);
                if (!string.IsNullOrEmpty(generateDir) && !Directory.Exists(generateDir))
                {
                    Console.WriteLine($"📁 Creating directory for generated file: {generateDir}");
                    Directory.CreateDirectory(generateDir);
                }

                // Step 8: Execute orchestration
                Console.WriteLine("════════════════════════════════════════════════════════════");
                Console.WriteLine();
                var result = await orchestrator.ExecuteAsync(request);
                Console.WriteLine();
                Console.WriteLine("════════════════════════════════════════════════════════════");
                Console.WriteLine();

                // Step 9: Display results
                if (result.Success)
                {
                    Console.WriteLine("✅ SUCCESS!");
                    Console.WriteLine($"   Test file: {generateFilePath}");
                    Console.WriteLine($"   Coverage: {result.FinalCoverage:F1}%");
                    Console.WriteLine($"   Attempts: {result.Attempts}");
                    Environment.ExitCode = 0;
                }
                else
                {
                    Console.WriteLine("⚠️  PARTIAL SUCCESS");
                    Console.WriteLine($"   Test file: {generateFilePath}");
                    Console.WriteLine($"   Coverage: {result.FinalCoverage:F1}% (Target: {config.Coverage.Target}%)");
                    Console.WriteLine($"   Attempts: {result.Attempts}");
                    if (!string.IsNullOrEmpty(result.ErrorMessage))
                    {
                        Console.WriteLine($"   Message: {result.ErrorMessage}");
                    }
                    Environment.ExitCode = 1;
                }

                Console.WriteLine();
                Console.WriteLine("📊 Iteration Log:");
                foreach (var log in result.IterationLog)
                {
                    Console.WriteLine($"   {log}");
                }

                // Display code quality warnings if any (show even on success)
                if (result.CodeQualityWarnings.Count > 0)
                {
                    Console.WriteLine();
                    Console.WriteLine("════════════════════════════════════════════════════════════");
                    Console.WriteLine();
                    Console.WriteLine("⚠️  CODE QUALITY ANALYSIS");
                    Console.WriteLine($"   Source file: {filePath}");
                    Console.WriteLine();
                    Console.WriteLine("   The following code patterns were detected that may indicate unreachable code:");
                    Console.WriteLine();
                    foreach (var warning in result.CodeQualityWarnings)
                    {
                        Console.WriteLine($"   • {warning}");
                    }
                    Console.WriteLine();
                    Console.WriteLine("   💡 Recommendation: Review these sections in your source code.");
                    Console.WriteLine("      If confirmed unreachable, consider refactoring to improve code quality.");
                    Console.WriteLine("      Unreachable code cannot be tested and may indicate logic issues.");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine();
                Console.WriteLine("❌ FATAL ERROR");
                Console.WriteLine($"   {ex.Message}");
                Console.WriteLine();
                Console.WriteLine("Stack Trace:");
                Console.WriteLine(ex.StackTrace);
                Environment.ExitCode = 1;
            }
        }, filePathOption, generateFilePathOption, coverageOption, maxAttemptsOption);

        return await rootCommand.InvokeAsync(args);
    }
}

