using Microsoft.AspNetCore.Identity;

namespace PjBudget.Shared.Database.Entities;

public class ApplicationUser : IdentityUser<Guid>
{
	public string DisplayName { get; set; } = string.Empty;
}
