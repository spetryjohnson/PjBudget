using PjBudget.Features.Payroll.Engine;
using PjBudget.Shared.Domain;

namespace PjBudget.Tests.Features.Payroll.Engine;

public class ContributionLimitsTests
{
	private static readonly ContributionLimits Limits2026 = new(
		ElectiveDeferral: 24_500m, CatchUpAge50: 8_000m, CatchUpAge60To63: 11_250m,
		HsaSelfOnly: 4_400m, HsaFamily: 8_750m, HsaCatchUpAge55: 1_000m, HealthFsa: 3_400m);

	[TestCase(null, 24_500)]
	[TestCase(49, 24_500)]
	[TestCase(50, 32_500)]
	[TestCase(59, 32_500)]
	[TestCase(60, 35_750)]
	[TestCase(63, 35_750)]
	[TestCase(64, 32_500)]
	public void ElectiveDeferralLimitIncludesTheCatchUpForTheAgeReachedByYearEnd(int? age, decimal expected)
	{
		Assert.That(Limits2026.ElectiveDeferralLimitFor(age), Is.EqualTo(expected));
	}

	[TestCase(HsaCoverage.None, 60, 0)]
	[TestCase(HsaCoverage.SelfOnly, 54, 4_400)]
	[TestCase(HsaCoverage.SelfOnly, 55, 5_400)]
	[TestCase(HsaCoverage.Family, null, 8_750)]
	[TestCase(HsaCoverage.Family, 55, 9_750)]
	public void HsaLimitDependsOnCoverageAndCatchUpAge(HsaCoverage coverage, int? age, decimal expected)
	{
		Assert.That(Limits2026.HsaLimitFor(coverage, age), Is.EqualTo(expected));
	}
}
