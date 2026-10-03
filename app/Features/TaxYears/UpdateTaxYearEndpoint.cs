using FastEndpoints;
using PjBudget.Features.Authentication;

namespace PjBudget.Features.TaxYears;

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
