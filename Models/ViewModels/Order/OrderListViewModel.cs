namespace TMDTStore.Models.ViewModels.Order;

public class OrderListViewModel
{
    public string? SearchQuery { get; set; }
    public string? StatusFilter { get; set; }
    public List<TMDTStore.Models.Order> Orders { get; set; } = new();
    public int Page { get; set; } = 1;
    public int PageSize { get; set; } = 15;
    public int TotalItems { get; set; }
    public int TotalPages => (int)Math.Ceiling((double)TotalItems / PageSize);
}
