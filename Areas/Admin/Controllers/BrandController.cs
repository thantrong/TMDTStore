namespace TMDTStore.Areas.Admin.Controllers;

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using TMDTStore.Models;

[Area("Admin")]
[Authorize(Roles = "Admin")]
public class BrandController : Controller
{
    private readonly StoreDbContext _context;

    public BrandController(StoreDbContext context)
    {
        _context = context;
    }

    // GET: /Admin/Brand
    [HttpGet]
    public async Task<IActionResult> Index()
    {
        var brands = await _context.Brands
            .OrderBy(b => b.Name)
            .ToListAsync();
        return View(brands);
    }

    // GET: /Admin/Brand/Create
    [HttpGet]
    public IActionResult Create()
    {
        return View();
    }

    // POST: /Admin/Brand/Create
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(Brand model)
    {
        ModelState.Remove("Id");
        ModelState.Remove("Slug");

        if (!ModelState.IsValid)
        {
            return View(model);
        }

        model.Slug = model.Name.ToLower().Replace(" ", "-");
        model.CreatedAt = DateTime.UtcNow;

        _context.Brands.Add(model);
        await _context.SaveChangesAsync();

        TempData["ToastType"] = "success";
        TempData["ToastMessage"] = "Thương hiệu đã được tạo.";
        return RedirectToAction("Index");
    }

    // GET: /Admin/Brand/Edit/{id}
    [HttpGet]
    public async Task<IActionResult> Edit(string id)
    {
        var brand = await _context.Brands.FindAsync(id);
        if (brand == null) return NotFound();
        return View(brand);
    }

    // POST: /Admin/Brand/Edit/{id}
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(string id, Brand model)
    {
        ModelState.Remove("Slug");

        if (!ModelState.IsValid)
        {
            return View(model);
        }

        var brand = await _context.Brands.FindAsync(id);
        if (brand == null) return NotFound();

        brand.Name = model.Name;
        brand.Slug = model.Name.ToLower().Replace(" ", "-");
        brand.LogoUrl = model.LogoUrl;
        brand.Description = model.Description;
        brand.IsActive = model.IsActive;

        await _context.SaveChangesAsync();

        TempData["ToastType"] = "success";
        TempData["ToastMessage"] = "Thương hiệu đã được cập nhật.";
        return RedirectToAction("Index");
    }
}
