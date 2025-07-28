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

    public PaymentController(
        ApplicationDbContext context,
        IPayStackPaymentService payStackService,
        ILogger<PaymentController> logger,
        ActorSystem actorSystem)
    {
        _context = context;
        _payStackService = payStackService;
        _logger = logger;
        _actorSystem = actorSystem;
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
                            booking.FinalPrice ?? 0,
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
                        booking.FinalPrice ?? 0,
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