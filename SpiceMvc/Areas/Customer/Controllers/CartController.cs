using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity.UI.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SpiceMvc.Data;
using SpiceMvc.Models;
using SpiceMvc.Utility;
using SpiceMvc.ViewModels;
using Stripe;

namespace SpiceMvc.Areas.Customer.Controllers
{
    [Area("Customer")]
    public class CartController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly IEmailSender _emailSender;

        [BindProperty] public OrderDetailsCart DetailCart { get; set; }

        public CartController(ApplicationDbContext context, IEmailSender emailSender)
        {
            _context = context;
            _emailSender = emailSender;
        }

        public async Task<IActionResult> Index()
        {
            DetailCart = new OrderDetailsCart()
            {
                OrderHeader = new OrderHeader()
            };

            DetailCart.OrderHeader.OrderTotal = 0;

            var claimsIdentity = (ClaimsIdentity)User.Identity;
            var claim = claimsIdentity.FindFirst(ClaimTypes.NameIdentifier);

            var cart = _context.ShoppingCarts.Include(sc => sc.MenuItem).Where(c => c.ApplicationUserId == claim.Value);

            if (claim != null) DetailCart.ListCart = await cart.ToListAsync();

            foreach (var list in DetailCart.ListCart)
            {
                list.MenuItem = await _context.MenuItems.FirstOrDefaultAsync(m => m.Id == list.MenuItem.Id);
                DetailCart.OrderHeader.OrderTotal += list.MenuItem.Price * list.Count;
                list.MenuItem.Description = SD.ConvertToRawHtml(list.MenuItem.Description);

                if (list.MenuItem.Description.Length > 100)
                {
                    list.MenuItem.Description = $"{list.MenuItem.Description.Substring(0, 99)}...";
                }

            }

            DetailCart.OrderHeader.OrderTotalOriginal = DetailCart.OrderHeader.OrderTotal;

            if (HttpContext.Session.GetString(SD.ssCouponCode) == null) return View(DetailCart);
            {
                DetailCart.OrderHeader.CouponCode = HttpContext.Session.GetString(SD.ssCouponCode);
                var couponFromDb = await _context.Coupons
                    .Where(c => c.Name.ToLower() == DetailCart.OrderHeader.CouponCode.ToLower()).FirstOrDefaultAsync();
                DetailCart.OrderHeader.OrderTotal =
                    SD.DiscountedPrice(couponFromDb, DetailCart.OrderHeader.OrderTotalOriginal);
            }

            return View(DetailCart);
        }


        public IActionResult AddCoupon()
        {
            if (DetailCart.OrderHeader.CouponCode == null) DetailCart.OrderHeader.CouponCode = "";
            HttpContext.Session.SetString(SD.ssCouponCode, DetailCart.OrderHeader.CouponCode);
            return RedirectToAction(nameof(Index));
        }

        public IActionResult RemoveCoupon()
        {
            HttpContext.Session.SetString(SD.ssCouponCode, string.Empty);
            return RedirectToAction(nameof(Index));
        }

        public async Task<IActionResult> Plus(int cartId)
        {
            var cart = await _context.ShoppingCarts.FirstOrDefaultAsync(c => c.Id == cartId);
            cart.Count += 1;
            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }

        public async Task<IActionResult> Minus(int cartId)
        {
            var cart = await _context.ShoppingCarts.FirstOrDefaultAsync(c => c.Id == cartId);
            if (cart.Count == 1)
            {
                _context.ShoppingCarts.Remove(cart);
                await _context.SaveChangesAsync();
                var shoppingListForUser = await _context.ShoppingCarts.Where(u => u.ApplicationUserId == cart.ApplicationUserId)
                    .ToListAsync();
                var count = shoppingListForUser.Count;
                HttpContext.Session.SetInt32(SD.ssShoppingCartCount, count);
            }
            else
            {
                cart.Count -= 1;
                await _context.SaveChangesAsync();
            }

            return RedirectToAction(nameof(Index));
        }

        public async Task<IActionResult> Remove(int cartId)
        {
            var cart = await _context.ShoppingCarts.FirstOrDefaultAsync(c => c.Id == cartId);

            _context.ShoppingCarts.Remove(cart);
            await _context.SaveChangesAsync();
            var shoppingListForUser = await _context.ShoppingCarts.Where(u => u.ApplicationUserId == cart.ApplicationUserId)
                .ToListAsync();
            var count = shoppingListForUser.Count;
            HttpContext.Session.SetInt32(SD.ssShoppingCartCount, count);

            return RedirectToAction(nameof(Index));
        }

