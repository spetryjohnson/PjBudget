using FluentValidation;
using Microsoft.EntityFrameworkCore;
using PjBudget.Features.Payroll.Engine.States;
using PjBudget.Shared.Database;
using PjBudget.Shared.Domain;
using PjBudget.Shared.Errors;

namespace PjBudget.Features.Payroll;

/// <summary>
/// Everything a payroll source must satisfy before it can be saved or simulated: field rules, plus references to
/// people and locales that only the database can confirm.
/// </summary>
public sealed class PayrollSourceValidator
{
	private static readonly PayrollSourceModelValidator FieldRules = new();

	private readonly AppDbContext _db;
	private readonly StateTaxModules _states;

	public PayrollSourceValidator(AppDbContext db, StateTaxModules states)
	{
		_db = db;
		_states = states;
	}

	public async Task ValidateAsync(PayrollSourceModel model, CancellationToken ct)
	{
		await FieldRules.ValidateOrThrowAsync(model, ct);

		var errors = new Dictionary<string, string[]>();

		if (!await _db.People.AnyAsync(p => p.Id == model.PersonId, ct))
		{
			errors["personId"] = ["Choose who earns this income."];
		}

		var stateCode = await _db.Locales.Where(l => l.Id == model.WorkLocaleId).Select(l => l.StateCode).SingleOrDefaultAsync(ct);
		if (stateCode is null)
		{
			errors["workLocaleId"] = ["Choose where the work is done."];
		}
		else if (!_states.IsSupported(stateCode))
		{
			errors["workLocaleId"] = [$"Taxes for {stateCode} aren't supported yet."];
		}

		if (errors.Count > 0)
		{
			throw new DomainValidationException(errors);
		}
	}
}

public sealed class PayrollSourceModelValidator : AbstractValidator<PayrollSourceModel>
{
	public PayrollSourceModelValidator()
	{
		RuleFor(x => x.Name).NotEmpty().MaximumLength(100);
		RuleFor(x => x.EmployerName).MaximumLength(100);

		RuleFor(x => x.AnnualSalary).Must(v => v > 0).When(x => x.PayBasis == PayBasis.Salary)
			.WithMessage("Enter the annual salary.");
		RuleFor(x => x.HourlyRate).Must(v => v > 0).When(x => x.PayBasis == PayBasis.Hourly)
			.WithMessage("Enter the hourly rate.");
		RuleFor(x => x.HoursPerCheck).Must(v => v is > 0 and <= 400).When(x => x.PayBasis == PayBasis.Hourly)
			.WithMessage("Enter the hours paid on a typical check.");

		When(x => x.PayFrequency == PayFrequency.Semimonthly, () =>
		{
			RuleFor(x => x.SemimonthlyPayDay1).Must(d => d is >= 1 and <= 31)
				.WithMessage("Enter a day of the month from 1 to 31.");
			RuleFor(x => x.SemimonthlyPayDay2).Must(d => d is >= 1 and <= 31)
				.WithMessage("Enter a day of the month from 1 to 31.")
				.GreaterThan(x => x.SemimonthlyPayDay1).WithMessage("The second payday must come after the first.")
				.Must((x, day2) => PaydaysAreAWeekApart(x.SemimonthlyPayDay1, day2))
				.WithMessage("Paydays need to be at least a week apart, including from the second payday to the next month's first.");
		});
		RuleFor(x => x.BiweeklyAnchorDate).NotNull().When(x => x.PayFrequency == PayFrequency.Biweekly)
			.WithMessage("Enter the date of any payday so the schedule can be counted from it.");

		RuleFor(x => x.Traditional401kPercent).InclusiveBetween(0m, 100m);
		RuleFor(x => x.EmployerNonElectivePercent).InclusiveBetween(0m, 100m);
		RuleFor(x => x.EmployerMatchPercent).InclusiveBetween(0m, 100m);
		RuleFor(x => x.EmployerMatchCapPercent).InclusiveBetween(0m, 100m);

		RuleFor(x => x.Traditional401kPerCheckOverride).GreaterThanOrEqualTo(0).When(x => x.Traditional401kPerCheckOverride is not null);
		RuleFor(x => x.HealthFsaPerCheckOverride).GreaterThanOrEqualTo(0).When(x => x.HealthFsaPerCheckOverride is not null);
		RuleFor(x => x.HsaEmployeePerCheck).GreaterThanOrEqualTo(0);
		RuleFor(x => x.HsaEmployerPerCheck).GreaterThanOrEqualTo(0);
		RuleFor(x => x.HealthFsaAnnualElection).GreaterThanOrEqualTo(0);
		RuleFor(x => x.StipendPerCheck).GreaterThanOrEqualTo(0);

		RuleFor(x => x.W4Credits).GreaterThanOrEqualTo(0);
		RuleFor(x => x.W4OtherIncome).GreaterThanOrEqualTo(0);
		RuleFor(x => x.W4Deductions).GreaterThanOrEqualTo(0);
		RuleFor(x => x.W4ExtraWithholding).GreaterThanOrEqualTo(0);
		RuleFor(x => x.StateWithholdingExemptions).InclusiveBetween(0, 99);
		RuleFor(x => x.StateAdditionalWithholding).GreaterThanOrEqualTo(0);

		RuleFor(x => x.NetPayAdjustmentPerCheck).InclusiveBetween(-10_000m, 10_000m);
		RuleFor(x => x.ActualNetPayDate).NotNull().When(x => x.ActualNetPay is not null)
			.WithMessage("Enter the date of the paycheck you're comparing against.");
		RuleFor(x => x.ActualNetPay).NotNull().When(x => x.ActualNetPayDate is not null)
			.WithMessage("Enter the net pay from that paycheck.");

		RuleFor(x => x.Deductions).Must(d => d.Count <= 20).WithMessage("A payroll source can have at most 20 deductions.");
		RuleForEach(x => x.Deductions).ChildRules(d =>
		{
			d.RuleFor(x => x.Label).NotEmpty().MaximumLength(100);
			d.RuleFor(x => x.AnnualAmount).GreaterThanOrEqualTo(0);
			d.RuleFor(x => x.PerCheckOverride).GreaterThanOrEqualTo(0).When(x => x.PerCheckOverride is not null);
		});
	}

	/// <summary>
	/// Weekend and holiday shifts move a payday back by up to four days. Paydays closer together than that could land
	/// on the same date and silently merge into one check. The gap across the month boundary is measured in February,
	/// the shortest month.
	/// </summary>
	private static bool PaydaysAreAWeekApart(int? day1, int? day2)
		=> day1 is not { } first || day2 is not { } second
		   || (second - first >= 7 && first + 28 - Math.Min(second, 28) >= 7);
}
