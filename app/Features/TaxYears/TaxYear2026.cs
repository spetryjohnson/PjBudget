using PjBudget.Shared.Domain;

namespace PjBudget.Features.TaxYears;

/// <summary>
/// Published 2026 values, used to seed a new database. Users maintain later years by copying and editing in the UI.
/// </summary>
/// <remarks>
/// Sources:
/// <list type="bullet">
/// <item>Federal brackets and standard deductions: Rev. Proc. 2025-32. These are cross-checked against every row of
/// the IRS Pub 15-T (2026) Annual Percentage Method tables, which the engine derives from them.</item>
/// <item>Line 1g amounts: Pub 15-T (2026) Worksheet 1A.</item>
/// <item>Contribution limits: IRS 2026 cost-of-living adjustments.</item>
/// <item>Ohio withholding: the Optional Computer Formula effective 2026-08-01.</item>
/// <item>Ohio income tax and credits: ORC 5747.02 (rates), 5747.025 (exemptions) and 5747.05 (joint filing
/// credit).</item>
/// </list>
/// </remarks>
public static class TaxYear2026
{
	public static TaxYear Create()
	{
		var taxYear = new TaxYear
		{
			Year = 2026,
			Fica = new TaxYearFica
			{
				SocialSecurityRate = 0.062m,
				SocialSecurityWageBase = 184_500m,
				MedicareRate = 0.0145m,
				AdditionalMedicareRate = 0.009m,
				AdditionalMedicareWithholdingThreshold = 200_000m,
			},
			Limits = new TaxYearLimits
			{
				ElectiveDeferral = 24_500m,
				CatchUpAge50 = 8_000m,
				CatchUpAge60To63 = 11_250m,
				HsaSelfOnly = 4_400m,
				HsaFamily = 8_750m,
				HsaCatchUpAge55 = 1_000m,
				HealthFsa = 3_400m,
			},
			Ohio = new TaxYearOhio
			{
				WithholdingExemptionAmount = 650m,
				ExemptionMagiLimit = 500_000m,
				JointFilingCreditCap = 650m,
				JointFilingCreditMagiLimit = 500_000m,
				JointFilingCreditMinSpouseIncome = 500m,
			},
			FilingStatuses =
			[
				FilingStatusParameters(FilingStatus.Single, standardDeduction: 16_100m, line1g: 8_600m, additionalMedicare: 200_000m),
				FilingStatusParameters(FilingStatus.MarriedFilingJointly, standardDeduction: 32_200m, line1g: 12_900m, additionalMedicare: 250_000m),
				FilingStatusParameters(FilingStatus.HeadOfHousehold, standardDeduction: 24_150m, line1g: 8_600m, additionalMedicare: 200_000m),
			],
		};

		AddFederalBrackets(taxYear, FilingStatus.Single,
			(0m, 0.10m), (12_400m, 0.12m), (50_400m, 0.22m), (105_700m, 0.24m), (201_775m, 0.32m), (256_225m, 0.35m), (640_600m, 0.37m));
		AddFederalBrackets(taxYear, FilingStatus.MarriedFilingJointly,
			(0m, 0.10m), (24_800m, 0.12m), (100_800m, 0.22m), (211_400m, 0.24m), (403_550m, 0.32m), (512_450m, 0.35m), (768_700m, 0.37m));
		AddFederalBrackets(taxYear, FilingStatus.HeadOfHousehold,
			(0m, 0.10m), (17_700m, 0.12m), (67_450m, 0.22m), (105_700m, 0.24m), (201_750m, 0.32m), (256_200m, 0.35m), (640_600m, 0.37m));

		AddSchedule(taxYear, TaxScheduleKind.OhioWithholding,
			(0m, 0m, 0.016m), (26_050m, 416.80m, 0.0299m), (100_000m, 2_627.91m, 0.034m));
		AddSchedule(taxYear, TaxScheduleKind.OhioIncome,
			(0m, 0m, 0m), (26_050m, 332m, 0.0275m));
		AddSchedule(taxYear, TaxScheduleKind.OhioExemption,
			(0m, 2_350m, 0m), (40_000m, 2_100m, 0m), (80_000m, 1_850m, 0m));
		AddSchedule(taxYear, TaxScheduleKind.OhioJointFilingCredit,
			(0m, 0m, 0.20m), (25_000m, 0m, 0.15m), (50_000m, 0m, 0.10m), (75_000m, 0m, 0.05m));

		return taxYear;
	}

	private static FederalFilingStatusParameters FilingStatusParameters(
		FilingStatus status, decimal standardDeduction, decimal line1g, decimal additionalMedicare) => new()
	{
		FilingStatus = status,
		StandardDeduction = standardDeduction,
		StandardWithholdingAdjustment = line1g,
		AdditionalMedicareLiabilityThreshold = additionalMedicare,
	};

	private static void AddFederalBrackets(TaxYear taxYear, FilingStatus status, params (decimal Over, decimal Rate)[] brackets)
	{
		taxYear.ScheduleRows.AddRange(brackets.Select(b => new TaxScheduleRow
		{
			Kind = TaxScheduleKind.FederalIncome,
			FilingStatus = status,
			Over = b.Over,
			Rate = b.Rate,
		}));
	}

	private static void AddSchedule(TaxYear taxYear, TaxScheduleKind kind, params (decimal Over, decimal BaseAmount, decimal Rate)[] rows)
	{
		taxYear.ScheduleRows.AddRange(rows.Select(r => new TaxScheduleRow
		{
			Kind = kind,
			Over = r.Over,
			BaseAmount = r.BaseAmount,
			Rate = r.Rate,
		}));
	}
}
