using PjBudget.Features.Payroll.Engine;
using PjBudget.Shared.Domain;
using PjBudget.Tests.TestSupport;

namespace PjBudget.Tests.Features.Payroll.Engine;

/// <summary>
/// Worksheet 1A cases. Each expected value is worked by hand in the comment next to it, using the 2026 tables.
/// </summary>
public class FederalIncomeTaxWithholdingTests
{
	private static decimal Withholding(decimal wages, FederalW4 w4, int periods = 24)
	{
		var rules = PayrollTestData.TaxYear2026Parameters.FederalRulesFor(w4.FilingStatus);
		return new FederalIncomeTaxWithholding(w4, rules, periods).Calculate(wages);
	}

	[Test]
	public void MarriedStandard()
	{
		// 5,000 × 24 = 120,000 − 12,900 = 107,100 → 2,480 + 12% × 63,000 = 10,040 ÷ 24 = 418.33
		Assert.That(Withholding(5_000m, new FederalW4(FilingStatus.MarriedFilingJointly)), Is.EqualTo(418.33m));
	}

	[Test]
	public void MarriedWithStep2CheckboxUsesTheHigherTableAndNoLine1gAllowance()
	{
		// 120,000 → 5,800 + 22% × 53,500 = 17,570 ÷ 24 = 732.08
		Assert.That(Withholding(5_000m, new FederalW4(FilingStatus.MarriedFilingJointly, MultipleJobs: true)), Is.EqualTo(732.08m));
	}

	[Test]
	public void Step3CreditsReduceEachCheck()
	{
		// 418.33 − round(4,400 ÷ 24 = 183.333) = 235.00
		Assert.That(Withholding(5_000m, new FederalW4(FilingStatus.MarriedFilingJointly, Credits: 4_400m)), Is.EqualTo(235.00m));
	}

	[Test]
	public void Step4AdjustmentsAndExtraWithholding()
	{
		// 120,000 + 12,000 − (6,000 + 12,900) = 113,100 → 2,480 + 12% × 69,000 = 10,760 ÷ 24 = 448.33 + 50
		var w4 = new FederalW4(FilingStatus.MarriedFilingJointly, OtherIncome: 12_000m, Deductions: 6_000m, ExtraWithholding: 50m);

		Assert.That(Withholding(5_000m, w4), Is.EqualTo(498.33m));
	}

	[Test]
	public void CreditsCantMakeWithholdingNegativeButExtraWithholdingStillApplies()
	{
		// 24,000 − 12,900 = 11,100 is inside the 0% band, so only the extra $25 is withheld.
		var w4 = new FederalW4(FilingStatus.MarriedFilingJointly, Credits: 2_200m, ExtraWithholding: 25m);

		Assert.That(Withholding(1_000m, w4), Is.EqualTo(25.00m));
	}

	[Test]
	public void SingleBiweekly()
	{
		// 2,000 × 26 = 52,000 − 8,600 = 43,400 → 1,240 + 12% × 23,500 = 4,060 ÷ 26 = 156.15
		Assert.That(Withholding(2_000m, new FederalW4(FilingStatus.Single), periods: 26), Is.EqualTo(156.15m));
	}

	[Test]
	public void HeadOfHousehold()
	{
		// 96,000 − 8,600 = 87,400 → 7,740 + 22% × 4,400 = 8,708 ÷ 24 = 362.83
		Assert.That(Withholding(4_000m, new FederalW4(FilingStatus.HeadOfHousehold)), Is.EqualTo(362.83m));
	}
}
