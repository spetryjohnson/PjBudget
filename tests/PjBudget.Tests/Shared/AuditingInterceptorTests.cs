using Microsoft.EntityFrameworkCore;
using PjBudget.Features.Scenarios;
using PjBudget.Tests.TestSupport;

namespace PjBudget.Tests.Shared;

public class AuditingInterceptorTests
{
	private TestDatabase _database = null!;

	[SetUp]
	public void SetUp() => _database = new TestDatabase();

	[TearDown]
	public void TearDown() => _database.Dispose();

	[Test]
	public void InsertStampsBothTimestampsAndAssignsAVersion()
	{
		using var db = _database.CreateContext();
		var scenario = new Scenario { Name = "Test" };
		db.Scenarios.Add(scenario);
		db.SaveChanges();

		Assert.Multiple(() =>
		{
			Assert.That(scenario.CreatedAt, Is.EqualTo(TestDatabase.StartTime));
			Assert.That(scenario.UpdatedAt, Is.EqualTo(TestDatabase.StartTime));
			Assert.That(scenario.Version, Is.Not.EqualTo(Guid.Empty));
		});
	}

	[Test]
	public void UpdateKeepsCreatedAtButRefreshesUpdatedAtAndVersion()
	{
		using var db = _database.CreateContext();
		var scenario = new Scenario { Name = "Test" };
		db.Scenarios.Add(scenario);
		db.SaveChanges();
		var originalVersion = scenario.Version;

		var later = TestDatabase.StartTime.AddHours(2);
		_database.SetTime(later);
		scenario.Name = "Renamed";
		db.SaveChanges();

		Assert.Multiple(() =>
		{
			Assert.That(scenario.CreatedAt, Is.EqualTo(TestDatabase.StartTime));
			Assert.That(scenario.UpdatedAt, Is.EqualTo(later));
			Assert.That(scenario.Version, Is.Not.EqualTo(originalVersion));
		});
	}

	[Test]
	public void ConcurrentUpdateOfAStaleCopyIsRejected()
	{
		int id;
		using (var db = _database.CreateContext())
		{
			var scenario = new Scenario { Name = "Test" };
			db.Scenarios.Add(scenario);
			db.SaveChanges();
			id = scenario.Id;
		}

		using var first = _database.CreateContext();
		using var second = _database.CreateContext();
		var firstCopy = first.Scenarios.Single(s => s.Id == id);
		var secondCopy = second.Scenarios.Single(s => s.Id == id);

		firstCopy.Name = "First";
		first.SaveChanges();

		secondCopy.Name = "Second";
		Assert.Throws<DbUpdateConcurrencyException>(() => second.SaveChanges());
	}
}
