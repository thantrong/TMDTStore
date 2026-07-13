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
        // SSE streaming endpoint — relative URL trên cùng domain
        // Web server proxy request đến RAG nội bộ (tránh CORS + internal DNS)
        ViewBag.RagEndpoint = "/api/rag/chat/stream";
        return View();
    }
}
