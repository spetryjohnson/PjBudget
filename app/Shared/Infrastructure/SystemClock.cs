namespace PjBudget.Shared.Infrastructure;

public sealed class SystemClock : ISystemClock
{
	public DateTime Now => DateTime.Now;
	public DateTime UtcNow => DateTime.UtcNow;
	public DateOnly Today => DateOnly.FromDateTime(DateTime.Now);
}
