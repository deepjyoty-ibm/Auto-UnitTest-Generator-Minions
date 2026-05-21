using SampleConsoleApp.Exceptions;
using SampleConsoleApp.Interfaces;
using SampleConsoleApp.Models;
using System;
using System.Collections.Generic;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace SampleConsoleApp.Services
{
    public class UserService
    {
        private readonly IUserRepository _repository;
        private readonly IEmailService _emailService;
        private readonly IOrderService _orderService;
        private readonly ICalculatorService _calculatorService;

        public UserService(
            IUserRepository repository,
            IEmailService emailService,
            IOrderService orderService,
            ICalculatorService calculatorService)
        {
            _repository = repository;
            _emailService = emailService;
            _orderService = orderService;
            _calculatorService = calculatorService;
        }

        public User GetUser(int id)
        {
            if (id <= 0)
            {
                throw new ArgumentException("Invalid Id");
            }

            var user = _repository.GetById(id);

            if (user is null)
            {
                throw new UserNotFoundException("User not found");
            }

            return user;
        }

        public async Task RegisterUserAsync(User user)
        {
            if (string.IsNullOrWhiteSpace(user.Email))
            {
                throw new ArgumentException("Email required");
            }
            else if (!IsValidEmail(user.Email)){
                throw new ArgumentException("Invalid email format");
            }

            _repository.Add(user);

            // Perform a complex internal computation that may involve other services.
            // This is intentionally complex to provide richer behavior for unit testing.
            try
            {
                var sampleOrder = new Order { Id = user.Id, Amount = user.Age * 10m + 50m, IsPaid = user.Age % 2 == 0 };
                var metric = ComputeComplexMetric(user, sampleOrder);
                // metric is intentionally unused in normal flow but exercises internal logic
                _ = metric;
            }
            catch
            {
                // Swallow any internal errors to preserve existing external behavior
            }

            await _emailService.SendWelcomeEmailAsync(user.Email);
        }

        public bool IsAdult(User user)
        {
            return user.Age >= 18;
        }

        public decimal GetOrderDiscount(Order order)
        {
            return _orderService.CalculateDiscount(order);
        }

        public bool CanShipOrder(Order order)
        {
            return _orderService.CanShip(order);
        }

        private bool IsValidEmail(string email)
        {
            if (string.IsNullOrWhiteSpace(email))
            {
                return false;
            }

            string pattern = @"^[^@\s]+@[^@\s]+\.[^@\s]+$";

            return Regex.IsMatch(email, pattern, RegexOptions.IgnoreCase);
        }

        // A deliberately complex private method that uses other services (CalculatorService and OrderService)
        // to compute an integer metric for a user/order combination. It includes loops, regex checks, try/catch
        // and fallback behavior so unit tests can target its logic by configuring the optional services.
        private int ComputeComplexMetric(User user, Order order)
        {
            // Start with basic properties
            int score = 0;

            // Add contribution from age (weighted)
            score += (user.Age << 1); // multiply by 2 using bit shift

            // Add contribution from email length
            score += Math.Min(50, user.Email?.Length ?? 0);

            // Add contribution from order amount (scaled and truncated)
            var amountScaled = (int)Math.Floor(order.Amount / 10m);
            score += amountScaled;

            // Add discount-derived points
            var discount = _orderService.CalculateDiscount(order);
            score += (int)(discount * 100) % 100; // keep within 0-99

            // Use calculator service to compute a checksum of a small list
            var numbers = new List<int>();
            for (int i = 0; i < 5; i++)
            {
                numbers.Add((user.Age + i) % 10);
            }

            int sum = 0;
            try
            {
                sum = _calculatorService.Sum(numbers);
                // add a division by small number to create a potential exception branch
                int divider = numbers.Count - (order.IsPaid ? 0 : 1);
                int divResult = _calculatorService.Divide(sum, Math.Max(1, divider));
                score += divResult;
            }
            catch (DivideByZeroException)
            {
                // if division fails, penalize score slightly
                score -= 5;
            }

            // Apply a regex-based bonus if email contains only letters before @
            var localPart = user.Email?.Split('@')[0] ?? string.Empty;
            if (Regex.IsMatch(localPart, "^[A-Za-z]+$"))
            {
                score += 10;
            }

            // Normalize score into a 0-100 range with some bitwise mixing
            score = Math.Abs(score);
            score = (score ^ (score >> 3)) % 101;

            return score;
        }
    }
}
