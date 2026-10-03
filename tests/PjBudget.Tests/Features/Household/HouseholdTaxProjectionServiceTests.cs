using PjBudget.Features.Household;
using PjBudget.Features.Payroll.Engine;
using PjBudget.Features.Payroll.Engine.States;
using PjBudget.Features.TaxYears;
using PjBudget.Shared.Database;
using PjBudget.Tests.TestSupport;

namespace PjBudget.Tests.Features.Household;

public class HouseholdTaxProjectionServiceTests
{
	private static HouseholdTaxProjectionService Service(AppDbContext db) => new(
		db, TestServices.Simulations(db), new TaxYearService(db), new HouseholdTaxProjector(new StateTaxModules([new OhioTaxModule()])));

	[Test]
	public async Task ProjectsTheScenariosSavedSources()
	{
		using var database = new TestDatabase();
		var baseline = await TestServices.SeedBaselineAsync(database);
		await TestServices.PayrollSources(database.CreateContext())
			.CreateAsync(baseline.ScenarioId, TestServices.SalariedSource(baseline), CancellationToken.None);

		var projection = await Service(database.CreateContext()).ProjectAsync(baseline.ScenarioId, 2026, CancellationToken.None);

		var federal = projection.Sections.Single(s => s.Title == "Federal");
		Assert.Multiple(() =>
		{
			// The synthetic source has 4,250 of federal wages and 328.33 of federal withholding on each of 24 checks.
			Assert.That(federal.Lines.Single(l => l.Label == "Wages").Amount, Is.EqualTo(102_000m));
			Assert.That(federal.Withheld, Is.EqualTo(7_879.92m));
			Assert.That(projection.Sections.Select(s => s.Title), Is.EqualTo(new[] { "Federal", "Ohio", "School district", "City" }));
		});
	}
}
