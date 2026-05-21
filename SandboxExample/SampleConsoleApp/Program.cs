using Microsoft.Extensions.DependencyInjection;
using SampleConsoleApp.Interfaces;
using SampleConsoleApp.Models;
using SampleConsoleApp.Repository;
using SampleConsoleApp.Services;

var services = new ServiceCollection();

services.AddScoped<IUserRepository, UserRepository>();
services.AddScoped<IEmailService, FakeEmailService>();

services.AddScoped<IOrderService, OrderService>();
services.AddScoped<ICalculatorService, CalculatorService>();
services.AddScoped<UserService>();

var provider = services.BuildServiceProvider();

var userService = provider.GetRequiredService<UserService>();
var calculator = provider.GetRequiredService<ICalculatorService>();
var orderService = provider.GetRequiredService<IOrderService>();

try
{
    var user = userService.GetUser(1);

    Console.WriteLine($"User found: {user.Name}");

    await userService.RegisterUserAsync(
        new User
        {
            Id = 2,
            Name = "Alice",
            Email = "alice@test.com",
            Age = 25
        });

    Console.WriteLine("User registered");
}
catch (Exception ex)
{
    Console.WriteLine(ex.Message);
}

var result = calculator.Add(10, 20);

Console.WriteLine($"Addition Result: {result}");

var total = calculator.Sum([1, 2, 3, 4]);

Console.WriteLine($"Total Sum: {total}");

var order = new Order
{
    Id = 1,
    Amount = 1500,
    IsPaid = true
};

var discount = orderService.CalculateDiscount(order);

Console.WriteLine($"Discount: {discount}");

var canShip = orderService.CanShip(order);

Console.WriteLine($"Can Ship: {canShip}");