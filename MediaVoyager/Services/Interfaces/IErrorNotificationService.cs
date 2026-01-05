namespace MediaVoyager.Services.Interfaces
{
    public interface IErrorNotificationService
    {
        Task SendErrorNotificationAsync(string endpoint, string userId, string errorType, string errorDetails);
    }
}
