namespace GloHorizonApi.Services.Interfaces;

public interface IEmailService
{
    Task<EmailResponse> SendEmailAsync(string toEmail, string subject, string body, bool isHtml = true);
    Task<EmailResponse> SendBookingConfirmationAsync(string toEmail, string customerName, string referenceNumber, string serviceType);
    Task<EmailResponse> SendAdminNotificationAsync(string toEmail, string subject, string message);
    Task<EmailResponse> SendBookingStatusUpdateAsync(string toEmail, string customerName, string referenceNumber, string newStatus, string? adminNotes = null);
}

public class EmailResponse
{
    public bool Success { get; set; }
    public string Message { get; set; } = string.Empty;
    public string? MessageId { get; set; }
    public string? Error { get; set; }
} 