namespace PjBudget.Features.Payroll.Engine.States;

/// <summary>
/// One state's withholding rules, covering both its state tax and the local taxes (city, school district) it
/// authorizes. Local taxes live with the state because their rules are set by state law and often depend on state
/// figures. For example, Ohio's traditional-base school districts withhold on the state wage base.
/// </summary>
public interface IStateTaxModule
{
	/// <summary>Two-letter postal code, e.g. "OH".</summary>
	string StateCode { get; }

	StateAndLocalWithholding CalculateWithholding(StateWithholdingContext context, TaxableWages wages);

	/// <summary>
	/// The household's annual state and local liability compared with withholding, one section per tax.
	/// </summary>
	IReadOnlyList<TaxProjectionSection> ProjectAnnualTaxes(StateProjectionContext context);
}

public sealed record StateProjectionContext(
	TaxYearParameters TaxYear,
	HouseholdTaxInputs Inputs,
	HomeLocation? Home,
	decimal FederalAdjustedGrossIncome,
	IReadOnlyList<SourceYear> Sources);

public sealed record StateWithholdingContext(
	int PeriodsPerYear,
	StateWithholdingElections Elections,
	WorkLocation Work,
	HomeLocation? Home,
	TaxYearParameters TaxYear);

public readonly record struct StateAndLocalWithholding(decimal State, decimal City, decimal SchoolDistrict);

public sealed class StateTaxModules
{
	private readonly Dictionary<string, IStateTaxModule> _modules;

	public StateTaxModules(IEnumerable<IStateTaxModule> modules)
		=> _modules = modules.ToDictionary(m => m.StateCode, StringComparer.OrdinalIgnoreCase);

	public IReadOnlyCollection<string> SupportedStates => _modules.Keys;

	public bool IsSupported(string stateCode) => _modules.ContainsKey(stateCode);

	/// <remarks>Callers validate the state first, so an unsupported state here is a programming error.</remarks>
	public IStateTaxModule For(string stateCode)
		=> _modules.TryGetValue(stateCode, out var module)
			? module
			: throw new InvalidOperationException($"No tax module supports state '{stateCode}'.");
}
