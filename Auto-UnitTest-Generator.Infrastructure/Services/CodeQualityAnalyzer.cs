using System.Text.RegularExpressions;
using System.Xml.Linq;

namespace Auto_UnitTest_Generator.Infrastructure.Services;

/// <summary>
/// Analyzes source code and coverage data to identify potential code quality issues
/// that prevent achieving target coverage.
/// </summary>
public class CodeQualityAnalyzer
{
    /// <summary>
    /// Analyzes source code and coverage XML to detect unreachable code and other quality issues.
    /// Returns generic warnings that apply to any codebase.
    /// </summary>
    public List<string> AnalyzeCodeQuality(string sourceCode, string? coverageXmlPath, double currentCoverage, int targetCoverage)
    {
        var warnings = new List<string>();

        // Always analyze for unreachable code if coverage is less than 100%
        if (currentCoverage < 100)
        {
            warnings.AddRange(DetectUnreachableCodePatterns(sourceCode));
            
            if (warnings.Count > 0 && currentCoverage < targetCoverage)
            {
                warnings.Insert(0, $"⚠️  Coverage gap: {targetCoverage - currentCoverage:F1}% remaining may be due to unreachable code:");
            }
            else if (warnings.Count > 0)
            {
                warnings.Insert(0, $"⚠️  Potential unreachable code detected (current coverage: {currentCoverage:F1}%):");
            }
        }

        return warnings;
    }

    /// <summary>
    /// Detects common unreachable code patterns in a generic way.
    /// </summary>
    private List<string> DetectUnreachableCodePatterns(string sourceCode)
    {
        var warnings = new List<string>();
        var lines = sourceCode.Split(new[] { "\r\n", "\r", "\n" }, StringSplitOptions.None);

        for (int i = 0; i < lines.Length; i++)
        {
            var line = lines[i];
            var trimmed = line.Trim();

            // Pattern 1: Exception handlers that can never be reached due to protective code
            if (trimmed.StartsWith("catch (") && i > 5)
            {
                var exceptionType = ExtractExceptionType(trimmed);
                if (!string.IsNullOrEmpty(exceptionType))
                {
                    // Look backwards for protective code that prevents this exception
                    var contextBefore = string.Join("\n", lines.Skip(Math.Max(0, i - 10)).Take(10));
                    
                    if (HasProtectiveCode(contextBefore, exceptionType))
                    {
                        warnings.Add($"Line {i + 1}: catch ({exceptionType}) block may be unreachable due to protective code that prevents this exception");
                    }
                }
            }

            // Pattern 2: Redundant validation in private/internal methods
            if (trimmed.Contains("if (") && (trimmed.Contains("null") || trimmed.Contains("IsNullOrWhiteSpace") || trimmed.Contains("IsNullOrEmpty")))
            {
                // Look backwards to find the method declaration
                var methodName = string.Empty;
                var isPrivateMethod = false;
                
                for (int j = Math.Max(0, i - 15); j < i; j++)
                {
                    var prevLine = lines[j].Trim();
                    if ((prevLine.Contains("private ") || prevLine.Contains("internal ")) &&
                        (prevLine.Contains("bool ") || prevLine.Contains("void ") || prevLine.Contains("string ")))
                    {
                        isPrivateMethod = true;
                        // Extract method name
                        var match = Regex.Match(prevLine, @"(?:private|internal)\s+\w+\s+(\w+)\s*\(");
                        if (match.Success)
                        {
                            methodName = match.Groups[1].Value;
                        }
                    }
                }
                
                if (isPrivateMethod && !string.IsNullOrEmpty(methodName))
                {
                    // Check if similar validation exists elsewhere in the code (likely in caller)
                    var fullCode = string.Join("\n", lines);
                    var validationPattern = ExtractValidationPattern(trimmed);
                    
                    if (!string.IsNullOrEmpty(validationPattern))
                    {
                        // Count how many times this validation pattern appears
                        int validationCount = 0;
                        for (int j = 0; j < lines.Length; j++)
                        {
                            if (j != i && lines[j].Contains(validationPattern))
                            {
                                validationCount++;
                            }
                        }
                        
                        // If the same validation appears elsewhere, it's likely redundant in the private method
                        if (validationCount > 0)
                        {
                            warnings.Add($"Line {i + 1}: Validation check may be redundant - caller likely validates before calling this private method");
                        }
                    }
                }
            }

            // Pattern 3: Code after return/throw statements
            if ((trimmed.StartsWith("return ") || trimmed.StartsWith("throw ")) && i + 1 < lines.Length)
            {
                var nextLine = lines[i + 1].Trim();
                if (!string.IsNullOrWhiteSpace(nextLine) && 
                    !nextLine.StartsWith("}") && 
                    !nextLine.StartsWith("//") &&
                    !nextLine.StartsWith("catch") && 
                    !nextLine.StartsWith("finally") &&
                    !nextLine.StartsWith("#"))
                {
                    warnings.Add($"Line {i + 2}: Code after return/throw statement is unreachable");
                }
            }

            // Pattern 4: Always-true or always-false conditions
            if (trimmed.StartsWith("if (") && i > 2)
            {
                var condition = ExtractCondition(trimmed);
                if (!string.IsNullOrEmpty(condition))
                {
                    var previousLines = string.Join("\n", lines.Skip(Math.Max(0, i - 5)).Take(5));
                    if (IsAlwaysTrueOrFalse(condition, previousLines))
                    {
                        warnings.Add($"Line {i + 1}: Condition appears to be always true/false based on preceding code");
                    }
                }
            }
        }

        return warnings;
    }

