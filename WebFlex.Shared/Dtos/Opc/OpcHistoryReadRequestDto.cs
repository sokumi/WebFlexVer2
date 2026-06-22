namespace WebFlex.Shared.Dtos.Opc;

public class OpcHistoryReadRequestDto {
    public string EndpointUrl { get; set; } = "";
    public string NodeId { get; set; } = "";

    public bool UseSecurity { get; set; }
    public bool UseAnonymous { get; set; } = true;
    public string? UserName { get; set; }
    public string? Password { get; set; }

    public DateTime StartTime { get; set; }
    public DateTime EndTime { get; set; }

    public string ReadMode { get; set; } = "Raw";
    public bool ReturnBounds { get; set; } = true;
    public bool ReadModified { get; set; } = false;
    public uint NumValuesPerNode { get; set; } = 0;
    public string TimestampsToReturn { get; set; } = "Both";
    public bool ReleaseContinuationPoints { get; set; } = false;
    public int MaxContinuationReads { get; set; } = 10;
}