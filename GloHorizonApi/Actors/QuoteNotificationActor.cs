using Akka.Actor;
using GloHorizonApi.Services;
using GloHorizonApi.Services.Interfaces;
using GloHorizonApi.Models.DomainModels;

namespace GloHorizonApi.Actors;

public class QuoteNotificationActor : ReceiveActor
{
    private readonly ISmsService _smsService;
    private readonly IEmailService _emailService;
    private readonly IConfiguration _configuration;
    private readonly ILogger<QuoteNotificationActor> _logger;

    public QuoteNotificationActor(ActorNotificationServices services)
    {
        _smsService = services.SmsService;
        _emailService = services.EmailService;
        _configuration = services.Configuration;
        _logger = services.Logger;

        ReceiveAsync<NewQuoteRequestMessage>(Handle);
        ReceiveAsync<QuoteProvidedMessage>(Handle);
        ReceiveAsync<QuoteStatusUpdateMessage>(Handle);
    }

    private async Task Handle(NewQuoteRequestMessage message)
    {
        try
        {
            _logger.LogInformation($"🎯 ACTOR TRIGGERED: Processing new quote request - {message.ReferenceNumber}");
            _logger.LogInformation($"📞 Customer Phone: {message.CustomerPhone}");
            _logger.LogInformation($"📧 Customer Email: {message.CustomerEmail}");
            
            // Send personalized customer acknowledgment
            var customerName = !string.IsNullOrEmpty(message.CustomerName) ? message.CustomerName : "Valued Customer";
            var serviceTypeDisplay = message.ServiceType.ToString().Replace("CompletePackage", "Complete Package");
            
            _logger.LogInformation($"📧 Sending email confirmation to: {message.CustomerEmail}");
            await _emailService.SendBookingConfirmationAsync(
                message.CustomerEmail,
                customerName,
                message.ReferenceNumber,
                $"{serviceTypeDisplay} Quote Request"
            );

            // Personalized SMS confirmation
            var personalizedSmsMessage = $"Hello {customerName}! Your {serviceTypeDisplay} quote request ({message.ReferenceNumber}) has been received. We'll respond within 24hrs. Thank you for choosing Global Horizons Travel & Tour! 🌍";
            
            _logger.LogInformation($"📱 Sending SMS to customer: {message.CustomerPhone}");
            var smsResult = await _smsService.SendSmsAsync(
                message.CustomerPhone,
                personalizedSmsMessage
            );
            _logger.LogInformation($"📱 Customer SMS result: Success={smsResult.Success}, Error={smsResult.Error}");

            // Send INSTANT admin notifications for quote requests
            var adminEmails = _configuration.GetSection("AdminSettings:AdminEmails").Get<string[]>() ?? Array.Empty<string>();
            var adminPhones = _configuration.GetSection("AdminSettings:AdminPhones").Get<string[]>() ?? Array.Empty<string>();

            var urgencyFlag = message.Urgency == UrgencyLevel.Emergency ? "🚨 URGENT" : 
                             message.Urgency == UrgencyLevel.Urgent ? "⚡ HIGH PRIORITY" : "";

            var customerType = message.IsGuestRequest ? "Guest Customer" : "Registered User";

            foreach (var adminEmail in adminEmails)
            {
                await _emailService.SendAdminNotificationAsync(
                    adminEmail,
                    $"{urgencyFlag} New Quote Request - {serviceTypeDisplay} | {customerName}",
                    $"🎯 NEW QUOTE REQUEST RECEIVED {urgencyFlag}\n\n" +
                    $"📋 Reference: {message.ReferenceNumber}\n" +
                    $"🎫 Service: {serviceTypeDisplay}\n" +
                    $"👤 Customer: {customerName} ({customerType})\n" +
                    $"📧 Email: {message.CustomerEmail}\n" +
                    $"📞 Phone: {message.CustomerPhone}\n" +
                    $"⚡ Urgency: {message.Urgency}\n" +
                    $"🕒 Submitted: {message.CreatedAt:yyyy-MM-dd HH:mm:ss} UTC\n\n" +
                    $"🚀 ACTION REQUIRED: Please review and provide quote within 24 hours\n" +
                    $"💼 Login to admin dashboard to respond to {customerName} immediately\n" +
                    $"🌍 Help {customerName} plan their perfect {serviceTypeDisplay.ToLower()} experience with Global Horizons Travel & Tour!"
                );
            }

            _logger.LogInformation($"📱 Sending admin SMS alerts to {adminPhones.Length} numbers: [{string.Join(", ", adminPhones)}]");
            foreach (var adminPhone in adminPhones)
            {
                _logger.LogInformation($"📱 Sending admin SMS to: {adminPhone}");
                var adminSmsResult = await _smsService.SendAdminAlertAsync(
                    adminPhone,
                    $"{urgencyFlag} NEW QUOTE: {customerName} requests {serviceTypeDisplay} quote ({message.ReferenceNumber}). Please respond within 24hrs to secure this booking! 🌍"
                );
                _logger.LogInformation($"📱 Admin SMS to {adminPhone} result: Success={adminSmsResult.Success}, Error={adminSmsResult.Error}");
            }

            _logger.LogInformation($"Quote request notifications sent for {message.ReferenceNumber} ({message.Urgency} priority)");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error sending quote request notifications for {ReferenceNumber}", message.ReferenceNumber);
        }
    }

