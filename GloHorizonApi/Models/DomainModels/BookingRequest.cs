using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace GloHorizonApi.Models.DomainModels;

public class BookingRequest
{
    [Key]
    public int Id { get; set; }
    
    [Required]
    [MaxLength(50)]
    public string ReferenceNumber { get; set; } = string.Empty;
    
    public string? UserId { get; set; }
    
    [Required]
    public BookingType ServiceType { get; set; } // Updated enum name
    
    [Required]
    public BookingStatus Status { get; set; } = BookingStatus.Submitted; // Updated default
    
    // Pricing information  
    public decimal? QuotedAmount { get; set; }
    public decimal? FinalAmount { get; set; }
    public string? Currency { get; set; } = "GHS";
    
    // Travel information
    public DateTime? TravelDate { get; set; }
    
    [MaxLength(200)]
    public string? Destination { get; set; }
    
    // Contact information
    [Required]
    [MaxLength(255)]
    public string ContactEmail { get; set; } = string.Empty;
    
    [Required]
    [MaxLength(20)]
    public string ContactPhone { get; set; } = string.Empty;
    
    // Timestamps
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? UpdatedAt { get; set; }
    public DateTime? CompletedAt { get; set; }
    
    // Notes and requests
    [MaxLength(1000)]
    public string? SpecialRequests { get; set; }
    
    [MaxLength(1000)]
    public string? AdminNotes { get; set; }
    
    // Urgency level
    public UrgencyLevel Urgency { get; set; } = UrgencyLevel.Standard;
    
    // JSON fields for service-specific data (using JSONB for PostgreSQL)
    [Column(TypeName = "jsonb")]
    public string? FlightDetails { get; set; }
    
    [Column(TypeName = "jsonb")]
    public string? HotelDetails { get; set; }
    
    [Column(TypeName = "jsonb")]
    public string? TourDetails { get; set; }
    
    [Column(TypeName = "jsonb")]
    public string? VisaDetails { get; set; }
    
    [Column(TypeName = "jsonb")]
    public string? PackageDetails { get; set; }
    
    // Navigation properties
    [ForeignKey("UserId")]
    public virtual User User { get; set; } = null!;
    
    public virtual ICollection<BookingStatusHistory> StatusHistory { get; set; } = new List<BookingStatusHistory>();
    
    public virtual ICollection<BookingDocument> Documents { get; set; } = new List<BookingDocument>();
}

public enum BookingType
{
    Flight = 1,
    Hotel = 2, 
    Tour = 3,
    Visa = 4,
    CompletePackage = 5
}

public enum BookingStatus
{
    Submitted = 1,
    UnderReview = 2,
    QuoteProvided = 3,
    PaymentPending = 4,
    Processing = 5,
    Confirmed = 6,
    Completed = 7,
    Cancelled = 8
}

public enum UrgencyLevel
{
    Standard = 0,
    Urgent = 1,
    Emergency = 2
} 