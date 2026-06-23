//using Microsoft.EntityFrameworkCore;
//using Microsoft.EntityFrameworkCore.Design; 
//using System.ComponentModel.DataAnnotations.Schema;
//using System.Data.Common;
//using System.Reflection;
//using static WebFlex.UI.Controllers.MVCPath;
//using WebFlex.Shared.Entities.Timeseries;

//namespace WebFlex.IoTGateway.DbAccess;

///// <summary>
///// IoT Gateway 데이터베이스 컨텍스트
///// </summary>
//public class IoTDbContext : BaseDbContext {
//    public IoTDbContext() {
//    }

//    public IoTDbContext(DbConnection dbConnection)
//        : base(dbConnection) {
//    }

//    public IoTDbContext(DbContextOptions<IoTDbContext> options)
//        : base(options) {
//    }


//    protected override void OnModelCreating(ModelBuilder modelBuilder) {
//        base.OnModelCreating(modelBuilder);

//        ApplyModelEntities(modelBuilder);
//        ApplySeedData(modelBuilder);
//        ApplyIndexes(modelBuilder);
//        ApplyDeleteBehaviors(modelBuilder);
//    }

//    /// <summary>
//    /// WebFlex.IoTGateway.Model assembly 안에서 [Table]이 붙은 모델을 자동 등록한다.
//    /// </summary>
//    private static void ApplyModelEntities(ModelBuilder modelBuilder) {
//        var modelAssembly = typeof(Device).Assembly;

//        var entityTypes = modelAssembly
//            .GetTypes()
//            .Where(type =>
//                type is { IsClass: true, IsAbstract: false } &&
//                type.GetCustomAttribute<TableAttribute>() != null);

//        foreach (var type in entityTypes) {
//            modelBuilder.Entity(type);
//        }
//    }

//    /// <summary>
//    /// 기본 데이터 등록.
//    /// 기존 솔루션처럼 이곳의 HasData를 수정하면 Add-Migration 시 InsertData/UpdateData/DeleteData로 반영된다.
//    /// </summary>
//    private static void ApplySeedData(ModelBuilder modelBuilder) {
//        SeedAdapters(modelBuilder);
//        SeedAdapterParameters(modelBuilder);
//    }

//    private static void SeedAdapters(ModelBuilder modelBuilder) {
//        var createdAt = new DateTimeOffset(2026, 1, 1, 0, 0, 0, TimeSpan.Zero);

//        modelBuilder.Entity<Adapter>().HasData(
//          new() { ADAPTER_ID = "OPCUA", ADAPTER_TYPE = "OPCUA", ADAPTER_NAME = "OPC UA", ASSEMBLY_NAME = "WebFlex.IoTGateway.OPCUADriver", TYPE_NAME = "WebFlex.IoTGateway.OPCUADriver.OpcUaDriver", USE_YN = "Y", REMARK = "OPC UA 기본 드라이버", CREATED_AT = createdAt },
//          new() { ADAPTER_ID = "MODBUS_TCP", ADAPTER_TYPE = "MODBUS_TCP", ADAPTER_NAME = "Modbus TCP", ASSEMBLY_NAME = null, TYPE_NAME = null, USE_YN = "Y", REMARK = "Modbus TCP 기본 드라이버", CREATED_AT = createdAt }
//        );
//    }

//    private static void SeedAdapterParameters(ModelBuilder modelBuilder) {
//        modelBuilder.Entity<AdapterParameter>().HasData(
//            new() { PARAMETER_ID = "AP_OPCUA_ENDPOINT", ADAPTER_ID = "OPCUA", PARAMETER_KEY = "endpoint_url", PARAMETER_NAME = "Endpoint Url", DATA_TYPE = "string", REQUIRED_YN = "Y", DEFAULT_VALUE = "opc.tcp://localhost:49320", SORT_ORDER = 1, DESCRIPTION = "OPC UA 서버 Endpoint" },
//            new() { PARAMETER_ID = "AP_OPCUA_SECURITY_MODE", ADAPTER_ID = "OPCUA", PARAMETER_KEY = "security_mode", PARAMETER_NAME = "Security Mode", DATA_TYPE = "string", REQUIRED_YN = "N", DEFAULT_VALUE = "None", SORT_ORDER = 2, DESCRIPTION = "OPC UA 보안 모드" },
//            new() { PARAMETER_ID = "AP_OPCUA_SECURITY_POLICY", ADAPTER_ID = "OPCUA", PARAMETER_KEY = "security_policy", PARAMETER_NAME = "Security Policy", DATA_TYPE = "string", REQUIRED_YN = "N", DEFAULT_VALUE = "None", SORT_ORDER = 3, DESCRIPTION = "OPC UA 보안 정책" },
//            new() { PARAMETER_ID = "AP_OPCUA_USERNAME", ADAPTER_ID = "OPCUA", PARAMETER_KEY = "username", PARAMETER_NAME = "User Name", DATA_TYPE = "string", REQUIRED_YN = "N", DEFAULT_VALUE = null, SORT_ORDER = 4, DESCRIPTION = "OPC UA 사용자명" },
//            new() { PARAMETER_ID = "AP_OPCUA_PASSWORD", ADAPTER_ID = "OPCUA", PARAMETER_KEY = "password", PARAMETER_NAME = "Password", DATA_TYPE = "password", REQUIRED_YN = "N", DEFAULT_VALUE = null, SORT_ORDER = 5, DESCRIPTION = "OPC UA 비밀번호" },

