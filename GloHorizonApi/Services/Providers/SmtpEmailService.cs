using System.Net;
using System.Net.Mail;
using GloHorizonApi.Services.Interfaces;

namespace GloHorizonApi.Services.Providers;

public class SmtpEmailService : IEmailService
{
    private readonly IConfiguration _configuration;
    private readonly ILogger<SmtpEmailService> _logger;

    public SmtpEmailService(
        IConfiguration configuration,
        ILogger<SmtpEmailService> logger)
    {
        _configuration = configuration;
        _logger = logger;
    }

    public async Task<EmailResponse> SendEmailAsync(string toEmail, string subject, string body, bool isHtml = true)
    {
        try
        {
            var smtpHost = _configuration.GetValue<string>("EmailSettings:SmtpHost");
            var smtpPort = _configuration.GetValue<int>("EmailSettings:SmtpPort");
            var smtpUsername = _configuration.GetValue<string>("EmailSettings:Username");
            var smtpPassword = _configuration.GetValue<string>("EmailSettings:Password");
            var fromEmail = _configuration.GetValue<string>("EmailSettings:FromEmail");
            var fromName = _configuration.GetValue<string>("EmailSettings:FromName") ?? "Global Horizons Travel";

            if (string.IsNullOrEmpty(smtpHost) || string.IsNullOrEmpty(smtpUsername) || string.IsNullOrEmpty(fromEmail))
            {
                return new EmailResponse
                {
                    Success = false,
                    Error = "Email configuration is incomplete"
                };
            }

            using var client = new SmtpClient(smtpHost, smtpPort)
            {
                Credentials = new NetworkCredential(smtpUsername, smtpPassword),
                EnableSsl = true
            };

            using var message = new MailMessage
            {
                From = new MailAddress(fromEmail, fromName),
                Subject = subject,
                Body = body,
                IsBodyHtml = isHtml
            };

            message.To.Add(toEmail);

            await client.SendMailAsync(message);

            _logger.LogInformation($"Email sent successfully to {toEmail}");

            return new EmailResponse
            {
                Success = true,
                Message = "Email sent successfully"
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error sending email to {ToEmail}", toEmail);
            return new EmailResponse
            {
                Success = false,
                Error = "Failed to send email"
            };
        }
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
            <p>Phone: +233 XXX XXX XXX</p>
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
            <p>Phone: +233 XXX XXX XXX</p>
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