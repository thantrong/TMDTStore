using System.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using TMDTStore.Models;
using TMDTStore.ViewModels;

namespace TMDTStore.Controllers;

public class HomeController : Controller
{
    private readonly ILogger<HomeController> _logger;
    private readonly StoreDbContext _context;

    public HomeController(ILogger<HomeController> logger, StoreDbContext context)
    {
        _logger = logger;
        _context = context;
    }

    public async Task<IActionResult> Index()
    {
        var model = new HomeViewModel
        {
            // Danh mục
            Categories = await _context.Categories
                .Where(c => c.ParentId == null)
                .Include(c => c.InverseParent)
                .OrderBy(c => c.Name)
                .Take(6)
                .ToListAsync(),

            // Thương hiệu
            Brands = await _context.Brands
                .Where(b => b.IsActive == true)
                .OrderBy(b => b.Name)
                .ToListAsync(),

            // Sản phẩm nổi bật (đánh giá cao)
            FeaturedProducts = await _context.Products
                .Include(p => p.Category)
                .Include(p => p.Brand)
                .Include(p => p.ProductBadges)
                .Include(p => p.ProductVariants)
                .Where(p => p.IsActive == true)
                .OrderByDescending(p => p.RatingAvg)
                .Take(8)
                .ToListAsync(),

            // Sản phẩm mới nhất
            NewProducts = await _context.Products
                .Include(p => p.Category)
                .Include(p => p.Brand)
                .Include(p => p.ProductBadges)
                .Include(p => p.ProductVariants)
                .Where(p => p.IsActive == true)
                .OrderByDescending(p => p.CreatedAt)
                .Take(8)
                .ToListAsync(),

            // Bán chạy (RatingCount như proxy)
            BestSellers = await _context.Products
                .Include(p => p.Category)
                .Include(p => p.Brand)
                .Include(p => p.ProductBadges)
                .Include(p => p.ProductVariants)
                .Where(p => p.IsActive == true)
                .OrderByDescending(p => p.RatingCount)
                .Take(8)
                .ToListAsync(),
        };

        return View(model);
    }

    public IActionResult Privacy()
    {
        return View();
    }

    [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
    public IActionResult Error()
    {
        return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
    }
}
