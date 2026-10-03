using Microsoft.EntityFrameworkCore;
using PjBudget.Features.Payroll.Engine;
using PjBudget.Shared.Database;
using PjBudget.Shared.Errors;

namespace PjBudget.Features.TaxYears;

public sealed class TaxYearService
{
	private static readonly TaxYearModelValidator Validator = new();

	private readonly AppDbContext _db;

	public TaxYearService(AppDbContext db) => _db = db;

	public async Task<IReadOnlyList<TaxYearSummaryModel>> ListAsync(CancellationToken ct)
		=> await _db.TaxYears
			.OrderBy(t => t.Year)
			.Select(t => new TaxYearSummaryModel { Year = t.Year, UpdatedAt = t.UpdatedAt })
			.ToListAsync(ct);

	public async Task<TaxYearModel> GetAsync(int year, CancellationToken ct)
		=> TaxYearMapper.ToModel(await LoadAsync(year, tracked: false, ct));

	/// <summary>
	/// Loads the rules the payroll engine runs on. Missing years are reported as a validation error rather than "not
	/// found", because the caller asked to simulate a year, not to fetch a tax year.
	/// </summary>
	public async Task<TaxYearParameters> GetParametersAsync(int year, CancellationToken ct)
	{
		var entity = await FindAsync(year, tracked: false, ct)
			?? throw new DomainValidationException("year",
				$"There are no tax rules for {year}. Copy an existing year under Settings > Tax years to create them.");

		return TaxYearMapper.ToParameters(entity);
	}

	public async Task<IReadOnlyList<WithholdingTablesModel>> GetWithholdingTablesAsync(int year, CancellationToken ct)
	{
		var parameters = TaxYearMapper.ToParameters(await LoadAsync(year, tracked: false, ct));

		return parameters.Federal
			.OrderBy(f => f.Key)
			.Select(f => new WithholdingTablesModel(
				f.Key,
				Rows(FederalWithholdingTables.Standard(f.Value)),
				Rows(FederalWithholdingTables.Step2Checkbox(f.Value))))
			.ToList();

		static IReadOnlyList<WithholdingTableRowModel> Rows(TaxSchedule table)
			=> table.Brackets.Select(b => new WithholdingTableRowModel(b.Over, b.BaseAmount, b.Rate)).ToList();
	}

	public async Task<TaxYearModel> UpdateAsync(int year, TaxYearModel model, CancellationToken ct)
	{
		model.Year = year;
		await Validator.ValidateOrThrowAsync(model, ct);

		var entity = await LoadAsync(year, tracked: true, ct);
		entity.EnsureVersion(model.Version, $"Tax year {year}");

		TaxYearMapper.Apply(model, entity);
		_db.TouchRoot(entity);
		await _db.SaveChangesAsync(ct);

		return TaxYearMapper.ToModel(entity);
	}

	public async Task<TaxYearModel> CopyAsync(int fromYear, int toYear, CancellationToken ct)
	{
		if (await _db.TaxYears.AnyAsync(t => t.Year == toYear, ct))
		{
			throw new ConflictException($"Tax year {toYear} already exists.");
		}

		var model = await GetAsync(fromYear, ct);
		model.Year = toYear;
		await Validator.ValidateOrThrowAsync(model, ct);

		var copy = new TaxYear();
		TaxYearMapper.Apply(model, copy);
		_db.TaxYears.Add(copy);
		await _db.SaveChangesAsync(ct);

		return TaxYearMapper.ToModel(copy);
	}

	public async Task DeleteAsync(int year, CancellationToken ct)
	{
		var entity = await LoadAsync(year, tracked: true, ct);
		_db.Remove(entity);
		await _db.SaveChangesAsync(ct);
	}

	private async Task<TaxYear> LoadAsync(int year, bool tracked, CancellationToken ct)
		=> await FindAsync(year, tracked, ct) ?? throw new NotFoundException($"Tax year {year} doesn't exist.");

	private Task<TaxYear?> FindAsync(int year, bool tracked, CancellationToken ct)
	{
		var query = _db.TaxYears
			.Include(t => t.FilingStatuses)
			.Include(t => t.ScheduleRows)
			.AsSplitQuery();

		return (tracked ? query : query.AsNoTracking()).SingleOrDefaultAsync(t => t.Year == year, ct);
	}
}
