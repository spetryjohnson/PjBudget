using FastEndpoints;
using PjBudget.Features.Authentication;

namespace PjBudget.Features.TaxYears;

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
