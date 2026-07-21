using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using TMDTStore.Models;

namespace TMDTStore.Controllers;

[Authorize]
public class ReviewController : Controller
{
    private readonly StoreDbContext _context;
    private readonly UserManager<User> _userManager;

    public ReviewController(StoreDbContext context, UserManager<User> userManager)
    {
        _context = context;
        _userManager = userManager;
    }

    // POST: /Review/Add
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Add(string productId, short rating, string comment)
    {
        var user = await _userManager.GetUserAsync(User);
        if (user == null)
            return RedirectToAction("Login", "Auth");

        // Validate rating
        if (rating < 1 || rating > 5)
        {
            TempData["ToastType"] = "error";
            TempData["ToastMessage"] = "Điểm đánh giá không hợp lệ.";
            return RedirectToAction("Details", "Product", new { id = productId });
        }

        // Validate comment
        if (string.IsNullOrWhiteSpace(comment) || comment.Trim().Length < 10)
        {
            TempData["ToastType"] = "error";
            TempData["ToastMessage"] = "Vui lòng nhập ít nhất 10 ký tự.";
            return RedirectToAction("Details", "Product", new { id = productId });
        }

        var existing = await _context.Reviews
            .FirstOrDefaultAsync(r => r.ProductId == productId && r.UserId == user.Id && r.ParentId == null);

        if (existing != null)
        {
            TempData["ToastType"] = "error";
            TempData["ToastMessage"] = "Bạn đã đánh giá sản phẩm này rồi.";
            return RedirectToAction("Details", "Product", new { id = productId });
        }

        var review = new Review
        {
            Id = Guid.NewGuid().ToString("N")[..12].ToUpper(),
            ProductId = productId,
            UserId = user.Id,
            Rating = rating,
            Comment = comment.Trim(),
            IsStaffReply = false,
            CreatedAt = DateTime.UtcNow
        };

        _context.Reviews.Add(review);

        // Cập nhật RatingAvg và RatingCount
        var product = await _context.Products
            .Include(p => p.Reviews)
            .FirstOrDefaultAsync(p => p.Id == productId);

        if (product != null)
        {
            var allRatings = product.Reviews
                .Where(r => r.Rating.HasValue && r.ParentId == null)
                .Select(r => (decimal)r.Rating.Value)
                .Append((decimal)rating)
                .ToList();

            product.RatingAvg = Math.Round(allRatings.Average(), 1);
            product.RatingCount = allRatings.Count;
        }

        await _context.SaveChangesAsync();

        TempData["ToastType"] = "success";
        TempData["ToastMessage"] = "Đánh giá của bạn đã được gửi thành công!";
        return RedirectToAction("Details", "Product", new { id = productId });
    }
}
