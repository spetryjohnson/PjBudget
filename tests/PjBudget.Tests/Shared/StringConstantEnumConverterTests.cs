using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using PjBudget.Framework;
using PjBudget.Shared.Database;

namespace PjBudget.Tests.Shared;

public class StringConstantEnumConverterTests
{
	public enum Color
	{
		[StringConstant("RED")] Red,
		[StringConstant("GREEN")] Green,
	}

	[Flags]
	public enum Access
	{
		[StringConstant("NONE")] None = 0,
		[StringConstant("R")] Read = 1,
		[StringConstant("W")] Write = 2,
		[StringConstant("X")] Execute = 4,
	}

	public class Widget
	{
		public int Id { get; set; }
		public Color Color { get; set; }
		public Color? OptionalColor { get; set; }
		public Access Access { get; set; }
	}

	private sealed class WidgetContext(DbContextOptions options) : DbContext(options)
	{
		public DbSet<Widget> Widgets => Set<Widget>();

		protected override void OnModelCreating(ModelBuilder b)
		{
			b.Entity<Widget>();
			b.UseStringConstantsForEnums();
		}
	}

	private SqliteConnection _connection = null!;

	[SetUp]
	public void SetUp()
	{
		_connection = new SqliteConnection("DataSource=:memory:");
		_connection.Open();
		using var db = CreateContext();
		db.Database.EnsureCreated();
	}

	[TearDown]
	public void TearDown() => _connection.Dispose();

	private WidgetContext CreateContext()
		=> new(new DbContextOptionsBuilder().UseSqlite(_connection).Options);

	[Test]
	public void EnumsAreStoredAsTheirStringConstants()
	{
		using (var db = CreateContext())
		{
			db.Widgets.Add(new Widget { Color = Color.Green, OptionalColor = null, Access = Access.Read | Access.Execute });
			db.SaveChanges();
		}

		using var command = _connection.CreateCommand();
		command.CommandText = "SELECT Color, OptionalColor, Access FROM Widgets";
		using var reader = command.ExecuteReader();
		Assert.That(reader.Read(), Is.True);
		Assert.Multiple(() =>
		{
			Assert.That(reader.GetString(0), Is.EqualTo("GREEN"));
			Assert.That(reader.IsDBNull(1), Is.True);
			Assert.That(reader.GetString(2), Is.EqualTo("R|X"));
		});
	}

	[Test]
	public void EnumsRoundTripAndCanBeQueried()
	{
		using (var db = CreateContext())
		{
			db.Widgets.Add(new Widget { Color = Color.Red, OptionalColor = Color.Green, Access = Access.None });
			db.Widgets.Add(new Widget { Color = Color.Green, Access = Access.Write });
			db.SaveChanges();
		}

		using (var db = CreateContext())
		{
			var red = db.Widgets.Single(w => w.Color == Color.Red);
			Assert.Multiple(() =>
			{
				Assert.That(red.OptionalColor, Is.EqualTo(Color.Green));
				Assert.That(red.Access, Is.EqualTo(Access.None));
				Assert.That(db.Widgets.Single(w => w.Color == Color.Green).Access, Is.EqualTo(Access.Write));
			});
		}
	}
}
