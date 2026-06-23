using Microsoft.EntityFrameworkCore;
using System.Reflection;
using System.Text.RegularExpressions;
using WebFlex.Shared;
using WebFlex.Shared.Entities;

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
        // 기존 seed 유지
    }

    private static void ApplyIndexes(ModelBuilder modelBuilder) {
        // 기존 index 유지
    }

    private static void ApplyDeleteBehaviors(ModelBuilder modelBuilder) {
        // 기존 FK delete behavior 유지
    }
}