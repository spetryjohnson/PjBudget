using PjBudget.Features.Payroll.Engine;

namespace PjBudget.Tests.Features.Payroll.Engine;

public class TaxScheduleTests
{
	// 2026 MFJ federal brackets.
	private static readonly TaxSchedule Mfj = TaxSchedule.FromMarginalRates(
	[
		(0m, 0.10m), (24_800m, 0.12m), (100_800m, 0.22m), (211_400m, 0.24m),
		(403_550m, 0.32m), (512_450m, 0.35m), (768_700m, 0.37m),
	]);

	[Test]
	public void FromMarginalRatesAccumulatesTheTaxBelowEachBracket()
	{
		Assert.That(Mfj.Brackets.Select(b => b.BaseAmount),
			Is.EqualTo(new[] { 0m, 2_480m, 11_600m, 35_932m, 82_048m, 116_896m, 206_583.50m }));
	}

	[TestCase(0, 0)]
	[TestCase(24_800, 2_480)]
	[TestCase(100_000, 11_504)]
	[TestCase(247_111.28, 44_502.7072)]
	[TestCase(1_000_000, 292_164.50)]
	public void CalculatesProgressiveTax(decimal income, decimal expected)
	{
		Assert.That(Mfj.Calculate(income), Is.EqualTo(expected));
	}

	[Test]
	public void ExclusiveLowerBoundsKeepAnAmountAtTheBoundaryInTheLowerBracket()
	{
		// Ohio 2026: no tax at or below $26,050; above it, $332 plus 2.75% of the excess.
		var ohio = new TaxSchedule([new TaxBracket(0m, 0m, 0m), new TaxBracket(26_050m, 332m, 0.0275m)]);

		Assert.Multiple(() =>
		{
			Assert.That(ohio.Calculate(26_050m), Is.EqualTo(0m));
			Assert.That(ohio.Calculate(26_050.01m), Is.EqualTo(332.000275m));
		});
	}

	[Test]
	public void InclusiveLowerBoundsMoveAnAmountAtTheBoundaryIntoTheUpperBracket()
	{
		var schedule = new TaxSchedule(
			[new TaxBracket(0m, 0m, 0.10m), new TaxBracket(100m, 50m, 0.20m)], inclusiveLowerBounds: true);

		Assert.That(schedule.FindBracket(100m).BaseAmount, Is.EqualTo(50m));
	}

	[TestCase(0, 2_350)]
	[TestCase(40_000, 2_350)]
	[TestCase(40_000.01, 2_100)]
	[TestCase(80_001, 1_850)]
	public void TierLookupsUseTheFirstTierForAmountsAtItsLowerBound(decimal magi, decimal expectedExemption)
	{
		var exemptions = new TaxSchedule(
			[new TaxBracket(0m, 2_350m, 0m), new TaxBracket(40_000m, 2_100m, 0m), new TaxBracket(80_000m, 1_850m, 0m)]);

		Assert.That(exemptions.FindBracket(magi).BaseAmount, Is.EqualTo(expectedExemption));
	}
}
