using PjBudget.Features.Payroll.Engine;
using PjBudget.Features.Payroll.Engine.States;
using PjBudget.Shared.Domain;
using PjBudget.Tests.TestSupport;

namespace PjBudget.Tests.Features.Payroll.Engine;

/// <summary>
/// Projections built from synthetic annual totals, so every expected value can be worked by hand.
/// </summary>
public class HouseholdTaxProjectorTests
{
	private readonly HouseholdTaxProjector _projector = new(new StateTaxModules([new OhioTaxModule()]));

	private static readonly WorkLocation Work = new("OH", 0.02m);

	private static PaycheckLines Annual(
		decimal federalWages, decimal medicareWages, decimal federalTax, decimal stateTax, decimal cityTax, decimal schoolTax,
		decimal socialSecurityTax = 0m, decimal traditional401k = 0m, decimal hsa = 0m, decimal employerHsa = 0m) => new()
	{
		TaxableWages = new TaxableWages(federalWages, federalWages, medicareWages, medicareWages, medicareWages, federalWages),
		FederalIncomeTax = federalTax,
		StateIncomeTax = stateTax,
		CityIncomeTax = cityTax,
		SchoolDistrictTax = schoolTax,
		SocialSecurityTax = socialSecurityTax,
		Traditional401k = traditional401k,
		HsaEmployee = hsa,
		EmployerHsa = employerHsa,
	};

	private static readonly SourceYear PatJob = new(1, "Pat's job", Work, Annual(100_000m, 110_000m, 12_000m, 3_000m, 2_200m, 1_000m));
	private static readonly SourceYear SamJob = new(2, "Sam's job", Work, Annual(60_000m, 65_000m, 5_000m, 1_500m, 1_300m, 600m));

	private static HouseholdProjectionRequest Request(
		IReadOnlyList<SourceYear> sources,
		decimal credits = 4_400m,
		decimal? itemized = null,
		HomeLocation? home = null,
		bool noHome = false,
		HsaCoverage hsaCoverage = HsaCoverage.Family)
		=> new(
			2026,
			PayrollTestData.TaxYear2026Parameters,
			new HouseholdTaxInputs(FilingStatus.MarriedFilingJointly, 0m, 0m, itemized, credits, 0m, 2, 0m),
			noHome ? null : home ?? PayrollTestData.HomeWithEarnedIncomeSchoolTax1Percent,
			hsaCoverage,
			[new PersonInfo(1, "Pat", null), new PersonInfo(2, "Sam", null)],
			sources);

	private static TaxProjectionSection Section(HouseholdTaxProjection projection, string title)
		=> projection.Sections.Single(s => s.Title == title);

	[Test]
	public void FederalUsesCombinedWagesAndTheMarriedBrackets()
	{
		var federal = Section(_projector.Project(Request([PatJob, SamJob])), "Federal");

		// 160,000 − 32,200 = 127,800 → 11,600 + 22% × 27,000 = 17,540, less 4,400 of credits
		Assert.Multiple(() =>
		{
			Assert.That(federal.Lines.Single(l => l.Label == "Taxable income").Amount, Is.EqualTo(127_800m));
			Assert.That(federal.Lines.Single(l => l.Label == "Income tax").Amount, Is.EqualTo(17_540m));
			Assert.That(federal.Liability, Is.EqualTo(13_140m));
			Assert.That(federal.Withheld, Is.EqualTo(17_000m));
			Assert.That(federal.RefundOrBalanceDue, Is.EqualTo(3_860m));
		});
	}

