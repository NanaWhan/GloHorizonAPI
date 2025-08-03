using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using GloHorizonApi.Data;
using GloHorizonApi.Models.DomainModels;
using GloHorizonApi.Models.Dtos.Newsletter;
using GloHorizonApi.Services.Interfaces;
using BCrypt.Net;

namespace GloHorizonApi.Controllers;

[ApiController]
[Route("api/[controller]")]
public class NewsletterController : ControllerBase
{
    private readonly ApplicationDbContext _context;
    private readonly ILogger<NewsletterController> _logger;
    private readonly ISmsService _smsService;

    public NewsletterController(
        ApplicationDbContext context,
        ILogger<NewsletterController> logger,
        ISmsService smsService)
    {
        _context = context;
        _logger = logger;
        _smsService = smsService;
    }

    /// <summary>
    /// Subscribe a phone number to SMS newsletter
    /// </summary>
    [HttpPost("subscribe")]
    [AllowAnonymous]
    public async Task<ActionResult<NewsletterSubscriptionResponse>> Subscribe([FromBody] NewsletterSubscriptionRequest request)
    {
        try
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(new NewsletterSubscriptionResponse
                {
                    Success = false,
                    Message = "Invalid phone number format. Please enter a valid Ghanaian phone number."
                });
            }

            // Format phone number to consistent format
            var formattedPhone = FormatPhoneNumber(request.PhoneNumber);
            
            _logger.LogInformation($"Newsletter subscription request for: {formattedPhone}");

            // Check if already subscribed
            var existingSubscriber = await _context.NewsletterSubscribers
                .FirstOrDefaultAsync(n => n.PhoneNumber == formattedPhone);

            if (existingSubscriber != null)
            {
                if (existingSubscriber.IsActive)
                {
                    return Ok(new NewsletterSubscriptionResponse
                    {
                        Success = true,
                        Message = "You are already subscribed to our travel updates!",
                        PhoneNumber = formattedPhone,
                        SubscribedAt = existingSubscriber.SubscribedAt
                    });
                }
                else
                {
                    // Re-activate subscription
                    existingSubscriber.IsActive = true;
                    existingSubscriber.SubscribedAt = DateTime.UtcNow;
                    existingSubscriber.UnsubscribedAt = null;
                    await _context.SaveChangesAsync();
                    
                    // Send reactivation SMS
                    await SendWelcomeSms(formattedPhone, true);
                    
                    return Ok(new NewsletterSubscriptionResponse
                    {
                        Success = true,
                        Message = "Welcome back! You've been re-subscribed to Global Horizons travel updates.",
                        PhoneNumber = formattedPhone,
                        SubscribedAt = existingSubscriber.SubscribedAt
                    });
                }
            }

            // Create new subscription
            var newSubscriber = new NewsletterSubscriber
            {
                PhoneNumber = formattedPhone,
                SubscribedAt = DateTime.UtcNow,
                IsActive = true,
                Source = request.Source ?? "API"
            };

            _context.NewsletterSubscribers.Add(newSubscriber);
            await _context.SaveChangesAsync();

            // Send welcome SMS
            await SendWelcomeSms(formattedPhone, false);
            
            // Send admin notification
            await SendAdminNotification(formattedPhone, request.Source);

            _logger.LogInformation($"Newsletter subscription successful for: {formattedPhone}");

            return Ok(new NewsletterSubscriptionResponse
            {
                Success = true,
                Message = "Successfully subscribed! You'll receive travel deals and updates via SMS.",
                PhoneNumber = formattedPhone,
                SubscribedAt = newSubscriber.SubscribedAt
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing newsletter subscription for: {PhoneNumber}", request.PhoneNumber);
            return StatusCode(500, new NewsletterSubscriptionResponse
            {
                Success = false,
                Message = "Unable to process subscription. Please try again later."
            });
        }
    }

    /// <summary>
    /// Unsubscribe a phone number from SMS newsletter
    /// </summary>
    [HttpPost("unsubscribe")]
    [AllowAnonymous]
    public async Task<ActionResult<NewsletterSubscriptionResponse>> Unsubscribe([FromBody] NewsletterUnsubscribeRequest request)
    {
        try
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(new NewsletterSubscriptionResponse
                {
                    Success = false,
                    Message = "Invalid phone number format."
                });
            }

