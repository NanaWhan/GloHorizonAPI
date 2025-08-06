using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Text.Json;
using GloHorizonApi.Data;
using GloHorizonApi.Models.DomainModels;
using GloHorizonApi.Services.Interfaces;
using GloHorizonApi.Actors.Messages;
using GloHorizonApi.Extensions;
using Akka.Actor;

namespace GloHorizonApi.Controllers;

[ApiController]
[Route("api/[controller]")]
public class PaymentController : ControllerBase
{
    private readonly ApplicationDbContext _context;
    private readonly IPayStackPaymentService _payStackService;
    private readonly ILogger<PaymentController> _logger;
    private readonly ActorSystem _actorSystem;
    private readonly IConfiguration _configuration;

    public PaymentController(
        ApplicationDbContext context,
        IPayStackPaymentService payStackService,
        ILogger<PaymentController> logger,
        ActorSystem actorSystem,
        IConfiguration configuration)
    {
        _context = context;
        _payStackService = payStackService;
        _logger = logger;
        _actorSystem = actorSystem;
        _configuration = configuration;
    }

    private string GetFrontendBaseUrl()
    {
        // Determine if we're in development or production
        var isDevelopment = Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT") == "Development";
        
        return isDevelopment 
            ? _configuration.GetValue<string>("FrontendUrls:DevelopmentBaseUrl") ?? "http://localhost:3000"
            : _configuration.GetValue<string>("FrontendUrls:ProductionBaseUrl") ?? "https://yourfrontend.com";
    }

    /// <summary>
    /// PayStack webhook endpoint for payment notifications
    /// </summary>
    [HttpPost("webhook")]
    public async Task<IActionResult> PayStackWebhook()
    {
        _logger.LogInformation("PayStack webhook received");

        try
        {
            // Read the raw body
            using var reader = new StreamReader(Request.Body);
            var rawBody = await reader.ReadToEndAsync();

            _logger.LogInformation($"Webhook raw body: {rawBody}");

            if (string.IsNullOrEmpty(rawBody))
            {
                _logger.LogWarning("Empty webhook body received");
                return BadRequest("Empty body");
            }

            // Parse the webhook data
            var paymentInfo = JsonSerializer.Deserialize<PayStackWebhookDto>(rawBody);

            _logger.LogInformation($"Parsed webhook data. Event: {paymentInfo?.Event}, Reference: {paymentInfo?.Data?.Reference}, Status: {paymentInfo?.Data?.Status}");

            if (paymentInfo?.Data?.Reference != null)
            {
                string reference = paymentInfo.Data.Reference;

                // Find the booking by reference number
                var booking = await _context.BookingRequests
                    .Include(b => b.User)
                    .FirstOrDefaultAsync(b => b.ReferenceNumber == reference);

                if (booking == null)
                {
                    _logger.LogWarning($"Booking not found for reference: {reference}");
                    return Ok(); // Return OK to acknowledge webhook
                }

                _logger.LogInformation($"Found booking for reference: {reference}. Current status: {booking.Status}");

                // Process payment success
                if (paymentInfo.Event == "charge.success")
                {
                    _logger.LogInformation($"Processing successful payment for reference: {reference}");

                    // Update booking status to payment confirmed
                    if (booking.Status != BookingStatus.Processing)
                    {
                        var oldStatus = booking.Status;
                        booking.Status = BookingStatus.Processing;
                        booking.UpdatedAt = DateTime.UtcNow;

                        // Add status history
                        var statusHistory = new BookingStatusHistory
                        {
                            BookingRequestId = booking.Id,
                            FromStatus = oldStatus,
                            ToStatus = BookingStatus.Processing,
                            Notes = "Payment confirmed via PayStack webhook",
                            ChangedBy = "PayStack Webhook",
                            ChangedAt = DateTime.UtcNow
                        };

                        _context.BookingStatusHistories.Add(statusHistory);
                        await _context.SaveChangesAsync();

                        _logger.LogInformation($"Booking status updated to Processing for reference: {reference}");

                        // Send payment completion message to actor system
                        var paymentActor = _actorSystem.ActorSelection("/user/payment-actor");
                        var paymentMessage = new PaymentCompletedMessage(
                            reference,
                            booking.FinalAmount ?? 0,
                            booking.User.PhoneNumber ?? ""
                        );

                        paymentActor.Tell(paymentMessage);
                        _logger.LogInformation($"Sent payment completion message to actor system for: {reference}");
                    }
                    else
                    {
                        _logger.LogInformation($"Booking {reference} already in Processing status, skipping update");
                    }
                }
                else if (paymentInfo.Event == "charge.failed")
                {
                    _logger.LogInformation($"Processing failed payment for reference: {reference}");
                    
                    // Update booking status to failed if currently pending payment
                    if (booking.Status == BookingStatus.PaymentPending)
                    {
                        booking.Status = BookingStatus.Cancelled;
                        booking.UpdatedAt = DateTime.UtcNow;

                        // Add status history
                        var statusHistory = new BookingStatusHistory
                        {
                            BookingRequestId = booking.Id,
                            FromStatus = BookingStatus.PaymentPending,
                            ToStatus = BookingStatus.Cancelled,
                            Notes = "Payment failed via PayStack webhook",
                            ChangedBy = "PayStack Webhook",
                            ChangedAt = DateTime.UtcNow
                        };

                        _context.BookingStatusHistories.Add(statusHistory);
                        await _context.SaveChangesAsync();

                        _logger.LogInformation($"Booking status updated to Cancelled for failed payment: {reference}");
                    }
                }
                else
                {
                    _logger.LogInformation($"Received unhandled event type: {paymentInfo.Event} for reference: {reference}");
                }
            }
            else
            {
                _logger.LogWarning("No reference found in webhook data");
            }

            return Ok();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing PayStack webhook");
            return StatusCode(500, "Internal server error");
        }
    }

