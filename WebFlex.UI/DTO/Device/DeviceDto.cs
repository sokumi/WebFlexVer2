namespace WebFlex.UI.DTO;

public class DeviceDto {
    public string Id { get; set; }

    public string DeviceCode { get; set; }
    public string DeviceName { get; set; }
    public string DeviceAddress { get; set; }
    public int? Port { get; set; }

    public string EndpointUrl { get; set; }
    public string DeviceType { get; set; }

    public bool IsCollectEnabled { get; set; } = true;

    public bool UseSecurity { get; set; }
    public string? SecurityPolicy { get; set; }
    public string? SecurityMode { get; set; }

    public bool UseAnonymous { get; set; }
    public string? UserName { get; set; }
    public string? Password { get; set; }

    public int? PublishingIntervalMs { get; set; }
    public int? SamplingIntervalMs { get; set; }
    public int QueueSize { get; set; }

    public int SortOrder { get; set; }
    public string? Description { get; set; }

    public bool IsEnabled { get; set; }
}