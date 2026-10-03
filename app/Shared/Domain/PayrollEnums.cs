using PjBudget.Framework;

namespace PjBudget.Shared.Domain;

public enum PayFrequency
{
	/// <summary>Twice a month on fixed days: 24 checks a year.</summary>
	[StringConstant("SEMIMONTHLY")] Semimonthly,

	/// <summary>Every 14 days: 26 checks in most years, 27 in some.</summary>
	[StringConstant("BIWEEKLY")] Biweekly,
}

public enum PayBasis
{
	[StringConstant("SALARY")] Salary,
	[StringConstant("HOURLY")] Hourly,
}

public enum DeductionType
{
	[StringConstant("MEDICAL")] Medical,
	[StringConstant("DENTAL")] Dental,
	[StringConstant("VISION")] Vision,
	[StringConstant("LIFE")] Life,
	[StringConstant("DISABILITY")] Disability,
	[StringConstant("LEGAL")] Legal,
	[StringConstant("OTHER")] Other,
}

/// <summary>
/// High-deductible health plan coverage. It sets the household's HSA contribution limit.
/// </summary>
public enum HsaCoverage
{
	[StringConstant("NONE")] None,
	[StringConstant("SELF_ONLY")] SelfOnly,
	[StringConstant("FAMILY")] Family,
}
