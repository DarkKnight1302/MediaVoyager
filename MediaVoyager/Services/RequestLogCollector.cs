using MediaVoyager.Services.Interfaces;
using System.Collections.Concurrent;

namespace MediaVoyager.Services
{
    /// <summary>
    /// A scoped service that collects log messages during the lifetime of an HTTP request.
    /// This service is registered as Scoped, so each HTTP request gets its own instance.
    /// </summary>
    public class RequestLogCollector : IRequestLogCollector
    {
        private readonly ConcurrentQueue<string> _logs = new();

        public void AddLog(string message)
        {
            if (!string.IsNullOrEmpty(message))
            {
                string timestampedMessage = $"[{DateTime.UtcNow:HH:mm:ss.fff}] {message}";
                _logs.Enqueue(timestampedMessage);
            }
        }

        public string GetLogs()
        {
            return string.Join(Environment.NewLine, _logs);
        }

        public IReadOnlyList<string> GetLogsList()
        {
            return _logs.ToList().AsReadOnly();
        }

        public void Clear()
        {
            while (_logs.TryDequeue(out _)) { }
        }
    }
}
