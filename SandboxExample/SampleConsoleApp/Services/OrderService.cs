using SampleConsoleApp.Models;
using System;
using System.Collections.Generic;
using System.Text;

using SampleConsoleApp.Interfaces;

namespace SampleConsoleApp.Services
{
    public class OrderService : IOrderService
    {
        public decimal CalculateDiscount(Order order)
        {
            if (order.Amount > 1000)
            {
                return order.Amount * 0.10m;
            }

            return 0;
        }

        public bool CanShip(Order order)
        {
            return order.IsPaid;
        }
    }
}
