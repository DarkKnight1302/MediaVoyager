using MediaVoyager.Models.Dashboard;

namespace MediaVoyager.Repositories
{
    public interface IApiRequestLogRepository
    {
        Task LogAsync(string api, string method, int statusCode, DateTimeOffset timestamp, string? userId = null, string? path = null);

        Task<List<ApiDayFailureCount>> GetFailureCountsByApiAndDateAsync(DateTimeOffset fromDate, DateTimeOffset toDate);
    }
}
