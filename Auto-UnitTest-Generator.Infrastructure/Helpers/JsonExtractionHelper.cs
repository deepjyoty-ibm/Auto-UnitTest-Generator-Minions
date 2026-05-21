using System.Text.Json;
using System.Text.RegularExpressions;

namespace Auto_UnitTest_Generator.Infrastructure.Helpers;

public static class JsonExtractionHelper
{
    public static T? ExtractJson<T>(string text) where T : class
    {
        if (string.IsNullOrWhiteSpace(text))
        {
            return null;
        }

        // Try to find JSON in markdown code blocks
        var jsonMatch = Regex.Match(text, @"```json\s*([\s\S]*?)\s*```", RegexOptions.IgnoreCase);
        if (jsonMatch.Success)
        {
            text = jsonMatch.Groups[1].Value;
        }
        else
        {
            // Try to find JSON object directly
            var objectMatch = Regex.Match(text, @"\{[\s\S]*\}", RegexOptions.Multiline);
            if (objectMatch.Success)
            {
                text = objectMatch.Value;
            }
        }

        try
        {
            return JsonSerializer.Deserialize<T>(text, new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            });
        }
        catch
        {
            return null;
        }
    }

    public static string ExtractCodeBlock(string text, string language = "csharp")
    {
        if (string.IsNullOrWhiteSpace(text))
        {
            return string.Empty;
        }

        // Try to find code in markdown code blocks
        var codeMatch = Regex.Match(text, $@"```{language}\s*([\s\S]*?)\s*```", RegexOptions.IgnoreCase);
        if (codeMatch.Success)
        {
            return codeMatch.Groups[1].Value.Trim();
        }

        // Try generic code block
        codeMatch = Regex.Match(text, @"```\s*([\s\S]*?)\s*```", RegexOptions.IgnoreCase);
        if (codeMatch.Success)
        {
            return codeMatch.Groups[1].Value.Trim();
        }

        // Return as-is if no code block found
        return text.Trim();
    }
}


