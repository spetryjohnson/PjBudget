using PjBudget.Features.Payroll;
using PjBudget.Shared.Domain;
using PjBudget.Shared.Errors;
using PjBudget.Tests.TestSupport;

namespace PjBudget.Tests.Features.Payroll;

public class PayrollSourceServiceTests
{
	private TestDatabase _database = null!;
	private TestServices.Baseline _baseline = null!;

	[SetUp]
	public async Task SetUp()
	{
		_database = new TestDatabase();
		_baseline = await TestServices.SeedBaselineAsync(_database);
	}

	[TearDown]
	public void TearDown() => _database.Dispose();

	private PayrollSourceService Service() => TestServices.PayrollSources(_database.CreateContext());

	private Task<PayrollSourceModel> CreateAsync(PayrollSourceModel? model = null)
		=> Service().CreateAsync(_baseline.ScenarioId, model ?? TestServices.SalariedSource(_baseline), CancellationToken.None);

	[Test]
	public async Task CreateAndGetRoundTripEveryField()
	{
		var created = await CreateAsync();

		var loaded = await Service().GetAsync(_baseline.ScenarioId, created.Id, CancellationToken.None);

		Assert.Multiple(() =>
		{
			Assert.That(loaded.Name, Is.EqualTo("Main job"));
			Assert.That(loaded.AnnualSalary, Is.EqualTo(120_000m));
			Assert.That(loaded.SemimonthlyPayDay2, Is.EqualTo(25));
			Assert.That(loaded.Traditional401kPreTaxFor, Is.EqualTo(TaxableWageTypes.Federal | TaxableWageTypes.State | TaxableWageTypes.School));
			Assert.That(loaded.Deductions.Select(d => (d.Label, d.PreTaxFor)),
				Is.EqualTo(new[] { ("Medical", TaxableWageTypes.All), ("Supplemental life", TaxableWageTypes.None) }));
			Assert.That(loaded.Version, Is.Not.EqualTo(Guid.Empty));
		});
	}

	[Test]
	public async Task SettingsThatDontApplyToThePayBasisOrFrequencyArentStored()
	{
		var model = TestServices.SalariedSource(_baseline);
		model.HourlyRate = 50m;
		model.BiweeklyAnchorDate = new DateOnly(2026, 1, 2);

		var created = await CreateAsync(model);

		Assert.Multiple(() =>
		{
			Assert.That(created.HourlyRate, Is.Null);
			Assert.That(created.BiweeklyAnchorDate, Is.Null);
		});
	}

	[Test]
	public async Task UpdateReplacesDeductionsAndAdvancesTheVersion()
	{
		var created = await CreateAsync();
		created.Deductions = [new PayrollDeductionModel { Type = DeductionType.Vision, Label = "Vision", AnnualAmount = 120m, PreTaxFor = TaxableWageTypes.All }];

		var updated = await Service().UpdateAsync(_baseline.ScenarioId, created.Id, created, CancellationToken.None);

		Assert.Multiple(() =>
		{
			Assert.That(updated.Deductions.Select(d => d.Label), Is.EqualTo(new[] { "Vision" }));
			Assert.That(updated.Version, Is.Not.EqualTo(created.Version));
		});
	}

	[Test]
	public async Task UpdateWithAStaleVersionIsRejected()
	{
		var created = await CreateAsync();
		await Service().UpdateAsync(_baseline.ScenarioId, created.Id, created, CancellationToken.None);

		Assert.ThrowsAsync<ConflictException>(
			() => Service().UpdateAsync(_baseline.ScenarioId, created.Id, created, CancellationToken.None));
	}

	[Test]
	public async Task APlanHoldsAtMostFourSources()
	{
		for (var i = 0; i < PayrollSourceService.MaxSourcesPerScenario; i++)
		{
			await CreateAsync();
		}

		Assert.ThrowsAsync<DomainValidationException>(() => CreateAsync());
	}

