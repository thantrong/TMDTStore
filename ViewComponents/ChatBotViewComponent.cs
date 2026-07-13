using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;

namespace TMDTStore.ViewComponents;

public class ChatBotViewComponent : ViewComponent
{
    private readonly IConfiguration _configuration;

    public ChatBotViewComponent(IConfiguration configuration)
    {
        _configuration = configuration;
    }

    public IViewComponentResult Invoke(string? productId = null)
    {
        ViewBag.ProductId = productId ?? "";
        // SSE streaming endpoint — đọc từ appsettings.json, fallback localhost:8001
        var ragBaseUrl = _configuration.GetValue<string>("RagSettings:BaseUrl") ?? "http://localhost:8001";
        ViewBag.RagEndpoint = $"{ragBaseUrl.TrimEnd('/')}/api/rag/chat/stream";
        return View();
    }
}
