namespace Auto_UnitTest_Generator.Infrastructure.Services;

public class SolutionContextService
{
    public string? FindSolutionFile(string startDirectory)
    {
        var currentDir = new DirectoryInfo(startDirectory);

        while (currentDir != null)
        {
            var solutionFiles = Directory.GetFiles(currentDir.FullName, "*.sln");
            if (solutionFiles.Length > 0)
            {
                return solutionFiles[0];
            }

            currentDir = currentDir.Parent;
        }

        return null;
    }

    public string? GetSolutionDirectory(string startDirectory)
    {
        var solutionFile = FindSolutionFile(startDirectory);
        return solutionFile != null ? Path.GetDirectoryName(solutionFile) : null;
    }
}


