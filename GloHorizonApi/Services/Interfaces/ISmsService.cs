namespace GloHorizonApi.Services.Interfaces;

public interface ISmsService
{
    Task<SmsResponse> SendSmsAsync(string phoneNumber, string message);
    Task<SmsResponse> SendOtpAsync(string phoneNumber, string otpCode);
    Task<SmsResponse> SendBookingNotificationAsync(string phoneNumber, string referenceNumber, string serviceType);
    Task<SmsResponse> SendAdminAlertAsync(string phoneNumber, string message);
}

public class SmsResponse
{
    public bool Success { get; set; }
    public string Message { get; set; } = string.Empty;
    public string? MessageId { get; set; }
    public string? Error { get; set; }
} 