namespace TMDTStore.Areas.Admin.Controllers;

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using TMDTStore.Models;
using TMDTStore.Models.ViewModels.Category;
using System.Globalization;
using System.Text;
[Area("Admin")]
[Authorize(Roles = "Admin")]
public class CategoryController : Controller
{
    private readonly StoreDbContext _context;

    public CategoryController(StoreDbContext context)
    {
        _context = context;
    }

    // GET: /Admin/Category
    [HttpGet]
    public async Task<IActionResult> Index()
    {
        var categories = await _context.Categories
            .Include(c => c.Parent)
            .OrderBy(c => c.Name)
            .ToListAsync();
        return View(categories);
    }

    // GET: /Admin/Category/Create
    [HttpGet]
    public async Task<IActionResult> Create()
    {
        ViewBag.ParentCategories = await _context.Categories
            .OrderBy(c => c.Name)
            .ToListAsync();
        return View();
    }
    private string GenerateSlug(string name)
    {
        // Chuẩn hoá Unicode → tách dấu khỏi chữ
        var normalized = name.Normalize(NormalizationForm.FormD);
        var chars = normalized
            .Where(c => char.GetUnicodeCategory(c) != System.Globalization.UnicodeCategory.NonSpacingMark)
            .ToArray();
        var slug = new string(chars).Normalize(NormalizationForm.FormC);

        // Chuyển về chữ thường, thay khoảng trắng bằng dấu gạch ngang
        return slug.ToLower()
            .Replace("đ", "d")
            .Replace(" ", "-")
            .Replace("--", "-")
            .Trim('-');
    }
    // POST: /Admin/Category/Create
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(CategoryCreateViewModels model)
    {
        if (!ModelState.IsValid)
        {
            TempData["ToastType"] = "error";
            TempData["ToastMessage"] = "Vui lòng kiểm tra lại thông tin nhập vào";
            ViewBag.ParentCategories = await _context.Categories.OrderBy(c => c.Name).ToListAsync();
            return View(model);
        }
        model.Slug = GenerateSlug(model.Name);
        model.CreatedAt = DateTime.UtcNow;

        var category = new Category
        {
            Name = model.Name,
            Slug = model.Slug,
            ParentId = model.ParentId,
            CreatedAt = model.CreatedAt
        };

        _context.Categories.Add(category);
        await _context.SaveChangesAsync();

        TempData["ToastType"] = "success";
        TempData["ToastMessage"] = "Danh mục đã được tạo.";
        return RedirectToAction("Index");
    }

    // GET: /Admin/Category/Edit/{id}
    [HttpGet]
    public async Task<IActionResult> Edit(string id)
    {
        var category = await _context.Categories.FindAsync(id);
        if (category == null) return NotFound();

        ViewBag.ParentCategories = await _context.Categories
            .Where(c => c.Id != id)
            .OrderBy(c => c.Name)
            .ToListAsync();

        return View(category);
    }

    // POST: /Admin/Category/Edit/{id}
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(string id, Category model)
    {
        if (!ModelState.IsValid)
        {
            ViewBag.ParentCategories = await _context.Categories
                .Where(c => c.Id != id)
                .OrderBy(c => c.Name)
                .ToListAsync();
            return View(model);
        }

        var category = await _context.Categories.FindAsync(id);
        if (category == null) return NotFound();

        category.Name = model.Name;
        category.Slug = model.Name.ToLower().Replace(" ", "-");
        category.ParentId = model.ParentId;

        await _context.SaveChangesAsync();

        TempData["ToastType"] = "success";
        TempData["ToastMessage"] = "Danh mục đã được cập nhật.";
        return RedirectToAction("Index");
    }

    // POST: /Admin/Category/Delete/{id}
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete(string id)
    {
        var category = await _context.Categories
            .Include(c => c.Products)
            .Include(c => c.InverseParent)
            .FirstOrDefaultAsync(c => c.Id == id);

        if (category == null) return NotFound();

        if (category.Products.Count > 0 || category.InverseParent.Count > 0)
        {
            TempData["ToastType"] = "error";
            TempData["ToastMessage"] = "Không thể xoá danh mục này vì còn sản phẩm hoặc danh mục con.";
            return RedirectToAction("Index");
        }

        _context.Categories.Remove(category);
        await _context.SaveChangesAsync();

        TempData["ToastType"] = "success";
        TempData["ToastMessage"] = "Danh mục đã được xoá.";
        return RedirectToAction("Index");
    }
}

