namespace WebFlex.Shared.Dtos.Opc;

public class OpcHistoryReadResponseDto {
    public bool Success { get; set; }
    public string Message { get; set; } = "";
    public string EndpointUrl { get; set; } = "";
    public string NodeId { get; set; } = "";
    public List<OpcHistoryReadValueDto> Values { get; set; } = new();
}