using PjBudget.Features.TaxYears;
using PjBudget.Shared.Domain;
using PjBudget.Shared.Errors;
using PjBudget.Tests.TestSupport;

namespace PjBudget.Tests.Features.TaxYears;

public class TaxYearServiceTests
{
	private TestDatabase _database = null!;

	[SetUp]
	public async Task SetUp()
	{
		_database = new TestDatabase();
		await using var db = _database.CreateContext();
		await TaxYearSeeder.EnsureSeededAsync(db);
	}

	[TearDown]
	public void TearDown() => _database.Dispose();

	private TaxYearService CreateService() => new(_database.CreateContext());

	[Test]
	public async Task SeederIsIdempotent()
	{
		await using (var db = _database.CreateContext())
		{
			await TaxYearSeeder.EnsureSeededAsync(db);
		}

		var years = await CreateService().ListAsync(CancellationToken.None);
		Assert.That(years.Select(y => y.Year), Is.EqualTo(new[] { 2026 }));
	}

	[Test]
	public async Task GetGroupsRowsIntoSchedules()
	{
		var model = await CreateService().GetAsync(2026, CancellationToken.None);

		var mfj = model.Schedules.Single(s => s.Kind == TaxScheduleKind.FederalIncome && s.FilingStatus == FilingStatus.MarriedFilingJointly);
		Assert.Multiple(() =>
		{
			Assert.That(model.Schedules, Has.Count.EqualTo(7));
			Assert.That(mfj.Rows.Select(r => r.Over), Is.EqualTo(new[] { 0m, 24_800m, 100_800m, 211_400m, 403_550m, 512_450m, 768_700m }));
			Assert.That(model.FilingStatuses.Select(f => f.FilingStatus), Is.EquivalentTo(Enum.GetValues<FilingStatus>()));
		});
	}

	[Test]
	public async Task UpdateReplacesValuesAndSchedules()
	{
		var model = await CreateService().GetAsync(2026, CancellationToken.None);
		model.Fica.SocialSecurityWageBase = 190_000m;
		model.Schedules.Single(s => s.Kind == TaxScheduleKind.OhioIncome).Rows[1].BaseAmount = 300m;

		var updated = await CreateService().UpdateAsync(2026, model, CancellationToken.None);
		var reloaded = await CreateService().GetAsync(2026, CancellationToken.None);

		Assert.Multiple(() =>
		{
			Assert.That(updated.Version, Is.Not.EqualTo(model.Version));
			Assert.That(reloaded.Fica.SocialSecurityWageBase, Is.EqualTo(190_000m));
			Assert.That(reloaded.Schedules.Single(s => s.Kind == TaxScheduleKind.OhioIncome).Rows[1].BaseAmount, Is.EqualTo(300m));
		});
	}

	[Test]
	public async Task UpdateWithAStaleVersionIsRejected()
	{
		var first = await CreateService().GetAsync(2026, CancellationToken.None);
		var second = await CreateService().GetAsync(2026, CancellationToken.None);
		await CreateService().UpdateAsync(2026, first, CancellationToken.None);

		Assert.ThrowsAsync<ConflictException>(() => CreateService().UpdateAsync(2026, second, CancellationToken.None));
	}

	[Test]
	public async Task UpdateReportsValidationErrorsWithCamelCasePaths()
	{
		var model = await CreateService().GetAsync(2026, CancellationToken.None);
		model.Fica.SocialSecurityRate = 6.2m;
		model.Schedules.Single(s => s.Kind == TaxScheduleKind.OhioWithholding).Rows.Reverse();

		var error = Assert.ThrowsAsync<DomainValidationException>(() => CreateService().UpdateAsync(2026, model, CancellationToken.None));

		Assert.That(error!.Errors.Keys, Is.SupersetOf(new[] { "fica.socialSecurityRate", "schedules[3].rows" }));
	}

	[Test]
	public async Task CopyCreatesAnIndependentYear()
	{
		await CreateService().CopyAsync(2026, 2027, CancellationToken.None);

		var copy = await CreateService().GetAsync(2027, CancellationToken.None);
		copy.Limits.ElectiveDeferral = 25_000m;
		await CreateService().UpdateAsync(2027, copy, CancellationToken.None);

		var original = await CreateService().GetAsync(2026, CancellationToken.None);
		Assert.Multiple(() =>
		{
			Assert.That(original.Limits.ElectiveDeferral, Is.EqualTo(24_500m));
			Assert.That(copy.Schedules.Sum(s => s.Rows.Count), Is.EqualTo(original.Schedules.Sum(s => s.Rows.Count)));
		});
	}

	[Test]
	public void CopyOntoAnExistingYearIsRejected()
	{
		Assert.ThrowsAsync<ConflictException>(() => CreateService().CopyAsync(2026, 2026, CancellationToken.None));
	}

	[Test]
	public async Task DeleteRemovesTheYear()
	{
		await CreateService().CopyAsync(2026, 2027, CancellationToken.None);
		await CreateService().DeleteAsync(2027, CancellationToken.None);

		Assert.ThrowsAsync<NotFoundException>(() => CreateService().GetAsync(2027, CancellationToken.None));
	}

	[Test]
	public async Task WithholdingTablesAreDerivedForEachFilingStatus()
	{
		var tables = await CreateService().GetWithholdingTablesAsync(2026, CancellationToken.None);

		var mfj = tables.Single(t => t.FilingStatus == FilingStatus.MarriedFilingJointly);
		Assert.Multiple(() =>
		{
			Assert.That(tables, Has.Count.EqualTo(3));
			Assert.That(mfj.Standard, Does.Contain(new WithholdingTableRowModel(120_100m, 11_600m, 0.22m)));
			Assert.That(mfj.Step2Checkbox, Does.Contain(new WithholdingTableRowModel(66_500m, 5_800m, 0.22m)));
		});
	}

	[Test]
	public void ParametersForAMissingYearExplainHowToCreateThem()
	{
		var error = Assert.ThrowsAsync<DomainValidationException>(() => CreateService().GetParametersAsync(2030, CancellationToken.None));

		Assert.That(error!.Errors["year"].Single(), Does.Contain("Copy an existing year"));
	}
}
