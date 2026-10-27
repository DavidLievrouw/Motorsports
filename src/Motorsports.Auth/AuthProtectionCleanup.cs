using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Motorsports.Infrastructure.Auth;

namespace Motorsports.Auth;

internal sealed class AuthProtectionCleanup(
    AuthThrottleStore store,
    TimeProvider clock,
    ILogger<AuthProtectionCleanup> logger
) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        using var timer = new PeriodicTimer(TimeSpan.FromMinutes(1), clock);
        try
        {
            while (await timer.WaitForNextTickAsync(stoppingToken))
            {
                var result = await store.CleanupAsync(stoppingToken);
                if (!result.IsSuccess)
                {
                    logger.LogWarning("Authentication protection cleanup failed: {Reason}", result.Error.Message);
                }
            }
        }
        catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
        {
            // Normal host shutdown cancels the timer or the active cleanup.
        }
    }
}
