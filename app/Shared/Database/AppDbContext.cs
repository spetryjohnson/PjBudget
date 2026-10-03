using PjBudget.Features.Household;
using PjBudget.Features.Locales;
using PjBudget.Features.Payroll;
using PjBudget.Features.People;
using PjBudget.Features.Scenarios;
using PjBudget.Features.TaxYears;
using PjBudget.Shared.Database.Entities;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace PjBudget.Shared.Database;

public class AppDbContext
	: IdentityDbContext<ApplicationUser, IdentityRole<Guid>, Guid>
{
	public AppDbContext(DbContextOptions<AppDbContext> options)
		: base(options) { }

	public DbSet<Scenario> Scenarios => Set<Scenario>();
	public DbSet<TaxYear> TaxYears => Set<TaxYear>();
	public DbSet<Locale> Locales => Set<Locale>();
	public DbSet<Person> People => Set<Person>();
	public DbSet<HouseholdProfile> HouseholdProfiles => Set<HouseholdProfile>();
	public DbSet<PayrollSource> PayrollSources => Set<PayrollSource>();

	protected override void OnModelCreating(ModelBuilder b)
	{
		base.OnModelCreating(b);

		b.Entity<ApplicationUser>(e =>
		{
			e.Property(x => x.DisplayName)
				.HasMaxLength(256)
				.IsRequired();
		});

		b.ApplyConfigurationsFromAssembly(typeof(AppDbContext).Assembly);

		// Conventions go last so they see every property configured above.
		b.UseStringConstantsForEnums();
		b.UseUtcDateTimes();
		b.UseVersionConcurrencyTokens();
	}
}
