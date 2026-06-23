using Microsoft.EntityFrameworkCore;
using WebFlex.Shared.Entities;

namespace WebFlex.OpcCollector.Data;

public abstract class BaseDbContext : DbContext {
    protected BaseDbContext() {
    }

    protected BaseDbContext(DbContextOptions options)
        : base(options) {
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
        var now = DateTime.UtcNow;

        foreach (var entry in ChangeTracker.Entries<BaseEntity>()) {
            if (entry.State == EntityState.Added) {
                if (entry.Entity.CreatedAt == default)
                    entry.Entity.CreatedAt = now;

                entry.Entity.UpdatedAt = now;
            }

            if (entry.State == EntityState.Modified) {
                entry.Entity.UpdatedAt = now;
            }
        }
    }
}