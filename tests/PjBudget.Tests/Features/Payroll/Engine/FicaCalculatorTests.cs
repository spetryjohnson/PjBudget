using PjBudget.Features.Payroll.Engine;
using PjBudget.Tests.TestSupport;

namespace PjBudget.Tests.Features.Payroll.Engine;

public class FicaCalculatorTests
{
	private static readonly FicaParameters Fica2026 = PayrollTestData.TaxYear2026Parameters.Fica;

	[Test]
	public void BelowEveryThreshold()
	{
		var taxes = FicaCalculator.Calculate(5_000m, 5_000m, default, Fica2026);

		Assert.That(taxes, Is.EqualTo(new FicaTaxes(5_000m, 310.00m, 72.50m, 0m)));
	}

	[Test]
	public void EachCheckBringsTheYearToDateTaxToTheRateOnYearToDateWages()
	{
		// 1,000.20 × 1.45% = 14.5029, which rounds down to 14.50. Two checks' wages are 2,000.40, and 1.45% of that is
		// 29.0058, which rounds up to 29.01, so the second check withholds 14.51.
		var first = FicaCalculator.Calculate(1_000.20m, 1_000.20m, default, Fica2026);
		var second = FicaCalculator.Calculate(1_000.20m, 1_000.20m,
			new FicaYearToDate(1_000.20m, first.SocialSecurity, 1_000.20m, first.Medicare, 0m), Fica2026);

		Assert.Multiple(() =>
		{
			Assert.That(first.Medicare, Is.EqualTo(14.50m));
			Assert.That(second.Medicare, Is.EqualTo(14.51m));
			Assert.That(first.SocialSecurity + second.SocialSecurity, Is.EqualTo(124.02m), "6.2% of 2,000.40, rounded once");
		});
	}

	[Test]
	public void OnlyWagesUpToTheWageBaseAreTaxedForSocialSecurity()
	{
		var taxes = FicaCalculator.Calculate(8_000m, 8_000m, new FicaYearToDate(180_000m, 11_160m, 180_000m, 2_610m, 0m), Fica2026);

		Assert.Multiple(() =>
		{
			Assert.That(taxes.SocialSecurityTaxedWages, Is.EqualTo(4_500m));
			Assert.That(taxes.SocialSecurity, Is.EqualTo(279.00m));
			Assert.That(taxes.Medicare, Is.EqualTo(116.00m));
		});
	}

	[Test]
	public void TheYearsSocialSecurityTaxEndsExactlyOnTheMaximum()
	{
		// 23 checks of 8,000.10 bring year-to-date tax to round(184,002.30 × 6.2%) = 11,408.14. The rest of the wage
		// base (497.70) finishes the year at round(184,500 × 6.2%) = 11,439.00, the maximum.
		var taxes = FicaCalculator.Calculate(8_000.10m, 8_000.10m,
			new FicaYearToDate(184_002.30m, 11_408.14m, 184_002.30m, 2_668.03m, 0m), Fica2026);

		Assert.Multiple(() =>
		{
			Assert.That(taxes.SocialSecurityTaxedWages, Is.EqualTo(497.70m));
			Assert.That(11_408.14m + taxes.SocialSecurity, Is.EqualTo(Fica2026.MaxSocialSecurityTax));
		});
	}

	[Test]
	public void NothingIsWithheldOnceTheWageBaseIsReached()
	{
		var taxes = FicaCalculator.Calculate(8_000m, 8_000m, new FicaYearToDate(184_500m, 11_439m, 190_000m, 2_755m, 0m), Fica2026);

		Assert.Multiple(() =>
		{
			Assert.That(taxes.SocialSecurity, Is.EqualTo(0m));
			Assert.That(taxes.Medicare, Is.EqualTo(116.00m), "Medicare has no wage base");
		});
	}

	[Test]
	public void AdditionalMedicareStartsWithWagesAbove200k()
	{
		var crossing = FicaCalculator.Calculate(10_000m, 10_000m,
			new FicaYearToDate(184_500m, 11_439m, 195_000m, 2_827.50m, 0m), Fica2026);
		var above = FicaCalculator.Calculate(10_000m, 10_000m,
			new FicaYearToDate(184_500m, 11_439m, 250_000m, 3_625m, 450m), Fica2026);

		Assert.Multiple(() =>
		{
			Assert.That(crossing.AdditionalMedicare, Is.EqualTo(45.00m), "0.9% of the 5,000 above 200,000");
			Assert.That(above.AdditionalMedicare, Is.EqualTo(90.00m));
		});
	}
}
