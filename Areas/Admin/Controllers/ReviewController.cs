using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using TMDTStore.Models;

namespace TMDTStore.Areas.Admin.Controllers;

[Area("Admin")]
[Authorize(Roles = "Admin,Staff")]
public class ReviewController : Controller
{
    private readonly StoreDbContext _context;
    private readonly UserManager<User> _userManager;

    public ReviewController(StoreDbContext context, UserManager<User> userManager)
    {
        _context = context;
        _userManager = userManager;
    }

    // GET: /Admin/Review
    [HttpGet]
    public async Task<IActionResult> Index(string? search, int page = 1)
    {
        const int pageSize = 20;

        var query = _context.Reviews
            .Include(r => r.Product)
            .Include(r => r.User)
            .Include(r => r.InverseParent)
            .Where(r => r.ParentId == null) // chỉ lấy review gốc, không lấy reply
            .AsQueryable();

        if (!string.IsNullOrWhiteSpace(search))
        {
            query = query.Where(r =>
                r.Comment.Contains(search) ||
                (r.User != null && r.User.FullName.Contains(search)) ||
                (r.Product != null && r.Product.Name.Contains(search)));
        }

        var totalItems = await query.CountAsync();
        var reviews = await query
            .OrderByDescending(r => r.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();

        ViewBag.TotalPages = (int)Math.Ceiling((double)totalItems / pageSize);
        ViewBag.CurrentPage = page;
        ViewBag.Search = search;

        return View(reviews);
    }

    // POST: /Admin/Review/Reply
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Reply(string reviewId, string comment)
    {
        var user = await _userManager.GetUserAsync(User);
        if (user == null)
            return RedirectToAction("Login", "Auth", new { area = "" });

        if (string.IsNullOrWhiteSpace(comment) || comment.Trim().Length < 1)
        {
            TempData["ToastType"] = "error";
            TempData["ToastMessage"] = "Vui lòng nhập nội dung phản hồi.";
            return RedirectToAction("Index");
        }

        var parentReview = await _context.Reviews.FindAsync(reviewId);
        if (parentReview == null)
        {
            TempData["ToastType"] = "error";
            TempData["ToastMessage"] = "Đánh giá không tồn tại.";
            return RedirectToAction("Index");
        }

        var reply = new Review
        {
            Id = Guid.NewGuid().ToString("N")[..12].ToUpper(),
            ProductId = parentReview.ProductId,
            UserId = user.Id,
            ParentId = reviewId,
            Rating = null,
            Comment = comment.Trim(),
            IsStaffReply = true,
            CreatedAt = DateTime.UtcNow
        };

        _context.Reviews.Add(reply);
        await _context.SaveChangesAsync();

        TempData["ToastType"] = "success";
        TempData["ToastMessage"] = "Đã phản hồi đánh giá thành công!";
        return RedirectToAction("Index");
    }

    // POST: /Admin/Review/Delete
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete(string id)
    {
        var review = await _context.Reviews
            .Include(r => r.InverseParent)
            .FirstOrDefaultAsync(r => r.Id == id);

        if (review == null)
        {
            TempData["ToastType"] = "error";
            TempData["ToastMessage"] = "Đánh giá không tồn tại.";
            return RedirectToAction("Index");
        }

        // Xoá cả reply con
        if (review.InverseParent.Any())
        {
            _context.Reviews.RemoveRange(review.InverseParent);
        }

        _context.Reviews.Remove(review);

        // Cập nhật lại RatingAvg/RatingCount nếu là review gốc
        if (review.ParentId == null && review.ProductId != null)
        {
            var product = await _context.Products
                .Include(p => p.Reviews)
                .FirstOrDefaultAsync(p => p.Id == review.ProductId);

            if (product != null)
            {
                var activeReviews = product.Reviews
                    .Where(r => r.Rating.HasValue && r.ParentId == null && r.Id != id)
                    .ToList();

                product.RatingCount = activeReviews.Count;
                product.RatingAvg = activeReviews.Count > 0
                    ? Math.Round(activeReviews.Average(r => (decimal)r.Rating!.Value), 1)
                    : 0;
            }
        }

        await _context.SaveChangesAsync();

        TempData["ToastType"] = "success";
        TempData["ToastMessage"] = "Đã xoá đánh giá thành công!";
        return RedirectToAction("Index");
    }
}