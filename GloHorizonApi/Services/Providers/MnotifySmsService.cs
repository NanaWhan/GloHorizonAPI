using System.Text;
using System.Text.Json;
using GloHorizonApi.Services.Interfaces;

namespace GloHorizonApi.Services.Providers;

public class MnotifySmsService : ISmsService
{
    private readonly HttpClient _httpClient;
    private readonly IConfiguration _configuration;
    private readonly ILogger<MnotifySmsService> _logger;

    public MnotifySmsService(
        HttpClient httpClient,
        IConfiguration configuration,
        ILogger<MnotifySmsService> logger)
    {
        _httpClient = httpClient;
        _configuration = configuration;
        _logger = logger;
    }

    public async Task<SmsResponse> SendSmsAsync(string phoneNumber, string message)
    {
        try
        {
            var apiKey = _configuration.GetValue<string>("MnotifySettings:ApiKey");
            var senderId = _configuration.GetValue<string>("MnotifySettings:SenderId") ?? "GloHorizon";

            if (string.IsNullOrEmpty(apiKey))
            {
                return new SmsResponse
                {
                    Success = false,
                    Error = "Mnotify API key not configured"
                };
            }

            var payload = new
            {
                key = apiKey,
                to = FormatPhoneNumber(phoneNumber),
                msg = message,
                sender_id = senderId
            };

            var jsonContent = JsonSerializer.Serialize(payload);
            var content = new StringContent(jsonContent, Encoding.UTF8, "application/json");

            var response = await _httpClient.PostAsync("https://apps.mnotify.net/smsapi", content);
            var responseContent = await response.Content.ReadAsStringAsync();

            _logger.LogInformation($"Mnotify SMS response: {responseContent}");

            if (response.IsSuccessStatusCode)
            {
                var mnotifyResponse = JsonSerializer.Deserialize<MnotifyResponse>(responseContent);
                
                if (mnotifyResponse?.Code == "1000")
                {
                    return new SmsResponse
                    {
                        Success = true,
                        Message = "SMS sent successfully",
                        MessageId = mnotifyResponse.MessageId
                    };
                }
                else
                {
                    return new SmsResponse
                    {
                        Success = false,
                        Error = mnotifyResponse?.Message ?? "Unknown error from Mnotify"
                    };
                }
            }
            else
            {
                return new SmsResponse
                {
                    Success = false,
                    Error = $"HTTP Error: {response.StatusCode}"
                };
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error sending SMS via Mnotify");
            return new SmsResponse
            {
                Success = false,
                Error = "Failed to send SMS"
            };
        }
    }

    public async Task<SmsResponse> SendOtpAsync(string phoneNumber, string otpCode)
    {
        var message = $"Your Global Horizons verification code is: {otpCode}. Valid for 10 minutes. Do not share with anyone.";
        return await SendSmsAsync(phoneNumber, message);
    }

    public async Task<SmsResponse> SendBookingNotificationAsync(string phoneNumber, string referenceNumber, string serviceType)
    {
        var message = $"Thank you for your {serviceType} booking request! Your reference number is {referenceNumber}. We will contact you shortly with your quote. - Global Horizons Travel";
        return await SendSmsAsync(phoneNumber, message);
    }

    public async Task<SmsResponse> SendAdminAlertAsync(string phoneNumber, string message)
    {
        var adminMessage = $"[ADMIN ALERT] {message} - Global Horizons Travel System";
        return await SendSmsAsync(phoneNumber, adminMessage);
    }

    private static string FormatPhoneNumber(string phoneNumber)
    {
        // Remove any spaces, dashes, or other formatting
        var cleaned = phoneNumber.Replace(" ", "").Replace("-", "").Replace("(", "").Replace(")", "");
        
        // Ensure Ghana country code format
        if (cleaned.StartsWith("0"))
        {
            cleaned = "+233" + cleaned.Substring(1);
        }
        else if (!cleaned.StartsWith("+233"))
        {
            cleaned = "+233" + cleaned;
        }

        return cleaned;
    }
}

public class MnotifyResponse
{
    public string Code { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;
    public string MessageId { get; set; } = string.Empty;
} 