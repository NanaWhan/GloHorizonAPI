using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using System.Text.Json;
using GloHorizonApi.Data;
using GloHorizonApi.Models.DomainModels;
using GloHorizonApi.Models.Dtos.Booking;

namespace GloHorizonApi.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class BookingController : ControllerBase
{
    private readonly ApplicationDbContext _context;
    private readonly ILogger<BookingController> _logger;

    public BookingController(
        ApplicationDbContext context,
        ILogger<BookingController> logger)
    {
        _context = context;
        _logger = logger;
    }

    [HttpPost("submit")]
    public async Task<ActionResult<BookingResponse>> SubmitBooking([FromBody] SubmitBookingRequest request)
    {
        try
        {
            // Get user ID from JWT token
            var userId = User.FindFirst("Id")?.Value;
            
            if (string.IsNullOrEmpty(userId))
            {
                return Unauthorized(new BookingResponse
                {
                    Success = false,
                    Message = "Invalid authentication token"
                });
            }

            // Verify user exists
            var user = await _context.Users.FindAsync(userId);
            if (user == null)
            {
                return Unauthorized(new BookingResponse
                {
                    Success = false,
                    Message = "User not found"
                });
            }

            // Generate unique reference number
            var referenceNumber = GenerateReferenceNumber(request.ServiceType);

            // Create booking request
            var booking = new BookingRequest
            {
                ReferenceNumber = referenceNumber,
                UserId = userId,
                ServiceType = request.ServiceType,
                Status = BookingStatus.Pending,
                BookingData = JsonSerializer.Serialize(request.BookingData),
                Urgency = request.Urgency,
                AdminNotes = request.SpecialRequests,
                CreatedAt = DateTime.UtcNow
            };

            _context.BookingRequests.Add(booking);

            // Add initial status history
            var statusHistory = new BookingStatusHistory
            {
                BookingRequestId = booking.Id,
                FromStatus = BookingStatus.Pending,
                ToStatus = BookingStatus.Pending,
                Notes = "Booking request submitted",
                ChangedBy = "System",
                ChangedAt = DateTime.UtcNow
            };

            _context.BookingStatusHistories.Add(statusHistory);
            await _context.SaveChangesAsync();

            // TODO: Send admin notifications (email/SMS)
            // This will be implemented with actor system
            _logger.LogInformation($"New booking submitted: {referenceNumber} by user {userId}");

            return Ok(new BookingResponse
            {
                Success = true,
                Message = "Booking request submitted successfully",
                ReferenceNumber = referenceNumber,
                Booking = new BookingInfo
                {
                    Id = booking.Id,
                    ReferenceNumber = booking.ReferenceNumber,
                    ServiceType = booking.ServiceType,
                    Status = booking.Status,
                    Urgency = booking.Urgency,
                    CreatedAt = booking.CreatedAt,
                    Currency = booking.Currency
                }
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error submitting booking request");
            return StatusCode(500, new BookingResponse
            {
                Success = false,
                Message = "An error occurred while submitting your booking request"
            });
        }
    }

    [HttpGet("track/{referenceNumber}")]
    public async Task<ActionResult<TrackBookingResponse>> TrackBooking(string referenceNumber)
    {
        try
        {
            // Get user ID from JWT token
            var userId = User.FindFirst("Id")?.Value;
            
            if (string.IsNullOrEmpty(userId))
            {
                return Unauthorized(new TrackBookingResponse
                {
                    Success = false,
                    Message = "Invalid authentication token"
                });
            }

            // Find booking by reference number and user ID for security
            var booking = await _context.BookingRequests
                .Include(b => b.StatusHistory)
                .FirstOrDefaultAsync(b => b.ReferenceNumber == referenceNumber && b.UserId == userId);

            if (booking == null)
            {
                return NotFound(new TrackBookingResponse
                {
                    Success = false,
                    Message = "Booking not found or you don't have access to this booking"
                });
            }

            return Ok(new TrackBookingResponse
            {
                Success = true,
                Message = "Booking found",
                Booking = new BookingTrackingInfo
                {
                    Id = booking.Id,
                    ReferenceNumber = booking.ReferenceNumber,
                    ServiceType = booking.ServiceType,
                    Status = booking.Status,
                    Urgency = booking.Urgency,
                    CreatedAt = booking.CreatedAt,
                    UpdatedAt = booking.UpdatedAt,
                    EstimatedPrice = booking.EstimatedPrice,
                    FinalPrice = booking.FinalPrice,
                    Currency = booking.Currency,
                    AdminNotes = booking.AdminNotes,
                    StatusHistory = booking.StatusHistory.Select(h => new StatusHistoryInfo
                    {
                        FromStatus = h.FromStatus,
                        ToStatus = h.ToStatus,
                        Notes = h.Notes,
                        ChangedAt = h.ChangedAt,
                        ChangedBy = h.ChangedBy
                    }).OrderBy(h => h.ChangedAt).ToList()
                }
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error tracking booking: {ReferenceNumber}", referenceNumber);
            return StatusCode(500, new TrackBookingResponse
            {
                Success = false,
                Message = "An error occurred while tracking your booking"
            });
        }
    }

    [HttpGet("my-bookings")]
    public async Task<ActionResult<List<BookingInfo>>> GetMyBookings()
    {
        try
        {
            // Get user ID from JWT token
            var userId = User.FindFirst("Id")?.Value;
            
            if (string.IsNullOrEmpty(userId))
            {
                return Unauthorized();
            }

            // Get all bookings for the user
            var bookings = await _context.BookingRequests
                .Where(b => b.UserId == userId)
                .OrderByDescending(b => b.CreatedAt)
                .Select(b => new BookingInfo
                {
                    Id = b.Id,
                    ReferenceNumber = b.ReferenceNumber,
                    ServiceType = b.ServiceType,
                    Status = b.Status,
                    Urgency = b.Urgency,
                    CreatedAt = b.CreatedAt,
                    EstimatedPrice = b.EstimatedPrice,
                    FinalPrice = b.FinalPrice,
                    Currency = b.Currency
                })
                .ToListAsync();

            return Ok(bookings);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving user bookings");
            return StatusCode(500, "An error occurred while retrieving your bookings");
        }
    }

    private static string GenerateReferenceNumber(ServiceType serviceType)
    {
        var prefix = serviceType switch
        {
            ServiceType.Flight => "FL",
            ServiceType.Hotel => "HT",
            ServiceType.Tour => "TR",
            ServiceType.Visa => "VS",
            ServiceType.CompletePackage => "CP",
            _ => "BK"
        };

        var timestamp = DateTime.UtcNow.ToString("yyyyMMddHHmmss");
        var random = new Random().Next(1000, 9999);
        
        return $"GH{prefix}{timestamp}{random}";
    }
} 