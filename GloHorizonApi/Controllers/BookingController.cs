using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using System.Text.Json;
using Akka.Actor;
using GloHorizonApi.Data;
using GloHorizonApi.Models.DomainModels;
using GloHorizonApi.Models.Dtos.Booking;
using GloHorizonApi.Actors;

namespace GloHorizonApi.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class BookingController : ControllerBase
{
    private readonly ApplicationDbContext _context;
    private readonly ILogger<BookingController> _logger;
    private readonly ActorSystem _actorSystem;

    public BookingController(
        ApplicationDbContext context,
        ILogger<BookingController> logger,
        ActorSystem actorSystem)
    {
        _context = context;
        _logger = logger;
        _actorSystem = actorSystem;
    }

    // ✈️ FLIGHT BOOKING SUBMISSION
    [HttpPost("flight")]
    public async Task<ActionResult<BookingSubmissionResponse>> SubmitFlightBooking([FromBody] FlightBookingSubmissionDto request)
    {
        return await ProcessBookingSubmission(
            BookingType.Flight,
            request.ContactEmail,
            request.ContactPhone,
            request.SpecialRequests,
            request.Urgency,
            flightDetails: request.FlightDetails,
            destination: request.FlightDetails.ArrivalCity,
            travelDate: request.FlightDetails.DepartureDate
        );
    }

    // 🏨 HOTEL BOOKING SUBMISSION
    [HttpPost("hotel")]
    public async Task<ActionResult<BookingSubmissionResponse>> SubmitHotelBooking([FromBody] HotelBookingSubmissionDto request)
    {
        return await ProcessBookingSubmission(
            BookingType.Hotel,
            request.ContactEmail,
            request.ContactPhone,
            request.SpecialRequests,
            request.Urgency,
            hotelDetails: request.HotelDetails,
            destination: request.HotelDetails.Destination,
            travelDate: request.HotelDetails.CheckInDate
        );
    }

    // 🗺️ TOUR BOOKING SUBMISSION
    [HttpPost("tour")]
    public async Task<ActionResult<BookingSubmissionResponse>> SubmitTourBooking([FromBody] TourBookingSubmissionDto request)
    {
        return await ProcessBookingSubmission(
            BookingType.Tour,
            request.ContactEmail,
            request.ContactPhone,
            request.SpecialRequests,
            request.Urgency,
            tourDetails: request.TourDetails,
            destination: request.TourDetails.Destination,
            travelDate: request.TourDetails.StartDate
        );
    }

    // 📄 VISA BOOKING SUBMISSION
    [HttpPost("visa")]
    public async Task<ActionResult<BookingSubmissionResponse>> SubmitVisaBooking([FromBody] VisaBookingSubmissionDto request)
    {
        return await ProcessBookingSubmission(
            BookingType.Visa,
            request.ContactEmail,
            request.ContactPhone,
            request.SpecialRequests,
            request.Urgency,
            visaDetails: request.VisaDetails,
            destination: request.VisaDetails.DestinationCountry,
            travelDate: request.VisaDetails.IntendedTravelDate
        );
    }

    // 📦 COMPLETE PACKAGE SUBMISSION
    [HttpPost("complete-package")]
    public async Task<ActionResult<BookingSubmissionResponse>> SubmitCompletePackage([FromBody] CompletePackageSubmissionDto request)
    {
        var destination = request.PackageDetails.FlightDetails?.ArrivalCity ?? 
                         request.PackageDetails.HotelDetails?.Destination ?? 
                         request.PackageDetails.VisaDetails?.DestinationCountry ?? "Multiple";
        
        var travelDate = request.PackageDetails.FlightDetails?.DepartureDate ?? 
                        request.PackageDetails.HotelDetails?.CheckInDate ?? 
                        request.PackageDetails.VisaDetails?.IntendedTravelDate ?? DateTime.Now.AddMonths(1);

        return await ProcessBookingSubmission(
            BookingType.CompletePackage,
            request.ContactEmail,
            request.ContactPhone,
            request.SpecialRequests,
            request.Urgency,
            packageDetails: request.PackageDetails,
            destination: destination,
            travelDate: travelDate
        );
    }

