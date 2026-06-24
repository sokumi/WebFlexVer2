using Microsoft.EntityFrameworkCore;
using System.Reflection;
using System.Text.RegularExpressions;
using WebFlex.Shared;
using WebFlex.Shared.Entities;
using WebFlex.Shared.Dtos.Opc;

namespace WebFlex.UI.Data;

public class WebFlexDbContext : BaseDbContext {
    public WebFlexDbContext(DbContextOptions<WebFlexDbContext> options)
        : base(options) {
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder) {
        base.OnModelCreating(modelBuilder);

        ApplyModelEntities(modelBuilder);
        ApplySeedData(modelBuilder);
        ApplyIndexes(modelBuilder);
        ApplyDeleteBehaviors(modelBuilder);
    }

    private static void ApplyModelEntities(ModelBuilder modelBuilder) {
        var entityTypes = typeof(BaseEntity).Assembly
            .GetTypes()
            .Where(type =>
                type is { IsClass: true, IsAbstract: false } &&
                typeof(BaseEntity).IsAssignableFrom(type));

        foreach (var type in entityTypes) {
            var entity = modelBuilder.Entity(type);

            var keyColumnName = GetKeyFieldColumnName(type);

            if (!string.IsNullOrWhiteSpace(keyColumnName)) {
                entity.HasKey(nameof(BaseEntity.ID));

                entity.Property(nameof(BaseEntity.ID))
                    .HasColumnName(ToSnakeCase(keyColumnName))
                    .HasColumnOrder(1)     // 무조건 1번
                    .ValueGeneratedNever();
            }
        }
    }

    private static string? GetKeyFieldColumnName(Type type) {
        var attribute = type.GetCustomAttributesData()
            .FirstOrDefault(x => x.AttributeType.Name == "KeyFieldColumnAttribute");

        return attribute?.ConstructorArguments.FirstOrDefault().Value?.ToString();
    }

    private static string ToSnakeCase(string value) {
        if (string.IsNullOrWhiteSpace(value)) {
            return value;
        }

        if (value.Contains('_')) {
            return value.ToLowerInvariant();
        }

        return Regex.Replace(value, "([a-z0-9])([A-Z])", "$1_$2")
            .ToLowerInvariant();
    }

    private static void ApplySeedData(ModelBuilder modelBuilder) {
        var createdAt = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc);

        modelBuilder.Entity<OpcCollectOption>().HasData(new OpcCollectOption {
            ID = "COLLECT_OPTION_DEFAULT",
            OPTION_CODE = "DEFAULT",
            OPTION_NAME = "기본 OPC 수집 옵션",

            RELOAD_INTERVAL_SECONDS = 3600,
            SAVE_INTERVAL_MILLISECONDS = 1000,
            FLUSH_INTERVAL_MILLISECONDS = 200,
            MAX_BATCH_SIZE = 5000,
            SAVE_ONLY_CHANGED_VALUE = false,
            AUTO_RECONNECT = true,
            RECONNECT_INTERVAL_SECONDS = 10,
            CONNECTION_CHECK_INTERVAL_SECONDS = 30,

            OPTION_JSON = GetDefaultCollectOptionJson(),
            CONFIGURED_OPTION_NAMES = GetConfiguredOptionNames<OpcCollectorRuntimeOptionsDto>(),
            DESCRIPTION = "OPC Collector 기본 수집 옵션",

            IsEnabled = true,
            CreatedAt = createdAt,
            UpdatedAt = null
        });

