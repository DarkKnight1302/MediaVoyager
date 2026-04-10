using MediaVoyager.Services.Interfaces;
using Quartz;

namespace MediaVoyager.Services
{
    [DisallowConcurrentExecution]
    public class WatchHistoryCleanupJob : IJob
    {
        private readonly IWatchHistoryCleanupService watchHistoryCleanupService;
        private readonly ILogger<WatchHistoryCleanupJob> logger;

        public WatchHistoryCleanupJob(
            IWatchHistoryCleanupService watchHistoryCleanupService,
            ILogger<WatchHistoryCleanupJob> logger)
        {
            this.watchHistoryCleanupService = watchHistoryCleanupService;
            this.logger = logger;
        }

        public async Task Execute(IJobExecutionContext context)
        {
            try
            {
                logger.LogInformation("[WatchHistoryCleanup] Starting daily cleanup run");
                await watchHistoryCleanupService.RunCleanupAsync(context.CancellationToken);
                logger.LogInformation("[WatchHistoryCleanup] Daily cleanup run completed");
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                logger.LogError(ex, "[WatchHistoryCleanup] Error during cleanup run");
            }
        }
    }
}
