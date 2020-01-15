using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Threading.Tasks;

namespace SpiceMvc.Models
{
    public class OrderDetail
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public int OrderId { get; set; }

        [ForeignKey("OrderId")]
        public virtual OrderHeader OrderHeader{ get; set; }

        [Required] public int MenuItemId { get; set; } 
        public virtual MenuItem MenuItem { get; set; }

        public int Count { get; set; }
        public string Name { get; set; }
        public string Description { get; set; }

        [Required] public double Price { get; set; }

    }
}
