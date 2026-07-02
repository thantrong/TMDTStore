namespace TMDTStore.Areas.Admin.Controllers;

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using TMDTStore.Models;
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

    // GET: /Admin/Brand
    [HttpGet]
    public async Task<IActionResult> Index()
    {
        var brands = await _context.Brands
            .Include(b => b.Products)
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

        // Upload logo nếu có
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

    // GET: /Admin/Brand/Details/{id}
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

        // Upload logo mới nếu có (giữ logo cũ nếu không upload)
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
