using Microsoft.EntityFrameworkCore;
using WebFlex.Shared;

namespace WebFlex.OpcCollector.Data;

public class WebFlexConfigDbContext : BaseDbContext {
    public WebFlexConfigDbContext(DbContextOptions<WebFlexConfigDbContext> options)
        : base(options) {
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder) {
        base.OnModelCreating(modelBuilder);

        var entityTypes = typeof(BaseEntity).Assembly
            .GetTypes()
            .Where(t =>
                t.IsClass &&
                !t.IsAbstract &&
                typeof(BaseEntity).IsAssignableFrom(t));

        foreach (var entityType in entityTypes) {
            modelBuilder.Entity(entityType);
        }
    }
}