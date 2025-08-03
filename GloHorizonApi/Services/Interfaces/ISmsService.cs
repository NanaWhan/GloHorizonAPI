namespace GloHorizonApi.Services.Interfaces;

public interface ISmsService
{
    Task<SmsResponse> SendSmsAsync(string phoneNumber, string message);
    Task<SmsResponse> SendOtpAsync(string phoneNumber, string otpCode);
    Task<SmsResponse> SendBookingNotificationAsync(string phoneNumber, string referenceNumber, string serviceType);
    Task<SmsResponse> SendAdminAlertAsync(string phoneNumber, string message);
    Task<SmsResponse> SendWelcomeSmsAsync(string phoneNumber, string firstName);
    Task<BroadcastSmsResponse> SendBroadcastSmsAsync(List<string> phoneNumbers, string message);
}

public class SmsResponse
{
    public bool Success { get; set; }
    public string Message { get; set; } = string.Empty;
    public string? MessageId { get; set; }
    public string? Error { get; set; }
}

public class BroadcastSmsResponse
{
    public bool Success { get; set; }
    public string Message { get; set; } = string.Empty;
    public int TotalRecipients { get; set; }
    public int SuccessfulSends { get; set; }
    public int FailedSends { get; set; }
    public List<string> FailedNumbers { get; set; } = new();
    public List<string> Errors { get; set; } = new();
} 