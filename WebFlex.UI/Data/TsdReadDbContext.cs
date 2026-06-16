using Microsoft.EntityFrameworkCore;
using WebFlex.Shared.Entities.Timeseries;

namespace WebFlex.UI.Data;

public class TsdReadDbContext : DbContext {
    public TsdReadDbContext(DbContextOptions<TsdReadDbContext> options)
        : base(options) {
    }

    public DbSet<TimescaleValue> TimescaleValues => Set<TimescaleValue>();
    public DbSet<TimescaleMinuteValue> TimescaleMinuteValues => Set<TimescaleMinuteValue>();
    public DbSet<CurrentValue> CurrentValues => Set<CurrentValue>();
    public DbSet<CurrentValueMinute> CurrentValueMinutes => Set<CurrentValueMinute>();

    protected override void OnModelCreating(ModelBuilder modelBuilder) {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<TimescaleValue>(entity => {
            entity.ToTable("timescale");
            entity.HasNoKey();

            entity.Property(x => x.Time).HasColumnName("time");
            entity.Property(x => x.EndpointUrl).HasColumnName("endpoint_url");
            entity.Property(x => x.NodeId).HasColumnName("node_id");
            entity.Property(x => x.Value).HasColumnName("value");
            entity.Property(x => x.Status).HasColumnName("status");
            entity.Property(x => x.SourceTimestamp).HasColumnName("source_timestamp");
            entity.Property(x => x.ReceivedAt).HasColumnName("received_at");
        });

        modelBuilder.Entity<TimescaleMinuteValue>(entity => {
            entity.ToTable("timescale_minute");
            entity.HasNoKey();

            entity.Property(x => x.Time).HasColumnName("time");
            entity.Property(x => x.EndpointUrl).HasColumnName("endpoint_url");
            entity.Property(x => x.NodeId).HasColumnName("node_id");
            entity.Property(x => x.Value).HasColumnName("value");
            entity.Property(x => x.Status).HasColumnName("status");
            entity.Property(x => x.SourceTimestamp).HasColumnName("source_timestamp");
            entity.Property(x => x.ReceivedAt).HasColumnName("received_at");
        });

        modelBuilder.Entity<CurrentValue>(entity => {
            entity.ToTable("currentvalue");
            entity.HasNoKey();

            entity.Property(x => x.EndpointUrl).HasColumnName("endpoint_url");
            entity.Property(x => x.NodeId).HasColumnName("node_id");
            entity.Property(x => x.Value).HasColumnName("value");
            entity.Property(x => x.Status).HasColumnName("status");
            entity.Property(x => x.SourceTimestamp).HasColumnName("source_timestamp");
            entity.Property(x => x.ReceivedAt).HasColumnName("received_at");
            entity.Property(x => x.UpdatedAt).HasColumnName("updated_at");
        });

        modelBuilder.Entity<CurrentValueMinute>(entity => {
            entity.ToTable("currentvalue_minute");
            entity.HasNoKey();

            entity.Property(x => x.EndpointUrl).HasColumnName("endpoint_url");
            entity.Property(x => x.NodeId).HasColumnName("node_id");
            entity.Property(x => x.MinuteTime).HasColumnName("minute_time");
            entity.Property(x => x.Value).HasColumnName("value");
            entity.Property(x => x.Status).HasColumnName("status");
            entity.Property(x => x.SourceTimestamp).HasColumnName("source_timestamp");
            entity.Property(x => x.ReceivedAt).HasColumnName("received_at");
            entity.Property(x => x.UpdatedAt).HasColumnName("updated_at");
        });
    }
}