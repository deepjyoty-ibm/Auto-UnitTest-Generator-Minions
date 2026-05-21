using SampleConsoleApp.Models;
using System;
using System.Collections.Generic;
using System.Text;

namespace SampleConsoleApp.Interfaces
{
    public interface IUserRepository
    {
        User? GetById(int id);

        Task<User?> GetByIdAsync(int id);

        void Add(User user);
    }
}
