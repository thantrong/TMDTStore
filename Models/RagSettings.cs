namespace TMDTStore.Models;

public class RagSettings
{
    /// <summary>
    /// Base URL of the RAG service (internal on Railway: http://rag.railway.internal:8080)
    /// </summary>
    public string BaseUrl { get; set; } = "http://localhost:8001";
}
