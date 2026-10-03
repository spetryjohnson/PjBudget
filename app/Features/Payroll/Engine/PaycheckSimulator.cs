using System.Globalization;
using PjBudget.Features.Payroll.Engine.States;
using PjBudget.Shared.Domain;

namespace PjBudget.Features.Payroll.Engine;

/// <summary>
/// Simulates a payroll source check by check through a calendar year, the way a payroll provider runs it. Each check
/// is calculated in pay-date order, and year-to-date totals drive the annual caps: the 401(k), HSA and FSA limits,
/// the Social Security wage base, and the Additional Medicare threshold.
/// </summary>
public sealed class PaycheckSimulator
{
	private static readonly CultureInfo UsCulture = CultureInfo.GetCultureInfo("en-US");

	private readonly PayScheduleGenerator _schedule;
	private readonly StateTaxModules _states;

	public PaycheckSimulator(PayScheduleGenerator schedule, StateTaxModules states)
	{
		_schedule = schedule;
		_states = states;
	}

	public PayrollSimulation Simulate(PayrollSimulationRequest request)
	{
		var source = request.Source;
		var periods = PayScheduleGenerator.PeriodsPerYear(source.Schedule.Frequency);
		var ageAtYearEnd = request.PersonBirthDate is { } birthDate ? request.Year - birthDate.Year : (int?)null;

		var payDates = _schedule.PayDates(source.Schedule, request.Year);

		// Employers commit to their HSA contributions for the whole year, so payroll only lets the employee elect
		// what's left of the limit. The employee's contributions are the ones that stop early, not the employer's.
		var hsaLimit = request.TaxYear.Limits.HsaLimitFor(request.HsaCoverage, ageAtYearEnd);
		var employerHsa = Math.Min(source.HsaEmployerPerCheck * payDates.Count, hsaLimit);

		var limits = new AnnualLimits(
			request.TaxYear.Limits.ElectiveDeferralLimitFor(ageAtYearEnd),
			hsaLimit,
			employerHsa,
			hsaLimit - employerHsa,
			Math.Min(source.HealthFsaAnnualElection, request.TaxYear.Limits.HealthFsa));

		var calculator = new CheckCalculator(request, periods, limits, _states.For(request.Work.StateCode));
		var checks = new List<Paycheck>();
		var yearToDate = PaycheckLines.Zero(source.Deductions.Count);

		foreach (var payDate in payDates)
		{
			var current = calculator.Calculate(yearToDate);
			yearToDate = yearToDate.Plus(current);
			checks.Add(new Paycheck(checks.Count + 1, payDate, current, yearToDate));
		}

		var summary = Summarize(request.Year, source, checks, limits);

		return new PayrollSimulation(
			request.Year,
			periods,
			source.Deductions.Select(d => new DeductionHeader(d.Type, d.Label)).ToList(),
			checks,
			summary,
			Warnings(request, checks, summary, limits, periods));
	}

	/// <param name="Hsa">The HSA limit for the household's coverage, including catch-up.</param>
	/// <param name="EmployerHsa">What the employer will contribute this year, up to the limit.</param>
	/// <param name="EmployeeHsa">What's left of the limit for the employee's own contributions.</param>
	private readonly record struct AnnualLimits(
		decimal Traditional401k, decimal Hsa, decimal EmployerHsa, decimal EmployeeHsa, decimal HealthFsa);

	private sealed class CheckCalculator
	{
		private readonly PayrollSourceInput _source;
		private readonly int _periods;
		private readonly AnnualLimits _limits;
		private readonly decimal _basePay;
		private readonly FederalIncomeTaxWithholding _federal;
		private readonly FicaParameters _fica;
		private readonly IStateTaxModule _state;
		private readonly StateWithholdingContext _stateContext;

