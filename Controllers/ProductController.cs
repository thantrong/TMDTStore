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
        model.Categories = await _context.Categories
            .Include(c => c.Parent)
            .Include(c => c.InverseParent)
            .ToListAsync();

        // Build flat tree for category navigation
        var flatTree = new List<(TMDTStore.Models.Category Cat, int Level)>();
        void AddNode(List<TMDTStore.Models.Category> source, string? parentId, int level)
        {
            var children = source.Where(c => c.ParentId == parentId).OrderBy(c => c.Name);
            foreach (var cat in children)
            {
                flatTree.Add((cat, level));
                AddNode(source, cat.Id, level + 1);
            }
        }
        AddNode(model.Categories, null, 0);
        ViewBag.CategoryTree = flatTree;

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
            // Lấy tất cả ID danh mục con (đệ quy)
            var categoryIds = new List<string> { model.CategoryId };
            void GetChildIds(string parentId)
            {
                var children = model.Categories.Where(c => c.ParentId == parentId).ToList();
                foreach (var child in children)
                {
                    categoryIds.Add(child.Id);
                    GetChildIds(child.Id);
                }
            }
            GetChildIds(model.CategoryId);

            query = query.Where(p => p.CategoryId != null && categoryIds.Contains(p.CategoryId));
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
            query = query.Where(p => p.Price.HasValue && p.Price.Value >= model.MinPrice.Value);
        }
        if (model.MaxPrice.HasValue)
        {
            query = query.Where(p => p.Price.HasValue && p.Price.Value <= model.MaxPrice.Value);
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
            .Include(p => p.ProductVariants.Where(v => v.IsActive).OrderBy(v => v.Price))
            .FirstOrDefaultAsync(p => p.Id == id && p.IsActive == true);

        if (product == null)
        {
            return NotFound();
        }
        return View(product);
    }

}
