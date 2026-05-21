using System.Collections.Generic;

namespace SampleConsoleApp.Interfaces
{
    public interface ICalculatorService
    {
        int Add(int a, int b);

        int Divide(int a, int b);

        int Sum(List<int> numbers);
    }
}
