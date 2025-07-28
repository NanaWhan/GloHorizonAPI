using System.ComponentModel.DataAnnotations;

namespace GloHorizonApi.Models.DomainModels;

public class Admin
{
    [Key]
    public string Id { get; set; } = Guid.NewGuid().ToString();
    
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
    [MaxLength(255)]
    public string PasswordHash { get; set; } = string.Empty;
    
    [Required]
    public AdminRole Role { get; set; } = AdminRole.Admin;
    
    public bool IsActive { get; set; } = true;
    
    // Notification preferences
    public bool ReceiveEmailNotifications { get; set; } = true;
    public bool ReceiveSmsNotifications { get; set; } = true;
    
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? LastLoginAt { get; set; }
    
    [MaxLength(50)]
    public string? CreatedBy { get; set; }
}

public enum AdminRole
{
    SuperAdmin = 1,
    Admin = 2,
    Manager = 3
} 