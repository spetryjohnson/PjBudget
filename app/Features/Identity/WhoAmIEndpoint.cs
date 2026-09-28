using System.Security.Claims;
using FastEndpoints;
using PjBudget.Shared.Database.Entities;
using Microsoft.AspNetCore.Identity;

namespace PjBudget.Features.Identity;

public sealed class WhoAmIEndpoint : EndpointWithoutRequest
{
	private readonly UserManager<ApplicationUser> _users;

	public WhoAmIEndpoint(UserManager<ApplicationUser> users) => _users = users;

	public override void Configure()
	{
		Get("/api/identity/whoami");
		AllowAnonymous();
	}

	public override async Task HandleAsync(CancellationToken ct)
	{
		var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);

		if (userId is null || !Guid.TryParse(userId, out _))
		{
			await Send.OkAsync(new { user = (object?)null }, ct);
			return;
		}

		var user = await _users.FindByIdAsync(userId);
		if (user is null)
		{
			await Send.OkAsync(new { user = (object?)null }, ct);
			return;
		}

		var roles = await _users.GetRolesAsync(user);
		var authMethod = User.FindFirstValue("auth_method");

		await Send.OkAsync(new
		{
			user = new
			{
				user.Id,
				user.Email,
				user.DisplayName,
				AuthMethod = authMethod,
				Roles = roles
			}
		}, ct);
	}
}
