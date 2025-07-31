using System.ComponentModel.DataAnnotations;
using GloHorizonApi.Models.DomainModels;

namespace GloHorizonApi.Models.Dtos.Booking;

// ✈️ FLIGHT BOOKING SUBMISSION
public class FlightBookingSubmissionDto
{
    [Required]
    public FlightBookingDetails FlightDetails { get; set; } = new();
    
    [Required]
    [EmailAddress]
    public string ContactEmail { get; set; } = string.Empty;
    
    [Required]
    [Phone]
    public string ContactPhone { get; set; } = string.Empty;
    
    [MaxLength(1000)]
    public string? SpecialRequests { get; set; }
    
    public UrgencyLevel Urgency { get; set; } = UrgencyLevel.Standard;
}

// 🏨 HOTEL BOOKING SUBMISSION
public class HotelBookingSubmissionDto
{
    [Required]
    public HotelBookingDetails HotelDetails { get; set; } = new();
    
    [Required]
    [EmailAddress]
    public string ContactEmail { get; set; } = string.Empty;
    
    [Required]
    [Phone]
    public string ContactPhone { get; set; } = string.Empty;
    
    [MaxLength(1000)]
    public string? SpecialRequests { get; set; }
    
    public UrgencyLevel Urgency { get; set; } = UrgencyLevel.Standard;
}

// 🗺️ TOUR BOOKING SUBMISSION
public class TourBookingSubmissionDto
{
    [Required]
    public TourBookingDetails TourDetails { get; set; } = new();
    
    [Required]
    [EmailAddress]
    public string ContactEmail { get; set; } = string.Empty;
    
    [Required]
    [Phone]
    public string ContactPhone { get; set; } = string.Empty;
    
    [MaxLength(1000)]
    public string? SpecialRequests { get; set; }
    
    public UrgencyLevel Urgency { get; set; } = UrgencyLevel.Standard;
}

// 📄 VISA BOOKING SUBMISSION
public class VisaBookingSubmissionDto
{
    [Required]
    public VisaBookingDetails VisaDetails { get; set; } = new();
    
    [Required]
    [EmailAddress]
    public string ContactEmail { get; set; } = string.Empty;
    
    [Required]
    [Phone]
    public string ContactPhone { get; set; } = string.Empty;
    
    [MaxLength(1000)]
    public string? SpecialRequests { get; set; }
    
    public UrgencyLevel Urgency { get; set; } = UrgencyLevel.Standard;
}

// 📦 COMPLETE PACKAGE SUBMISSION
public class CompletePackageSubmissionDto
{
    [Required]
    public CompletePackageDetails PackageDetails { get; set; } = new();
    
    [Required]
    [EmailAddress]
    public string ContactEmail { get; set; } = string.Empty;
    
    [Required]
    [Phone]
    public string ContactPhone { get; set; } = string.Empty;
    
    [MaxLength(1000)]
    public string? SpecialRequests { get; set; }
    
    public UrgencyLevel Urgency { get; set; } = UrgencyLevel.Standard;
}

// 📋 BOOKING RESPONSE
public class BookingSubmissionResponse
{
    public bool Success { get; set; }
    public string Message { get; set; } = string.Empty;
    public string? ReferenceNumber { get; set; }
    public BookingTrackingDto? Booking { get; set; }
}

// 📊 BOOKING TRACKING DTO
public class BookingTrackingDto
{
    public int Id { get; set; }
    public string ReferenceNumber { get; set; } = string.Empty;
    public BookingType ServiceType { get; set; }
    public BookingStatus Status { get; set; }
    public string? Destination { get; set; }
    public decimal? QuotedAmount { get; set; }
    public decimal? FinalAmount { get; set; }
    public string? Currency { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? TravelDate { get; set; }
    public string ContactEmail { get; set; } = string.Empty;
    public string ContactPhone { get; set; } = string.Empty;
    public string? SpecialRequests { get; set; }
    public string? AdminNotes { get; set; }
    public UrgencyLevel Urgency { get; set; }
    public List<BookingStatusHistoryDto> StatusHistory { get; set; } = new();
    public List<BookingDocumentDto> Documents { get; set; } = new();
}

// 📈 STATUS HISTORY DTO
public class BookingStatusHistoryDto
{
    public BookingStatus FromStatus { get; set; }
    public BookingStatus ToStatus { get; set; }
    public string? Notes { get; set; }
    public DateTime ChangedAt { get; set; }
    public string ChangedBy { get; set; } = string.Empty;
}

// 📄 DOCUMENT DTO
public class BookingDocumentDto
{
    public int Id { get; set; }
    public string DocumentType { get; set; } = string.Empty;
    public string FileName { get; set; } = string.Empty;
    public string FileUrl { get; set; } = string.Empty;
    public string FileSize { get; set; } = string.Empty;
    public DateTime UploadedAt { get; set; }
    public bool IsRequired { get; set; }
    public bool IsVerified { get; set; }
}

// 🔄 BOOKING LIST RESPONSE
public class BookingListResponse
{
    public bool Success { get; set; }
    public string Message { get; set; } = string.Empty;
    public List<BookingTrackingDto> Bookings { get; set; } = new();
    public int TotalCount { get; set; }
} 