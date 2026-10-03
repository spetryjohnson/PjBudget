using Microsoft.EntityFrameworkCore;
using PjBudget.Shared.Database;

namespace PjBudget.Features.Scenarios;

public static class ScenarioSeeder
{
	public const string DefaultScenarioName = "Current plan";

	/// <summary>
	/// Every plan record belongs to a scenario, so a current scenario must exist before anything else is saved.
	/// </summary>
	public static async Task<Scenario> EnsureCurrentScenarioAsync(AppDbContext db, CancellationToken ct = default)
	{
		var current = await db.Scenarios.SingleOrDefaultAsync(s => s.IsCurrent, ct);
		if (current is not null)
		{
			return current;
		}

		current = new Scenario { Name = DefaultScenarioName, IsCurrent = true };
		db.Scenarios.Add(current);
		await db.SaveChangesAsync(ct);
		return current;
	}
}
