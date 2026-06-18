using System.ComponentModel.DataAnnotations.Schema;
using WebFlex.Shared.Entities;

namespace WebFlex.Shared.Entities.Opc;

public class OpcCollectRuntimeStatus : BaseEntity {
    [Column("clrs_id")]
    public new string Id { get; set; }
    public string? OpcDeviceId { get; set; }

    public OpcDevice? Device { get; set; }

    public string EndpointUrl { get; set; } = "";

    public bool IsConnected { get; set; }

    public int SubscribedCount { get; set; }

    public DateTime? LastConnectedAt { get; set; }

    public DateTime? LastDisconnectedAt { get; set; }

    public DateTime? LastReceivedAt { get; set; }

    public string? LastErrorMessage { get; set; }

    public DateTime StatusUpdatedAt { get; set; } = DateTime.UtcNow;
}