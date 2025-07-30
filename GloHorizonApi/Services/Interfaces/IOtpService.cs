namespace GloHorizonApi.Services.Interfaces;

public interface IOtpService
{
    Task<string> GenerateOtpAsync(string phoneNumber, int expiryMinutes = 10);
    Task<bool> VerifyOtpAsync(string phoneNumber, string otpCode);
    Task<bool> InvalidateOtpAsync(string phoneNumber);
    Task<int> GetAttemptCountAsync(string phoneNumber);
    Task<bool> IncrementAttemptCountAsync(string phoneNumber);
    Task<bool> IsOtpValidAsync(string phoneNumber);
}

public class OtpVerificationResult
{
    public bool IsValid { get; set; }
    public string Message { get; set; } = string.Empty;
    public bool IsExpired { get; set; }
    public bool IsUsed { get; set; }
    public int AttemptCount { get; set; }
}