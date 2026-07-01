namespace TMDTStore.Areas.Admin.Controllers;

using Microsoft.AspNetCore.Mvc;
using TMDTStore.Models;
using TMDTStore.Models.ViewModels.Product;
using TMDTStore.Services.Cloudinary;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Authorization;

[Area("Admin")]
public class ProductController : Controller
{
    private readonly ICloudinaryService _cloudinaryService;
    private readonly StoreDbContext _context;

    public ProductController(ICloudinaryService cloudinaryService, StoreDbContext context)
    {
        _cloudinaryService = cloudinaryService;
        _context = context;
    }

    // GET: /Admin/Product
    [HttpGet]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> Index(ProductListViewModels model)
    {
        model.Categories = await _context.Categories.ToListAsync();

        var query = _context.Products
        .Include(p => p.Category)
        .Include(p => p.Brand)
        .Include(p => p.Inventory)
        .Include(p => p.ProductBadges)
        .Where(p => p.IsActive == true)
        .AsQueryable();

        // Filter theo CategoryId
        if (!string.IsNullOrEmpty(model.CategoryId))
        {
            query = query.Where(p => p.CategoryId == model.CategoryId);
        }
        if (!string.IsNullOrEmpty(model.SearchQuery))
        {
            query = query.Where(p => p.Name.Contains(model.SearchQuery));
        }
        if (string.IsNullOrEmpty(model.BrandName) == false)
        {
            query = query.Where(p => p.BrandName.Contains(model.BrandName));
        }
        if (model.MinPrice.HasValue)
        {
            query = query.Where(p => p.Price >= model.MinPrice.Value);
        }
        if (model.MaxPrice.HasValue)
        {
            query = query.Where(p => p.Price <= model.MaxPrice.Value);
        }
        model.Products = await query.ToListAsync();
        return View(model);
    }

    // GET: /Admin/Product/Create
    [HttpGet]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> Create()
    {
        var model = new ProductCreatViewModels
        {
            Categories = await _context.Categories.ToListAsync(),
            Brands = await _context.Brands.ToListAsync(),
            ProductBadges = await _context.ProductBadges.ToListAsync()
        };
        return View(model);
    }

