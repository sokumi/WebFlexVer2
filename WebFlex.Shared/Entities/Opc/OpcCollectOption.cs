using System.ComponentModel.DataAnnotations.Schema;
using WebFlex.Shared.Entities;

namespace WebFlex.Shared.Entities.Opc;

public class OpcCollectOption : BaseEntity {
    [Column("clop_id")]
    public new string Id { get; set; }
    public string OptionCode { get; set; } = "DEFAULT";

    public string OptionName { get; set; } = "기본 수집 옵션";

    public int ReloadIntervalSeconds { get; set; } = 10;

    public int SaveIntervalMilliseconds { get; set; } = 1000;

    public int FlushIntervalMilliseconds { get; set; } = 200;

    public int MaxBatchSize { get; set; } = 5000;

    public bool SaveOnlyChangedValue { get; set; }

    public bool AutoReconnect { get; set; } = true;

    public int ReconnectIntervalSeconds { get; set; } = 5;

    public int ConnectionCheckIntervalSeconds { get; set; } = 1;

    public string? Description { get; set; }
}