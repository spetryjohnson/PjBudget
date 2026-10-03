using System.Collections.Concurrent;

namespace PjBudget.Features.Payroll.Engine;

/// <summary>
/// Federal Reserve holidays. Direct deposits can't settle on these days, so a payday that lands on one is paid the
/// business day before.
/// </summary>
public static class BankHolidayCalendar
{
	private static readonly ConcurrentDictionary<int, HashSet<DateOnly>> HolidaysByYear = new();

	public static bool IsBusinessDay(DateOnly date)
		=> date.DayOfWeek is not (DayOfWeek.Saturday or DayOfWeek.Sunday)
		   && !HolidaysByYear.GetOrAdd(date.Year, HolidaysIn).Contains(date);

	public static DateOnly OnOrBeforeBusinessDay(DateOnly date)
	{
		while (!IsBusinessDay(date))
		{
			date = date.AddDays(-1);
		}

		return date;
	}

	private static HashSet<DateOnly> HolidaysIn(int year) =>
	[
		Observed(new DateOnly(year, 1, 1)),
		NthWeekday(year, 1, DayOfWeek.Monday, 3), // Martin Luther King Jr. Day
		NthWeekday(year, 2, DayOfWeek.Monday, 3), // Washington's Birthday
		LastWeekday(year, 5, DayOfWeek.Monday), // Memorial Day
		Observed(new DateOnly(year, 6, 19)), // Juneteenth
		Observed(new DateOnly(year, 7, 4)),
		NthWeekday(year, 9, DayOfWeek.Monday, 1), // Labor Day
		NthWeekday(year, 10, DayOfWeek.Monday, 2), // Columbus Day
		Observed(new DateOnly(year, 11, 11)), // Veterans Day
		NthWeekday(year, 11, DayOfWeek.Thursday, 4), // Thanksgiving
		Observed(new DateOnly(year, 12, 25)),
	];

	// The Fed closes the Monday after a Sunday holiday, but stays open the Friday before a Saturday holiday.
	private static DateOnly Observed(DateOnly holiday)
		=> holiday.DayOfWeek == DayOfWeek.Sunday ? holiday.AddDays(1) : holiday;

	private static DateOnly NthWeekday(int year, int month, DayOfWeek day, int n)
	{
		var first = new DateOnly(year, month, 1);
		var offset = ((int)day - (int)first.DayOfWeek + 7) % 7;
		return first.AddDays(offset + 7 * (n - 1));
	}

	private static DateOnly LastWeekday(int year, int month, DayOfWeek day)
	{
		var last = new DateOnly(year, month, DateTime.DaysInMonth(year, month));
		var offset = ((int)last.DayOfWeek - (int)day + 7) % 7;
		return last.AddDays(-offset);
	}
}
