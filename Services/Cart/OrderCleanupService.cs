using Microsoft.EntityFrameworkCore;
using TMDTStore.Models;

namespace TMDTStore.Services;

public class OrderCleanupService : BackgroundService
{
    private readonly IServiceProvider _services;
    private readonly ILogger<OrderCleanupService> _logger;

    public OrderCleanupService(IServiceProvider services, ILogger<OrderCleanupService> logger)
    {
        _services = services;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("OrderCleanupService started.");

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                using var scope = _services.CreateScope();
                var context = scope.ServiceProvider.GetRequiredService<StoreDbContext>();

                var now = DateTime.UtcNow;
                var expiredOrders = await context.Orders
                    .Where(o => o.Status == "WaitingPayment"
                        && o.ExpiresAt.HasValue
                        && o.ExpiresAt.Value < now)
                    .ToListAsync(stoppingToken);

                foreach (var order in expiredOrders)
                {
                    order.Status = "Cancelled";

                    // Hoàn lại tồn kho
                    foreach (var item in await context.OrderItems
                        .Where(i => i.OrderId == order.Id).ToListAsync(stoppingToken))
                    {
                        if (!string.IsNullOrEmpty(item.VariantId))
                        {
                            var variant = await context.ProductVariants.FindAsync(new object[] { item.VariantId }, stoppingToken);
                            if (variant != null) variant.StockQuantity += item.Quantity;
                        }
                        else
                        {
                            var inventory = await context.Inventories.FirstOrDefaultAsync(i => i.ProductId == item.ProductId, stoppingToken);
                            if (inventory != null) inventory.StockQuantity += item.Quantity;
                        }
                    }

                    context.Set<OrderStatusHistory>().Add(new OrderStatusHistory
                    {
                        OrderId = order.Id,
                        Status = "Cancelled",
                        Reason = "Quá thời gian thanh toán (15 phút)",
                        ChangedAtUtc = now
                    });

                    _logger.LogInformation("Order {OrderId} auto-cancelled due to payment timeout.", order.Id);
                }

                if (expiredOrders.Count > 0)
                    await context.SaveChangesAsync(stoppingToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error while cleaning up expired orders.");
            }

            await Task.Delay(TimeSpan.FromMinutes(1), stoppingToken);
        }
    }
}