	[Test]
	public void OhioAppliesExemptionsTheBaseAmountAndTheJointFilingCredit()
	{
		var ohio = Section(_projector.Project(Request([PatJob, SamJob])), "Ohio");

		// 160,000 − 2 × 1,850 = 156,300 → 332 + 2.75% × 130,250 = 3,913.88; joint filing credit 5% = 195.69
		Assert.Multiple(() =>
		{
			Assert.That(ohio.Lines.Single(l => l.Label == "Ohio taxable income").Amount, Is.EqualTo(156_300m));
			Assert.That(ohio.Lines.Single(l => l.Label == "Income tax").Amount, Is.EqualTo(3_913.88m));
			Assert.That(ohio.Lines.Single(l => l.Label == "Joint filing credit").Amount, Is.EqualTo(-195.69m));
			Assert.That(ohio.Liability, Is.EqualTo(3_718.19m));
			Assert.That(ohio.Withheld, Is.EqualTo(4_500m));
		});
	}

	[Test]
	public void LocalTaxesAndTotals()
	{
		var projection = _projector.Project(Request([PatJob, SamJob]));

		Assert.Multiple(() =>
		{
			Assert.That(projection.Sections.Select(s => s.Title), Is.EqualTo(new[] { "Federal", "Ohio", "School district", "City" }));
			Assert.That(Section(projection, "School district").Liability, Is.EqualTo(1_600m), "1% of 160,000 earned income");
			Assert.That(Section(projection, "City").Liability, Is.EqualTo(3_500m), "settled by work-city withholding");
			Assert.That(projection.TotalRefundOrBalanceDue, Is.EqualTo(4_641.81m));
			Assert.That(projection.Warnings, Is.Empty);
		});
	}

	[Test]
	public void ItemizedDeductionsAreUsedWhenLarger()
	{
		var federal = Section(_projector.Project(Request([PatJob, SamJob], itemized: 40_000m)), "Federal");

		Assert.Multiple(() =>
		{
			Assert.That(federal.Lines.Single(l => l.Label == "Itemized deductions").Amount, Is.EqualTo(-40_000m));
			Assert.That(federal.Lines.Single(l => l.Label == "Income tax").Amount, Is.EqualTo(15_824m));
		});
	}

	[Test]
	public void AdditionalMedicareIsOwedOnCombinedWagesAbove250kEvenWhenNoneWasWithheld()
	{
		var pat = PatJob with { Annual = Annual(190_000m, 200_000m, 30_000m, 6_000m, 4_000m, 1_900m) };
		var sam = SamJob with { Annual = Annual(95_000m, 100_000m, 10_000m, 2_500m, 2_000m, 950m) };

		var federal = Section(_projector.Project(Request([pat, sam])), "Federal");

		Assert.That(federal.Lines.Single(l => l.Label == "Additional Medicare tax").Amount, Is.EqualTo(450m));
	}

	[Test]
	public void ExcessSocialSecurityFromTwoJobsComesBackAsACredit()
	{
		var first = new SourceYear(1, "Old job", Work, Annual(100_000m, 100_000m, 12_000m, 3_000m, 2_000m, 1_000m, socialSecurityTax: 8_000m));
		var second = new SourceYear(1, "New job", Work, Annual(60_000m, 60_000m, 5_000m, 1_500m, 1_200m, 600m, socialSecurityTax: 6_000m));

		var federal = Section(_projector.Project(Request([first, second])), "Federal");

		Assert.Multiple(() =>
		{
			Assert.That(federal.Lines.Single(l => l.Label.StartsWith("Excess Social Security")).Amount, Is.EqualTo(2_561m));
			Assert.That(federal.Withheld, Is.EqualTo(17_000m + 2_561m));
		});
	}

	[Test]
	public void TheJointFilingCreditNeedsTwoEarners()
	{
		var ohio = Section(_projector.Project(Request([PatJob])), "Ohio");

		Assert.That(ohio.Lines.Single(l => l.Label == "Joint filing credit").Amount, Is.EqualTo(0m));
	}

