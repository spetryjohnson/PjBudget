using PjBudget.Features.Locales;
using PjBudget.Features.People;
using PjBudget.Features.Scenarios;
using PjBudget.Shared.Domain;
using PjBudget.Shared.Errors;
using PjBudget.Tests.TestSupport;

namespace PjBudget.Tests.Features.Household;

public class HouseholdServiceTests
{
	private TestDatabase _database = null!;

	[SetUp]
	public void SetUp() => _database = new TestDatabase();

	[TearDown]
	public void TearDown() => _database.Dispose();

	[Test]
	public async Task ANewScenarioGetsDefaultHouseholdSettings()
	{
		int scenarioId;
		await using (var db = _database.CreateContext())
		{
			scenarioId = (await ScenarioSeeder.EnsureCurrentScenarioAsync(db)).Id;
		}

		var household = await TestServices.Household(_database.CreateContext()).GetAsync(scenarioId, CancellationToken.None);

		Assert.Multiple(() =>
		{
			Assert.That(household.HomeLocaleId, Is.Null);
			Assert.That(household.HsaCoverage, Is.EqualTo(HsaCoverage.None));
			Assert.That(household.TaxFilingStatus, Is.EqualTo(FilingStatus.MarriedFilingJointly));
			Assert.That(household.OhioExemptionCount, Is.EqualTo(2));
		});
	}

	[Test]
	public async Task UpdateSavesSettings()
	{
		var baseline = await TestServices.SeedBaselineAsync(_database);
		var household = await TestServices.Household(_database.CreateContext()).GetAsync(baseline.ScenarioId, CancellationToken.None);
		household.HsaCoverage = HsaCoverage.SelfOnly;
		household.FederalItemizedDeductions = 40_000m;

		await TestServices.Household(_database.CreateContext()).UpdateAsync(baseline.ScenarioId, household, CancellationToken.None);
		var reloaded = await TestServices.Household(_database.CreateContext()).GetAsync(baseline.ScenarioId, CancellationToken.None);

		Assert.Multiple(() =>
		{
			Assert.That(reloaded.HsaCoverage, Is.EqualTo(HsaCoverage.SelfOnly));
			Assert.That(reloaded.FederalItemizedDeductions, Is.EqualTo(40_000m));
			Assert.That(reloaded.HomeLocaleId, Is.EqualTo(baseline.LocaleId));
		});
	}

	[Test]
	public async Task ChangingTheHomeLocaleReplacesTheLoadedOne()
	{
		var baseline = await TestServices.SeedBaselineAsync(_database);
		var other = await TestServices.Locales(_database.CreateContext()).CreateAsync(
			new LocaleModel { Name = "Otherville", City = "Otherville", StateCode = "OH", MunicipalTaxRate = 0.025m }, CancellationToken.None);

		var household = await TestServices.Household(_database.CreateContext()).GetAsync(baseline.ScenarioId, CancellationToken.None);
		household.HomeLocaleId = other.Id;
		await TestServices.Household(_database.CreateContext()).UpdateAsync(baseline.ScenarioId, household, CancellationToken.None);

		var reloaded = await TestServices.Household(_database.CreateContext()).GetAsync(baseline.ScenarioId, CancellationToken.None);
		Assert.That(reloaded.HomeLocaleId, Is.EqualTo(other.Id));
	}

	[Test]
	public async Task AHomeInAnUnsupportedStateIsRejected()
	{
		var baseline = await TestServices.SeedBaselineAsync(_database);
		var indiana = await TestServices.Locales(_database.CreateContext()).CreateAsync(
			new LocaleModel { Name = "Indianapolis", City = "Indianapolis", StateCode = "IN" }, CancellationToken.None);

		var household = await TestServices.Household(_database.CreateContext()).GetAsync(baseline.ScenarioId, CancellationToken.None);
		household.HomeLocaleId = indiana.Id;

		var error = Assert.ThrowsAsync<DomainValidationException>(
			() => TestServices.Household(_database.CreateContext()).UpdateAsync(baseline.ScenarioId, household, CancellationToken.None));
		Assert.That(error!.Errors.Keys, Is.EqualTo(new[] { "homeLocaleId" }));
	}

	[Test]
	public async Task APersonWithPayrollSourcesCantBeDeleted()
	{
		var baseline = await TestServices.SeedBaselineAsync(_database);
		await TestServices.PayrollSources(_database.CreateContext())
			.CreateAsync(baseline.ScenarioId, TestServices.SalariedSource(baseline), CancellationToken.None);

		Assert.ThrowsAsync<ConflictException>(
			() => TestServices.People(_database.CreateContext()).DeleteAsync(baseline.PersonId, CancellationToken.None));

		var spare = await TestServices.People(_database.CreateContext())
			.CreateAsync(new PersonModel { DisplayName = "Sam" }, CancellationToken.None);
		await TestServices.People(_database.CreateContext()).DeleteAsync(spare.Id, CancellationToken.None);
	}
}
