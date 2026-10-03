namespace PjBudget.Shared.Database;

/// <summary>
/// Timestamps are maintained by <see cref="AuditingInterceptor"/>; entities never set them directly.
/// </summary>
public interface IAuditedEntity
{
	DateTime CreatedAt { get; set; }
	DateTime UpdatedAt { get; set; }
}