	[Test]
	public void ValidationReportsFieldAndReferenceProblems()
	{
		var model = TestServices.SalariedSource(_baseline);
		model.SemimonthlyPayDay2 = 5;
		model.Traditional401kPercent = 120m;

		var fieldErrors = Assert.ThrowsAsync<DomainValidationException>(() => CreateAsync(model));
		Assert.That(fieldErrors!.Errors.Keys, Is.EquivalentTo(new[] { "semimonthlyPayDay2", "traditional401kPercent" }));

		model = TestServices.SalariedSource(_baseline);
		model.PersonId = 999;
		model.WorkLocaleId = 999;

		var referenceErrors = Assert.ThrowsAsync<DomainValidationException>(() => CreateAsync(model));
		Assert.That(referenceErrors!.Errors.Keys, Is.EquivalentTo(new[] { "personId", "workLocaleId" }));
	}

	[TestCase(1, 31, false)]
	[TestCase(30, 31, false)]
	[TestCase(10, 15, false)]
	[TestCase(10, 25, true)]
	[TestCase(15, 31, true)]
	[TestCase(1, 15, true)]
	public async Task SemimonthlyPaydaysMustBeAWeekApartSoShiftedDatesCantMerge(int day1, int day2, bool valid)
	{
		var model = TestServices.SalariedSource(_baseline);
		model.SemimonthlyPayDay1 = day1;
		model.SemimonthlyPayDay2 = day2;

		if (valid)
		{
			Assert.That((await CreateAsync(model)).Id, Is.GreaterThan(0));
		}
		else
		{
			var error = Assert.ThrowsAsync<DomainValidationException>(() => CreateAsync(model));
			Assert.That(error!.Errors.Keys, Is.EqualTo(new[] { "semimonthlyPayDay2" }));
		}
	}

	[Test]
	public async Task ASourceWhoseLocaleIsNoLongerSupportedReportsAnErrorInsteadOfFailing()
	{
		await CreateAsync();
		await using (var db = _database.CreateContext())
		{
			// Simulates data that predates the guard against moving an in-use locale to an unsupported state.
			db.Locales.Single(l => l.Id == _baseline.LocaleId).StateCode = "MI";
			await db.SaveChangesAsync();
		}

		var summary = (await Service().ListAsync(_baseline.ScenarioId, 2026, CancellationToken.None)).Single();

		Assert.That(summary.SimulationError, Does.Contain("Taxes for MI aren't supported"));
	}

	[Test]
	public void BiweeklyPayNeedsAnAnchorDate()
	{
		var model = TestServices.SalariedSource(_baseline);
		model.PayFrequency = PayFrequency.Biweekly;

		var error = Assert.ThrowsAsync<DomainValidationException>(() => CreateAsync(model));
		Assert.That(error!.Errors.Keys, Is.EqualTo(new[] { "biweeklyAnchorDate" }));
	}

	[Test]
	public async Task ListIncludesHeadlineNumbersForTheYear()
	{
		await CreateAsync();

		var summary = (await Service().ListAsync(_baseline.ScenarioId, 2026, CancellationToken.None)).Single();

		Assert.Multiple(() =>
		{
			Assert.That(summary.PersonName, Is.EqualTo("Pat"));
			Assert.That(summary.Headline!.CheckCount, Is.EqualTo(24));
			Assert.That(summary.Headline.AnnualGrossPay, Is.EqualTo(120_000m));
			Assert.That(summary.Headline.RegularNetPay, Is.EqualTo(3_323.46m));
			Assert.That(summary.SimulationError, Is.Null);
		});
	}

	[Test]
	public async Task ListStillWorksForAYearWithoutTaxRules()
	{
		await CreateAsync();

		var summary = (await Service().ListAsync(_baseline.ScenarioId, 2031, CancellationToken.None)).Single();

		Assert.Multiple(() =>
		{
			Assert.That(summary.Headline, Is.Null);
			Assert.That(summary.SimulationError, Does.Contain("no tax rules for 2031"));
		});
	}

	[Test]
	public async Task DeleteRemovesTheSource()
	{
		var created = await CreateAsync();

		await Service().DeleteAsync(_baseline.ScenarioId, created.Id, CancellationToken.None);

		Assert.ThrowsAsync<NotFoundException>(() => Service().GetAsync(_baseline.ScenarioId, created.Id, CancellationToken.None));
	}
}
