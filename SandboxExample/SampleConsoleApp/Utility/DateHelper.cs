using System;
using System.Collections.Generic;
using System.Text;

namespace SampleConsoleApp.Utility
{
    public static class DateHelper
    {
        public static int GetAge(DateTime birthDate)
        {
            var age = DateTime.Today.Year - birthDate.Year;

            if (birthDate.Date > DateTime.Today.AddYears(-age))
            {
                age--;
            }

            return age;
        }
    }
}
