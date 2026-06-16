namespace WebFlex.Shared.Entities.Timeseries;

public class CurrentValueMinute {
    public string EndpointUrl { get; set; } = "";

    public string NodeId { get; set; } = "";

    public DateTime MinuteTime { get; set; }

    public string? Value { get; set; }

    public string? Status { get; set; }

    public DateTime? SourceTimestamp { get; set; }

    public DateTime ReceivedAt { get; set; }

    public DateTime UpdatedAt { get; set; }
}