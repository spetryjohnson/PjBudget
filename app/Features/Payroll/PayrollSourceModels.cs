using PjBudget.Features.Payroll.Engine;
using PjBudget.Shared.Domain;

namespace PjBudget.Features.Payroll;

/// <summary>
/// A payroll source as the editor sees it. It is also what the live preview simulates, so it must be complete
/// enough to run the engine even before it's saved.
/// </summary>
public sealed class PayrollSourceModel
{
	public int Id { get; set; }
	public Guid Version { get; set; }
	public int PersonId { get; set; }
	public string Name { get; set; } = string.Empty;
	public string? EmployerName { get; set; }
	public int SortOrder { get; set; }
	public int WorkLocaleId { get; set; }

	public PayBasis PayBasis { get; set; }
	public decimal? AnnualSalary { get; set; }
	public decimal? HourlyRate { get; set; }
	public decimal? HoursPerCheck { get; set; }
	public PayFrequency PayFrequency { get; set; }
	public int? SemimonthlyPayDay1 { get; set; }
	public int? SemimonthlyPayDay2 { get; set; }
	public DateOnly? BiweeklyAnchorDate { get; set; }

	public decimal Traditional401kPercent { get; set; }
	public decimal? Traditional401kPerCheckOverride { get; set; }
	public TaxableWageTypes Traditional401kPreTaxFor { get; set; }
	public decimal EmployerNonElectivePercent { get; set; }
	public decimal EmployerMatchPercent { get; set; }
	public decimal EmployerMatchCapPercent { get; set; }

	public decimal HsaEmployeePerCheck { get; set; }
	public decimal HsaEmployerPerCheck { get; set; }
	public TaxableWageTypes HsaPreTaxFor { get; set; }

	public decimal HealthFsaAnnualElection { get; set; }
	public decimal? HealthFsaPerCheckOverride { get; set; }
	public TaxableWageTypes HealthFsaPreTaxFor { get; set; }

	public decimal StipendPerCheck { get; set; }
	public bool StipendIsTaxable { get; set; }

	public decimal GroupTermLifePerCheck { get; set; }
	public TaxableWageTypes GroupTermLifeTaxedFor { get; set; } = DefaultTaxTreatment.GroupTermLife;

	public FilingStatus W4FilingStatus { get; set; }
	public bool W4MultipleJobs { get; set; }
	public decimal W4Credits { get; set; }
	public decimal W4OtherIncome { get; set; }
	public decimal W4Deductions { get; set; }
	public decimal W4ExtraWithholding { get; set; }

	public int StateWithholdingExemptions { get; set; }
	public decimal StateAdditionalWithholding { get; set; }
	public bool WithholdsSchoolDistrictTax { get; set; } = true;

	public decimal NetPayAdjustmentPerCheck { get; set; }
	public decimal? ActualNetPay { get; set; }
	public DateOnly? ActualNetPayDate { get; set; }

	public List<PayrollDeductionModel> Deductions { get; set; } = [];

	/// <summary>A new source with the defaults most jobs start from.</summary>
	public static PayrollSourceModel Template() => new()
	{
		PayBasis = PayBasis.Salary,
		PayFrequency = PayFrequency.Semimonthly,
		SemimonthlyPayDay1 = 10,
		SemimonthlyPayDay2 = 25,
		Traditional401kPreTaxFor = DefaultTaxTreatment.Traditional401k,
		HsaPreTaxFor = DefaultTaxTreatment.CafeteriaPlan,
		HealthFsaPreTaxFor = DefaultTaxTreatment.CafeteriaPlan,
		W4FilingStatus = FilingStatus.MarriedFilingJointly,
	};
}

public sealed class PayrollDeductionModel
{
	public DeductionType Type { get; set; }
	public string Label { get; set; } = string.Empty;
	public decimal AnnualAmount { get; set; }
	public decimal? PerCheckOverride { get; set; }
	public TaxableWageTypes PreTaxFor { get; set; }
}

public sealed class PayrollSourceSummaryModel
{
	public int Id { get; set; }
	public string Name { get; set; } = string.Empty;
	public string? EmployerName { get; set; }
	public int PersonId { get; set; }
	public string PersonName { get; set; } = string.Empty;
	public PayFrequency PayFrequency { get; set; }
	public int SortOrder { get; set; }

	/// <summary>Headline numbers for the requested year, or null with <see cref="SimulationError"/> if it can't run.</summary>
	public PayrollSourceHeadline? Headline { get; set; }
	public string? SimulationError { get; set; }
}

public sealed record PayrollSourceHeadline(
	int Year,
	int CheckCount,
	decimal AnnualGrossPay,
	decimal AnnualNetPay,
	decimal RegularNetPay,
	int WarningCount);

public sealed class PayrollPreviewRequest
{
	public int Year { get; set; }
	public PayrollSourceModel Source { get; set; } = new();
}

/// <summary>Reference data the payroll editor needs in order to build and explain a source.</summary>
public sealed record PayrollReferenceModel(
	int CurrentYear,
	IReadOnlyCollection<string> SupportedStates,
	PayrollSourceModel NewSourceTemplate,
	IReadOnlyDictionary<string, TaxableWageTypes> DefaultDeductionTreatment,
	TaxableWageTypes DefaultTraditional401kTreatment,
	TaxableWageTypes DefaultCafeteriaPlanTreatment,
	TaxableWageTypes DefaultGroupTermLifeTreatment,
	int MaxPayrollSources);
