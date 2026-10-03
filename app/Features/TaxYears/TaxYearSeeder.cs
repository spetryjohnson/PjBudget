using Microsoft.EntityFrameworkCore;
using PjBudget.Shared.Database;

namespace PjBudget.Features.TaxYears;

public static class TaxYearSeeder
{
	/// <summary>
	/// Seeds only into an empty table, so edits made in the UI are never overwritten by a later deploy.
	/// </summary>
	public static async Task EnsureSeededAsync(AppDbContext db, CancellationToken ct = default)
	{
		if (await db.TaxYears.AnyAsync(ct))
		{
			return;
		}

		db.TaxYears.Add(TaxYear2026.Create());
		await db.SaveChangesAsync(ct);
	}
}
