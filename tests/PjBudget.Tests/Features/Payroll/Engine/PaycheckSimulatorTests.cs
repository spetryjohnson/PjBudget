using PjBudget.Features.Payroll.Engine;
using PjBudget.Shared.Domain;
using static PjBudget.Tests.TestSupport.PayrollTestData;

namespace PjBudget.Tests.Features.Payroll.Engine;

public class PaycheckSimulatorTests
{
	private readonly PaycheckSimulator _simulator = Simulator();

	/// <summary>
	/// $120k salary, semimonthly: 10% 401(k), HSA, FSA, medical (pre-tax for everything) and life (post-tax).
	/// </summary>
	private static PayrollSourceInput TypicalSource() => SemimonthlySalary(120_000m) with
	{
		Traditional401kPercent = 10m,
		HsaEmployeePerCheck = 100m,
		HealthFsaAnnualElection = 1_200m,
		Deductions =
		[
			new DeductionInput(DeductionType.Medical, "Medical", 2_400m, DefaultTaxTreatment.ForDeduction(DeductionType.Medical)),
			new DeductionInput(DeductionType.Life, "Supplemental life", 240m, DefaultTaxTreatment.ForDeduction(DeductionType.Life)),
		],
		StipendPerCheck = 25m,
	};

	[Test]
	public void CalculatesEveryLineOfACheck()
	{
		var check = _simulator.Simulate(Request(TypicalSource())).Checks[0];
		var lines = check.Current;

		Assert.Multiple(() =>
		{
			Assert.That(check.PayDate, Is.EqualTo(new DateOnly(2026, 1, 9)));
			Assert.That(lines.GrossPay, Is.EqualTo(5_000m));
			Assert.That(lines.Traditional401k, Is.EqualTo(500m));
			Assert.That(lines.HsaEmployee, Is.EqualTo(100m));
			Assert.That(lines.HealthFsa, Is.EqualTo(50m));
			Assert.That(lines.Deductions, Is.EqualTo(new[] { 100m, 10m }));

			// 401(k) reduces income tax wages but not FICA or city wages.
			Assert.That(lines.TaxableWages, Is.EqualTo(new TaxableWages(4_250m, 4_250m, 4_750m, 4_750m, 4_750m, 4_250m)));

			Assert.That(lines.FederalIncomeTax, Is.EqualTo(328.33m));
			Assert.That(lines.SocialSecurityTax, Is.EqualTo(294.50m));
			Assert.That(lines.MedicareTax, Is.EqualTo(68.88m));
			Assert.That(lines.StateIncomeTax, Is.EqualTo(112.33m));
			Assert.That(lines.CityIncomeTax, Is.EqualTo(95.00m));
			Assert.That(lines.SchoolDistrictTax, Is.EqualTo(42.50m));
			Assert.That(lines.NonTaxableStipend, Is.EqualTo(25m));

			// 5,000 − 941.54 taxes − 760 deductions + 25 stipend
			Assert.That(lines.NetPay, Is.EqualTo(3_323.46m));
		});
	}

	[Test]
	public void SummarizesASteadyYear()
	{
		var simulation = _simulator.Simulate(Request(TypicalSource()));
		var summary = simulation.Summary;

		Assert.Multiple(() =>
		{
			Assert.That(summary.CheckCount, Is.EqualTo(24));
			Assert.That(summary.AnnualTotals.NetPay, Is.EqualTo(79_763.04m));
			Assert.That(summary.RegularNetPay, Is.EqualTo(3_323.46m));
			Assert.That(summary.FinalSocialSecurityCheck, Is.EqualTo(24));
			Assert.That(summary.ChecksWithoutSocialSecurity, Is.EqualTo(0));
			Assert.That(summary.AdditionalMedicareStartsOnCheck, Is.Null);
			Assert.That(summary.Traditional401kLimitReachedOnCheck, Is.Null);
			Assert.That(summary.NetPayByMonth, Has.Count.EqualTo(12).And.All.Matches<MonthlyNetPay>(m => m.CheckCount == 2));
			Assert.That(simulation.Checks[^1].YearToDate.HealthFsa, Is.EqualTo(1_200m));
			Assert.That(simulation.Warnings, Is.Empty);
		});
	}

