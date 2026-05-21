using System.Text.RegularExpressions;

namespace Auto_UnitTest_Generator.Infrastructure.Helpers;

public static class CodeContextExtractor
{
    public static string ExtractManifest(string sourceCode)
    {
        if (string.IsNullOrWhiteSpace(sourceCode))
            return string.Empty;

        var sb = new System.Text.StringBuilder();

        // Try to find class signature
        var classMatch = Regex.Match(sourceCode, @"class\s+(\w+)");
        if (classMatch.Success)
        {
            sb.AppendLine($"Class: {classMatch.Groups[1].Value}");
        }

        // Constructor params
        var ctorMatch = Regex.Match(sourceCode, @"public\s+\w+\s*\(([^)]*)\)");
        if (ctorMatch.Success)
        {
            sb.AppendLine("ConstructorParameters:");
            var paramsText = ctorMatch.Groups[1].Value.Trim();
            if (!string.IsNullOrEmpty(paramsText))
            {
                var parts = paramsText.Split(',');
                foreach (var p in parts)
                {
                    sb.AppendLine($"- {p.Trim()}");
                }
            }
            else
            {
                sb.AppendLine("- (none)");
            }
        }

        // Public methods
        sb.AppendLine("PublicMethods:");
        var methodMatches = Regex.Matches(sourceCode, @"public\s+(?:async\s+)?[\w<>\[\]]+\s+(\w+)\s*\(([^)]*)\)");
        foreach (Match m in methodMatches)
        {
            sb.AppendLine($"- {m.Groups[1].Value}({m.Groups[2].Value.Trim()})");
        }

        return sb.ToString();
    }


    public static string ExtractDetailedManifest(string sourceCode, string sourceFilePath)
    {
        var sb = new System.Text.StringBuilder();

        if (string.IsNullOrWhiteSpace(sourceCode))
            return string.Empty;

        // Namespace
        var nsMatch = System.Text.RegularExpressions.Regex.Match(sourceCode, @"namespace\s+([\w\.]+)");
        if (nsMatch.Success)
        {
            sb.AppendLine($"Namespace: {nsMatch.Groups[1].Value}");
        }

        // Class and constructor info
        var classMatch = System.Text.RegularExpressions.Regex.Match(sourceCode, @"class\s+(\w+)");
        if (classMatch.Success)
        {
            sb.AppendLine($"Class: {classMatch.Groups[1].Value}");
        }

        var ctorMatch = System.Text.RegularExpressions.Regex.Match(sourceCode, @"public\s+\w+\s*\(([^)]*)\)");
        sb.AppendLine("ConstructorParameters:");
        if (ctorMatch.Success)
        {
            var paramsText = ctorMatch.Groups[1].Value.Trim();
            if (!string.IsNullOrEmpty(paramsText))
            {
                var parts = paramsText.Split(',');
                foreach (var p in parts)
                {
                    var param = p.Trim();
                    sb.AppendLine($"- {param}");
                    
                    // Analyze parameter type
                    var paramParts = param.Split(new[] { ' ', '=' }, StringSplitOptions.RemoveEmptyEntries);
                    if (paramParts.Length >= 2)
                    {
                        var fullTypeName = paramParts[0];  // e.g., "OrderService?" or "IUserRepository"
                        var paramName = paramParts[1];
                        var typeName = fullTypeName.Replace("?", "");
                        var isNullable = fullTypeName.Contains("?");
                        var isInterface = typeName.StartsWith("I") && typeName.Length > 1 && char.IsUpper(typeName[1]);
                        
                        // Determine if it's a primitive type
                        var isPrimitive = typeName.Contains("string") || typeName.Contains("int") ||
                            typeName.Contains("bool") || typeName.Contains("decimal") ||
                            typeName.Contains("double") || typeName.Contains("float") ||
                            typeName.Contains("DateTime") || typeName.Contains("Guid");
                        
                        if (isInterface)
                        {
                            sb.AppendLine($"  ✅ TYPE: INTERFACE");
                            sb.AppendLine($"  ✅ IN TEST: Mock<{typeName}>().Object");
                            sb.AppendLine($"  ❌ DO NOT USE: new {typeName}() or Mock<ConcreteClass>");
                        }
                        else if (!isPrimitive)
                        {
                            sb.AppendLine($"  ✅ TYPE: CONCRETE CLASS");
                            if (isNullable)
                            {
                                sb.AppendLine($"  ✅ IN TEST: null  (or new {typeName}() if test needs it)");
                            }
                            else
                            {
                                sb.AppendLine($"  ✅ IN TEST: new {typeName}()");
                            }
                            sb.AppendLine($"  ❌ DO NOT USE: Mock<{typeName}>() or Mock<I{typeName}>().Object");
                            sb.AppendLine($"  ⚠️  CRITICAL: Constructor expects {fullTypeName}, NOT I{typeName}");
                        }
                    }
                }
            }
            else
            {
                sb.AppendLine("- (none)");
            }
        }

        // Public methods
        sb.AppendLine("PublicMethods:");
        var methodMatches = System.Text.RegularExpressions.Regex.Matches(sourceCode, @"public\s+(?:async\s+)?[\w<>'\[\]]+\s+(\w+)\s*\(([^)]*)\)");
        foreach (System.Text.RegularExpressions.Match m in methodMatches)
        {
            sb.AppendLine($"- {m.Groups[1].Value}({m.Groups[2].Value.Trim()})");
        }

        // Try to find nearest project file and include minimal csproj contents (PackageReference & ProjectReference)
        try
        {
            var dir = Path.GetDirectoryName(Path.GetFullPath(sourceFilePath)) ?? string.Empty;
            while (!string.IsNullOrEmpty(dir))
            {
                var csproj = Directory.GetFiles(dir, "*.csproj").FirstOrDefault();
                if (csproj != null)
                {
                    sb.AppendLine("ProjectFilePath: " + csproj);
                    var csprojText = File.ReadAllText(csproj);
                    // Extract PackageReference and ProjectReference lines
                    var pkgMatches = System.Text.RegularExpressions.Regex.Matches(csprojText, "<PackageReference[^>]*/>", System.Text.RegularExpressions.RegexOptions.IgnoreCase);
                    var projMatches = System.Text.RegularExpressions.Regex.Matches(csprojText, "<ProjectReference[^>]*/>", System.Text.RegularExpressions.RegexOptions.IgnoreCase);
                    sb.AppendLine("PackageReferences:");
                    foreach (System.Text.RegularExpressions.Match m in pkgMatches)
                    {
                        sb.AppendLine(m.Value.Trim());
                    }
                    sb.AppendLine("ProjectReferences:");
                    foreach (System.Text.RegularExpressions.Match m in projMatches)
                    {
                        sb.AppendLine(m.Value.Trim());
                    }
                    break;
                }
                dir = Path.GetDirectoryName(dir);
            }
        }
        catch
        {
            // ignore errors
        }

        return sb.ToString();
    }
}
