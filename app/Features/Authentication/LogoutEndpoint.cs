using FastEndpoints;
using PjBudget.Shared.Database.Entities;
using Microsoft.AspNetCore.Identity;

namespace PjBudget.Features.Authentication;

public sealed class LogoutEndpoint : EndpointWithoutRequest
{
	private readonly SignInManager<ApplicationUser> _signIn;

	public LogoutEndpoint(SignInManager<ApplicationUser> signIn) => _signIn = signIn;

	public override void Configure()
	{
		Post("/api/auth/logout");
		AllowAnonymous();
	}

	public override async Task HandleAsync(CancellationToken ct)
	{
		await _signIn.SignOutAsync();
		await Send.OkAsync(new { ok = true }, ct);
	}
}
