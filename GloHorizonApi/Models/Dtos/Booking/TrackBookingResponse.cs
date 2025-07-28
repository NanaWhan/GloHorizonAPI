using GloHorizonApi.Models.DomainModels;

namespace GloHorizonApi.Models.Dtos.Booking;

public class TrackBookingResponse
{
    public bool Success { get; set; }
    public string Message { get; set; } = string.Empty;
    public BookingTrackingInfo? Booking { get; set; }
}

public class BookingTrackingInfo
{
    public int Id { get; set; }
    public string ReferenceNumber { get; set; } = string.Empty;
    public ServiceType ServiceType { get; set; }
    public BookingStatus Status { get; set; }
    public UrgencyLevel Urgency { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
    public decimal? EstimatedPrice { get; set; }
    public decimal? FinalPrice { get; set; }
    public string? Currency { get; set; }
    public string? AdminNotes { get; set; }
    public List<StatusHistoryInfo> StatusHistory { get; set; } = new();
}

public class StatusHistoryInfo
{
    public BookingStatus FromStatus { get; set; }
    public BookingStatus ToStatus { get; set; }
    public string? Notes { get; set; }
    public DateTime ChangedAt { get; set; }
    public string ChangedBy { get; set; } = string.Empty;
} 