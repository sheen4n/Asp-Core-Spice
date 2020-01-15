using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Security.Claims;
using System.Text;
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
    public class OrderController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly IEmailSender _emailSender;
        public int PageSize = 2;


        public OrderController(ApplicationDbContext context, IEmailSender emailSender)
        {
            _context = context;
            _emailSender = emailSender;
        }

        [Authorize]
        public async Task<IActionResult> Confirm(int id)
        {
            var claimsIdentity = (ClaimsIdentity) User.Identity;
            var claim = claimsIdentity.FindFirst(ClaimTypes.NameIdentifier);

            var orderHeaderFromDb = await _context.OrderHeaders.Include(o => o.ApplicationUser)
                .FirstOrDefaultAsync(o => o.Id == id && o.ApplicationUserId == claim.Value);

            var orderDetailsFromDb = await _context.OrderDetails.Where(o => o.OrderId == id).ToListAsync();



            var viewModel = new OrderDetailsViewModel
            {
                OrderHeader = orderHeaderFromDb,
                OrderDetails = orderDetailsFromDb
            };

            return View(viewModel);
        }

        [Authorize]
        public async Task<IActionResult> OrderHistory(int productPage = 1)
        {
            var claimsIdentity = (ClaimsIdentity) User.Identity;
            var claim = claimsIdentity.FindFirst(ClaimTypes.NameIdentifier);

            var orderHeaderList = await _context.OrderHeaders.Include(o => o.ApplicationUser).Where(o => o.ApplicationUserId == claim.Value).ToListAsync();

            var orderListVm = new OrderListViewModel();

            foreach (var item in orderHeaderList)
            {
                var individual = new OrderDetailsViewModel
                {
                    OrderHeader = item,
                    OrderDetails = await _context.OrderDetails.Where(o => o.OrderId == item.Id).ToListAsync()
                };
                orderListVm.Orders.Add(individual);
            }

            var totalCount = orderListVm.Orders.Count;

            orderListVm.Orders = orderListVm.Orders.OrderByDescending(p => p.OrderHeader.Id)
                .Skip((productPage - 1) * PageSize)
                .Take(PageSize).ToList();

            var pagingInfo = new PagingInfo
            {
                CurrentPage = productPage,
                ItemsPerPage = PageSize,
                TotalItem = totalCount,
                UrlParam = "/Customer/Order/OrderHistory?productPage=:"
            };

            orderListVm.PagingInfo = pagingInfo;
            return View(orderListVm);
        }

        [Authorize]
        public async Task<IActionResult> GetOrderDetails(int id)
        {
            var orderDetailsViewModel = new OrderDetailsViewModel
            {
                OrderHeader = await _context.OrderHeaders.Include(o => o.ApplicationUser).FirstOrDefaultAsync(o => o.Id == id),
                OrderDetails = await _context.OrderDetails.Where(o => o.OrderId == id).ToListAsync()
            };

            //var orderDetailsViewModel = await _context.OrderHeaders.Include(o => o.ApplicationUser)
            //    .Select(o => new OrderDetailsViewModel
            //    {
            //        OrderHeader = o,
            //        OrderDetails = _context.OrderDetails.Where(orderDetail => orderDetail.OrderId == o.Id).ToList()
            //    }).FirstOrDefaultAsync();
            return PartialView("_IndividualOrderDetails", orderDetailsViewModel); ;
        }

        [Authorize]
        public async Task<IActionResult> GetOrderStatus(int id)
        {

            var orderFromDb = await _context.OrderHeaders.FindAsync(id);
            var orderStatus = orderFromDb.Status;

            return PartialView("_IndividualOrderStatus", orderStatus);
        }

        [Authorize(Roles = SD.KitchenUser + "," + SD.ManagerUser)]
        public async Task<IActionResult> ManageOrder()
        {

            var orderDetailsVm = new List<OrderDetailsViewModel>();

            var orderHeaderList = await _context.OrderHeaders
                .Where(o => o.Status == SD.StatusSubmitted || o.Status == SD.StatusInProcess)
                .OrderByDescending(o => o.PickupTime).ToListAsync();

            foreach (var item in orderHeaderList)
            {
                var individual = new OrderDetailsViewModel
                {
                    OrderHeader = item,
                    OrderDetails = await _context.OrderDetails.Where(o => o.OrderId == item.Id).ToListAsync()
                };
                orderDetailsVm.Add(individual);
            }

            return View(orderDetailsVm.OrderBy(o => o.OrderHeader.PickupTime));
        }

        [Authorize(Roles = SD.KitchenUser + "," + SD.ManagerUser)]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> OrderPrepare(int OrderId)
        {
            var orderHeaderFromDb = await _context.OrderHeaders.FindAsync(OrderId);
            orderHeaderFromDb.Status = SD.StatusInProcess;
            await _context.SaveChangesAsync();


            var user = await _context.ApplicationUsers
                .FirstOrDefaultAsync(u => u.Id == orderHeaderFromDb.ApplicationUserId);

            var email = user.Email;

            await _emailSender.SendEmailAsync(email, "Spice - Order Is Being Prepared Now" + orderHeaderFromDb.Id, "Your order is being prepared - Stay Tuned.");

            return RedirectToAction("ManageOrder", "Order");
        }

        [Authorize(Roles = SD.KitchenUser + "," + SD.ManagerUser)]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> OrderReady(int OrderId)
        {
            var orderHeaderFromDb = await _context.OrderHeaders.FindAsync(OrderId);
            orderHeaderFromDb.Status = SD.StatusReady;
            await _context.SaveChangesAsync();


            var user = await _context.ApplicationUsers
                .FirstOrDefaultAsync(u => u.Id == orderHeaderFromDb.ApplicationUserId);

            var email = user.Email;

            await _emailSender.SendEmailAsync(email, "Spice - Order Ready" + orderHeaderFromDb.Id, "Your order is now ready for collection.");

            return RedirectToAction("ManageOrder", "Order");
        }

        [Authorize(Roles = SD.KitchenUser + "," + SD.ManagerUser)]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> OrderCancel(int OrderId)
        {
            var orderHeaderFromDb = await _context.OrderHeaders.FindAsync(OrderId);
            orderHeaderFromDb.Status = SD.StatusCancelled;
            await _context.SaveChangesAsync();

            var user = await _context.ApplicationUsers
                .FirstOrDefaultAsync(u => u.Id == orderHeaderFromDb.ApplicationUserId);

            var email = user.Email;

            await _emailSender.SendEmailAsync(email, "Spice - Order Cancelled" + orderHeaderFromDb.Id, "Order has been Cancelled.");


            return RedirectToAction("ManageOrder", "Order");
        }

        [Authorize(Roles = SD.KitchenUser + "," + SD.ManagerUser)]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> OrderPickup(int OrderId)
        {
            var orderHeaderFromDb = await _context.OrderHeaders.FindAsync(OrderId);
            orderHeaderFromDb.Status = SD.StatusCompleted;
            await _context.SaveChangesAsync();
            var user = await _context.ApplicationUsers
                .FirstOrDefaultAsync(u => u.Id == orderHeaderFromDb.ApplicationUserId);

            var email = user.Email;

            await _emailSender.SendEmailAsync(email, "Spice - Order Completed" + orderHeaderFromDb.Id, "Order has been Picked Up successfully.");
            return RedirectToAction("OrderPickup", "Order");
        }

        [Authorize]
        public async Task<IActionResult> OrderPickup(int productPage = 1, string searchEmail = null, string searchName = null, string searchPhone = null)
        {
            var orderListVm = new OrderListViewModel();
            var param = new StringBuilder().Append("/Customer/Order/OrderPickup?productPage=:");
            if (searchName != null) param.Append($"&searchName={searchName}");
            if (searchPhone != null) param.Append($"&searchPhone={searchPhone}");
            if (searchEmail != null) param.Append($"&searchEmail={searchEmail}");

            Expression<Func<OrderHeader, bool>> whereClause;

            if (searchName != null)
            {
                whereClause = o => o.PickupName.ToLower().Contains(searchName.ToLower());
            }
            else if (searchPhone != null)
            {
                whereClause = o => o.PhoneNumber.Contains(searchPhone);
            }
            else if (searchEmail != null)
            {
                var user = await _context.ApplicationUsers.Where(u => u.Email.ToLower().Contains(searchEmail.ToLower()))
                    .FirstOrDefaultAsync();
                whereClause = o => o.ApplicationUserId == user.Id;
            }
            else
            {
                whereClause = o => o.Status == SD.StatusReady;
            }

            var orderHeaderList = await _context.OrderHeaders.Include(o => o.ApplicationUser).Where(whereClause).ToListAsync();

            foreach (var item in orderHeaderList)
            {
                var individual = new OrderDetailsViewModel
                {
                    OrderHeader = item,
                    OrderDetails = await _context.OrderDetails.Where(o => o.OrderId == item.Id).ToListAsync()
                };
                orderListVm.Orders.Add(individual);
            }

            var totalCount = orderListVm.Orders.Count;

            orderListVm.Orders = orderListVm.Orders.OrderByDescending(p => p.OrderHeader.Id)
                .Skip((productPage - 1) * PageSize)
                .Take(PageSize).ToList();

            var pagingInfo = new PagingInfo
            {
                CurrentPage = productPage,
                ItemsPerPage = PageSize,
                TotalItem = totalCount,
                UrlParam = param.ToString()
            };

            orderListVm.PagingInfo = pagingInfo;
            return View(orderListVm);
        }

    }


}