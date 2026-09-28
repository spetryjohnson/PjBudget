using PjBudget.Shared.Infrastructure;

namespace PjBudget.Features.BackgroundTasks;

/// <summary>
/// Base class for background services that need to run on a recurring schedule.
/// Subclasses define when to run (NextRunAt) and what to do (RunAsync), and the base handles
/// the wait/execute/loop machinery, cancellation, and per-iteration error handling.
/// </summary>
public abstract class ScheduledBackgroundService : BackgroundService
{
	private readonly ISystemClock _clock;
	private readonly ILogger _logger;

	protected ScheduledBackgroundService(ISystemClock clock, ILogger logger)
	{
		_clock = clock;
		_logger = logger;
	}

	/// <summary>
	/// If true, run once immediately at startup before entering the scheduled loop.
	/// </summary>
	protected virtual bool RunOnStartup => false;

	/// <summary>
	/// Compute when this service should next run, given the current local time.
	/// Examples: "every day at 8 AM", "every Monday at noon", "every 15 minutes".
	/// </summary>
	protected abstract DateTime NextRunAt(DateTime now);

	/// <summary>
	/// Do the work for one scheduled invocation.
	/// </summary>
	protected abstract Task RunAsync(CancellationToken ct);

	protected override async Task ExecuteAsync(CancellationToken stoppingToken)
	{
		_logger.LogInformation("{Service} starting", GetType().Name);

		if (RunOnStartup)
		{
			await SafeRunAsync(stoppingToken);
		}

		while (!stoppingToken.IsCancellationRequested)
		{
			var now = _clock.Now;
			var next = NextRunAt(now);
			if (next <= now)
			{
				// Schedule produced a non-future time; fall back to running again in a minute
				// to avoid a tight loop.
				next = now.AddMinutes(1);
			}

			var delay = next - now;
			_logger.LogInformation("{Service} next run at {Time}", GetType().Name, next);

			try
			{
				await Task.Delay(delay, stoppingToken);
				await SafeRunAsync(stoppingToken);
			}
			catch (TaskCanceledException)
			{
				_logger.LogInformation("{Service} stopping", GetType().Name);
				break;
			}
		}
	}

	private async Task SafeRunAsync(CancellationToken ct)
	{
		try
		{
			await RunAsync(ct);
		}
		catch (Exception ex)
		{
			_logger.LogError(ex, "{Service} run failed", GetType().Name);
		}
	}
}
