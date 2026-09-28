using System.ComponentModel.DataAnnotations;
using System.Security.Claims;
using FastEndpoints;
using PjBudget.Shared.Database.Entities;
using Microsoft.AspNetCore.Identity;

namespace PjBudget.Features.Authentication;

public sealed class LoginRequest
{
	[Required, EmailAddress] public string Email { get; set; } = null!;
	[Required] public string Password { get; set; } = null!;
	public bool RememberMe { get; set; }
}

public sealed class LoginEndpoint : Endpoint<LoginRequest>
{
	private readonly SignInManager<ApplicationUser> _signIn;
	private readonly UserManager<ApplicationUser> _users;

	public LoginEndpoint(SignInManager<ApplicationUser> signIn, UserManager<ApplicationUser> users)
	{
		_signIn = signIn;
		_users = users;
	}

	public override void Configure()
	{
		Post("/api/auth/login");
		AllowAnonymous();
	}

	public override async Task HandleAsync(LoginRequest req, CancellationToken ct)
	{
		var user = await _users.FindByEmailAsync(req.Email);
		if (user is null)
		{
			await Send.UnauthorizedAsync(ct);
			return;
		}

		var result = await _signIn.CheckPasswordSignInAsync(user, req.Password, lockoutOnFailure: true);
		if (!result.Succeeded)
		{
			await Send.UnauthorizedAsync(ct);
			return;
		}

		await _signIn.SignInWithClaimsAsync(user, req.RememberMe,
			[new Claim("auth_method", "local")]);

		var roles = await _users.GetRolesAsync(user);

		// User must change password on first login with the default seeded password.
		var mustChangePassword = req.Email == DefaultCredentials.DefaultAdminEmail &&
		                         req.Password == DefaultCredentials.DefaultAdminPassword;

		await Send.OkAsync(new
		{
			ok = true,
			user = new { user.Id, user.Email, user.DisplayName, Roles = roles },
			mustChangePassword
		}, ct);
	}
}
