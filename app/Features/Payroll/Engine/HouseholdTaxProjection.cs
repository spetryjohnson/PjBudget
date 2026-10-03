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
public sealed record SourceYear(int PersonId, string Name, WorkLocation Work, PaycheckLines Annual);

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
