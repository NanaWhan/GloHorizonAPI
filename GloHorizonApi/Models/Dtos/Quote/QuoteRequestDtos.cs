using System.ComponentModel.DataAnnotations;
using GloHorizonApi.Models.DomainModels;

namespace GloHorizonApi.Models.Dtos.Quote;

// ✈️ FLIGHT QUOTE REQUEST
public class FlightQuoteRequestDto
{
    [Required]
    public FlightBookingDetails FlightDetails { get; set; } = new();
    
    [Required]
    [EmailAddress]
    public string ContactEmail { get; set; } = string.Empty;
    
    [Required]
    public string ContactPhone { get; set; } = string.Empty;
    
    [MaxLength(255)]
    public string? ContactName { get; set; }
    
    [MaxLength(1000)]
    public string? SpecialRequests { get; set; }
    
    public UrgencyLevel Urgency { get; set; } = UrgencyLevel.Standard;
}

// 🏨 HOTEL QUOTE REQUEST
public class HotelQuoteRequestDto
{
    [Required]
    public HotelBookingDetails HotelDetails { get; set; } = new();
    
    [Required]
    [EmailAddress]
    public string ContactEmail { get; set; } = string.Empty;
    
    [Required]
    public string ContactPhone { get; set; } = string.Empty;
    
    [MaxLength(255)]
    public string? ContactName { get; set; }
    
    [MaxLength(1000)]
    public string? SpecialRequests { get; set; }
    
    public UrgencyLevel Urgency { get; set; } = UrgencyLevel.Standard;
}

// 🗺️ TOUR QUOTE REQUEST
public class TourQuoteRequestDto
{
    [Required]
    public TourBookingDetails TourDetails { get; set; } = new();
    
    [Required]
    [EmailAddress]
    public string ContactEmail { get; set; } = string.Empty;
    
    [Required]
    public string ContactPhone { get; set; } = string.Empty;
    
    [MaxLength(255)]
    public string? ContactName { get; set; }
    
    [MaxLength(1000)]
    public string? SpecialRequests { get; set; }
    
    public UrgencyLevel Urgency { get; set; } = UrgencyLevel.Standard;
}

// 📄 VISA QUOTE REQUEST
public class VisaQuoteRequestDto
{
    [Required]
    public VisaBookingDetails VisaDetails { get; set; } = new();
    
    [Required]
    [EmailAddress]
    public string ContactEmail { get; set; } = string.Empty;
    
    [Required]
    public string ContactPhone { get; set; } = string.Empty;
    
    [MaxLength(255)]
    public string? ContactName { get; set; }
    
    [MaxLength(1000)]
    public string? SpecialRequests { get; set; }
    
    public UrgencyLevel Urgency { get; set; } = UrgencyLevel.Standard;
}

// 📦 COMPLETE PACKAGE QUOTE REQUEST
public class CompletePackageQuoteRequestDto
{
    [Required]
    public CompletePackageDetails PackageDetails { get; set; } = new();
    
    [Required]
    [EmailAddress]
    public string ContactEmail { get; set; } = string.Empty;
    
    [Required]
    public string ContactPhone { get; set; } = string.Empty;
    
    [MaxLength(255)]
    public string? ContactName { get; set; }
    
    [MaxLength(1000)]
    public string? SpecialRequests { get; set; }
    
    public UrgencyLevel Urgency { get; set; } = UrgencyLevel.Standard;
}

// 📋 QUOTE REQUEST RESPONSE
public class QuoteRequestResponse
{
    public bool Success { get; set; }
    public string Message { get; set; } = string.Empty;
    public string? ReferenceNumber { get; set; }
    public QuoteTrackingDto? Quote { get; set; }
}

// 📊 QUOTE TRACKING DTO
public class QuoteTrackingDto
{
    public int Id { get; set; }
    public string ReferenceNumber { get; set; } = string.Empty;
    public QuoteType ServiceType { get; set; }
    public QuoteStatus Status { get; set; }
    public string? Destination { get; set; }
    public decimal? QuotedAmount { get; set; }
    public string? Currency { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? TravelDate { get; set; }
    public DateTime? QuoteProvidedAt { get; set; }
    public DateTime? QuoteExpiresAt { get; set; }
    public string ContactEmail { get; set; } = string.Empty;
    public string ContactPhone { get; set; } = string.Empty;
    public string? ContactName { get; set; }
    public string? SpecialRequests { get; set; }
    public string? AdminNotes { get; set; }
    public UrgencyLevel Urgency { get; set; }
    public string? PaymentLinkUrl { get; set; }
    public List<QuoteStatusHistoryDto> StatusHistory { get; set; } = new();
}

// 📈 QUOTE STATUS HISTORY DTO
public class QuoteStatusHistoryDto
{
    public QuoteStatus FromStatus { get; set; }
    public QuoteStatus ToStatus { get; set; }
    public string? Notes { get; set; }
    public DateTime ChangedAt { get; set; }
    public string ChangedBy { get; set; } = string.Empty;
}

// 🔄 QUOTE LIST RESPONSE
public class QuoteListResponse
{
    public bool Success { get; set; }
    public string Message { get; set; } = string.Empty;
    public List<QuoteTrackingDto> Quotes { get; set; } = new();
    public int TotalCount { get; set; }
}

// 💰 ADMIN QUOTE PROVISION DTO
public class ProvideQuoteDto
{
    [Required]
    [Range(0.01, double.MaxValue, ErrorMessage = "Quote amount must be greater than 0")]
    public decimal QuotedAmount { get; set; }
    
    [MaxLength(3)]
    public string Currency { get; set; } = "GHS";
    
    [Required]
    public DateTime QuoteExpiresAt { get; set; }
    
    [Required]
    public string PaymentLinkUrl { get; set; } = string.Empty;
    
    [MaxLength(1000)]
    public string? AdminNotes { get; set; }
}