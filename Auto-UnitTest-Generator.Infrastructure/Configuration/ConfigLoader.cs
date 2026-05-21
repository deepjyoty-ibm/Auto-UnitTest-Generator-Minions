using System.Text.Json;

namespace Auto_UnitTest_Generator.Infrastructure.Configuration;

public class ConfigLoader
{
    private const string ConfigFileName = "autounittest.config.json";
    public static string[] ValidProviders = new[] { "openai", "groq" };
    public static ToolConfiguration LoadConfiguration(string? startDirectory = null)
    {
        var configPath = FindConfigFile(startDirectory ?? Directory.GetCurrentDirectory());
        
        if (configPath == null)
        {
            Console.WriteLine("No configuration file found. Using default settings.");
            return new ToolConfiguration();
        }

        try
        {
            var json = File.ReadAllText(configPath);
            var config = JsonSerializer.Deserialize<ToolConfiguration>(json, new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            }) ?? new ToolConfiguration();

            // Resolve API key if it's an environment variable reference
            if (config.Ai.ApiKey.StartsWith("env:", StringComparison.OrdinalIgnoreCase))
            {
                var envVarName = config.Ai.ApiKey[4..];
                var envValue = Environment.GetEnvironmentVariable(envVarName);
                if (string.IsNullOrEmpty(envValue))
                {
                    throw new InvalidOperationException($"Environment variable '{envVarName}' is not set.");
                }
                config.Ai.ApiKey = envValue;
            }

            Console.WriteLine($"Configuration loaded from: {configPath}");
            return config;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error loading configuration: {ex.Message}");
            Console.WriteLine("Using default settings.");
            return new ToolConfiguration();
        }
    }

    private static string? FindConfigFile(string startDirectory)
    {
        // Search order:
        // 1. Current directory and parent directories (up to solution root)
        // 2. User profile directory (~/.autounittest/)
        // 3. Tool installation directory
        
        // First, search from current directory upwards
        var currentDir = new DirectoryInfo(startDirectory);
        while (currentDir != null)
        {
            var configPath = Path.Combine(currentDir.FullName, ConfigFileName);
            if (File.Exists(configPath))
            {
                return configPath;
            }

            // Check if we've reached a solution root
            if (Directory.GetFiles(currentDir.FullName, "*.sln*").Length > 0)
            {
                // We're at solution root, stop searching upwards
                break;
            }

            currentDir = currentDir.Parent;
        }

        // Second, check user profile directory
        var userProfilePath = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
        var userConfigDir = Path.Combine(userProfilePath, ".autounittest");
        var userConfigPath = Path.Combine(userConfigDir, ConfigFileName);
        if (File.Exists(userConfigPath))
        {
            return userConfigPath;
        }

        // Third, check tool installation directory (where the DLL is located)
        var assemblyLocation = System.Reflection.Assembly.GetExecutingAssembly().Location;
        if (!string.IsNullOrEmpty(assemblyLocation))
        {
            var assemblyDir = Path.GetDirectoryName(assemblyLocation);
            if (!string.IsNullOrEmpty(assemblyDir))
            {
                var toolConfigPath = Path.Combine(assemblyDir, ConfigFileName);
                if (File.Exists(toolConfigPath))
                {
                    return toolConfigPath;
                }
            }
        }

        return null;
    }

    public static void ValidateConfiguration(ToolConfiguration config)
    {
        var errors = new List<string>();

        // Validate AI configuration
        if (string.IsNullOrWhiteSpace(config.Ai.ApiKey))
        {
            errors.Add("AI API key is required.");
        }

        if (string.IsNullOrWhiteSpace(config.Ai.Provider))
        {
            errors.Add("AI provider is required.");
        }

         
        if (!ValidProviders.Contains(config.Ai.Provider.ToLowerInvariant()))
        {
            errors.Add($"Invalid AI provider: {config.Ai.Provider}. Valid options: {string.Join(", ", ValidProviders)}");
        }

        if (string.IsNullOrWhiteSpace(config.Ai.Model))
        {
            errors.Add("AI model is required.");
        }

        // Validate test framework
        var validFrameworks = new[] { "xunit", "nunit", "mstest" };
        if (!validFrameworks.Contains(config.TestFramework.Type.ToLowerInvariant()))
        {
            errors.Add($"Invalid test framework: {config.TestFramework.Type}. Valid options: {string.Join(", ", validFrameworks)}");
        }

        var validMockLibraries = new[] { "moq", "nsubstitute" };
        if (!validMockLibraries.Contains(config.TestFramework.MockingLibrary.ToLowerInvariant()))
        {
            errors.Add($"Invalid mocking library: {config.TestFramework.MockingLibrary}. Valid options: {string.Join(", ", validMockLibraries)}");
        }

        var validAssertionLibraries = new[] { "fluentassertions", "builtin" };
        if (!validAssertionLibraries.Contains(config.TestFramework.AssertionLibrary.ToLowerInvariant()))
        {
            errors.Add($"Invalid assertion library: {config.TestFramework.AssertionLibrary}. Valid options: {string.Join(", ", validAssertionLibraries)}");
        }

        // Validate coverage
        if (config.Coverage.Target < 0 || config.Coverage.Target > 100)
        {
            errors.Add("Coverage target must be between 0 and 100.");
        }

        if (config.Coverage.MaxAttempts < 1)
        {
            errors.Add("Max attempts must be at least 1.");
        }

        if (errors.Any())
        {
            throw new InvalidOperationException($"Configuration validation failed:\n{string.Join("\n", errors)}");
        }
    }
}


