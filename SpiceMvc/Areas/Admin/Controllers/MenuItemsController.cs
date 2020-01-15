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
    public class MenuItemsController : Controller
    {
        private readonly ApplicationDbContext _context;
        public readonly IWebHostEnvironment _hostingEnvironment;

        [TempData] public string StatusMessage { get; set; }
        [BindProperty] public MenuItemFormViewModel MenuItemFormViewModel { get; set; }


        public MenuItemsController(ApplicationDbContext context, IWebHostEnvironment env)
        {
            _hostingEnvironment = env;
            _context = context;
            MenuItemFormViewModel = new MenuItemFormViewModel()
            {
                Categories =  _context.Categories,
                MenuItem =  new MenuItem()
            };
        }

        public async Task<IActionResult> Index()
        {
            var menuItems = await _context.MenuItems.Include(m => m.SubCategory).Include(s => s.Category).ToListAsync();

            return View(menuItems);
        }

        [HttpGet]
        public IActionResult Create()
        {
            return View(MenuItemFormViewModel);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [ActionName("Create")]
        public async Task <IActionResult> CreatePost()
        {
            MenuItemFormViewModel.MenuItem.SubCategoryId = Convert.ToInt32(Request.Form["SubCategoryId"].ToString());

            if (!ModelState.IsValid) return View("Create", MenuItemFormViewModel);

            _context.Add(MenuItemFormViewModel.MenuItem);
            await _context.SaveChangesAsync();

            // Work on the image saving section
            string webRootPath = _hostingEnvironment.WebRootPath;

            var files = HttpContext.Request.Form.Files;

            var menuItemFromDb = await _context.MenuItems.FindAsync(MenuItemFormViewModel.MenuItem.Id);

            if (files.Count > 0)
            {
                // files has been uploaded
                var uploads = Path.Combine(webRootPath, "images");
                var extension = Path.GetExtension(files[0].FileName);

                using (var fileStream =
                    new FileStream(Path.Combine(uploads, MenuItemFormViewModel.MenuItem.Id + extension), FileMode.Create))
                {
                    files[0].CopyTo(fileStream);
                };
                menuItemFromDb.Image = $@"\images\{MenuItemFormViewModel.MenuItem.Id}{extension}";
            }
            else
            {
                // no file, use default
                var uploads = Path.Combine(webRootPath, $@"images\{SD.DefaultFoodImage}");
                System.IO.File.Copy(uploads, $@"{webRootPath}\images\{MenuItemFormViewModel.MenuItem.Id}.png");
                menuItemFromDb.Image = $@"\images\{MenuItemFormViewModel.MenuItem.Id}.png";
            }

            await _context.SaveChangesAsync();

            return RedirectToAction(nameof(Index));
        }

        [HttpGet]
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null) return NotFound();

            MenuItemFormViewModel.MenuItem = await _context.MenuItems.Include(m => m.Category).Include(m => m.SubCategory)
                .SingleOrDefaultAsync(m => m.Id == id);
            MenuItemFormViewModel.SubCategories =
                await _context.SubCategories.Where(s => s.CategoryId == MenuItemFormViewModel.MenuItem.CategoryId).ToListAsync();

            if (MenuItemFormViewModel.MenuItem == null) return NotFound();

            return View(MenuItemFormViewModel);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [ActionName("Edit")]
        public async Task<IActionResult> EditPost()
        {
            MenuItemFormViewModel.MenuItem.SubCategoryId = Convert.ToInt32(Request.Form["SubCategoryId"].ToString());

            if (!ModelState.IsValid)
            {
                MenuItemFormViewModel.SubCategories =
                    await _context.SubCategories.Where(s => s.CategoryId == MenuItemFormViewModel.MenuItem.CategoryId).ToListAsync();
                return View("Edit", MenuItemFormViewModel);
            }

            // Work on the image saving section
            string webRootPath = _hostingEnvironment.WebRootPath;

            var files = HttpContext.Request.Form.Files;

            var menuItemFromDb = await _context.MenuItems.FindAsync(MenuItemFormViewModel.MenuItem.Id);

            if (files.Count > 0)
            {
                // new files has been uploaded
                var uploads = Path.Combine(webRootPath, "images");
                var newExtension = Path.GetExtension(files[0].FileName);
                
                
                //delete the original file
                var imagePath = Path.Combine(webRootPath, menuItemFromDb.Image.TrimStart('\\'));

                if (System.IO.File.Exists(imagePath)) System.IO.File.Delete(imagePath);

                //upload the new file
                using (var fileStream =
                    new FileStream(Path.Combine(uploads, MenuItemFormViewModel.MenuItem.Id + newExtension), FileMode.Create))
                {
                    files[0].CopyTo(fileStream);
                };
                menuItemFromDb.Image = $@"\images\{MenuItemFormViewModel.MenuItem.Id}{newExtension}";
            }

            menuItemFromDb.Name = MenuItemFormViewModel.MenuItem.Name;
            menuItemFromDb.Description = MenuItemFormViewModel.MenuItem.Description;
            menuItemFromDb.Price = MenuItemFormViewModel.MenuItem.Price;
            menuItemFromDb.Spicyness = MenuItemFormViewModel.MenuItem.Spicyness;
            menuItemFromDb.CategoryId = MenuItemFormViewModel.MenuItem.CategoryId;
            menuItemFromDb.SubCategoryId = MenuItemFormViewModel.MenuItem.SubCategoryId;

            await _context.SaveChangesAsync();

            return RedirectToAction(nameof(Index));
        }

        [HttpGet]
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null) return NotFound();

            MenuItemFormViewModel.MenuItem = await _context.MenuItems.Include(m => m.Category).Include(m => m.SubCategory)
                .SingleOrDefaultAsync(m => m.Id == id);
            MenuItemFormViewModel.SubCategories =
                await _context.SubCategories.Where(s => s.CategoryId == MenuItemFormViewModel.MenuItem.CategoryId).ToListAsync();

            if (MenuItemFormViewModel.MenuItem == null) return NotFound();

            return View(MenuItemFormViewModel);
        }

        [HttpGet]
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null) return NotFound();

            MenuItemFormViewModel.MenuItem = await _context.MenuItems.Include(m => m.Category).Include(m => m.SubCategory)
                .SingleOrDefaultAsync(m => m.Id == id);
            MenuItemFormViewModel.SubCategories =
                await _context.SubCategories.Where(s => s.CategoryId == MenuItemFormViewModel.MenuItem.CategoryId).ToListAsync();

            if (MenuItemFormViewModel.MenuItem == null) return NotFound();

            return View(MenuItemFormViewModel);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [ActionName("Delete")]
        public async Task<IActionResult> DeletePost(int? id)
        {
            // Work on the image deleting section
            var webRootPath = _hostingEnvironment.WebRootPath;
            
            var menuItemFromDb = await _context.MenuItems.FindAsync(id);

            if (menuItemFromDb == null) return NotFound();

            //delete the original file
            var imagePath = Path.Combine(webRootPath, menuItemFromDb.Image.TrimStart('\\'));
            if (System.IO.File.Exists(imagePath)) System.IO.File.Delete(imagePath);

            // delete the menu item

            _context.MenuItems.Remove(menuItemFromDb);

            await _context.SaveChangesAsync();

            return RedirectToAction(nameof(Index));
        }
    }
}