using System.Text;
using System.Text.Json;
using GloHorizonApi.Services.Interfaces;

namespace GloHorizonApi.Services.Providers;

public class ResendEmailService : IEmailService
{
    private readonly IConfiguration _configuration;
    private readonly ILogger<ResendEmailService> _logger;
    private readonly HttpClient _httpClient;

    public ResendEmailService(
        IConfiguration configuration,
        ILogger<ResendEmailService> logger,
        HttpClient httpClient)
    {
        _configuration = configuration;
        _logger = logger;
        _httpClient = httpClient;
    }

    public async Task<EmailResponse> SendEmailAsync(string toEmail, string subject, string body, bool isHtml = true)
    {
        try
        {
            var apiKey = _configuration.GetValue<string>("ResendSettings:ApiKey");
            var fromEmail = _configuration.GetValue<string>("ResendSettings:FromEmail") ?? "info@glohorizonsgh.com";
            var fromName = _configuration.GetValue<string>("ResendSettings:FromName") ?? "Global Horizons Travel";

            if (string.IsNullOrEmpty(apiKey))
            {
                return new EmailResponse
                {
                    Success = false,
                    Error = "Resend API key not configured"
                };
            }

            var emailRequest = new
            {
                from = $"{fromName} <{fromEmail}>",
                to = new[] { toEmail },
                subject = subject,
                html = isHtml ? body : null,
                text = !isHtml ? body : null
            };

            var jsonContent = JsonSerializer.Serialize(emailRequest);
            var content = new StringContent(jsonContent, Encoding.UTF8, "application/json");

            _httpClient.DefaultRequestHeaders.Clear();
            _httpClient.DefaultRequestHeaders.Add("Authorization", $"Bearer {apiKey}");

            var response = await _httpClient.PostAsync("https://api.resend.com/emails", content);
            var responseContent = await response.Content.ReadAsStringAsync();

            if (response.IsSuccessStatusCode)
            {
                _logger.LogInformation("✅ Email sent successfully to {ToEmail} via Resend", toEmail);
                
                return new EmailResponse
                {
                    Success = true,
                    Message = "Email sent successfully via Resend"
                };
            }
            else
            {
                _logger.LogError("❌ Failed to send email via Resend - Status: {StatusCode}, Response: {Content}", 
                    response.StatusCode, responseContent);
                
                return new EmailResponse
                {
                    Success = false,
                    Error = $"Failed to send email: {response.StatusCode} - {responseContent}"
                };
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "❌ Error sending email via Resend to {ToEmail}", toEmail);
            return new EmailResponse
            {
                Success = false,
                Error = $"Email error: {ex.Message}"
            };
        }
    }

    private string ConvertHtmlToText(string html)
    {
        if (string.IsNullOrEmpty(html)) return string.Empty;
        
        // Simple HTML to text conversion
        var text = html
            .Replace("<br>", "\n")
            .Replace("<br/>", "\n")
            .Replace("<br />", "\n")
            .Replace("</p>", "\n")
            .Replace("</div>", "\n")
            .Replace("</h1>", "\n")
            .Replace("</h2>", "\n")
            .Replace("</h3>", "\n");
        
        // Remove HTML tags using regex
        return System.Text.RegularExpressions.Regex.Replace(text, "<.*?>", "").Trim();
    }

    public async Task<EmailResponse> SendBookingConfirmationAsync(string toEmail, string customerName, string referenceNumber, string serviceType)
    {
        var subject = $"Booking Confirmation - {referenceNumber}";
        var body = GenerateBookingConfirmationHtml(customerName, referenceNumber, serviceType);
        
        return await SendEmailAsync(toEmail, subject, body, true);
    }

    public async Task<EmailResponse> SendAdminNotificationAsync(string toEmail, string subject, string message)
    {
        var adminSubject = $"[ADMIN] {subject}";
        var adminBody = GenerateAdminNotificationHtml(subject, message);
        
        return await SendEmailAsync(toEmail, adminSubject, adminBody, true);
    }

    public async Task<EmailResponse> SendBookingStatusUpdateAsync(string toEmail, string customerName, string referenceNumber, string newStatus, string? adminNotes = null)
    {
        var subject = $"Booking Update - {referenceNumber}";
        var body = GenerateStatusUpdateHtml(customerName, referenceNumber, newStatus, adminNotes);
        
        return await SendEmailAsync(toEmail, subject, body, true);
    }

