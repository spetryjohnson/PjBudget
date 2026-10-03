using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using PjBudget.Features.Locales;
using PjBudget.Features.People;
using PjBudget.Features.Scenarios;
using PjBudget.Shared.Database;
using PjBudget.Shared.Domain;

namespace PjBudget.Features.Payroll;

/// <summary>
/// One job's pay and payroll elections within a scenario. <c>*Percent</c> values are 0–100; other amounts are dollars.
/// Each <c>*PreTaxFor</c> flag set lists the wage bases that deduction reduces.
/// </summary>
public class PayrollSource : IAuditedEntity, IVersionedEntity
{
	public int Id { get; set; }
	public int ScenarioId { get; set; }
	public int PersonId { get; set; }
	public Person? Person { get; set; }
	public string Name { get; set; } = string.Empty;
	public string? EmployerName { get; set; }
	public int SortOrder { get; set; }

	/// <summary>Where the work is done, which sets the state and the city that taxes it.</summary>
	public int WorkLocaleId { get; set; }
	public Locale? WorkLocale { get; set; }

	public PayBasis PayBasis { get; set; }
	public decimal? AnnualSalary { get; set; }
	public decimal? HourlyRate { get; set; }
	public decimal? HoursPerCheck { get; set; }
	public PayFrequency PayFrequency { get; set; }
	public int? SemimonthlyPayDay1 { get; set; }
	public int? SemimonthlyPayDay2 { get; set; }

	/// <summary>Any known biweekly payday; the rest of the schedule is counted from it in 14-day steps.</summary>
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

	public FilingStatus W4FilingStatus { get; set; }
	public bool W4MultipleJobs { get; set; }
	public decimal W4Credits { get; set; }
	public decimal W4OtherIncome { get; set; }
	public decimal W4Deductions { get; set; }
	public decimal W4ExtraWithholding { get; set; }

	public int StateWithholdingExemptions { get; set; }
	public decimal StateAdditionalWithholding { get; set; }

	/// <summary>Absorbs whatever difference from the real paycheck the model can't explain, so net pay matches exactly.</summary>
	public decimal NetPayAdjustmentPerCheck { get; set; }

	public decimal? ActualNetPay { get; set; }
	public DateOnly? ActualNetPayDate { get; set; }

	public List<PayrollDeduction> Deductions { get; set; } = [];

	public DateTime CreatedAt { get; set; }
	public DateTime UpdatedAt { get; set; }
	public Guid Version { get; set; }
}

public class PayrollDeduction
{
	public int Id { get; set; }
	public int PayrollSourceId { get; set; }
	public DeductionType Type { get; set; }
	public string Label { get; set; } = string.Empty;

	/// <summary>The yearly premium or election; it is deducted in equal parts from each check.</summary>
	public decimal AnnualAmount { get; set; }

	/// <summary>The exact amount from a real paystub, when dividing the annual amount doesn't match it.</summary>
	public decimal? PerCheckOverride { get; set; }

	public TaxableWageTypes PreTaxFor { get; set; }
	public int SortOrder { get; set; }
}

public sealed class PayrollSourceConfiguration : IEntityTypeConfiguration<PayrollSource>
{
	public void Configure(EntityTypeBuilder<PayrollSource> e)
	{
		e.Property(x => x.Name).HasMaxLength(100).IsRequired();
		e.Property(x => x.EmployerName).HasMaxLength(100);

		e.HasOne<Scenario>().WithMany().HasForeignKey(x => x.ScenarioId).OnDelete(DeleteBehavior.Cascade);
		e.HasOne(x => x.Person).WithMany().HasForeignKey(x => x.PersonId).OnDelete(DeleteBehavior.Restrict);
		e.HasOne(x => x.WorkLocale).WithMany().HasForeignKey(x => x.WorkLocaleId).OnDelete(DeleteBehavior.Restrict);
		e.HasMany(x => x.Deductions).WithOne().HasForeignKey(x => x.PayrollSourceId).OnDelete(DeleteBehavior.Cascade);
	}
}

public sealed class PayrollDeductionConfiguration : IEntityTypeConfiguration<PayrollDeduction>
{
	public void Configure(EntityTypeBuilder<PayrollDeduction> e)
	{
		e.Property(x => x.Label).HasMaxLength(100).IsRequired();
	}
}
