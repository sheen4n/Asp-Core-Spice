using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using SpiceMvc.Utility;

namespace SpiceMvc.ViewModels
{
    public class OrderListViewModel
    {
        public IList<OrderDetailsViewModel> Orders { get; set; }
        public PagingInfo PagingInfo { get; set; }

        public OrderListViewModel()
        {
            Orders = new List<OrderDetailsViewModel>();
        }
    }
}
