using System.Collections.Concurrent;
using Opc.Ua.Client;
using WebFlex.Shared.Dtos.Opc;

namespace WebFlex.OpcCollector.Runtime;

public class OpcDeviceRuntime {
    public string DeviceId { get; set; }

    public string DeviceCode { get; set; } = "";

    public string DeviceName { get; set; } = "";

    public string EndpointUrl { get; set; } = "";

    public Session Session { get; set; } = null!;

    public Subscription Subscription { get; set; } = null!;

    public ConcurrentDictionary<string, MonitoredItem> Items { get; } =
        new(StringComparer.OrdinalIgnoreCase);

    public ConcurrentDictionary<string, MonitoredItemNotificationEventHandler> ItemHandlers { get; } =
        new(StringComparer.OrdinalIgnoreCase);

    public ConcurrentDictionary<string, OpcCollectTargetTagDto> Tags { get; } =
        new(StringComparer.OrdinalIgnoreCase);

    public ConcurrentDictionary<string, OpcCurrentRuntimeValue> CurrentValues { get; } =
        new(StringComparer.OrdinalIgnoreCase);

    public SemaphoreSlim SyncLock { get; } = new(1, 1);


    public DateTime LastConnectionOkTime { get; set; } = DateTime.MinValue;

    public DateTime LastStatusUpdatedAt { get; set; } = DateTime.MinValue;

    public bool IsConnected => Session != null && Session.Connected;
}