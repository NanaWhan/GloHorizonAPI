using Microsoft.EntityFrameworkCore;
using GloHorizonApi.Data;
using GloHorizonApi.Models.DomainModels;
using GloHorizonApi.Services.Interfaces;

namespace GloHorizonApi.Services.Providers;

public class DatabaseOtpService : IOtpService
{
    private readonly ApplicationDbContext _context;
    private readonly ILogger<DatabaseOtpService> _logger;
    private const int MaxAttempts = 3;

    public DatabaseOtpService(
        ApplicationDbContext context,
        ILogger<DatabaseOtpService> logger)
    {
        _context = context;
        _logger = logger;
    }

    public async Task<string> GenerateOtpAsync(string phoneNumber, int expiryMinutes = 10)
    {
        try
        {
            // Invalidate existing OTPs for this phone number
            var existingOtps = await _context.OtpVerifications
                .Where(o => o.PhoneNumber == phoneNumber && !o.IsUsed)
                .ToListAsync();

            foreach (var otp in existingOtps)
            {
                otp.IsUsed = true;
                otp.UsedAt = DateTime.UtcNow;
            }

            // Generate new 6-digit OTP
            var random = new Random();
            var otpCode = random.Next(100000, 999999).ToString();

            // Create new OTP record
            var otpVerification = new OtpVerification
            {
                PhoneNumber = phoneNumber,
                OtpCode = otpCode,
                CreatedAt = DateTime.UtcNow,
                ExpiresAt = DateTime.UtcNow.AddMinutes(expiryMinutes),
                AttemptCount = 0,
                IsUsed = false,
                UsedAt = null
            };

            _context.OtpVerifications.Add(otpVerification);
            await _context.SaveChangesAsync();

            _logger.LogDebug("Generated OTP for phone number {PhoneNumber}", phoneNumber);
            return otpCode;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error generating OTP for {PhoneNumber}", phoneNumber);
            throw;
        }
    }

    public async Task<bool> VerifyOtpAsync(string phoneNumber, string otpCode)
    {
        try
        {
            var otpVerification = await _context.OtpVerifications
                .Where(o => o.PhoneNumber == phoneNumber && 
                           o.OtpCode == otpCode && 
                           !o.IsUsed && 
                           o.ExpiresAt > DateTime.UtcNow)
                .OrderByDescending(o => o.CreatedAt)
                .FirstOrDefaultAsync();

            if (otpVerification == null)
            {
                // Increment attempt count for latest OTP
                await IncrementAttemptCountAsync(phoneNumber);
                _logger.LogWarning("Invalid OTP verification attempt for {PhoneNumber}", phoneNumber);
                return false;
            }

            // Check if attempts exceeded
            if (otpVerification.AttemptCount >= MaxAttempts)
            {
                _logger.LogWarning("OTP attempts exceeded for {PhoneNumber}", phoneNumber);
                return false;
            }

            // Mark as used
            otpVerification.IsUsed = true;
            otpVerification.UsedAt = DateTime.UtcNow;
            await _context.SaveChangesAsync();

            _logger.LogDebug("OTP verified successfully for {PhoneNumber}", phoneNumber);
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error verifying OTP for {PhoneNumber}", phoneNumber);
            return false;
        }
    }

    public async Task<bool> InvalidateOtpAsync(string phoneNumber)
    {
        try
        {
            var otps = await _context.OtpVerifications
                .Where(o => o.PhoneNumber == phoneNumber && !o.IsUsed)
                .ToListAsync();

            foreach (var otp in otps)
            {
                otp.IsUsed = true;
                otp.UsedAt = DateTime.UtcNow;
            }

            await _context.SaveChangesAsync();
            _logger.LogDebug("Invalidated {Count} OTPs for {PhoneNumber}", otps.Count, phoneNumber);
            return otps.Any();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error invalidating OTP for {PhoneNumber}", phoneNumber);
            return false;
        }
    }

    public async Task<int> GetAttemptCountAsync(string phoneNumber)
    {
        try
        {
            var otp = await _context.OtpVerifications
                .Where(o => o.PhoneNumber == phoneNumber && 
                           !o.IsUsed && 
                           o.ExpiresAt > DateTime.UtcNow)
                .OrderByDescending(o => o.CreatedAt)
                .FirstOrDefaultAsync();

            return otp?.AttemptCount ?? 0;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting attempt count for {PhoneNumber}", phoneNumber);
            return 0;
        }
    }

    public async Task<bool> IncrementAttemptCountAsync(string phoneNumber)
    {
        try
        {
            var otps = await _context.OtpVerifications
                .Where(o => o.PhoneNumber == phoneNumber && 
                           !o.IsUsed && 
                           o.ExpiresAt > DateTime.UtcNow)
                .ToListAsync();

            foreach (var otp in otps)
            {
                otp.AttemptCount++;
            }

            await _context.SaveChangesAsync();
            return otps.Any();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error incrementing attempt count for {PhoneNumber}", phoneNumber);
            return false;
        }
    }

    public async Task<bool> IsOtpValidAsync(string phoneNumber)
    {
        try
        {
            return await _context.OtpVerifications
                .AnyAsync(o => o.PhoneNumber == phoneNumber && 
                              !o.IsUsed && 
                              o.ExpiresAt > DateTime.UtcNow);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error checking OTP validity for {PhoneNumber}", phoneNumber);
            return false;
        }
    }
}