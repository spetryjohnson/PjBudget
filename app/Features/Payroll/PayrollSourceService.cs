using Microsoft.EntityFrameworkCore;
using PjBudget.Shared.Database;
using PjBudget.Shared.Errors;

namespace PjBudget.Features.Payroll;

public sealed class PayrollSourceService
{
	/// <summary>The budget matrix lays out every check of every source as columns, which stays usable up to four sources.</summary>
	public const int MaxSourcesPerScenario = 4;

	private readonly AppDbContext _db;
	private readonly PayrollSourceValidator _validator;
	private readonly PayrollSimulationService _simulations;

	public PayrollSourceService(AppDbContext db, PayrollSourceValidator validator, PayrollSimulationService simulations)
	{
		_db = db;
		_validator = validator;
		_simulations = simulations;
	}

	public async Task<IReadOnlyList<PayrollSourceSummaryModel>> ListAsync(int scenarioId, int year, CancellationToken ct)
	{
		var summaries = await _db.PayrollSources
			.AsNoTracking()
			.Where(s => s.ScenarioId == scenarioId)
			.OrderBy(s => s.SortOrder).ThenBy(s => s.Id)
			.Select(s => new PayrollSourceSummaryModel
			{
				Id = s.Id,
				Name = s.Name,
				EmployerName = s.EmployerName,
				PersonId = s.PersonId,
				PersonName = s.Person!.DisplayName,
				PayFrequency = s.PayFrequency,
				SortOrder = s.SortOrder,
			})
			.ToListAsync(ct);

		try
		{
			var simulations = (await _simulations.SimulateScenarioAsync(scenarioId, year, ct))
				.ToDictionary(s => s.Source.Id, s => s.Simulation);

			foreach (var summary in summaries)
			{
				var simulation = simulations[summary.Id];
				summary.Headline = new PayrollSourceHeadline(
					year,
					simulation.Summary.CheckCount,
					simulation.Summary.AnnualTotals.GrossPay,
					simulation.Summary.AnnualTotals.NetPay,
					simulation.Summary.RegularNetPay,
					simulation.Warnings.Count);
			}
		}
		catch (DomainValidationException e)
		{
			// Typically a year with no tax rules yet; the list is still useful without the numbers.
			var message = string.Join(" ", e.Errors.Values.SelectMany(v => v));
			foreach (var summary in summaries)
			{
				summary.SimulationError = message;
			}
		}

		return summaries;
	}

	public async Task<PayrollSourceModel> GetAsync(int scenarioId, int id, CancellationToken ct)
		=> PayrollSourceMapper.ToModel(await LoadAsync(scenarioId, id, tracked: false, ct));

	public async Task<PayrollSourceModel> CreateAsync(int scenarioId, PayrollSourceModel model, CancellationToken ct)
	{
		await _validator.ValidateAsync(model, ct);

		if (await _db.PayrollSources.CountAsync(s => s.ScenarioId == scenarioId, ct) >= MaxSourcesPerScenario)
		{
			throw new DomainValidationException("", $"A plan can have at most {MaxSourcesPerScenario} payroll sources.");
		}

		var source = new PayrollSource { ScenarioId = scenarioId };
		PayrollSourceMapper.Apply(model, source);
		_db.PayrollSources.Add(source);
		await _db.SaveChangesAsync(ct);

		return PayrollSourceMapper.ToModel(source);
	}

	public async Task<PayrollSourceModel> UpdateAsync(int scenarioId, int id, PayrollSourceModel model, CancellationToken ct)
	{
		await _validator.ValidateAsync(model, ct);

		var source = await LoadAsync(scenarioId, id, tracked: true, ct);
		source.EnsureVersion(model.Version, $"'{source.Name}'");

		PayrollSourceMapper.Apply(model, source);
		_db.TouchRoot(source);
		await _db.SaveChangesAsync(ct);

		return PayrollSourceMapper.ToModel(source);
	}

	public async Task DeleteAsync(int scenarioId, int id, CancellationToken ct)
	{
		var source = await LoadAsync(scenarioId, id, tracked: true, ct);
		_db.PayrollSources.Remove(source);
		await _db.SaveChangesAsync(ct);
	}

	private async Task<PayrollSource> LoadAsync(int scenarioId, int id, bool tracked, CancellationToken ct)
	{
		var query = _db.PayrollSources.Include(s => s.Deductions).Where(s => s.Id == id && s.ScenarioId == scenarioId);

		return await (tracked ? query : query.AsNoTracking()).SingleOrDefaultAsync(ct)
		       ?? throw new NotFoundException($"Payroll source {id} doesn't exist.");
	}
}
