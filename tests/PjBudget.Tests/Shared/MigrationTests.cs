using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using PjBudget.Features.Payroll.Engine;
using PjBudget.Framework;
using PjBudget.Shared.Database;
using PjBudget.Shared.Domain;

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

	[Test]
	public void PayrollSourcesSavedBeforeTheLifeInsuranceSettingsGetTheDefaults()
	{
		using var connection = new SqliteConnection("DataSource=:memory:");
		connection.Open();
		using var db = new AppDbContext(new DbContextOptionsBuilder<AppDbContext>().UseSqlite(connection).Options);
		db.Database.Migrate();

		// SQLite fills existing rows from the column default when a column is added.
		string? ColumnDefault(string column)
		{
			using var command = connection.CreateCommand();
			command.CommandText = "SELECT dflt_value FROM pragma_table_info('PayrollSources') WHERE name = $column";
			command.Parameters.AddWithValue("$column", column);
			return command.ExecuteScalar() as string;
		}

		Assert.Multiple(() =>
		{
			Assert.That(ColumnDefault("GroupTermLifeTaxedFor")!.Trim('\'').ToEnum<TaxableWageTypes>(),
				Is.EqualTo(DefaultTaxTreatment.GroupTermLife));
			Assert.That(ColumnDefault("WithholdsSchoolDistrictTax"), Is.EqualTo("1"), "existing sources keep withholding school tax");
		});
	}
}
