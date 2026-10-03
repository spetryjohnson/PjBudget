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
	public void OnlyWagesUpToTheWageBaseAreTaxedForSocialSecurity()
	{
		var taxes = FicaCalculator.Calculate(8_000m, 8_000m, new FicaYearToDate(180_000m, 11_160m, 180_000m), Fica2026);

		Assert.That(taxes.SocialSecurityTaxedWages, Is.EqualTo(4_500m));
		Assert.That(taxes.SocialSecurity, Is.EqualTo(279.00m));
	}

	[Test]
	public void TheFinalSocialSecurityCheckAbsorbsRoundingSoTheYearTotalsTheMaximum()
	{
		// 23 checks of 8,000.10 each withheld round(496.0062) = 496.01. The rest of the wage base (497.70) would round
		// to 30.86, but only 11,439.00 − 11,408.23 = 30.77 of the annual maximum is left.
		var taxes = FicaCalculator.Calculate(8_000.10m, 8_000.10m, new FicaYearToDate(184_002.30m, 11_408.23m, 184_002.30m), Fica2026);

		Assert.That(taxes.SocialSecurity, Is.EqualTo(30.77m));
	}

	[Test]
	public void NothingIsWithheldOnceTheWageBaseIsReached()
	{
		var taxes = FicaCalculator.Calculate(8_000m, 8_000m, new FicaYearToDate(184_500m, 11_439m, 190_000m), Fica2026);

		Assert.Multiple(() =>
		{
			Assert.That(taxes.SocialSecurity, Is.EqualTo(0m));
			Assert.That(taxes.Medicare, Is.EqualTo(116.00m), "Medicare has no wage base");
		});
	}

	[Test]
	public void AdditionalMedicareStartsWithWagesAbove200k()
	{
		var crossing = FicaCalculator.Calculate(10_000m, 10_000m, new FicaYearToDate(184_500m, 11_439m, 195_000m), Fica2026);
		var above = FicaCalculator.Calculate(10_000m, 10_000m, new FicaYearToDate(184_500m, 11_439m, 250_000m), Fica2026);

		Assert.Multiple(() =>
		{
			Assert.That(crossing.AdditionalMedicare, Is.EqualTo(45.00m), "0.9% of the 5,000 above 200,000");
			Assert.That(above.AdditionalMedicare, Is.EqualTo(90.00m));
		});
	}
}
