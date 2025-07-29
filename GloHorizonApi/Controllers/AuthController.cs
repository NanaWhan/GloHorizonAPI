using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Authorization;
using System.ComponentModel.DataAnnotations;
using GloHorizonApi.Data;
using GloHorizonApi.Models.DomainModels;
using GloHorizonApi.Models.Dtos.Auth;
using GloHorizonApi.Services.Providers;
using GloHorizonApi.Services.Interfaces;
using BCrypt.Net;
using System.Linq;

namespace GloHorizonApi.Controllers;

[ApiController]
[Route("api/[controller]")]
public class AuthController : ControllerBase
{
    private readonly ApplicationDbContext _context;
    private readonly JwtTokenGenerator _jwtTokenGenerator;
    private readonly IEmailService _emailService;
    private readonly ISmsService _smsService;
    private readonly ILogger<AuthController> _logger;
    private readonly IConfiguration _configuration;

    public AuthController(
        ApplicationDbContext context,
        JwtTokenGenerator jwtTokenGenerator,
        IEmailService emailService,
        ISmsService smsService,
        ILogger<AuthController> logger,
        IConfiguration configuration)
    {
        _context = context;
        _jwtTokenGenerator = jwtTokenGenerator;
        _emailService = emailService;
        _smsService = smsService;
        _logger = logger;
        _configuration = configuration;
    }

