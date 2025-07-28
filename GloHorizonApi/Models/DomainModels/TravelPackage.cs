using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace GloHorizonApi.Models.DomainModels;

public class TravelPackage
{
    [Key]
    public int Id { get; set; }
    
    [Required]
    [MaxLength(200)]
    public string Name { get; set; } = string.Empty;
    
    [Required]
    [MaxLength(1000)]
    public string Description { get; set; } = string.Empty;
    
    [Required]
    [MaxLength(100)]
    public string Destination { get; set; } = string.Empty;
    
    [Required]
    public int Duration { get; set; } // in days
    
    [Required]
    [Column(TypeName = "decimal(10,2)")]
    public decimal BasePrice { get; set; }
    
    [MaxLength(10)]
    public string Currency { get; set; } = "GHS";
    
    // Pricing rules stored as JSON
    [Column(TypeName = "jsonb")]
    public string? PricingRules { get; set; }
    
    // Package details stored as JSON
    [Column(TypeName = "jsonb")]
    public string? PackageDetails { get; set; }
    
    [MaxLength(500)]
    public string? ImageUrl { get; set; }
    
    [Column(TypeName = "jsonb")]
    public string? ImageGallery { get; set; } // Array of image URLs
    
    public bool IsActive { get; set; } = true;
    public bool IsFeatured { get; set; } = false;
    
    public int DisplayOrder { get; set; } = 0;
    
    // Category for filtering
    [Required]
    public PackageCategory Category { get; set; }
    
    // Availability
    public DateTime? AvailableFrom { get; set; }
    public DateTime? AvailableUntil { get; set; }
    
    // SEO fields
    [MaxLength(200)]
    public string? SeoTitle { get; set; }
    
    [MaxLength(500)]
    public string? SeoDescription { get; set; }
    
    // Audit fields
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? UpdatedAt { get; set; }
    
    [MaxLength(50)]
    public string? CreatedBy { get; set; }
    
    [MaxLength(50)]
    public string? UpdatedBy { get; set; }
}

public enum PackageCategory
{
    Adventure = 1,
    Cultural = 2,
    Luxury = 3,
    Budget = 4,
    Family = 5,
    Business = 6,
    Honeymoon = 7,
    Group = 8
} 