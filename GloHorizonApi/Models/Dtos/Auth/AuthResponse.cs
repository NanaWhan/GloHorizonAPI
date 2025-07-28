namespace GloHorizonApi.Models.Dtos.Auth;

public class AuthResponse
{
    public bool Success { get; set; }
    public string Message { get; set; } = string.Empty;
    public string? Token { get; set; }
    public UserInfo? User { get; set; }
}

public class UserInfo
{
    public string Id { get; set; } = string.Empty;
    public string FullName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string PhoneNumber { get; set; } = string.Empty;
    public bool EmailVerified { get; set; }
    public bool PhoneVerified { get; set; }
}

public class OtpResponse
{
    public bool Success { get; set; }
    public string Message { get; set; } = string.Empty;
    public string? OtpId { get; set; } // For tracking OTP session
} 