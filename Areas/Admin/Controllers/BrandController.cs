namespace TMDTStore.Areas.Admin.Controllers;

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using TMDTStore.Models;
using TMDTStore.Models.ViewModels.Brand;
using TMDTStore.Services.Cloudinary;

[Area("Admin")]
[Authorize(Roles = "Admin")]
public class BrandController : Controller
{
    private readonly StoreDbContext _context;
    private readonly ICloudinaryService _cloudinaryService;

    public BrandController(StoreDbContext context, ICloudinaryService cloudinaryService)
    {
        _context = context;
        _cloudinaryService = cloudinaryService;
    }

    [HttpGet]
    public async Task<IActionResult> Index(BrandListViewModel model)
    {
        var query = _context.Brands
            .Include(b => b.Products)
            .AsQueryable();

        // Search
        if (!string.IsNullOrEmpty(model.SearchQuery))
        {
            var keyword = model.SearchQuery.ToLower();
            query = query.Where(b => b.Name.ToLower().Contains(keyword));
        }

        // Count
        model.TotalItems = await query.CountAsync();

        // Paginate
        model.Brands = await query
            .OrderBy(b => b.Name)
            .Skip((model.Page - 1) * model.PageSize)
            .Take(model.PageSize)
            .ToListAsync();

        return View(model);
    }

    [HttpGet]
    public IActionResult Create()
    {
        return View();
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(Brand model, IFormFile? LogoFile)
    {
        ModelState.Remove("Id");
        ModelState.Remove("Slug");

        if (!ModelState.IsValid)
        {
            return View(model);
        }

        model.Slug = model.Name.ToLower().Replace(" ", "-");
        model.CreatedAt = DateTime.UtcNow;

        if (LogoFile != null && LogoFile.Length > 0)
        {
            try
            {
                var logoUrl = await _cloudinaryService.UploadImageAsync(LogoFile, "brands");
                if (!string.IsNullOrEmpty(logoUrl))
                {
                    model.LogoUrl = logoUrl;
                }
            }
            catch (Exception)
            {
                TempData["ToastType"] = "error";
                TempData["ToastMessage"] = "Không thể tải logo lên.";
                return View(model);
            }
        }

        _context.Brands.Add(model);
        await _context.SaveChangesAsync();

        TempData["ToastType"] = "success";
        TempData["ToastMessage"] = "Thương hiệu đã được tạo.";
        return RedirectToAction("Index");
    }

    [HttpGet]
    public async Task<IActionResult> Details(string id)
    {
        var brand = await _context.Brands
            .Include(b => b.Products)
            .FirstOrDefaultAsync(b => b.Id == id);
        if (brand == null) return NotFound();
        return View(brand);
    }

    // GET: /Admin/Brand/Edit/{id}
    public async Task<IActionResult> Edit(string id)
    {
        var brand = await _context.Brands.FindAsync(id);
        if (brand == null) return NotFound();
        return View(brand);
    }

    // POST: /Admin/Brand/Delete/{id}
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete(string id)
    {
        var brand = await _context.Brands
            .Include(b => b.Products)
            .FirstOrDefaultAsync(b => b.Id == id);
        if (brand == null) return NotFound();

        if (brand.Products.Count > 0)
        {
            TempData["ToastType"] = "error";
            TempData["ToastMessage"] = "Không thể xoá thương hiệu này vì còn sản phẩm.";
            return RedirectToAction("Index");
        }

        _context.Brands.Remove(brand);
        await _context.SaveChangesAsync();

        TempData["ToastType"] = "success";
        TempData["ToastMessage"] = "Thương hiệu đã được xoá.";
        return RedirectToAction("Index");
    }

    // POST: /Admin/Brand/Edit/{id}
    [HttpPost]
    public async Task<IActionResult> Edit(string id, Brand model, IFormFile? LogoFile)
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
        brand.Description = model.Description;
        brand.IsActive = model.IsActive;

        if (LogoFile != null && LogoFile.Length > 0)
        {
            try
            {
                var logoUrl = await _cloudinaryService.UploadImageAsync(LogoFile, "brands");
                if (!string.IsNullOrEmpty(logoUrl))
                {
                    brand.LogoUrl = logoUrl;
                }
            }
            catch (Exception)
            {
                TempData["ToastType"] = "error";
                TempData["ToastMessage"] = "Không thể tải logo lên.";
                return View(model);
            }
        }

        await _context.SaveChangesAsync();

        TempData["ToastType"] = "success";
        TempData["ToastMessage"] = "Thương hiệu đã được cập nhật.";
        return RedirectToAction("Index");
    }
}
