using PjBudget.Shared.Domain;

namespace PjBudget.Features.Payroll.Engine;

public sealed class PayScheduleGenerator
{
	/// <summary>
	/// The pay periods per year that withholding formulas annualize with (IRS Pub 15-T Table 3). For biweekly this is
	/// 26 even in a year that happens to contain 27 paydays.
	/// </summary>
	public static int PeriodsPerYear(PayFrequency frequency) => frequency switch
	{
		PayFrequency.Semimonthly => 24,
		PayFrequency.Biweekly => 26,
		_ => throw new ArgumentOutOfRangeException(nameof(frequency), frequency, null),
	};

	/// <summary>
	/// Actual pay dates in <paramref name="year"/>, after moving paydays off weekends and bank holidays. A check
	/// belongs to the year it is paid in, so nominal paydays in early January of the next year are also considered:
	/// a payday that falls on New Year's Day is paid on Dec 31.
	/// </summary>
	public IReadOnlyList<DateOnly> PayDates(PayScheduleSettings settings, int year)
	{
		var nominal = settings.Frequency switch
		{
			PayFrequency.Semimonthly => SemimonthlyPaydays(settings, year),
			PayFrequency.Biweekly => BiweeklyPaydays(settings, year),
			_ => throw new ArgumentOutOfRangeException(nameof(settings), settings.Frequency, null),
		};

		return nominal
			.Select(BankHolidayCalendar.OnOrBeforeBusinessDay)
			.Where(d => d.Year == year)
			.Distinct()
			.Order()
			.ToList();
	}

	private static IEnumerable<DateOnly> SemimonthlyPaydays(PayScheduleSettings settings, int year)
	{
		var days = new[]
		{
			settings.SemimonthlyPayDay1 ?? throw new ArgumentException("Semimonthly pay needs a first payday."),
			settings.SemimonthlyPayDay2 ?? throw new ArgumentException("Semimonthly pay needs a second payday."),
		};

		for (var month = new DateOnly(year, 1, 1); month <= new DateOnly(year + 1, 1, 1); month = month.AddMonths(1))
		{
			var daysInMonth = DateTime.DaysInMonth(month.Year, month.Month);
			foreach (var day in days)
			{
				yield return new DateOnly(month.Year, month.Month, Math.Min(day, daysInMonth));
			}
		}
	}

	private static IEnumerable<DateOnly> BiweeklyPaydays(PayScheduleSettings settings, int year)
	{
		var anchor = settings.BiweeklyAnchorDate
			?? throw new ArgumentException("Biweekly pay needs a known payday to count from.");

		var startOfYear = new DateOnly(year, 1, 1);
		var periodsFromAnchor = (int)Math.Ceiling((startOfYear.DayNumber - anchor.DayNumber) / 14.0);
		var last = new DateOnly(year + 1, 1, 14);

		for (var payday = anchor.AddDays(14 * periodsFromAnchor); payday <= last; payday = payday.AddDays(14))
		{
			yield return payday;
		}
	}
}
