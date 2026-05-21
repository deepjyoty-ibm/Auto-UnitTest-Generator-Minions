using System;
using System.Collections.Generic;
using System.Text;

using SampleConsoleApp.Interfaces;

namespace SampleConsoleApp.Services
{
    public class CalculatorService : ICalculatorService
    {
        public int Add(int a, int b)
        {
            return a + b;
        }

        public int Divide(int a, int b)
        {
            if (b == 0)
            {
                throw new DivideByZeroException();
            }

            return a / b;
        }

        public int Sum(List<int> numbers)
        {
            int total = 0;

            foreach (var number in numbers)
            {
                total += number;
            }

            return total;
        }
    }
}