    // POST: /Admin/Product/Create
    [HttpPost]
    [Authorize(Roles = "Admin")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(ProductCreatViewModels model)
    {
        if (!ModelState.IsValid)
        {
            model.Categories = await _context.Categories.ToListAsync();
            model.Brands = await _context.Brands.ToListAsync();
            model.ProductBadges = await _context.ProductBadges.ToListAsync();
            return View(model);
        }

        var product = new Product
        {
            Name = model.Name,
            Slug = model.Name.ToLower().Replace(" ", "-").Replace("đ", "d").Replace("--", "-").Trim('-'),
            ShortDescription = model.ShortDescription,
            Description = model.Description,
            TechnicalSpecs = model.TechnicalSpecs,
            Price = model.Price,
            ListPrice = model.ListPrice,
            SalePrice = model.SalePrice,
            BrandId = model.BrandId,
            CategoryId = model.CategoryId,
            WarrantyMonths = model.WarrantyMonths,
            ReturnDays = model.ReturnDays,
            CreatedAt = DateTime.UtcNow,
            IsActive = true
        };

        // Tạo Inventory
        if (model.Quantity > 0)
        {
            product.Inventory = new Inventory
            {
                StockQuantity = model.Quantity,
                UpdatedAt = DateTime.UtcNow
            };
        }

        // Upload hình ảnh nếu có
        if (model.ImageFile != null && model.ImageFile.Count > 0)
        {
            var imageUrls = new List<string>();
            foreach (var image in model.ImageFile)
            {
                var imageUrl = await _cloudinaryService.UploadImageAsync(image, "products");
                if (!string.IsNullOrEmpty(imageUrl))
                {
                    imageUrls.Add(imageUrl);
                }
            }
            product.ImageUrls = System.Text.Json.JsonSerializer.Serialize(imageUrls);
            product.ImageUrl = imageUrls.FirstOrDefault();
        }

        // Thêm ProductBadges nếu có
        if (model.SelectedBadgeIds != null && model.SelectedBadgeIds.Count > 0)
        {
            foreach (var badgeLabel in model.SelectedBadgeIds)
            {
                var badge = await _context.ProductBadges
                    .FirstOrDefaultAsync(b => b.Label == badgeLabel);
                if (badge != null)
                {
                    product.ProductBadges.Add(badge);
                }
            }
        }

        _context.Products.Add(product);
        await _context.SaveChangesAsync();

        TempData["ToastType"] = "success";
        TempData["ToastMessage"] = "Sản phẩm đã được tạo thành công.";
        return RedirectToAction("Index");
    }

    // GET: /Admin/Product/Details/{id}
    [HttpGet]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> Details(string id)
    {
        var product = await _context.Products
           .Include(p => p.Category)
           .Include(p => p.Brand)
           .Include(p => p.Inventory)
           .Include(p => p.ProductBadges)
           .Include(p => p.Reviews)
           .FirstOrDefaultAsync(p => p.Id == id && p.IsActive == true);

        if (product == null)
        {
            return NotFound();
        }
        return View(product);
    }

    // GET: /Admin/Product/Edit/{id}
    [HttpGet]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> Edit(string id)
    {
        var product = await _context.Products
            .Include(p => p.ProductBadges)
            .FirstOrDefaultAsync(p => p.Id == id && p.IsActive == true);

        if (product == null)
        {
            return NotFound();
        }

        var model = new ProductEditViewModels
        {
            Id = product.Id,
            Name = product.Name,
            Description = product.Description,
            TechnicalSpecs = product.TechnicalSpecs,
            Price = product.Price,
            BrandId = product.BrandId,
            CategoryId = product.CategoryId,
            ExistingImageUrls = product.ImageUrls,
            Categories = await _context.Categories.ToListAsync(),
            Brands = await _context.Brands.ToListAsync(),
            AvailableBadges = await _context.ProductBadges.ToListAsync(),
            SelectedBadgeIds = product.ProductBadges.Select(b => b.Label).ToList()
        };

        ViewBag.TechnicalSpecsJson = product.TechnicalSpecs ?? "[]";

        return View(model);
    }

    // POST: /Admin/Product/Edit/{id}
    [HttpPost]
    [Authorize(Roles = "Admin")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(string id, ProductEditViewModels model)
    {
        if (!ModelState.IsValid)
        {
            model.Categories = await _context.Categories.ToListAsync();
            model.Brands = await _context.Brands.ToListAsync();
            model.AvailableBadges = await _context.ProductBadges.ToListAsync();
            return View(model);
        }

        var product = await _context.Products
            .Include(p => p.ProductBadges)
            .FirstOrDefaultAsync(p => p.Id == id);

        if (product == null) return NotFound();

        product.Name = model.Name;
        product.Slug = model.Name.ToLower().Replace(" ", "-").Replace("đ", "d").Replace("--", "-").Trim('-');
        product.Price = model.Price;
        product.ShortDescription = model.ShortDescription;
        product.Description = model.Description;
        product.TechnicalSpecs = model.TechnicalSpecs;
        product.ListPrice = model.ListPrice;
        product.SalePrice = model.SalePrice;
        product.WarrantyMonths = model.WarrantyMonths;
        product.ReturnDays = model.ReturnDays;
        product.BrandId = model.BrandId;
        product.CategoryId = model.CategoryId;

        // Cập nhật tồn kho
        var inventory = await _context.Inventories.FirstOrDefaultAsync(i => i.ProductId == id);
        if (inventory != null)
        {
            inventory.StockQuantity = model.InventoryQuantity;
            inventory.UpdatedAt = DateTime.UtcNow;
        }

        // Upload ảnh mới nếu có
        if (model.ImageFiles != null && model.ImageFiles.Count > 0)
        {
            var imageUrls = new List<string>();
            foreach (var image in model.ImageFiles)
            {
                try
                {
                    var imageUrl = await _cloudinaryService.UploadImageAsync(image, "products");
                    if (!string.IsNullOrEmpty(imageUrl))
                    {
                        imageUrls.Add(imageUrl);
                    }
                }
                catch (Exception)
                {
                    TempData["ToastType"] = "error";
                    TempData["ToastMessage"] = "Không thể tải ảnh lên.";
                    return View(model);
                }
            }
            // Lưu danh sách ảnh dưới dạng JSON
            product.ImageUrls = System.Text.Json.JsonSerializer.Serialize(imageUrls);
            // Ảnh đầu tiên làm ảnh đại diện
            product.ImageUrl = imageUrls.FirstOrDefault();
        }

        // Cập nhật badges
        product.ProductBadges.Clear();
        if (model.SelectedBadgeIds != null)
        {
            foreach (var badgeLabel in model.SelectedBadgeIds)
            {
                var badge = await _context.ProductBadges
                    .FirstOrDefaultAsync(b => b.Label == badgeLabel);
                if (badge != null) product.ProductBadges.Add(badge);
            }
        }

        await _context.SaveChangesAsync();

        TempData["ToastType"] = "success";
        TempData["ToastMessage"] = "Sản phẩm đã được cập nhật.";
        return RedirectToAction("Index");
    }

    // POST: /Admin/Product/ToggleStatus/{id}
    [HttpPost]
    [Authorize(Roles = "Admin")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ToggleStatus(string id)
    {
        var product = await _context.Products.FindAsync(id);
        if (product == null) return NotFound();

        product.IsActive = !product.IsActive;
        await _context.SaveChangesAsync();

        TempData["ToastType"] = "success";
        TempData["ToastMessage"] = product.IsActive == true
            ? "Sản phẩm đã được hiển thị."
            : "Sản phẩm đã được ẩn.";
        return RedirectToAction("Index");
    }
}
