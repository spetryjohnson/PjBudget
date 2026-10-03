using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using PjBudget.Shared.Database;

namespace PjBudget.Tests.Shared;

public class MigrationTests
{
	[Test]
	public void MigrationsApplyToAnEmptyDatabaseAndMatchTheModel()
	{
		using var connection = new SqliteConnection("DataSource=:memory:");
		connection.Open();
		using var db = new AppDbContext(new DbContextOptionsBuilder<AppDbContext>().UseSqlite(connection).Options);

		db.Database.Migrate();

		Assert.That(db.Database.HasPendingModelChanges(), Is.False,
			"The model has changes that aren't in a migration. Run 'dotnet ef migrations add <Name>' from the app folder.");
	}
}
