namespace WebFlex.Shared;

public class TimescaleMinuteValue {
    public DateTime Time { get; set; }

    [ColumnStringLength(15)]
    public string TAG_ID { get; set; }

    [ColumnStringLength(15)]
    public string GROUP_ID { get; set; }

    [ColumnStringLength(4)]
    public VaribaleStatusType? STATUS { get; set; }

    public string? VALUE { get; set; }

    public string? COOKIE_VALUE { get; set; }

    public DateTime? SourceTimestamp { get; set; }

    public DateTime ReceivedAt { get; set; }
}