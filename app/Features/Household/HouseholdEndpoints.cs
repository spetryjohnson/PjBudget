using FastEndpoints;
using PjBudget.Features.Authentication;
using PjBudget.Features.Scenarios;

namespace PjBudget.Features.Household;

public sealed class GetHouseholdEndpoint : EndpointWithoutRequest<HouseholdModel>
{
	private readonly HouseholdService _household;
	private readonly ICurrentScenarioAccessor _scenario;

	public GetHouseholdEndpoint(HouseholdService household, ICurrentScenarioAccessor scenario)
	{
		_household = household;
		_scenario = scenario;
	}

	public override void Configure()
	{
		Get("/api/household");
		Policies(AuthorizationPolicies.RequireUser);
	}

	public override async Task<HouseholdModel> ExecuteAsync(CancellationToken ct)
		=> await _household.GetAsync(await _scenario.GetCurrentScenarioIdAsync(ct), ct);
}

public sealed class UpdateHouseholdEndpoint : Endpoint<HouseholdModel, HouseholdModel>
{
	private readonly HouseholdService _household;
	private readonly ICurrentScenarioAccessor _scenario;

	public UpdateHouseholdEndpoint(HouseholdService household, ICurrentScenarioAccessor scenario)
	{
		_household = household;
		_scenario = scenario;
	}

	public override void Configure()
	{
		Put("/api/household");
		Policies(AuthorizationPolicies.RequireUser);
	}

	public override async Task<HouseholdModel> ExecuteAsync(HouseholdModel req, CancellationToken ct)
		=> await _household.UpdateAsync(await _scenario.GetCurrentScenarioIdAsync(ct), req, ct);
}
