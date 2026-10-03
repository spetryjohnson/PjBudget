using FastEndpoints;
using Microsoft.EntityFrameworkCore;
using PjBudget.Features.Authentication;
using PjBudget.Features.Locales;
using PjBudget.Features.Payroll;
using PjBudget.Features.Payroll.Engine;
using PjBudget.Features.Scenarios;
using PjBudget.Features.TaxYears;
using PjBudget.Shared.Database;
using PjBudget.Shared.Infrastructure;

namespace PjBudget.Features.Household;

public sealed class HouseholdTaxProjectionService
{
	private readonly AppDbContext _db;
	private readonly PayrollSimulationService _simulations;
	private readonly TaxYearService _taxYears;
	private readonly HouseholdTaxProjector _projector;

	public HouseholdTaxProjectionService(
		AppDbContext db, PayrollSimulationService simulations, TaxYearService taxYears, HouseholdTaxProjector projector)
	{
		_db = db;
		_simulations = simulations;
		_taxYears = taxYears;
		_projector = projector;
	}

	public async Task<HouseholdTaxProjection> ProjectAsync(int scenarioId, int year, CancellationToken ct)
	{
		var simulations = await _simulations.SimulateScenarioAsync(scenarioId, year, ct);
		var taxYear = await _taxYears.GetParametersAsync(year, ct);
		var profile = await HouseholdSeeder.EnsureProfileAsync(_db, scenarioId, ct);
		var people = await _db.People.AsNoTracking().ToListAsync(ct);
		var locales = await _db.Locales.AsNoTracking().ToDictionaryAsync(l => l.Id, ct);

		var sources = simulations
			.Select(s => new SourceYear(
				s.Source.PersonId,
				s.Source.Name,
				locales[s.Source.WorkLocaleId].ToWorkLocation(),
				s.Simulation.Summary.AnnualTotals,
				s.Source.GroupTermLifeTaxedFor))
			.ToList();

		var inputs = new HouseholdTaxInputs(
			profile.TaxFilingStatus,
			profile.FederalOtherIncome,
			profile.FederalAdjustments,
			profile.FederalItemizedDeductions,
			profile.FederalCredits,
			profile.OhioAdjustments,
			profile.OhioExemptionCount,
			profile.OhioOtherCredits);

		return _projector.Project(new HouseholdProjectionRequest(
			year,
			taxYear,
			inputs,
			profile.HomeLocale?.ToHomeLocation(),
			profile.HsaCoverage,
			people.Select(p => new PersonInfo(p.Id, p.DisplayName, p.BirthDate)).ToList(),
			sources));
	}
}

public sealed class GetHouseholdTaxProjectionEndpoint : EndpointWithoutRequest<HouseholdTaxProjection>
{
	private readonly HouseholdTaxProjectionService _projections;
	private readonly ICurrentScenarioAccessor _scenario;
	private readonly ISystemClock _clock;

	public GetHouseholdTaxProjectionEndpoint(
		HouseholdTaxProjectionService projections, ICurrentScenarioAccessor scenario, ISystemClock clock)
	{
		_projections = projections;
		_scenario = scenario;
		_clock = clock;
	}

	public override void Configure()
	{
		Get("/api/household/projection");
		Policies(AuthorizationPolicies.RequireUser);
	}

	public override async Task<HouseholdTaxProjection> ExecuteAsync(CancellationToken ct)
		=> await _projections.ProjectAsync(
			await _scenario.GetCurrentScenarioIdAsync(ct), Query<int?>("year", isRequired: false) ?? _clock.Today.Year, ct);
}
