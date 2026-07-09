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

    [ResponseCache(Duration = 120, Location = ResponseCacheLocation.Client)]
    public async Task<IActionResult> Index()
    {
        // Chạy song song các query độc lập
        var categoriesTask = _context.Categories
            .Where(c => c.ParentId == null)
            .Include(c => c.InverseParent)
            .OrderBy(c => c.Name)
            .Take(6)
            .ToListAsync();

        var brandsTask = _context.Brands
            .Where(b => b.IsActive == true)
            .OrderBy(b => b.Name)
            .ToListAsync();

        // 1 query duy nhất lấy sản phẩm — dùng AsSplitQuery chống Cartesian Explosion
        var allProductsTask = _context.Products
            .AsSplitQuery()
            .Include(p => p.Brand)
            .Include(p => p.ProductBadges)
            .Include(p => p.ProductVariants)
            .Where(p => p.IsActive == true)
            .ToListAsync();

        // 1 query nhẹ tính doanh số thực cho BestSellers
        var salesTask = _context.OrderItems
            .GroupBy(oi => oi.ProductId)
            .Select(g => new { ProductId = g.Key, TotalSold = g.Sum(oi => oi.Quantity) })
            .ToListAsync();

        await Task.WhenAll(categoriesTask, brandsTask, allProductsTask, salesTask);

        var allProducts = allProductsTask.Result;
        var salesData = salesTask.Result
            .ToDictionary(x => x.ProductId, x => x.TotalSold);

        var model = new HomeViewModel
        {
            Categories = categoriesTask.Result,
            Brands = brandsTask.Result,

            // Sản phẩm nổi bật — top 8 theo RatingAvg
            FeaturedProducts = allProducts
                .OrderByDescending(p => p.RatingAvg)
                .Take(8)
                .ToList(),

            // Sản phẩm mới — top 8 theo CreatedAt
            NewProducts = allProducts
                .OrderByDescending(p => p.CreatedAt)
                .Take(8)
                .ToList(),

            // Bán chạy — top 8 theo doanh số thực (từ OrderItems)
            BestSellers = allProducts
                .OrderByDescending(p => salesData.GetValueOrDefault(p.Id, 0))
                .Take(8)
                .ToList(),
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
