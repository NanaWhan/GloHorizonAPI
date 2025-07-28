using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace GloHorizonApi.Models.DomainModels;

public class BookingRequest
{
    [Key]
    public int Id { get; set; }
    
    [Required]
    [MaxLength(20)]
    public string ReferenceNumber { get; set; } = string.Empty;
    
    [Required]
    public string UserId { get; set; } = string.Empty;
    
    [Required]
    public ServiceType ServiceType { get; set; }
    
    [Required]
    public BookingStatus Status { get; set; } = BookingStatus.Pending;
    
    // JSON data containing the specific booking details
    [Required]
    [Column(TypeName = "jsonb")]
    public string BookingData { get; set; } = string.Empty;
    
    // Pricing information  
    public decimal? EstimatedPrice { get; set; }
    public decimal? FinalPrice { get; set; }
    public string? Currency { get; set; } = "GHS";
    
    // Timestamps
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? UpdatedAt { get; set; }
    public DateTime? CompletedAt { get; set; }
    
    // Admin notes
    [MaxLength(1000)]
    public string? AdminNotes { get; set; }
    
    // Urgency level
    public UrgencyLevel Urgency { get; set; } = UrgencyLevel.Standard;
    
    // Navigation properties
    [ForeignKey("UserId")]
    public virtual User User { get; set; } = null!;
    
    public virtual ICollection<BookingStatusHistory> StatusHistory { get; set; } = new List<BookingStatusHistory>();
}

public enum ServiceType
{
    Flight = 1,
    Hotel = 2, 
    Tour = 3,
    Visa = 4,
    CompletePackage = 5
}

public enum BookingStatus
{
    Pending = 1,
    UnderReview = 2,
    QuoteReady = 3,
    QuoteAccepted = 4,
    PaymentPending = 5,
    Processing = 6,
    Confirmed = 7,
    Completed = 8,
    Cancelled = 9,
    Rejected = 10
}

public enum UrgencyLevel
{
    Standard = 1,
    Urgent = 2,
    Emergency = 3
} 