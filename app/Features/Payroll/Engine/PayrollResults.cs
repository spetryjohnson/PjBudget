using PjBudget.Shared.Domain;

namespace PjBudget.Features.Payroll.Engine;

public sealed record PayrollSimulation(
	int Year,
	int PeriodsPerYear,
	IReadOnlyList<DeductionHeader> Deductions,
	IReadOnlyList<Paycheck> Checks,
	PayrollSummary Summary,
	IReadOnlyList<PayrollWarning> Warnings);

/// <summary>Labels for <see cref="PaycheckLines.Deductions"/>, in the same order.</summary>
public sealed record DeductionHeader(DeductionType Type, string Label);

public sealed record Paycheck(int Number, DateOnly PayDate, PaycheckLines Current, PaycheckLines YearToDate);

public readonly record struct TaxableWages(
	decimal Federal,
	decimal State,
	decimal SocialSecurity,
	decimal Medicare,
	decimal City,
	decimal School)
{
	public static TaxableWages operator +(TaxableWages a, TaxableWages b) => new(
		a.Federal + b.Federal, a.State + b.State, a.SocialSecurity + b.SocialSecurity,
		a.Medicare + b.Medicare, a.City + b.City, a.School + b.School);
}

/// <summary>
/// Every amount on a paystub. The same shape holds one check's amounts or year-to-date totals.
/// </summary>
public sealed record PaycheckLines
{
	public decimal BasePay { get; init; }
	public decimal TaxableStipend { get; init; }
	public decimal GrossPay { get; init; }

	public decimal Traditional401k { get; init; }
	public decimal HsaEmployee { get; init; }
	public decimal HealthFsa { get; init; }
	public IReadOnlyList<decimal> Deductions { get; init; } = [];

	public TaxableWages TaxableWages { get; init; }

	/// <summary>The part of Social Security wages actually taxed, after the annual wage base is applied.</summary>
	public decimal SocialSecurityTaxedWages { get; init; }

	public decimal FederalIncomeTax { get; init; }
	public decimal SocialSecurityTax { get; init; }
	public decimal MedicareTax { get; init; }
	public decimal AdditionalMedicareTax { get; init; }
	public decimal StateIncomeTax { get; init; }
	public decimal CityIncomeTax { get; init; }
	public decimal SchoolDistrictTax { get; init; }

	public decimal NonTaxableStipend { get; init; }
	public decimal NetPayAdjustment { get; init; }
	public decimal NetPay { get; init; }

	/// <summary>Employer 401(k) contributions. Shown for retirement totals; not part of pay.</summary>
	public decimal EmployerRetirement { get; init; }

	/// <summary>Employer HSA contributions. They count toward the HSA limit but aren't part of pay.</summary>
	public decimal EmployerHsa { get; init; }

	public decimal TotalTaxes => FederalIncomeTax + SocialSecurityTax + MedicareTax + AdditionalMedicareTax
	                             + StateIncomeTax + CityIncomeTax + SchoolDistrictTax;

	public decimal TotalDeductions => Traditional401k + HsaEmployee + HealthFsa + Deductions.Sum();

	public static PaycheckLines Zero(int deductionCount) => new() { Deductions = new decimal[deductionCount] };

	public PaycheckLines Plus(PaycheckLines other) => new()
	{
		BasePay = BasePay + other.BasePay,
		TaxableStipend = TaxableStipend + other.TaxableStipend,
		GrossPay = GrossPay + other.GrossPay,
		Traditional401k = Traditional401k + other.Traditional401k,
		HsaEmployee = HsaEmployee + other.HsaEmployee,
		HealthFsa = HealthFsa + other.HealthFsa,
		Deductions = Deductions.Zip(other.Deductions, (a, b) => a + b).ToArray(),
		TaxableWages = TaxableWages + other.TaxableWages,
		SocialSecurityTaxedWages = SocialSecurityTaxedWages + other.SocialSecurityTaxedWages,
		FederalIncomeTax = FederalIncomeTax + other.FederalIncomeTax,
		SocialSecurityTax = SocialSecurityTax + other.SocialSecurityTax,
		MedicareTax = MedicareTax + other.MedicareTax,
		AdditionalMedicareTax = AdditionalMedicareTax + other.AdditionalMedicareTax,
		StateIncomeTax = StateIncomeTax + other.StateIncomeTax,
		CityIncomeTax = CityIncomeTax + other.CityIncomeTax,
		SchoolDistrictTax = SchoolDistrictTax + other.SchoolDistrictTax,
		NonTaxableStipend = NonTaxableStipend + other.NonTaxableStipend,
		NetPayAdjustment = NetPayAdjustment + other.NetPayAdjustment,
		NetPay = NetPay + other.NetPay,
		EmployerRetirement = EmployerRetirement + other.EmployerRetirement,
		EmployerHsa = EmployerHsa + other.EmployerHsa,
	};
}

public sealed record PayrollSummary
{
	public required PaycheckLines AnnualTotals { get; init; }
	public int CheckCount { get; init; }

	/// <summary>The last check with any Social Security withheld; equal to <see cref="CheckCount"/> unless the wage base is reached.</summary>
	public int FinalSocialSecurityCheck { get; init; }
	public int ChecksWithoutSocialSecurity { get; init; }

	public int? AdditionalMedicareStartsOnCheck { get; init; }

	public decimal Traditional401kLimit { get; init; }
	public int? Traditional401kLimitReachedOnCheck { get; init; }

	/// <summary>The % of base pay that reaches the 401(k) limit exactly on the final check, rounded up to 0.01%.</summary>
	public decimal Traditional401kMaxOutPercent { get; init; }

	public decimal HsaLimit { get; init; }

	/// <summary>The most common net pay. Checks differing from it are the irregular ones (e.g. after the SS wage base).</summary>
	public decimal RegularNetPay { get; init; }
	public decimal MinimumNetPay { get; init; }
	public decimal MaximumNetPay { get; init; }
	public decimal AverageNetPay { get; init; }

	public IReadOnlyList<MonthlyNetPay> NetPayByMonth { get; init; } = [];
	public ActualPaycheckComparison? ActualComparison { get; init; }
}

public sealed record MonthlyNetPay(int Month, int CheckCount, decimal NetPay);

public sealed record ActualPaycheckComparison(
	DateOnly PayDate,
	int CheckNumber,
	decimal ActualNetPay,
	decimal SimulatedNetPay,
	decimal Difference);

public sealed record PayrollWarning(string Code, string Message);
