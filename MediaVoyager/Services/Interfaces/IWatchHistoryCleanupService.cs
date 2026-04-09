namespace MediaVoyager.Services.Interfaces
{
    public interface IWatchHistoryCleanupService
    {
        Task CleanupUserWatchHistoryAsync(string userId);
    }
}
