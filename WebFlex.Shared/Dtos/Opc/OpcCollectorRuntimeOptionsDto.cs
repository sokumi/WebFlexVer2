namespace WebFlex.Shared.Dtos.Opc;

public class OpcCollectorRuntimeOptionsDto {
    public bool EnableAutoReload { get; set; }
    public bool EnableSnapshotSave { get; set; }
    public bool EnableTimescaleHistorySave { get; set; }
    public bool EnableCurrentValueSave { get; set; }

    public int ReloadIntervalSeconds { get; set; }
    public int SaveIntervalMilliseconds { get; set; }
    public int MaxBatchSize { get; set; }
    public int WriterLogIntervalSeconds { get; set; }

    public int DefaultPublishingIntervalMs { get; set; }
    public int DefaultSamplingIntervalMs { get; set; }
    public int DefaultQueueSize { get; set; }

    public int SubscriptionKeepAliveCount { get; set; }
    public int SubscriptionLifetimeCount { get; set; }
    public int MaxNotificationsPerPublish { get; set; }
    public byte SubscriptionPriority { get; set; }
    public bool DiscardOldest { get; set; }

    public bool AutoAcceptUntrustedCertificates { get; set; }
    public bool RejectSHA1SignedCertificates { get; set; }
    public int MinimumCertificateKeySize { get; set; }
    public bool SuppressNonceValidationErrors { get; set; }
    public string CertificateStoreRootPath { get; set; } = "";

    public int OperationTimeoutMilliseconds { get; set; }
    public int DefaultSessionTimeoutMilliseconds { get; set; }
    public int MinSubscriptionLifetimeMilliseconds { get; set; }

    public int MaxStringLength { get; set; }
    public int MaxByteStringLength { get; set; }
    public int MaxArrayLength { get; set; }
    public int MaxMessageSize { get; set; }
    public int MaxBufferSize { get; set; }
    public int ChannelLifetime { get; set; }
    public int SecurityTokenLifetime { get; set; }

    public bool DisableHiResClock { get; set; }

    public bool DefaultUseSecurity { get; set; }
    public bool DefaultUseAnonymous { get; set; }
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