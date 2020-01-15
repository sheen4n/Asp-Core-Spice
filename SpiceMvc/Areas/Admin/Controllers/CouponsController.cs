using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using SpiceMvc.Data;
using SpiceMvc.Models;
using SpiceMvc.Utility;
using SpiceMvc.ViewModels;

namespace SpiceMvc.Areas.Admin.Controllers
{
    [Authorize(Roles = SD.ManagerUser)]
    [Area("Admin")]
    public class CouponsController : Controller
    {
        private readonly ApplicationDbContext _context;

        [BindProperty] public Coupon Coupon { get; set; }


        public CouponsController(ApplicationDbContext context)
        {
            _context = context;
            Coupon = new Coupon();
        }

        public async Task<IActionResult> Index()
        {
            var coupons = await _context.Coupons.ToListAsync();

            return View(coupons);
        }

        [HttpGet]
        public IActionResult Create()
        {
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [ActionName("Create")]
        public async Task<IActionResult> CreatePost()
        {
            if (!ModelState.IsValid) return View();

            var files = HttpContext.Request.Form.Files;

            if (files.Count > 0)
            {
                byte[] p1 = null;
                using (var fs1 = files[0].OpenReadStream())
                {
                    using (var ms1 = new MemoryStream())
                    {
                        fs1.CopyTo(ms1);
                        p1 = ms1.ToArray();
                    }
                }

                Coupon.Picture = p1;
            }

            _context.Coupons.Add(Coupon);
            await _context.SaveChangesAsync();

            return RedirectToAction(nameof(Index));
        }

        [HttpGet]
        public async Task<IActionResult> Edit(int? id)
        {
            var couponFromDb = await _context.Coupons.FindAsync(id);

            if (couponFromDb == null) return NotFound();

            Coupon = couponFromDb;

            return View(Coupon);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [ActionName("Edit")]
        public async Task<IActionResult> EditPost(int? id)
        {
            if (id == null) return NotFound();

            var couponFromDb = await _context.Coupons.FindAsync(id);

            if (!ModelState.IsValid) return View(Coupon);

            var files = HttpContext.Request.Form.Files;

            if (files.Count > 0)
            {
                byte[] p1 = null;
                using (var fs1 = files[0].OpenReadStream())
                {
                    using (var ms1 = new MemoryStream())
                    {
                        fs1.CopyTo(ms1);
                        p1 = ms1.ToArray();
                    }
                }

                couponFromDb.Picture = p1;
            }

            couponFromDb.Name = Coupon.Name;
            couponFromDb.CouponType = Coupon.CouponType;
            couponFromDb.Discount = Coupon.Discount;
            couponFromDb.MinimumAmount = Coupon.MinimumAmount;
            couponFromDb.IsActive = Coupon.IsActive;

            await _context.SaveChangesAsync();

            return RedirectToAction(nameof(Index));
        }

        [HttpGet]
        public async Task<IActionResult> Details(int? id)
        {
            var couponFromDb = await _context.Coupons.FindAsync(id);

            if (couponFromDb == null) return NotFound();

            Coupon = couponFromDb;

            return View(Coupon);
        }

        [HttpGet]
        public async Task<IActionResult> Delete(int? id)
        {
            var couponFromDb = await _context.Coupons.FindAsync(id);

            if (couponFromDb == null) return NotFound();

            Coupon = couponFromDb;

            return View(Coupon);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [ActionName("Delete")]
        public async Task<IActionResult> DeletePost(int? id)
        {
            var couponFromDb = await _context.Coupons.FindAsync(id);

            if (couponFromDb == null) return NotFound();

            _context.Coupons.Remove(couponFromDb);

            await _context.SaveChangesAsync();

            return RedirectToAction(nameof(Index));
        }
    }
}

