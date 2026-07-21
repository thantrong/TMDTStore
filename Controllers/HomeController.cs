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
        var categories = await _context.Categories
            .AsNoTracking()
            .Where(c => c.ParentId == null)
            .Include(c => c.InverseParent)
            .OrderBy(c => c.Name)
            .Take(6)
            .ToListAsync();

        var brands = await _context.Brands
            .AsNoTracking()
            .Where(b => b.IsActive == true)
            .OrderBy(b => b.Name)
            .ToListAsync();

        var allProducts = await _context.Products
            .AsNoTracking()
            .AsSplitQuery()
            .Include(p => p.Brand)
            .Include(p => p.ProductBadges)
            .Include(p => p.ProductVariants)
            .Where(p => p.IsActive == true)
            .ToListAsync();

        // 1 query nhẹ tính doanh số thực cho BestSellers
        var salesData = await _context.OrderItems
            .AsNoTracking()
            .GroupBy(oi => oi.ProductId)
            .Select(g => new { ProductId = g.Key, TotalSold = g.Sum(oi => oi.Quantity) })
            .ToListAsync();

        var salesDict = salesData.ToDictionary(x => x.ProductId, x => x.TotalSold);

        var model = new HomeViewModel
        {
            Categories = categories,
            Brands = brands,

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
                .OrderByDescending(p => salesDict.GetValueOrDefault(p.Id, 0))
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