        modelBuilder.Entity<OpcClientOption>().HasData(new OpcClientOption {
            ID = "CLIENT_OPTION_DEFAULT",
            OPTION_CODE = "DEFAULT",
            OPTION_NAME = "기본 OPC Client 옵션",
            OPTION_JSON = GetDefaultClientOptionJson(),
            CONFIGURED_OPTION_NAMES = GetConfiguredOptionNames<OpcClientOptionDto>(),
            DESCRIPTION = "OPC UA Client 기본 옵션",

            IsEnabled = true,
            CreatedAt = createdAt,
            UpdatedAt = null
        });
    }

    private static string GetConfiguredOptionNames<T>() {
        return string.Join(",", typeof(T).GetProperties().Select(x => x.Name));
    }

    private static string GetDefaultCollectOptionJson() {
        return """
    {
      "enableAutoReload": true,
      "enableSnapshotSave": true,
      "enableTimescaleHistorySave": true,
      "enableCurrentValueSave": true,
      "reloadIntervalSeconds": 3600,
      "saveIntervalMilliseconds": 1000,
      "maxBatchSize": 5000,
      "writerLogIntervalSeconds": 30,
      "defaultPublishingIntervalMs": 1000,
      "defaultSamplingIntervalMs": 1000,
      "defaultQueueSize": 1,
      "subscriptionKeepAliveCount": -1,
      "subscriptionLifetimeCount": -1,
      "maxNotificationsPerPublish": -1,
      "subscriptionPriority": 100,
      "discardOldest": true,
      "autoAcceptUntrustedCertificates": true,
      "rejectSHA1SignedCertificates": false,
      "minimumCertificateKeySize": 1024,
      "suppressNonceValidationErrors": true,
      "certificateStoreRootPath": "pki",
      "operationTimeoutMilliseconds": 6000000,
      "defaultSessionTimeoutMilliseconds": -1,
      "minSubscriptionLifetimeMilliseconds": -1,
      "maxStringLength": 2147483647,
      "maxByteStringLength": 2147483647,
      "maxArrayLength": 65535,
      "maxMessageSize": 419430400,
      "maxBufferSize": 65535,
      "channelLifetime": -1,
      "securityTokenLifetime": -1,
      "disableHiResClock": true,
      "defaultUseSecurity": false,
      "defaultUseAnonymous": true,
      "defaultSecurityPolicy": "",
      "defaultSecurityMode": ""
    }
    """;
    }

    private static string GetDefaultClientOptionJson() {
        return """
    {
      "applicationName": "WebFlexOpcCollector",
      "applicationUri": "urn:localhost:WebFlexOpcCollector",
      "productUri": "WebFlexOpcCollector",
      "applicationType": "Client",
      "disableHiResClock": true,

      "applicationCertificateStoreType": "Directory",
      "applicationCertificateStorePath": "pki/own",
      "applicationCertificateSubjectName": "WebFlexOpcCollector",
      "applicationCertificateThumbprint": "",

      "trustedPeerCertificatesStoreType": "Directory",
      "trustedPeerCertificatesStorePath": "pki/trusted",
      "trustedIssuerCertificatesStoreType": "Directory",
      "trustedIssuerCertificatesStorePath": "pki/issuer",
      "rejectedCertificateStoreType": "Directory",
      "rejectedCertificateStorePath": "pki/rejected",

      "autoAcceptUntrustedCertificates": true,
      "rejectSHA1SignedCertificates": false,
      "rejectUnknownRevocationStatus": false,
      "minimumCertificateKeySize": 1024,
      "addAppCertToTrustedStore": false,
      "suppressNonceValidationErrors": true,
      "sendCertificateChain": false,

      "operationTimeout": 6000000,
      "maxStringLength": 2147483647,
      "maxByteStringLength": 2147483647,
      "maxArrayLength": 65535,
      "maxMessageSize": 419430400,
      "maxBufferSize": 65535,
      "channelLifetime": -1,
      "securityTokenLifetime": -1,

      "defaultSessionTimeout": -1,
      "minSubscriptionLifetime": -1,
      "wellKnownDiscoveryUrls": "",
      "discoveryServers": "",
      "endpointCacheFilePath": "",

      "endpointUrl": "opc.tcp://127.0.0.1:49320",
      "useSecurity": false,
      "securityPolicyUri": "",
      "messageSecurityMode": "",
      "transportProfileUri": "",
      "endpointSelectionTimeout": 15000,

      "identityType": "Anonymous",
      "userName": "",
      "password": "",
      "certificateUserStoreType": "Directory",
      "certificateUserStorePath": "pki/user",
      "certificateUserSubjectName": "",

      "sessionName": "WebFlexOpcCollector",
      "sessionTimeout": 60000,
      "updateBeforeConnect": false,
      "checkDomain": false,
      "preferredLocales": "ko-KR,en-US",

      "publishingInterval": 1000,
      "lifetimeCount": 60,
      "keepAliveCount": 10,
      "maxNotificationsPerPublish": 0,
      "publishingEnabled": true,
      "priority": 100,

      "attributeId": "Value",
      "monitoringMode": "Reporting",
      "samplingInterval": 1000,
      "queueSize": 1,
      "discardOldest": true,
      "deadbandType": "None",
      "deadbandValue": 0,
      "dataChangeTrigger": "StatusValue",

      "browseNodeClassMask": "Object,Variable",
      "browseResultMask": "All",
      "browseMaxReferencesToReturn": 0,
      "readMaxAge": 0,
      "readTimestampsToReturn": "Both",

      "enableSessionKeepAlive": true,
      "keepAliveInterval": 5000,
      "reconnectPeriod": 10000,
      "maxReconnectAttempts": -1,

      "historyReadMode": "Raw",
      "historyReturnBounds": true,
      "historyReadModified": false,
      "historyNumValuesPerNode": 0,
      "historyTimestampsToReturn": "Both",
      "historyReleaseContinuationPoints": false,
      "historyMaxContinuationReads": 10,
      "historyDefaultRangeMinutes": 60
    }
    """;
    }

    private static void ApplyIndexes(ModelBuilder modelBuilder) {
        // 기존 index 유지
    }

    private static void ApplyDeleteBehaviors(ModelBuilder modelBuilder) {
        // 기존 FK delete behavior 유지
    }
}