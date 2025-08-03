using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using System.Text.Json;
using Akka.Actor;
using GloHorizonApi.Data;
using GloHorizonApi.Models.DomainModels;
using GloHorizonApi.Models.Dtos.Quote;
using GloHorizonApi.Actors;
using GloHorizonApi.Services;
using GloHorizonApi.Services.Interfaces;

namespace GloHorizonApi.Controllers;

[ApiController]
[Route("api/[controller]")]
public class QuoteController : ControllerBase
{
    private readonly ApplicationDbContext _context;
    private readonly ILogger<QuoteController> _logger;
    private readonly IActorRef _notificationActor;
    private readonly ISmsService _smsService;

    public QuoteController(
        ApplicationDbContext context,
        ILogger<QuoteController> logger,
        IActorRef notificationActor,
        ISmsService smsService)
    {
        _context = context;
        _logger = logger;
        _notificationActor = notificationActor;
        _smsService = smsService;
    }

    // ✈️ FLIGHT QUOTE REQUEST
    [HttpPost("flight")]
    [AllowAnonymous]
    public async Task<ActionResult<QuoteRequestResponse>> RequestFlightQuote([FromBody] FlightQuoteRequestDto request)
    {
        return await ProcessQuoteRequest(
            QuoteType.Flight,
            request.ContactEmail,
            request.ContactPhone,
            request.ContactName,
            request.SpecialRequests,
            request.Urgency,
            flightDetails: request.FlightDetails,
            destination: request.FlightDetails.ArrivalCity,
            travelDate: request.FlightDetails.DepartureDate
        );
    }

    // 🏨 HOTEL QUOTE REQUEST
    [HttpPost("hotel")]
    [AllowAnonymous]
    public async Task<ActionResult<QuoteRequestResponse>> RequestHotelQuote([FromBody] HotelQuoteRequestDto request)
    {
        _logger.LogInformation("Hotel quote request received");
        
        if (!ModelState.IsValid)
        {
            _logger.LogWarning("Model validation failed for hotel quote request: {ValidationErrors}", 
                string.Join(", ", ModelState.SelectMany(x => x.Value.Errors.Select(e => e.ErrorMessage))));
            
            return BadRequest(new QuoteRequestResponse 
            { 
                Success = false, 
                Message = "Invalid request data. Please check your input and try again." 
            });
        }
        
        _logger.LogInformation("Model validation passed, processing hotel quote request");
        
        return await ProcessQuoteRequest(
            QuoteType.Hotel,
            request.ContactEmail,
            request.ContactPhone,
            request.ContactName,
            request.SpecialRequests,
            request.Urgency,
            hotelDetails: request.HotelDetails,
            destination: request.HotelDetails.Destination,
            travelDate: request.HotelDetails.CheckInDate
        );
    }

    // 🗺️ TOUR QUOTE REQUEST
    [HttpPost("tour")]
    [AllowAnonymous]
    public async Task<ActionResult<QuoteRequestResponse>> RequestTourQuote([FromBody] TourQuoteRequestDto request)
    {
        return await ProcessQuoteRequest(
            QuoteType.Tour,
            request.ContactEmail,
            request.ContactPhone,
            request.ContactName,
            request.SpecialRequests,
            request.Urgency,
            tourDetails: request.TourDetails,
            destination: request.TourDetails.Destination,
            travelDate: request.TourDetails.StartDate
        );
    }

    // 📄 VISA QUOTE REQUEST
    [HttpPost("visa")]
    [AllowAnonymous]
    public async Task<ActionResult<QuoteRequestResponse>> RequestVisaQuote([FromBody] VisaQuoteRequestDto request)
    {
        return await ProcessQuoteRequest(
            QuoteType.Visa,
            request.ContactEmail,
            request.ContactPhone,
            request.ContactName,
            request.SpecialRequests,
            request.Urgency,
            visaDetails: request.VisaDetails,
            destination: request.VisaDetails.DestinationCountry,
            travelDate: request.VisaDetails.IntendedTravelDate
        );
    }

