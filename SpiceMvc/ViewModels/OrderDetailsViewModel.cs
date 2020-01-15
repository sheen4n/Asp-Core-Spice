using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using SpiceMvc.Models;

namespace SpiceMvc.ViewModels
{
    public class OrderDetailsViewModel
    {
        public OrderHeader OrderHeader { get; set; }
        public List<OrderDetail> OrderDetails { get; set; }

        public OrderDetailsViewModel()
        {
            OrderDetails = new List<OrderDetail>();
        }
    }
}
