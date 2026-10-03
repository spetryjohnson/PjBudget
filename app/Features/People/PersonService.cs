using FluentValidation;
using Microsoft.EntityFrameworkCore;
using PjBudget.Shared.Database;
using PjBudget.Shared.Errors;

namespace PjBudget.Features.People;

public sealed class PersonModel
{
	public int Id { get; set; }
	public Guid Version { get; set; }
	public string DisplayName { get; set; } = string.Empty;
	public DateOnly? BirthDate { get; set; }
	public int SortOrder { get; set; }
}

public sealed class PersonModelValidator : AbstractValidator<PersonModel>
{
	public PersonModelValidator()
	{
		RuleFor(x => x.DisplayName).NotEmpty().MaximumLength(100);
		RuleFor(x => x.BirthDate)
			.InclusiveBetween(new DateOnly(1900, 1, 1), new DateOnly(2100, 12, 31))
			.When(x => x.BirthDate is not null);
	}
}

public sealed class PersonService
{
	private static readonly PersonModelValidator Validator = new();

	private readonly AppDbContext _db;

	public PersonService(AppDbContext db) => _db = db;

	public async Task<IReadOnlyList<PersonModel>> ListAsync(CancellationToken ct)
	{
		var people = await _db.People.AsNoTracking().OrderBy(p => p.SortOrder).ThenBy(p => p.DisplayName).ToListAsync(ct);
		return people.Select(ToModel).ToList();
	}

	public async Task<PersonModel> CreateAsync(PersonModel model, CancellationToken ct)
	{
		await Validator.ValidateOrThrowAsync(model, ct);

		var person = new Person();
		Apply(model, person);
		_db.People.Add(person);
		await _db.SaveChangesAsync(ct);

		return ToModel(person);
	}

	public async Task<PersonModel> UpdateAsync(int id, PersonModel model, CancellationToken ct)
	{
		await Validator.ValidateOrThrowAsync(model, ct);

		var person = await FindAsync(id, ct);
		person.EnsureVersion(model.Version, $"'{person.DisplayName}'");
		Apply(model, person);
		await _db.SaveChangesAsync(ct);

		return ToModel(person);
	}

	public async Task DeleteAsync(int id, CancellationToken ct)
	{
		var person = await FindAsync(id, ct);

		if (await _db.PayrollSources.AnyAsync(s => s.PersonId == id, ct))
		{
			throw new ConflictException($"{person.DisplayName} has payroll sources (in this or another scenario), so they can't be deleted.");
		}

		_db.People.Remove(person);
		await _db.SaveChangesAsync(ct);
	}

	private async Task<Person> FindAsync(int id, CancellationToken ct)
		=> await _db.People.SingleOrDefaultAsync(p => p.Id == id, ct)
		   ?? throw new NotFoundException($"Person {id} doesn't exist.");

	private static void Apply(PersonModel model, Person person)
	{
		person.DisplayName = model.DisplayName.Trim();
		person.BirthDate = model.BirthDate;
		person.SortOrder = model.SortOrder;
	}

	private static PersonModel ToModel(Person person) => new()
	{
		Id = person.Id,
		Version = person.Version,
		DisplayName = person.DisplayName,
		BirthDate = person.BirthDate,
		SortOrder = person.SortOrder,
	};
}
