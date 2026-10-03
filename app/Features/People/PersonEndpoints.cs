using FastEndpoints;
using PjBudget.Features.Authentication;

namespace PjBudget.Features.People;

public sealed class ListPeopleEndpoint : EndpointWithoutRequest<IReadOnlyList<PersonModel>>
{
	private readonly PersonService _people;

	public ListPeopleEndpoint(PersonService people) => _people = people;

	public override void Configure()
	{
		Get("/api/people");
		Policies(AuthorizationPolicies.RequireUser);
	}

	public override Task<IReadOnlyList<PersonModel>> ExecuteAsync(CancellationToken ct) => _people.ListAsync(ct);
}

public sealed class CreatePersonEndpoint : Endpoint<PersonModel, PersonModel>
{
	private readonly PersonService _people;

	public CreatePersonEndpoint(PersonService people) => _people = people;

	public override void Configure()
	{
		Post("/api/people");
		Policies(AuthorizationPolicies.RequireUser);
	}

	public override Task<PersonModel> ExecuteAsync(PersonModel req, CancellationToken ct) => _people.CreateAsync(req, ct);
}

public sealed class UpdatePersonEndpoint : Endpoint<PersonModel, PersonModel>
{
	private readonly PersonService _people;

	public UpdatePersonEndpoint(PersonService people) => _people = people;

	public override void Configure()
	{
		Put("/api/people/{id}");
		Policies(AuthorizationPolicies.RequireUser);
	}

	public override Task<PersonModel> ExecuteAsync(PersonModel req, CancellationToken ct)
		=> _people.UpdateAsync(Route<int>("id"), req, ct);
}

public sealed class DeletePersonEndpoint : EndpointWithoutRequest
{
	private readonly PersonService _people;

	public DeletePersonEndpoint(PersonService people) => _people = people;

	public override void Configure()
	{
		Delete("/api/people/{id}");
		Policies(AuthorizationPolicies.RequireUser);
	}

	public override async Task HandleAsync(CancellationToken ct)
	{
		await _people.DeleteAsync(Route<int>("id"), ct);
		await Send.NoContentAsync(ct);
	}
}
