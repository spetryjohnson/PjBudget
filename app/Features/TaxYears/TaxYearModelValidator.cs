using FluentValidation;
using PjBudget.Shared.Domain;

namespace PjBudget.Features.TaxYears;

public sealed class TaxYearModelValidator : AbstractValidator<TaxYearModel>
{
	private static readonly FilingStatus[] AllFilingStatuses = Enum.GetValues<FilingStatus>();

	private static readonly TaxScheduleKind[] OhioScheduleKinds =
	[
		TaxScheduleKind.OhioWithholding,
		TaxScheduleKind.OhioIncome,
		TaxScheduleKind.OhioExemption,
		TaxScheduleKind.OhioJointFilingCredit,
	];

	public TaxYearModelValidator()
	{
		RuleFor(x => x.Year).InclusiveBetween(2000, 2100);

		RuleFor(x => x.Fica.SocialSecurityRate).Must(BeARate).WithMessage(RateMessage);
		RuleFor(x => x.Fica.MedicareRate).Must(BeARate).WithMessage(RateMessage);
		RuleFor(x => x.Fica.AdditionalMedicareRate).Must(BeARate).WithMessage(RateMessage);
		RuleFor(x => x.Fica.SocialSecurityWageBase).GreaterThan(0);
		RuleFor(x => x.Fica.AdditionalMedicareWithholdingThreshold).GreaterThan(0);

		RuleFor(x => x.Limits.ElectiveDeferral).GreaterThan(0);
		RuleFor(x => x.Limits.CatchUpAge50).GreaterThanOrEqualTo(0);
		RuleFor(x => x.Limits.CatchUpAge60To63).GreaterThanOrEqualTo(0);
		RuleFor(x => x.Limits.HsaSelfOnly).GreaterThanOrEqualTo(0);
		RuleFor(x => x.Limits.HsaFamily).GreaterThanOrEqualTo(0);
		RuleFor(x => x.Limits.HsaCatchUpAge55).GreaterThanOrEqualTo(0);
		RuleFor(x => x.Limits.HealthFsa).GreaterThanOrEqualTo(0);

		RuleFor(x => x.Ohio.WithholdingExemptionAmount).GreaterThanOrEqualTo(0);
		RuleFor(x => x.Ohio.ExemptionMagiLimit).GreaterThan(0);
		RuleFor(x => x.Ohio.JointFilingCreditCap).GreaterThanOrEqualTo(0);
		RuleFor(x => x.Ohio.JointFilingCreditMagiLimit).GreaterThan(0);
		RuleFor(x => x.Ohio.JointFilingCreditMinSpouseIncome).GreaterThanOrEqualTo(0);

		RuleFor(x => x.FilingStatuses)
			.Must(list => AllFilingStatuses.All(s => list.Count(f => f.FilingStatus == s) == 1))
			.WithMessage("Enter exactly one set of federal parameters for each filing status.");

		RuleForEach(x => x.FilingStatuses).ChildRules(f =>
		{
			f.RuleFor(x => x.StandardDeduction).GreaterThanOrEqualTo(0);
			f.RuleFor(x => x.StandardWithholdingAdjustment).GreaterThanOrEqualTo(0)
				.LessThanOrEqualTo(x => x.StandardDeduction)
				.WithMessage("The W-4 line 1g amount can't be more than the standard deduction.");
			f.RuleFor(x => x.AdditionalMedicareLiabilityThreshold).GreaterThan(0);
		});

		RuleFor(x => x.Schedules)
			.Must(list => AllFilingStatuses.All(s => list.Count(x => x.Kind == TaxScheduleKind.FederalIncome && x.FilingStatus == s) == 1))
			.WithMessage("Enter exactly one federal income tax schedule for each filing status.")
			.Must(list => OhioScheduleKinds.All(k => list.Count(x => x.Kind == k) == 1))
			.WithMessage("Enter exactly one of each Ohio schedule.");

		RuleForEach(x => x.Schedules).ChildRules(s =>
		{
			s.RuleFor(x => x.FilingStatus)
				.NotNull().When(x => x.Kind == TaxScheduleKind.FederalIncome)
				.WithMessage("Federal brackets need a filing status.");
			s.RuleFor(x => x.FilingStatus)
				.Null().When(x => x.Kind != TaxScheduleKind.FederalIncome)
				.WithMessage("Only federal brackets have a filing status.");

			s.RuleFor(x => x.Rows).NotEmpty();
			s.RuleFor(x => x.Rows)
				.Must(rows => rows.Count == 0 || rows[0].Over == 0)
				.WithMessage("The first row must start at 0.")
				.Must(rows => rows.Zip(rows.Skip(1)).All(pair => pair.Second.Over > pair.First.Over))
				.WithMessage("Each row must start above the one before it.");

			s.RuleForEach(x => x.Rows).ChildRules(r =>
			{
				r.RuleFor(x => x.Over).GreaterThanOrEqualTo(0);
				r.RuleFor(x => x.BaseAmount).GreaterThanOrEqualTo(0);
				r.RuleFor(x => x.Rate).Must(BeARate).WithMessage(RateMessage);
			});
		});
	}

	private const string RateMessage = "Rates must be between 0% and 100%.";

	private static bool BeARate(decimal rate) => rate is >= 0 and <= 1;
}
