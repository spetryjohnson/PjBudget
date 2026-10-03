using PjBudget.Framework;

namespace PjBudget.Shared.Domain;

/// <summary>
/// Federal filing status, as chosen on a W-4 or a tax return. "Single" also covers married filing separately,
/// which uses the same withholding tables.
/// </summary>
public enum FilingStatus
{
	[StringConstant("SINGLE")] Single,
	[StringConstant("MFJ")] MarriedFilingJointly,
	[StringConstant("HOH")] HeadOfHousehold,
}

/// <summary>
/// The bracketed schedules stored for a tax year. Every schedule has the same "over / base amount / rate" shape,
/// so they share one table. Tier schedules use only the base amount (an amount per exemption) or only the rate.
/// </summary>
public enum TaxScheduleKind
{
	/// <summary>Statutory federal income tax brackets, one schedule per filing status.</summary>
	[StringConstant("FEDERAL_INCOME")] FederalIncome,

	/// <summary>Ohio's optional computer formula for employer withholding.</summary>
	[StringConstant("OHIO_WITHHOLDING")] OhioWithholding,

	/// <summary>Ohio income tax owed on the annual return.</summary>
	[StringConstant("OHIO_INCOME")] OhioIncome,

	/// <summary>Ohio personal exemption amount by modified AGI; the base amount is the amount per exemption.</summary>
	[StringConstant("OHIO_EXEMPTION")] OhioExemption,

	/// <summary>Ohio joint filing credit percentage by income less exemptions; the rate is the credit percentage.</summary>
	[StringConstant("OHIO_JOINT_FILING_CREDIT")] OhioJointFilingCredit,
}

/// <summary>
/// The wage bases a payroll deduction can reduce. A deduction that is "pre-tax" for a wage type is subtracted from
/// gross pay before that tax is calculated.
/// </summary>
[Flags]
public enum TaxableWageTypes
{
	[StringConstant("NONE")] None = 0,
	[StringConstant("FED")] Federal = 1,
	[StringConstant("STATE")] State = 2,
	[StringConstant("SS")] SocialSecurity = 4,
	[StringConstant("MEDICARE")] Medicare = 8,
	[StringConstant("CITY")] City = 16,
	[StringConstant("SCHOOL")] School = 32,

	All = Federal | State | SocialSecurity | Medicare | City | School,
}

/// <summary>
/// Which income an Ohio school district taxes. This determines both what is withheld and what is owed.
/// </summary>
public enum SchoolDistrictTaxBase
{
	/// <summary>Wages and self-employment income only, with no personal exemptions.</summary>
	[StringConstant("EARNED_INCOME")] EarnedIncome,

	/// <summary>Ohio taxable income (Ohio AGI less exemptions); withheld on the same wages and exemptions as state tax.</summary>
	[StringConstant("TRADITIONAL")] Traditional,
}
