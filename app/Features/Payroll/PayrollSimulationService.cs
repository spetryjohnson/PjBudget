using Microsoft.EntityFrameworkCore;
using PjBudget.Features.Household;
using PjBudget.Features.Locales;
using PjBudget.Features.Payroll.Engine;
using PjBudget.Features.TaxYears;
using PjBudget.Shared.Database;
using PjBudget.Shared.Domain;
using PjBudget.Shared.Errors;

namespace PjBudget.Features.Payroll;

public sealed record SourceSimulation(PayrollSourceModel Source, PayrollSimulation Simulation);

/// <summary>
/// Gathers everything a simulation needs (tax rules, household settings, the person and the work locale) and runs
/// the engine. Saved sources and unsaved editor state go through the same path, so the live preview always shows
/// what saving would produce.
/// </summary>
public sealed class PayrollSimulationService
{
	private readonly AppDbContext _db;
	private readonly TaxYearService _taxYears;
	private readonly PaycheckSimulator _simulator;
	private readonly PayrollSourceValidator _validator;

	public PayrollSimulationService(
		AppDbContext db, TaxYearService taxYears, PaycheckSimulator simulator, PayrollSourceValidator validator)
	{
		_db = db;
		_taxYears = taxYears;
		_simulator = simulator;
		_validator = validator;
	}

	public async Task<PayrollSimulation> SimulateSavedAsync(int scenarioId, int sourceId, int year, CancellationToken ct)
	{
		var source = await _db.PayrollSources
			.AsNoTracking()
			.Include(s => s.Deductions)
			.SingleOrDefaultAsync(s => s.Id == sourceId && s.ScenarioId == scenarioId, ct)
			?? throw new NotFoundException($"Payroll source {sourceId} doesn't exist.");

		var context = await LoadContextAsync(scenarioId, year, ct);
		return await SimulateAsync(PayrollSourceMapper.ToModel(source), context, ct);
	}

	public async Task<PayrollSimulation> PreviewAsync(int scenarioId, PayrollSourceModel model, int year, CancellationToken ct)
	{
		await _validator.ValidateAsync(model, ct);

		var context = await LoadContextAsync(scenarioId, year, ct);
		return await SimulateAsync(model, context, ct);
	}

	public async Task<IReadOnlyList<SourceSimulation>> SimulateScenarioAsync(int scenarioId, int year, CancellationToken ct)
	{
		var sources = await _db.PayrollSources
			.AsNoTracking()
			.Include(s => s.Deductions)
			.Where(s => s.ScenarioId == scenarioId)
			.OrderBy(s => s.SortOrder).ThenBy(s => s.Id)
			.ToListAsync(ct);

		if (sources.Count == 0)
		{
			return [];
		}

		var context = await LoadContextAsync(scenarioId, year, ct);
		var results = new List<SourceSimulation>();
		foreach (var source in sources)
		{
			var model = PayrollSourceMapper.ToModel(source);
			results.Add(new SourceSimulation(model, await SimulateAsync(model, context, ct)));
		}

		return results;
	}

	private sealed record SimulationContext(int Year, TaxYearParameters TaxYear, HsaCoverage HsaCoverage, HomeLocation? Home);

	private async Task<SimulationContext> LoadContextAsync(int scenarioId, int year, CancellationToken ct)
	{
		var taxYear = await _taxYears.GetParametersAsync(year, ct);
		var household = await HouseholdSeeder.EnsureProfileAsync(_db, scenarioId, ct);

		return new SimulationContext(year, taxYear, household.HsaCoverage, household.HomeLocale?.ToHomeLocation());
	}

	private async Task<PayrollSimulation> SimulateAsync(PayrollSourceModel model, SimulationContext context, CancellationToken ct)
	{
		var birthDate = await _db.People.Where(p => p.Id == model.PersonId).Select(p => p.BirthDate).SingleAsync(ct);
		var work = await _db.Locales.AsNoTracking().SingleAsync(l => l.Id == model.WorkLocaleId, ct);

		return _simulator.Simulate(new PayrollSimulationRequest(
			context.Year,
			PayrollSourceMapper.ToEngineInput(model),
			birthDate,
			context.HsaCoverage,
			work.ToWorkLocation(),
			context.Home,
			context.TaxYear));
	}
}