	[Test]
	public void FrontLoaded401kStopsAtTheLimitAndLaterChecksWithholdMore()
	{
		var source = SemimonthlySalary(240_000m) with { Traditional401kPercent = 15m };

		var simulation = _simulator.Simulate(Request(source));
		var checks = simulation.Checks;

		Assert.Multiple(() =>
		{
			Assert.That(checks[15].Current.Traditional401k, Is.EqualTo(1_500m));
			Assert.That(checks[16].Current.Traditional401k, Is.EqualTo(500m), "check 17 takes only what's left of the limit");
			Assert.That(checks[17].Current.Traditional401k, Is.EqualTo(0m));
			Assert.That(checks[^1].YearToDate.Traditional401k, Is.EqualTo(24_500m));

			Assert.That(checks[0].Current.FederalIncomeTax, Is.EqualTo(1_134.17m));
			Assert.That(checks[16].Current.FederalIncomeTax, Is.EqualTo(1_354.17m));
			Assert.That(checks[17].Current.FederalIncomeTax, Is.EqualTo(1_464.17m));

			Assert.That(simulation.Summary.Traditional401kLimitReachedOnCheck, Is.EqualTo(17));
			Assert.That(simulation.Summary.Traditional401kMaxOutPercent, Is.EqualTo(10.21m));
			Assert.That(simulation.Warnings.Select(w => w.Code), Does.Contain("401K_LIMIT_REACHED"));
		});
	}

	[Test]
	public void SocialSecurityStopsAtTheWageBaseAndAdditionalMedicareStartsAbove200k()
	{
		var simulation = _simulator.Simulate(Request(SemimonthlySalary(240_000m)));
		var checks = simulation.Checks;

		Assert.Multiple(() =>
		{
			Assert.That(checks[17].Current.SocialSecurityTax, Is.EqualTo(620.00m));
			Assert.That(checks[18].Current.SocialSecurityTax, Is.EqualTo(279.00m), "only 4,500 of wage base left");
			Assert.That(checks[19].Current.SocialSecurityTax, Is.EqualTo(0m));
			Assert.That(checks[^1].YearToDate.SocialSecurityTax, Is.EqualTo(11_439.00m));
			Assert.That(simulation.Summary.FinalSocialSecurityCheck, Is.EqualTo(19));
			Assert.That(simulation.Summary.ChecksWithoutSocialSecurity, Is.EqualTo(5));

			// Check 20 brings wages to exactly 200,000; the 0.9% applies to wages above that.
			Assert.That(checks[19].Current.AdditionalMedicareTax, Is.EqualTo(0m));
			Assert.That(checks[20].Current.AdditionalMedicareTax, Is.EqualTo(90.00m));
			Assert.That(simulation.Summary.AdditionalMedicareStartsOnCheck, Is.EqualTo(21));

			Assert.That(simulation.Summary.RegularNetPay, Is.EqualTo(checks[0].Current.NetPay));
			Assert.That(simulation.Summary.MaximumNetPay, Is.GreaterThan(simulation.Summary.RegularNetPay));
		});
	}

	[TestCase(1976, 32_500, 22)]
	[TestCase(1966, 35_750, 24)]
	[TestCase(1990, 24_500, 17)]
	public void CatchUpRaisesThe401kLimitByAge(int birthYear, decimal expectedLimit, int expectedLimitCheck)
	{
		var source = SemimonthlySalary(240_000m) with { Traditional401kPercent = 15m };

		var simulation = _simulator.Simulate(Request(source, birthDate: new DateOnly(birthYear, 7, 1)));

		Assert.Multiple(() =>
		{
			Assert.That(simulation.Summary.Traditional401kLimit, Is.EqualTo(expectedLimit));
			Assert.That(simulation.Summary.Traditional401kLimitReachedOnCheck, Is.EqualTo(expectedLimitCheck));
			Assert.That(simulation.Checks[^1].YearToDate.Traditional401k, Is.EqualTo(Math.Min(expectedLimit, 36_000m)));
		});
	}

