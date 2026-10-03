using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using PjBudget.Shared.Database;

namespace PjBudget.Features.People;

/// <summary>
/// A household member who earns income. People are shared by all scenarios; each scenario's payroll sources point at
/// them. Per-person limits (the 401(k) limit across jobs, catch-up contributions) are what tie sources to a person.
/// </summary>
public class Person : IAuditedEntity, IVersionedEntity
{
	public int Id { get; set; }
	public string DisplayName { get; set; } = string.Empty;

	/// <summary>Only the year matters: catch-up eligibility depends on the age reached by December 31.</summary>
	public DateOnly? BirthDate { get; set; }

	public int SortOrder { get; set; }

	public DateTime CreatedAt { get; set; }
	public DateTime UpdatedAt { get; set; }
	public Guid Version { get; set; }
}

public sealed class PersonConfiguration : IEntityTypeConfiguration<Person>
{
	public void Configure(EntityTypeBuilder<Person> e)
	{
		e.ToTable("People");
		e.Property(x => x.DisplayName).HasMaxLength(100).IsRequired();
	}
}
