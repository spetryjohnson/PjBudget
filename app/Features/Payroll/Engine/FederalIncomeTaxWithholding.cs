namespace PjBudget.Features.Payroll.Engine;

/// <summary>
/// Federal income tax withholding by the IRS Pub 15-T percentage method for automated payroll systems
/// (Worksheet 1A, 2020-or-later W-4). Each check is annualized on its own, so checks with equal wages withhold equally.
/// </summary>
public sealed class FederalIncomeTaxWithholding
{
	private readonly FederalW4 _w4;
	private readonly int _periodsPerYear;
	private readonly decimal _line1g;
	private readonly TaxSchedule _table;

	public FederalIncomeTaxWithholding(FederalW4 w4, FederalFilingStatusRules rules, int periodsPerYear)
	{
		_w4 = w4;
		_periodsPerYear = periodsPerYear;

		// The checkbox tables already assume two jobs, so they get no separate standard-deduction allowance.
		_line1g = w4.MultipleJobs ? 0m : rules.StandardWithholdingAdjustment;
		_table = w4.MultipleJobs ? FederalWithholdingTables.Step2Checkbox(rules) : FederalWithholdingTables.Standard(rules);
	}

	public decimal Calculate(decimal federalWages)
	{
		var annualWages = federalWages * _periodsPerYear + _w4.OtherIncome;                      // lines 1c-1e
		var adjustedAnnualWage = Math.Max(0m, annualWages - (_w4.Deductions + _line1g));           // lines 1f-1i
		var tentativePerPeriod = Money.Round(_table.Calculate(adjustedAnnualWage) / _periodsPerYear); // lines 2a-2h
		var creditsPerPeriod = Money.Round(_w4.Credits / _periodsPerYear);                          // lines 3a-3b

		return Math.Max(0m, tentativePerPeriod - creditsPerPeriod) + _w4.ExtraWithholding;          // lines 3c-4b
	}
}
