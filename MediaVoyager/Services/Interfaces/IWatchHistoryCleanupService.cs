namespace MediaVoyager.Services.Interfaces
{
    public interface IWatchHistoryCleanupService
    {
        Task CleanupUserWatchHistoryAsync(string userId);
        Task RunCleanupAsync(CancellationToken cancellationToken = default);
    }
}
