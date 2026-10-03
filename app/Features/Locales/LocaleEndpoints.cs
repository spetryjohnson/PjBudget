using FastEndpoints;
using PjBudget.Features.Authentication;

namespace PjBudget.Features.Locales;

public sealed class ListLocalesEndpoint : EndpointWithoutRequest<IReadOnlyList<LocaleModel>>
{
	private readonly LocaleService _locales;

	public ListLocalesEndpoint(LocaleService locales) => _locales = locales;

	public override void Configure()
	{
		Get("/api/locales");
		Policies(AuthorizationPolicies.RequireUser);
	}

	public override Task<IReadOnlyList<LocaleModel>> ExecuteAsync(CancellationToken ct) => _locales.ListAsync(ct);
}

public sealed class CreateLocaleEndpoint : Endpoint<LocaleModel, LocaleModel>
{
	private readonly LocaleService _locales;

	public CreateLocaleEndpoint(LocaleService locales) => _locales = locales;

	public override void Configure()
	{
		Post("/api/locales");
		Policies(AuthorizationPolicies.RequireUser);
	}

	public override Task<LocaleModel> ExecuteAsync(LocaleModel req, CancellationToken ct) => _locales.CreateAsync(req, ct);
}

public sealed class UpdateLocaleEndpoint : Endpoint<LocaleModel, LocaleModel>
{
	private readonly LocaleService _locales;

	public UpdateLocaleEndpoint(LocaleService locales) => _locales = locales;

	public override void Configure()
	{
		Put("/api/locales/{id}");
		Policies(AuthorizationPolicies.RequireUser);
	}

	public override Task<LocaleModel> ExecuteAsync(LocaleModel req, CancellationToken ct)
		=> _locales.UpdateAsync(Route<int>("id"), req, ct);
}

public sealed class DeleteLocaleEndpoint : EndpointWithoutRequest
{
	private readonly LocaleService _locales;

	public DeleteLocaleEndpoint(LocaleService locales) => _locales = locales;

	public override void Configure()
	{
		Delete("/api/locales/{id}");
		Policies(AuthorizationPolicies.RequireUser);
	}

	public override async Task HandleAsync(CancellationToken ct)
	{
		await _locales.DeleteAsync(Route<int>("id"), ct);
		await Send.NoContentAsync(ct);
	}
}
