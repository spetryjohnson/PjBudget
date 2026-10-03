using PjBudget.Features.Household;
using PjBudget.Features.Locales;
using PjBudget.Shared.Domain;
using PjBudget.Shared.Errors;
using PjBudget.Tests.TestSupport;

namespace PjBudget.Tests.Features.Locales;

public class LocaleServiceTests
{
	private TestDatabase _database = null!;

	[SetUp]
	public void SetUp() => _database = new TestDatabase();

	[TearDown]
	public void TearDown() => _database.Dispose();

	private static LocaleModel Columbus() => new()
	{
		Name = " Columbus, OH ",
		City = "Columbus",
		StateCode = "OH",
		ZipCode = "43215",
		MunicipalTaxRate = 0.025m,
	};

	[Test]
	public async Task CreateTrimsAndListsLocales()
	{
		await using var db = _database.CreateContext();
		await TestServices.Locales(db).CreateAsync(Columbus(), CancellationToken.None);

		var locales = await TestServices.Locales(_database.CreateContext()).ListAsync(CancellationToken.None);

		Assert.Multiple(() =>
		{
			Assert.That(locales.Single().Name, Is.EqualTo("Columbus, OH"));
			Assert.That(locales.Single().MunicipalTaxRate, Is.EqualTo(0.025m));
		});
	}

	[Test]
	public async Task ASchoolDistrictWithoutARateDropsItsTaxBase()
	{
		var model = Columbus();
		model.SchoolDistrictName = "Columbus City SD";
		model.SchoolDistrictTaxBase = SchoolDistrictTaxBase.Traditional;

		var saved = await TestServices.Locales(_database.CreateContext()).CreateAsync(model, CancellationToken.None);

		Assert.Multiple(() =>
		{
			Assert.That(saved.SchoolDistrictName, Is.EqualTo("Columbus City SD"));
			Assert.That(saved.SchoolDistrictTaxRate, Is.Null);
			Assert.That(saved.SchoolDistrictTaxBase, Is.Null);
		});
	}

	[Test]
	public void ValidationRequiresATaxBaseForASchoolTaxAndATwoLetterState()
	{
		var model = Columbus();
		model.StateCode = "Ohio";
		model.SchoolDistrictTaxRate = 0.0075m;

		var error = Assert.ThrowsAsync<DomainValidationException>(
			() => TestServices.Locales(_database.CreateContext()).CreateAsync(model, CancellationToken.None));

		Assert.That(error!.Errors.Keys, Is.EquivalentTo(new[] { "stateCode", "schoolDistrictTaxBase" }));
	}

	[Test]
	public async Task UpdateWithAStaleVersionIsRejected()
	{
		var saved = await TestServices.Locales(_database.CreateContext()).CreateAsync(Columbus(), CancellationToken.None);
		var staleVersion = saved.Version;
		saved.MunicipalTaxRate = 0.03m;
		await TestServices.Locales(_database.CreateContext()).UpdateAsync(saved.Id, saved, CancellationToken.None);

		saved.Version = staleVersion;
		Assert.ThrowsAsync<ConflictException>(
			() => TestServices.Locales(_database.CreateContext()).UpdateAsync(saved.Id, saved, CancellationToken.None));
	}

	[Test]
	public async Task ALocaleInUseCantBeDeleted()
	{
		var baseline = await TestServices.SeedBaselineAsync(_database);

		Assert.ThrowsAsync<ConflictException>(
			() => TestServices.Locales(_database.CreateContext()).DeleteAsync(baseline.LocaleId, CancellationToken.None));

		await using (var db = _database.CreateContext())
		{
			var profile = await HouseholdSeeder.EnsureProfileAsync(db, baseline.ScenarioId);
			profile.HomeLocaleId = null;
			await db.SaveChangesAsync();
		}

		await TestServices.Locales(_database.CreateContext()).DeleteAsync(baseline.LocaleId, CancellationToken.None);
		Assert.That(await TestServices.Locales(_database.CreateContext()).ListAsync(CancellationToken.None), Is.Empty);
	}
}
