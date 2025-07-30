using Microsoft.EntityFrameworkCore;
using GloHorizonApi.Data;

namespace GloHorizonApi.Services.Providers;

public class OtpCleanupService : BackgroundService
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<OtpCleanupService> _logger;
    private readonly TimeSpan _cleanupInterval = TimeSpan.FromHours(1); // Run every hour

    public OtpCleanupService(
        IServiceProvider serviceProvider,
        ILogger<OtpCleanupService> logger)
    {
        _serviceProvider = serviceProvider;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("OTP Cleanup Service is starting.");

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await CleanupExpiredOtps(stoppingToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred during OTP cleanup.");
            }

            await Task.Delay(_cleanupInterval, stoppingToken);
        }

        _logger.LogInformation("OTP Cleanup Service is stopping.");
    }

    private async Task CleanupExpiredOtps(CancellationToken stoppingToken)
    {
        using var scope = _serviceProvider.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

        try
        {
            // Delete OTPs that are older than 24 hours (well past their 10-minute expiry)
            var cutoffTime = DateTime.UtcNow.AddHours(-24);
            
            var expiredOtps = await dbContext.OtpVerifications
                .Where(o => o.CreatedAt < cutoffTime)
                .CountAsync(stoppingToken);

            if (expiredOtps > 0)
            {
                await dbContext.OtpVerifications
                    .Where(o => o.CreatedAt < cutoffTime)
                    .ExecuteDeleteAsync(stoppingToken);

                _logger.LogInformation("Cleaned up {ExpiredOtpCount} expired OTP records older than 24 hours.", expiredOtps);
            }
            else
            {
                _logger.LogDebug("No expired OTP records found for cleanup.");
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to cleanup expired OTP records.");
        }
    }
}