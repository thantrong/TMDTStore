using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using TMDTStore.Models;

namespace TMDTStore.Controllers;

[Route("api/rag/chat")]
[ApiExplorerSettings(IgnoreApi = true)]
public class RagProxyController : Controller
{
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly RagSettings _ragSettings;
    private readonly ILogger<RagProxyController> _logger;

    public RagProxyController(
        IHttpClientFactory httpClientFactory,
        IOptions<RagSettings> ragSettings,
        ILogger<RagProxyController> logger)
    {
        _httpClientFactory = httpClientFactory;
        _ragSettings = ragSettings.Value;
        _logger = logger;
    }

    [HttpPost("stream")]
    public async Task Stream()
    {
        var targetUrl = $"{_ragSettings.BaseUrl.TrimEnd('/')}/api/rag/chat/stream";

        using var reader = new StreamReader(Request.Body);
        var body = await reader.ReadToEndAsync();

        _logger.LogInformation("Proxying to {TargetUrl}", targetUrl);

        try
        {
            var client = _httpClientFactory.CreateClient("RagProxy");
            var request = new HttpRequestMessage(HttpMethod.Post, targetUrl)
            {
                Content = new StringContent(body, System.Text.Encoding.UTF8, "application/json")
            };

            using var response = await client.SendAsync(request, HttpCompletionOption.ResponseHeadersRead);

            if (!response.IsSuccessStatusCode)
            {
                var errorBody = await response.Content.ReadAsStringAsync();
                _logger.LogError("RAG proxy failed: {StatusCode} {Error}", (int)response.StatusCode, errorBody);
                Response.StatusCode = (int)response.StatusCode;
                await Response.WriteAsync(errorBody);
                return;
            }

            // Stream SSE response về browser
            Response.StatusCode = (int)response.StatusCode;
            Response.ContentType = response.Content.Headers.ContentType?.ToString() ?? "text/event-stream";

            using var responseStream = await response.Content.ReadAsStreamAsync();
            await responseStream.CopyToAsync(Response.Body);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "RAG proxy error");
            Response.StatusCode = 502;
            await Response.WriteAsync($"{{\"error\":\"RAG service unreachable: {ex.Message}\"}}");
        }
    }
}