    private string ExtractExceptionType(string catchLine)
    {
        var match = Regex.Match(catchLine, @"catch\s*\(\s*(\w+)");
        return match.Success ? match.Groups[1].Value : string.Empty;
    }

    private bool HasProtectiveCode(string context, string exceptionType)
    {
        // Generic patterns that prevent common exceptions
        var protectivePatterns = new Dictionary<string, string[]>
        {
            { "DivideByZeroException", new[] { "Math.Max", "Math.Min", "!= 0", "> 0", "< 0" } },
            { "NullReferenceException", new[] { "!= null", "?.", "??", "is not null" } },
            { "ArgumentNullException", new[] { "!= null", "IsNullOrEmpty", "IsNullOrWhiteSpace" } },
            { "IndexOutOfRangeException", new[] { ".Length", ".Count", "< ", "> " } },
            { "InvalidOperationException", new[] { "if (", "?.","??" } }
        };

        if (protectivePatterns.ContainsKey(exceptionType))
        {
            foreach (var pattern in protectivePatterns[exceptionType])
            {
                if (context.Contains(pattern))
                {
                    return true;
                }
            }
        }

        return false;
    }
    private string ExtractValidationPattern(string validationLine)
    {
        // Extract the core validation pattern (e.g., "IsNullOrWhiteSpace", "== null", etc.)
        if (validationLine.Contains("IsNullOrWhiteSpace"))
        {
            return "IsNullOrWhiteSpace";
        }
        else if (validationLine.Contains("IsNullOrEmpty"))
        {
            return "IsNullOrEmpty";
        }
        else if (validationLine.Contains("== null"))
        {
            return "== null";
        }
        else if (validationLine.Contains("!= null"))
        {
            return "!= null";
        }
        
        return string.Empty;
    }


    private bool HasDuplicateValidation(string validationLine, string fullCode, int currentLine)
    {
        // Extract the variable being validated
        var match = Regex.Match(validationLine, @"if\s*\(\s*(?:string\.)?(?:IsNullOrWhiteSpace|IsNullOrEmpty)\s*\(\s*(\w+)");
        if (!match.Success)
        {
            match = Regex.Match(validationLine, @"if\s*\(\s*(\w+)\s*(?:==|!=)\s*null");
        }

        if (match.Success)
        {
            var variable = match.Groups[1].Value;
            var lines = fullCode.Split('\n');
            
            // Look for similar validation in other parts of the code
            int validationCount = 0;
            for (int i = 0; i < lines.Length; i++)
            {
                if (i != currentLine && lines[i].Contains(variable) && 
                    (lines[i].Contains("null") || lines[i].Contains("IsNullOr")))
                {
                    validationCount++;
                }
            }
            
            return validationCount > 0;
        }

        return false;
    }

    private string ExtractCondition(string ifLine)
    {
        var match = Regex.Match(ifLine, @"if\s*\(\s*(.+?)\s*\)");
        return match.Success ? match.Groups[1].Value : string.Empty;
    }

    private bool IsAlwaysTrueOrFalse(string condition, string previousContext)
    {
        // Check for patterns like: var x = value; if (x != null) where value is never null
        var match = Regex.Match(condition, @"(\w+)\s*(?:==|!=)\s*null");
        if (match.Success)
        {
            var variable = match.Groups[1].Value;
            // Check if variable was just assigned a non-null value
            if (previousContext.Contains($"{variable} =") && !previousContext.Contains("= null"))
            {
                return true;
            }
        }

        return false;
    }
}

