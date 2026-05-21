using SampleConsoleApp.Models;

namespace SampleConsoleApp.Interfaces
{
    public interface IOrderService
    {
        decimal CalculateDiscount(Order order);

        bool CanShip(Order order);
    }
}
