using System.ComponentModel.DataAnnotations;
using GloHorizonApi.Models.DomainModels;

namespace GloHorizonApi.Models.Dtos.Package;

public class TravelPackageDto
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string Destination { get; set; } = string.Empty;
    public int Duration { get; set; }
    public decimal BasePrice { get; set; }
    public string Currency { get; set; } = "GHS";
    public string? ImageUrl { get; set; }
    public PackageCategory Category { get; set; }
    public bool IsFeatured { get; set; }
    public DateTime? AvailableFrom { get; set; }
    public DateTime? AvailableUntil { get; set; }
}

public class TravelPackageDetailDto : TravelPackageDto
{
    public string? PricingRules { get; set; }
    public string? PackageDetails { get; set; }
    public string? ImageGallery { get; set; }
    public string? SeoTitle { get; set; }
    public string? SeoDescription { get; set; }
}

public class CreateTravelPackageRequest
{
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
    public int Duration { get; set; }
    
    [Required]
    public decimal BasePrice { get; set; }
    
    [MaxLength(10)]
    public string? Currency { get; set; }
    
    public object? PricingRules { get; set; }
    public object? PackageDetails { get; set; }
    
    [MaxLength(500)]
    public string? ImageUrl { get; set; }
    
    public string[]? ImageGallery { get; set; }
    
    [Required]
    public PackageCategory Category { get; set; }
    
    public bool IsFeatured { get; set; } = false;
    public int DisplayOrder { get; set; } = 0;
    
    public DateTime? AvailableFrom { get; set; }
    public DateTime? AvailableUntil { get; set; }
    
    [MaxLength(200)]
    public string? SeoTitle { get; set; }
    
    [MaxLength(500)]
    public string? SeoDescription { get; set; }
}

public class UpdateTravelPackageRequest
{
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
    public int Duration { get; set; }
    
    [Required]
    public decimal BasePrice { get; set; }
    
    [MaxLength(10)]
    public string? Currency { get; set; }
    
    public object? PricingRules { get; set; }
    public object? PackageDetails { get; set; }
    
    [MaxLength(500)]
    public string? ImageUrl { get; set; }
    
    public string[]? ImageGallery { get; set; }
    
    [Required]
    public PackageCategory Category { get; set; }
    
    public bool IsFeatured { get; set; } = false;
    public int DisplayOrder { get; set; } = 0;
    
    public DateTime? AvailableFrom { get; set; }
    public DateTime? AvailableUntil { get; set; }
    
    [MaxLength(200)]
    public string? SeoTitle { get; set; }
    
    [MaxLength(500)]
    public string? SeoDescription { get; set; }
} 