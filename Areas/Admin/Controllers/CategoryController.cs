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
    public async Task<IActionResult> Index(string? search)
    {
        var allCategories = await _context.Categories
            .Include(c => c.Products)
            .OrderBy(c => c.Name)
            .ToListAsync();

        // Filter theo search nếu có
        if (!string.IsNullOrEmpty(search))
        {
            var kw = search.ToLower();
            // Tìm category phù hợp + tất cả con cháu của nó
            var matchedIds = allCategories
                .Where(c => c.Name.ToLower().Contains(kw) || c.Slug.ToLower().Contains(kw))
                .Select(c => c.Id)
                .ToHashSet();

            // Thêm tất cả con cháu của các category được match
            var allDescendantIds = new HashSet<string>();
            void GetDescendants(string pid)
            {
                var children = allCategories.Where(c => c.ParentId == pid).ToList();
                foreach (var child in children)
                {
                    allDescendantIds.Add(child.Id);
                    GetDescendants(child.Id);
                }
            }
            foreach (var mid in matchedIds)
                GetDescendants(mid);

            matchedIds.UnionWith(allDescendantIds);

            allCategories = allCategories.Where(c => matchedIds.Contains(c.Id)).ToList();
            ViewBag.SearchResult = true;
        }

        // Build tree: gán level và sắp xếp theo cấp bậc
        var flatTree = new List<(Category Cat, int Level)>();
        void AddNode(List<Category> source, string? parentId, int level)
        {
            var children = source.Where(c => c.ParentId == parentId).OrderBy(c => c.Name);
            foreach (var cat in children)
            {
                flatTree.Add((cat, level));
                AddNode(source, cat.Id, level + 1);
            }
        }
        AddNode(allCategories, null, 0);

        ViewBag.Search = search;
        ViewBag.TotalCategories = allCategories.Count;
        return View(flatTree);
    }

    // Helper: đệ quy lấy tất cả ID con cháu của một category
    private HashSet<string> GetDescendantIds(List<Category> allCategories, string parentId)
    {
        var ids = new HashSet<string>();
        var children = allCategories.Where(c => c.ParentId == parentId).ToList();
        foreach (var child in children)
        {
            ids.Add(child.Id);
            ids.UnionWith(GetDescendantIds(allCategories, child.Id));
        }
        return ids;
    }

    // Helper: breadcrumb cho category (trả về list tên từ gốc đến hiện tại)
    private List<Category> GetBreadcrumb(List<Category> allCategories, string? categoryId)
    {
        var breadcrumb = new List<Category>();
        var current = allCategories.FirstOrDefault(c => c.Id == categoryId);
        while (current != null)
        {
            breadcrumb.Insert(0, current);
            current = allCategories.FirstOrDefault(c => c.Id == current.ParentId);
        }
        return breadcrumb;
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
        var allCategories = await _context.Categories.OrderBy(c => c.Name).ToListAsync();
        var category = allCategories.FirstOrDefault(c => c.Id == id);
        if (category == null) return NotFound();

        // Loại trừ chính nó + tất cả con cháu khỏi danh sách cha
        var excludeIds = GetDescendantIds(allCategories, id);
        excludeIds.Add(id);
        ViewBag.ParentCategories = allCategories.Where(c => !excludeIds.Contains(c.Id)).ToList();

        // Breadcrumb
        ViewBag.Breadcrumb = GetBreadcrumb(allCategories, id);

        var model = new CategoryEditViewModels
        {
            Id = category.Id,
            Name = category.Name,
            ParentId = category.ParentId,
        };

        return View(model);
    }

    // POST: /Admin/Category/Edit/{id}
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(string id, CategoryEditViewModels model)
    {
        if (!ModelState.IsValid)
        {
            ViewBag.ParentCategories = await _context.Categories
                .Where(c => c.Id != id)
                .OrderBy(c => c.Name)
                .ToListAsync();
            TempData["ToastType"] = "error";
            TempData["ToastMessage"] = "Vui lòng kiểm tra lại thông tin nhập vào";
            return View(model);
        }

        var category = await _context.Categories.FindAsync(id);
        if (category == null) return NotFound();

        category.Name = model.Name;
        category.Slug = GenerateSlug(model.Name);
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

