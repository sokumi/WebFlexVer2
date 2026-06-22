namespace WebFlex.Shared.Dtos.Opc;

public class TimescaleOptionTableDto {
    public string SchemaName { get; set; } = "";
    public string TableName { get; set; } = "";
    public string FullName => $"{SchemaName}.{TableName}";

    public bool IsHypertable { get; set; }
    public string? TimeColumnName { get; set; }
    public string? ChunkTimeInterval { get; set; }

    public int? ChunkCount { get; set; }
    public string? TotalSize { get; set; }
    public string? TableSize { get; set; }
    public string? IndexSize { get; set; }

    public bool RetentionEnabled { get; set; }
    public string? RetentionDropAfter { get; set; }
    public string? RetentionScheduleInterval { get; set; }

    public bool CompressionEnabled { get; set; }
    public bool CompressionPolicyEnabled { get; set; }
    public string? CompressionAfter { get; set; }
    public string? CompressionScheduleInterval { get; set; }
    public string? CompressionSegmentBy { get; set; }
    public string? CompressionOrderBy { get; set; }

    public string? LastError { get; set; }
}