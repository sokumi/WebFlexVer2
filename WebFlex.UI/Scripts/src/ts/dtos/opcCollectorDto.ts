export type OpcCollectorOptionsDto = {
    enableAutoReload: boolean;
    enableSnapshotSave: boolean;
    enableTimescaleHistorySave: boolean;
    enableCurrentValueSave: boolean;

    reloadIntervalSeconds: number;
    saveIntervalMilliseconds: number;
    maxBatchSize: number;
    writerLogIntervalSeconds: number;

    defaultPublishingIntervalMs: number;
    defaultSamplingIntervalMs: number;
    defaultQueueSize: number;

    subscriptionKeepAliveCount: number;
    subscriptionLifetimeCount: number;
    maxNotificationsPerPublish: number;
    subscriptionPriority: number;
    discardOldest: boolean;

    autoAcceptUntrustedCertificates: boolean;
    rejectSHA1SignedCertificates: boolean;
    minimumCertificateKeySize: number;
    suppressNonceValidationErrors: boolean;
    certificateStoreRootPath: string;

    operationTimeoutMilliseconds: number;
    defaultSessionTimeoutMilliseconds: number;
    minSubscriptionLifetimeMilliseconds: number;

    maxStringLength: number;
    maxByteStringLength: number;
    maxArrayLength: number;
    maxMessageSize: number;
    maxBufferSize: number;
    channelLifetime: number;
    securityTokenLifetime: number;

    disableHiResClock: boolean;

    defaultUseSecurity: boolean;
    defaultUseAnonymous: boolean;
    defaultSecurityPolicy: string;
    defaultSecurityMode: string;
};