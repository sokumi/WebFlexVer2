namespace WebFlex.Shared.Dtos.Opc;

public class OpcCollectorRuntimeOptionsDto {
    public bool EnableAutoReload { get; set; } = true;
    public bool EnableSnapshotSave { get; set; } = true;
    public bool EnableTimescaleHistorySave { get; set; } = true;
    public bool EnableCurrentValueSave { get; set; } = true;

    public int ReloadIntervalSeconds { get; set; } = 3600;
    public int SaveIntervalMilliseconds { get; set; } = 1000;
    public int FlushIntervalMilliseconds { get; set; } = 200;
    public int MaxBatchSize { get; set; } = 5000;
    public int WriterLogIntervalSeconds { get; set; } = 30;

    public int DefaultPublishingIntervalMs { get; set; } = 1000;
    public int DefaultSamplingIntervalMs { get; set; } = 1000;
    public int DefaultQueueSize { get; set; } = 1;

    // -1이면 uint.MaxValue로 처리
    public int SubscriptionKeepAliveCount { get; set; } = -1;
    public int SubscriptionLifetimeCount { get; set; } = -1;
    public int MaxNotificationsPerPublish { get; set; } = -1;
    public byte SubscriptionPriority { get; set; } = 100;
    public bool DiscardOldest { get; set; } = true;

    public bool AutoAcceptUntrustedCertificates { get; set; } = true;
    public bool RejectSHA1SignedCertificates { get; set; } = false;
    public int MinimumCertificateKeySize { get; set; } = 1024;
    public bool SuppressNonceValidationErrors { get; set; } = true;
    public string CertificateStoreRootPath { get; set; } = "pki";

    public int OperationTimeoutMilliseconds { get; set; } = 6000000;
    public int DefaultSessionTimeoutMilliseconds { get; set; } = -1;
    public int MinSubscriptionLifetimeMilliseconds { get; set; } = -1;

    public int MaxStringLength { get; set; } = int.MaxValue;
    public int MaxByteStringLength { get; set; } = int.MaxValue;
    public int MaxArrayLength { get; set; } = 65535;
    public int MaxMessageSize { get; set; } = 419430400;
    public int MaxBufferSize { get; set; } = 65535;
    public int ChannelLifetime { get; set; } = -1;
    public int SecurityTokenLifetime { get; set; } = -1;

    public bool DisableHiResClock { get; set; } = true;

    public bool DefaultUseSecurity { get; set; } = false;
    public bool DefaultUseAnonymous { get; set; } = true;
    public string DefaultSecurityPolicy { get; set; } = "";
    public string DefaultSecurityMode { get; set; } = "";

    public static OpcCollectorRuntimeOptionsDto Clone(OpcCollectorRuntimeOptionsDto source) {
        return new OpcCollectorRuntimeOptionsDto {
            EnableAutoReload = source.EnableAutoReload,
            EnableSnapshotSave = source.EnableSnapshotSave,
            EnableTimescaleHistorySave = source.EnableTimescaleHistorySave,
            EnableCurrentValueSave = source.EnableCurrentValueSave,

            ReloadIntervalSeconds = source.ReloadIntervalSeconds,
            SaveIntervalMilliseconds = source.SaveIntervalMilliseconds,
            FlushIntervalMilliseconds = source.FlushIntervalMilliseconds,
            MaxBatchSize = source.MaxBatchSize,
            WriterLogIntervalSeconds = source.WriterLogIntervalSeconds,

            DefaultPublishingIntervalMs = source.DefaultPublishingIntervalMs,
            DefaultSamplingIntervalMs = source.DefaultSamplingIntervalMs,
            DefaultQueueSize = source.DefaultQueueSize,

            SubscriptionKeepAliveCount = source.SubscriptionKeepAliveCount,
            SubscriptionLifetimeCount = source.SubscriptionLifetimeCount,
            MaxNotificationsPerPublish = source.MaxNotificationsPerPublish,
            SubscriptionPriority = source.SubscriptionPriority,
            DiscardOldest = source.DiscardOldest,

            AutoAcceptUntrustedCertificates = source.AutoAcceptUntrustedCertificates,
            RejectSHA1SignedCertificates = source.RejectSHA1SignedCertificates,
            MinimumCertificateKeySize = source.MinimumCertificateKeySize,
            SuppressNonceValidationErrors = source.SuppressNonceValidationErrors,
            CertificateStoreRootPath = source.CertificateStoreRootPath,

            OperationTimeoutMilliseconds = source.OperationTimeoutMilliseconds,
            DefaultSessionTimeoutMilliseconds = source.DefaultSessionTimeoutMilliseconds,
            MinSubscriptionLifetimeMilliseconds = source.MinSubscriptionLifetimeMilliseconds,

            MaxStringLength = source.MaxStringLength,
            MaxByteStringLength = source.MaxByteStringLength,
            MaxArrayLength = source.MaxArrayLength,
            MaxMessageSize = source.MaxMessageSize,
            MaxBufferSize = source.MaxBufferSize,
            ChannelLifetime = source.ChannelLifetime,
            SecurityTokenLifetime = source.SecurityTokenLifetime,

            DisableHiResClock = source.DisableHiResClock,

            DefaultUseSecurity = source.DefaultUseSecurity,
            DefaultUseAnonymous = source.DefaultUseAnonymous,
            DefaultSecurityPolicy = source.DefaultSecurityPolicy,
            DefaultSecurityMode = source.DefaultSecurityMode
        };
    }
}