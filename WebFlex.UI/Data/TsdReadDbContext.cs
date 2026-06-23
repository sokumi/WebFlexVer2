using Microsoft.EntityFrameworkCore;
using WebFlex.Shared;

namespace WebFlex.UI.Data;

public class TsdReadDbContext : DbContext {
    public TsdReadDbContext(DbContextOptions<TsdReadDbContext> options)
        : base(options) {
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder) {
        base.OnModelCreating(modelBuilder);

        ApplyTsdEntities(modelBuilder);
    }

    private static void ApplyTsdEntities(ModelBuilder modelBuilder) {
        modelBuilder.Entity<TimescaleValue>(entity => {
            entity.ToTable("timescale");
            entity.HasKey(x => new { x.Time, x.TAG_ID });

            entity.Property(x => x.Time).HasColumnName("time");
            entity.Property(x => x.TAG_ID).HasColumnName("tag_id").HasMaxLength(15);
            entity.Property(x => x.GROUP_ID).HasColumnName("group_id").HasMaxLength(15);
            entity.Property(x => x.STATUS).HasColumnName("status");
            entity.Property(x => x.VALUE).HasColumnName("value");
            entity.Property(x => x.COOKIE_VALUE).HasColumnName("cookie_value");
            entity.Property(x => x.SourceTimestamp).HasColumnName("source_timestamp");
            entity.Property(x => x.ReceivedAt).HasColumnName("received_at");
        });

        modelBuilder.Entity<TimescaleMinuteValue>(entity => {
            entity.ToTable("timescale_minute");
            entity.HasKey(x => new { x.Time, x.TAG_ID });

            entity.Property(x => x.Time).HasColumnName("time");
            entity.Property(x => x.TAG_ID).HasColumnName("tag_id").HasMaxLength(15);
            entity.Property(x => x.GROUP_ID).HasColumnName("group_id").HasMaxLength(15);
            entity.Property(x => x.STATUS).HasColumnName("status");
            entity.Property(x => x.VALUE).HasColumnName("value");
            entity.Property(x => x.COOKIE_VALUE).HasColumnName("cookie_value");
            entity.Property(x => x.SourceTimestamp).HasColumnName("source_timestamp");
            entity.Property(x => x.ReceivedAt).HasColumnName("received_at");
        });

        modelBuilder.Entity<CurrentValue>(entity => {
            entity.ToTable("currentvalue");
            entity.HasKey(x => x.TAG_ID);

            entity.Property(x => x.TAG_ID).HasColumnName("tag_id").HasMaxLength(15);
            entity.Property(x => x.GROUP_ID).HasColumnName("group_id").HasMaxLength(15);
            entity.Property(x => x.STATUS).HasColumnName("status");
            entity.Property(x => x.VALUE).HasColumnName("value");
            entity.Property(x => x.COOKIE_VALUE).HasColumnName("cookie_value");
            entity.Property(x => x.UPDATE_COUNT).HasColumnName("update_count");
            entity.Property(x => x.SOURCETIMESTAMP).HasColumnName("source_timestamp");
            entity.Property(x => x.RECEIVEDAT).HasColumnName("received_at");
            entity.Property(x => x.UPDATEDAT).HasColumnName("updated_at");
        });

        modelBuilder.Entity<CurrentValueMinute>(entity => {
            entity.ToTable("currentvalue_minute");
            entity.HasKey(x => x.TAG_ID);

            entity.Property(x => x.TAG_ID).HasColumnName("tag_id").HasMaxLength(15);
            entity.Property(x => x.GROUP_ID).HasColumnName("group_id").HasMaxLength(15);
            entity.Property(x => x.STATUS).HasColumnName("status");
            entity.Property(x => x.VALUE).HasColumnName("value");
            entity.Property(x => x.COOKIE_VALUE).HasColumnName("cookie_value");
            entity.Property(x => x.UPDATE_COUNT).HasColumnName("update_count");
            entity.Property(x => x.SOURCETIMESTAMP).HasColumnName("source_timestamp");
            entity.Property(x => x.RECEIVEDAT).HasColumnName("received_at");
            entity.Property(x => x.UPDATEDAT).HasColumnName("updated_at");
        });
    }
}