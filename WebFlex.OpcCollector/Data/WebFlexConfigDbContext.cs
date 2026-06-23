using Microsoft.EntityFrameworkCore;
using WebFlex.Shared;

namespace WebFlex.OpcCollector.Data;

public class WebFlexConfigDbContext : DbContext {
    public WebFlexConfigDbContext(DbContextOptions<WebFlexConfigDbContext> options)
        : base(options) {
    }

    public DbSet<OpcMajorGroup> OpcMajorGroups => Set<OpcMajorGroup>();
    public DbSet<OpcGroup> OpcGroups => Set<OpcGroup>();
    public DbSet<OpcDevice> OpcDevices => Set<OpcDevice>();
    public DbSet<OpcTag> OpcTags => Set<OpcTag>();
    public DbSet<OpcCollectOption> OpcCollectOptions => Set<OpcCollectOption>();
    public DbSet<OpcCollectRuntimeStatus> OpcCollectRuntimeStatuses => Set<OpcCollectRuntimeStatus>();
    public DbSet<OpcClientOption> OpcClientOptions => Set<OpcClientOption>();

    protected override void OnModelCreating(ModelBuilder modelBuilder) {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<OpcClientOption>(entity => {
            entity.ToTable("opc_client_option");

            entity.HasKey(x => x.Id);

            entity.Property(x => x.Id).HasColumnName("id");
            entity.Property(x => x.OptionCode).HasColumnName("option_code");
            entity.Property(x => x.OptionName).HasColumnName("option_name");
            entity.Property(x => x.OptionJson).HasColumnName("option_json");
            entity.Property(x => x.ConfiguredOptionNames).HasColumnName("configured_option_names");
            entity.Property(x => x.Description).HasColumnName("description");
            entity.Property(x => x.IsEnabled).HasColumnName("is_enabled");
            entity.Property(x => x.CreatedAt).HasColumnName("created_at");
            entity.Property(x => x.UpdatedAt).HasColumnName("updated_at");
        });

        modelBuilder.Entity<OpcMajorGroup>(entity => {
            entity.ToTable("opc_major_group");
            entity.HasKey(x => x.Id);

            entity.Property(x => x.Id).HasColumnName("id");
            entity.Property(x => x.MajorGroupCode).HasColumnName("major_group_code");
            entity.Property(x => x.MajorGroupName).HasColumnName("major_group_name");
            entity.Property(x => x.SortOrder).HasColumnName("sort_order");
            entity.Property(x => x.Description).HasColumnName("description");
            entity.Property(x => x.IsEnabled).HasColumnName("is_enabled");
            entity.Property(x => x.CreatedAt).HasColumnName("created_at");
            entity.Property(x => x.UpdatedAt).HasColumnName("updated_at");
        });

        modelBuilder.Entity<OpcGroup>(entity => {
            entity.ToTable("opc_group");
            entity.HasKey(x => x.Id);

            entity.Property(x => x.Id).HasColumnName("id");
            entity.Property(x => x.OpcMajorGroupId).HasColumnName("opc_major_group_id");
            entity.Property(x => x.GroupCode).HasColumnName("group_code");
            entity.Property(x => x.GroupName).HasColumnName("group_name");
            entity.Property(x => x.SortOrder).HasColumnName("sort_order");
            entity.Property(x => x.Description).HasColumnName("description");
            entity.Property(x => x.IsEnabled).HasColumnName("is_enabled");
            entity.Property(x => x.CreatedAt).HasColumnName("created_at");
            entity.Property(x => x.UpdatedAt).HasColumnName("updated_at");
        });

        modelBuilder.Entity<OpcDevice>(entity => {
            entity.ToTable("opc_device");
            entity.HasKey(x => x.Id);

            entity.Property(x => x.Id).HasColumnName("id");
            entity.Property(x => x.OpcGroupId).HasColumnName("opc_group_id");
            entity.Property(x => x.DeviceCode).HasColumnName("device_code");
            entity.Property(x => x.DeviceName).HasColumnName("device_name");
            entity.Property(x => x.DeviceAddress).HasColumnName("device_address");
            entity.Property(x => x.Port).HasColumnName("port");
            entity.Property(x => x.EndpointUrl).HasColumnName("endpoint_url");
            entity.Property(x => x.DeviceType).HasColumnName("device_type");
            entity.Property(x => x.IsCollectEnabled).HasColumnName("is_collect_enabled");
            entity.Property(x => x.UseSecurity).HasColumnName("use_security");
            entity.Property(x => x.SecurityPolicy).HasColumnName("security_policy");
            entity.Property(x => x.SecurityMode).HasColumnName("security_mode");
            entity.Property(x => x.UseAnonymous).HasColumnName("use_anonymous");
            entity.Property(x => x.UserName).HasColumnName("user_name");
            entity.Property(x => x.Password).HasColumnName("password");
            entity.Property(x => x.PublishingIntervalMs).HasColumnName("publishing_interval_ms");
            entity.Property(x => x.SamplingIntervalMs).HasColumnName("sampling_interval_ms");
            entity.Property(x => x.QueueSize).HasColumnName("queue_size");
            entity.Property(x => x.SortOrder).HasColumnName("sort_order");
            entity.Property(x => x.Description).HasColumnName("description");
            entity.Property(x => x.IsEnabled).HasColumnName("is_enabled");
            entity.Property(x => x.CreatedAt).HasColumnName("created_at");
            entity.Property(x => x.UpdatedAt).HasColumnName("updated_at");

            entity.HasMany(x => x.Tags)
                .WithOne(x => x.Device)
                .HasForeignKey(x => x.OpcDeviceId);
        });

        modelBuilder.Entity<OpcTag>(entity => {
            entity.ToTable("opc_tag");
            entity.HasKey(x => x.Id);

            entity.Property(x => x.Id).HasColumnName("id");
            entity.Property(x => x.OpcDeviceId).HasColumnName("opc_device_id");
            entity.Property(x => x.OpcGroupId).HasColumnName("opc_group_id");
            entity.Property(x => x.TagCode).HasColumnName("tag_code");
            entity.Property(x => x.NodeId).HasColumnName("node_id");
            entity.Property(x => x.DisplayName).HasColumnName("display_name");
            entity.Property(x => x.GroupName).HasColumnName("group_name");
            entity.Property(x => x.DataType).HasColumnName("data_type");
            entity.Property(x => x.IsCollectEnabled).HasColumnName("is_collect_enabled");
            entity.Property(x => x.SaveToDatabase).HasColumnName("save_to_database");
            entity.Property(x => x.ShowOnDashboard).HasColumnName("show_on_dashboard");
            entity.Property(x => x.SamplingIntervalMs).HasColumnName("sampling_interval_ms");
            entity.Property(x => x.QueueSize).HasColumnName("queue_size");
            entity.Property(x => x.SortOrder).HasColumnName("sort_order");
            entity.Property(x => x.Description).HasColumnName("description");
            entity.Property(x => x.IsEnabled).HasColumnName("is_enabled");
            entity.Property(x => x.CreatedAt).HasColumnName("created_at");
            entity.Property(x => x.UpdatedAt).HasColumnName("updated_at");
        });

        modelBuilder.Entity<OpcCollectOption>(entity => {
            entity.ToTable("opc_collect_option");
            entity.HasKey(x => x.Id);

            entity.Property(x => x.Id).HasColumnName("id");
            entity.Property(x => x.OptionCode).HasColumnName("option_code");
            entity.Property(x => x.OptionName).HasColumnName("option_name");
            entity.Property(x => x.ReloadIntervalSeconds).HasColumnName("reload_interval_seconds");
            entity.Property(x => x.SaveIntervalMilliseconds).HasColumnName("save_interval_milliseconds");
            entity.Property(x => x.FlushIntervalMilliseconds).HasColumnName("flush_interval_milliseconds");
            entity.Property(x => x.MaxBatchSize).HasColumnName("max_batch_size");
            entity.Property(x => x.SaveOnlyChangedValue).HasColumnName("save_only_changed_value");
            entity.Property(x => x.AutoReconnect).HasColumnName("auto_reconnect");
            entity.Property(x => x.ReconnectIntervalSeconds).HasColumnName("reconnect_interval_seconds");
            entity.Property(x => x.ConnectionCheckIntervalSeconds).HasColumnName("connection_check_interval_seconds");
            entity.Property(x => x.Description).HasColumnName("description");
            entity.Property(x => x.IsEnabled).HasColumnName("is_enabled");
            entity.Property(x => x.CreatedAt).HasColumnName("created_at");
            entity.Property(x => x.UpdatedAt).HasColumnName("updated_at");
        });

        modelBuilder.Entity<OpcCollectRuntimeStatus>(entity => {
            entity.ToTable("opc_collect_runtime_status");
            entity.HasKey(x => x.Id);

            entity.Property(x => x.Id).HasColumnName("id");
            entity.Property(x => x.OpcDeviceId).HasColumnName("opc_device_id");
            entity.Property(x => x.EndpointUrl).HasColumnName("endpoint_url");
            entity.Property(x => x.IsConnected).HasColumnName("is_connected");
            entity.Property(x => x.SubscribedCount).HasColumnName("subscribed_count");
            entity.Property(x => x.LastConnectedAt).HasColumnName("last_connected_at");
            entity.Property(x => x.LastDisconnectedAt).HasColumnName("last_disconnected_at");
            entity.Property(x => x.LastReceivedAt).HasColumnName("last_received_at");
            entity.Property(x => x.LastErrorMessage).HasColumnName("last_error_message");
            entity.Property(x => x.StatusUpdatedAt).HasColumnName("status_updated_at");
            entity.Property(x => x.IsEnabled).HasColumnName("is_enabled");
            entity.Property(x => x.CreatedAt).HasColumnName("created_at");
            entity.Property(x => x.UpdatedAt).HasColumnName("updated_at");
        });
    }
}