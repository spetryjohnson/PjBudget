using FastEndpoints;
using PjBudget.Features.Authentication;

namespace PjBudget.Features.TaxYears;

public sealed class ListTaxYearsEndpoint : EndpointWithoutRequest<IReadOnlyList<TaxYearSummaryModel>>
{
	private readonly TaxYearService _taxYears;

	public ListTaxYearsEndpoint(TaxYearService taxYears) => _taxYears = taxYears;

	public override void Configure()
	{
		Get("/api/tax-years");
		Policies(AuthorizationPolicies.RequireUser);
	}

	public override Task<IReadOnlyList<TaxYearSummaryModel>> ExecuteAsync(CancellationToken ct)
		=> _taxYears.ListAsync(ct);
}

public sealed class TaxYearRequest
{
	public int Year { get; set; }
}

public sealed class GetTaxYearEndpoint : Endpoint<TaxYearRequest, TaxYearModel>
{
	private readonly TaxYearService _taxYears;

	public GetTaxYearEndpoint(TaxYearService taxYears) => _taxYears = taxYears;

	public override void Configure()
	{
		Get("/api/tax-years/{year}");
		Policies(AuthorizationPolicies.RequireUser);
	}

	public override Task<TaxYearModel> ExecuteAsync(TaxYearRequest req, CancellationToken ct)
		=> _taxYears.GetAsync(req.Year, ct);
}

public sealed class UpdateTaxYearEndpoint : Endpoint<TaxYearModel, TaxYearModel>
{
	private readonly TaxYearService _taxYears;

	public UpdateTaxYearEndpoint(TaxYearService taxYears) => _taxYears = taxYears;

	public override void Configure()
	{
		Put("/api/tax-years/{year}");
		Policies(AuthorizationPolicies.RequireUser);
	}

	public override Task<TaxYearModel> ExecuteAsync(TaxYearModel req, CancellationToken ct)
		=> _taxYears.UpdateAsync(Route<int>("year"), req, ct);
}

public sealed class CopyTaxYearRequest
{
	public int Year { get; set; }
	public int TargetYear { get; set; }
}

public sealed class CopyTaxYearEndpoint : Endpoint<CopyTaxYearRequest, TaxYearModel>
{
	private readonly TaxYearService _taxYears;

	public CopyTaxYearEndpoint(TaxYearService taxYears) => _taxYears = taxYears;

	public override void Configure()
	{
		Post("/api/tax-years/{year}/copy");
		Policies(AuthorizationPolicies.RequireUser);
	}

	public override Task<TaxYearModel> ExecuteAsync(CopyTaxYearRequest req, CancellationToken ct)
		=> _taxYears.CopyAsync(req.Year, req.TargetYear, ct);
}

public sealed class DeleteTaxYearEndpoint : Endpoint<TaxYearRequest>
{
	private readonly TaxYearService _taxYears;

	public DeleteTaxYearEndpoint(TaxYearService taxYears) => _taxYears = taxYears;

	public override void Configure()
	{
		Delete("/api/tax-years/{year}");
		Policies(AuthorizationPolicies.RequireUser);
	}

	public override async Task HandleAsync(TaxYearRequest req, CancellationToken ct)
	{
		await _taxYears.DeleteAsync(req.Year, ct);
		await Send.NoContentAsync(ct);
	}
}
