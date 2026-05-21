using System;
using System.Collections.Generic;
using System.Text;

namespace SampleConsoleApp.Utility
{
    public static class PasswordHelper
    {
        public static bool IsStrongPassword(string password)
        {
            return password.Length >= 8
                   && password.Any(char.IsUpper)
                   && password.Any(char.IsDigit);
        }
    }
}
