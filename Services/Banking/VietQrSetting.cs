namespace TMDTStore.Services.Banking;

public class VietQrSetting
{
    public string BankId { get; set; } = "";
    public string AccountNo { get; set; } = "";
    public string AccountName { get; set; } = "";
    public string Template { get; set; } = "compact2";
    public string ImageApiBaseUrl { get; set; } = "https://img.vietqr.io/image";
    public bool SandboxMode { get; set; }
}