		public CheckCalculator(PayrollSimulationRequest request, int periods, AnnualLimits limits, IStateTaxModule state)
		{
			_source = request.Source;
			_periods = periods;
			_limits = limits;
			_basePay = _source.PayBasis == PayBasis.Hourly
				? Money.Round(_source.HourlyRate * _source.HoursPerCheck)
				: Money.Round(_source.AnnualSalary / periods);
			_federal = new FederalIncomeTaxWithholding(
				_source.W4, request.TaxYear.FederalRulesFor(_source.W4.FilingStatus), periods);
			_fica = request.TaxYear.Fica;
			_state = state;
			_stateContext = new StateWithholdingContext(periods, _source.StateElections, request.Work, request.Home, request.TaxYear);
		}

		public PaycheckLines Calculate(PaycheckLines ytd)
		{
			var taxableStipend = _source.StipendIsTaxable ? _source.StipendPerCheck : 0m;
			var gross = _basePay + taxableStipend;

			var deferral = Capped(
				_source.Traditional401kPerCheckOverride ?? PercentOf(_basePay, _source.Traditional401kPercent),
				_limits.Traditional401k - ytd.Traditional401k);

			var employerHsa = Capped(_source.HsaEmployerPerCheck, _limits.EmployerHsa - ytd.EmployerHsa);
			var hsa = Capped(_source.HsaEmployeePerCheck, _limits.EmployeeHsa - ytd.HsaEmployee);

			var fsa = Capped(
				_source.HealthFsaPerCheckOverride ?? Money.Round(_source.HealthFsaAnnualElection / _periods),
				_limits.HealthFsa - ytd.HealthFsa);

			var deductions = _source.Deductions
				.Select(d => d.PerCheckOverride ?? Money.Round(d.AnnualAmount / _periods))
				.ToArray();

			var preTax = new List<(decimal Amount, TaxableWageTypes PreTaxFor)>
			{
				(deferral, _source.Traditional401kPreTaxFor),
				(hsa, _source.HsaPreTaxFor),
				(fsa, _source.HealthFsaPreTaxFor),
			};
			preTax.AddRange(deductions.Select((amount, i) => (amount, _source.Deductions[i].PreTaxFor)));
			var groupTermLife = _source.GroupTermLifePerCheck;
			var wages = TaxableWagesAfter(gross, preTax, groupTermLife, _source.GroupTermLifeTaxedFor);

			var fica = FicaCalculator.Calculate(wages.SocialSecurity, wages.Medicare,
				new FicaYearToDate(ytd.SocialSecurityTaxedWages, ytd.SocialSecurityTax, ytd.TaxableWages.Medicare,
					ytd.MedicareTax, ytd.AdditionalMedicareTax),
				_fica);
			var stateAndLocal = _state.CalculateWithholding(_stateContext, wages);
			var schoolDistrictTax = _source.WithholdsSchoolDistrictTax ? stateAndLocal.SchoolDistrict : 0m;

			var lines = new PaycheckLines
			{
				BasePay = _basePay,
				TaxableStipend = taxableStipend,
				GrossPay = gross,
				Traditional401k = deferral,
				HsaEmployee = hsa,
				HealthFsa = fsa,
				Deductions = deductions,
				TaxableWages = wages,
				SocialSecurityTaxedWages = fica.SocialSecurityTaxedWages,
				FederalIncomeTax = _federal.Calculate(wages.Federal),
				SocialSecurityTax = fica.SocialSecurity,
				MedicareTax = fica.Medicare,
				AdditionalMedicareTax = fica.AdditionalMedicare,
				StateIncomeTax = stateAndLocal.State,
				CityIncomeTax = stateAndLocal.City,
				SchoolDistrictTax = schoolDistrictTax,
				SchoolDistrictTaxNotWithheld = stateAndLocal.SchoolDistrict - schoolDistrictTax,
				NonTaxableStipend = _source.StipendIsTaxable ? 0m : _source.StipendPerCheck,
				NetPayAdjustment = _source.NetPayAdjustmentPerCheck,
				GroupTermLife = groupTermLife,
				EmployerRetirement = EmployerRetirement(deferral),
				EmployerHsa = employerHsa,
			};

			return lines with
			{
				NetPay = lines.GrossPay - lines.TotalTaxes - lines.TotalDeductions + lines.NonTaxableStipend + lines.NetPayAdjustment,
			};
		}

