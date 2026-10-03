using PjBudget.Features.Payroll.Engine;
using PjBudget.Shared.Domain;

namespace PjBudget.Features.TaxYears;

internal static class TaxYearMapper
{
	public static TaxYearModel ToModel(TaxYear entity) => new()
	{
		Year = entity.Year,
		Version = entity.Version,
		Fica = Clone(entity.Fica),
		Limits = Clone(entity.Limits),
		Ohio = Clone(entity.Ohio),
		FilingStatuses = entity.FilingStatuses
			.OrderBy(f => f.FilingStatus)
			.Select(f => new FilingStatusParametersModel
			{
				FilingStatus = f.FilingStatus,
				StandardDeduction = f.StandardDeduction,
				StandardWithholdingAdjustment = f.StandardWithholdingAdjustment,
				AdditionalMedicareLiabilityThreshold = f.AdditionalMedicareLiabilityThreshold,
			})
			.ToList(),
		Schedules = entity.ScheduleRows
			.GroupBy(r => (r.Kind, r.FilingStatus))
			.OrderBy(g => g.Key.Kind)
			.ThenBy(g => g.Key.FilingStatus)
			.Select(g => new TaxScheduleModel
			{
				Kind = g.Key.Kind,
				FilingStatus = g.Key.FilingStatus,
				Rows = g.OrderBy(r => r.Over)
					.Select(r => new TaxScheduleRowModel { Over = r.Over, BaseAmount = r.BaseAmount, Rate = r.Rate })
					.ToList(),
			})
			.ToList(),
	};

	/// <summary>
	/// Copies everything except identity and concurrency fields. Child rows are rebuilt rather than matched, because
	/// they have no identity of their own beyond their position in the schedule.
	/// </summary>
	public static void Apply(TaxYearModel model, TaxYear entity)
	{
		entity.Year = model.Year;
		entity.Fica = Clone(model.Fica);
		entity.Limits = Clone(model.Limits);
		entity.Ohio = Clone(model.Ohio);

		entity.FilingStatuses.Clear();
		entity.FilingStatuses.AddRange(model.FilingStatuses.Select(f => new FederalFilingStatusParameters
		{
			FilingStatus = f.FilingStatus,
			StandardDeduction = f.StandardDeduction,
			StandardWithholdingAdjustment = f.StandardWithholdingAdjustment,
			AdditionalMedicareLiabilityThreshold = f.AdditionalMedicareLiabilityThreshold,
		}));

		entity.ScheduleRows.Clear();
		entity.ScheduleRows.AddRange(model.Schedules.SelectMany(s => s.Rows.Select(r => new TaxScheduleRow
		{
			Kind = s.Kind,
			FilingStatus = s.FilingStatus,
			Over = r.Over,
			BaseAmount = r.BaseAmount,
			Rate = r.Rate,
		})));
	}

	public static TaxYearParameters ToParameters(TaxYear entity)
	{
		var federal = entity.FilingStatuses.ToDictionary(
			f => f.FilingStatus,
			f => new FederalFilingStatusRules(
				f.StandardDeduction,
				f.StandardWithholdingAdjustment,
				f.AdditionalMedicareLiabilityThreshold,
				TaxSchedule.FromMarginalRates(entity.ScheduleRows
					.Where(r => r.Kind == TaxScheduleKind.FederalIncome && r.FilingStatus == f.FilingStatus)
					.Select(r => (r.Over, r.Rate)))));

		return new TaxYearParameters(
			entity.Year,
			new FicaParameters(
				entity.Fica.SocialSecurityRate,
				entity.Fica.SocialSecurityWageBase,
				entity.Fica.MedicareRate,
				entity.Fica.AdditionalMedicareRate,
				entity.Fica.AdditionalMedicareWithholdingThreshold),
			new ContributionLimits(
				entity.Limits.ElectiveDeferral,
				entity.Limits.CatchUpAge50,
				entity.Limits.CatchUpAge60To63,
				entity.Limits.HsaSelfOnly,
				entity.Limits.HsaFamily,
				entity.Limits.HsaCatchUpAge55,
				entity.Limits.HealthFsa),
			federal,
			new OhioParameters(
				entity.Ohio.WithholdingExemptionAmount,
				Schedule(entity, TaxScheduleKind.OhioWithholding),
				Schedule(entity, TaxScheduleKind.OhioIncome),
				Schedule(entity, TaxScheduleKind.OhioExemption),
				entity.Ohio.ExemptionMagiLimit,
				Schedule(entity, TaxScheduleKind.OhioJointFilingCredit),
				entity.Ohio.JointFilingCreditCap,
				entity.Ohio.JointFilingCreditMagiLimit,
				entity.Ohio.JointFilingCreditMinSpouseIncome));
	}

	private static TaxSchedule Schedule(TaxYear entity, TaxScheduleKind kind)
		=> new(entity.ScheduleRows
			.Where(r => r.Kind == kind)
			.Select(r => new TaxBracket(r.Over, r.BaseAmount, r.Rate)));

	private static TaxYearFica Clone(TaxYearFica source) => new()
	{
		SocialSecurityRate = source.SocialSecurityRate,
		SocialSecurityWageBase = source.SocialSecurityWageBase,
		MedicareRate = source.MedicareRate,
		AdditionalMedicareRate = source.AdditionalMedicareRate,
		AdditionalMedicareWithholdingThreshold = source.AdditionalMedicareWithholdingThreshold,
	};

	private static TaxYearLimits Clone(TaxYearLimits source) => new()
	{
		ElectiveDeferral = source.ElectiveDeferral,
		CatchUpAge50 = source.CatchUpAge50,
		CatchUpAge60To63 = source.CatchUpAge60To63,
		HsaSelfOnly = source.HsaSelfOnly,
		HsaFamily = source.HsaFamily,
		HsaCatchUpAge55 = source.HsaCatchUpAge55,
		HealthFsa = source.HealthFsa,
	};

	private static TaxYearOhio Clone(TaxYearOhio source) => new()
	{
		WithholdingExemptionAmount = source.WithholdingExemptionAmount,
		ExemptionMagiLimit = source.ExemptionMagiLimit,
		JointFilingCreditCap = source.JointFilingCreditCap,
		JointFilingCreditMagiLimit = source.JointFilingCreditMagiLimit,
		JointFilingCreditMinSpouseIncome = source.JointFilingCreditMinSpouseIncome,
	};
}