    /// <summary>
    /// PayStack callback endpoint - where users are redirected after payment
    /// </summary>
    [HttpGet("callback")]
    public async Task<IActionResult> PaymentCallback([FromQuery] string reference, [FromQuery] string? trxref)
    {
        try
        {
            _logger.LogInformation("Payment callback received for reference: {Reference}", reference);

            var frontendBaseUrl = GetFrontendBaseUrl();

            if (string.IsNullOrEmpty(reference))
            {
                _logger.LogWarning("Payment callback received without reference");
                return Redirect($"{frontendBaseUrl}/payment/error?reason=missing-reference");
            }

            // Verify payment with PayStack first
            var verification = await _payStackService.VerifyTransactionAsync(reference);

            if (verification == null || !verification.Status)
            {
                _logger.LogWarning("Payment verification failed for reference: {Reference}", reference);
                return Redirect($"{frontendBaseUrl}/payment/error?ref={reference}&reason=verification-failed");
            }

            if (verification.Data.Status == "success")
            {
                _logger.LogInformation("Payment successful for reference: {Reference}", reference);

                // Try to find related booking (optional - payment might be standalone)
                var booking = await _context.BookingRequests
                    .Include(b => b.User)
                    .FirstOrDefaultAsync(b => b.ReferenceNumber == reference);

                if (booking != null)
                {
                    // Payment is linked to a booking - redirect with booking details
                    return Redirect($"{frontendBaseUrl}/payment/success?ref={reference}&type=booking&service={booking.ServiceType}&customer={Uri.EscapeDataString(booking.User.FullName)}&amount={verification.Data.Amount / 100}");
                }
                else
                {
                    // Standalone payment - redirect with payment details only
                    return Redirect($"{frontendBaseUrl}/payment/success?ref={reference}&type=payment&amount={verification.Data.Amount / 100}&customer={Uri.EscapeDataString(verification.Data.Customer.Email)}");
                }
            }
            else
            {
                _logger.LogWarning("Payment verification failed or payment was unsuccessful for reference: {Reference}", reference);
                
                // Redirect to failure page
                return Redirect($"{frontendBaseUrl}/payment/failed?ref={reference}&reason=payment-failed");
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing payment callback for reference: {Reference}", reference);
            // Redirect to error page
            var errorFrontendUrl = GetFrontendBaseUrl();
            return Redirect($"{errorFrontendUrl}/payment/error?reason=server-error&ref={reference}");
        }
    }

    /// <summary>
    /// Initialize a payment (create payment link)
    /// </summary>
    [HttpPost("initialize")]
    public async Task<IActionResult> InitializePayment([FromBody] PaymentInitializeRequest request)
    {
        try
        {
            _logger.LogInformation("Payment initialization requested for amount: {Amount}, reference: {Reference}", 
                request.Amount, request.ClientReference);

            // Clear model state to avoid validation issues
            ModelState.Clear();

            if (request.Amount <= 0)
            {
                return BadRequest(new { Success = false, Message = "Amount must be greater than 0" });
            }

            if (string.IsNullOrEmpty(request.ClientReference))
            {
                return BadRequest(new { Success = false, Message = "Client reference is required" });
            }

            // Get user from token
            var userId = User.FindFirst("Id")?.Value;
            if (string.IsNullOrEmpty(userId))
            {
                return Unauthorized(new { Success = false, Message = "Invalid authentication token" });
            }

            var user = await _context.Users.FindAsync(userId);
            if (user == null)
            {
                return Unauthorized(new { Success = false, Message = "User not found" });
            }

            // Create generic payment request
            var paymentRequest = new Models.Dtos.Payment.GenericPaymentRequest
            {
                Amount = request.Amount,
                ClientReference = request.ClientReference,
                TicketName = request.TicketName,
                User = user
            };

            // Create payment link using PayStack service
            var paymentResult = await _payStackService.CreatePayLink(paymentRequest);

            if (paymentResult.Status)
            {
                _logger.LogInformation("Payment link created successfully for reference: {Reference}", request.ClientReference);
                
                return Ok(new
                {
                    Success = true,
                    Message = "Payment link created successfully",
                    Data = new
                    {
                        AuthorizationUrl = paymentResult.Data?.AuthorizationUrl,
                        Reference = paymentResult.Data?.Reference,
                        Amount = request.Amount,
                        AccessCode = paymentResult.Data?.AccessCode
                    }
                });
            }
            else
            {
                _logger.LogError("Failed to create payment link: {Message}", paymentResult.Message);
                return BadRequest(new 
                { 
                    Success = false, 
                    Message = paymentResult.Message ?? "Failed to create payment link" 
                });
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error initializing payment for reference: {Reference}", request.ClientReference);
            return StatusCode(500, new 
            { 
                Success = false, 
                Message = "Internal server error during payment initialization" 
            });
        }
    }

    /// <summary>
    /// Manual payment verification endpoint
    /// </summary>
    [HttpPost("verify/{reference}")]
    public async Task<IActionResult> VerifyPayment(string reference)
    {
        try
        {
            _logger.LogInformation($"Manual payment verification requested for reference: {reference}");

            // Find the booking
            var booking = await _context.BookingRequests
                .Include(b => b.User)
                .FirstOrDefaultAsync(b => b.ReferenceNumber == reference);

            if (booking == null)
            {
                return NotFound($"Booking not found for reference: {reference}");
            }

            _logger.LogInformation($"Found booking for reference: {reference}. Current status: {booking.Status}");

            // Verify with PayStack
            var verification = await _payStackService.VerifyTransactionAsync(reference);

            if (verification != null && verification.Status)
            {
                if (verification.Data.Status == "success" && booking.Status != BookingStatus.Processing)
                {
                    _logger.LogInformation($"Payment verified as successful. Updating booking status for reference: {reference}");

                    var oldStatus = booking.Status;
                    booking.Status = BookingStatus.Processing;
                    booking.UpdatedAt = DateTime.UtcNow;

                    // Add status history
                    var statusHistory = new BookingStatusHistory
                    {
                        BookingRequestId = booking.Id,
                        FromStatus = oldStatus,
                        ToStatus = BookingStatus.Processing,
                        Notes = "Payment verified manually via admin portal",
                        ChangedBy = "Manual Verification",
                        ChangedAt = DateTime.UtcNow
                    };

                    _context.BookingStatusHistories.Add(statusHistory);
                    await _context.SaveChangesAsync();

                    // Send payment completion message to actor system
                    var paymentActor = _actorSystem.ActorSelection("/user/payment-actor");
                    var paymentMessage = new PaymentCompletedMessage(
                        reference,
                        booking.FinalAmount ?? 0,
                        booking.User.PhoneNumber ?? ""
                    );

                    paymentActor.Tell(paymentMessage);
                    _logger.LogInformation($"Payment verification successful for: {reference}");

                    return Ok(new { 
                        Success = true, 
                        Message = "Payment verified and booking updated", 
                        Status = booking.Status.ToString(),
                        PaymentStatus = verification.Data.Status 
                    });
                }
                else
                {
                    return Ok(new { 
                        Success = true, 
                        Message = "Payment status retrieved", 
                        Status = booking.Status.ToString(),
                        PaymentStatus = verification.Data.Status 
                    });
                }
            }
            else
            {
                _logger.LogWarning($"Failed to verify payment for reference: {reference}");
                return BadRequest(new { 
                    Success = false, 
                    Message = "Failed to verify payment with PayStack" 
                });
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"Error verifying payment for reference: {reference}");
            return StatusCode(500, new { 
                Success = false, 
                Message = "Internal server error during payment verification" 
            });
        }
    }

    /// <summary>
    /// Test callback endpoint - simulates PayStack callback for testing
    /// </summary>
    [HttpGet("test-callback")]
    public async Task<IActionResult> TestCallback([FromQuery] string reference, [FromQuery] string status = "success")
    {
        try
        {
            _logger.LogInformation("Test callback triggered for reference: {Reference} with status: {Status}", reference, status);

            if (string.IsNullOrEmpty(reference))
            {
                return BadRequest(new { Success = false, Message = "Reference is required" });
            }

            // Simulate PayStack callback by redirecting to our actual callback endpoint
            var callbackUrl = $"{Request.Scheme}://{Request.Host}/api/payment/callback?reference={reference}&trxref={reference}&status={status}";
            
            _logger.LogInformation("Redirecting to callback URL: {CallbackUrl}", callbackUrl);
            
            return Redirect(callbackUrl);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error in test callback for reference: {Reference}", reference);
            return StatusCode(500, new { Success = false, Message = "Internal server error" });
        }
    }

    /// <summary>
    /// Get callback configuration for frontend
    /// </summary>
    [HttpGet("callback-config")]
    public IActionResult GetCallbackConfig()
    {
        try
        {
            var config = new
            {
                CallbackUrl = $"{Request.Scheme}://{Request.Host}/api/payment/callback",
                WebhookUrl = $"{Request.Scheme}://{Request.Host}/api/payment/webhook",
                FrontendUrls = new
                {
                    Success = $"{GetFrontendBaseUrl()}/payment/success",
                    Failed = $"{GetFrontendBaseUrl()}/payment/failed",
                    Error = $"{GetFrontendBaseUrl()}/payment/error"
                }
            };

            return Ok(new { Success = true, Data = config });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting callback configuration");
            return StatusCode(500, new { Success = false, Message = "Internal server error" });
        }
    }
}

// PayStack Webhook DTOs
public class PayStackWebhookDto
{
    public string Event { get; set; } = string.Empty;
    public PayStackWebhookData? Data { get; set; }
}

public class PayStackWebhookData
{
    public string Reference { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public decimal Amount { get; set; }
    public string Currency { get; set; } = string.Empty;
}

public class PaymentInitializeRequest
{
    public decimal Amount { get; set; }
    public string ClientReference { get; set; } = string.Empty;
    public string TicketName { get; set; } = string.Empty;
} 