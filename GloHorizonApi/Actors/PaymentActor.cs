using Akka.Actor;
using Microsoft.EntityFrameworkCore;
using GloHorizonApi.Data;
using GloHorizonApi.Services.Interfaces;
using GloHorizonApi.Actors.Messages;
using GloHorizonApi.Models.DomainModels;

namespace GloHorizonApi.Actors;

public class PaymentActor : ReceiveActor
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<PaymentActor> _logger;
    private readonly ApplicationDbContext _db;
    private readonly ISmsService _smsService;
    private readonly IEmailService _emailService;
    private readonly IConfiguration _configuration;
    
    // Track processed payments to avoid duplicates
    private readonly HashSet<string> _processedPayments = new HashSet<string>();
    
    // Admin notification phone numbers
    private readonly string[] _adminNotificationPhones = { "0249058729", "0201234567" }; // Configure these

    public PaymentActor(
        IServiceProvider serviceProvider,
        ILogger<PaymentActor> logger,
        ApplicationDbContext db,
        ISmsService smsService,
        IEmailService emailService,
        IConfiguration configuration)
    {
        _serviceProvider = serviceProvider;
        _logger = logger;
        _db = db;
        _smsService = smsService;
        _emailService = emailService;
        _configuration = configuration;

        _logger.LogInformation("PaymentActor initialized and ready to receive messages");
        ReceiveAsync<PaymentCompletedMessage>(ProcessCompletedPayment);
    }

    private async Task ProcessCompletedPayment(PaymentCompletedMessage message)
    {
        _logger.LogInformation($"PaymentActor received message for transaction reference: {message.TransactionReference}");

        // Check if this payment has already been processed
        if (_processedPayments.Contains(message.TransactionReference))
        {
            _logger.LogInformation($"PaymentActor: Payment {message.TransactionReference} has already been processed. Skipping to avoid duplicate messages.");
            return;
        }

        try
        {
            // Find the booking request
            var booking = await _db.BookingRequests
                .Include(b => b.User)
                .AsNoTracking()
                .FirstOrDefaultAsync(b => b.ReferenceNumber == message.TransactionReference);

            if (booking == null)
            {
                _logger.LogWarning($"PaymentActor: Booking not found for reference: {message.TransactionReference}");
                return;
            }

            _logger.LogInformation($"PaymentActor: Found booking. Status: {booking.Status}, Amount: {booking.FinalPrice}, CustomerPhone: {booking.User.PhoneNumber ?? "None"}");

            // Mark as processed to avoid duplicates
            _processedPayments.Add(message.TransactionReference);

            // Update booking status to processing
            await UpdateBookingStatus(booking);

            _logger.LogInformation($"PaymentActor: Processing payment {message.TransactionReference} with parallel tasks");

            // Execute notification tasks in parallel
            var sendCustomerSmsTask = SendCustomerPaymentConfirmation(booking);
            var sendCustomerEmailTask = SendCustomerPaymentEmail(booking);
            var notifyAdminsTask = SendAdminPaymentNotifications(booking);
            var updateStatusHistoryTask = AddStatusHistoryEntry(booking);

            // Wait for all tasks to complete
            await Task.WhenAll(sendCustomerSmsTask, sendCustomerEmailTask, notifyAdminsTask, updateStatusHistoryTask);

            _logger.LogInformation($"PaymentActor: Successfully processed payment for {message.TransactionReference}");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"PaymentActor: Error processing payment for {message.TransactionReference}");
            
            // Remove from processed set so it can be retried
            _processedPayments.Remove(message.TransactionReference);
        }
    }

    private async Task UpdateBookingStatus(BookingRequest booking)
    {
        try
        {
            var bookingToUpdate = await _db.BookingRequests
                .FirstOrDefaultAsync(b => b.ReferenceNumber == booking.ReferenceNumber);

            if (bookingToUpdate != null && bookingToUpdate.Status == BookingStatus.PaymentPending)
            {
                bookingToUpdate.Status = BookingStatus.Processing;
                bookingToUpdate.UpdatedAt = DateTime.UtcNow;
                await _db.SaveChangesAsync();
                _logger.LogInformation($"PaymentActor: Updated booking status to Processing for {booking.ReferenceNumber}");
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"PaymentActor: Error updating booking status for {booking.ReferenceNumber}");
        }
    }

    private async Task SendCustomerPaymentConfirmation(BookingRequest booking)
    {
        try
        {
            if (string.IsNullOrEmpty(booking.User.PhoneNumber))
            {
                _logger.LogInformation($"PaymentActor: No phone number provided for booking: {booking.ReferenceNumber}, skipping SMS notification");
                return;
            }

            var message = $"Payment confirmed for your {booking.ServiceType} booking {booking.ReferenceNumber}. We're now processing your request. You'll receive updates via SMS and email.";
            
            var result = await _smsService.SendSmsAsync(booking.User.PhoneNumber, message);
            
            if (result.Success)
            {
                _logger.LogInformation($"PaymentActor: Payment confirmation SMS sent successfully for {booking.ReferenceNumber}");
            }
            else
            {
                _logger.LogWarning($"PaymentActor: Failed to send payment confirmation SMS for {booking.ReferenceNumber}. Error: {result.Error}");
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"PaymentActor: Error sending payment confirmation SMS for {booking.ReferenceNumber}");
        }
    }

    private async Task SendCustomerPaymentEmail(BookingRequest booking)
    {
        try
        {
            if (string.IsNullOrEmpty(booking.User.Email))
            {
                _logger.LogInformation($"PaymentActor: No email provided for booking: {booking.ReferenceNumber}, skipping email notification");
                return;
            }

            var subject = $"Payment Confirmed - {booking.ServiceType} Booking {booking.ReferenceNumber}";
            var message = $"Dear {booking.User.FirstName},\n\nYour payment for booking {booking.ReferenceNumber} has been confirmed. We're now processing your {booking.ServiceType} request.\n\nAmount: {booking.FinalPrice:C}\nService: {booking.ServiceType}\n\nYou'll receive further updates as we process your booking.\n\nThank you for choosing GloHorizon Travel!";
            
            var result = await _emailService.SendBookingStatusUpdateAsync(
                booking.User.Email,
                booking.User.FirstName + " " + booking.User.LastName,
                booking.ReferenceNumber,
                "Payment Confirmed - Processing",
                message
            );
            
            if (result.Success)
            {
                _logger.LogInformation($"PaymentActor: Payment confirmation email sent successfully for {booking.ReferenceNumber}");
            }
            else
            {
                _logger.LogWarning($"PaymentActor: Failed to send payment confirmation email for {booking.ReferenceNumber}. Error: {result.Error}");
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"PaymentActor: Error sending payment confirmation email for {booking.ReferenceNumber}");
        }
    }

    private async Task SendAdminPaymentNotifications(BookingRequest booking)
    {
        try
        {
            // Send email notifications to admin emails
            var adminEmails = _configuration.GetSection("AdminSettings:AdminEmails").Get<string[]>() ?? Array.Empty<string>();
            var emailTasks = adminEmails.Select(async adminEmail =>
            {
                var subject = $"Payment Received - {booking.ServiceType} Booking";
                var message = $"Payment confirmed for booking {booking.ReferenceNumber}\n" +
                             $"Customer: {booking.User.FirstName} {booking.User.LastName}\n" +
                             $"Email: {booking.User.Email}\n" +
                             $"Phone: {booking.User.PhoneNumber}\n" +
                             $"Service: {booking.ServiceType}\n" +
                             $"Amount: {booking.FinalPrice:C}\n" +
                             $"Urgency: {booking.Urgency}\n" +
                             $"Status: Processing";

                await _emailService.SendAdminNotificationAsync(adminEmail, subject, message);
            });

            // Send SMS notifications to admin phones
            var smsTasks = _adminNotificationPhones.Select(async adminPhone =>
            {
                var smsMessage = $"Payment received: {booking.ServiceType} booking {booking.ReferenceNumber} from {booking.User.FirstName} {booking.User.LastName}. Amount: {booking.FinalPrice:C}. Urgency: {booking.Urgency}";
                await _smsService.SendSmsAsync(adminPhone, smsMessage);
            });

            // Execute all admin notifications in parallel
            await Task.WhenAll(emailTasks.Concat(smsTasks));

            _logger.LogInformation($"PaymentActor: Admin notifications sent for payment {booking.ReferenceNumber}");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"PaymentActor: Error sending admin payment notifications for {booking.ReferenceNumber}");
        }
    }

    private async Task AddStatusHistoryEntry(BookingRequest booking)
    {
        try
        {
            var statusHistory = new BookingStatusHistory
            {
                BookingRequestId = booking.Id,
                FromStatus = BookingStatus.PaymentPending,
                ToStatus = BookingStatus.Processing,
                Notes = "Payment confirmed and booking moved to processing",
                ChangedBy = "PaymentActor",
                ChangedAt = DateTime.UtcNow
            };

            _db.BookingStatusHistories.Add(statusHistory);
            await _db.SaveChangesAsync();

            _logger.LogInformation($"PaymentActor: Status history entry added for {booking.ReferenceNumber}");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"PaymentActor: Error adding status history for {booking.ReferenceNumber}");
        }
    }

    public static Props Props(IServiceProvider serviceProvider, ILogger<PaymentActor> logger, ApplicationDbContext db, ISmsService smsService, IEmailService emailService, IConfiguration configuration) =>
        Akka.Actor.Props.Create(() => new PaymentActor(serviceProvider, logger, db, smsService, emailService, configuration));
}