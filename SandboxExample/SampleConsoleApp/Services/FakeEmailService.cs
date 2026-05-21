using SampleConsoleApp.Interfaces;
using System;
using System.Collections.Generic;
using System.Text;

namespace SampleConsoleApp.Services
{
    public class FakeEmailService : IEmailService
    {
        public async Task SendWelcomeEmailAsync(string email)
        {
            await Task.Delay(100);

            Console.WriteLine($"Welcome email sent to {email}");
        }
    }
}
