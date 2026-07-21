namespace TMDTStore.Areas.Admin.Controllers;

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using TMDTStore.Models;

[Area("Admin")]
[Authorize(Roles = "Admin")]
public class VoucherController : Controller
{
    private readonly StoreDbContext _context;

    public VoucherController(StoreDbContext context)
    {
        _context = context;
    }

    [HttpGet]
    public async Task<IActionResult> Index(string? search, int page = 1)
    {
        var query = _context.Vouchers.AsQueryable();

        if (!string.IsNullOrEmpty(search))
        {
            var kw = search.ToLower();
            query = query.Where(v => v.Code.ToLower().Contains(kw));
        }

        var totalItems = await query.CountAsync();
        var pageSize = 15;
        var vouchers = await query
            .OrderByDescending(v => v.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();

        ViewBag.TotalItems = totalItems;
        ViewBag.TotalPages = (int)Math.Ceiling((double)totalItems / pageSize);
        ViewBag.CurrentPage = page;
        ViewBag.Search = search;

        return View(vouchers);
    }

    [HttpGet]
    public IActionResult Create()
    {
        return View(new Voucher
        {
            StartDate = DateTime.Now,
            EndDate = DateTime.Now.AddMonths(1),
            DiscountType = "percentage"
        });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(Voucher model)
    {
        ModelState.Remove("Id");
        ModelState.Remove("UsedCount");
        ModelState.Remove("Orders");

        if (!ModelState.IsValid)
        {
            return View(model);
        }

        var exists = await _context.Vouchers.AnyAsync(v => v.Code == model.Code);
        if (exists)
        {
            ModelState.AddModelError("Code", "Mã giảm giá này đã tồn tại.");
            return View(model);
        }

        model.UsedCount = 0;
        model.IsActive = true;
        model.CreatedAt = DateTime.UtcNow;

        _context.Vouchers.Add(model);
        await _context.SaveChangesAsync();

        TempData["ToastType"] = "success";
        TempData["ToastMessage"] = $"Đã tạo mã giảm giá {model.Code} thành công!";
        return RedirectToAction("Index");
    }

    // GET: /Admin/Voucher/Edit/{id}
    public async Task<IActionResult> Edit(string id)
    {
        var voucher = await _context.Vouchers.FindAsync(id);
        if (voucher == null) return NotFound();
        return View(voucher);
    }

    // POST: /Admin/Voucher/Edit/{id}
    [HttpPost]
    public async Task<IActionResult> Edit(string id, Voucher model)
    {
        if (id != model.Id) return NotFound();
        ModelState.Remove("Orders");

        if (!ModelState.IsValid) return View(model);

        var voucher = await _context.Vouchers.FindAsync(id);
        if (voucher == null) return NotFound();

        var exists = await _context.Vouchers.AnyAsync(v => v.Code == model.Code && v.Id != id);
        if (exists)
        {
            ModelState.AddModelError("Code", "Mã giảm giá này đã tồn tại.");
            return View(model);
        }

        voucher.Code = model.Code;
        voucher.DiscountType = model.DiscountType;
        voucher.DiscountValue = model.DiscountValue;
        voucher.MinOrderValue = model.MinOrderValue;
        voucher.MaxDiscountAmount = model.MaxDiscountAmount;
        voucher.StartDate = model.StartDate;
        voucher.EndDate = model.EndDate;
        voucher.UsageLimit = model.UsageLimit;

        await _context.SaveChangesAsync();

        TempData["ToastType"] = "success";
        TempData["ToastMessage"] = "Cập nhật mã giảm giá thành công!";
        return RedirectToAction("Index");
    }

    // POST: /Admin/Voucher/ToggleStatus/{id}
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ToggleStatus(string id)
    {
        var voucher = await _context.Vouchers.FindAsync(id);
        if (voucher == null) return NotFound();

        voucher.IsActive = !(voucher.IsActive ?? true);
        await _context.SaveChangesAsync();

        TempData["ToastType"] = "info";
        TempData["ToastMessage"] = $"Đã {(voucher.IsActive == true ? "kích hoạt" : "vô hiệu")} mã {voucher.Code}.";
        return RedirectToAction("Index");
    }
}
