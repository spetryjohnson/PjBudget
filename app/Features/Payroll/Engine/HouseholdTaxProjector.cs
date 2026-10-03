using System.Globalization;
using PjBudget.Features.Payroll.Engine.States;
using PjBudget.Shared.Domain;

namespace PjBudget.Features.Payroll.Engine;

/// <summary>
/// A simple projection of the household's annual returns. It combines every source's simulated wages and compares
/// what's owed with what was withheld. It's meant to show whether withholding is roughly on target, so it covers
/// wages plus a few entered amounts, not every line of a real return.
/// </summary>
public sealed class HouseholdTaxProjector
{
	private static readonly CultureInfo UsCulture = CultureInfo.GetCultureInfo("en-US");

	private readonly StateTaxModules _states;

	public HouseholdTaxProjector(StateTaxModules states) => _states = states;

	public HouseholdTaxProjection Project(HouseholdProjectionRequest request)
	{
		var federal = Federal(request, out var agi);
		var sections = new List<TaxProjectionSection> { federal };

		// State income tax is owed to the state you live in; fall back to where you work if home isn't set.
		var stateCode = request.Home?.StateCode ?? request.Sources.FirstOrDefault()?.Work.StateCode;
		if (stateCode is not null && _states.IsSupported(stateCode))
		{
			sections.AddRange(_states.For(stateCode).ProjectAnnualTaxes(new StateProjectionContext(
				request.TaxYear, request.Inputs, request.Home, agi, request.Sources)));
		}

		return new HouseholdTaxProjection(request.Year, sections, Warnings(request));
	}

	private static TaxProjectionSection Federal(HouseholdProjectionRequest request, out decimal agi)
	{
		var inputs = request.Inputs;
		var rules = request.TaxYear.FederalRulesFor(inputs.FilingStatus);
		var sources = request.Sources;

		var wages = sources.Sum(s => s.ReturnWages(TaxableWageTypes.Federal));
		agi = wages + inputs.FederalOtherIncome - inputs.FederalAdjustments;

		var itemized = inputs.FederalItemizedDeductions ?? 0m;
		var deduction = Math.Max(rules.StandardDeduction, itemized);
		var taxableIncome = Math.Max(0m, agi - deduction);
		var incomeTax = Money.Round(rules.IncomeTaxBrackets.Calculate(taxableIncome));

		// Credits are treated as nonrefundable, which keeps the projection simple at some cost in precision.
		var credits = Math.Min(inputs.FederalCredits, incomeTax);

		// Withholding uses a $200k threshold per employer, but what's owed depends on combined wages and filing status.
		var medicareWages = sources.Sum(s => s.ReturnWages(TaxableWageTypes.Medicare));
		var additionalMedicare = Money.Round(
			Math.Max(0m, medicareWages - rules.AdditionalMedicareLiabilityThreshold) * request.TaxYear.Fica.AdditionalMedicareRate);

		var excessSocialSecurity = ExcessSocialSecurity(request);

		var lines = new List<TaxProjectionLine>
		{
			new("Wages", wages),
			new("Other income", inputs.FederalOtherIncome),
			new("Adjustments", -inputs.FederalAdjustments),
			new("Adjusted gross income", agi),
			new(itemized > rules.StandardDeduction ? "Itemized deductions" : "Standard deduction", -deduction),
			new("Taxable income", taxableIncome),
			new("Income tax", incomeTax),
			new("Credits", -credits),
			new("Additional Medicare tax", additionalMedicare),
		};
		if (excessSocialSecurity > 0)
		{
			lines.Add(new TaxProjectionLine("Excess Social Security withheld (refunded)", excessSocialSecurity));
		}

		var withheld = sources.Sum(s => s.Annual.FederalIncomeTax + s.Annual.AdditionalMedicareTax) + excessSocialSecurity;

		return new TaxProjectionSection("Federal", lines.Where(l => l.Amount != 0 || IsKeyLine(l)).ToList(),
			incomeTax - credits + additionalMedicare, withheld);
	}

	private static bool IsKeyLine(TaxProjectionLine line)
		=> line.Label is "Wages" or "Adjusted gross income" or "Taxable income" or "Income tax";

	/// <summary>
	/// Each employer withholds Social Security up to the wage base on its own, so someone with two jobs can
	/// overpay. The excess comes back as a credit on the return.
	/// </summary>
	private static decimal ExcessSocialSecurity(HouseholdProjectionRequest request)
		=> request.Sources
			.GroupBy(s => s.PersonId)
			.Where(g => g.Count() > 1)
			.Sum(g => Math.Max(0m, g.Sum(s => s.Annual.SocialSecurityTax) - request.TaxYear.Fica.MaxSocialSecurityTax));

	private static IReadOnlyList<PayrollWarning> Warnings(HouseholdProjectionRequest request)
	{
		var warnings = new List<PayrollWarning>();
		var limits = request.TaxYear.Limits;

		if (request.Home is null)
		{
			warnings.Add(new PayrollWarning("NO_HOME_LOCALE",
				"Set the household's home locale so school district tax is withheld and projected."));
		}

		foreach (var person in request.People)
		{
			var personSources = request.Sources.Where(s => s.PersonId == person.PersonId).ToList();
			if (personSources.Count < 2)
			{
				continue;
			}

			var age = person.BirthDate is { } birthDate ? request.Year - birthDate.Year : (int?)null;
			var limit = limits.ElectiveDeferralLimitFor(age);
			var deferred = personSources.Sum(s => s.Annual.Traditional401k);
			if (deferred > limit)
			{
				warnings.Add(new PayrollWarning("401K_COMBINED_OVER_LIMIT", string.Create(UsCulture,
					$"{person.Name}'s 401(k) contributions across jobs total {deferred:C0}, over the {limit:C0} limit. Each employer only caps its own plan, so the excess would need to be withdrawn.")));
			}
		}

		if (request.HsaCoverage == HsaCoverage.Family)
		{
			// Spouses with family coverage share one limit, but each one 55 or older can add their own catch-up.
			var catchUps = request.People.Count(p => p.BirthDate is { } b && request.Year - b.Year >= 55) * limits.HsaCatchUpAge55;
			var familyLimit = limits.HsaFamily + catchUps;
			var contributed = request.Sources.Sum(s => s.Annual.HsaEmployee + s.Annual.EmployerHsa);
			if (contributed > familyLimit)
			{
				warnings.Add(new PayrollWarning("HSA_COMBINED_OVER_LIMIT", string.Create(UsCulture,
					$"HSA contributions across all sources total {contributed:C0}, over the {familyLimit:C0} family limit.")));
			}
		}

		return warnings;
	}
}
