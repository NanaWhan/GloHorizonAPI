using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace GloHorizonApi.Models.DomainModels;

public class QuoteRequest
{
    [Key]
    public int Id { get; set; }
    
    [Required]
    [MaxLength(20)]
    public string ReferenceNumber { get; set; } = string.Empty;
    
    // Optional - null for guest users, populated for registered users
    public string? UserId { get; set; }
    
    [Required]
    public QuoteType ServiceType { get; set; }
    
    [Required]
    public QuoteStatus Status { get; set; } = QuoteStatus.Submitted;
    
    // Customer contact information (required for all requests)
    [Required]
    [MaxLength(255)]
    public string ContactEmail { get; set; } = string.Empty;
    
    [Required]
    [MaxLength(20)]
    public string ContactPhone { get; set; } = string.Empty;
    
    [MaxLength(255)]
    public string? ContactName { get; set; }
    
    // Travel information
    public DateTime? TravelDate { get; set; }
    
    [MaxLength(200)]
    public string? Destination { get; set; }
    
    // Quote and pricing information
    public decimal? QuotedAmount { get; set; }
    public string? Currency { get; set; } = "GHS";
    public DateTime? QuoteProvidedAt { get; set; }
    public DateTime? QuoteExpiresAt { get; set; }
    
    // Payment information
    public string? PaymentLinkUrl { get; set; }
    public string? PaymentReference { get; set; }
    public DateTime? PaidAt { get; set; }
    
    // Conversion to booking
    public int? BookingRequestId { get; set; }
    
    // Timestamps
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? UpdatedAt { get; set; }
    
    // Request details and notes
    [MaxLength(1000)]
    public string? SpecialRequests { get; set; }
    
    [MaxLength(1000)]
    public string? AdminNotes { get; set; }
    
    // Urgency level
    public UrgencyLevel Urgency { get; set; } = UrgencyLevel.Standard;
    
    // JSON fields for service-specific data (reuse existing detail classes)
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
    public virtual User? User { get; set; }
    
    [ForeignKey("BookingRequestId")]
    public virtual BookingRequest? BookingRequest { get; set; }
    
    public virtual ICollection<QuoteStatusHistory> StatusHistory { get; set; } = new List<QuoteStatusHistory>();
}

public enum QuoteType
{
    Flight = 1,
    Hotel = 2, 
    Tour = 3,
    Visa = 4,
    CompletePackage = 5
}

public enum QuoteStatus
{
    Submitted = 1,          // Initial submission
    UnderReview = 2,        // Admin is reviewing
    QuoteProvided = 3,      // Quote sent to customer
    PaymentPending = 4,     // Customer has quote, payment pending
    Paid = 5,              // Payment completed
    BookingConfirmed = 6,   // Converted to actual booking
    Expired = 7,           // Quote expired without payment
    Cancelled = 8          // Cancelled by customer or admin
}

// Status history tracking
public class QuoteStatusHistory
{
    [Key]
    public int Id { get; set; }
    
    [Required]
    public int QuoteRequestId { get; set; }
    
    [Required]
    public QuoteStatus FromStatus { get; set; }
    
    [Required]
    public QuoteStatus ToStatus { get; set; }
    
    [MaxLength(500)]
    public string? Notes { get; set; }
    
    [Required]
    [MaxLength(100)]
    public string ChangedBy { get; set; } = string.Empty;
    
    public DateTime ChangedAt { get; set; } = DateTime.UtcNow;
    
    // Navigation property
    [ForeignKey("QuoteRequestId")]
    public virtual QuoteRequest QuoteRequest { get; set; } = null!;
}