    // 📊 BOOKING TRACKING
    [HttpGet("track/{referenceNumber}")]
    public async Task<ActionResult<BookingTrackingDto>> TrackBooking(string referenceNumber)
    {
        try
        {
            var userId = User.FindFirst("Id")?.Value;
            if (string.IsNullOrEmpty(userId))
            {
                return Unauthorized(new { Success = false, Message = "Invalid authentication token" });
            }

            var booking = await _context.BookingRequests
                .Include(b => b.StatusHistory)
                .Include(b => b.Documents)
                .Include(b => b.User)
                .FirstOrDefaultAsync(b => b.ReferenceNumber == referenceNumber && b.UserId == userId);

            if (booking == null)
            {
                return NotFound(new { Success = false, Message = "Booking not found or access denied" });
            }

            return Ok(MapToTrackingDto(booking));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error tracking booking: {ReferenceNumber}", referenceNumber);
            return StatusCode(500, new { Success = false, Message = "Error retrieving booking information" });
        }
    }

    // 📋 GET USER'S BOOKINGS
    [HttpGet("my-bookings")]
    public async Task<ActionResult<BookingListResponse>> GetMyBookings()
    {
        try
        {
            var userId = User.FindFirst("Id")?.Value;
            if (string.IsNullOrEmpty(userId))
            {
                return Unauthorized(new BookingListResponse { Success = false, Message = "Invalid authentication token" });
            }

            var bookings = await _context.BookingRequests
                .Include(b => b.StatusHistory)
                .Include(b => b.Documents)
                .Where(b => b.UserId == userId)
                .OrderByDescending(b => b.CreatedAt)
                .ToListAsync();

            return Ok(new BookingListResponse
            {
                Success = true,
                Message = "Bookings retrieved successfully",
                Bookings = bookings.Select(MapToTrackingDto).ToList(),
                TotalCount = bookings.Count
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving user bookings");
            return StatusCode(500, new BookingListResponse { Success = false, Message = "Error retrieving bookings" });
        }
    }

    // ⚙️ PRIVATE HELPER METHODS
    private async Task<ActionResult<BookingSubmissionResponse>> ProcessBookingSubmission(
        BookingType serviceType,
        string contactEmail,
        string contactPhone,
        string? specialRequests,
        UrgencyLevel urgency,
        FlightBookingDetails? flightDetails = null,
        HotelBookingDetails? hotelDetails = null,
        TourBookingDetails? tourDetails = null,
        VisaBookingDetails? visaDetails = null,
        CompletePackageDetails? packageDetails = null,
        string? destination = null,
        DateTime? travelDate = null)
    {
        try
        {
            var userId = User.FindFirst("Id")?.Value;
            if (string.IsNullOrEmpty(userId))
            {
                return Unauthorized(new BookingSubmissionResponse
                {
                    Success = false,
                    Message = "Invalid authentication token"
                });
            }

            var user = await _context.Users.FindAsync(userId);
            if (user == null)
            {
                return Unauthorized(new BookingSubmissionResponse
                {
                    Success = false,
                    Message = "User not found"
                });
            }

            var referenceNumber = GenerateReferenceNumber(serviceType);

            var booking = new BookingRequest
            {
                ReferenceNumber = referenceNumber,
                UserId = userId,
                ServiceType = serviceType,
                Status = BookingStatus.Submitted,
                ContactEmail = contactEmail,
                ContactPhone = contactPhone,
                SpecialRequests = specialRequests,
                Urgency = urgency,
                Destination = destination,
                TravelDate = travelDate,
                CreatedAt = DateTime.UtcNow,
                
                // Store service-specific details as JSON
                FlightDetails = flightDetails != null ? JsonSerializer.Serialize(flightDetails) : null,
                HotelDetails = hotelDetails != null ? JsonSerializer.Serialize(hotelDetails) : null,
                TourDetails = tourDetails != null ? JsonSerializer.Serialize(tourDetails) : null,
                VisaDetails = visaDetails != null ? JsonSerializer.Serialize(visaDetails) : null,
                PackageDetails = packageDetails != null ? JsonSerializer.Serialize(packageDetails) : null
            };

            _context.BookingRequests.Add(booking);

            // Add initial status history
            var statusHistory = new BookingStatusHistory
            {
                BookingRequestId = booking.Id,
                FromStatus = BookingStatus.Submitted,
                ToStatus = BookingStatus.Submitted,
                Notes = $"{serviceType} booking request submitted",
                ChangedBy = "System",
                ChangedAt = DateTime.UtcNow
            };

            _context.BookingStatusHistories.Add(statusHistory);
            await _context.SaveChangesAsync();

            // Send notifications
            await SendBookingNotifications(booking, user);

            return Ok(new BookingSubmissionResponse
            {
                Success = true,
                Message = $"{serviceType} booking submitted successfully",
                ReferenceNumber = referenceNumber,
                Booking = MapToTrackingDto(booking)
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error submitting {ServiceType} booking", serviceType);
            return StatusCode(500, new BookingSubmissionResponse
            {
                Success = false,
                Message = $"Error submitting {serviceType} booking request"
            });
        }
    }

    private async Task SendBookingNotifications(BookingRequest booking, User user)
    {
        try
        {
            var bookingActor = _actorSystem.ActorSelection("/user/booking-notification-actor");
            var notificationMessage = new NewBookingMessage
            {
                ReferenceNumber = booking.ReferenceNumber,
                CustomerName = user.FullName,
                CustomerEmail = user.Email,
                CustomerPhone = user.PhoneNumber,
                ServiceType = booking.ServiceType, // No cast needed since both use BookingType now
                Urgency = booking.Urgency
            };
            
            bookingActor.Tell(notificationMessage);
            _logger.LogInformation($"Notification sent for booking: {booking.ReferenceNumber}");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"Failed to send notification for booking: {booking.ReferenceNumber}");
        }
    }

    private BookingTrackingDto MapToTrackingDto(BookingRequest booking)
    {
        return new BookingTrackingDto
        {
            Id = booking.Id,
            ReferenceNumber = booking.ReferenceNumber,
            ServiceType = booking.ServiceType,
            Status = booking.Status,
            Destination = booking.Destination,
            QuotedAmount = booking.QuotedAmount,
            FinalAmount = booking.FinalAmount,
            Currency = booking.Currency,
            CreatedAt = booking.CreatedAt,
            TravelDate = booking.TravelDate,
            ContactEmail = booking.ContactEmail,
            ContactPhone = booking.ContactPhone,
            SpecialRequests = booking.SpecialRequests,
            AdminNotes = booking.AdminNotes,
            Urgency = booking.Urgency,
            StatusHistory = booking.StatusHistory.Select(h => new BookingStatusHistoryDto
            {
                FromStatus = h.FromStatus,
                ToStatus = h.ToStatus,
                Notes = h.Notes,
                ChangedAt = h.ChangedAt,
                ChangedBy = h.ChangedBy
            }).OrderBy(h => h.ChangedAt).ToList(),
            Documents = booking.Documents.Select(d => new BookingDocumentDto
            {
                Id = d.Id,
                DocumentType = d.DocumentType,
                FileName = d.FileName,
                FileUrl = d.FileUrl,
                FileSize = d.FileSize,
                UploadedAt = d.UploadedAt,
                IsRequired = d.IsRequired,
                IsVerified = d.IsVerified
            }).ToList()
        };
    }

    private static string GenerateReferenceNumber(BookingType serviceType)
    {
        var prefix = serviceType switch
        {
            BookingType.Flight => "FL",
            BookingType.Hotel => "HT",
            BookingType.Tour => "TR",
            BookingType.Visa => "VS",
            BookingType.CompletePackage => "CP",
            _ => "BK"
        };

        var timestamp = DateTime.UtcNow.ToString("yyyyMMddHHmmss");
        var random = new Random().Next(1000, 9999);
        
        return $"GH{prefix}{timestamp}{random}";
    }
} 