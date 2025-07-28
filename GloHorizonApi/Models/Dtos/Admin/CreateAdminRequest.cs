using System.ComponentModel.DataAnnotations;
using GloHorizonApi.Models.DomainModels;

namespace GloHorizonApi.Models.Dtos.Admin;

public class CreateAdminRequest
{
    [Required]
    [MaxLength(100)]
    public string FullName { get; set; } = string.Empty;
    
    [Required]
    [EmailAddress]
    [MaxLength(150)]
    public string Email { get; set; } = string.Empty;
    
    [Required]
    [Phone]
    [MaxLength(20)]
    public string PhoneNumber { get; set; } = string.Empty;
    
    [Required]
    [MinLength(6)]
    [MaxLength(100)]
    public string Password { get; set; } = string.Empty;
    
    [Required]
    public AdminRole Role { get; set; } = AdminRole.Admin;
}