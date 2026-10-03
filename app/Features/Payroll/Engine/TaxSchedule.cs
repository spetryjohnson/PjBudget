namespace PjBudget.Features.Payroll.Engine;

/// <summary>
/// One row of a bracketed schedule: an amount above <see cref="Over"/> owes <see cref="BaseAmount"/> plus
/// <see cref="Rate"/> times the excess over <see cref="Over"/>.
/// </summary>
public readonly record struct TaxBracket(decimal Over, decimal BaseAmount, decimal Rate);

/// <summary>
/// A "base amount plus a rate on the excess" schedule. IRS and Ohio publish every table in this shape. It is also
/// used for tier lookups, such as an exemption amount by income, where only one column of the bracket matters.
/// </summary>
public sealed class TaxSchedule
{
	private readonly TaxBracket[] _brackets;
	private readonly bool _inclusiveLowerBounds;

	/// <param name="inclusiveLowerBounds">
	/// IRS withholding tables read "at least A but less than B", while most statutes say "more than A". This only
	/// matters where adjacent brackets don't meet exactly, such as Ohio's base jump at the start of its taxable
	/// bracket, or IRS tables whose thresholds were rounded for publication.
	/// </param>
	public TaxSchedule(IEnumerable<TaxBracket> brackets, bool inclusiveLowerBounds = false)
	{
		_brackets = brackets.OrderBy(b => b.Over).ToArray();
		if (_brackets.Length == 0)
		{
			throw new ArgumentException("A tax schedule needs at least one bracket.", nameof(brackets));
		}

		_inclusiveLowerBounds = inclusiveLowerBounds;
	}

	public IReadOnlyList<TaxBracket> Brackets => _brackets;

	/// <summary>
	/// Builds a schedule from statutory marginal rates by accumulating the tax owed below each bracket into its base
	/// amount.
	/// </summary>
	public static TaxSchedule FromMarginalRates(IEnumerable<(decimal Over, decimal Rate)> marginalRates)
	{
		var brackets = new List<TaxBracket>();
		foreach (var (over, rate) in marginalRates.OrderBy(r => r.Over))
		{
			var baseAmount = brackets.Count == 0
				? 0m
				: brackets[^1].BaseAmount + (over - brackets[^1].Over) * brackets[^1].Rate;

			brackets.Add(new TaxBracket(over, baseAmount, rate));
		}

		return new TaxSchedule(brackets);
	}

	/// <summary>
	/// The bracket that applies to <paramref name="amount"/>. Amounts at or below the first bracket's lower bound use
	/// the first bracket, so tier lookups starting at 0 also cover 0.
	/// </summary>
	public TaxBracket FindBracket(decimal amount)
	{
		for (var i = _brackets.Length - 1; i > 0; i--)
		{
			var bracket = _brackets[i];
			if (amount > bracket.Over || (_inclusiveLowerBounds && amount == bracket.Over))
			{
				return bracket;
			}
		}

		return _brackets[0];
	}

	public decimal Calculate(decimal amount)
	{
		var bracket = FindBracket(amount);
		return bracket.BaseAmount + Math.Max(0m, amount - bracket.Over) * bracket.Rate;
	}
}
