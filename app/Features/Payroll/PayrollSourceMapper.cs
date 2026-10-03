using PjBudget.Features.Payroll.Engine;
using PjBudget.Shared.Domain;

namespace PjBudget.Features.Payroll;

internal static class PayrollSourceMapper
{
	public static PayrollSourceModel ToModel(PayrollSource s) => new()
	{
		Id = s.Id,
		Version = s.Version,
		PersonId = s.PersonId,
		Name = s.Name,
		EmployerName = s.EmployerName,
		SortOrder = s.SortOrder,
		WorkLocaleId = s.WorkLocaleId,
		PayBasis = s.PayBasis,
		AnnualSalary = s.AnnualSalary,
		HourlyRate = s.HourlyRate,
		HoursPerCheck = s.HoursPerCheck,
		PayFrequency = s.PayFrequency,
		SemimonthlyPayDay1 = s.SemimonthlyPayDay1,
		SemimonthlyPayDay2 = s.SemimonthlyPayDay2,
		BiweeklyAnchorDate = s.BiweeklyAnchorDate,
		Traditional401kPercent = s.Traditional401kPercent,
		Traditional401kPerCheckOverride = s.Traditional401kPerCheckOverride,
		Traditional401kPreTaxFor = s.Traditional401kPreTaxFor,
		EmployerNonElectivePercent = s.EmployerNonElectivePercent,
		EmployerMatchPercent = s.EmployerMatchPercent,
		EmployerMatchCapPercent = s.EmployerMatchCapPercent,
		HsaEmployeePerCheck = s.HsaEmployeePerCheck,
		HsaEmployerPerCheck = s.HsaEmployerPerCheck,
		HsaPreTaxFor = s.HsaPreTaxFor,
		HealthFsaAnnualElection = s.HealthFsaAnnualElection,
		HealthFsaPerCheckOverride = s.HealthFsaPerCheckOverride,
		HealthFsaPreTaxFor = s.HealthFsaPreTaxFor,
		StipendPerCheck = s.StipendPerCheck,
		StipendIsTaxable = s.StipendIsTaxable,
		W4FilingStatus = s.W4FilingStatus,
		W4MultipleJobs = s.W4MultipleJobs,
		W4Credits = s.W4Credits,
		W4OtherIncome = s.W4OtherIncome,
		W4Deductions = s.W4Deductions,
		W4ExtraWithholding = s.W4ExtraWithholding,
		StateWithholdingExemptions = s.StateWithholdingExemptions,
		StateAdditionalWithholding = s.StateAdditionalWithholding,
		NetPayAdjustmentPerCheck = s.NetPayAdjustmentPerCheck,
		ActualNetPay = s.ActualNetPay,
		ActualNetPayDate = s.ActualNetPayDate,
		Deductions = s.Deductions
			.OrderBy(d => d.SortOrder)
			.Select(d => new PayrollDeductionModel
			{
				Type = d.Type,
				Label = d.Label,
				AnnualAmount = d.AnnualAmount,
				PerCheckOverride = d.PerCheckOverride,
				PreTaxFor = d.PreTaxFor,
			})
			.ToList(),
	};

