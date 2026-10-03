using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using PjBudget.Shared.Database;
using PjBudget.Shared.Domain;

namespace PjBudget.Features.TaxYears;

/// <summary>
/// One calendar year's tax rules and contribution limits. Every value is public data that changes annually, so it
/// lives in the database (editable in the UI) rather than in code. Rates are fractions (0.062 means 6.2%).
/// </summary>
public class TaxYear : IAuditedEntity, IVersionedEntity
{
	public int Id { get; set; }
	public int Year { get; set; }

	public TaxYearFica Fica { get; set; } = new();
	public TaxYearLimits Limits { get; set; } = new();
	public TaxYearOhio Ohio { get; set; } = new();

	public List<FederalFilingStatusParameters> FilingStatuses { get; set; } = [];
	public List<TaxScheduleRow> ScheduleRows { get; set; } = [];

	public DateTime CreatedAt { get; set; }
	public DateTime UpdatedAt { get; set; }
	public Guid Version { get; set; }
}

public class TaxYearFica
{
	public decimal SocialSecurityRate { get; set; }
	public decimal SocialSecurityWageBase { get; set; }
	public decimal MedicareRate { get; set; }
	public decimal AdditionalMedicareRate { get; set; }

	/// <summary>Employers withhold Additional Medicare on wages above this, regardless of filing status.</summary>
	public decimal AdditionalMedicareWithholdingThreshold { get; set; }
}

public class TaxYearLimits
{
	public decimal ElectiveDeferral { get; set; }
	public decimal CatchUpAge50 { get; set; }
	public decimal CatchUpAge60To63 { get; set; }
	public decimal HsaSelfOnly { get; set; }
	public decimal HsaFamily { get; set; }
	public decimal HsaCatchUpAge55 { get; set; }
	public decimal HealthFsa { get; set; }
}

public class TaxYearOhio
{
	/// <summary>Annual amount per IT 4 exemption, subtracted before the withholding formula is applied.</summary>
	public decimal WithholdingExemptionAmount { get; set; }

	/// <summary>Personal exemptions are only allowed when modified AGI is below this.</summary>
	public decimal ExemptionMagiLimit { get; set; }

	public decimal JointFilingCreditCap { get; set; }
	public decimal JointFilingCreditMagiLimit { get; set; }

	/// <summary>Each spouse needs at least this much qualifying income for the joint filing credit.</summary>
	public decimal JointFilingCreditMinSpouseIncome { get; set; }
}

public class FederalFilingStatusParameters
{
	public int Id { get; set; }
	public int TaxYearId { get; set; }
	public FilingStatus FilingStatus { get; set; }
	public decimal StandardDeduction { get; set; }

	/// <summary>
	/// IRS Pub 15-T Worksheet 1A line 1g: subtracted from annualized wages when the W-4 Step 2 box is unchecked.
	/// </summary>
	public decimal StandardWithholdingAdjustment { get; set; }

	/// <summary>Combined Medicare wages above this owe Additional Medicare tax on the annual return.</summary>
	public decimal AdditionalMedicareLiabilityThreshold { get; set; }
}

public class TaxScheduleRow
{
	public int Id { get; set; }
	public int TaxYearId { get; set; }
	public TaxScheduleKind Kind { get; set; }

	/// <summary>Set only for federal income tax brackets, which differ by filing status.</summary>
	public FilingStatus? FilingStatus { get; set; }

	/// <summary>The bracket applies to amounts greater than this; the first row of a schedule is 0.</summary>
	public decimal Over { get; set; }

	public decimal BaseAmount { get; set; }
	public decimal Rate { get; set; }
}

public sealed class TaxYearConfiguration : IEntityTypeConfiguration<TaxYear>
{
	public void Configure(EntityTypeBuilder<TaxYear> e)
	{
		e.HasIndex(x => x.Year).IsUnique();

		e.OwnsOne(x => x.Fica);
		e.OwnsOne(x => x.Limits);
		e.OwnsOne(x => x.Ohio);
		e.Navigation(x => x.Fica).IsRequired();
		e.Navigation(x => x.Limits).IsRequired();
		e.Navigation(x => x.Ohio).IsRequired();

		e.HasMany(x => x.FilingStatuses).WithOne().HasForeignKey(x => x.TaxYearId).OnDelete(DeleteBehavior.Cascade);
		e.HasMany(x => x.ScheduleRows).WithOne().HasForeignKey(x => x.TaxYearId).OnDelete(DeleteBehavior.Cascade);
	}
}

public sealed class FederalFilingStatusParametersConfiguration : IEntityTypeConfiguration<FederalFilingStatusParameters>
{
	public void Configure(EntityTypeBuilder<FederalFilingStatusParameters> e)
	{
		e.HasIndex(x => new { x.TaxYearId, x.FilingStatus }).IsUnique();
	}
}