    private async Task Handle(QuoteProvidedMessage message)
    {
        try
        {
            var customerName = !string.IsNullOrEmpty(message.CustomerName) ? message.CustomerName : "Valued Customer";
            var serviceTypeDisplay = message.ServiceType.ToString().Replace("CompletePackage", "Complete Package");
            
            // Send personalized quote to customer with payment link
            var quoteEmailBody = $"Dear {customerName},\n\n" +
                               $"Great news! Your {serviceTypeDisplay} quote is ready! 🎉\n\n" +
                               $"📋 Reference: {message.ReferenceNumber}\n" +
                               $"💰 Quote Amount: {message.QuotedAmount:N2} {message.Currency}\n" +
                               $"🔗 Secure Payment Link: {message.PaymentLinkUrl}\n" +
                               $"⏰ Quote Expires: {message.QuoteExpiresAt:yyyy-MM-dd HH:mm}\n\n" +
                               (!string.IsNullOrEmpty(message.AdminNotes) ? $"📝 Special Notes: {message.AdminNotes}\n\n" : "") +
                               $"🌍 Thank you for choosing Global Horizons Travel & Tour! We're excited to help you create amazing memories.\n\n" +
                               $"Ready to proceed? Click the payment link above to secure your booking!\n" +
                               $"Questions? Reply to this email or call us anytime.";

            await _emailService.SendEmailAsync(
                message.CustomerEmail,
                $"🎉 {customerName}, Your {serviceTypeDisplay} Quote is Ready! - {message.ReferenceNumber}",
                quoteEmailBody
            );

            await _smsService.SendSmsAsync(
                message.CustomerPhone,
                $"Hello {customerName}! Your quote {message.ReferenceNumber} is ready: {message.QuotedAmount:N2} {message.Currency}. Check email for payment link. Book now to secure your spot! 🌍"
            );

            _logger.LogInformation($"Quote provided notifications sent for {message.ReferenceNumber}");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error sending quote provided notifications for {ReferenceNumber}", message.ReferenceNumber);
        }
    }

    private async Task Handle(QuoteStatusUpdateMessage message)
    {
        try
        {
            var customerName = !string.IsNullOrEmpty(message.CustomerName) ? message.CustomerName : "Valued Customer";
            
            await _emailService.SendBookingStatusUpdateAsync(
                message.CustomerEmail,
                customerName,
                message.ReferenceNumber,
                message.NewStatus.ToString(),
                message.AdminNotes
            );

            var statusEmoji = message.NewStatus switch
            {
                QuoteStatus.UnderReview => "🔍",
                QuoteStatus.QuoteProvided => "💰", 
                QuoteStatus.PaymentPending => "⏳",
                QuoteStatus.Paid => "✅",
                QuoteStatus.BookingConfirmed => "🎉",
                QuoteStatus.Expired => "⏰",
                QuoteStatus.Cancelled => "❌",
                _ => "📋"
            };

            var personalizedStatusMessage = $"Hello {customerName}! {statusEmoji} Your quote {message.ReferenceNumber} status: {message.NewStatus}. Global Horizons Travel & Tour is here for you! 🌍";
            await _smsService.SendSmsAsync(message.CustomerPhone, personalizedStatusMessage);

            _logger.LogInformation($"Quote status update notifications sent for {message.ReferenceNumber}");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error sending quote status update notifications for {ReferenceNumber}", message.ReferenceNumber);
        }
    }

    public static Props Props(ActorNotificationServices services) =>
        Akka.Actor.Props.Create(() => new QuoteNotificationActor(services));
}

// Quote Actor Messages
public class NewQuoteRequestMessage
{
    public string ReferenceNumber { get; set; } = string.Empty;
    public string CustomerName { get; set; } = string.Empty;
    public string CustomerEmail { get; set; } = string.Empty;
    public string CustomerPhone { get; set; } = string.Empty;
    public QuoteType ServiceType { get; set; }
    public UrgencyLevel Urgency { get; set; }
    public bool IsGuestRequest { get; set; }
    public DateTime CreatedAt { get; set; }
}

public class QuoteProvidedMessage
{
    public string ReferenceNumber { get; set; } = string.Empty;
    public string CustomerName { get; set; } = string.Empty;
    public string CustomerEmail { get; set; } = string.Empty;
    public string CustomerPhone { get; set; } = string.Empty;
    public QuoteType ServiceType { get; set; }
    public decimal QuotedAmount { get; set; }
    public string Currency { get; set; } = "GHS";
    public string PaymentLinkUrl { get; set; } = string.Empty;
    public DateTime QuoteExpiresAt { get; set; }
    public string? AdminNotes { get; set; }
}

public class QuoteStatusUpdateMessage
{
    public string ReferenceNumber { get; set; } = string.Empty;
    public string CustomerName { get; set; } = string.Empty;
    public string CustomerEmail { get; set; } = string.Empty;
    public string CustomerPhone { get; set; } = string.Empty;
    public QuoteStatus NewStatus { get; set; }
    public string? AdminNotes { get; set; }
}