    private static string GenerateBookingConfirmationHtml(string customerName, string referenceNumber, string serviceType)
    {
        return $@"
<!DOCTYPE html>
<html>
<head>
    <meta charset='utf-8'>
    <title>Booking Confirmation</title>
</head>
<body style='font-family: Arial, sans-serif; line-height: 1.6; color: #333;'>
    <div style='max-width: 600px; margin: 0 auto; padding: 20px;'>
        <div style='text-align: center; margin-bottom: 30px;'>
            <h1 style='color: #2563eb;'>Global Horizons Travel</h1>
        </div>
        
        <h2>Thank you for your booking request!</h2>
        
        <p>Dear {customerName},</p>
        
        <p>We have received your <strong>{serviceType}</strong> booking request and are currently processing it.</p>
        
        <div style='background-color: #f8f9fa; padding: 20px; border-radius: 8px; margin: 20px 0;'>
            <h3>Booking Details:</h3>
            <p><strong>Reference Number:</strong> {referenceNumber}</p>
            <p><strong>Service Type:</strong> {serviceType}</p>
            <p><strong>Status:</strong> Under Review</p>
        </div>
        
        <p>Our team will review your request and contact you within 24 hours with a detailed quote and next steps.</p>
        
        <p>You can track your booking status using your reference number on our website.</p>
        
        <div style='margin-top: 30px; padding-top: 20px; border-top: 1px solid #ddd;'>
            <p><strong>Contact Us:</strong></p>
            <p>Email: info@globalhorizonstravel.com</p>
            <p>Phone: +233 205 078 908</p>
        </div>
        
        <p style='margin-top: 30px;'>
            Best regards,<br>
            The Global Horizons Travel Team
        </p>
    </div>
</body>
</html>";
    }

    private static string GenerateAdminNotificationHtml(string subject, string message)
    {
        return $@"
<!DOCTYPE html>
<html>
<head>
    <meta charset='utf-8'>
    <title>Admin Notification</title>
</head>
<body style='font-family: Arial, sans-serif; line-height: 1.6; color: #333;'>
    <div style='max-width: 600px; margin: 0 auto; padding: 20px;'>
        <div style='background-color: #dc3545; color: white; padding: 15px; border-radius: 8px; margin-bottom: 20px;'>
            <h2 style='margin: 0;'>🚨 Admin Alert</h2>
        </div>
        
        <h3>{subject}</h3>
        
        <div style='background-color: #f8f9fa; padding: 20px; border-radius: 8px; margin: 20px 0;'>
            <p>{message}</p>
        </div>
        
        <p><strong>Time:</strong> {DateTime.UtcNow:yyyy-MM-dd HH:mm:ss} UTC</p>
        
        <p>Please log in to the admin dashboard to take action if required.</p>
        
        <div style='margin-top: 30px; padding-top: 20px; border-top: 1px solid #ddd; font-size: 12px; color: #666;'>
            <p>This is an automated message from Global Horizons Travel booking system.</p>
        </div>
    </div>
</body>
</html>";
    }

    private static string GenerateStatusUpdateHtml(string customerName, string referenceNumber, string newStatus, string? adminNotes)
    {
        return $@"
<!DOCTYPE html>
<html>
<head>
    <meta charset='utf-8'>
    <title>Booking Status Update</title>
</head>
<body style='font-family: Arial, sans-serif; line-height: 1.6; color: #333;'>
    <div style='max-width: 600px; margin: 0 auto; padding: 20px;'>
        <div style='text-align: center; margin-bottom: 30px;'>
            <h1 style='color: #2563eb;'>Global Horizons Travel</h1>
        </div>
        
        <h2>Booking Status Update</h2>
        
        <p>Dear {customerName},</p>
        
        <p>Your booking status has been updated:</p>
        
        <div style='background-color: #f8f9fa; padding: 20px; border-radius: 8px; margin: 20px 0;'>
            <p><strong>Reference Number:</strong> {referenceNumber}</p>
            <p><strong>New Status:</strong> <span style='color: #28a745; font-weight: bold;'>{newStatus}</span></p>
            {(string.IsNullOrEmpty(adminNotes) ? "" : $"<p><strong>Notes:</strong> {adminNotes}</p>")}
        </div>
        
        <p>You can always track your booking status on our website using your reference number.</p>
        
        <div style='margin-top: 30px; padding-top: 20px; border-top: 1px solid #ddd;'>
            <p><strong>Need Help?</strong></p>
            <p>Email: info@globalhorizonstravel.com</p>
            <p>Phone: +233 205 078 908</p>
        </div>
        
        <p style='margin-top: 30px;'>
            Best regards,<br>
            The Global Horizons Travel Team
        </p>
    </div>
</body>
</html>";
    }
}