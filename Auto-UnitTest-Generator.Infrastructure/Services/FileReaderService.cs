namespace Auto_UnitTest_Generator.Infrastructure.Services;

public class FileReaderService
{
    public async Task<string> ReadFileAsync(string filePath)
    {
        if (!File.Exists(filePath))
        {
            throw new FileNotFoundException($"File not found: {filePath}");
        }

        return await File.ReadAllTextAsync(filePath);
    }

    public bool FileExists(string filePath)
    {
        return File.Exists(filePath);
    }
}


