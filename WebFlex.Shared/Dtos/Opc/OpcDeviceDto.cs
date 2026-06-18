namespace WebFlex.Shared.Dtos.Opc;

public class OpcDeviceDto {
    public string Id { get; set; }

    public string? OpcGroupId { get; set; }

    public string DeviceCode { get; set; } = "";

    public string DeviceName { get; set; } = "";

    public string DeviceAddress { get; set; } = "";

    public int Port { get; set; }

    public string EndpointUrl { get; set; } = "";

    public string DeviceType { get; set; } = "OPC_UA";

    public bool IsEnabled { get; set; }

    public bool IsCollectEnabled { get; set; }

    public bool UseSecurity { get; set; }

    public bool UseAnonymous { get; set; }

    public int PublishingIntervalMs { get; set; }

    public int SamplingIntervalMs { get; set; }

    public int QueueSize { get; set; }

    public int TagCount { get; set; }

    public string? Description { get; set; }
}