    // 📦 COMPLETE PACKAGE QUOTE REQUEST
    [HttpPost("complete-package")]
    [AllowAnonymous]
    public async Task<ActionResult<QuoteRequestResponse>> RequestCompletePackageQuote([FromBody] CompletePackageQuoteRequestDto request)
    {
        var destination = request.PackageDetails.FlightDetails?.ArrivalCity ?? 
                         request.PackageDetails.HotelDetails?.Destination ?? 
                         request.PackageDetails.VisaDetails?.DestinationCountry ?? "Multiple";
        
        var travelDate = request.PackageDetails.FlightDetails?.DepartureDate ?? 
                        request.PackageDetails.HotelDetails?.CheckInDate ?? 
                        request.PackageDetails.VisaDetails?.IntendedTravelDate ?? DateTime.Now.AddMonths(1);

        return await ProcessQuoteRequest(
            QuoteType.CompletePackage,
            request.ContactEmail,
            request.ContactPhone,
            request.ContactName,
            request.SpecialRequests,
            request.Urgency,
            packageDetails: request.PackageDetails,
            destination: destination,
            travelDate: travelDate
        );
    }

    // 🧪 TEST ENDPOINT
    [HttpPost("test")]
    [AllowAnonymous]
    public async Task<ActionResult> TestEndpoint([FromBody] object data)
    {
        _logger.LogInformation("Test endpoint hit successfully");
        return Ok(new { success = true, message = "Test endpoint working", data = data?.ToString() });
    }

