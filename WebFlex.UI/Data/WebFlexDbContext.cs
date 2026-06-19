using Microsoft.EntityFrameworkCore;
using WebFlex.Shared.Entities.Opc;
using WebFlex.Shared.Entities.System;

namespace WebFlex.UI.Data;

public class WebFlexDbContext : DbContext {
    public WebFlexDbContext(DbContextOptions<WebFlexDbContext> options)
        : base(options) {
    }

    public DbSet<OpcMajorGroup> OpcMajorGroups => Set<OpcMajorGroup>();
    public DbSet<OpcGroup> OpcGroups => Set<OpcGroup>();
    public DbSet<OpcDevice> OpcDevices => Set<OpcDevice>();
    public DbSet<OpcTag> OpcTags => Set<OpcTag>();
    public DbSet<OpcCollectOption> OpcCollectOptions => Set<OpcCollectOption>();
    public DbSet<OpcCollectRuntimeStatus> OpcCollectRuntimeStatuses => Set<OpcCollectRuntimeStatus>();

    public DbSet<SUser> SUsers => Set<SUser>();
    public DbSet<SRole> SRoles => Set<SRole>();
    public DbSet<SUserRole> SUserRoles => Set<SUserRole>();
    public DbSet<SMenu> SMenus => Set<SMenu>();
    public DbSet<SRoleMenu> SRoleMenus => Set<SRoleMenu>();
    public DbSet<OpcClientOption> OpcClientOptions => Set<OpcClientOption>();

    protected override void OnModelCreating(ModelBuilder modelBuilder) {
        base.OnModelCreating(modelBuilder);

        ConfigureOpc(modelBuilder);
        ConfigureSystem(modelBuilder);
    }

