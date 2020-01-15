using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using SpiceMvc.Models;

namespace SpiceMvc.Utility
{
    public static class SD
    {
        public const string DefaultFoodImage = "default_food.png";
        public const string ManagerUser = "Manager";
        public const string KitchenUser = "Kitchen";
        public const string FrontDeskUser = "FrontDesk";
        public const string CustomerEndUser = "Customer";

        public const string ssShoppingCartCount = "ssCartCount";
        public const string ssCouponCode = "ssCouponCode";

        public const string StatusSubmitted = "Submitted";
        public const string StatusInProcess = "Being Prepared";
        public const string StatusReady = "Ready for Pickup";
        public const string StatusCompleted = "Completed";
        public const string StatusCancelled = "Cancelled";

        public const string PaymentStatusPending = "Pending";
        public const string PaymentStatusApproved = "Approved";
        public const string PaymentStatusRejected = "Rejected";


        public static string ConvertToRawHtml(string source)
        {
            char[] array = new char[source.Length];
            int arrayIndex = 0;
            bool inside = false;

            foreach (var let in source)
            {
                if (let == '<')
                {
                    inside = true;
                    continue;
                }

                if (let == '>')
                {
                    inside = false;
                    continue;
                }

                if (inside) continue;
                array[arrayIndex] = let;
                arrayIndex++;
            }

            return new string(array, 0, arrayIndex);
        }

        public static double DiscountedPrice(Coupon couponFromDb, double originalOrderTotal)
        {
            if (couponFromDb == null) return originalOrderTotal;
            switch (Convert.ToInt32(couponFromDb.CouponType))
            {
                case (int)Coupon.ECouponType.Dollar:
                    return Math.Round(originalOrderTotal - couponFromDb.Discount, 2);

                case (int)Coupon.ECouponType.Percent:
                    return Math.Round((1 - couponFromDb.Discount / 100) * originalOrderTotal, 2);
            }
            return originalOrderTotal;
        }
    }
}
