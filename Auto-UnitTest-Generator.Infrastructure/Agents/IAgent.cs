namespace Auto_UnitTest_Generator.Infrastructure.Agents;

public interface IAgent
{
    Task<string> ExecuteAsync(string input);
}


