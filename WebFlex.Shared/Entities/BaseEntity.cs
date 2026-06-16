namespace WebFlex.Shared.Entities;

public abstract class BaseEntity {
    public long Id { get; set; }

    public bool IsEnabled { get; set; } = true;

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public DateTime? UpdatedAt { get; set; }
}