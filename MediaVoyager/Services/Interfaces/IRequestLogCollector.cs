namespace MediaVoyager.Services.Interfaces
{
    /// <summary>
    /// A scoped service that collects log messages during the lifetime of an HTTP request.
    /// </summary>
    public interface IRequestLogCollector
    {
        /// <summary>
        /// Adds a log message to the collector.
        /// </summary>
        /// <param name="message">The log message to add.</param>
        void AddLog(string message);

        /// <summary>
        /// Gets all collected log messages as a single string.
        /// </summary>
        /// <returns>A string containing all log messages separated by newlines.</returns>
        string GetLogs();

        /// <summary>
        /// Gets all collected log messages as a list.
        /// </summary>
        /// <returns>A list of log messages.</returns>
        IReadOnlyList<string> GetLogsList();

        /// <summary>
        /// Clears all collected log messages.
        /// </summary>
        void Clear();
    }
}
