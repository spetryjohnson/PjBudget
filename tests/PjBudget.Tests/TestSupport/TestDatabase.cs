using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Moq;
using PjBudget.Shared.Database;
using PjBudget.Shared.Infrastructure;

namespace PjBudget.Tests.TestSupport;

/// <summary>
/// A private in-memory SQLite database with the real schema. The connection stays open for the lifetime of the
/// instance, so separate contexts (used to prove data round-trips through the database) see the same data.
/// </summary>
public sealed class TestDatabase : IDisposable
{
	public static readonly DateTime StartTime = new(2026, 3, 1, 12, 0, 0, DateTimeKind.Utc);

	private readonly SqliteConnection _connection;

	public Mock<ISystemClock> Clock { get; } = new();

	public TestDatabase()
	{
		SetTime(StartTime);

		_connection = new SqliteConnection("DataSource=:memory:");
		_connection.Open();

		using var db = CreateContext();
		db.Database.EnsureCreated();
	}

	public void SetTime(DateTime utcNow)
	{
		Clock.SetupGet(c => c.UtcNow).Returns(utcNow);
		Clock.SetupGet(c => c.Now).Returns(utcNow.ToLocalTime());
		Clock.SetupGet(c => c.Today).Returns(DateOnly.FromDateTime(utcNow));
	}

	public AppDbContext CreateContext()
	{
		var options = new DbContextOptionsBuilder<AppDbContext>()
			.UseSqlite(_connection)
			.AddInterceptors(new AuditingInterceptor(Clock.Object))
			.Options;

		return new AppDbContext(options);
	}

	public void Dispose() => _connection.Dispose();
}
