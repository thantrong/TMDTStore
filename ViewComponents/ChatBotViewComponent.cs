using Microsoft.AspNetCore.Mvc;

namespace TMDTStore.ViewComponents;

public class ChatBotViewComponent : ViewComponent
{
    public IViewComponentResult Invoke(string? productId = null)
    {
        ViewBag.ProductId = productId ?? "";
        // SSE streaming endpoint — chatbot.js đọc response.body.getReader()
        ViewBag.RagEndpoint = "http://localhost:8001/api/rag/chat/stream";
        return View();
    }
}
