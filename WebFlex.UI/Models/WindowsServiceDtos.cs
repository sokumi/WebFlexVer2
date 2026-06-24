namespace WebFlex.UI.Models;

public sealed class WindowsServiceStatusDto {
    public string ServiceName { get; set; } = "";
    public string DisplayName { get; set; } = "";
    public string Status { get; set; } = "";
    public bool Exists { get; set; }
    public string ExePath { get; set; } = "";
}

public sealed class WindowsServiceCommandResultDto {
    public bool Success { get; set; }
    public string Message { get; set; } = "";
    public string? Output { get; set; }
    public string? Error { get; set; }
}