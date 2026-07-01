namespace TMDTStore.Controllers;

using Microsoft.AspNetCore.Mvc;
using TMDTStore.Models;
using TMDTStore.Models.ViewModels.Product;
using TMDTStore.Services.Cloudinary;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;

public class ProductController : Controller
{
    private readonly ICloudinaryService _cloudinaryService;
    private readonly StoreDbContext _context;

    public ProductController(ICloudinaryService cloudinaryService, StoreDbContext context)
    {
        _cloudinaryService = cloudinaryService;
        _context = context;
    }

    // GET: /Product
    [HttpGet]
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

        query = model.SortBy switch
        {
            "price_asc" => query.OrderBy(p => p.Price),
            "price_desc" => query.OrderByDescending(p => p.Price),
            "newest" => query.OrderByDescending(p => p.CreatedAt),
            "rating" => query.OrderByDescending(p => p.RatingAvg),
            _ => query.OrderBy(p => p.Name)
        };

        model.TotalItems = await query.CountAsync();
        model.Products = await query.Skip((model.Page - 1) * model.PageSize).Take(model.PageSize).ToListAsync();

        return View(model);
    }

    // GET: /Product/Details/{id}
    [HttpGet]
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

}
