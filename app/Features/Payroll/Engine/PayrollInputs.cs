using PjBudget.Shared.Domain;

namespace PjBudget.Features.Payroll.Engine;

/// <summary>
/// Everything needed to simulate one payroll source for one calendar year.
/// </summary>
public sealed record PayrollSimulationRequest(
	int Year,
	PayrollSourceInput Source,
	DateOnly? PersonBirthDate,
	HsaCoverage HsaCoverage,
	WorkLocation Work,
	HomeLocation? Home,
	TaxYearParameters TaxYear);

public sealed record PayScheduleSettings(
	PayFrequency Frequency,
	int? SemimonthlyPayDay1 = null,
	int? SemimonthlyPayDay2 = null,
	DateOnly? BiweeklyAnchorDate = null);

/// <summary>
/// The engine's view of a payroll source. <c>*Percent</c> values are 0–100; amounts are dollars.
/// </summary>
public sealed record PayrollSourceInput
{
	public PayBasis PayBasis { get; init; } = PayBasis.Salary;
	public decimal AnnualSalary { get; init; }
	public decimal HourlyRate { get; init; }
	public decimal HoursPerCheck { get; init; }
	public required PayScheduleSettings Schedule { get; init; }

	public decimal Traditional401kPercent { get; init; }
	public decimal? Traditional401kPerCheckOverride { get; init; }
	public TaxableWageTypes Traditional401kPreTaxFor { get; init; } = DefaultTaxTreatment.Traditional401k;
	public decimal EmployerNonElectivePercent { get; init; }
	public decimal EmployerMatchPercent { get; init; }
	public decimal EmployerMatchCapPercent { get; init; }

	public decimal HsaEmployeePerCheck { get; init; }
	public decimal HsaEmployerPerCheck { get; init; }
	public TaxableWageTypes HsaPreTaxFor { get; init; } = DefaultTaxTreatment.CafeteriaPlan;

	public decimal HealthFsaAnnualElection { get; init; }
	public decimal? HealthFsaPerCheckOverride { get; init; }
	public TaxableWageTypes HealthFsaPreTaxFor { get; init; } = DefaultTaxTreatment.CafeteriaPlan;

	public IReadOnlyList<DeductionInput> Deductions { get; init; } = [];

	public decimal StipendPerCheck { get; init; }
	public bool StipendIsTaxable { get; init; }

	/// <summary>
	/// The taxable value of employer-paid group-term life insurance over $50,000 (the "GTL" line on a paystub). It
	/// isn't paid out, but it's added to the wages for each tax in <see cref="GroupTermLifeTaxedFor"/>.
	/// </summary>
	public decimal GroupTermLifePerCheck { get; init; }
	public TaxableWageTypes GroupTermLifeTaxedFor { get; init; } = DefaultTaxTreatment.GroupTermLife;

	public FederalW4 W4 { get; init; } = new(FilingStatus.Single);
	public StateWithholdingElections StateElections { get; init; } = new(0, 0m);

	/// <summary>
	/// Whether the employer withholds the home school district's income tax. When it doesn't, the tax is still
	/// calculated so it can be planned for, but it doesn't come out of net pay.
	/// </summary>
	public bool WithholdsSchoolDistrictTax { get; init; } = true;

	public decimal NetPayAdjustmentPerCheck { get; init; }
	public ActualPaycheck? ActualPaycheck { get; init; }
}

public sealed record DeductionInput(
	DeductionType Type,
	string Label,
	decimal AnnualAmount,
	TaxableWageTypes PreTaxFor,
	decimal? PerCheckOverride = null);

/// <param name="MultipleJobs">Step 2(c): the "two jobs" checkbox, which selects the higher-withholding tables.</param>
/// <param name="Credits">Step 3: annual credits, such as for dependents.</param>
/// <param name="OtherIncome">Step 4(a): annual income from outside this job.</param>
/// <param name="Deductions">Step 4(b): annual deductions beyond the standard deduction.</param>
/// <param name="ExtraWithholding">Step 4(c): an extra amount withheld from every check.</param>
public sealed record FederalW4(
	FilingStatus FilingStatus,
	bool MultipleJobs = false,
	decimal Credits = 0m,
	decimal OtherIncome = 0m,
	decimal Deductions = 0m,
	decimal ExtraWithholding = 0m);

/// <summary>The state's equivalent of a W-4. For Ohio, this is the IT 4 exemption count and extra withholding.</summary>
public sealed record StateWithholdingElections(int Exemptions, decimal AdditionalWithholding);

/// <summary>A real paycheck the simulation is checked against, for calibration.</summary>
public sealed record ActualPaycheck(DateOnly PayDate, decimal NetPay);

public sealed record WorkLocation(string StateCode, decimal MunicipalTaxRate);

public sealed record HomeLocation(
	string StateCode,
	decimal MunicipalTaxRate,
	decimal? SchoolDistrictTaxRate,
	SchoolDistrictTaxBase? SchoolDistrictTaxBase);

/// <summary>
/// The wage bases each kind of deduction reduces by default, and the ones taxable life insurance is added to. These
/// follow federal law and Ohio's municipal rules, and any of them can be changed per source to match how an employer
/// actually runs payroll.
/// </summary>
public static class DefaultTaxTreatment
{
	/// <summary>
	/// Group-term life insurance over $50,000 is taxable income. Employers must add it to Social Security and Medicare
	/// wages, and Ohio cities tax Medicare wages. Withholding income tax on it is optional, and employers usually
	/// skip it.
	/// </summary>
	public const TaxableWageTypes GroupTermLife = TaxableWageTypes.SocialSecurity | TaxableWageTypes.Medicare | TaxableWageTypes.City;

	/// <summary>
	/// 401(k) deferrals avoid income tax but not FICA, and Ohio cities add them back (ORC 718.01). They also stay
	/// out of school district earned income, because that base only counts income included in Ohio AGI.
	/// </summary>
	public const TaxableWageTypes Traditional401k = TaxableWageTypes.Federal | TaxableWageTypes.State | TaxableWageTypes.School;

	/// <summary>Section 125 cafeteria-plan deductions (HSA, FSA, health premiums) are pre-tax for everything.</summary>
	public const TaxableWageTypes CafeteriaPlan = TaxableWageTypes.All;

	public static TaxableWageTypes ForDeduction(DeductionType type) => type switch
	{
		DeductionType.Medical or DeductionType.Dental or DeductionType.Vision => CafeteriaPlan,
		_ => TaxableWageTypes.None,
	};
}
