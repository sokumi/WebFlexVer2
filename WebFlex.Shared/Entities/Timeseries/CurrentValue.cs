using System.Diagnostics;

namespace WebFlex.Shared;

[DebuggerDisplay("{TAG_ID} {GROUP_ID} {Value} {CookieValue} {CookieValue}")]
public class CurrentValue {
    [ColumnStringLength(15)]
    public string TAG_ID { get; set; }

    [ColumnStringLength(15)]
    public string GROUP_ID { get; set; }

    [ColumnStringLength(4)]
    public int? STATUS { get; set; }

    public string? VALUE { get; set; }

    public string? COOKIE_VALUE { get; set; }

    [ColumnStringLength(8)]
    public int? UPDATE_COUNT { get; set; }

    public DateTime? SOURCETIMESTAMP { get; set; }

    public DateTime RECEIVEDAT { get; set; }

    public DateTime UPDATEDAT { get; set; }
}