using Akka.Actor;
using GloHorizonApi.Services.Interfaces;
using GloHorizonApi.Models.DomainModels;

namespace GloHorizonApi.Actors;

public class BookingNotificationActor : ReceiveActor
{
    private readonly ISmsService _smsService;
    private readonly IEmailService _emailService;
    private readonly IConfiguration _configuration;
    private readonly ILogger<BookingNotificationActor> _logger;

    public BookingNotificationActor(
        ISmsService smsService,
        IEmailService emailService,
        IConfiguration configuration,
        ILogger<BookingNotificationActor> logger)
    {
        _smsService = smsService;
        _emailService = emailService;
        _configuration = configuration;
        _logger = logger;

        ReceiveAsync<NewBookingMessage>(Handle);
        ReceiveAsync<BookingStatusUpdateMessage>(Handle);
        ReceiveAsync<AdminAlertMessage>(Handle);
    }

    private async Task Handle(NewBookingMessage message)
    {
        try
        {
            // Send customer confirmation
            await _emailService.SendBookingConfirmationAsync(
                message.CustomerEmail,
                message.CustomerName,
                message.ReferenceNumber,
                message.ServiceType.ToString()
            );

            await _smsService.SendBookingNotificationAsync(
                message.CustomerPhone,
                message.ReferenceNumber,
                message.ServiceType.ToString()
            );

            // Send admin notifications
            var adminEmails = _configuration.GetSection("AdminSettings:AdminEmails").Get<string[]>() ?? Array.Empty<string>();
            var adminPhones = _configuration.GetSection("AdminSettings:AdminPhones").Get<string[]>() ?? Array.Empty<string>();

            foreach (var adminEmail in adminEmails)
            {
                await _emailService.SendAdminNotificationAsync(
                    adminEmail,
                    "New Booking Request",
                    $"New {message.ServiceType} booking request received.\n" +
                    $"Reference: {message.ReferenceNumber}\n" +
                    $"Customer: {message.CustomerName}\n" +
                    $"Email: {message.CustomerEmail}\n" +
                    $"Phone: {message.CustomerPhone}\n" +
                    $"Urgency: {message.Urgency}"
                );
            }

            foreach (var adminPhone in adminPhones)
            {
                await _smsService.SendAdminAlertAsync(
                    adminPhone,
                    $"New {message.ServiceType} booking: {message.ReferenceNumber} from {message.CustomerName}. Urgency: {message.Urgency}"
                );
            }

            _logger.LogInformation($"Booking notifications sent for {message.ReferenceNumber}");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error sending booking notifications for {ReferenceNumber}", message.ReferenceNumber);
        }
    }

    private async Task Handle(BookingStatusUpdateMessage message)
    {
        try
        {
            await _emailService.SendBookingStatusUpdateAsync(
                message.CustomerEmail,
                message.CustomerName,
                message.ReferenceNumber,
                message.NewStatus.ToString(),
                message.AdminNotes
            );

            var statusMessage = $"Your booking {message.ReferenceNumber} status has been updated to: {message.NewStatus}";
            await _smsService.SendSmsAsync(message.CustomerPhone, statusMessage);

            _logger.LogInformation($"Status update notifications sent for {message.ReferenceNumber}");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error sending status update notifications for {ReferenceNumber}", message.ReferenceNumber);
        }
    }

    private async Task Handle(AdminAlertMessage message)
    {
        try
        {
            var adminEmails = _configuration.GetSection("AdminSettings:AdminEmails").Get<string[]>() ?? Array.Empty<string>();
            var adminPhones = _configuration.GetSection("AdminSettings:AdminPhones").Get<string[]>() ?? Array.Empty<string>();

            foreach (var adminEmail in adminEmails)
            {
                await _emailService.SendAdminNotificationAsync(adminEmail, message.Subject, message.Message);
            }

            foreach (var adminPhone in adminPhones)
            {
                await _smsService.SendAdminAlertAsync(adminPhone, message.Message);
            }

            _logger.LogInformation($"Admin alert sent: {message.Subject}");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error sending admin alert: {Subject}", message.Subject);
        }
    }

    public static Props Props(ISmsService smsService, IEmailService emailService, IConfiguration configuration, ILogger<BookingNotificationActor> logger) =>
        Akka.Actor.Props.Create(() => new BookingNotificationActor(smsService, emailService, configuration, logger));
}

// Actor Messages
public class NewBookingMessage
{
    public string ReferenceNumber { get; set; } = string.Empty;
    public string CustomerName { get; set; } = string.Empty;
    public string CustomerEmail { get; set; } = string.Empty;
    public string CustomerPhone { get; set; } = string.Empty;
    public BookingType ServiceType { get; set; }  // Updated to use BookingType
    public UrgencyLevel Urgency { get; set; }
}

public class BookingStatusUpdateMessage
{
    public string ReferenceNumber { get; set; } = string.Empty;
    public string CustomerName { get; set; } = string.Empty;
    public string CustomerEmail { get; set; } = string.Empty;
    public string CustomerPhone { get; set; } = string.Empty;
    public BookingStatus NewStatus { get; set; }
    public string? AdminNotes { get; set; }
}

public class AdminAlertMessage
{
    public string Subject { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;
} 