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

	protected override void OnModelCreating(ModelBuilder b)
	{
		base.OnModelCreating(b);

		b.Entity<ApplicationUser>(e =>
		{
			e.Property(x => x.DisplayName)
				.HasMaxLength(256)
				.IsRequired();
		});
	}
}