	[Test]
	public void HourlyBiweeklyYearWith27Checks()
	{
		var source = new PayrollSourceInput
		{
			PayBasis = PayBasis.Hourly,
			HourlyRate = 50m,
			HoursPerCheck = 80m,
			Schedule = new PayScheduleSettings(PayFrequency.Biweekly, BiweeklyAnchorDate: new DateOnly(2026, 1, 2)),
			W4 = new FederalW4(FilingStatus.MarriedFilingJointly, MultipleJobs: true),
		};

		var simulation = _simulator.Simulate(Request(source));

		Assert.Multiple(() =>
		{
			Assert.That(simulation.PeriodsPerYear, Is.EqualTo(26));
			Assert.That(simulation.Checks, Has.Count.EqualTo(27));
			Assert.That(simulation.Checks.Select(c => c.Current.GrossPay).Distinct(), Is.EqualTo(new[] { 4_000m }));
			Assert.That(simulation.Summary.AnnualTotals.GrossPay, Is.EqualTo(108_000m));
			Assert.That(simulation.Summary.NetPayByMonth.Where(m => m.CheckCount == 3).Select(m => m.Month), Is.EqualTo(new[] { 1, 7, 12 }));
			Assert.That(simulation.Warnings.Select(w => w.Code), Is.EqualTo(new[] { "EXTRA_PAYCHECK" }));
		});
	}

	[Test]
	public void OverridesTaxableStipendAndNetAdjustment()
	{
		var baseline = _simulator.Simulate(Request(TypicalSource())).Checks[0].Current;
		var source = TypicalSource() with
		{
			HealthFsaPerCheckOverride = 48m,
			Deductions = [TypicalSource().Deductions[0] with { PerCheckOverride = 101m }, TypicalSource().Deductions[1]],
			StipendIsTaxable = true,
			NetPayAdjustmentPerCheck = 4.01m,
		};

		var lines = _simulator.Simulate(Request(source)).Checks[0].Current;

		Assert.Multiple(() =>
		{
			Assert.That(lines.HealthFsa, Is.EqualTo(48m));
			Assert.That(lines.Deductions[0], Is.EqualTo(101m));
			Assert.That(lines.GrossPay, Is.EqualTo(5_025m), "a taxable stipend is part of gross pay");
			Assert.That(lines.NonTaxableStipend, Is.EqualTo(0m));
			Assert.That(lines.Traditional401k, Is.EqualTo(500m), "401(k) is a % of base pay, not the stipend");
			Assert.That(lines.TaxableWages.Federal, Is.EqualTo(baseline.TaxableWages.Federal + 25m + 2m - 1m));
			Assert.That(lines.NetPay, Is.EqualTo(lines.GrossPay - lines.TotalTaxes - lines.TotalDeductions + 4.01m));
		});
	}

	[Test]
	public void ComparesAnActualPaycheckWithTheSimulatedCheckOnThatDate()
	{
		var source = TypicalSource() with { ActualPaycheck = new ActualPaycheck(new DateOnly(2026, 3, 11), 3_330m) };

		var comparison = _simulator.Simulate(Request(source)).Summary.ActualComparison;

		Assert.That(comparison, Is.EqualTo(new ActualPaycheckComparison(new DateOnly(2026, 3, 10), 5, 3_330m, 3_323.46m, 6.54m)));
	}

	[Test]
	public void APaycheckFromAnotherYearIsNotCompared()
	{
		var source = TypicalSource() with { ActualPaycheck = new ActualPaycheck(new DateOnly(2027, 1, 8), 3_330m) };

		Assert.That(_simulator.Simulate(Request(source)).Summary.ActualComparison, Is.Null);
	}

