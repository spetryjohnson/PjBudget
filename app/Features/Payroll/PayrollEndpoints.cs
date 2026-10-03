using FastEndpoints;
using PjBudget.Features.Authentication;
using PjBudget.Features.Payroll.Engine;
using PjBudget.Features.Payroll.Engine.States;
using PjBudget.Features.Scenarios;
using PjBudget.Framework;
using PjBudget.Shared.Domain;
using PjBudget.Shared.Infrastructure;

namespace PjBudget.Features.Payroll;

public sealed class ListPayrollSourcesEndpoint : EndpointWithoutRequest<IReadOnlyList<PayrollSourceSummaryModel>>
{
	private readonly PayrollSourceService _sources;
	private readonly ICurrentScenarioAccessor _scenario;
	private readonly ISystemClock _clock;

	public ListPayrollSourcesEndpoint(PayrollSourceService sources, ICurrentScenarioAccessor scenario, ISystemClock clock)
	{
		_sources = sources;
		_scenario = scenario;
		_clock = clock;
	}

	public override void Configure()
	{
		Get("/api/payroll-sources");
		Policies(AuthorizationPolicies.RequireUser);
	}

	public override async Task<IReadOnlyList<PayrollSourceSummaryModel>> ExecuteAsync(CancellationToken ct)
		=> await _sources.ListAsync(await _scenario.GetCurrentScenarioIdAsync(ct), Query<int?>("year", isRequired: false) ?? _clock.Today.Year, ct);
}

public sealed class GetPayrollSourceEndpoint : EndpointWithoutRequest<PayrollSourceModel>
{
	private readonly PayrollSourceService _sources;
	private readonly ICurrentScenarioAccessor _scenario;

	public GetPayrollSourceEndpoint(PayrollSourceService sources, ICurrentScenarioAccessor scenario)
	{
		_sources = sources;
		_scenario = scenario;
	}

	public override void Configure()
	{
		Get("/api/payroll-sources/{id}");
		Policies(AuthorizationPolicies.RequireUser);
	}

	public override async Task<PayrollSourceModel> ExecuteAsync(CancellationToken ct)
		=> await _sources.GetAsync(await _scenario.GetCurrentScenarioIdAsync(ct), Route<int>("id"), ct);
}

public sealed class CreatePayrollSourceEndpoint : Endpoint<PayrollSourceModel, PayrollSourceModel>
{
	private readonly PayrollSourceService _sources;
	private readonly ICurrentScenarioAccessor _scenario;

	public CreatePayrollSourceEndpoint(PayrollSourceService sources, ICurrentScenarioAccessor scenario)
	{
		_sources = sources;
		_scenario = scenario;
	}

	public override void Configure()
	{
		Post("/api/payroll-sources");
		Policies(AuthorizationPolicies.RequireUser);
	}

	public override async Task<PayrollSourceModel> ExecuteAsync(PayrollSourceModel req, CancellationToken ct)
		=> await _sources.CreateAsync(await _scenario.GetCurrentScenarioIdAsync(ct), req, ct);
}

public sealed class UpdatePayrollSourceEndpoint : Endpoint<PayrollSourceModel, PayrollSourceModel>
{
	private readonly PayrollSourceService _sources;
	private readonly ICurrentScenarioAccessor _scenario;

	public UpdatePayrollSourceEndpoint(PayrollSourceService sources, ICurrentScenarioAccessor scenario)
	{
		_sources = sources;
		_scenario = scenario;
	}

	public override void Configure()
	{
		Put("/api/payroll-sources/{id}");
		Policies(AuthorizationPolicies.RequireUser);
	}

	public override async Task<PayrollSourceModel> ExecuteAsync(PayrollSourceModel req, CancellationToken ct)
		=> await _sources.UpdateAsync(await _scenario.GetCurrentScenarioIdAsync(ct), Route<int>("id"), req, ct);
}

public sealed class DeletePayrollSourceEndpoint : EndpointWithoutRequest
{
	private readonly PayrollSourceService _sources;
	private readonly ICurrentScenarioAccessor _scenario;

	public DeletePayrollSourceEndpoint(PayrollSourceService sources, ICurrentScenarioAccessor scenario)
	{
		_sources = sources;
		_scenario = scenario;
	}

	public override void Configure()
	{
		Delete("/api/payroll-sources/{id}");
		Policies(AuthorizationPolicies.RequireUser);
	}

	public override async Task HandleAsync(CancellationToken ct)
	{
		await _sources.DeleteAsync(await _scenario.GetCurrentScenarioIdAsync(ct), Route<int>("id"), ct);
		await Send.NoContentAsync(ct);
	}
}

public sealed class GetPayrollScheduleEndpoint : EndpointWithoutRequest<PayrollSimulation>
{
	private readonly PayrollSimulationService _simulations;
	private readonly ICurrentScenarioAccessor _scenario;
	private readonly ISystemClock _clock;

	public GetPayrollScheduleEndpoint(PayrollSimulationService simulations, ICurrentScenarioAccessor scenario, ISystemClock clock)
	{
		_simulations = simulations;
		_scenario = scenario;
		_clock = clock;
	}

	public override void Configure()
	{
		Get("/api/payroll-sources/{id}/schedule");
		Policies(AuthorizationPolicies.RequireUser);
	}

	public override async Task<PayrollSimulation> ExecuteAsync(CancellationToken ct)
		=> await _simulations.SimulateSavedAsync(
			await _scenario.GetCurrentScenarioIdAsync(ct), Route<int>("id"), Query<int?>("year", isRequired: false) ?? _clock.Today.Year, ct);
}

public sealed class PreviewPayrollEndpoint : Endpoint<PayrollPreviewRequest, PayrollSimulation>
{
	private readonly PayrollSimulationService _simulations;
	private readonly ICurrentScenarioAccessor _scenario;

	public PreviewPayrollEndpoint(PayrollSimulationService simulations, ICurrentScenarioAccessor scenario)
	{
		_simulations = simulations;
		_scenario = scenario;
	}

	public override void Configure()
	{
		Post("/api/payroll/preview");
		Policies(AuthorizationPolicies.RequireUser);
	}

	public override async Task<PayrollSimulation> ExecuteAsync(PayrollPreviewRequest req, CancellationToken ct)
		=> await _simulations.PreviewAsync(await _scenario.GetCurrentScenarioIdAsync(ct), req.Source, req.Year, ct);
}

public sealed class PayrollReferenceEndpoint : EndpointWithoutRequest<PayrollReferenceModel>
{
	private readonly StateTaxModules _states;
	private readonly ISystemClock _clock;

	public PayrollReferenceEndpoint(StateTaxModules states, ISystemClock clock)
	{
		_states = states;
		_clock = clock;
	}

	public override void Configure()
	{
		Get("/api/payroll/reference");
		Policies(AuthorizationPolicies.RequireUser);
	}

	public override Task<PayrollReferenceModel> ExecuteAsync(CancellationToken ct)
		=> Task.FromResult(new PayrollReferenceModel(
			_clock.Today.Year,
			_states.SupportedStates,
			PayrollSourceModel.Template(),
			Enum.GetValues<DeductionType>().ToDictionary(t => t.ToStringConstant(), DefaultTaxTreatment.ForDeduction),
			DefaultTaxTreatment.Traditional401k,
			DefaultTaxTreatment.CafeteriaPlan,
			PayrollSourceService.MaxSourcesPerScenario));
}