	/// <summary>
	/// Copies the editable fields onto an entity. Settings that don't apply to the chosen pay basis or frequency are
	/// cleared, so a stored source never carries stale values the engine would ignore.
	/// </summary>
	public static void Apply(PayrollSourceModel m, PayrollSource s)
	{
		var hourly = m.PayBasis == PayBasis.Hourly;
		var semimonthly = m.PayFrequency == PayFrequency.Semimonthly;

		s.PersonId = m.PersonId;
		s.Name = m.Name.Trim();
		s.EmployerName = string.IsNullOrWhiteSpace(m.EmployerName) ? null : m.EmployerName.Trim();
		s.SortOrder = m.SortOrder;
		s.WorkLocaleId = m.WorkLocaleId;
		s.PayBasis = m.PayBasis;
		s.AnnualSalary = hourly ? null : m.AnnualSalary;
		s.HourlyRate = hourly ? m.HourlyRate : null;
		s.HoursPerCheck = hourly ? m.HoursPerCheck : null;
		s.PayFrequency = m.PayFrequency;
		s.SemimonthlyPayDay1 = semimonthly ? m.SemimonthlyPayDay1 : null;
		s.SemimonthlyPayDay2 = semimonthly ? m.SemimonthlyPayDay2 : null;
		s.BiweeklyAnchorDate = semimonthly ? null : m.BiweeklyAnchorDate;
		s.Traditional401kPercent = m.Traditional401kPercent;
		s.Traditional401kPerCheckOverride = m.Traditional401kPerCheckOverride;
		s.Traditional401kPreTaxFor = m.Traditional401kPreTaxFor;
		s.EmployerNonElectivePercent = m.EmployerNonElectivePercent;
		s.EmployerMatchPercent = m.EmployerMatchPercent;
		s.EmployerMatchCapPercent = m.EmployerMatchCapPercent;
		s.HsaEmployeePerCheck = m.HsaEmployeePerCheck;
		s.HsaEmployerPerCheck = m.HsaEmployerPerCheck;
		s.HsaPreTaxFor = m.HsaPreTaxFor;
		s.HealthFsaAnnualElection = m.HealthFsaAnnualElection;
		s.HealthFsaPerCheckOverride = m.HealthFsaPerCheckOverride;
		s.HealthFsaPreTaxFor = m.HealthFsaPreTaxFor;
		s.StipendPerCheck = m.StipendPerCheck;
		s.StipendIsTaxable = m.StipendIsTaxable;
		s.W4FilingStatus = m.W4FilingStatus;
		s.W4MultipleJobs = m.W4MultipleJobs;
		s.W4Credits = m.W4Credits;
		s.W4OtherIncome = m.W4OtherIncome;
		s.W4Deductions = m.W4Deductions;
		s.W4ExtraWithholding = m.W4ExtraWithholding;
		s.StateWithholdingExemptions = m.StateWithholdingExemptions;
		s.StateAdditionalWithholding = m.StateAdditionalWithholding;
		s.NetPayAdjustmentPerCheck = m.NetPayAdjustmentPerCheck;
		s.ActualNetPay = m.ActualNetPay;
		s.ActualNetPayDate = m.ActualNetPayDate;

		s.Deductions.Clear();
		s.Deductions.AddRange(m.Deductions.Select((d, i) => new PayrollDeduction
		{
			Type = d.Type,
			Label = d.Label.Trim(),
			AnnualAmount = d.AnnualAmount,
			PerCheckOverride = d.PerCheckOverride,
			PreTaxFor = d.PreTaxFor,
			SortOrder = i,
		}));
	}

	public static PayrollSourceInput ToEngineInput(PayrollSourceModel m) => new()
	{
		PayBasis = m.PayBasis,
		AnnualSalary = m.AnnualSalary ?? 0m,
		HourlyRate = m.HourlyRate ?? 0m,
		HoursPerCheck = m.HoursPerCheck ?? 0m,
		Schedule = new PayScheduleSettings(m.PayFrequency, m.SemimonthlyPayDay1, m.SemimonthlyPayDay2, m.BiweeklyAnchorDate),
		Traditional401kPercent = m.Traditional401kPercent,
		Traditional401kPerCheckOverride = m.Traditional401kPerCheckOverride,
		Traditional401kPreTaxFor = m.Traditional401kPreTaxFor,
		EmployerNonElectivePercent = m.EmployerNonElectivePercent,
		EmployerMatchPercent = m.EmployerMatchPercent,
		EmployerMatchCapPercent = m.EmployerMatchCapPercent,
		HsaEmployeePerCheck = m.HsaEmployeePerCheck,
		HsaEmployerPerCheck = m.HsaEmployerPerCheck,
		HsaPreTaxFor = m.HsaPreTaxFor,
		HealthFsaAnnualElection = m.HealthFsaAnnualElection,
		HealthFsaPerCheckOverride = m.HealthFsaPerCheckOverride,
		HealthFsaPreTaxFor = m.HealthFsaPreTaxFor,
		Deductions = m.Deductions
			.Select(d => new DeductionInput(d.Type, d.Label, d.AnnualAmount, d.PreTaxFor, d.PerCheckOverride))
			.ToList(),
		StipendPerCheck = m.StipendPerCheck,
		StipendIsTaxable = m.StipendIsTaxable,
		W4 = new FederalW4(m.W4FilingStatus, m.W4MultipleJobs, m.W4Credits, m.W4OtherIncome, m.W4Deductions, m.W4ExtraWithholding),
		StateElections = new StateWithholdingElections(m.StateWithholdingExemptions, m.StateAdditionalWithholding),
		NetPayAdjustmentPerCheck = m.NetPayAdjustmentPerCheck,
		ActualPaycheck = m.ActualNetPay is { } net && m.ActualNetPayDate is { } date ? new ActualPaycheck(date, net) : null,
	};
}
