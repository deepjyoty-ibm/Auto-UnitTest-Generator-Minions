namespace Auto_UnitTest_Generator.Infrastructure.AI;

public interface IAiClient
{
    Task<string> GenerateCompletionAsync(string prompt, double temperature = 0.2);
}


