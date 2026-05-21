using System.Text.RegularExpressions;
namespace Auto_UnitTest_Generator.Infrastructure.Services;

/// <summary>
/// Ensures generated test code has all required using statements.
/// Prevents build failures from missing usings like Moq, FluentAssertions, System.Reflection, System.Collections.Generic, etc.
/// </summary>
public class CodeSanitizerService
{
    private static readonly string[] RequiredUsings = new[]
    {
        "using System;",
        "using System.Collections.Generic;",
        "using System.Reflection;",
        "using Xunit;",
        "using Moq;",
        "using FluentAssertions;",
    };

    /// <summary>
    /// Ensures all critical using statements are present in the generated code.
    /// If code is missing usings, they are injected before the namespace declaration.
    /// </summary>
    public static string EnsureRequiredUsings(string code)
    {
        if (string.IsNullOrWhiteSpace(code))
            return code;

        var codeLines = code.Split(new[] { "\r\n", "\r", "\n" }, StringSplitOptions.None);
        var existingUsings = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        // Collect existing usings (case-insensitive to avoid duplicates)
        foreach (var line in codeLines)
        {
            var trimmed = line.Trim();
            if (trimmed.StartsWith("using", StringComparison.OrdinalIgnoreCase) && trimmed.EndsWith(";"))
            {
                existingUsings.Add(trimmed.ToLowerInvariant());
            }
        }

        // Find which required usings are missing
        var missingUsings = new List<string>();
        foreach (var requiredUsing in RequiredUsings)
        {
            var normalizedRequired = requiredUsing.ToLowerInvariant();
            if (!existingUsings.Contains(normalizedRequired))
            {
                missingUsings.Add(requiredUsing);
            }
        }

        // If all usings are present, return code as-is
        if (missingUsings.Count == 0)
            return code;

        // Find the position to insert usings (before namespace or class declaration if no namespace)
        var namespaceMatch = Regex.Match(code, @"\s*namespace\s+", RegexOptions.Multiline);
        var classMatch = Regex.Match(code, @"\s*public\s+class\s+", RegexOptions.Multiline);

        int insertPosition = 0;
        if (namespaceMatch.Success)
        {
            insertPosition = code.LastIndexOf('\n', namespaceMatch.Index) + 1;
        }
        else if (classMatch.Success)
        {
            insertPosition = code.LastIndexOf('\n', classMatch.Index) + 1;
        }

        // Build the new code with missing usings injected
        var usingLines = string.Join("\r\n", missingUsings);
        var resultCode = code.Insert(insertPosition, usingLines + "\r\n");

        return resultCode;
    }

    /// <summary>
    /// Removes duplicate using statements (case-insensitive).
    /// </summary>
    public static string RemoveDuplicateUsings(string code)
    {
        if (string.IsNullOrWhiteSpace(code))
            return code;

        var lines = code.Split(new[] { "\r\n", "\r", "\n" }, StringSplitOptions.None);
        var seenUsings = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var resultLines = new List<string>();
        var usingsSectionEnded = false;

        foreach (var line in lines)
        {
            var trimmed = line.Trim();

            // Track when we've exited the usings section (first non-empty, non-using line)
            if (!string.IsNullOrWhiteSpace(trimmed) && !trimmed.StartsWith("using"))
            {
                usingsSectionEnded = true;
            }

            // If in usings section and this is a using statement
            if (!usingsSectionEnded && trimmed.StartsWith("using") && trimmed.EndsWith(";"))
            {
                var normalizedUsing = trimmed.ToLowerInvariant();
                if (seenUsings.Add(normalizedUsing))
                {
                    resultLines.Add(line); // Add if not seen before
                }
                // Skip duplicate usings
            }
            else
            {
                resultLines.Add(line); // Add non-using lines
            }
        }

        return string.Join("\r\n", resultLines);
    }

    /// <summary>
    /// Finds the position to insert missing using statements.
    /// </summary>
    private static int FindInsertPosition(string code)
    {
        var namespaceMatch = Regex.Match(code, @"\s*namespace\s+", RegexOptions.Multiline);
        var classMatch = Regex.Match(code, @"\s*public\s+class\s+", RegexOptions.Multiline);

        if (namespaceMatch.Success)
        {
            return code.LastIndexOf('\n', namespaceMatch.Index) + 1;
        }
        else if (classMatch.Success)
        {
            return code.LastIndexOf('\n', classMatch.Index) + 1;
        }

        return 0; // Default to the start of the code
    }
}
