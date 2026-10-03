using PjBudget.Features.Payroll.Engine;
using PjBudget.Features.Payroll.Engine.States;
using PjBudget.Shared.Domain;
using PjBudget.Tests.TestSupport;

namespace PjBudget.Tests.Features.Payroll.Engine;

/// <summary>
/// Ohio withholding with the optional computer formula effective 2026-08-01. Expected values are worked in comments.
/// </summary>
public class OhioTaxModuleTests
{
	private readonly OhioTaxModule _ohio = new();

	private StateAndLocalWithholding Withhold(
		decimal stateWages, int periods = 24, int exemptions = 0, decimal additional = 0m,
		decimal cityWages = 0m, decimal schoolWages = 0m, HomeLocation? home = null)
	{
		var context = new StateWithholdingContext(
			periods, new StateWithholdingElections(exemptions, additional), PayrollTestData.WorkCity2Percent, home,
			PayrollTestData.TaxYear2026Parameters);

		return _ohio.CalculateWithholding(context, new TaxableWages(0m, stateWages, 0m, 0m, cityWages, schoolWages));
	}

	[Test]
	public void TopBracket()
	{
		// 5,000 × 24 = 120,000 → 2,627.91 + 3.4% × 20,000 = 3,307.91 ÷ 24 = 137.8296, and the fraction of a cent is dropped
		Assert.That(Withhold(5_000m).State, Is.EqualTo(137.82m));
	}

	[Test]
	public void ExemptionsReduceAnnualWagesBy650Each()
	{
		// 120,000 − 1,300 = 118,700 → 2,627.91 + 3.4% × 18,700 = 3,263.71 ÷ 24 = 135.9879 → 135.98
		Assert.That(Withhold(5_000m, exemptions: 2).State, Is.EqualTo(135.98m));
	}

	[Test]
	public void MiddleBracketBiweeklyWithAdditionalWithholding()
	{
		// 2,000 × 26 − 650 = 51,350 → 416.80 + 2.99% × 25,300 = 1,173.27 ÷ 26 = 45.1258 → 45.12, plus 10 extra
		Assert.That(Withhold(2_000m, periods: 26, exemptions: 1, additional: 10m).State, Is.EqualTo(55.12m));
	}

	[Test]
	public void BottomBracket()
	{
		// 24,000 × 1.6% = 384 ÷ 24 = 16.00
		Assert.That(Withhold(1_000m).State, Is.EqualTo(16.00m));
	}

	[Test]
	public void CityTaxIsTheWorkCityRateOnCityWages()
	{
		Assert.That(Withhold(4_000m, cityWages: 4_750m).City, Is.EqualTo(95.00m));
	}

	[Test]
	public void EarnedIncomeSchoolDistrictsUseAFlatRateWithoutExemptions()
	{
		var tax = Withhold(5_000m, exemptions: 2, schoolWages: 4_000m, home: PayrollTestData.HomeWithEarnedIncomeSchoolTax1Percent);

		Assert.That(tax.SchoolDistrict, Is.EqualTo(40.00m));
	}

	[Test]
	public void TraditionalSchoolDistrictsUseTheStateWageBaseAndExemptions()
	{
		// (120,000 − 1,300) × 1% = 1,187 ÷ 24 = 49.46
		var home = new HomeLocation("OH", 0.02m, 0.01m, SchoolDistrictTaxBase.Traditional);

		Assert.That(Withhold(5_000m, exemptions: 2, schoolWages: 4_000m, home: home).SchoolDistrict, Is.EqualTo(49.46m));
	}

	[Test]
	public void NoSchoolDistrictTaxWithoutARate()
	{
		var home = new HomeLocation("OH", 0.02m, null, null);

		Assert.That(Withhold(5_000m, schoolWages: 5_000m, home: home).SchoolDistrict, Is.EqualTo(0m));
	}
}
