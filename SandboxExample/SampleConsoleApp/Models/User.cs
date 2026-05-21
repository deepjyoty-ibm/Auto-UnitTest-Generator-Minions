using System;
using System.Collections.Generic;
using System.Text;

namespace SampleConsoleApp.Models
{
    public class User
    {
        public int Id { get; set; }

        public string Name { get; set; } = string.Empty;

        public string Email { get; set; } = string.Empty;

        public int Age { get; set; }
    }
}
