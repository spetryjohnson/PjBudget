using PjBudget.Shared.Domain;

namespace PjBudget.Features.TaxYears;

public sealed class TaxYearSummaryModel
{
	public int Year { get; set; }
	public DateTime UpdatedAt { get; set; }
}

/// <summary>
/// A full tax year as the editor sees it. Schedules are grouped by kind (and filing status, for federal brackets)
/// so that each one can be edited as its own table.
/// </summary>
public sealed class TaxYearModel
{
	public int Year { get; set; }
	public Guid Version { get; set; }
	public TaxYearFica Fica { get; set; } = new();
	public TaxYearLimits Limits { get; set; } = new();
	public TaxYearOhio Ohio { get; set; } = new();
	public List<FilingStatusParametersModel> FilingStatuses { get; set; } = [];
	public List<TaxScheduleModel> Schedules { get; set; } = [];
}

public sealed class FilingStatusParametersModel
{
	public FilingStatus FilingStatus { get; set; }
	public decimal StandardDeduction { get; set; }
	public decimal StandardWithholdingAdjustment { get; set; }
	public decimal AdditionalMedicareLiabilityThreshold { get; set; }
}

public sealed class TaxScheduleModel
{
	public TaxScheduleKind Kind { get; set; }
	public FilingStatus? FilingStatus { get; set; }
	public List<TaxScheduleRowModel> Rows { get; set; } = [];
}

public sealed class TaxScheduleRowModel
{
	public decimal Over { get; set; }
	public decimal BaseAmount { get; set; }
	public decimal Rate { get; set; }
}
