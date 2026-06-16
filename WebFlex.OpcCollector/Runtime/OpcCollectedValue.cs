namespace WebFlex.OpcCollector.Runtime;

public class OpcCollectedValue {
    public DateTime Time { get; set; }

    public string EndpointUrl { get; set; } = "";

    public string NodeId { get; set; } = "";

    public string? Value { get; set; }

    public string? Status { get; set; }

    public DateTime? SourceTimestamp { get; set; }

    public DateTime ReceivedAt { get; set; }
}