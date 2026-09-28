using PjBudget.Shared.Infrastructure;

namespace PjBudget.Features.BackgroundTasks;

/// <summary>
/// Sample scheduled service that logs a heartbeat once a day at 08:00 local time.
/// Use this as a template: copy, change NextRunAt to your schedule, and put real work in RunAsync.
/// </summary>
public sealed class SampleDailyService : ScheduledBackgroundService
{
	private readonly ILogger<SampleDailyService> _logger;

	public SampleDailyService(ISystemClock clock, ILogger<SampleDailyService> logger)
		: base(clock, logger)
	{
		_logger = logger;
	}

	protected override bool RunOnStartup => true;

	protected override DateTime NextRunAt(DateTime now)
	{
		var next = now.Date.AddHours(8);
		return next > now ? next : next.AddDays(1);
	}

	protected override Task RunAsync(CancellationToken ct)
	{
		_logger.LogInformation("SampleDailyService heartbeat");
		return Task.CompletedTask;
	}
}
