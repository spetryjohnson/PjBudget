namespace PjBudget.Features.Payroll.Engine;

/// <summary>
/// Builds the IRS Pub 15-T "Annual Percentage Method" withholding tables from the statutory income tax brackets,
/// so only the brackets need to be maintained each year.
/// </summary>
/// <remarks>
/// The published tables are the statutory brackets shifted by an allowance for the standard deduction.
/// <list type="bullet">
/// <item>STANDARD schedule (W-4 Step 2 box unchecked): shifted by the standard deduction minus the Worksheet 1A line
/// 1g amount, because line 1g has already been subtracted from wages.</item>
/// <item>CHECKBOX schedule (box checked): uses half of every bracket width and half of the standard deduction, which
/// approximates a household with two jobs paying about the same.</item>
/// </list>
/// The IRS publishes thresholds rounded to whole dollars and base amounts rounded to cents, with the bases computed
/// from the unrounded thresholds. Reproducing that rounding makes the derived tables match the published ones
/// exactly.
/// </remarks>
public static class FederalWithholdingTables
{
	public static TaxSchedule Standard(FederalFilingStatusRules rules)
		=> Derive(rules.IncomeTaxBrackets, offset: rules.StandardDeduction - rules.StandardWithholdingAdjustment, scale: 1m);

	public static TaxSchedule Step2Checkbox(FederalFilingStatusRules rules)
		=> Derive(rules.IncomeTaxBrackets, offset: rules.StandardDeduction / 2, scale: 0.5m);

	private static TaxSchedule Derive(TaxSchedule incomeTax, decimal offset, decimal scale)
	{
		var rows = new List<TaxBracket> { new(0m, 0m, 0m) };

		foreach (var bracket in incomeTax.Brackets)
		{
			var start = offset + bracket.Over * scale;
			var baseAmount = bracket.BaseAmount * scale;

			rows.Add(new TaxBracket(Money.RoundToDollar(start), Money.Round(baseAmount), bracket.Rate));
		}

		return new TaxSchedule(rows, inclusiveLowerBounds: true);
	}
}
