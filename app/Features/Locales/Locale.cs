using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using PjBudget.Shared.Database;
using PjBudget.Shared.Domain;

namespace PjBudget.Features.Locales;

/// <summary>
/// A place that levies local income taxes. A payroll source uses its work locale for city tax, and the household
/// uses its home locale for school district tax. Rates are fractions (0.02 means 2%).
/// </summary>
public class Locale : IAuditedEntity, IVersionedEntity
{
	public int Id { get; set; }
	public string Name { get; set; } = string.Empty;
	public string City { get; set; } = string.Empty;
	public string StateCode { get; set; } = string.Empty;
	public string? ZipCode { get; set; }
	public decimal MunicipalTaxRate { get; set; }

	public string? SchoolDistrictName { get; set; }

	/// <summary>The state's identifier for the district (Ohio uses four digits, entered on form IT 4).</summary>
	public string? SchoolDistrictNumber { get; set; }

	public decimal? SchoolDistrictTaxRate { get; set; }
	public SchoolDistrictTaxBase? SchoolDistrictTaxBase { get; set; }

	public DateTime CreatedAt { get; set; }
	public DateTime UpdatedAt { get; set; }
	public Guid Version { get; set; }
}

public sealed class LocaleConfiguration : IEntityTypeConfiguration<Locale>
{
	public void Configure(EntityTypeBuilder<Locale> e)
	{
		e.Property(x => x.Name).HasMaxLength(100).IsRequired();
		e.Property(x => x.City).HasMaxLength(100).IsRequired();
		e.Property(x => x.StateCode).HasMaxLength(2).IsRequired();
		e.Property(x => x.ZipCode).HasMaxLength(10);
		e.Property(x => x.SchoolDistrictName).HasMaxLength(100);
		e.Property(x => x.SchoolDistrictNumber).HasMaxLength(10);
	}
}