    // 🧪 MINIMAL QUOTE TEST
    [HttpPost("test-minimal")]
    [AllowAnonymous]
    public async Task<ActionResult<QuoteRequestResponse>> TestMinimalQuote()
    {
        try
        {
            _logger.LogInformation("Testing minimal quote creation");
            
            var refNumber = "QHT" + DateTime.UtcNow.ToString("HHmmssfff") + new Random().Next(10, 99);
            var quote = new QuoteRequest
            {
                ReferenceNumber = refNumber,
                ServiceType = QuoteType.Hotel,
                Status = QuoteStatus.Submitted,
                ContactEmail = "test@example.com",
                ContactPhone = "+233123456789",
                ContactName = "Test User",
                CreatedAt = DateTime.UtcNow,
                Urgency = UrgencyLevel.Standard,
                Destination = "Dubai",
                TravelDate = DateTime.Now.AddMonths(1)
            };

            _logger.LogInformation("Adding minimal quote to database");
            _context.QuoteRequests.Add(quote);
            await _context.SaveChangesAsync();
            _logger.LogInformation($"Minimal quote saved with ID: {quote.Id}");

            // Test notifications
            try
            {
                await SendQuoteNotifications(quote, null, quote.ContactEmail, quote.ContactPhone, quote.ContactName);
                _logger.LogInformation($"🎉 NOTIFICATIONS SENT SUCCESSFULLY for: {quote.ReferenceNumber}");
            }
            catch (Exception notificationEx)
            {
                _logger.LogError(notificationEx, $"Notification failed for: {quote.ReferenceNumber}");
            }

            return Ok(new QuoteRequestResponse
            {
                Success = true,
                Message = "Minimal quote created successfully with notifications!",
                ReferenceNumber = quote.ReferenceNumber,
                Quote = MapToTrackingDto(quote)
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error in minimal quote test: {ErrorMessage}", ex.Message);
            return StatusCode(500, new QuoteRequestResponse
            {
                Success = false,
                Message = $"Minimal quote test failed: {ex.Message}"
            });
        }
    }

    // 📊 QUOTE TRACKING (Public - anyone with reference number)
    [HttpGet("track/{referenceNumber}")]
    [AllowAnonymous]
    public async Task<ActionResult<QuoteTrackingDto>> TrackQuote(string referenceNumber)
    {
        try
        {
            var quote = await _context.QuoteRequests
                .Include(q => q.StatusHistory)
                .Include(q => q.User)
                .FirstOrDefaultAsync(q => q.ReferenceNumber == referenceNumber);

            if (quote == null)
            {
                return NotFound(new { Success = false, Message = "Quote not found" });
            }

            return Ok(MapToTrackingDto(quote));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error tracking quote: {ReferenceNumber}", referenceNumber);
            return StatusCode(500, new { Success = false, Message = "Error retrieving quote information" });
        }
    }

    // 📋 GET USER'S QUOTES (Authenticated users only)
    [HttpGet("my-quotes")]
    [Authorize]
    public async Task<ActionResult<QuoteListResponse>> GetMyQuotes()
    {
        try
        {
            var userId = User.FindFirst("Id")?.Value;
            if (string.IsNullOrEmpty(userId))
            {
                return Unauthorized(new QuoteListResponse { Success = false, Message = "Invalid authentication token" });
            }

            var quotes = await _context.QuoteRequests
                .Include(q => q.StatusHistory)
                .Where(q => q.UserId == userId)
                .OrderByDescending(q => q.CreatedAt)
                .ToListAsync();

            return Ok(new QuoteListResponse
            {
                Success = true,
                Message = "Quotes retrieved successfully",
                Quotes = quotes.Select(MapToTrackingDto).ToList(),
                TotalCount = quotes.Count
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving user quotes");
            return StatusCode(500, new QuoteListResponse { Success = false, Message = "Error retrieving quotes" });
        }
    }

    // ⚙️ PRIVATE HELPER METHODS
    private async Task<ActionResult<QuoteRequestResponse>> ProcessQuoteRequest(
        QuoteType serviceType,
        string contactEmail,
        string contactPhone,
        string? contactName,
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
            _logger.LogInformation($"Starting {serviceType} quote request processing for {contactEmail}");
            // Check if user is authenticated (optional for quotes)
            var userId = User.FindFirst("Id")?.Value;
            User? user = null;
            
            if (!string.IsNullOrEmpty(userId))
            {
                _logger.LogInformation($"Authenticated user detected: {userId}");
                // Authenticated user - fetch user details for better experience
                user = await _context.Users.FindAsync(userId);
                // If contactName not provided, use user's name
                contactName ??= user?.FullName;
            }
            else
            {
                _logger.LogInformation("Guest user request");
            }

            var referenceNumber = GenerateQuoteReferenceNumber(serviceType);
            _logger.LogInformation($"Generated reference number: {referenceNumber}");

            var quote = new QuoteRequest
            {
                ReferenceNumber = referenceNumber,
                UserId = userId, // Can be null for guest requests
                ServiceType = serviceType,
                Status = QuoteStatus.Submitted,
                ContactEmail = contactEmail,
                ContactPhone = contactPhone,
                ContactName = contactName,
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

            _logger.LogInformation($"Adding quote to database: {referenceNumber}");
            _context.QuoteRequests.Add(quote);
            
            _logger.LogInformation($"Saving quote to get ID for: {referenceNumber}");
            await _context.SaveChangesAsync();
            _logger.LogInformation($"Quote saved with ID: {quote.Id}");

            // Add initial status history (after quote is saved to get the ID)
            var statusHistory = new QuoteStatusHistory
            {
                QuoteRequestId = quote.Id,
                FromStatus = QuoteStatus.Submitted,
                ToStatus = QuoteStatus.Submitted,
                Notes = $"{serviceType} quote request submitted",
                ChangedBy = "System",
                ChangedAt = DateTime.UtcNow
            };

            _logger.LogInformation($"Adding status history for quote ID: {quote.Id}");
            _context.QuoteStatusHistories.Add(statusHistory);
            
            _logger.LogInformation($"Saving status history for: {referenceNumber}");
            await _context.SaveChangesAsync();
            _logger.LogInformation($"Database save successful for: {referenceNumber}");

            // Send instant notification to admin
            try
            {
                await SendQuoteNotifications(quote, user, contactEmail, contactPhone, contactName);
                _logger.LogInformation($"Quote notification sent for: {quote.ReferenceNumber}");
            }
            catch (Exception notificationEx)
            {
                _logger.LogError(notificationEx, $"Failed to send notifications for: {quote.ReferenceNumber}");
                // Continue execution - don't fail the quote request due to notification issues
            }

            return Ok(new QuoteRequestResponse
            {
                Success = true,
                Message = $"{serviceType} quote request submitted successfully. You will receive a quote within 24 hours.",
                ReferenceNumber = referenceNumber,
                Quote = MapToTrackingDto(quote)
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error submitting {ServiceType} quote request: {ErrorMessage} | StackTrace: {StackTrace}", 
                serviceType, ex.Message, ex.StackTrace);
            return StatusCode(500, new QuoteRequestResponse
            {
                Success = false,
                Message = $"Error submitting {serviceType} quote request. Please try again."
            });
        }
    }

    private async Task SendQuoteNotifications(QuoteRequest quote, User? user, string contactEmail, string contactPhone, string? contactName)
    {
        try
        {
            var message = new NewQuoteRequestMessage
            {
                ReferenceNumber = quote.ReferenceNumber,
                CustomerName = contactName ?? "Guest Customer",
                CustomerEmail = contactEmail,
                CustomerPhone = contactPhone,
                ServiceType = quote.ServiceType,
                Urgency = quote.Urgency,
                IsGuestRequest = user == null,
                CreatedAt = quote.CreatedAt
            };

            // Send message to notification actor
            _logger.LogInformation($"🚀 CONTROLLER: About to send message to notification actor for: {quote.ReferenceNumber}");
            _logger.LogInformation($"🚀 CONTROLLER: Actor reference is null: {_notificationActor == null}");
            _logger.LogInformation($"🚀 CONTROLLER: Message details - Phone: {message.CustomerPhone}, Email: {message.CustomerEmail}");
            
            _notificationActor.Tell(message);
            _logger.LogInformation($"🚀 CONTROLLER: Quote notification message sent to actor for: {quote.ReferenceNumber}");
            
            // BACKUP: Send direct SMS notifications to ensure delivery
            try
            {
                _logger.LogInformation($"🔄 BACKUP: Sending direct SMS notifications for {quote.ReferenceNumber}");
                
                // Send customer SMS
                var serviceTypeName = quote.ServiceType.ToString().Replace("CompletePackage", "Complete Package");
                var customerMsg = $"Hello {contactName ?? "Valued Customer"}! Your {serviceTypeName} quote request ({quote.ReferenceNumber}) has been received. We'll respond within 24hrs. Thank you for choosing Global Horizons Travel! 🌍";
                var customerSmsResult = await _smsService.SendSmsAsync(contactPhone, customerMsg);
                _logger.LogInformation($"🔄 BACKUP Customer SMS: Success={customerSmsResult.Success}, Error={customerSmsResult.Error}");
                
                // Send admin SMS
                var adminPhones = new[] { "0205078908", "0240464248" };
                foreach (var adminPhone in adminPhones)
                {
                    var adminMsg = $"NEW QUOTE: {contactName ?? "Guest"} requests {serviceTypeName} quote ({quote.ReferenceNumber}). Please respond within 24hrs! 🌍";
                    var adminSmsResult = await _smsService.SendSmsAsync(adminPhone, adminMsg);
                    _logger.LogInformation($"🔄 BACKUP Admin SMS to {adminPhone}: Success={adminSmsResult.Success}, Error={adminSmsResult.Error}");
                }
            }
            catch (Exception backupEx)
            {
                _logger.LogError(backupEx, "BACKUP SMS notifications failed");
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"Failed to send quote notification for: {quote.ReferenceNumber}");
        }
    }

    private QuoteTrackingDto MapToTrackingDto(QuoteRequest quote)
    {
        return new QuoteTrackingDto
        {
            Id = quote.Id,
            ReferenceNumber = quote.ReferenceNumber,
            ServiceType = quote.ServiceType,
            Status = quote.Status,
            Destination = quote.Destination,
            QuotedAmount = quote.QuotedAmount,
            Currency = quote.Currency,
            CreatedAt = quote.CreatedAt,
            TravelDate = quote.TravelDate,
            QuoteProvidedAt = quote.QuoteProvidedAt,
            QuoteExpiresAt = quote.QuoteExpiresAt,
            ContactEmail = quote.ContactEmail,
            ContactPhone = quote.ContactPhone,
            ContactName = quote.ContactName,
            SpecialRequests = quote.SpecialRequests,
            AdminNotes = quote.AdminNotes,
            Urgency = quote.Urgency,
            PaymentLinkUrl = quote.PaymentLinkUrl,
            StatusHistory = quote.StatusHistory.Select(h => new QuoteStatusHistoryDto
            {
                FromStatus = h.FromStatus,
                ToStatus = h.ToStatus,
                Notes = h.Notes,
                ChangedAt = h.ChangedAt,
                ChangedBy = h.ChangedBy
            }).OrderBy(h => h.ChangedAt).ToList()
        };
    }

    private static string GenerateQuoteReferenceNumber(QuoteType serviceType)
    {
        var prefix = serviceType switch
        {
            QuoteType.Flight => "QFL",
            QuoteType.Hotel => "QHT",
            QuoteType.Tour => "QTR",
            QuoteType.Visa => "QVS",
            QuoteType.CompletePackage => "QCP",
            _ => "QBK"
        };

        // Keep within 20 character limit - MaxLength constraint
        var timestamp = DateTime.UtcNow.ToString("HHmmssfff");
        var random = new Random().Next(10, 99);
        
        return $"{prefix}{timestamp}{random}";
    }
}