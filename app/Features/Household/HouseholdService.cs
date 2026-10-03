using FluentValidation;
using Microsoft.EntityFrameworkCore;
using PjBudget.Features.Payroll.Engine.States;
using PjBudget.Shared.Database;
using PjBudget.Shared.Domain;
using PjBudget.Shared.Errors;

namespace PjBudget.Features.Household;

public sealed class HouseholdModel
{
	public Guid Version { get; set; }
	public int? HomeLocaleId { get; set; }
	public HsaCoverage HsaCoverage { get; set; }
	public FilingStatus TaxFilingStatus { get; set; }
	public decimal FederalOtherIncome { get; set; }
	public decimal FederalAdjustments { get; set; }
	public decimal? FederalItemizedDeductions { get; set; }
	public decimal FederalCredits { get; set; }
	public decimal OhioAdjustments { get; set; }
	public int OhioExemptionCount { get; set; }
	public decimal OhioOtherCredits { get; set; }
}

public sealed class HouseholdModelValidator : AbstractValidator<HouseholdModel>
{
	public HouseholdModelValidator()
	{
		RuleFor(x => x.FederalOtherIncome).GreaterThanOrEqualTo(0);
		RuleFor(x => x.FederalAdjustments).GreaterThanOrEqualTo(0);
		RuleFor(x => x.FederalItemizedDeductions).GreaterThanOrEqualTo(0).When(x => x.FederalItemizedDeductions is not null);
		RuleFor(x => x.FederalCredits).GreaterThanOrEqualTo(0);
		RuleFor(x => x.OhioExemptionCount).InclusiveBetween(0, 20);
		RuleFor(x => x.OhioOtherCredits).GreaterThanOrEqualTo(0);
	}
}

public sealed class HouseholdService
{
	private static readonly HouseholdModelValidator Validator = new();

	private readonly AppDbContext _db;
	private readonly StateTaxModules _states;

	public HouseholdService(AppDbContext db, StateTaxModules states)
	{
		_db = db;
		_states = states;
	}

	public async Task<HouseholdModel> GetAsync(int scenarioId, CancellationToken ct)
		=> ToModel(await HouseholdSeeder.EnsureProfileAsync(_db, scenarioId, ct));

	public async Task<HouseholdModel> UpdateAsync(int scenarioId, HouseholdModel model, CancellationToken ct)
	{
		await Validator.ValidateOrThrowAsync(model, ct);

		if (model.HomeLocaleId is { } localeId)
		{
			var stateCode = await _db.Locales.Where(l => l.Id == localeId).Select(l => l.StateCode).SingleOrDefaultAsync(ct)
				?? throw new DomainValidationException("homeLocaleId", "That locale doesn't exist.");

			if (!_states.IsSupported(stateCode))
			{
				throw new DomainValidationException("homeLocaleId", $"Taxes for {stateCode} aren't supported yet.");
			}
		}

		var profile = await HouseholdSeeder.EnsureProfileAsync(_db, scenarioId, ct);
		profile.EnsureVersion(model.Version, "The household settings");

		profile.HomeLocaleId = model.HomeLocaleId;
		profile.HsaCoverage = model.HsaCoverage;
		profile.TaxFilingStatus = model.TaxFilingStatus;
		profile.FederalOtherIncome = model.FederalOtherIncome;
		profile.FederalAdjustments = model.FederalAdjustments;
		profile.FederalItemizedDeductions = model.FederalItemizedDeductions;
		profile.FederalCredits = model.FederalCredits;
		profile.OhioAdjustments = model.OhioAdjustments;
		profile.OhioExemptionCount = model.OhioExemptionCount;
		profile.OhioOtherCredits = model.OhioOtherCredits;
		await _db.SaveChangesAsync(ct);

		return ToModel(profile);
	}

	private static HouseholdModel ToModel(HouseholdProfile profile) => new()
	{
		Version = profile.Version,
		HomeLocaleId = profile.HomeLocaleId,
		HsaCoverage = profile.HsaCoverage,
		TaxFilingStatus = profile.TaxFilingStatus,
		FederalOtherIncome = profile.FederalOtherIncome,
		FederalAdjustments = profile.FederalAdjustments,
		FederalItemizedDeductions = profile.FederalItemizedDeductions,
		FederalCredits = profile.FederalCredits,
		OhioAdjustments = profile.OhioAdjustments,
		OhioExemptionCount = profile.OhioExemptionCount,
		OhioOtherCredits = profile.OhioOtherCredits,
	};
}

public static class HouseholdSeeder
{
	/// <summary>
	/// Every scenario has exactly one household profile; it is created with defaults the first time it's needed.
	/// </summary>
	public static async Task<HouseholdProfile> EnsureProfileAsync(AppDbContext db, int scenarioId, CancellationToken ct = default)
	{
		var profile = await db.HouseholdProfiles
			.Include(h => h.HomeLocale)
			.SingleOrDefaultAsync(h => h.ScenarioId == scenarioId, ct);

		if (profile is not null)
		{
			return profile;
		}

		profile = new HouseholdProfile { ScenarioId = scenarioId };
		db.HouseholdProfiles.Add(profile);
		await db.SaveChangesAsync(ct);
		return profile;
	}
}
