using SampleConsoleApp.Interfaces;
using SampleConsoleApp.Models;
using System;
using System.Collections.Generic;
using System.Text;

namespace SampleConsoleApp.Repository
{
    public class UserRepository : IUserRepository
    {
        private readonly List<User> _users =
        [
            new() { Id = 1, Name = "John", Email = "john@test.com", Age = 20 },
            new() { Id = 2, Name = "Dolly", Email = "dolly@test.com", Age = 25 },
        ];

        public User? GetById(int id)
        {
            return _users.FirstOrDefault(x => x.Id == id);
        }

        public async Task<User?> GetByIdAsync(int id)
        {
            await Task.Delay(100);

            return _users.FirstOrDefault(x => x.Id == id);
        }

        public void Add(User user)
        {
            _users.Add(user);
        }
    }
}
