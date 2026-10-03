namespace PjBudget.Features.Payroll.Engine;

public readonly record struct FicaYearToDate(
	decimal SocialSecurityTaxedWages,
	decimal SocialSecurityTax,
	decimal MedicareWages,
	decimal MedicareTax,
	decimal AdditionalMedicareTax);

public readonly record struct FicaTaxes(
	decimal SocialSecurityTaxedWages,
	decimal SocialSecurity,
	decimal Medicare,
	decimal AdditionalMedicare);

/// <summary>
/// Social Security and Medicare as one employer withholds them. Both annual thresholds (the SS wage base and the
/// $200k Additional Medicare threshold) are per employer, so a second job starts over at zero.
/// </summary>
/// <remarks>
/// Each tax is figured on year-to-date wages and rounded once, and a check withholds whatever brings the year's total
/// up to that. ADP works this way, so a check can be a cent more or less than its own wages times the rate. Rounding
/// each check separately would drift from real paystubs. Figuring the year's total this way also makes Social
/// Security end exactly on the annual maximum.
/// </remarks>
public static class FicaCalculator
{
	public static FicaTaxes Calculate(decimal socialSecurityWages, decimal medicareWages, FicaYearToDate ytd, FicaParameters fica)
	{
		var remainingWageBase = Math.Max(0m, fica.SocialSecurityWageBase - ytd.SocialSecurityTaxedWages);
		var ssTaxedWages = Math.Min(socialSecurityWages, remainingWageBase);
		var socialSecurity = DueThisCheck(ytd.SocialSecurityTaxedWages + ssTaxedWages, fica.SocialSecurityRate, ytd.SocialSecurityTax);

		var medicareWagesToDate = ytd.MedicareWages + medicareWages;
		var medicare = DueThisCheck(medicareWagesToDate, fica.MedicareRate, ytd.MedicareTax);

		var wagesAboveThreshold = Math.Max(0m, medicareWagesToDate - fica.AdditionalMedicareWithholdingThreshold);
		var additionalMedicare = DueThisCheck(wagesAboveThreshold, fica.AdditionalMedicareRate, ytd.AdditionalMedicareTax);

		return new FicaTaxes(ssTaxedWages, socialSecurity, medicare, additionalMedicare);
	}

	/// <summary>The tax on wages so far this year, less what earlier checks already withheld.</summary>
	private static decimal DueThisCheck(decimal wagesToDate, decimal rate, decimal alreadyWithheld)
		=> Math.Max(0m, Money.Round(wagesToDate * rate) - alreadyWithheld);
}
