using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
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
    public class SubCategoriesController : Controller
    {
        private readonly ApplicationDbContext _context;

        [TempData] public string StatusMessage { get; set; }

        public SubCategoriesController(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<IActionResult> Index()
        {
            var subCategories = await _context.SubCategories.Include(sc => sc.Category).ToListAsync();

            return View(subCategories);
        }

        [HttpGet]
        public async Task<IActionResult> Create()
        {

            var viewModel = new SubCategoryFormViewModel
            {
                CategoryList = await _context.Categories.ToListAsync(),
                SubCategory = new SubCategory(),
                SubCategoryList =
                    await _context.SubCategories.OrderBy(sc => sc.Name).Select(sc => sc.Name).ToListAsync()
            };

            return View(viewModel);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(SubCategoryFormViewModel formViewModel)
        {
            if (ModelState.IsValid)
            {
                var subCategoriesThatExist = _context.SubCategories.Include(s => s.Category).Where(s =>
                    s.Name.ToLower() == formViewModel.SubCategory.Name.ToLower() && s.CategoryId == formViewModel.SubCategory.CategoryId);

                if (subCategoriesThatExist.Any())
                {
                    StatusMessage = $"Error : Sub Category with the same name exists in Category - {subCategoriesThatExist.First().Category.Name}.";
                }
                else
                {
                    _context.SubCategories.Add(formViewModel.SubCategory);
                    await _context.SaveChangesAsync();

                    return RedirectToAction(nameof(Index));
                }
            }

            var pageVm = new SubCategoryFormViewModel
            {
                CategoryList = await _context.Categories.ToListAsync(),
                StatusMessage =  StatusMessage,
                SubCategory = formViewModel.SubCategory,
                SubCategoryList =
                    await _context.SubCategories.OrderBy(sc => sc.Name).Select(sc => sc.Name).ToListAsync()
            };
            return View(pageVm);
        }


        [Route("Admin/SubCategories/Categories/{id}")]
        public async Task<IActionResult> GetSubCategory(int id)
        {
            var subCategories = await _context.SubCategories.Where(s => s.CategoryId == id).ToListAsync();

            return Json(new SelectList(subCategories, "Id", "Name"));
        }

        [HttpGet]
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null) return NotFound();

            var subCategoryFromDb = await _context.SubCategories.FindAsync(id);

            var viewModel = new SubCategoryFormViewModel
            {
                CategoryList = await _context.Categories.ToListAsync(),
                SubCategory = subCategoryFromDb,
                SubCategoryList =
                    await _context.SubCategories.OrderBy(sc => sc.Name).Select(sc => sc.Name).ToListAsync()
            };

            return View(viewModel);
        }


        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(SubCategoryFormViewModel formViewModel)
        {
            if (ModelState.IsValid)
            {
                var subCategoriesThatExist = _context.SubCategories.Include(s => s.Category).Where(s =>
                    s.Name.ToLower() == formViewModel.SubCategory.Name.ToLower() && s.CategoryId == formViewModel.SubCategory.CategoryId);

                if (subCategoriesThatExist.Any())
                {
                    StatusMessage = $"Error : Sub Category with the same name exists in Category - {subCategoriesThatExist.First().Category.Name}.";
                }
                else
                {
                    var subCategoryFromDb = await _context.SubCategories.FindAsync(formViewModel.SubCategory.Id);
                    subCategoryFromDb.Name = formViewModel.SubCategory.Name;
                    await _context.SaveChangesAsync();

                    return RedirectToAction(nameof(Index));
                }
            }

            var pageVm = new SubCategoryFormViewModel
            {
                CategoryList = await _context.Categories.ToListAsync(),
                StatusMessage = StatusMessage,
                SubCategory = formViewModel.SubCategory,
                SubCategoryList =
                    await _context.SubCategories.OrderBy(sc => sc.Name).Select(sc => sc.Name).ToListAsync()
            };
            return View(pageVm);
        }

        [HttpGet]
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null) return NotFound();

            var subCategoryFromDb = await _context.SubCategories.FindAsync(id);

            var viewModel = new SubCategoryFormViewModel
            {
                CategoryList = await _context.Categories.ToListAsync(),
                SubCategory = subCategoryFromDb,
                SubCategoryList =
                    await _context.SubCategories.OrderBy(sc => sc.Name).Select(sc => sc.Name).ToListAsync()
            };

            return View(viewModel);
        }

        [HttpGet]
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null) return NotFound();

            var subCategoryFromDb = await _context.SubCategories.FindAsync(id);

            var viewModel = new SubCategoryFormViewModel
            {
                CategoryList = await _context.Categories.ToListAsync(),
                SubCategory = subCategoryFromDb,
                SubCategoryList =
                    await _context.SubCategories.OrderBy(sc => sc.Name).Select(sc => sc.Name).ToListAsync()
            };

            return View(viewModel);
        }

        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int? id)
        {

            var subCategoryFromDb = await _context.SubCategories.FindAsync(id);
            if (subCategoryFromDb == null) return NotFound();

            _context.SubCategories.Remove(subCategoryFromDb);
            await _context.SaveChangesAsync();

            return RedirectToAction(nameof(Index));
        }
    }
}