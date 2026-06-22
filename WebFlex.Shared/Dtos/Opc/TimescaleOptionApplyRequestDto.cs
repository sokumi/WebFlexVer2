namespace WebFlex.Shared.Dtos.Opc;

public class TimescaleOptionApplyRequestDto {
    public string SchemaName { get; set; } = "public";
    public string TableName { get; set; } = "";

    public string? ChunkTimeInterval { get; set; }

    public bool RetentionEnabled { get; set; }
    public string? RetentionDropAfter { get; set; }
    public string? RetentionScheduleInterval { get; set; }

    public bool CompressionEnabled { get; set; }
    public string? CompressionAfter { get; set; }
    public string? CompressionScheduleInterval { get; set; }
    public string? CompressionSegmentBy { get; set; }
    public string? CompressionOrderBy { get; set; }
}