    [HttpPost("register")]
    public async Task<ActionResult<AuthResponse>> Register([FromBody] RegisterRequest request)
    {
        try
        {
            // Check if user already exists
            var existingUser = await _context.Users
                .FirstOrDefaultAsync(u => u.Email == request.Email || u.PhoneNumber == request.PhoneNumber);

            if (existingUser != null)
            {
                return BadRequest(new AuthResponse
                {
                    Success = false,
                    Message = "User with this email or phone number already exists"
                });
            }

            // Hash the password
            var passwordHash = BCrypt.Net.BCrypt.HashPassword(request.Password);

            // Create new user
            var nameParts = request.FullName.Trim().Split(' ', 2);
            var user = new User
            {
                FirstName = nameParts[0],
                LastName = nameParts.Length > 1 ? nameParts[1] : "",
                Email = request.Email,
                PhoneNumber = request.PhoneNumber,
                PasswordHash = passwordHash,
                EmailVerified = false,
                PhoneVerified = false
            };

            _context.Users.Add(user);
            await _context.SaveChangesAsync();

            // Generate JWT token
            var token = _jwtTokenGenerator.GenerateToken(user);

            return Ok(new AuthResponse
            {
                Success = true,
                Message = "Registration successful",
                Token = token,
                User = new UserInfo
                {
                    Id = user.Id,
                    FullName = user.FullName,
                    Email = user.Email,
                    PhoneNumber = user.PhoneNumber,
                    EmailVerified = user.EmailVerified,
                    PhoneVerified = user.PhoneVerified
                }
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during user registration");
            return StatusCode(500, new AuthResponse
            {
                Success = false,
                Message = "An error occurred during registration"
            });
        }
    }

    [HttpPost("login")]
    public async Task<ActionResult<AuthResponse>> Login([FromBody] LoginRequest request)
    {
        try
        {
            // Find user by email
            var user = await _context.Users
                .FirstOrDefaultAsync(u => u.Email == request.Email);

            if (user == null || string.IsNullOrEmpty(user.PasswordHash) || !BCrypt.Net.BCrypt.Verify(request.Password, user.PasswordHash))
            {
                return Unauthorized(new AuthResponse
                {
                    Success = false,
                    Message = "Invalid email or password"
                });
            }

            // Update last login
            user.LastLoginAt = DateTime.UtcNow;
            await _context.SaveChangesAsync();

            // Generate JWT token
            var token = _jwtTokenGenerator.GenerateToken(user);

            return Ok(new AuthResponse
            {
                Success = true,
                Message = "Login successful",
                Token = token,
                User = new UserInfo
                {
                    Id = user.Id,
                    FullName = user.FullName,
                    Email = user.Email,
                    PhoneNumber = user.PhoneNumber,
                    EmailVerified = user.EmailVerified,
                    PhoneVerified = user.PhoneVerified
                }
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during user login");
            return StatusCode(500, new AuthResponse
            {
                Success = false,
                Message = "An error occurred during login"
            });
        }
    }

    [HttpPost("request-otp")]
    public async Task<ActionResult<OtpResponse>> RequestOtp([FromBody] OtpLoginRequest request)
    {
        try
        {
            // Invalidate any existing OTPs for this phone number
            var existingOtps = await _context.OtpVerifications
                .Where(o => o.PhoneNumber == request.PhoneNumber && 
                           !o.IsUsed && 
                           DateTime.UtcNow <= o.ExpiresAt && 
                           o.AttemptCount < 3)
                .ToListAsync();

            foreach (var otp in existingOtps)
            {
                otp.IsUsed = true;
                otp.UsedAt = DateTime.UtcNow;
            }

            // Generate 6-digit OTP
            var otpCode = new Random().Next(100000, 999999).ToString();
            
            // Create OTP verification record
            var otpVerification = new OtpVerification
            {
                PhoneNumber = request.PhoneNumber,
                OtpCode = otpCode,
                CreatedAt = DateTime.UtcNow,
                ExpiresAt = DateTime.UtcNow.AddMinutes(10)
            };

            _context.OtpVerifications.Add(otpVerification);
            await _context.SaveChangesAsync();

            // Send OTP via SMS
            var smsResult = await _smsService.SendOtpAsync(request.PhoneNumber, otpCode);
            
            if (!smsResult.Success)
            {
                _logger.LogError($"Failed to send OTP SMS to {request.PhoneNumber}: {smsResult.Error}");
                return StatusCode(500, new OtpResponse
                {
                    Success = false,
                    Message = "Failed to send OTP. Please try again."
                });
            }

            // For development, log the OTP (remove in production)
            _logger.LogInformation($"OTP for {request.PhoneNumber}: {otpCode}");

            return Ok(new OtpResponse
            {
                Success = true,
                Message = "OTP sent successfully to your phone",
                OtpId = otpVerification.Id
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error sending OTP to {PhoneNumber}: {ErrorMessage}", request.PhoneNumber, ex.Message);
            return StatusCode(500, new OtpResponse
            {
                Success = false,
                Message = $"Failed to send OTP. Error: {ex.Message}"
            });
        }
    }

    [HttpPost("verify-otp")]
    public async Task<ActionResult<AuthResponse>> VerifyOtp([FromBody] VerifyOtpRequest request)
    {
        try
        {
            if (request.OtpCode.Length != 6 || !request.OtpCode.All(char.IsDigit))
            {
                return BadRequest(new AuthResponse
                {
                    Success = false,
                    Message = "Invalid OTP code. Please enter a 6-digit code."
                });
            }

            // Find the OTP verification record
            var otpVerification = await _context.OtpVerifications
                .FirstOrDefaultAsync(o => o.PhoneNumber == request.PhoneNumber && 
                                        o.OtpCode == request.OtpCode && 
                                        !o.IsUsed && 
                                        DateTime.UtcNow <= o.ExpiresAt && 
                                        o.AttemptCount < 3);

            if (otpVerification == null)
            {
                // Increment attempt count for existing OTPs
                var existingOtps = await _context.OtpVerifications
                    .Where(o => o.PhoneNumber == request.PhoneNumber && !o.IsUsed && !o.IsExpired)
                    .ToListAsync();

                foreach (var otp in existingOtps)
                {
                    otp.AttemptCount++;
                }
                await _context.SaveChangesAsync();

                return BadRequest(new AuthResponse
                {
                    Success = false,
                    Message = "Invalid or expired OTP code."
                });
            }

            if (otpVerification.IsExpired)
            {
                return BadRequest(new AuthResponse
                {
                    Success = false,
                    Message = "OTP has expired. Please request a new one."
                });
            }

            if (otpVerification.IsUsed)
            {
                return BadRequest(new AuthResponse
                {
                    Success = false,
                    Message = "OTP has already been used."
                });
            }

            if (otpVerification.AttemptCount >= 3)
            {
                return BadRequest(new AuthResponse
                {
                    Success = false,
                    Message = "Too many failed attempts. Please request a new OTP."
                });
            }

            // Mark OTP as used
            otpVerification.IsUsed = true;
            otpVerification.UsedAt = DateTime.UtcNow;

            // Find or create user by phone number
            var user = await _context.Users
                .FirstOrDefaultAsync(u => u.PhoneNumber == request.PhoneNumber);

            if (user == null)
            {
                // Create new user with phone verification
                user = new User
                {
                    FirstName = "Phone",
                    LastName = "User", // Can be updated later
                    Email = $"user.{request.PhoneNumber.Replace("+", "").Replace(" ", "")}@glohorizon.com",
                    PhoneNumber = request.PhoneNumber,
                    PhoneVerified = true,
                    EmailVerified = false
                };

                _context.Users.Add(user);
            }
            else
            {
                user.PhoneVerified = true;
                user.LastLoginAt = DateTime.UtcNow;
            }

            await _context.SaveChangesAsync();

            // Generate JWT token
            var token = _jwtTokenGenerator.GenerateToken(user);

            return Ok(new AuthResponse
            {
                Success = true,
                Message = "OTP verification successful",
                Token = token,
                User = new UserInfo
                {
                    Id = user.Id,
                    FullName = user.FullName,
                    Email = user.Email,
                    PhoneNumber = user.PhoneNumber,
                    EmailVerified = user.EmailVerified,
                    PhoneVerified = user.PhoneVerified
                }
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error verifying OTP");
            return StatusCode(500, new AuthResponse
            {
                Success = false,
                Message = "An error occurred during OTP verification"
            });
        }
    }


} 