using PjBudget.Features.Authentication;
using PjBudget.Features.Identity;
using PjBudget.Features.Scenarios;
using PjBudget.Features.TaxYears;
using PjBudget.Shared.Database;
using PjBudget.Shared.Database.Entities;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace PjBudget.Shared.AppStartup;

public static class SeedData
{
	public static async Task EnsureSeededAsync(IServiceProvider services)
	{
		using var scope = services.CreateScope();
		var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
		var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
		var roleManager = scope.ServiceProvider.GetRequiredService<RoleManager<IdentityRole<Guid>>>();

		await db.Database.MigrateAsync();

		// Enable WAL mode for better concurrent read/write performance
		await db.Database.ExecuteSqlRawAsync("PRAGMA journal_mode=WAL;");

		foreach (var roleName in new[] { AppRoles.SystemAdmin, AppRoles.User })
		{
			if (!await roleManager.RoleExistsAsync(roleName))
			{
				await roleManager.CreateAsync(new IdentityRole<Guid>(roleName));
			}
		}

		var user = await userManager.Users.FirstOrDefaultAsync(u => u.Email == DefaultCredentials.DefaultAdminEmail);
		if (user is null)
		{
			user = new ApplicationUser
			{
				Id = Guid.NewGuid(),
				UserName = DefaultCredentials.DefaultAdminEmail,
				Email = DefaultCredentials.DefaultAdminEmail,
				EmailConfirmed = true,
				DisplayName = "Default Admin Account"
			};

			var result = await userManager.CreateAsync(user, DefaultCredentials.DefaultAdminPassword);
			if (!result.Succeeded)
				throw new InvalidOperationException("Failed to create admin user: " +
				                                    string.Join("; ", result.Errors));

			await userManager.AddToRoleAsync(user, AppRoles.SystemAdmin);
			await userManager.AddToRoleAsync(user, AppRoles.User);
		}
		else if (!await userManager.HasPasswordAsync(user))
		{
			// Admin exists with no password (e.g. failed prior change). Reset so we can log in again;
			// the login flow will force a password change on next login.
			var resetResult = await userManager.AddPasswordAsync(user, DefaultCredentials.DefaultAdminPassword);
			if (!resetResult.Succeeded)
				throw new InvalidOperationException("Failed to reset admin password: " +
				                                    string.Join("; ", resetResult.Errors));
		}

		await ScenarioSeeder.EnsureCurrentScenarioAsync(db);
		await TaxYearSeeder.EnsureSeededAsync(db);
	}
}