	[Test]
	public void EmployerContributionsAreTrackedButNotPaid()
	{
		var source = TypicalSource() with
		{
			EmployerNonElectivePercent = 3m,
			EmployerMatchPercent = 50m,
			EmployerMatchCapPercent = 6m,
			HsaEmployerPerCheck = 20m,
		};

		var lines = _simulator.Simulate(Request(source)).Checks[0].Current;

		Assert.Multiple(() =>
		{
			// 3% of 5,000 + 50% of the 300 (6% of pay) of the 500 deferred
			Assert.That(lines.EmployerRetirement, Is.EqualTo(300m));
			Assert.That(lines.EmployerHsa, Is.EqualTo(20m));
			Assert.That(lines.NetPay, Is.EqualTo(3_323.46m));
		});
	}

	[Test]
	public void EmployerHsaRunsAllYearAndTheEmployeeGetsWhatsLeftOfTheLimit()
	{
		var source = TypicalSource() with { HsaEmployeePerCheck = 350m, HsaEmployerPerCheck = 50m };

		var simulation = _simulator.Simulate(Request(source, hsaCoverage: HsaCoverage.Family));
		var checks = simulation.Checks;

		Assert.Multiple(() =>
		{
			// The employer's 24 × 50 = 1,200 is set aside first, leaving 7,550 of the 8,750 family limit:
			// 21 checks of 350, then 200 on check 22, then nothing.
			Assert.That(checks[21].Current.HsaEmployee, Is.EqualTo(200m));
			Assert.That(checks[22].Current.HsaEmployee, Is.EqualTo(0m));
			Assert.That(checks.Select(c => c.Current.EmployerHsa).Distinct(), Is.EqualTo(new[] { 50m }));
			Assert.That(checks[^1].YearToDate.HsaEmployee + checks[^1].YearToDate.EmployerHsa, Is.EqualTo(8_750m));
			Assert.That(simulation.Warnings.Select(w => w.Code), Does.Contain("HSA_LIMIT_REACHED"));
		});
	}

	[Test]
	public void EmployerHsaAloneCanUseUpMostOfASelfOnlyLimit()
	{
		var source = TypicalSource() with { HsaEmployeePerCheck = 200m, HsaEmployerPerCheck = 100m };

		var annual = _simulator.Simulate(Request(source, hsaCoverage: HsaCoverage.SelfOnly)).Summary.AnnualTotals;

		Assert.Multiple(() =>
		{
			Assert.That(annual.EmployerHsa, Is.EqualTo(2_400m));
			Assert.That(annual.HsaEmployee, Is.EqualTo(2_000m), "4,400 limit − 2,400 from the employer");
		});
	}

	[Test]
	public void HsaContributionsNeedCoverage()
	{
		var simulation = _simulator.Simulate(Request(TypicalSource(), hsaCoverage: HsaCoverage.None));

		Assert.Multiple(() =>
		{
			Assert.That(simulation.Summary.AnnualTotals.HsaEmployee, Is.EqualTo(0m));
			Assert.That(simulation.Warnings.Select(w => w.Code), Does.Contain("HSA_NO_COVERAGE"));
		});
	}

	[Test]
	public void FsaElectionAboveTheLimitIsCapped()
	{
		var simulation = _simulator.Simulate(Request(TypicalSource() with { HealthFsaAnnualElection = 4_000m }));

		Assert.Multiple(() =>
		{
			Assert.That(simulation.Checks[0].Current.HealthFsa, Is.EqualTo(166.67m));
			Assert.That(simulation.Checks[20].Current.HealthFsa, Is.EqualTo(66.60m), "3,400 − 20 × 166.67");
			Assert.That(simulation.Summary.AnnualTotals.HealthFsa, Is.EqualTo(3_400m));
			Assert.That(simulation.Warnings.Select(w => w.Code), Does.Contain("FSA_OVER_LIMIT"));
		});
	}
}
