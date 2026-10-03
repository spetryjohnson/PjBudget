using PjBudget.Shared.Errors;

namespace PjBudget.Shared.Database;

/// <summary>
/// An aggregate root whose updates are gated on the version the client last loaded. The version changes on every
/// save, so an editor holding a stale copy gets a conflict instead of silently overwriting a newer edit.
/// </summary>
public interface IVersionedEntity
{
	Guid Version { get; set; }
}

public static class VersionedEntityExtensions
{
	public static void EnsureVersion(this IVersionedEntity entity, Guid expectedVersion, string description)
	{
		if (entity.Version != expectedVersion)
		{
			throw new ConflictException($"{description} was changed since you loaded it. Reload and try again.");
		}
	}
}
