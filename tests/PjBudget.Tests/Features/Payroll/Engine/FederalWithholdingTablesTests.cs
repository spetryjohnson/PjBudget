using PjBudget.Features.Payroll.Engine;
using PjBudget.Features.TaxYears;
using PjBudget.Shared.Domain;

namespace PjBudget.Tests.Features.Payroll.Engine;

/// <summary>
/// Pins the derived withholding tables to every row of the published IRS Pub 15-T (2026) Annual Percentage Method
/// tables, so a change to the derivation or to the seeded brackets can't silently drift from the IRS.
/// </summary>
public class FederalWithholdingTablesTests
{
	private static readonly TaxYearParameters Year2026 = TaxYearMapper.ToParameters(TaxYear2026.Create());

	// Each row is (column A "at least", column C "tentative amount", column D "percentage"), copied from Pub 15-T.
	private static IEnumerable<TestCaseData> PublishedTables()
	{
		yield return Table(FilingStatus.MarriedFilingJointly, checkbox: false,
			(0, 0, 0), (19_300, 0, 10), (44_100, 2_480, 12), (120_100, 11_600, 22), (230_700, 35_932, 24),
			(422_850, 82_048, 32), (531_750, 116_896, 35), (788_000, 206_583.50m, 37));
		yield return Table(FilingStatus.MarriedFilingJointly, checkbox: true,
			(0, 0, 0), (16_100, 0, 10), (28_500, 1_240, 12), (66_500, 5_800, 22), (121_800, 17_966, 24),
			(217_875, 41_024, 32), (272_325, 58_448, 35), (400_450, 103_291.75m, 37));
		yield return Table(FilingStatus.Single, checkbox: false,
			(0, 0, 0), (7_500, 0, 10), (19_900, 1_240, 12), (57_900, 5_800, 22), (113_200, 17_966, 24),
			(209_275, 41_024, 32), (263_725, 58_448, 35), (648_100, 192_979.25m, 37));
		yield return Table(FilingStatus.Single, checkbox: true,
			(0, 0, 0), (8_050, 0, 10), (14_250, 620, 12), (33_250, 2_900, 22), (60_900, 8_983, 24),
			(108_938, 20_512, 32), (136_163, 29_224, 35), (328_350, 96_489.63m, 37));
		yield return Table(FilingStatus.HeadOfHousehold, checkbox: false,
			(0, 0, 0), (15_550, 0, 10), (33_250, 1_770, 12), (83_000, 7_740, 22), (121_250, 16_155, 24),
			(217_300, 39_207, 32), (271_750, 56_631, 35), (656_150, 191_171, 37));
		yield return Table(FilingStatus.HeadOfHousehold, checkbox: true,
			(0, 0, 0), (12_075, 0, 10), (20_925, 885, 12), (45_800, 3_870, 22), (64_925, 8_077.50m, 24),
			(112_950, 19_603.50m, 32), (140_175, 28_315.50m, 35), (332_375, 95_585.50m, 37));
	}

	[TestCaseSource(nameof(PublishedTables))]
	public void DerivedTableMatchesPub15T(FilingStatus filingStatus, bool checkbox, TaxBracket[] expected)
	{
		var rules = Year2026.FederalRulesFor(filingStatus);
		var table = checkbox ? FederalWithholdingTables.Step2Checkbox(rules) : FederalWithholdingTables.Standard(rules);

		Assert.That(table.Brackets, Is.EqualTo(expected));
	}

	private static TestCaseData Table(FilingStatus status, bool checkbox, params (decimal AtLeast, decimal Base, decimal Percent)[] rows)
		=> new TestCaseData(status, checkbox, rows.Select(r => new TaxBracket(r.AtLeast, r.Base, r.Percent / 100m)).ToArray())
			.SetName($"{status} {(checkbox ? "Step 2 checkbox" : "standard")} schedule");
}
