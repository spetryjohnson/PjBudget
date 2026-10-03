using PjBudget.Shared.Errors;
using PjBudget.Tests.TestSupport;

namespace PjBudget.Tests.Features.Payroll;

public class PayrollSimulationServiceTests
{
	private TestDatabase _database = null!;

	[SetUp]
	public void SetUp() => _database = new TestDatabase();

	[TearDown]
	public void TearDown() => _database.Dispose();

	[Test]
	public async Task ASavedSourceUsesThePersonsAgeAndTheHouseholdsSettings()
	{
		var baseline = await TestServices.SeedBaselineAsync(_database, birthDate: new DateOnly(1976, 4, 1));
		var source = TestServices.SalariedSource(baseline);
		var saved = await TestServices.PayrollSources(_database.CreateContext())
			.CreateAsync(baseline.ScenarioId, source, CancellationToken.None);

		var simulation = await TestServices.Simulations(_database.CreateContext())
			.SimulateSavedAsync(baseline.ScenarioId, saved.Id, 2026, CancellationToken.None);

		Assert.Multiple(() =>
		{
			Assert.That(simulation.Summary.Traditional401kLimit, Is.EqualTo(32_500m), "50 by year end gets the catch-up");
			Assert.That(simulation.Summary.HsaLimit, Is.EqualTo(8_750m), "family coverage from the household");
			Assert.That(simulation.Checks[0].Current.SchoolDistrictTax, Is.EqualTo(42.50m), "home district from the household");
			Assert.That(simulation.Checks[0].Current.CityIncomeTax, Is.EqualTo(95.00m), "work city from the source");
		});
	}

	[Test]
	public async Task APreviewMatchesWhatSavingWouldProduce()
	{
		var baseline = await TestServices.SeedBaselineAsync(_database);
		var source = TestServices.SalariedSource(baseline);

		var preview = await TestServices.Simulations(_database.CreateContext())
			.PreviewAsync(baseline.ScenarioId, source, 2026, CancellationToken.None);
		var saved = await TestServices.PayrollSources(_database.CreateContext())
			.CreateAsync(baseline.ScenarioId, source, CancellationToken.None);
		var simulated = await TestServices.Simulations(_database.CreateContext())
			.SimulateSavedAsync(baseline.ScenarioId, saved.Id, 2026, CancellationToken.None);

		Assert.That(preview.Summary.AnnualTotals.NetPay, Is.EqualTo(simulated.Summary.AnnualTotals.NetPay).And.EqualTo(79_763.40m));
	}

	[Test]
	public async Task AnInvalidPreviewReportsValidationErrors()
	{
		var baseline = await TestServices.SeedBaselineAsync(_database);
		var source = TestServices.SalariedSource(baseline);
		source.AnnualSalary = null;

		var error = Assert.ThrowsAsync<DomainValidationException>(() => TestServices.Simulations(_database.CreateContext())
			.PreviewAsync(baseline.ScenarioId, source, 2026, CancellationToken.None));

		Assert.That(error!.Errors.Keys, Is.EqualTo(new[] { "annualSalary" }));
	}

	[Test]
	public async Task PreviewingAYearWithoutTaxRulesExplainsWhatToDo()
	{
		var baseline = await TestServices.SeedBaselineAsync(_database);

		var error = Assert.ThrowsAsync<DomainValidationException>(() => TestServices.Simulations(_database.CreateContext())
			.PreviewAsync(baseline.ScenarioId, TestServices.SalariedSource(baseline), 2031, CancellationToken.None));

		Assert.That(error!.Errors["year"].Single(), Does.Contain("no tax rules for 2031"));
	}
}
