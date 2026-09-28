using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace PjBudget.Shared.Database;

public class AppDbContextFactory : IDesignTimeDbContextFactory<AppDbContext>
{
	public AppDbContext CreateDbContext(string[] args)
	{
		var basePath = Directory.GetCurrentDirectory();

		var config = new ConfigurationBuilder()
			.SetBasePath(basePath)
			.AddJsonFile("appsettings.json", optional: false, reloadOnChange: false)
			.Build();

		var connectionString = config.GetConnectionString("Default");

		if (connectionString != null && connectionString.Contains("Data Source="))
		{
			var parts = connectionString.Split("Data Source=");
			var relativePath = parts[1].Trim();
			var absolutePath = Path.GetFullPath(Path.Combine(basePath, relativePath));
			connectionString = $"Data Source={absolutePath}";
		}

		var optionsBuilder = new DbContextOptionsBuilder<AppDbContext>();
		optionsBuilder.UseSqlite(connectionString);

		return new AppDbContext(optionsBuilder.Options);
	}
}