//            new() { PARAMETER_ID = "AP_MODBUS_HOST", ADAPTER_ID = "MODBUS_TCP", PARAMETER_KEY = "host", PARAMETER_NAME = "Host", DATA_TYPE = "string", REQUIRED_YN = "Y", DEFAULT_VALUE = "127.0.0.1", SORT_ORDER = 1, DESCRIPTION = "Modbus TCP Host" },
//            new() { PARAMETER_ID = "AP_MODBUS_PORT", ADAPTER_ID = "MODBUS_TCP", PARAMETER_KEY = "port", PARAMETER_NAME = "Port", DATA_TYPE = "int", REQUIRED_YN = "Y", DEFAULT_VALUE = "502", SORT_ORDER = 2, DESCRIPTION = "Modbus TCP Port" },
//            new() { PARAMETER_ID = "AP_MODBUS_UNIT_ID", ADAPTER_ID = "MODBUS_TCP", PARAMETER_KEY = "unit_id", PARAMETER_NAME = "Unit Id", DATA_TYPE = "int", REQUIRED_YN = "N", DEFAULT_VALUE = "1", SORT_ORDER = 3, DESCRIPTION = "Modbus Unit Id" }
//        );
//    }

//    private static void ApplyIndexes(ModelBuilder modelBuilder) {
//        modelBuilder.Entity<AdapterParameter>().HasIndex(x => new { x.ADAPTER_ID, x.PARAMETER_KEY }).IsUnique().HasDatabaseName("ux_iot_adapter_parameter_adapter_key");

//        modelBuilder.Entity<Device>().HasIndex(x => x.ADAPTER_ID).HasDatabaseName("ix_iot_device_adapter_id");
//        modelBuilder.Entity<Device>().HasIndex(x => new { x.USE_YN, x.COLLECT_YN }).HasDatabaseName("ix_iot_device_use_collect");

//        modelBuilder.Entity<DeviceConnectionOption>().HasIndex(x => new { x.DEVICE_ID, x.OPTION_KEY }).IsUnique().HasDatabaseName("ux_iot_device_connection_option_device_key");

//        modelBuilder.Entity<DeviceNode>().HasIndex(x => new { x.DEVICE_ID, x.SOURCE_NODE_ID }).IsUnique().HasDatabaseName("ux_iot_device_node_device_source");

//        modelBuilder.Entity<DeviceTag>().HasIndex(x => new { x.DEVICE_ID, x.SOURCE_ADDRESS }).IsUnique().HasDatabaseName("ux_iot_device_tag_device_source");
//        modelBuilder.Entity<DeviceTag>().HasIndex(x => new { x.DEVICE_ID, x.USE_YN, x.COLLECT_YN }).HasDatabaseName("ix_iot_device_tag_collect");

//        modelBuilder.Entity<DeviceStatusHistory>().HasIndex(x => new { x.DEVICE_ID, x.POINT }).HasDatabaseName("ix_iot_device_status_history_device_point");

//        modelBuilder.Entity<MetrieMeasurement>().HasIndex(x => x.GROUP_ID).HasDatabaseName("ix_iot_metrie_measurement_group_id");

//        modelBuilder.Entity<MetrieField>().HasIndex(x => x.MEASUREMENT_ID).HasDatabaseName("ix_iot_metrie_field_measurement_id");
//        modelBuilder.Entity<MetrieField>().HasIndex(x => x.TAG_ID).HasDatabaseName("ix_iot_metrie_field_tag_id");
//        modelBuilder.Entity<MetrieField>().HasIndex(x => new { x.USE_YN, x.SAVE_YN, x.MQTT_PUBLISH_YN }).HasDatabaseName("ix_iot_metrie_field_use_policy");

//        modelBuilder.Entity<Timeseries>().HasKey(x => new { x.POINT, x.FIELD_ID });
//        modelBuilder.Entity<Timeseries>().HasIndex(x => new { x.FIELD_ID, x.POINT }).HasDatabaseName("ix_iot_timeseries_field_point");
//        modelBuilder.Entity<Timeseries>().HasIndex(x => new { x.DEVICE_ID, x.POINT }).HasDatabaseName("ix_iot_timeseries_device_point");
//        modelBuilder.Entity<Timeseries>().HasIndex(x => new { x.TAG_ID, x.POINT }).HasDatabaseName("ix_iot_timeseries_tag_point");

//        modelBuilder.Entity<CurrentValue>().HasIndex(x => x.DEVICE_ID).HasDatabaseName("ix_iot_current_value_device_id");
//        modelBuilder.Entity<CurrentValue>().HasIndex(x => x.UPDATED_AT).HasDatabaseName("ix_iot_current_value_updated_at");
//    }

