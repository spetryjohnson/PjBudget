namespace PjBudget.Shared.Infrastructure;

/// <summary>
/// Abstraction for system time access to enable testable date/time-dependent logic.
/// </summary>
public interface ISystemClock
{
	DateTime Now { get; }
	DateTime UtcNow { get; }
	DateOnly Today { get; }
}