	[Test]
	public void HighIncomesLoseExemptionsAndTheJointFilingCredit()
	{
		var pat = PatJob with { Annual = Annual(550_000m, 550_000m, 150_000m, 18_000m, 11_000m, 5_500m) };

		var ohio = Section(_projector.Project(Request([pat, SamJob])), "Ohio");

		Assert.Multiple(() =>
		{
			Assert.That(ohio.Lines.Single(l => l.Label.StartsWith("Exemptions")).Amount, Is.EqualTo(0m));
			Assert.That(ohio.Lines.Single(l => l.Label == "Joint filing credit").Amount, Is.EqualTo(0m));
		});
	}

	[Test]
	public void TraditionalSchoolDistrictsTaxOhioTaxableIncome()
	{
		var home = new HomeLocation("OH", 0.02m, 0.01m, SchoolDistrictTaxBase.Traditional);

		var school = Section(_projector.Project(Request([PatJob, SamJob], home: home)), "School district");

		Assert.That(school.Liability, Is.EqualTo(1_563m));
	}

	[Test]
	public void SchoolDistrictTaxThatIsntWithheldIsStillOwed()
	{
		var notWithheld = SamJob with { Annual = SamJob.Annual with { SchoolDistrictTax = 0m, SchoolDistrictTaxNotWithheld = 600m } };

		var school = Section(_projector.Project(Request([PatJob, notWithheld])), "School district");

		// 1% of 160,000 of earned income is owed, but only Pat's job withheld any of it.
		Assert.Multiple(() =>
		{
			Assert.That(school.Liability, Is.EqualTo(1_600m));
			Assert.That(school.Withheld, Is.EqualTo(1_000m));
			Assert.That(school.RefundOrBalanceDue, Is.EqualTo(-600m));
			Assert.That(school.Note, Does.Contain("estimated payments"));
		});
	}

	[Test]
	public void TaxableLifeInsuranceIsIncomeOnTheReturnEvenWhenPayrollDoesntWithholdOnIt()
	{
		// By default, payroll leaves life insurance out of federal and school wages, but the return still counts it.
		var withLifeInsurance = PatJob with { Annual = PatJob.Annual with { GroupTermLife = 120m } };

		var projection = _projector.Project(Request([withLifeInsurance, SamJob]));

		Assert.Multiple(() =>
		{
			Assert.That(Section(projection, "Federal").Lines.Single(l => l.Label == "Wages").Amount, Is.EqualTo(160_120m));
			Assert.That(Section(projection, "School district").Lines.Single().Amount, Is.EqualTo(160_120m));
		});
	}

	[Test]
	public void LifeInsuranceAlreadyInPayrollWagesIsntCountedTwice()
	{
		var withLifeInsurance = PatJob with
		{
			Annual = PatJob.Annual with { GroupTermLife = 120m },
			GroupTermLifeTaxedFor = TaxableWageTypes.All,
		};

		var federal = Section(_projector.Project(Request([withLifeInsurance, SamJob])), "Federal");

		Assert.That(federal.Lines.Single(l => l.Label == "Wages").Amount, Is.EqualTo(160_000m));
	}

	[Test]
	public void WarnsAboutCombinedLimitsAndAMissingHome()
	{
		var first = new SourceYear(1, "Old job", Work, Annual(100_000m, 100_000m, 12_000m, 3_000m, 2_000m, 0m, traditional401k: 20_000m, hsa: 5_000m));
		var second = new SourceYear(1, "New job", Work, Annual(60_000m, 60_000m, 5_000m, 1_500m, 1_200m, 0m, traditional401k: 10_000m, employerHsa: 4_000m));

		var projection = _projector.Project(Request([first, second], noHome: true));

		Assert.Multiple(() =>
		{
			Assert.That(projection.Warnings.Select(w => w.Code),
				Is.EquivalentTo(new[] { "NO_HOME_LOCALE", "401K_COMBINED_OVER_LIMIT", "HSA_COMBINED_OVER_LIMIT" }));
			Assert.That(projection.Sections.Select(s => s.Title), Does.Contain("Ohio").And.Not.Contain("School district"),
				"without a home, the work state is projected but no school district");
		});
	}
}
