using MediaVoyager.Services.Interfaces;
using NewHorizonLib.Services.Interfaces;

namespace MediaVoyager.Services
{
    public class ErrorNotificationService : IErrorNotificationService
    {
        private readonly IEmailService _emailService;
        private readonly ILogger<ErrorNotificationService> _logger;
        private const string NotificationEmail = "mediavoyager.in@gmail.com";
        private const string SenderName = "MediaVoyager Alert";
        private const string FromEmail = "noreply@mediavoyager.in";

        public ErrorNotificationService(IEmailService emailService, ILogger<ErrorNotificationService> logger)
        {
            _emailService = emailService;
            _logger = logger;
        }

        public async Task SendErrorNotificationAsync(string endpoint, string userId, string errorType, string errorDetails)
        {
            try
            {
                string subject = $"[MediaVoyager Alert] Recommendation API Error - {errorType}";
                string body = BuildErrorEmailBody(endpoint, userId, errorType, errorDetails);

                await _emailService.SendMail(NotificationEmail, body, subject, SenderName, FromEmail, true);
                _logger.LogInformation("Error notification email sent for endpoint: {Endpoint}, ErrorType: {ErrorType}", endpoint, errorType);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to send error notification email for endpoint: {Endpoint}", endpoint);
            }
        }

        private static string BuildErrorEmailBody(string endpoint, string userId, string errorType, string errorDetails)
        {
            return $@"<!DOCTYPE html>
<html lang=""en"">
<head>
    <meta charset=""UTF-8"">
    <meta name=""viewport"" content=""width=device-width, initial-scale=1"">
    <title>MediaVoyager - API Error Alert</title>
</head>
<body style=""margin:0; padding:0; background:#0b1020; font-family: Arial, Helvetica, sans-serif;"">
    <table role=""presentation"" cellpadding=""0"" cellspacing=""0"" width=""100%"" style=""background:#0b1020;"">
        <tr>
            <td align=""center"" style=""padding: 20px;"">
                <div style=""max-width:600px; margin:0 auto;"">
                    <div style=""background:#111827; border-radius:12px; overflow:hidden; border:1px solid #dc2626;"">
                        <div style=""text-align:center; padding:24px 16px 8px 16px;"">
                            <div style=""color:#dc2626; letter-spacing:1.8px; font-size:12px; font-weight:700;"">⚠️ MEDIA VOYAGER ALERT</div>
                            <div style=""color:#e6edf3; font-size:22px; font-weight:700; margin:10px 0 0 0;"">Recommendation API Error</div>
                        </div>
                        <div style=""padding:24px 20px; color:#d1d7e0;"">
                            <table style=""width:100%; border-collapse:collapse;"">
                                <tr>
                                    <td style=""padding:10px; border-bottom:1px solid #2b3645; color:#93a4b6; font-weight:600;"">Timestamp</td>
                                    <td style=""padding:10px; border-bottom:1px solid #2b3645; color:#f8fafc;"">{DateTime.UtcNow:yyyy-MM-dd HH:mm:ss} UTC</td>
                                </tr>
                                <tr>
                                    <td style=""padding:10px; border-bottom:1px solid #2b3645; color:#93a4b6; font-weight:600;"">Endpoint</td>
                                    <td style=""padding:10px; border-bottom:1px solid #2b3645; color:#f8fafc;"">{endpoint}</td>
                                </tr>
                                <tr>
                                    <td style=""padding:10px; border-bottom:1px solid #2b3645; color:#93a4b6; font-weight:600;"">User ID</td>
                                    <td style=""padding:10px; border-bottom:1px solid #2b3645; color:#f8fafc;"">{userId ?? "N/A"}</td>
                                </tr>
                                <tr>
                                    <td style=""padding:10px; border-bottom:1px solid #2b3645; color:#93a4b6; font-weight:600;"">Error Type</td>
                                    <td style=""padding:10px; border-bottom:1px solid #2b3645; color:#dc2626; font-weight:600;"">{errorType}</td>
                                </tr>
                                <tr>
                                    <td style=""padding:10px; color:#93a4b6; font-weight:600;"">Details</td>
                                    <td style=""padding:10px; color:#f8fafc;"">{errorDetails}</td>
                                </tr>
                            </table>
                        </div>
                        <div style=""background:#0f1426; padding:18px 16px; text-align:center; border-top:1px solid #2b3645;"">
                            <div style=""font-size:12px; color:#8ea2b7;"">This is an automated alert from Media Voyager monitoring system.</div>
                        </div>
                    </div>
                </div>
            </td>
        </tr>
    </table>
</body>
</html>";
        }
    }
}
