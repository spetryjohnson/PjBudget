using Microsoft.EntityFrameworkCore;

namespace PjBudget.Shared.Database;

public static class DbContextExtensions
{
	/// <summary>
	/// Marks an aggregate root as modified when only its child rows changed, so its timestamp and version still
	/// advance and stale editors of the aggregate are rejected.
	/// </summary>
	public static void TouchRoot<TRoot>(this DbContext db, TRoot root)
		where TRoot : class, IAuditedEntity
	{
		db.Entry(root).Property(r => r.UpdatedAt).IsModified = true;
	}
}
