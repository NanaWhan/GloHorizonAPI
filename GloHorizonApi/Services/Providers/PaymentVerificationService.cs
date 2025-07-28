using Microsoft.EntityFrameworkCore;
using GloHorizonApi.Data;
using GloHorizonApi.Services.Interfaces;
using GloHorizonApi.Extensions;
using GloHorizonApi.Models.DomainModels;
using Akka.Actor;
using GloHorizonApi.Actors.Messages;

namespace GloHorizonApi.Services.Providers;

public class PaymentVerificationService : BackgroundService
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<PaymentVerificationService> _logger;
    private readonly TimeSpan _verificationInterval = TimeSpan.FromMinutes(2);

    public PaymentVerificationService(
        IServiceProvider serviceProvider,
        ILogger<PaymentVerificationService> logger)
    {
        _serviceProvider = serviceProvider;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("Payment Verification Service is starting.");

        // Initial delay to allow the application to fully start
        await Task.Delay(TimeSpan.FromSeconds(30), stoppingToken);

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await VerifyPendingPayments(stoppingToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while verifying pending payments.");
            }

            await Task.Delay(_verificationInterval, stoppingToken);
        }

        _logger.LogInformation("Payment Verification Service is stopping.");
    }

    private async Task VerifyPendingPayments(CancellationToken stoppingToken)
    {
        _logger.LogInformation("Checking for pending payment bookings...");

        using var scope = _serviceProvider.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var payStackService = scope.ServiceProvider.GetRequiredService<IPayStackPaymentService>();

        // Get bookings with payment pending status
        var pendingPayments = await dbContext.BookingRequests
            .Include(b => b.User)
            .Where(b => b.Status == BookingStatus.PaymentPending)
            .Where(b => b.CreatedAt > DateTime.UtcNow.AddDays(-7)) // Only check recent bookings
            .ToListAsync(stoppingToken);

        if (!pendingPayments.Any())
        {
            _logger.LogInformation("No pending payment bookings found to verify.");
            return;
        }

        _logger.LogInformation($"Found {pendingPayments.Count} pending payment bookings to verify.");

        foreach (var booking in pendingPayments)
        {
            if (stoppingToken.IsCancellationRequested)
                break;

            try
            {
                _logger.LogInformation($"Verifying payment for booking: {booking.ReferenceNumber}");
                
                // This would need to be implemented based on your payment verification logic
                // var verification = await payStackService.VerifyTransactionAsync(booking.ReferenceNumber);
                
                // For now, simulate verification logic
                // You would replace this with actual PayStack verification
                await Task.Delay(100, stoppingToken); // Simulate API call
                
                // If payment is verified as successful
                // booking.Status = BookingStatus.Processing;
                // await dbContext.SaveChangesAsync(stoppingToken);
                
                // Send notification about successful payment
                // var actorSystem = scope.ServiceProvider.GetRequiredService<ActorSystem>();
                // var notificationActor = actorSystem.ActorSelection("/user/booking-notification");
                // await notificationActor.Tell(new PaymentCompletedMessage(
                //     booking.ReferenceNumber,
                //     booking.FinalPrice ?? 0,
                //     booking.User.PhoneNumber
                // ));

                _logger.LogInformation($"Payment verification completed for booking: {booking.ReferenceNumber}");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error verifying payment for booking: {booking.ReferenceNumber}");
            }
        }
    }
} 