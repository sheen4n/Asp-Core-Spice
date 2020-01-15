using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using SpiceMvc.Data;
using SpiceMvc.Models;
using SpiceMvc.Utility;

namespace SpiceMvc.Areas.Admin.Controllers
{
    [Authorize(Roles = SD.ManagerUser)]
    [Area("Admin")]
    public class UsersController : Controller
    {
        private readonly ApplicationDbContext _context;

        public UsersController(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<IActionResult> Index()
        {
            var claimsIdentity = (ClaimsIdentity) User.Identity;
            var claim = claimsIdentity.FindFirst(ClaimTypes.NameIdentifier);
            if (claim == null) return NotFound();
            return View(await _context.ApplicationUsers.Where(u=>u.Id != claim.Value).ToListAsync());
        }

        public async Task<IActionResult> Lock(string id)
        {
            if (id == null) return NotFound();

            var applicationUser = await _context.ApplicationUsers.FirstOrDefaultAsync(u => u.Id == id);

            if (applicationUser == null) return NotFound();

            applicationUser.LockoutEnd = DateTime.Now.AddYears(1000);
            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }

        public async Task<IActionResult> Unlock(string id)
        {
            if (id == null) return NotFound();

            var applicationUser = await _context.ApplicationUsers.FirstOrDefaultAsync(u => u.Id == id);

            if (applicationUser == null) return NotFound();

            applicationUser.LockoutEnd = DateTime.Now;
            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }
    }
}