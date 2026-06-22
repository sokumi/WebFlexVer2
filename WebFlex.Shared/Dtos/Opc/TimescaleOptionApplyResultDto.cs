namespace WebFlex.Shared.Dtos.Opc;

public class TimescaleOptionApplyResultDto {
    public bool Success { get; set; }
    public string Message { get; set; } = "";
    public List<string> Logs { get; set; } = new();
}