using System.ComponentModel.DataAnnotations;

namespace GloHorizonApi.Models.Dtos.Auth;

public class OtpLoginRequest
{
    [Required]
    [Phone]
    [MaxLength(20)]
    public string PhoneNumber { get; set; } = string.Empty;
}

public class VerifyOtpRequest
{
    [Required]
    [Phone]
    [MaxLength(20)]
    public string PhoneNumber { get; set; } = string.Empty;
    
    [Required]
    [StringLength(6, MinimumLength = 4)]
    public string OtpCode { get; set; } = string.Empty;
} 