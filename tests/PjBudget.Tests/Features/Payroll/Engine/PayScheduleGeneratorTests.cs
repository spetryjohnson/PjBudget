using PjBudget.Features.Payroll.Engine;
using PjBudget.Shared.Domain;

namespace PjBudget.Tests.Features.Payroll.Engine;

public class PayScheduleGeneratorTests
{
	private readonly PayScheduleGenerator _generator = new();

	private static DateOnly[] Dates2026(params string[] monthDays)
		=> monthDays.Select(md => DateOnly.Parse($"2026-{md}")).ToArray();

	[Test]
	public void SemimonthlyPaydaysMoveBackOffWeekendsAndHolidays()
	{
		var dates = _generator.PayDates(new PayScheduleSettings(PayFrequency.Semimonthly, 10, 25), 2026);

		// Memorial Day (05-25) moves to 05-22 and Christmas (12-25) moves to 12-24; the rest are weekend shifts.
		Assert.That(dates, Is.EqualTo(Dates2026(
			"01-09", "01-23", "02-10", "02-25", "03-10", "03-25", "04-10", "04-24", "05-08", "05-22", "06-10", "06-25",
			"07-10", "07-24", "08-10", "08-25", "09-10", "09-25", "10-09", "10-23", "11-10", "11-25", "12-10", "12-24")));
	}

	[Test]
	public void SemimonthlyDaysPastMonthEndUseTheLastDayOfTheMonth()
	{
		var dates = _generator.PayDates(new PayScheduleSettings(PayFrequency.Semimonthly, 15, 31), 2026);

		Assert.That(dates.Where(d => d.Day >= 27).Take(4), Is.EqualTo(Dates2026("01-30", "02-27", "03-31", "04-30")));
	}

	[Test]
	public void ACheckBelongsToTheYearItIsPaidIn()
	{
		// New Year's Day 2026 is paid on 2025-12-31, and New Year's Day 2027 is paid on 2026-12-31.
		var dates = _generator.PayDates(new PayScheduleSettings(PayFrequency.Semimonthly, 1, 15), 2026);

		Assert.Multiple(() =>
		{
			Assert.That(dates, Has.Count.EqualTo(24));
			Assert.That(dates[0], Is.EqualTo(new DateOnly(2026, 1, 15)));
			Assert.That(dates[^1], Is.EqualTo(new DateOnly(2026, 12, 31)));
		});
	}

	[Test]
	public void BiweeklyPaydaysCountFromTheAnchor()
	{
		var dates = _generator.PayDates(new PayScheduleSettings(PayFrequency.Biweekly, BiweeklyAnchorDate: new DateOnly(2026, 10, 16)), 2026);

		Assert.Multiple(() =>
		{
			Assert.That(dates, Has.Count.EqualTo(26));
			Assert.That(dates.Take(3), Is.EqualTo(Dates2026("01-09", "01-23", "02-06")));
			Assert.That(dates[^1], Is.EqualTo(new DateOnly(2026, 12, 24)), "Christmas Friday is paid Thursday");
		});
	}

	[Test]
	public void SomeBiweeklyYearsHave27Paydays()
	{
		var dates = _generator.PayDates(new PayScheduleSettings(PayFrequency.Biweekly, BiweeklyAnchorDate: new DateOnly(2025, 6, 6)), 2026);

		Assert.Multiple(() =>
		{
			Assert.That(dates, Has.Count.EqualTo(27));
			Assert.That(dates[0], Is.EqualTo(new DateOnly(2026, 1, 2)));
			Assert.That(dates, Does.Contain(new DateOnly(2026, 6, 18)), "Juneteenth Friday is paid Thursday");
			Assert.That(dates, Does.Contain(new DateOnly(2026, 7, 3)), "a Saturday holiday doesn't move a Friday payday");
			Assert.That(dates[^1], Is.EqualTo(new DateOnly(2026, 12, 31)), "New Year's Day 2027 is paid in 2026");
		});
	}

	[TestCase(PayFrequency.Semimonthly, 24)]
	[TestCase(PayFrequency.Biweekly, 26)]
	public void AnnualizationUsesTheIrsPeriodCounts(PayFrequency frequency, int expected)
	{
		Assert.That(PayScheduleGenerator.PeriodsPerYear(frequency), Is.EqualTo(expected));
	}
}
