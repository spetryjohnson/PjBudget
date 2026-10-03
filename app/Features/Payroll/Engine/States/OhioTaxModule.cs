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

	public IReadOnlyList<TaxProjectionSection> ProjectAnnualTaxes(StateProjectionContext context)
	{
		var ohio = context.TaxYear.Ohio;
		var inputs = context.Inputs;

		var ohioAgi = context.FederalAdjustedGrossIncome + inputs.StateAdjustments;

		// Ohio's MAGI adds back the business income deduction, which wage earners don't take, so Ohio AGI stands in.
		var magi = ohioAgi;
		var exemptionEach = magi < ohio.ExemptionMagiLimit ? ohio.ExemptionTiers.FindBracket(magi).BaseAmount : 0m;
		var exemptions = exemptionEach * inputs.StateExemptionCount;
		var taxableIncome = Math.Max(0m, ohioAgi - exemptions);

		var tax = Money.Round(ohio.IncomeTax.Calculate(taxableIncome));
		var jointFilingCredit = JointFilingCredit(context, magi, taxableIncome, tax);
		var otherCredits = Math.Min(inputs.StateCredits, tax - jointFilingCredit);

		var sections = new List<TaxProjectionSection>
		{
			new("Ohio",
			[
				new TaxProjectionLine("Ohio adjusted gross income", ohioAgi),
				new TaxProjectionLine($"Exemptions ({inputs.StateExemptionCount})", -exemptions),
				new TaxProjectionLine("Ohio taxable income", taxableIncome),
				new TaxProjectionLine("Income tax", tax),
				new TaxProjectionLine("Joint filing credit", -jointFilingCredit),
				new TaxProjectionLine("Other credits", -otherCredits),
			],
			tax - jointFilingCredit - otherCredits,
			context.Sources.Sum(s => s.Annual.StateIncomeTax)),
		};

		if (SchoolDistrictProjection(context, taxableIncome) is { } school)
		{
			sections.Add(school);
		}

		var cityWithheld = context.Sources.Sum(s => s.Annual.CityIncomeTax);
		if (cityWithheld > 0)
		{
			var homeRate = context.Home?.MunicipalTaxRate;
			var note = homeRate is null || context.Sources.All(s => s.Work.MunicipalTaxRate >= homeRate)
				? "Assumes withholding for your work cities settles city tax in full."
				: "Your home city may also tax wages earned in a lower-rate city; that isn't projected.";

			sections.Add(new TaxProjectionSection("City",
				[new TaxProjectionLine("Withheld for work cities", cityWithheld)], cityWithheld, cityWithheld, note));
		}

		return sections;
	}

	/// <summary>
	/// Ohio's credit for married couples who both earn income. It's a percentage of the tax, set by income tier and
	/// capped.
	/// </summary>
	private static decimal JointFilingCredit(StateProjectionContext context, decimal magi, decimal taxableIncome, decimal tax)
	{
		var ohio = context.TaxYear.Ohio;
		if (context.Inputs.FilingStatus != FilingStatus.MarriedFilingJointly || magi >= ohio.JointFilingCreditMagiLimit)
		{
			return 0m;
		}

		var earningSpouses = context.Sources
			.GroupBy(s => s.PersonId)
			.Count(g => g.Sum(s => s.Annual.TaxableWages.State) >= ohio.JointFilingCreditMinSpouseIncome);
		if (earningSpouses < 2)
		{
			return 0m;
		}

		var rate = ohio.JointFilingCreditTiers.FindBracket(taxableIncome).Rate;
		return Math.Min(Money.Round(tax * rate), ohio.JointFilingCreditCap);
	}

	private static TaxProjectionSection? SchoolDistrictProjection(StateProjectionContext context, decimal ohioTaxableIncome)
	{
		if (context.Home?.SchoolDistrictTaxRate is not { } rate || rate == 0m)
		{
			return null;
		}

		var traditional = context.Home.SchoolDistrictTaxBase == SchoolDistrictTaxBase.Traditional;
		var taxBase = traditional ? ohioTaxableIncome : context.Sources.Sum(s => s.ReturnWages(TaxableWageTypes.School));

		var note = $"Taxed at {rate * 100:0.###}%.";
		if (context.Sources.Any(s => s.Annual.SchoolDistrictTaxNotWithheld > 0))
		{
			note += " Not every employer withholds it, so pay the rest with the return or with SD 100ES estimated payments.";
		}

		return new TaxProjectionSection("School district",
			[new TaxProjectionLine(traditional ? "Ohio taxable income" : "Earned income", taxBase)],
			Money.Round(taxBase * rate),
			context.Sources.Sum(s => s.Annual.SchoolDistrictTax),
			note);
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
