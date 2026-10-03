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
