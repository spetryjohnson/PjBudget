using PjBudget.Features.Payroll.Engine;
using PjBudget.Features.Payroll.Engine.States;
using PjBudget.Features.TaxYears;
using PjBudget.Shared.Domain;

namespace PjBudget.Tests.TestSupport;

/// <summary>
/// Synthetic payroll inputs. The amounts are round numbers chosen so every expected value can be checked by hand;
/// they are not anyone's real pay.
/// </summary>
public static class PayrollTestData
{
	public static readonly TaxYearParameters TaxYear2026Parameters = TaxYearMapper.ToParameters(TaxYear2026.Create());

	public static readonly WorkLocation WorkCity2Percent = new("OH", 0.02m);

	public static readonly HomeLocation HomeWithEarnedIncomeSchoolTax1Percent =
		new("OH", 0.02m, 0.01m, SchoolDistrictTaxBase.EarnedIncome);

	public static PaycheckSimulator Simulator() => new(new PayScheduleGenerator(), new StateTaxModules([new OhioTaxModule()]));

	public static PayrollSourceInput SemimonthlySalary(decimal annualSalary) => new()
	{
		PayBasis = PayBasis.Salary,
		AnnualSalary = annualSalary,
		Schedule = new PayScheduleSettings(PayFrequency.Semimonthly, 10, 25),
		W4 = new FederalW4(FilingStatus.MarriedFilingJointly),
	};

	public static PayrollSimulationRequest Request(
		PayrollSourceInput source,
		DateOnly? birthDate = null,
		HsaCoverage hsaCoverage = HsaCoverage.Family,
		HomeLocation? home = null,
		int year = 2026)
		=> new(year, source, birthDate, hsaCoverage, WorkCity2Percent, home ?? HomeWithEarnedIncomeSchoolTax1Percent, TaxYear2026Parameters);
}