		private decimal EmployerRetirement(decimal deferral)
		{
			var nonElective = PercentOf(_basePay, _source.EmployerNonElectivePercent);
			var matchedDeferral = Math.Min(deferral, PercentOf(_basePay, _source.EmployerMatchCapPercent));
			return nonElective + Money.Round(matchedDeferral * _source.EmployerMatchPercent / 100m);
		}

		private static decimal PercentOf(decimal amount, decimal percent) => Money.Round(amount * percent / 100m);

		private static decimal Capped(decimal amount, decimal remaining) => Math.Max(0m, Math.Min(amount, remaining));

		/// <param name="groupTermLife">Taxable life insurance. It isn't paid, but it's added to the wages it's taxed for.</param>
		private static TaxableWages TaxableWagesAfter(
			decimal gross,
			IReadOnlyList<(decimal Amount, TaxableWageTypes PreTaxFor)> deductions,
			decimal groupTermLife,
			TaxableWageTypes groupTermLifeTaxedFor)
		{
			decimal WagesFor(TaxableWageTypes type)
				=> Math.Max(0m, gross - deductions.Where(d => d.PreTaxFor.HasFlag(type)).Sum(d => d.Amount))
				   + (groupTermLifeTaxedFor.HasFlag(type) ? groupTermLife : 0m);

			return new TaxableWages(
				WagesFor(TaxableWageTypes.Federal),
				WagesFor(TaxableWageTypes.State),
				WagesFor(TaxableWageTypes.SocialSecurity),
				WagesFor(TaxableWageTypes.Medicare),
				WagesFor(TaxableWageTypes.City),
				WagesFor(TaxableWageTypes.School));
		}
	}

	private static PayrollSummary Summarize(int year, PayrollSourceInput source, IReadOnlyList<Paycheck> checks, AnnualLimits limits)
	{
		var annual = checks.Count > 0 ? checks[^1].YearToDate : PaycheckLines.Zero(source.Deductions.Count);
		var netPays = checks.Select(c => c.Current.NetPay).ToList();
		var finalSocialSecurityCheck = checks.LastOrDefault(c => c.Current.SocialSecurityTax > 0)?.Number ?? 0;
		var actual = source.ActualPaycheck;
		// A paycheck from another year can't be compared with this year's simulation.
		var actualCheck = actual is null || actual.PayDate.Year != year
			? null
			: checks.LastOrDefault(c => c.PayDate <= actual.PayDate);

		return new PayrollSummary
		{
			AnnualTotals = annual,
			CheckCount = checks.Count,
			FinalSocialSecurityCheck = finalSocialSecurityCheck,
			ChecksWithoutSocialSecurity = checks.Count - finalSocialSecurityCheck,
			AdditionalMedicareStartsOnCheck = checks.FirstOrDefault(c => c.Current.AdditionalMedicareTax > 0)?.Number,
			Traditional401kLimit = limits.Traditional401k,
			Traditional401kLimitReachedOnCheck = limits.Traditional401k > 0
				? checks.FirstOrDefault(c => c.YearToDate.Traditional401k >= limits.Traditional401k)?.Number
				: null,
			Traditional401kMaxOutPercent = annual.BasePay > 0
				? Math.Ceiling(limits.Traditional401k / annual.BasePay * 10_000m) / 100m
				: 0m,
			HsaLimit = limits.Hsa,
			RegularNetPay = RegularNetPay(netPays),
			MinimumNetPay = netPays.DefaultIfEmpty().Min(),
			MaximumNetPay = netPays.DefaultIfEmpty().Max(),
			AverageNetPay = netPays.Count > 0 ? Money.Round(netPays.Average()) : 0m,
			NetPayByMonth = checks
				.GroupBy(c => c.PayDate.Month)
				.Select(g => new MonthlyNetPay(g.Key, g.Count(), g.Sum(c => c.Current.NetPay)))
				.ToList(),
			ActualComparison = actual is null || actualCheck is null
				? null
				: new ActualPaycheckComparison(actualCheck.PayDate, actualCheck.Number, actual.NetPay,
					actualCheck.Current.NetPay, actual.NetPay - actualCheck.Current.NetPay),
		};
	}

