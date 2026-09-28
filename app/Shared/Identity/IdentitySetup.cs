using PjBudget.Shared.Database;
using PjBudget.Shared.Database.Entities;
using Microsoft.AspNetCore.Identity;

namespace PjBudget.Shared.Identity;

public static class IdentitySetup
{
	public static IServiceCollection AddAppIdentity(this IServiceCollection services)
	{
		services
			.AddIdentity<ApplicationUser, IdentityRole<Guid>>(opts =>
			{
				opts.Password.RequireDigit = true;
				opts.Password.RequireLowercase = true;
				opts.Password.RequireUppercase = true;
				opts.Password.RequireNonAlphanumeric = false;
				opts.Password.RequiredLength = 8;

				opts.User.RequireUniqueEmail = true;
			})
			.AddEntityFrameworkStores<AppDbContext>()
			.AddDefaultTokenProviders();

		services.ConfigureApplicationCookie(o =>
		{
			o.LoginPath = "/auth/login";
			o.LogoutPath = "/auth/logout";
			o.AccessDeniedPath = "/auth/denied";

			// Return 401 for API calls instead of redirecting to login page
			o.Events.OnRedirectToLogin = ctx =>
			{
				if (ctx.Request.Path.StartsWithSegments("/api"))
				{
					ctx.Response.StatusCode = StatusCodes.Status401Unauthorized;
					return Task.CompletedTask;
				}
				ctx.Response.Redirect(ctx.RedirectUri);
				return Task.CompletedTask;
			};
		});

		return services;
	}
}
