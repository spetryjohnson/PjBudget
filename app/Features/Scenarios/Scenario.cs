using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using PjBudget.Shared.Database;

namespace PjBudget.Features.Scenarios;

/// <summary>
/// A complete, independent copy of the household plan (payroll sources, household settings, and later the budget).
/// Exactly one scenario is current; what-if scenarios are copies that can later be promoted to current.
/// </summary>
public class Scenario : IAuditedEntity, IVersionedEntity
{
	public int Id { get; set; }
	public string Name { get; set; } = string.Empty;
	public string? Description { get; set; }
	public bool IsCurrent { get; set; }

	public DateTime CreatedAt { get; set; }
	public DateTime UpdatedAt { get; set; }
	public Guid Version { get; set; }
}

public sealed class ScenarioConfiguration : IEntityTypeConfiguration<Scenario>
{
	public void Configure(EntityTypeBuilder<Scenario> e)
	{
		e.Property(x => x.Name).HasMaxLength(100).IsRequired();
		e.Property(x => x.Description).HasMaxLength(1000);

		// A partial unique index allows any number of non-current scenarios but only one current one.
		e.HasIndex(x => x.IsCurrent).IsUnique().HasFilter("\"IsCurrent\" = 1");
	}
}
