using WebFlex.Shared;

namespace WebFlex.OpcCollector.Runtime;

public class OpcCurrentRuntimeValue {
    public string TagId { get; set; } = "";

    public string? GroupId { get; set; }

    public string? Value { get; set; }

    public VaribaleStatusType? Status { get; set; }

    public string? CookieValue { get; set; }

    public DateTime? SourceTimestamp { get; set; }

    public DateTime ReceivedAt { get; set; }

    public bool SaveToDatabase { get; set; } = true;
}