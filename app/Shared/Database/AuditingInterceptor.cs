using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using PjBudget.Shared.Infrastructure;

namespace PjBudget.Shared.Database;

/// <summary>
/// Stamps <see cref="IAuditedEntity"/> timestamps and rotates <see cref="IVersionedEntity"/> versions on save.
/// EF compares the version's original value in the UPDATE's WHERE clause, so rotating it here is what turns a
/// concurrent write into a <see cref="DbUpdateConcurrencyException"/>.
/// </summary>
public sealed class AuditingInterceptor : SaveChangesInterceptor
{
	private readonly ISystemClock _clock;

	public AuditingInterceptor(ISystemClock clock) => _clock = clock;

	public override InterceptionResult<int> SavingChanges(DbContextEventData eventData, InterceptionResult<int> result)
	{
		Stamp(eventData.Context);
		return result;
	}

	public override ValueTask<InterceptionResult<int>> SavingChangesAsync(
		DbContextEventData eventData, InterceptionResult<int> result, CancellationToken cancellationToken = default)
	{
		Stamp(eventData.Context);
		return ValueTask.FromResult(result);
	}

	private void Stamp(DbContext? context)
	{
		if (context is null)
		{
			return;
		}

		// Interceptors run before SaveChanges detects changes, so property edits wouldn't show up as Modified yet.
		context.ChangeTracker.DetectChanges();

		var now = _clock.UtcNow;
		foreach (var entry in context.ChangeTracker.Entries())
		{
			if (entry.State is not (EntityState.Added or EntityState.Modified))
			{
				continue;
			}

			if (entry.Entity is IAuditedEntity audited)
			{
				if (entry.State == EntityState.Added)
				{
					audited.CreatedAt = now;
				}
				audited.UpdatedAt = now;
			}

			if (entry.Entity is IVersionedEntity versioned)
			{
				versioned.Version = Guid.NewGuid();
			}
		}
	}
}
