namespace WebFlex.Shared.Dtos.Opc;

public class OpcHistoryReadValueDto {
    public DateTime? SourceTimestamp { get; set; }
    public DateTime? ServerTimestamp { get; set; }
    public string? Value { get; set; }
    public string StatusCode { get; set; } = "";
}