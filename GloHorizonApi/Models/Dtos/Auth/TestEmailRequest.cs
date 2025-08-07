using System.ComponentModel.DataAnnotations;

namespace GloHorizonApi.Models.Dtos.Auth;

public class TestEmailRequest
{
    [Required]
    [EmailAddress]
    public string Email { get; set; } = string.Empty;
    
    public string? Message { get; set; }
} 