            var formattedPhone = FormatPhoneNumber(request.PhoneNumber);
            
            var subscriber = await _context.NewsletterSubscribers
                .FirstOrDefaultAsync(n => n.PhoneNumber == formattedPhone && n.IsActive);

            if (subscriber == null)
            {
                return Ok(new NewsletterSubscriptionResponse
                {
                    Success = true,
                    Message = "Phone number not found in our subscription list."
                });
            }

            subscriber.IsActive = false;
            subscriber.UnsubscribedAt = DateTime.UtcNow;
            await _context.SaveChangesAsync();

            // Send confirmation SMS
            await _smsService.SendSmsAsync(formattedPhone, 
                "You've been unsubscribed from Global Horizons travel updates. To re-subscribe, visit our website. Thank you! 🌍");

            _logger.LogInformation($"Newsletter unsubscription successful for: {formattedPhone}");

            return Ok(new NewsletterSubscriptionResponse
            {
                Success = true,
                Message = "Successfully unsubscribed from travel updates."
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing newsletter unsubscription for: {PhoneNumber}", request.PhoneNumber);
            return StatusCode(500, new NewsletterSubscriptionResponse
            {
                Success = false,
                Message = "Unable to process unsubscription. Please try again later."
            });
        }
    }

    /// <summary>
    /// TEMPORARY: Simple test endpoint
    /// </summary>
    [HttpPost("test-admin")]
    [AllowAnonymous]
    public ActionResult TestAdmin()
    {
        return Ok(new { message = "Admin creation endpoint is working!" });
    }

