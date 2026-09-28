using FastEndpoints;
using FastEndpoints.Swagger;
using PjBudget.Features.Authentication;
using PjBudget.Features.BackgroundTasks;
using PjBudget.Features.Identity;
using PjBudget.Shared.AppStartup;
using PjBudget.Shared.Database;
using PjBudget.Shared.Identity;
using PjBudget.Shared.Infrastructure;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Serilog;

var builder = WebApplication.CreateBuilder(args);

// Logging
Log.Logger = new LoggerConfiguration()
	.ReadFrom.Configuration(builder.Configuration)
	.Enrich.FromLogContext()
	.WriteTo.Console()
	.CreateLogger();

builder.Host.UseSerilog();

// SQLite (resolve relative DataSource to an absolute path so EF tooling and runtime agree)
var connString = builder.Configuration.GetConnectionString("Default")
                 ?? throw new InvalidOperationException("Missing ConnectionStrings:Default");

var csb = new SqliteConnectionStringBuilder(connString);
if (!string.IsNullOrWhiteSpace(csb.DataSource) && csb.DataSource != ":memory:" && !Path.IsPathRooted(csb.DataSource))
{
	csb.DataSource = Path.GetFullPath(Path.Combine(builder.Environment.ContentRootPath, csb.DataSource));
}
builder.Services.AddDbContext<AppDbContext>(opt =>
{
	opt.UseSqlite(csb.ToString());

	if (builder.Environment.IsDevelopment())
	{
		opt.EnableDetailedErrors();
		opt.EnableSensitiveDataLogging();
	}
});

// Infrastructure
builder.Services.AddSingleton<ISystemClock, SystemClock>();

// Background services (sample). Add additional hosted services here as the app grows.
builder.Services.AddHostedService<SampleDailyService>();

builder.Services.AddHttpContextAccessor();

// Identity (cookie auth)
builder.Services.AddAppIdentity();

// Authorization policies
builder.Services.AddAuthorization(o =>
{
	o.AddPolicy(AuthorizationPolicies.RequireAdmin, p => p.RequireRole(AppRoles.SystemAdmin));
	o.AddPolicy(AuthorizationPolicies.RequireUser, p => p.RequireRole(AppRoles.User, AppRoles.SystemAdmin));
});

// FastEndpoints + Swagger
builder.Services.AddFastEndpoints();
builder.Services.SwaggerDocument(o =>
{
	o.DocumentSettings = s => s.Title = "PjBudget";
});

// CORS — dev only; production is single-origin under one server, so no CORS headers needed.
builder.Services.AddCors(o =>
{
	o.AddDefaultPolicy(p =>
	{
		if (builder.Environment.IsDevelopment())
		{
			p.SetIsOriginAllowed(origin => new Uri(origin).Host == "localhost")
				.AllowAnyHeader()
				.AllowAnyMethod()
				.AllowCredentials();
		}
	});
});

var app = builder.Build();

// DB migrate + seed default admin
await SeedData.EnsureSeededAsync(app.Services);

if (app.Environment.IsProduction())
{
	app.UseHsts();
	app.UseHttpsRedirection();
}

var forwardedHeadersOptions = new ForwardedHeadersOptions
{
	ForwardedHeaders = ForwardedHeaders.XForwardedHost | ForwardedHeaders.XForwardedProto | ForwardedHeaders.XForwardedFor,
};
forwardedHeadersOptions.KnownIPNetworks.Clear();
forwardedHeadersOptions.KnownProxies.Clear();
app.UseForwardedHeaders(forwardedHeadersOptions);

app.UseCors();
app.UseAuthentication();
app.UseAuthorization();

app.UseFastEndpoints();
app.UseSwaggerGen();

// MCP endpoints are NOT mapped here. See app/Features/Mcp/McpSetup.cs for how to enable.

// Serve built frontend (production); in dev, Vite serves the UI.
app.UseDefaultFiles();
app.UseStaticFiles();
app.MapFallbackToFile("index.html");

app.Run();
