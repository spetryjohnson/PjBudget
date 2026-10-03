using PjBudget.Features.Scenarios;
using PjBudget.Tests.TestSupport;

namespace PjBudget.Tests.Features.Scenarios;

public class ScenarioSeederTests
{
	[Test]
	public async Task CreatesOneCurrentScenarioAndIsIdempotent()
	{
		using var database = new TestDatabase();

		using (var db = database.CreateContext())
		{
			await ScenarioSeeder.EnsureCurrentScenarioAsync(db);
		}

		using (var db = database.CreateContext())
		{
			var again = await ScenarioSeeder.EnsureCurrentScenarioAsync(db);

			Assert.Multiple(() =>
			{
				Assert.That(db.Scenarios.Count(), Is.EqualTo(1));
				Assert.That(again.IsCurrent, Is.True);
				Assert.That(again.Name, Is.EqualTo(ScenarioSeeder.DefaultScenarioName));
			});
		}
	}

	[Test]
	public async Task CurrentScenarioAccessorReturnsTheCurrentScenario()
	{
		using var database = new TestDatabase();
		using var db = database.CreateContext();
		db.Scenarios.Add(new Scenario { Name = "What-if" });
		var current = await ScenarioSeeder.EnsureCurrentScenarioAsync(db);

		var accessor = new CurrentScenarioAccessor(db);

		Assert.That(await accessor.GetCurrentScenarioIdAsync(CancellationToken.None), Is.EqualTo(current.Id));
	}
}
