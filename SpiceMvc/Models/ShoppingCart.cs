using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Threading.Tasks;

namespace SpiceMvc.Models
{
    public class ShoppingCart
    {
        public ShoppingCart()
        {
            Count = 1;
        }

        public int Id { get; set; }

        public string ApplicationUserId { get; set; }
        public ApplicationUser ApplicationUser { get; set; }

        public int MenuItemId { get; set; }
        public MenuItem MenuItem { get; set; }

        [Range(1, int.MaxValue, ErrorMessage = "Please enter a value greater or equal than 1")]
        public int Count { get; set; }
    }
}
