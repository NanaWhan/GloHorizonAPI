using System.ComponentModel.DataAnnotations;

namespace GloHorizonApi.Models.DomainModels;

public class User
{
    [Key]
    public string Id { get; set; } = Guid.NewGuid().ToString();
    
    [Required]
    [MaxLength(50)]
    public string FirstName { get; set; } = string.Empty;
    
    [Required]
    [MaxLength(50)]
    public string LastName { get; set; } = string.Empty;
    
    // Computed property for compatibility with existing code
    public string FullName => $"{FirstName} {LastName}".Trim();
    
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
    
    public DateTime? DateOfBirth { get; set; }
    public bool AcceptMarketing { get; set; } = false;
    
    [MaxLength(20)]
    public string Role { get; set; } = "User";
    
    public bool EmailVerified { get; set; } = false;
    public bool PhoneVerified { get; set; } = false;
    
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? LastLoginAt { get; set; }
    
    // Navigation properties
    public virtual ICollection<BookingRequest> BookingRequests { get; set; } = new List<BookingRequest>();
} 