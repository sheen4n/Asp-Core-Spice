using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using SpiceMvc.Models;

namespace SpiceMvc.ViewModels
{
    public class OrderDetailsCart
    {
        public List<ShoppingCart> ListCart { get; set; }
        public OrderHeader OrderHeader { get; set; }

        public OrderDetailsCart()
        {
            ListCart = new List<ShoppingCart>();
        }
    }
}
