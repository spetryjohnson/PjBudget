using PjBudget.Features.Household;
using PjBudget.Features.Locales;
using PjBudget.Features.Payroll;
using PjBudget.Features.Payroll.Engine.States;
using PjBudget.Features.People;
using PjBudget.Features.Scenarios;
using PjBudget.Features.TaxYears;
using PjBudget.Shared.Database;
using PjBudget.Shared.Domain;

namespace PjBudget.Tests.TestSupport;

/// <summary>
/// Wires services the way DI does, over a test database, plus a baseline of reference data that most service tests
/// need (current scenario, 2026 tax rules, a person and an Ohio locale).
/// </summary>
public static class TestServices
{
	public static readonly StateTaxModules States = new([new OhioTaxModule()]);

	public static LocaleService Locales(AppDbContext db) => new(db);
	public static PersonService People(AppDbContext db) => new(db);
	public static HouseholdService Household(AppDbContext db) => new(db, States);

	public static PayrollSimulationService Simulations(AppDbContext db)
		=> new(db, new TaxYearService(db), PayrollTestData.Simulator(), new PayrollSourceValidator(db, States));

	public static PayrollSourceService PayrollSources(AppDbContext db)
		=> new(db, new PayrollSourceValidator(db, States), Simulations(db));

	public sealed record Baseline(int ScenarioId, int PersonId, int LocaleId);

	public static async Task<Baseline> SeedBaselineAsync(TestDatabase database, DateOnly? birthDate = null)
	{
		await using var db = database.CreateContext();

		var scenario = await ScenarioSeeder.EnsureCurrentScenarioAsync(db);
		await TaxYearSeeder.EnsureSeededAsync(db);

		var person = new Person { DisplayName = "Pat", BirthDate = birthDate };
		var locale = new Locale
		{
			Name = "Testville, OH",
			City = "Testville",
			StateCode = "OH",
			MunicipalTaxRate = 0.02m,
			SchoolDistrictName = "Testville City SD",
			SchoolDistrictTaxRate = 0.01m,
			SchoolDistrictTaxBase = SchoolDistrictTaxBase.EarnedIncome,
		};
		db.AddRange(person, locale);
		await db.SaveChangesAsync();

		var profile = await HouseholdSeeder.EnsureProfileAsync(db, scenario.Id);
		profile.HomeLocaleId = locale.Id;
		profile.HsaCoverage = HsaCoverage.Family;
		await db.SaveChangesAsync();

		return new Baseline(scenario.Id, person.Id, locale.Id);
	}

	/// <summary>A valid $120k semimonthly source with a medical deduction, ready to save or preview.</summary>
	public static PayrollSourceModel SalariedSource(Baseline baseline) => new()
	{
		PersonId = baseline.PersonId,
		WorkLocaleId = baseline.LocaleId,
		Name = "Main job",
		EmployerName = "Acme",
		PayBasis = PayBasis.Salary,
		AnnualSalary = 120_000m,
		PayFrequency = PayFrequency.Semimonthly,
		SemimonthlyPayDay1 = 10,
		SemimonthlyPayDay2 = 25,
		Traditional401kPercent = 10m,
		Traditional401kPreTaxFor = PjBudget.Features.Payroll.Engine.DefaultTaxTreatment.Traditional401k,
		HsaEmployeePerCheck = 100m,
		HsaPreTaxFor = TaxableWageTypes.All,
		HealthFsaAnnualElection = 1_200m,
		HealthFsaPreTaxFor = TaxableWageTypes.All,
		W4FilingStatus = FilingStatus.MarriedFilingJointly,
		StipendPerCheck = 25m,
		Deductions =
		[
			new PayrollDeductionModel { Type = DeductionType.Medical, Label = "Medical", AnnualAmount = 2_400m, PreTaxFor = TaxableWageTypes.All },
			new PayrollDeductionModel { Type = DeductionType.Life, Label = "Supplemental life", AnnualAmount = 240m, PreTaxFor = TaxableWageTypes.None },
		],
	};
}
