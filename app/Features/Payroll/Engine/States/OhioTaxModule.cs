using PjBudget.Shared.Domain;

namespace PjBudget.Features.Payroll.Engine.States;

/// <summary>
/// Ohio employer withholding. State tax follows the Department of Taxation's optional computer formula, city tax is
/// withheld for the work city, and school district tax is withheld for the district the employee lives in.
/// </summary>
public sealed class OhioTaxModule : IStateTaxModule
{
	public string StateCode => "OH";

	public StateAndLocalWithholding CalculateWithholding(StateWithholdingContext context, TaxableWages wages)
	{
		var ohio = context.TaxYear.Ohio;
		var periods = context.PeriodsPerYear;

		// The formula works on annual wages after IT 4 exemptions, and the result is spread back over the year's checks.
		var annualTaxableWages = Math.Max(0m,
			wages.State * periods - ohio.WithholdingExemptionAmount * context.Elections.Exemptions);
		var state = Money.Round(ohio.Withholding.Calculate(annualTaxableWages) / periods)
		            + context.Elections.AdditionalWithholding;

		var city = Money.Round(wages.City * context.Work.MunicipalTaxRate);

		return new StateAndLocalWithholding(state, city, SchoolDistrict(context, wages, annualTaxableWages));
	}

	private static decimal SchoolDistrict(StateWithholdingContext context, TaxableWages wages, decimal annualTaxableStateWages)
	{
		if (context.Home?.SchoolDistrictTaxRate is not { } rate || rate == 0m)
		{
			return 0m;
		}

		// Traditional-base districts withhold on the same wages and exemptions as state tax. Earned-income districts
		// use a flat rate with no exemptions.
		return context.Home.SchoolDistrictTaxBase == SchoolDistrictTaxBase.Traditional
			? Money.Round(annualTaxableStateWages * rate / context.PeriodsPerYear)
			: Money.Round(wages.School * rate);
	}
}
