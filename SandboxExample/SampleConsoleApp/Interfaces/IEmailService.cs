using System;
using System.Collections.Generic;
using System.Text;

namespace SampleConsoleApp.Interfaces
{
    public interface IEmailService
    {
        Task SendWelcomeEmailAsync(string email);
    }
}
