using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using GloHorizonApi.Services.Interfaces;

namespace GloHorizonApi.Services.Providers;

public class MnotifySmsService : ISmsService
{
    private readonly IConfiguration _configuration;
    private readonly ILogger<MnotifySmsService> _logger;

    public MnotifySmsService(
        IConfiguration configuration,
        ILogger<MnotifySmsService> logger)
    {
        _configuration = configuration;
        _logger = logger;
    }

    public async Task<bool> TestApiKeyAsync()
    {
        try
        {
            var apiKey = _configuration.GetValue<string>("MnotifySettings:ApiKey");
            if (string.IsNullOrEmpty(apiKey))
            {
                _logger.LogWarning("Mnotify API key not configured");
                return false;
            }

            // Test with a simple message like in RamadanApi
            var testMessage = Uri.EscapeDataString("API Key Test");
            var requestUrl = $"https://apps.mnotify.net/smsapi?key={apiKey}&to=+233000000000&msg={testMessage}&sender_id=Test";

            using var httpClient = new HttpClient();
            var response = await httpClient.GetAsync(requestUrl);
            var responseContent = await response.Content.ReadAsStringAsync();

            _logger.LogInformation("API Key test response: {Status} - {Content}", response.StatusCode, responseContent);

            // If we get any response (even error), it means the endpoint is reachable
            return response.StatusCode != System.Net.HttpStatusCode.Unauthorized;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to test API key");
            return false;
        }
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

            _logger.LogInformation("Sending SMS to {PhoneNumber} with message: {Message}", phoneNumber, message);

            // Using the same approach as RamadanApi - simple GET request with query parameters
            var formattedPhone = FormatPhoneNumber(phoneNumber);
            var encodedMessage = Uri.EscapeDataString(message);
            var requestUrl = $"https://apps.mnotify.net/smsapi?key={apiKey}&to={formattedPhone}&msg={encodedMessage}&sender_id={senderId}";

            _logger.LogInformation("mNotify request URL: {RequestUrl}", requestUrl.Replace(apiKey, "***API_KEY***"));

            using var httpClient = new HttpClient();
            var response = await httpClient.GetAsync(requestUrl);
            var responseContent = await response.Content.ReadAsStringAsync();

            _logger.LogInformation("mNotify SMS response - Status: {StatusCode}, Content: {Content}", 
                response.StatusCode, responseContent);

            if (response.IsSuccessStatusCode)
            {
                _logger.LogInformation("SMS sent successfully to {PhoneNumber}", phoneNumber);
                
                return new SmsResponse
                {
                    Success = true,
                    Message = "SMS sent successfully",
                    MessageId = Guid.NewGuid().ToString() // Generate a simple ID
                };
            }
            else
            {
                _logger.LogError("Failed to send SMS - Status: {StatusCode}, Response: {Content}", 
                    response.StatusCode, responseContent);
                
                return new SmsResponse
                {
                    Success = false,
                    Error = $"SMS failed: {response.StatusCode} - {responseContent}"
                };
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error sending SMS via Mnotify");
            return new SmsResponse
            {
                Success = false,
                Error = $"SMS error: {ex.Message}"
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
        if (string.IsNullOrEmpty(phoneNumber))
            return phoneNumber;

        // Remove any spaces, dashes, or other formatting
        var cleaned = phoneNumber.Replace(" ", "").Replace("-", "").Replace("(", "").Replace(")", "").Replace("+", "");
        
        // Handle different country codes and formats
        if (cleaned.StartsWith("233")) // Already has Ghana country code without +
        {
            return "+" + cleaned;
        }
        else if (cleaned.StartsWith("0")) // Local Ghana number
        {
            return "+233" + cleaned.Substring(1);
        }
        else if (cleaned.Length == 9 && cleaned.All(char.IsDigit)) // 9-digit Ghana number without leading 0
        {
            return "+233" + cleaned;
        }
        else if (phoneNumber.StartsWith("+")) // International format already
        {
            return phoneNumber.Replace(" ", "").Replace("-", "").Replace("(", "").Replace(")", "");
        }
        else
        {
            // Default to Ghana country code for unrecognized formats
            return "+233" + cleaned;
        }
    }
} 