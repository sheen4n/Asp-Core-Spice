using System.Diagnostics;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using SpiceMvc.Data;
using SpiceMvc.Models;
using SpiceMvc.Utility;
using SpiceMvc.ViewModels;

namespace SpiceMvc.Areas.Customer.Controllers
{
    [Area("Customer")]
    public class HomeController : Controller
    {
        private readonly ApplicationDbContext _context;

        public HomeController(ApplicationDbContext context)
        {
            _context = context;

        }

        public async Task<IActionResult> Index()
        {
            var vm = new IndexViewModel
            {
                MenuItems = await _context.MenuItems.Include(m => m.Category).Include(m => m.SubCategory).ToListAsync(),
                Categories = await _context.Categories.ToListAsync(),
                Coupons = await _context.Coupons.Where(c => c.IsActive == true).ToListAsync()

            };

            var claimsIdentity = (ClaimsIdentity) User.Identity;
            var claim = claimsIdentity.FindFirst(ClaimTypes.NameIdentifier);

            if (claim != null)
            {
                var count = _context.ShoppingCarts.Where(s => s.ApplicationUserId == claim.Value).ToList().Count;
                HttpContext.Session.SetInt32(SD.ssShoppingCartCount,count);
            }

            return View(vm);
        }

        [Authorize]
        public async Task<IActionResult> Details(int id)
        {

            var menuItemFromDb = await _context.MenuItems.Include(m => m.Category).Include(m => m.SubCategory)
                .Where(m => m.Id == id).FirstOrDefaultAsync();
            var cartObj = new ShoppingCart
            {
                MenuItem = menuItemFromDb,
                MenuItemId = menuItemFromDb.Id
            };

            return View(cartObj);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize]
        public async Task<IActionResult> AddToCart(ShoppingCart CartObject)
        {
            CartObject.Id = 0;
            if (ModelState.IsValid)
            {
                var claimsIdentity = (ClaimsIdentity) User.Identity;
                var claim = claimsIdentity.FindFirst(ClaimTypes.NameIdentifier);
                CartObject.ApplicationUserId = claim.Value;

                var cartFromDb =
                    await _context.ShoppingCarts.Where(c => c.ApplicationUserId == CartObject.ApplicationUserId && 
                                                            c.MenuItemId == CartObject.MenuItemId).FirstOrDefaultAsync();

                if (cartFromDb == null) await _context.AddAsync(CartObject);
                
                else
                {
                    cartFromDb.Count += CartObject.Count;
                }

                await _context.SaveChangesAsync();

                var listOfCarts = await _context.ShoppingCarts.Where(c => c.ApplicationUserId == CartObject.ApplicationUserId)
                    .ToListAsync();

                var count = listOfCarts.Count();

                HttpContext.Session.SetInt32(SD.ssShoppingCartCount, count);
                return RedirectToAction("Index");
            }
            else
            {
                var menuItemFromDb = await _context.MenuItems.Include(m => m.Category).Include(m => m.SubCategory)
                    .Where(m => m.Id == CartObject.MenuItemId).FirstOrDefaultAsync();
                var cartObj = new ShoppingCart
                {
                    MenuItem = menuItemFromDb,
                    MenuItemId = menuItemFromDb.Id
                };

                return View("Details", cartObj);
            }
        }


        public IActionResult Privacy()
        {
            return View();
        }

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }
    }
}