    private static void ConfigureOpc(ModelBuilder modelBuilder) {
        modelBuilder.Entity<OpcClientOption>(entity => {
            entity.ToTable("opc_client_option");

            entity.HasKey(x => x.Id);

            entity.Property(x => x.Id).HasColumnName("id");
            entity.Property(x => x.OptionCode).HasColumnName("option_code").HasMaxLength(100).IsRequired();
            entity.Property(x => x.OptionName).HasColumnName("option_name").HasMaxLength(200).IsRequired();
            entity.Property(x => x.OptionJson).HasColumnName("option_json").IsRequired();
            entity.Property(x => x.ConfiguredOptionNames).HasColumnName("configured_option_names");
            entity.Property(x => x.Description).HasColumnName("description");
            entity.Property(x => x.IsEnabled).HasColumnName("is_enabled");
            entity.Property(x => x.CreatedAt).HasColumnName("created_at");
            entity.Property(x => x.UpdatedAt).HasColumnName("updated_at");

            entity.HasIndex(x => x.OptionCode).IsUnique();
        });

        modelBuilder.Entity<OpcMajorGroup>(entity => {
            entity.ToTable("opc_major_group");

            entity.HasKey(x => x.Id);

            entity.Property(x => x.Id).HasColumnName("id");
            entity.Property(x => x.MajorGroupCode).HasColumnName("major_group_code").HasMaxLength(100).IsRequired();
            entity.Property(x => x.MajorGroupName).HasColumnName("major_group_name").HasMaxLength(200).IsRequired();
            entity.Property(x => x.SortOrder).HasColumnName("sort_order");
            entity.Property(x => x.Description).HasColumnName("description");
            entity.Property(x => x.IsEnabled).HasColumnName("is_enabled");
            entity.Property(x => x.CreatedAt).HasColumnName("created_at");
            entity.Property(x => x.UpdatedAt).HasColumnName("updated_at");

            entity.HasIndex(x => x.MajorGroupCode).IsUnique();
        });

        modelBuilder.Entity<OpcGroup>(entity => {
            entity.ToTable("opc_group");

            entity.HasKey(x => x.Id);

            entity.Property(x => x.Id).HasColumnName("id");
            entity.Property(x => x.OpcMajorGroupId).HasColumnName("opc_major_group_id");
            entity.Property(x => x.GroupCode).HasColumnName("group_code").HasMaxLength(100).IsRequired();
            entity.Property(x => x.GroupName).HasColumnName("group_name").HasMaxLength(200).IsRequired();
            entity.Property(x => x.SortOrder).HasColumnName("sort_order");
            entity.Property(x => x.Description).HasColumnName("description");
            entity.Property(x => x.IsEnabled).HasColumnName("is_enabled");
            entity.Property(x => x.CreatedAt).HasColumnName("created_at");
            entity.Property(x => x.UpdatedAt).HasColumnName("updated_at");

            entity.HasOne(x => x.MajorGroup)
                .WithMany(x => x.Groups)
                .HasForeignKey(x => x.OpcMajorGroupId)
                .OnDelete(DeleteBehavior.SetNull);

            entity.HasIndex(x => x.GroupCode).IsUnique();
        });

        modelBuilder.Entity<OpcDevice>(entity => {
            entity.ToTable("opc_device");

            entity.HasKey(x => x.Id);

            entity.Property(x => x.Id).HasColumnName("id");
            entity.Property(x => x.OpcGroupId).HasColumnName("opc_group_id");
            entity.Property(x => x.DeviceCode).HasColumnName("device_code").HasMaxLength(100).IsRequired();
            entity.Property(x => x.DeviceName).HasColumnName("device_name").HasMaxLength(200).IsRequired();
            entity.Property(x => x.DeviceAddress).HasColumnName("device_address").HasMaxLength(200).IsRequired();
            entity.Property(x => x.Port).HasColumnName("port");
            entity.Property(x => x.EndpointUrl).HasColumnName("endpoint_url").HasMaxLength(300).IsRequired();
            entity.Property(x => x.DeviceType).HasColumnName("device_type").HasMaxLength(50).IsRequired();
            entity.Property(x => x.IsCollectEnabled).HasColumnName("is_collect_enabled");
            entity.Property(x => x.UseSecurity).HasColumnName("use_security");
            entity.Property(x => x.SecurityPolicy).HasColumnName("security_policy").HasMaxLength(100);
            entity.Property(x => x.SecurityMode).HasColumnName("security_mode").HasMaxLength(100);
            entity.Property(x => x.UseAnonymous).HasColumnName("use_anonymous");
            entity.Property(x => x.UserName).HasColumnName("user_name").HasMaxLength(100);
            entity.Property(x => x.Password).HasColumnName("password").HasMaxLength(500);
            entity.Property(x => x.PublishingIntervalMs).HasColumnName("publishing_interval_ms");
            entity.Property(x => x.SamplingIntervalMs).HasColumnName("sampling_interval_ms");
            entity.Property(x => x.QueueSize).HasColumnName("queue_size");
            entity.Property(x => x.SortOrder).HasColumnName("sort_order");
            entity.Property(x => x.Description).HasColumnName("description");
            entity.Property(x => x.IsEnabled).HasColumnName("is_enabled");
            entity.Property(x => x.CreatedAt).HasColumnName("created_at");
            entity.Property(x => x.UpdatedAt).HasColumnName("updated_at");

            entity.HasOne(x => x.Group)
                .WithMany(x => x.Devices)
                .HasForeignKey(x => x.OpcGroupId)
                .OnDelete(DeleteBehavior.SetNull);

            entity.HasIndex(x => x.DeviceCode).IsUnique();
            entity.HasIndex(x => x.EndpointUrl).IsUnique();
        });

        modelBuilder.Entity<OpcTag>(entity => {
            entity.ToTable("opc_tag");

            entity.HasKey(x => x.Id);

            entity.Property(x => x.Id).HasColumnName("id");
            entity.Property(x => x.OpcDeviceId).HasColumnName("opc_device_id");
            entity.Property(x => x.OpcGroupId).HasColumnName("opc_group_id");
            entity.Property(x => x.TagCode).HasColumnName("tag_code").HasMaxLength(100).IsRequired();
            entity.Property(x => x.NodeId).HasColumnName("node_id").HasMaxLength(500).IsRequired();
            entity.Property(x => x.DisplayName).HasColumnName("display_name").HasMaxLength(300).IsRequired();
            entity.Property(x => x.GroupName).HasColumnName("group_name").HasMaxLength(300);
            entity.Property(x => x.DataType).HasColumnName("data_type").HasMaxLength(100);
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

            entity.HasOne(x => x.Device)
                .WithMany(x => x.Tags)
                .HasForeignKey(x => x.OpcDeviceId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne(x => x.Group)
                .WithMany(x => x.Tags)
                .HasForeignKey(x => x.OpcGroupId)
                .OnDelete(DeleteBehavior.SetNull);

            entity.HasIndex(x => new { x.OpcDeviceId, x.NodeId }).IsUnique();
            entity.HasIndex(x => x.TagCode);
        });

        modelBuilder.Entity<OpcCollectOption>(entity => {
            entity.ToTable("opc_collect_option");

            entity.HasKey(x => x.Id);

            entity.Property(x => x.Id).HasColumnName("id");
            entity.Property(x => x.OptionCode).HasColumnName("option_code").HasMaxLength(100).IsRequired();
            entity.Property(x => x.OptionName).HasColumnName("option_name").HasMaxLength(200).IsRequired();
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

            entity.HasIndex(x => x.OptionCode).IsUnique();
        });

        modelBuilder.Entity<OpcCollectRuntimeStatus>(entity => {
            entity.ToTable("opc_collect_runtime_status");

            entity.HasKey(x => x.Id);

            entity.Property(x => x.Id).HasColumnName("id");
            entity.Property(x => x.OpcDeviceId).HasColumnName("opc_device_id");
            entity.Property(x => x.EndpointUrl).HasColumnName("endpoint_url").HasMaxLength(300).IsRequired();
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

            entity.HasOne(x => x.Device)
                .WithMany()
                .HasForeignKey(x => x.OpcDeviceId)
                .OnDelete(DeleteBehavior.SetNull);

            entity.HasIndex(x => x.EndpointUrl).IsUnique();
        });
    }

    private static void ConfigureSystem(ModelBuilder modelBuilder) {
        modelBuilder.Entity<SUser>(entity => {
            entity.ToTable("s_user");

            entity.HasKey(x => x.Id);

            entity.Property(x => x.Id).HasColumnName("id");
            entity.Property(x => x.UserId).HasColumnName("user_id").HasMaxLength(100).IsRequired();
            entity.Property(x => x.UserName).HasColumnName("user_name").HasMaxLength(100).IsRequired();
            entity.Property(x => x.PasswordHash).HasColumnName("password_hash").HasMaxLength(500);
            entity.Property(x => x.Email).HasColumnName("email").HasMaxLength(200);
            entity.Property(x => x.PhoneNumber).HasColumnName("phone_number").HasMaxLength(50);
            entity.Property(x => x.IsAdmin).HasColumnName("is_admin");
            entity.Property(x => x.LastLoginAt).HasColumnName("last_login_at");
            entity.Property(x => x.IsEnabled).HasColumnName("is_enabled");
            entity.Property(x => x.CreatedAt).HasColumnName("created_at");
            entity.Property(x => x.UpdatedAt).HasColumnName("updated_at");

            entity.HasIndex(x => x.UserId).IsUnique();
        });

        modelBuilder.Entity<SRole>(entity => {
            entity.ToTable("s_role");

            entity.HasKey(x => x.Id);

            entity.Property(x => x.Id).HasColumnName("id");
            entity.Property(x => x.RoleCode).HasColumnName("role_code").HasMaxLength(100).IsRequired();
            entity.Property(x => x.RoleName).HasColumnName("role_name").HasMaxLength(100).IsRequired();
            entity.Property(x => x.Description).HasColumnName("description");
            entity.Property(x => x.IsEnabled).HasColumnName("is_enabled");
            entity.Property(x => x.CreatedAt).HasColumnName("created_at");
            entity.Property(x => x.UpdatedAt).HasColumnName("updated_at");

            entity.HasIndex(x => x.RoleCode).IsUnique();
        });

        modelBuilder.Entity<SUserRole>(entity => {
            entity.ToTable("s_user_role");

            entity.HasKey(x => x.Id);

            entity.Property(x => x.Id).HasColumnName("id");
            entity.Property(x => x.SUserId).HasColumnName("s_user_id");
            entity.Property(x => x.SRoleId).HasColumnName("s_role_id");
            entity.Property(x => x.IsEnabled).HasColumnName("is_enabled");
            entity.Property(x => x.CreatedAt).HasColumnName("created_at");
            entity.Property(x => x.UpdatedAt).HasColumnName("updated_at");

            entity.HasOne(x => x.User)
                .WithMany(x => x.UserRoles)
                .HasForeignKey(x => x.SUserId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne(x => x.Role)
                .WithMany(x => x.UserRoles)
                .HasForeignKey(x => x.SRoleId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasIndex(x => new { x.SUserId, x.SRoleId }).IsUnique();
        });

        modelBuilder.Entity<SMenu>(entity => {
            entity.ToTable("s_menu");

            entity.HasKey(x => x.Id);

            entity.Property(x => x.Id).HasColumnName("id");
            entity.Property(x => x.ParentMenuId).HasColumnName("parent_menu_id");
            entity.Property(x => x.MenuCode).HasColumnName("menu_code").HasMaxLength(100).IsRequired();
            entity.Property(x => x.MenuName).HasColumnName("menu_name").HasMaxLength(100).IsRequired();
            entity.Property(x => x.ControllerName).HasColumnName("controller_name").HasMaxLength(100);
            entity.Property(x => x.ActionName).HasColumnName("action_name").HasMaxLength(100);
            entity.Property(x => x.Url).HasColumnName("url").HasMaxLength(300);
            entity.Property(x => x.Icon).HasColumnName("icon").HasMaxLength(100);
            entity.Property(x => x.SortOrder).HasColumnName("sort_order");
            entity.Property(x => x.ShowInMenu).HasColumnName("show_in_menu");
            entity.Property(x => x.IsEnabled).HasColumnName("is_enabled");
            entity.Property(x => x.CreatedAt).HasColumnName("created_at");
            entity.Property(x => x.UpdatedAt).HasColumnName("updated_at");

            entity.HasOne(x => x.ParentMenu)
                .WithMany(x => x.Children)
                .HasForeignKey(x => x.ParentMenuId)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasIndex(x => x.MenuCode).IsUnique();
        });

        modelBuilder.Entity<SRoleMenu>(entity => {
            entity.ToTable("s_role_menu");

            entity.HasKey(x => x.Id);

            entity.Property(x => x.Id).HasColumnName("id");
            entity.Property(x => x.SRoleId).HasColumnName("s_role_id");
            entity.Property(x => x.SMenuId).HasColumnName("s_menu_id");
            entity.Property(x => x.CanRead).HasColumnName("can_read");
            entity.Property(x => x.CanCreate).HasColumnName("can_create");
            entity.Property(x => x.CanUpdate).HasColumnName("can_update");
            entity.Property(x => x.CanDelete).HasColumnName("can_delete");
            entity.Property(x => x.CanExport).HasColumnName("can_export");
            entity.Property(x => x.IsEnabled).HasColumnName("is_enabled");
            entity.Property(x => x.CreatedAt).HasColumnName("created_at");
            entity.Property(x => x.UpdatedAt).HasColumnName("updated_at");

            entity.HasOne(x => x.Role)
                .WithMany(x => x.RoleMenus)
                .HasForeignKey(x => x.SRoleId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne(x => x.Menu)
                .WithMany(x => x.RoleMenus)
                .HasForeignKey(x => x.SMenuId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasIndex(x => new { x.SRoleId, x.SMenuId }).IsUnique();
        });
    }
}