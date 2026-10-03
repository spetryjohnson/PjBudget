using Microsoft.EntityFrameworkCore;
using PjBudget.Features.Payroll.Engine.States;
using PjBudget.Shared.Database;
using PjBudget.Shared.Errors;

namespace PjBudget.Features.Locales;

public sealed class LocaleService
{
	private static readonly LocaleModelValidator Validator = new();

	private readonly AppDbContext _db;
	private readonly StateTaxModules _states;

	public LocaleService(AppDbContext db, StateTaxModules states)
	{
		_db = db;
		_states = states;
	}

	public async Task<IReadOnlyList<LocaleModel>> ListAsync(CancellationToken ct)
	{
		var locales = await _db.Locales.AsNoTracking().OrderBy(l => l.Name).ToListAsync(ct);
		return locales.Select(ToModel).ToList();
	}

	public async Task<LocaleModel> CreateAsync(LocaleModel model, CancellationToken ct)
	{
		await Validator.ValidateOrThrowAsync(model, ct);

		var locale = new Locale();
		Apply(model, locale);
		_db.Locales.Add(locale);
		await _db.SaveChangesAsync(ct);

		return ToModel(locale);
	}

	public async Task<LocaleModel> UpdateAsync(int id, LocaleModel model, CancellationToken ct)
	{
		await Validator.ValidateOrThrowAsync(model, ct);

		var locale = await FindAsync(id, ct);
		locale.EnsureVersion(model.Version, $"Locale '{locale.Name}'");

		// Paychecks are simulated with the tax rules of the locale's state, so a locale in use can't move to a state
		// that has none.
		if (model.StateCode != locale.StateCode && !_states.IsSupported(model.StateCode) && await IsInUseAsync(id, ct))
		{
			throw new DomainValidationException("stateCode",
				$"This locale is used by a payroll source or as the household's home, and taxes for {model.StateCode} aren't supported yet.");
		}

		Apply(model, locale);
		await _db.SaveChangesAsync(ct);

		return ToModel(locale);
	}

	public async Task DeleteAsync(int id, CancellationToken ct)
	{
		var locale = await FindAsync(id, ct);

		if (await IsInUseAsync(id, ct))
		{
			throw new ConflictException($"'{locale.Name}' is used by a payroll source or as the household's home, so it can't be deleted.");
		}

		_db.Locales.Remove(locale);
		await _db.SaveChangesAsync(ct);
	}

	private async Task<bool> IsInUseAsync(int id, CancellationToken ct)
		=> await _db.PayrollSources.AnyAsync(s => s.WorkLocaleId == id, ct)
		   || await _db.HouseholdProfiles.AnyAsync(h => h.HomeLocaleId == id, ct);

	private async Task<Locale> FindAsync(int id, CancellationToken ct)
		=> await _db.Locales.SingleOrDefaultAsync(l => l.Id == id, ct)
		   ?? throw new NotFoundException($"Locale {id} doesn't exist.");

	private static void Apply(LocaleModel model, Locale locale)
	{
		locale.Name = model.Name.Trim();
		locale.City = model.City.Trim();
		locale.StateCode = model.StateCode;
		locale.ZipCode = NullIfBlank(model.ZipCode);
		locale.MunicipalTaxRate = model.MunicipalTaxRate;
		locale.SchoolDistrictName = NullIfBlank(model.SchoolDistrictName);
		locale.SchoolDistrictNumber = NullIfBlank(model.SchoolDistrictNumber);

		// A district without a tax rate doesn't need a tax base; drop it so the record doesn't look half-configured.
		locale.SchoolDistrictTaxRate = model.SchoolDistrictTaxRate is > 0 ? model.SchoolDistrictTaxRate : null;
		locale.SchoolDistrictTaxBase = locale.SchoolDistrictTaxRate is null ? null : model.SchoolDistrictTaxBase;
	}

	internal static LocaleModel ToModel(Locale locale) => new()
	{
		Id = locale.Id,
		Version = locale.Version,
		Name = locale.Name,
		City = locale.City,
		StateCode = locale.StateCode,
		ZipCode = locale.ZipCode,
		MunicipalTaxRate = locale.MunicipalTaxRate,
		SchoolDistrictName = locale.SchoolDistrictName,
		SchoolDistrictNumber = locale.SchoolDistrictNumber,
		SchoolDistrictTaxRate = locale.SchoolDistrictTaxRate,
		SchoolDistrictTaxBase = locale.SchoolDistrictTaxBase,
	};

	private static string? NullIfBlank(string? value) => string.IsNullOrWhiteSpace(value) ? null : value.Trim();
}
