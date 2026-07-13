namespace TMDTStore.Models.ViewModels.Admin;

public class DailyRevenueItem
{
    public string Date { get; set; } = string.Empty;
    public decimal Revenue { get; set; }
    public int DeliveredCount { get; set; }
    public int OrderCount { get; set; }
}

public class TopProductItem
{
    public string ProductId { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string? ImageUrl { get; set; }
    public int TotalSold { get; set; }
}
