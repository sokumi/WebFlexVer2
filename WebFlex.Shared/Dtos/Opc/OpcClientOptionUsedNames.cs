namespace WebFlex.Shared.Dtos.Opc;

public static class OpcClientOptionUsedNames {
    public static List<string> GetAll() {
        return new List<string> {
            "ApplicationName",
            "ApplicationUri",
            "ProductUri",
            "ApplicationType",
            "DisableHiResClock",

            "ApplicationCertificateStoreType",
            "ApplicationCertificateStorePath",
            "ApplicationCertificateSubjectName",
            "ApplicationCertificateThumbprint",

            "TrustedPeerCertificatesStoreType",
            "TrustedPeerCertificatesStorePath",
            "TrustedIssuerCertificatesStoreType",
            "TrustedIssuerCertificatesStorePath",
            "RejectedCertificateStoreType",
            "RejectedCertificateStorePath",

            "AutoAcceptUntrustedCertificates",
            "RejectSHA1SignedCertificates",
            "RejectUnknownRevocationStatus",
            "MinimumCertificateKeySize",
            "AddAppCertToTrustedStore",
            "SuppressNonceValidationErrors",
            "SendCertificateChain",

            "OperationTimeout",
            "MaxStringLength",
            "MaxByteStringLength",
            "MaxArrayLength",
            "MaxMessageSize",
            "MaxBufferSize",
            "ChannelLifetime",
            "SecurityTokenLifetime",

            "DefaultSessionTimeout",
            "MinSubscriptionLifetime",

            "UseSecurity",
            "IdentityType",
            "UserName",
            "Password",

            "SessionName",
            "SessionTimeout",
            "UpdateBeforeConnect",
            "CheckDomain",
            "PreferredLocales",

            "PublishingInterval",
            "LifetimeCount",
            "KeepAliveCount",
            "MaxNotificationsPerPublish",
            "PublishingEnabled",
            "Priority",

            "AttributeId",
            "MonitoringMode",
            "SamplingInterval",
            "QueueSize",
            "DiscardOldest",
            "DeadbandType",
            "DeadbandValue",
            "DataChangeTrigger"
        };
    }
}