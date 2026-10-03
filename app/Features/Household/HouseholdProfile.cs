using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using PjBudget.Features.Locales;
using PjBudget.Features.Scenarios;
using PjBudget.Shared.Database;
using PjBudget.Shared.Domain;

namespace PjBudget.Features.Household;

/// <summary>
/// Household-wide settings for one scenario. It holds the settings that shape every paycheck (where the household
/// lives, its HSA coverage) and the extra inputs the annual tax projection needs beyond wages.
/// </summary>
public class HouseholdProfile : IAuditedEntity, IVersionedEntity
{
	public int Id { get; set; }
	public int ScenarioId { get; set; }

	/// <summary>Drives school district withholding (and, in future, resident city tax).</summary>
	public int? HomeLocaleId { get; set; }
	public Locale? HomeLocale { get; set; }

	public HsaCoverage HsaCoverage { get; set; }

	/// <summary>The filing status on the annual return, which can differ from what each W-4 claims.</summary>
	public FilingStatus TaxFilingStatus { get; set; } = FilingStatus.MarriedFilingJointly;

	public decimal FederalOtherIncome { get; set; }
	public decimal FederalAdjustments { get; set; }

	/// <summary>Itemized deductions, if you itemize. The projection uses the larger of this and the standard deduction.</summary>
	public decimal? FederalItemizedDeductions { get; set; }

	public decimal FederalCredits { get; set; }

	/// <summary>Ohio additions (+) or deductions (−) to federal AGI, such as 529 contributions.</summary>
	public decimal OhioAdjustments { get; set; }

	public int OhioExemptionCount { get; set; } = 2;
	public decimal OhioOtherCredits { get; set; }

	public DateTime CreatedAt { get; set; }
	public DateTime UpdatedAt { get; set; }
	public Guid Version { get; set; }
}

public sealed class HouseholdProfileConfiguration : IEntityTypeConfiguration<HouseholdProfile>
{
	public void Configure(EntityTypeBuilder<HouseholdProfile> e)
	{
		e.HasIndex(x => x.ScenarioId).IsUnique();
		e.HasOne<Scenario>().WithMany().HasForeignKey(x => x.ScenarioId).OnDelete(DeleteBehavior.Cascade);
		e.HasOne(x => x.HomeLocale).WithMany().HasForeignKey(x => x.HomeLocaleId).OnDelete(DeleteBehavior.Restrict);
	}
}
