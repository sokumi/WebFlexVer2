using Microsoft.EntityFrameworkCore;
using System.Data.Common;

namespace WebFlex.IoTGateway.DbAccess;

/// <summary>
/// WebFlex 공통 데이터베이스 컨텍스트
/// </summary>
public abstract class BaseDbContext : Microsoft.EntityFrameworkCore.DbContext {
    protected DbConnection? DbConnection { get; private set; }

    protected BaseDbContext() {
    }

    protected BaseDbContext(DbContextOptions options)
        : base(options) {
    }

    protected BaseDbContext(DbConnection dbConnection) {
        DbConnection = dbConnection;
    }

    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder) {
        if (optionsBuilder.IsConfigured) {
            base.OnConfiguring(optionsBuilder);
            return;
        }

        if (DbConnection == null) {
            base.OnConfiguring(optionsBuilder);
            return;
        }

        ConfigureDbProvider(optionsBuilder, DbConnection);

        base.OnConfiguring(optionsBuilder);
    }

    public static void ConfigureDbProvider(
     DbContextOptionsBuilder optionsBuilder,
     DbConnection dbConnection) {
        if (string.IsNullOrWhiteSpace(dbConnection.ConnectionString)) {
            throw new InvalidOperationException(
                $"ConnectionString is empty. Connection name: {dbConnection.Name}");
        }

        if (string.IsNullOrWhiteSpace(dbConnection.MigrationAsmName)) {
            throw new InvalidOperationException(
                $"MigrationAsmName is empty. Connection name: {dbConnection.Name}");
        }

        switch (dbConnection.DbType.ToLowerInvariant()) {
            case "pgsql":
            case "postgresql":
                optionsBuilder.UseNpgsql(
                    dbConnection.ConnectionString,
                    options => {
                        options.MigrationsAssembly(dbConnection.MigrationAsmName);
                        options.MigrationsHistoryTable("s_migration_history");
                    });
                break;

            default:
                throw new NotSupportedException(
                    $"Unsupported DbType: {dbConnection.DbType}");
        }

        if (dbConnection.IsDebug) {
            optionsBuilder.EnableDetailedErrors();
            optionsBuilder.EnableSensitiveDataLogging();
        }
    }

    public override int SaveChanges() {
        SetAuditFields();
        return base.SaveChanges();
    }

    public override int SaveChanges(bool acceptAllChangesOnSuccess) {
        SetAuditFields();
        return base.SaveChanges(acceptAllChangesOnSuccess);
    }

    public override Task<int> SaveChangesAsync(CancellationToken cancellationToken = default) {
        SetAuditFields();
        return base.SaveChangesAsync(cancellationToken);
    }

    public override Task<int> SaveChangesAsync(
        bool acceptAllChangesOnSuccess,
        CancellationToken cancellationToken = default) {
        SetAuditFields();
        return base.SaveChangesAsync(acceptAllChangesOnSuccess, cancellationToken);
    }

    private void SetAuditFields() {
        var now = DateTimeOffset.UtcNow;

        foreach (var entry in ChangeTracker.Entries<IBasePoco>()) {
            if (entry.State == EntityState.Added) {
                if (entry.Entity.CREATED_AT == default) {
                    entry.Entity.CREATED_AT = now;
                }

                entry.Entity.UPDATED_AT = now;
            }

            if (entry.State == EntityState.Modified) {
                entry.Entity.UPDATED_AT = now;
            }
        }
    }
}