    /// <summary>
    /// TEMPORARY: Create newsletter table and test admin (will be removed after setup)
    /// </summary>
    [HttpPost("setup-database")]
    [AllowAnonymous] // TEMPORARY - will be removed
    public async Task<ActionResult> SetupDatabase()
    {
        try
        {
            _logger.LogInformation("🔧 Creating newsletter database table...");
            
            await _context.Database.ExecuteSqlRawAsync(@"
                CREATE TABLE IF NOT EXISTS ""NewsletterSubscribers"" (
                    ""Id"" SERIAL PRIMARY KEY,
                    ""PhoneNumber"" TEXT NOT NULL,
                    ""SubscribedAt"" TIMESTAMP WITHOUT TIME ZONE NOT NULL,
                    ""IsActive"" BOOLEAN NOT NULL,
                    ""UnsubscribedAt"" TIMESTAMP WITHOUT TIME ZONE NULL,
                    ""Source"" TEXT NULL
                );
                
                CREATE UNIQUE INDEX IF NOT EXISTS ""IX_NewsletterSubscribers_PhoneNumber"" 
                ON ""NewsletterSubscribers"" (""PhoneNumber"");
                
                CREATE INDEX IF NOT EXISTS ""IX_NewsletterSubscribers_IsActive_SubscribedAt"" 
                ON ""NewsletterSubscribers"" (""IsActive"", ""SubscribedAt"");
                
                INSERT INTO ""__EFMigrationsHistory"" (""MigrationId"", ""ProductVersion"") 
                VALUES ('20250801234000_AddNewsletterSubscribers', '8.0.0')
                ON CONFLICT (""MigrationId"") DO NOTHING;
            ");
            
            _logger.LogInformation("✅ Newsletter database table created successfully!");
            
            // Also create test admin
            _logger.LogInformation("🔧 Creating test admin account...");
            
            var hashedPassword = BCrypt.Net.BCrypt.HashPassword("TestAdmin123!");
            
            await _context.Database.ExecuteSqlRawAsync(@"
                INSERT INTO ""Admins"" (
                    ""Id"", ""FullName"", ""Email"", ""PhoneNumber"", ""PasswordHash"", 
                    ""Role"", ""IsActive"", ""ReceiveEmailNotifications"", ""ReceiveSmsNotifications"", 
                    ""CreatedAt"", ""CreatedBy""
                ) VALUES (
                    {0}, 'Test Admin', 'admin@globalhorizons.com', '0205078908', {1},
                    1, true, true, true, NOW(), 'System Setup'
                ) ON CONFLICT (""Email"") DO NOTHING;
            ", Guid.NewGuid().ToString(), hashedPassword);
            
            _logger.LogInformation("✅ Test admin created successfully!");
            
            return Ok(new { 
                success = true, 
                message = "Newsletter table and test admin created successfully!",
                adminEmail = "admin@globalhorizons.com",
                adminPassword = "TestAdmin123!"
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "❌ Error creating database table and admin");
            return StatusCode(500, new { success = false, message = "Error creating database setup", error = ex.Message });
        }
    }

    /// <summary>
    /// Get newsletter subscription statistics (Admin only)
    /// </summary>
    [HttpGet("stats")]
    [Authorize] // Add proper admin authorization if needed
    public async Task<ActionResult> GetSubscriptionStats()
    {
        try
        {
            var totalSubscribers = await _context.NewsletterSubscribers.CountAsync(n => n.IsActive);
            var totalUnsubscribed = await _context.NewsletterSubscribers.CountAsync(n => !n.IsActive);
            var recentSubscriptions = await _context.NewsletterSubscribers
                .Where(n => n.IsActive && n.SubscribedAt >= DateTime.UtcNow.AddDays(-7))
                .CountAsync();

            return Ok(new
            {
                ActiveSubscribers = totalSubscribers,
                UnsubscribedCount = totalUnsubscribed,
                RecentSubscriptions = recentSubscriptions,
                LastUpdated = DateTime.UtcNow
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error fetching newsletter statistics");
            return StatusCode(500, "Unable to fetch statistics");
        }
    }

    private async Task SendWelcomeSms(string phoneNumber, bool isReactivation)
    {
        try
        {
            var message = isReactivation 
                ? "Welcome back to Global Horizons! 🌍 You'll receive exclusive travel deals & updates. Reply STOP to unsubscribe."
                : "Welcome to Global Horizons Travel! 🌍 You'll receive exclusive travel deals, destination updates & special offers. Reply STOP to unsubscribe.";
            
            var result = await _smsService.SendSmsAsync(phoneNumber, message);
            
            if (!result.Success)
            {
                _logger.LogWarning($"Failed to send welcome SMS to {phoneNumber}: {result.Error}");
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error sending welcome SMS to {PhoneNumber}", phoneNumber);
        }
    }

    private async Task SendAdminNotification(string phoneNumber, string? source)
    {
        try
        {
            var adminPhones = new[] { "0205078908", "0240464248" };
            var sourceText = !string.IsNullOrEmpty(source) ? $" via {source}" : "";
            var adminMessage = $"📧 NEW NEWSLETTER SUBSCRIPTION: {phoneNumber} subscribed{sourceText} to travel updates! Total active subscribers growing. - Global Horizons";

            foreach (var adminPhone in adminPhones)
            {
                await _smsService.SendSmsAsync(adminPhone, adminMessage);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error sending admin notification for newsletter subscription");
        }
    }

    private static string FormatPhoneNumber(string phoneNumber)
    {
        if (string.IsNullOrEmpty(phoneNumber))
            return phoneNumber;

        // Remove any spaces, dashes, or other formatting
        var cleaned = phoneNumber.Replace(" ", "").Replace("-", "").Replace("(", "").Replace(")", "").Replace("+", "");
        
        // Handle different country codes and formats
        if (cleaned.StartsWith("233")) // Already has Ghana country code without +
        {
            return "+" + cleaned;
        }
        else if (cleaned.StartsWith("0")) // Local Ghana number
        {
            return "+233" + cleaned.Substring(1);
        }
        else if (cleaned.Length == 9 && cleaned.All(char.IsDigit)) // 9-digit Ghana number without leading 0
        {
            return "+233" + cleaned;
        }
        else if (phoneNumber.StartsWith("+")) // International format already
        {
            return phoneNumber.Replace(" ", "").Replace("-", "").Replace("(", "").Replace(")", "");
        }
        else
        {
            // Default to Ghana country code for unrecognized formats
            return "+233" + cleaned;
        }
    }
}