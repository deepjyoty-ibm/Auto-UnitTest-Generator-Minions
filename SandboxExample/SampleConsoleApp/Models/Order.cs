using System;
using System.Collections.Generic;
using System.Text;

namespace SampleConsoleApp.Models
{
    public class Order
    {
        public int Id { get; set; }

        public decimal Amount { get; set; }

        public bool IsPaid { get; set; }
    }
}
