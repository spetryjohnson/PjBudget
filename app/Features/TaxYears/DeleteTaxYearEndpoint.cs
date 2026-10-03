using FastEndpoints;
using PjBudget.Features.Authentication;

namespace PjBudget.Features.TaxYears;

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
