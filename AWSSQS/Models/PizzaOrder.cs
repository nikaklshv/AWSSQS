using System;
using System.Collections.Generic;
using System.Text;

namespace AWSSQS.Models
{
    public class PizzaOrder
    {
        public int OrderId { get; set; }
        public string CustomerName { get; set; }
        public string PizzaType { get; set; }
        public DateTime OrderDate { get; set; }
    }
}