        public async Task<IActionResult> Summary()
        {
            DetailCart = new OrderDetailsCart()
            {
                OrderHeader = new OrderHeader()
            };

            DetailCart.OrderHeader.OrderTotal = 0;

            var claimsIdentity = (ClaimsIdentity)User.Identity;
            var claim = claimsIdentity.FindFirst(ClaimTypes.NameIdentifier);

            var applicationUser =
                await _context.ApplicationUsers.Where(u => u.Id == claim.Value).FirstOrDefaultAsync();

            var cart = _context.ShoppingCarts.Include(sc => sc.MenuItem).Where(c => c.ApplicationUserId == claim.Value);

            if (claim != null) DetailCart.ListCart = await cart.ToListAsync();

            foreach (var list in DetailCart.ListCart)
            {
                list.MenuItem = await _context.MenuItems.FirstOrDefaultAsync(m => m.Id == list.MenuItem.Id);
                DetailCart.OrderHeader.OrderTotal += list.MenuItem.Price * list.Count;

            }

            DetailCart.OrderHeader.OrderTotalOriginal = DetailCart.OrderHeader.OrderTotal;
            DetailCart.OrderHeader.PickupName = applicationUser.Name;
            DetailCart.OrderHeader.PhoneNumber = applicationUser.PhoneNumber;
            DetailCart.OrderHeader.PickupTime = DateTime.Now;

            if (HttpContext.Session.GetString(SD.ssCouponCode) == null) return View(DetailCart);
            {
                DetailCart.OrderHeader.CouponCode = HttpContext.Session.GetString(SD.ssCouponCode);
                var couponFromDb = await _context.Coupons
                    .Where(c => c.Name.ToLower() == DetailCart.OrderHeader.CouponCode.ToLower()).FirstOrDefaultAsync();
                DetailCart.OrderHeader.OrderTotal =
                    SD.DiscountedPrice(couponFromDb, DetailCart.OrderHeader.OrderTotalOriginal);
            }

            return View(DetailCart);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [ActionName("Summary")]
        public async Task<IActionResult> SummaryPost(string stripeToken)
        {
            var claimsIdentity = (ClaimsIdentity)User.Identity;
            var claim = claimsIdentity.FindFirst(ClaimTypes.NameIdentifier);


            DetailCart.ListCart = await _context.ShoppingCarts.Where(s => s.ApplicationUserId == claim.Value).ToListAsync();

            DetailCart.OrderHeader.PaymentStatus = SD.PaymentStatusPending;
            DetailCart.OrderHeader.OrderDate = DateTime.Now;
            DetailCart.OrderHeader.ApplicationUserId = claim.Value;
            DetailCart.OrderHeader.Status = SD.PaymentStatusPending;
            var pickupDateString = Request.Form["pickupDate"];
            var pickupTimeString = Request.Form["pickupTime"];
            var pickupDateTime = DateTime.ParseExact(pickupDateString + " " + pickupTimeString, "MM/dd/yyyy hh:mm tt", System.Globalization.CultureInfo.InvariantCulture);
            DetailCart.OrderHeader.PickupTime = pickupDateTime;

            _context.OrderHeaders.Add(DetailCart.OrderHeader);
            await _context.SaveChangesAsync();

            DetailCart.OrderHeader.OrderTotalOriginal = 0;

            foreach (var item in DetailCart.ListCart)
            {
                item.MenuItem = await _context.MenuItems.FirstOrDefaultAsync(m => m.Id == item.MenuItemId);
                var orderDetails = new OrderDetail
                {
                    MenuItemId = item.MenuItemId,
                    OrderId = DetailCart.OrderHeader.Id,
                    Description = item.MenuItem.Description,
                    Name = item.MenuItem.Name,
                    Price = item.MenuItem.Price,
                    Count = item.Count
                };
                DetailCart.OrderHeader.OrderTotalOriginal += orderDetails.Count * orderDetails.Price;
                _context.OrderDetails.Add(orderDetails);

            }

            if (HttpContext.Session.GetString(SD.ssCouponCode) != null)
            {
                DetailCart.OrderHeader.CouponCode = HttpContext.Session.GetString(SD.ssCouponCode);
                var couponFromDb = await _context.Coupons.Where(c => c.Name.ToLower() == DetailCart.OrderHeader.CouponCode.ToLower()).FirstOrDefaultAsync();
                DetailCart.OrderHeader.OrderTotal = SD.DiscountedPrice(couponFromDb, DetailCart.OrderHeader.OrderTotalOriginal);
            }
            else
            {
                DetailCart.OrderHeader.OrderTotal = DetailCart.OrderHeader.OrderTotalOriginal;
            }
            DetailCart.OrderHeader.CouponCodeDiscount = DetailCart.OrderHeader.OrderTotalOriginal - DetailCart.OrderHeader.OrderTotal;

            _context.ShoppingCarts.RemoveRange(DetailCart.ListCart);
            HttpContext.Session.SetInt32(SD.ssShoppingCartCount, 0);
            await _context.SaveChangesAsync();

            var options = new ChargeCreateOptions
            {
                Amount = Convert.ToInt32(DetailCart.OrderHeader.OrderTotal * 100),
                Currency = "usd",
                Description = "Order ID : " + DetailCart.OrderHeader.Id,
                SourceId = stripeToken

            };
            var service = new ChargeService();
            var charge = service.Create(options);

            if (charge.BalanceTransactionId == null)
            {
                DetailCart.OrderHeader.PaymentStatus = SD.PaymentStatusRejected;
            }
            else
            {
                DetailCart.OrderHeader.TransactionId = charge.BalanceTransactionId;
            }

            if (charge.Status.ToLower() == "succeeded")
            {
                var user = await _context.ApplicationUsers
                    .FirstOrDefaultAsync(u => u.Id == claim.Value);

                var email = user.Email;

                await _emailSender.SendEmailAsync(email, "Spice - Order Created" + DetailCart.OrderHeader.Id, "Order has been submitted successfully.");
                DetailCart.OrderHeader.PaymentStatus = SD.PaymentStatusApproved;
                DetailCart.OrderHeader.Status = SD.StatusSubmitted;
            }
            else
            {
                DetailCart.OrderHeader.PaymentStatus = SD.PaymentStatusRejected;
            }

            await _context.SaveChangesAsync();
            return RedirectToAction("Index", "Home");

        }



    }
}