//    /// <summary>
//    /// FK 삭제 동작 설정.
//    ///
//    /// 실시간 대량 저장 테이블은 cascade 삭제를 피한다.
//    /// 특히 iot_timeseries는 수집 이력 테이블이므로 마스터 삭제와 함께 자동 삭제되면 위험하다.
//    /// </summary>
//    private static void ApplyDeleteBehaviors(ModelBuilder modelBuilder) {
//        modelBuilder.Entity<AdapterParameter>().HasOne(x => x.Adapter).WithMany(x => x.AdapterParameters).HasForeignKey(x => x.ADAPTER_ID).OnDelete(DeleteBehavior.Cascade);

//        modelBuilder.Entity<Device>().HasOne(x => x.Adapter).WithMany().HasForeignKey(x => x.ADAPTER_ID).OnDelete(DeleteBehavior.Restrict);

//        modelBuilder.Entity<DeviceConnectionOption>().HasOne(x => x.Device).WithMany(x => x.DeviceConnectionOptions).HasForeignKey(x => x.DEVICE_ID).OnDelete(DeleteBehavior.Cascade);

//        modelBuilder.Entity<DeviceNode>().HasOne(x => x.Device).WithMany(x => x.DeviceNodes).HasForeignKey(x => x.DEVICE_ID).OnDelete(DeleteBehavior.Cascade);

//        modelBuilder.Entity<DeviceTag>().HasOne(x => x.Device).WithMany(x => x.DeviceTags).HasForeignKey(x => x.DEVICE_ID).OnDelete(DeleteBehavior.Cascade);
//        modelBuilder.Entity<DeviceTag>().HasOne(x => x.DeviceNode).WithMany().HasForeignKey(x => x.NODE_KEY).OnDelete(DeleteBehavior.SetNull);

//        modelBuilder.Entity<MetrieMeasurement>().HasOne(x => x.MetrieGroup).WithMany(x => x.MetrieMeasurements).HasForeignKey(x => x.GROUP_ID).OnDelete(DeleteBehavior.Cascade);

//        modelBuilder.Entity<MetrieField>().HasOne(x => x.MetrieMeasurement).WithMany(x => x.MetrieFields).HasForeignKey(x => x.MEASUREMENT_ID).OnDelete(DeleteBehavior.Cascade);
//        modelBuilder.Entity<MetrieField>().HasOne(x => x.DeviceTag).WithMany().HasForeignKey(x => x.TAG_ID).OnDelete(DeleteBehavior.Restrict);

//        modelBuilder.Entity<CurrentValue>().HasOne(x => x.MetrieField).WithOne().HasForeignKey<CurrentValue>(x => x.FIELD_ID).OnDelete(DeleteBehavior.Cascade);
//        modelBuilder.Entity<CurrentValue>().HasOne(x => x.DeviceTag).WithMany().HasForeignKey(x => x.TAG_ID).OnDelete(DeleteBehavior.Restrict);
//        modelBuilder.Entity<CurrentValue>().HasOne(x => x.Device).WithMany().HasForeignKey(x => x.DEVICE_ID).OnDelete(DeleteBehavior.Restrict);

//        modelBuilder.Entity<Timeseries>().HasOne(x => x.MetrieField).WithMany().HasForeignKey(x => x.FIELD_ID).OnDelete(DeleteBehavior.Restrict);
//        modelBuilder.Entity<Timeseries>().HasOne(x => x.DeviceTag).WithMany().HasForeignKey(x => x.TAG_ID).OnDelete(DeleteBehavior.Restrict);
//        modelBuilder.Entity<Timeseries>().HasOne(x => x.Device).WithMany().HasForeignKey(x => x.DEVICE_ID).OnDelete(DeleteBehavior.Restrict);
//    }
//}

///// <summary>
///// DesignTimeFactory for EF Migration.
///// EF Core Tools will find this class and use the connection defined in appsettings.json
///// to run Add-Migration and Update-Database.
///// </summary>
//public class DataContextFactory : IDesignTimeDbContextFactory<IoTDbContext> {
//    private readonly IConfigurationRoot _configRoot;

//    public DataContextFactory() {
//        var builder = new ConfigurationBuilder()
//            .SetBasePath(Directory.GetCurrentDirectory())
//            .AddJsonFile("appsettings.json", optional: true, reloadOnChange: true)
//            .AddEnvironmentVariables();

//        _configRoot = builder.Build();
//    }

//    public IoTDbContext CreateDbContext(string[] args) {
//        var dbConnection = _configRoot
//            .GetSection("Connections")
//            .Get<List<DbConnection>>()?
//            .First(x => x.IsDefault);

//        return new IoTDbContext(dbConnection);
//    }

//    public static DbConnection GetDefaultConnection(IConfiguration configuration) {
//        var list = new List<DbConnection>();

//        configuration.GetSection("Connections").Bind(list);

//        return list.First(x => x.IsDefault);
//    }
//}