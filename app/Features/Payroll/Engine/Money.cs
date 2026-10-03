namespace PjBudget.Features.Payroll.Engine;

public static class Money
{
	/// <summary>
	/// Rounds to cents with midpoints away from zero, the way payroll systems and Excel's ROUND do. .NET's default
	/// (banker's rounding) would occasionally be a penny off from a real paystub.
	/// </summary>
	public static decimal Round(decimal amount) => Math.Round(amount, 2, MidpointRounding.AwayFromZero);

	/// <summary>Cuts an amount to whole cents, dropping any fraction of a cent instead of rounding it.</summary>
	public static decimal Truncate(decimal amount) => Math.Truncate(amount * 100m) / 100m;

	public static decimal RoundToDollar(decimal amount) => Math.Round(amount, 0, MidpointRounding.AwayFromZero);
}
