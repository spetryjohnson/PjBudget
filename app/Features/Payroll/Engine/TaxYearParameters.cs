using PjBudget.Shared.Domain;

namespace PjBudget.Features.Payroll.Engine;

/// <summary>
/// Everything the engine needs to know about one tax year's rules. It is immutable, and it is built from the stored
/// tax-year data so that the engine never touches the database.
/// </summary>
public sealed record TaxYearParameters(
	int Year,
	FicaParameters Fica,
	ContributionLimits Limits,
	IReadOnlyDictionary<FilingStatus, FederalFilingStatusRules> Federal,
	OhioParameters Ohio)
{
	public FederalFilingStatusRules FederalRulesFor(FilingStatus filingStatus)
		=> Federal.TryGetValue(filingStatus, out var rules)
			? rules
			: throw new InvalidOperationException($"Tax year {Year} has no federal rules for {filingStatus}.");
}

public sealed record FicaParameters(
	decimal SocialSecurityRate,
	decimal SocialSecurityWageBase,
	decimal MedicareRate,
	decimal AdditionalMedicareRate,
	decimal AdditionalMedicareWithholdingThreshold)
{
	/// <summary>
	/// The most Social Security tax one employer can withhold in a year. Per-check rounding could otherwise overshoot
	/// it by a few cents.
	/// </summary>
	public decimal MaxSocialSecurityTax => Money.Round(SocialSecurityWageBase * SocialSecurityRate);
}

public sealed record ContributionLimits(
	decimal ElectiveDeferral,
	decimal CatchUpAge50,
	decimal CatchUpAge60To63,
	decimal HsaSelfOnly,
	decimal HsaFamily,
	decimal HsaCatchUpAge55,
	decimal HealthFsa)
{
	/// <summary>
	/// The 401(k) elective deferral limit for someone who reaches <paramref name="ageAtYearEnd"/> by December 31.
	/// Ages 60–63 get the larger "super" catch-up instead of the regular one, not in addition to it.
	/// </summary>
	public decimal ElectiveDeferralLimitFor(int? ageAtYearEnd) => ageAtYearEnd switch
	{
		>= 60 and <= 63 => ElectiveDeferral + CatchUpAge60To63,
		>= 50 => ElectiveDeferral + CatchUpAge50,
		_ => ElectiveDeferral,
	};

	public decimal HsaLimitFor(HsaCoverage coverage, int? ageAtYearEnd)
	{
		var baseLimit = coverage switch
		{
			HsaCoverage.SelfOnly => HsaSelfOnly,
			HsaCoverage.Family => HsaFamily,
			_ => 0m,
		};

		return baseLimit > 0 && ageAtYearEnd >= 55 ? baseLimit + HsaCatchUpAge55 : baseLimit;
	}
}

public sealed record FederalFilingStatusRules(
	decimal StandardDeduction,
	decimal StandardWithholdingAdjustment,
	decimal AdditionalMedicareLiabilityThreshold,
	TaxSchedule IncomeTaxBrackets);

public sealed record OhioParameters(
	decimal WithholdingExemptionAmount,
	TaxSchedule Withholding,
	TaxSchedule IncomeTax,
	TaxSchedule ExemptionTiers,
	decimal ExemptionMagiLimit,
	TaxSchedule JointFilingCreditTiers,
	decimal JointFilingCreditCap,
	decimal JointFilingCreditMagiLimit,
	decimal JointFilingCreditMinSpouseIncome);
