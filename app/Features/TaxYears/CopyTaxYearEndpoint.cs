using FastEndpoints;
using PjBudget.Features.Authentication;

namespace PjBudget.Features.TaxYears;

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
