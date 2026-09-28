using System.ComponentModel.DataAnnotations;
using System.Security.Claims;
using FastEndpoints;
using PjBudget.Shared.Database.Entities;
using Microsoft.AspNetCore.Identity;

namespace PjBudget.Features.Authentication;

public sealed class ChangePasswordRequest
{
	[Required] public string CurrentPassword { get; set; } = null!;
	[Required] public string NewPassword { get; set; } = null!;
}

public sealed class ChangePasswordEndpoint : Endpoint<ChangePasswordRequest>
{
	private readonly UserManager<ApplicationUser> _users;

	public ChangePasswordEndpoint(UserManager<ApplicationUser> users)
	{
		_users = users;
	}

	public override void Configure()
	{
		Post("/api/auth/change-password");
	}

	public override async Task HandleAsync(ChangePasswordRequest req, CancellationToken ct)
	{
		var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
		if (userId is null)
		{
			await Send.UnauthorizedAsync(ct);
			return;
		}

		var user = await _users.FindByIdAsync(userId);
		if (user is null)
		{
			await Send.UnauthorizedAsync(ct);
			return;
		}

		var result = await _users.ChangePasswordAsync(user, req.CurrentPassword, req.NewPassword);
		if (!result.Succeeded)
		{
			foreach (var error in result.Errors)
			{
				AddError(error.Description);
			}
			await Send.ErrorsAsync(cancellation: ct);
			return;
		}

		await Send.OkAsync(new { ok = true }, ct);
	}
}
