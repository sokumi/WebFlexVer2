namespace WebFlex.Shared.Dtos.Opc;

public class OpcClientOptionDto {
    public string ApplicationName { get; set; } = "WebFlexOpcCollector";
    public string ApplicationUri { get; set; } = "urn:localhost:WebFlexOpcCollector";
    public string ProductUri { get; set; } = "WebFlexOpcCollector";
    public string ApplicationType { get; set; } = "Client";
    public bool DisableHiResClock { get; set; } = true;

    public string ApplicationCertificateStoreType { get; set; } = "Directory";
    public string ApplicationCertificateStorePath { get; set; } = "pki/own";
    public string ApplicationCertificateSubjectName { get; set; } = "WebFlexOpcCollector";
    public string ApplicationCertificateThumbprint { get; set; } = "";

    public string TrustedPeerCertificatesStoreType { get; set; } = "Directory";
    public string TrustedPeerCertificatesStorePath { get; set; } = "pki/trusted";
    public string TrustedIssuerCertificatesStoreType { get; set; } = "Directory";
    public string TrustedIssuerCertificatesStorePath { get; set; } = "pki/issuer";
    public string RejectedCertificateStoreType { get; set; } = "Directory";
    public string RejectedCertificateStorePath { get; set; } = "pki/rejected";

    public bool AutoAcceptUntrustedCertificates { get; set; } = true;
    public bool RejectSHA1SignedCertificates { get; set; } = false;
    public bool RejectUnknownRevocationStatus { get; set; } = false;
    public int MinimumCertificateKeySize { get; set; } = 1024;
    public bool AddAppCertToTrustedStore { get; set; } = false;
    public bool SuppressNonceValidationErrors { get; set; } = true;
    public bool SendCertificateChain { get; set; } = false;

    public int OperationTimeout { get; set; } = 6000000;
    public int MaxStringLength { get; set; } = int.MaxValue;
    public int MaxByteStringLength { get; set; } = int.MaxValue;
    public int MaxArrayLength { get; set; } = 65535;
    public int MaxMessageSize { get; set; } = 419430400;
    public int MaxBufferSize { get; set; } = 65535;
    public int ChannelLifetime { get; set; } = -1;
    public int SecurityTokenLifetime { get; set; } = -1;

    public int DefaultSessionTimeout { get; set; } = -1;
    public int MinSubscriptionLifetime { get; set; } = -1;
    public string WellKnownDiscoveryUrls { get; set; } = "";
    public string DiscoveryServers { get; set; } = "";
    public string EndpointCacheFilePath { get; set; } = "";

    public string EndpointUrl { get; set; } = "opc.tcp://127.0.0.1:49320";
    public bool UseSecurity { get; set; } = false;
    public string SecurityPolicyUri { get; set; } = "";
    public string MessageSecurityMode { get; set; } = "";
    public string TransportProfileUri { get; set; } = "";
    public int EndpointSelectionTimeout { get; set; } = 15000;

    public string IdentityType { get; set; } = "Anonymous";
    public string UserName { get; set; } = "";
    public string Password { get; set; } = "";
    public string CertificateUserStoreType { get; set; } = "Directory";
    public string CertificateUserStorePath { get; set; } = "pki/user";
    public string CertificateUserSubjectName { get; set; } = "";

    public string SessionName { get; set; } = "WebFlexOpcCollector";
    public int SessionTimeout { get; set; } = 60000;
    public bool UpdateBeforeConnect { get; set; } = false;
    public bool CheckDomain { get; set; } = false;
    public string PreferredLocales { get; set; } = "ko-KR,en-US";

    public int PublishingInterval { get; set; } = 1000;
    public uint LifetimeCount { get; set; } = 60;
    public uint KeepAliveCount { get; set; } = 10;
    public uint MaxNotificationsPerPublish { get; set; } = 0;
    public bool PublishingEnabled { get; set; } = true;
    public byte Priority { get; set; } = 100;

    public string AttributeId { get; set; } = "Value";
    public string MonitoringMode { get; set; } = "Reporting";
    public int SamplingInterval { get; set; } = 1000;
    public uint QueueSize { get; set; } = 1;
    public bool DiscardOldest { get; set; } = true;
    public string DeadbandType { get; set; } = "None";
    public double DeadbandValue { get; set; } = 0;
    public string DataChangeTrigger { get; set; } = "StatusValue";

    public string BrowseNodeClassMask { get; set; } = "Object,Variable";
    public string BrowseResultMask { get; set; } = "All";
    public uint BrowseMaxReferencesToReturn { get; set; } = 0;
    public double ReadMaxAge { get; set; } = 0;
    public string ReadTimestampsToReturn { get; set; } = "Both";

    public bool EnableSessionKeepAlive { get; set; } = true;
    public int KeepAliveInterval { get; set; } = 5000;
    public int ReconnectPeriod { get; set; } = 10000;
    public int MaxReconnectAttempts { get; set; } = -1;
}