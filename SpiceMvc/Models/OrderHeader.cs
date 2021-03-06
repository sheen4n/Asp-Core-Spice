﻿using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Threading.Tasks;

namespace SpiceMvc.Models
{
    public class OrderHeader
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public string ApplicationUserId { get; set; }
        public ApplicationUser ApplicationUser { get; set; }

        [Required] public DateTime OrderDate { get; set; }
        [Required] public double OrderTotalOriginal { get; set; }
        
        [Required] 
        [DisplayFormat(DataFormatString = "{0:C}")]
        [DisplayName("Order Total")]
        public double OrderTotal { get; set; }

        [Required]
        [DisplayName("Pickup Time")]
        public DateTime PickupTime { get; set; }

        [Required]
        [NotMapped]
        public DateTime PickupDate { get; set; }

        [DisplayName("Coupon Code")] public string CouponCode { get; set; }

        public double CouponCodeDiscount { get; set; }
        public string Status { get; set; }
        public string PaymentStatus { get; set; }
        public string Comments { get; set; }

        [DisplayName("Pickup Name")] public string PickupName { get; set; }
        [DisplayName("Phone Number")] public string PhoneNumber{ get; set; }

        public string TransactionId{ get; set; }

    }
}