	/// <summary>
	/// The most common net pay, so checks that differ (for example, after the Social Security wage base) stand out.
	/// If every check differs, as hourly pay can, this falls back to the median.
	/// </summary>
	private static decimal RegularNetPay(IReadOnlyList<decimal> netPays)
	{
		if (netPays.Count == 0)
		{
			return 0m;
		}

		var mostCommon = netPays.GroupBy(n => n).MaxBy(g => g.Count())!;
		if (mostCommon.Count() > 1 || netPays.Count == 1)
		{
			return mostCommon.Key;
		}

		var sorted = netPays.Order().ToList();
		var middle = sorted.Count / 2;
		return sorted.Count % 2 == 1 ? sorted[middle] : Money.Round((sorted[middle - 1] + sorted[middle]) / 2);
	}

	private static IReadOnlyList<PayrollWarning> Warnings(
		PayrollSimulationRequest request, IReadOnlyList<Paycheck> checks, PayrollSummary summary, AnnualLimits limits, int periods)
	{
		var source = request.Source;
		var warnings = new List<PayrollWarning>();

		if (summary.Traditional401kLimitReachedOnCheck is { } limitCheck && limitCheck < checks.Count)
		{
			warnings.Add(new PayrollWarning("401K_LIMIT_REACHED", string.Create(UsCulture,
				$"401(k) contributions reach the {limits.Traditional401k:C0} limit on check {limitCheck} of {checks.Count}, so later checks have no 401(k) deduction and more tax withheld. Contributing {summary.Traditional401kMaxOutPercent:0.##}% of pay would reach the limit on the final check instead.")));
		}

		var hsaPerCheck = source.HsaEmployeePerCheck + source.HsaEmployerPerCheck;
		if (hsaPerCheck > 0 && request.HsaCoverage == HsaCoverage.None)
		{
			warnings.Add(new PayrollWarning("HSA_NO_COVERAGE",
				"HSA contributions are ignored because the household's HSA coverage is set to none."));
		}
		else if (source.HsaEmployeePerCheck > 0
		         && checks.FirstOrDefault(c => c.YearToDate.HsaEmployee >= limits.EmployeeHsa) is { } hsaCheck
		         && hsaCheck.Number < checks.Count)
		{
			warnings.Add(new PayrollWarning("HSA_LIMIT_REACHED", string.Create(UsCulture,
				$"Your HSA contributions reach the {limits.Hsa:C0} limit (which includes {limits.EmployerHsa:C0} from your employer) on check {hsaCheck.Number} of {checks.Count}; later checks have no HSA deduction.")));
		}

		if (source.HealthFsaAnnualElection > request.TaxYear.Limits.HealthFsa)
		{
			warnings.Add(new PayrollWarning("FSA_OVER_LIMIT", string.Create(UsCulture,
				$"The health FSA election of {source.HealthFsaAnnualElection:C0} is over the {request.Year} limit of {request.TaxYear.Limits.HealthFsa:C0}, so contributions stop at the limit.")));
		}

		if (checks.Count > periods)
		{
			warnings.Add(new PayrollWarning("EXTRA_PAYCHECK", string.Create(UsCulture,
				$"{request.Year} has {checks.Count} paydays. Withholding is still annualized over {periods} pay periods, and deductions come out of every check.")));
		}

		if (checks.FirstOrDefault(c => c.Current.NetPay < 0) is { } negative)
		{
			warnings.Add(new PayrollWarning("NEGATIVE_NET_PAY", string.Create(UsCulture,
				$"Taxes and deductions are more than the pay on check {negative.Number}.")));
		}

		return warnings;
	}
}
