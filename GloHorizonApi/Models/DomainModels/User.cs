using System.ComponentModel.DataAnnotations;

namespace GloHorizonApi.Models.DomainModels;

public class User
{
    [Key]
    public string Id { get; set; } = Guid.NewGuid().ToString();
    
    [Required]
    [MaxLength(100)]
    public string FullName { get; set; } = string.Empty;
    
    // Computed properties for compatibility
    public string FirstName => FullName.Split(' ', 2).FirstOrDefault() ?? "";
    public string LastName => FullName.Split(' ', 2).Skip(1).FirstOrDefault() ?? "";
    
    [Required]
    [EmailAddress]
    [MaxLength(150)]
    public string Email { get; set; } = string.Empty;
    
    [Required]
    [Phone]
    [MaxLength(20)]
    public string PhoneNumber { get; set; } = string.Empty;
    
    [MaxLength(255)]
    public string? PasswordHash { get; set; }
    
    public bool EmailVerified { get; set; } = false;
    public bool PhoneVerified { get; set; } = false;
    
    // Profile information (to be added later with migration)
    // public string? ProfileImageUrl { get; set; }
    // public string? Address { get; set; }
    // public string? City { get; set; }
    // public string? Country { get; set; }
    // public DateTime? DateOfBirth { get; set; }
    
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? LastLoginAt { get; set; }
    
    // Navigation properties
    public virtual ICollection<BookingRequest> BookingRequests { get; set; } = new List<BookingRequest>();
} 