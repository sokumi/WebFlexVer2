using Microsoft.EntityFrameworkCore;
using System.Reflection;
using System.Text.RegularExpressions;
using WebFlex.Shared;
using WebFlex.Shared.Entities;
using WebFlex.Shared.Dtos.Opc;
using System.Security.Cryptography;
using System.Text;
using WebFlex.Shared.Entities.System;

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

        ApplySystemSeedData(modelBuilder, createdAt);

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

    private static void ApplySystemSeedData(ModelBuilder modelBuilder, DateTime createdAt) {
        var allRoles = new[] {
        "DevAuth",
        "OPCAuth",
        "TestAuth",
        "FileAuth",
        "FileUserAuth",
        "WebflexUser",
        "UserAuth",
        "BizAuth",
        "WOPCUserAuth"
    };

        modelBuilder.Entity<SRole>().HasData(
            new SRole { ID = "DevAuth", ROLE_CODE = "DevAuth", ROLE_NAME = "개발자 권한", DESCRIPTION = "최고 개발자 권한", IsEnabled = true, CreatedAt = createdAt },
            new SRole { ID = "OPCAuth", ROLE_CODE = "OPCAuth", ROLE_NAME = "컨설턴트 및 운영 사용자 권한", IsEnabled = true, CreatedAt = createdAt },
            new SRole { ID = "TestAuth", ROLE_CODE = "TestAuth", ROLE_NAME = "테스트 권한", IsEnabled = true, CreatedAt = createdAt },
            new SRole { ID = "FileAuth", ROLE_CODE = "FileAuth", ROLE_NAME = "파일 모니터링 관리자 권한", IsEnabled = true, CreatedAt = createdAt },
            new SRole { ID = "FileUserAuth", ROLE_CODE = "FileUserAuth", ROLE_NAME = "파일 모니터링 유저 권한", IsEnabled = true, CreatedAt = createdAt },
            new SRole { ID = "WebflexUser", ROLE_CODE = "WebflexUser", ROLE_NAME = "사내 개발자 권한", IsEnabled = true, CreatedAt = createdAt },
            new SRole { ID = "UserAuth", ROLE_CODE = "UserAuth", ROLE_NAME = "고객사 유저 권한", IsEnabled = true, CreatedAt = createdAt },
            new SRole { ID = "BizAuth", ROLE_CODE = "BizAuth", ROLE_NAME = "고객사 운영 관리자 권한", IsEnabled = true, CreatedAt = createdAt },
            new SRole { ID = "WOPCUserAuth", ROLE_CODE = "WOPCUserAuth", ROLE_NAME = "일반 사용자 권한", DESCRIPTION = "회원가입 사용자 권한", IsEnabled = true, CreatedAt = createdAt }
        );

        modelBuilder.Entity<SUser>().HasData(
            new SUser { ID = "U_DEV", USER_ID = "dev", USER_NAME = "개발자", PASSWORD_HASH = HashPassword("opc344!!"), IS_ADMIN = true, IsEnabled = true, CreatedAt = createdAt },
            new SUser { ID = "U_OPC", USER_ID = "opc", USER_NAME = "OPC 운영자", PASSWORD_HASH = HashPassword("opc2026!"), IS_ADMIN = false, IsEnabled = true, CreatedAt = createdAt },
            new SUser { ID = "U_TEST", USER_ID = "test", USER_NAME = "테스트 사용자", PASSWORD_HASH = HashPassword("test2026!"), IS_ADMIN = false, IsEnabled = true, CreatedAt = createdAt },
            new SUser { ID = "U_DEVOPC", USER_ID = "devopc", USER_NAME = "사내 개발자", PASSWORD_HASH = HashPassword("devopc2026!"), IS_ADMIN = false, IsEnabled = true, CreatedAt = createdAt },
            new SUser { ID = "U_ADMIN", USER_ID = "admin", USER_NAME = "운영 관리자", PASSWORD_HASH = HashPassword("admin2026!"), IS_ADMIN = true, IsEnabled = true, CreatedAt = createdAt }
        );

        modelBuilder.Entity<SUserRole>().HasData(
            new SUserRole { ID = "UR001", USER_UID = "U_DEV", ROLE_ID = "DevAuth", IsEnabled = true, CreatedAt = createdAt },
            new SUserRole { ID = "UR002", USER_UID = "U_OPC", ROLE_ID = "OPCAuth", IsEnabled = true, CreatedAt = createdAt },
            new SUserRole { ID = "UR003", USER_UID = "U_TEST", ROLE_ID = "TestAuth", IsEnabled = true, CreatedAt = createdAt },
            new SUserRole { ID = "UR004", USER_UID = "U_DEVOPC", ROLE_ID = "WebflexUser", IsEnabled = true, CreatedAt = createdAt },
            new SUserRole { ID = "UR005", USER_UID = "U_ADMIN", ROLE_ID = "BizAuth", IsEnabled = true, CreatedAt = createdAt }
        );

        modelBuilder.Entity<SMenu>().HasData(
     new SMenu { ID = "DASHBOARD", MENU_CODE = "DASHBOARD", MENU_NAME = "DASHBOARD", ICON = "layout-dashboard", SORT_ORDER = 10, SHOW_IN_MENU = true, IsEnabled = true, CreatedAt = createdAt },
     new SMenu { ID = "DASH_MAIN", PARENT_MENU_ID = "DASHBOARD", MENU_CODE = "DASHBOARD.MAIN", MENU_NAME = "MAIN", CONTROLLER_NAME = "Main", ACTION_NAME = "Index", URL = "/", ICON = "home", SORT_ORDER = 11, SHOW_IN_MENU = true, IsEnabled = true, CreatedAt = createdAt },
     new SMenu { ID = "DASH_CARD", PARENT_MENU_ID = "DASHBOARD", MENU_CODE = "DASHBOARD.CARD", MENU_NAME = "카드 대시보드", CONTROLLER_NAME = "Main", ACTION_NAME = "DBD2000", URL = "/main/dbd2000", ICON = "panel-top", SORT_ORDER = 12, SHOW_IN_MENU = true, IsEnabled = true, CreatedAt = createdAt },

     new SMenu { ID = "SERVICE", MENU_CODE = "SERVICE", MENU_NAME = "SERVICE", ICON = "activity", SORT_ORDER = 20, SHOW_IN_MENU = true, IsEnabled = true, CreatedAt = createdAt },
     new SMenu { ID = "SERVICE_DATA", PARENT_MENU_ID = "SERVICE", MENU_CODE = "SERVICE.DATA", MENU_NAME = "데이터 조회", URL = "/service/data", ICON = "database", SORT_ORDER = 21, SHOW_IN_MENU = true, IsEnabled = true, CreatedAt = createdAt },
     new SMenu { ID = "SERVICE_TREND", PARENT_MENU_ID = "SERVICE", MENU_CODE = "SERVICE.TREND", MENU_NAME = "트렌드 조회", URL = "/service/trend", ICON = "chart-line", SORT_ORDER = 22, SHOW_IN_MENU = true, IsEnabled = true, CreatedAt = createdAt },

     new SMenu { ID = "DEVICE", MENU_CODE = "DEVICE", MENU_NAME = "DEVICE", ICON = "cpu", SORT_ORDER = 30, SHOW_IN_MENU = true, IsEnabled = true, CreatedAt = createdAt },
     new SMenu { ID = "DEVICE_MANAGE", PARENT_MENU_ID = "DEVICE", MENU_CODE = "DEVICE.MANAGE", MENU_NAME = "디바이스 관리", CONTROLLER_NAME = "Device", ACTION_NAME = "DVC1000", URL = "/device/dvc1000", ICON = "server", SORT_ORDER = 31, SHOW_IN_MENU = true, IsEnabled = true, CreatedAt = createdAt },
     new SMenu { ID = "DEVICE_TAG", PARENT_MENU_ID = "DEVICE", MENU_CODE = "DEVICE.TAG", MENU_NAME = "태그 관리", CONTROLLER_NAME = "Device", ACTION_NAME = "DVC1010", URL = "/device/dvc1010", ICON = "tags", SORT_ORDER = 32, SHOW_IN_MENU = true, IsEnabled = true, CreatedAt = createdAt },
     new SMenu { ID = "DEVICE_GROUP", PARENT_MENU_ID = "DEVICE", MENU_CODE = "DEVICE.GROUP", MENU_NAME = "그룹 관리", CONTROLLER_NAME = "Device", ACTION_NAME = "DVC1020", URL = "/device/dvc1020", ICON = "folder-tree", SORT_ORDER = 33, SHOW_IN_MENU = true, IsEnabled = true, CreatedAt = createdAt },

     new SMenu { ID = "OPC", MENU_CODE = "OPC", MENU_NAME = "OPC", ICON = "network", SORT_ORDER = 40, SHOW_IN_MENU = true, IsEnabled = true, CreatedAt = createdAt },
     new SMenu { ID = "OPC_COLLECT", PARENT_MENU_ID = "OPC", MENU_CODE = "OPC.COLLECT", MENU_NAME = "OPC 수집 관리", CONTROLLER_NAME = "Opc", ACTION_NAME = "OPC1000", URL = "/opc/opc1000", ICON = "radio-tower", SORT_ORDER = 41, SHOW_IN_MENU = true, IsEnabled = true, CreatedAt = createdAt },

     new SMenu { ID = "SYSTEM", MENU_CODE = "SYSTEM", MENU_NAME = "SYSTEM", ICON = "settings", SORT_ORDER = 50, SHOW_IN_MENU = true, IsEnabled = true, CreatedAt = createdAt },
     new SMenu { ID = "SYSTEM_SVC", PARENT_MENU_ID = "SYSTEM", MENU_CODE = "SYSTEM.SERVICE", MENU_NAME = "Windows Service", CONTROLLER_NAME = "System", ACTION_NAME = "SVC1000", URL = "/system/svc1000", ICON = "hard-drive", SORT_ORDER = 51, SHOW_IN_MENU = true, IsEnabled = true, CreatedAt = createdAt },

     new SMenu { ID = "OPC_OPTIONS", MENU_CODE = "OPC_OPTIONS", MENU_NAME = "OPC OPTIONS", ICON = "sliders-horizontal", SORT_ORDER = 60, SHOW_IN_MENU = true, IsEnabled = true, CreatedAt = createdAt },
     new SMenu { ID = "OPTION_COLLECT", PARENT_MENU_ID = "OPC_OPTIONS", MENU_CODE = "OPC_OPTIONS.COLLECTOR", MENU_NAME = "Collector 옵션", CONTROLLER_NAME = "Opc", ACTION_NAME = "OPC1020", URL = "/opc/opc1020", ICON = "settings-2", SORT_ORDER = 61, SHOW_IN_MENU = true, IsEnabled = true, CreatedAt = createdAt },
     new SMenu { ID = "OPTION_CLIENT", PARENT_MENU_ID = "OPC_OPTIONS", MENU_CODE = "OPC_OPTIONS.CLIENT", MENU_NAME = "Client 옵션", CONTROLLER_NAME = "Opc", ACTION_NAME = "OPC1030", URL = "/opc/opc1030", ICON = "plug", SORT_ORDER = 62, SHOW_IN_MENU = true, IsEnabled = true, CreatedAt = createdAt },
     new SMenu { ID = "OPTION_HISTORY", PARENT_MENU_ID = "OPC_OPTIONS", MENU_CODE = "OPC_OPTIONS.HISTORY", MENU_NAME = "History 조회", CONTROLLER_NAME = "Opc", ACTION_NAME = "OPC3000", URL = "/opc/opc3000", ICON = "history", SORT_ORDER = 63, SHOW_IN_MENU = true, IsEnabled = true, CreatedAt = createdAt },
     new SMenu { ID = "OPTION_TSD", PARENT_MENU_ID = "OPC_OPTIONS", MENU_CODE = "OPC_OPTIONS.TIMESCALE", MENU_NAME = "Timescale 설정", CONTROLLER_NAME = "Opc", ACTION_NAME = "OPC4000", URL = "/opc/opc4000", ICON = "database-zap", SORT_ORDER = 64, SHOW_IN_MENU = true, IsEnabled = true, CreatedAt = createdAt },

     new SMenu { ID = "OPTIONS", MENU_CODE = "OPTIONS", MENU_NAME = "OPTIONS", ICON = "sliders-horizontal", SORT_ORDER = 70, SHOW_IN_MENU = true, IsEnabled = true, CreatedAt = createdAt },
     new SMenu { ID = "OPTION_CARD", PARENT_MENU_ID = "OPTIONS", MENU_CODE = "OPTIONS.CARD", MENU_NAME = "카드 대시보드 옵션", CONTROLLER_NAME = "Option", ACTION_NAME = "OPT1000", URL = "/option/opt1000", ICON = "panel-top", SORT_ORDER = 71, SHOW_IN_MENU = true, IsEnabled = true, CreatedAt = createdAt }
 );

        var roleMenus = new List<SRoleMenu>();
        var seq = 1;

        var dashboardMenuIds = new[] {
    "DASHBOARD", "DASH_MAIN", "DASH_CARD"
};

        var serviceMenuIds = new[] {
    "SERVICE", "SERVICE_DATA", "SERVICE_TREND"
};

        var deviceMenuIds = new[] {
    "DEVICE", "DEVICE_MANAGE", "DEVICE_TAG", "DEVICE_GROUP"
};

        var opcMenuIds = new[] {
    "OPC", "OPC_COLLECT",
    "OPC_OPTIONS", "OPTION_COLLECT", "OPTION_CLIENT", "OPTION_HISTORY", "OPTION_TSD"
};

        var systemMenuIds = new[] {
    "SYSTEM", "SYSTEM_SVC"
};

        var optionMenuIds = new[] {
    "OPTIONS", "OPTION_CARD"
};

        var mainRoleIds = new[] { "DevAuth", "BizAuth", "TestAuth" };

        AddRoleMenus(roleMenus, ref seq, mainRoleIds, dashboardMenuIds, createdAt);
        AddRoleMenus(roleMenus, ref seq, mainRoleIds, serviceMenuIds, createdAt);
        AddRoleMenus(roleMenus, ref seq, mainRoleIds, deviceMenuIds, createdAt);
        AddRoleMenus(roleMenus, ref seq, mainRoleIds, opcMenuIds, createdAt);
        AddRoleMenus(roleMenus, ref seq, mainRoleIds, systemMenuIds, createdAt);
        AddRoleMenus(roleMenus, ref seq, mainRoleIds, optionMenuIds, createdAt);

        modelBuilder.Entity<SRoleMenu>().HasData(roleMenus);
    }

    private static void AddRoleMenus(
        List<SRoleMenu> roleMenus,
        ref int seq,
        IEnumerable<string> roleIds,
        IEnumerable<string> menuIds,
        DateTime createdAt) {
        foreach (var roleId in roleIds) {
            foreach (var menuId in menuIds) {
                roleMenus.Add(new SRoleMenu {
                    ID = $"RM{seq:0000}",
                    ROLE_ID = roleId,
                    MENU_ID = menuId,
                    CAN_READ = true,
                    CAN_CREATE = true,
                    CAN_UPDATE = true,
                    CAN_DELETE = true,
                    CAN_EXPORT = true,
                    IsEnabled = true,
                    CreatedAt = createdAt
                });

                seq++;
            }
        }
    }

    private static string HashPassword(string password) {
        var bytes = SHA256.HashData(Encoding.UTF8.GetBytes(password));
        return Convert.ToHexString(bytes);
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