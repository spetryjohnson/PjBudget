using PjBudget.Shared.Domain;

namespace PjBudget.Features.Payroll.Engine;

/// <summary>
/// Everything the annual projection needs: each source's simulated year, plus the household-level inputs a tax
/// return has beyond wages.
/// </summary>
public sealed record HouseholdProjectionRequest(
	int Year,
	TaxYearParameters TaxYear,
	HouseholdTaxInputs Inputs,
	HomeLocation? Home,
	HsaCoverage HsaCoverage,
	IReadOnlyList<PersonInfo> People,
	IReadOnlyList<SourceYear> Sources);

/// <param name="StateAdjustments">Additions (+) or deductions (−) to federal AGI on the state return.</param>
public sealed record HouseholdTaxInputs(
	FilingStatus FilingStatus,
	decimal FederalOtherIncome,
	decimal FederalAdjustments,
	decimal? FederalItemizedDeductions,
	decimal FederalCredits,
	decimal StateAdjustments,
	int StateExemptionCount,
	decimal StateCredits);

public sealed record PersonInfo(int PersonId, string Name, DateOnly? BirthDate);

/// <summary>One payroll source's simulated year, with the person it belongs to.</summary>
/// <param name="GroupTermLifeTaxedFor">The wage bases payroll adds this job's taxable life insurance to.</param>
public sealed record SourceYear(
	int PersonId,
	string Name,
	WorkLocation Work,
	PaycheckLines Annual,
	TaxableWageTypes GroupTermLifeTaxedFor = DefaultTaxTreatment.GroupTermLife)
{
	/// <summary>
	/// Wages for one tax as the annual return counts them. Taxable life insurance is income on the return even when
	/// payroll leaves it out of that tax's withholding.
	/// </summary>
	public decimal ReturnWages(TaxableWageTypes type)
		=> Annual.TaxableWages.For(type) + (GroupTermLifeTaxedFor.HasFlag(type) ? 0m : Annual.GroupTermLife);
}

public sealed record HouseholdTaxProjection(
	int Year,
	IReadOnlyList<TaxProjectionSection> Sections,
	IReadOnlyList<PayrollWarning> Warnings)
{
	public decimal TotalLiability => Sections.Sum(s => s.Liability);
	public decimal TotalWithheld => Sections.Sum(s => s.Withheld);

	/// <summary>Positive is a projected refund; negative is a balance due.</summary>
	public decimal TotalRefundOrBalanceDue => TotalWithheld - TotalLiability;
}

/// <summary>
/// One jurisdiction's return in miniature: the lines that lead to the liability, compared with what was withheld.
/// </summary>
public sealed record TaxProjectionSection(
	string Title,
	IReadOnlyList<TaxProjectionLine> Lines,
	decimal Liability,
	decimal Withheld,
	string? Note = null)
{
	/// <summary>Positive is a projected refund; negative is a balance due.</summary>
	public decimal RefundOrBalanceDue => Withheld - Liability;
}

public sealed record TaxProjectionLine(string Label, decimal Amount);
