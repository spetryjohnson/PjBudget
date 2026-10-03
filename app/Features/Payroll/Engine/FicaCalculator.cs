namespace PjBudget.Features.Payroll.Engine;

public readonly record struct FicaYearToDate(decimal SocialSecurityTaxedWages, decimal SocialSecurityTax, decimal MedicareWages);

public readonly record struct FicaTaxes(
	decimal SocialSecurityTaxedWages,
	decimal SocialSecurity,
	decimal Medicare,
	decimal AdditionalMedicare);

/// <summary>
/// Social Security and Medicare as one employer withholds them. Both annual thresholds (the SS wage base and the
/// $200k Additional Medicare threshold) are per employer, so a second job starts over at zero.
/// </summary>
public static class FicaCalculator
{
	public static FicaTaxes Calculate(decimal socialSecurityWages, decimal medicareWages, FicaYearToDate ytd, FicaParameters fica)
	{
		var remainingWageBase = Math.Max(0m, fica.SocialSecurityWageBase - ytd.SocialSecurityTaxedWages);
		var ssTaxedWages = Math.Min(socialSecurityWages, remainingWageBase);

		// Rounding each check can push the year's total a few cents past the maximum; the final check absorbs it.
		var remainingSsTax = Math.Max(0m, fica.MaxSocialSecurityTax - ytd.SocialSecurityTax);
		var socialSecurity = Math.Min(Money.Round(ssTaxedWages * fica.SocialSecurityRate), remainingSsTax);

		var medicare = Money.Round(medicareWages * fica.MedicareRate);

		var wagesAboveThreshold = Math.Max(0m,
			ytd.MedicareWages + medicareWages - Math.Max(ytd.MedicareWages, fica.AdditionalMedicareWithholdingThreshold));
		var additionalMedicare = Money.Round(wagesAboveThreshold * fica.AdditionalMedicareRate);

		return new FicaTaxes(ssTaxedWages, socialSecurity, medicare, additionalMedicare);
	}
}
