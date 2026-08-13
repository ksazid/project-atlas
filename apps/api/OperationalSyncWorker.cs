using Microsoft.EntityFrameworkCore;

namespace Atlas.Api;

public sealed class OperationalSyncWorker(IServiceScopeFactory scopes, ILogger<OperationalSyncWorker> logger) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        using var timer = new PeriodicTimer(TimeSpan.FromMinutes(15));
        while (await timer.WaitForNextTickAsync(stoppingToken))
        {
            try
            {
                using var scope = scopes.CreateScope();
                var db = scope.ServiceProvider.GetRequiredService<AtlasDbContext>();
                var service = scope.ServiceProvider.GetRequiredService<OperationalConnectorService>();
                var now = DateTimeOffset.UtcNow;
                var due = await db.OperationalConnectors.AsNoTracking().Where(item =>
                    item.Status == OperationalConnectorStates.Connected && item.Schedule != OperationalSyncSchedules.Manual &&
                    (item.LastAttemptAt == null || item.LastAttemptAt < now.AddHours(item.Schedule == OperationalSyncSchedules.EverySixHours ? -6 : -24)))
                    .Select(item => item.BusinessId).Take(25).ToListAsync(stoppingToken);
                foreach (var businessId in due) await service.SyncBusinessAsync(businessId, now, stoppingToken);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested) { }
            catch (Exception error) { logger.LogError(error, "Scheduled operational sync cycle failed."); }
        }
    }
}
