using System.ComponentModel.DataAnnotations;
using GloHorizonApi.Models.DomainModels;

namespace GloHorizonApi.Models.Dtos.Booking;

public class SubmitBookingRequest
{
    [Required]
    public ServiceType ServiceType { get; set; }
    
    [Required]
    public object BookingData { get; set; } = new();
    
    public UrgencyLevel Urgency { get; set; } = UrgencyLevel.Standard;
    
    [MaxLength(500)]
    public string? SpecialRequests { get; set; }
}

public class BookingResponse
{
    public bool Success { get; set; }
    public string Message { get; set; } = string.Empty;
    public string? ReferenceNumber { get; set; }
    public BookingInfo? Booking { get; set; }
}

public class BookingInfo
{
    public int Id { get; set; }
    public string ReferenceNumber { get; set; } = string.Empty;
    public ServiceType ServiceType { get; set; }
    public BookingStatus Status { get; set; }
    public UrgencyLevel Urgency { get; set; }
    public DateTime CreatedAt { get; set; }
    public decimal? EstimatedPrice { get; set; }
    public decimal? FinalPrice { get; set; }
    public string? Currency { get; set; }
} 