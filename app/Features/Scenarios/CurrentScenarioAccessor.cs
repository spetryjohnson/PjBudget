using Microsoft.EntityFrameworkCore;
using PjBudget.Shared.Database;

namespace PjBudget.Features.Scenarios;

/// <summary>
/// Resolves which scenario a request operates on. Phase 1 has no scenario picker, so this is always the current
/// scenario; services take the id explicitly so a picker can be added without changing them.
/// </summary>
public interface ICurrentScenarioAccessor
{
	Task<int> GetCurrentScenarioIdAsync(CancellationToken ct);
}

public sealed class CurrentScenarioAccessor : ICurrentScenarioAccessor
{
	private readonly AppDbContext _db;
	private int? _currentScenarioId;

	public CurrentScenarioAccessor(AppDbContext db) => _db = db;

	public async Task<int> GetCurrentScenarioIdAsync(CancellationToken ct)
	{
		_currentScenarioId ??= await _db.Scenarios
			.Where(s => s.IsCurrent)
			.Select(s => s.Id)
			.SingleAsync(ct);

		return _currentScenarioId.Value;
	}
}
