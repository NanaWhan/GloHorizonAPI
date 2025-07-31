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
    private readonly IOtpService _otpService;
    private readonly ILogger<AuthController> _logger;
    private readonly IConfiguration _configuration;

    public AuthController(
        ApplicationDbContext context,
        JwtTokenGenerator jwtTokenGenerator,
        IEmailService emailService,
        ISmsService smsService,
        IOtpService otpService,
        ILogger<AuthController> logger,
        IConfiguration configuration)
    {
        _context = context;
        _jwtTokenGenerator = jwtTokenGenerator;
        _emailService = emailService;
        _smsService = smsService;
        _otpService = otpService;
        _logger = logger;
        _configuration = configuration;
    }

    [HttpPost("register")]
    public async Task<ActionResult<AuthResponse>> Register([FromBody] RegisterRequest request)
    {
        try
        {
            if (!ModelState.IsValid)
            {
                var errors = ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage);
                return BadRequest(new AuthResponse
                {
                    Success = false,
                    Message = string.Join("; ", errors)
                });
            }
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
            var user = new User
            {
                FirstName = request.FirstName.Trim(),
                LastName = request.LastName.Trim(),
                Email = request.Email,
                PhoneNumber = request.PhoneNumber,
                PasswordHash = passwordHash,
                DateOfBirth = request.DateOfBirth,
                AcceptMarketing = request.AcceptMarketing,
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
                    FirstName = user.FirstName,
                    LastName = user.LastName,
                    Email = user.Email,
                    PhoneNumber = user.PhoneNumber,
                    Role = user.Role,
                    CreatedAt = user.CreatedAt
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
            if (!ModelState.IsValid)
            {
                var errors = ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage);
                return BadRequest(new AuthResponse
                {
                    Success = false,
                    Message = string.Join("; ", errors)
                });
            }
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
                    FirstName = user.FirstName,
                    LastName = user.LastName,
                    Email = user.Email,
                    PhoneNumber = user.PhoneNumber,
                    Role = user.Role,
                    CreatedAt = user.CreatedAt
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
            if (!ModelState.IsValid)
            {
                var errors = ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage);
                return BadRequest(new OtpResponse
                {
                    Success = false,
                    Message = string.Join("; ", errors)
                });
            }

            // CRITICAL: Check if user exists with this phone number
            var user = await _context.Users
                .FirstOrDefaultAsync(u => u.PhoneNumber == request.PhoneNumber);

            if (user == null)
            {
                return NotFound(new OtpResponse
                {
                    Success = false,
                    Message = "Phone number not registered. Please create an account first."
                });
            }

            // Generate OTP using Redis service
            var otpCode = await _otpService.GenerateOtpAsync(request.PhoneNumber, 10);

            // Send OTP via SMS
            var smsResult = await _smsService.SendOtpAsync(request.PhoneNumber, otpCode);
            
            if (!smsResult.Success)
            {
                // If SMS fails, invalidate the OTP
                await _otpService.InvalidateOtpAsync(request.PhoneNumber);
                
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
                Message = Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT") == "Development" 
                    ? $"OTP sent successfully to your phone. DEV MODE OTP: {otpCode}"
                    : "OTP sent successfully to your phone",
                OtpId = Guid.NewGuid().ToString() // Generate a session ID
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
    public async Task<ActionResult<AuthResponse>> VerifyOtp([FromBody] Models.Dtos.Auth.VerifyOtpRequest request)
    {
        try
        {
            if (!ModelState.IsValid)
            {
                var errors = ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage);
                return BadRequest(new AuthResponse
                {
                    Success = false,
                    Message = string.Join("; ", errors)
                });
            }

            if (request.OtpCode.Length != 6 || !request.OtpCode.All(char.IsDigit))
            {
                return BadRequest(new AuthResponse
                {
                    Success = false,
                    Message = "Invalid OTP code. Please enter a 6-digit code."
                });
            }

            // Check attempt count before verification
            var attemptCount = await _otpService.GetAttemptCountAsync(request.PhoneNumber);
            if (attemptCount >= 3)
            {
                return BadRequest(new AuthResponse
                {
                    Success = false,
                    Message = "Too many failed attempts. Please request a new OTP."
                });
            }

            // Verify OTP using Redis service
            var isValidOtp = await _otpService.VerifyOtpAsync(request.PhoneNumber, request.OtpCode);
            
            if (!isValidOtp)
            {
                return BadRequest(new AuthResponse
                {
                    Success = false,
                    Message = "Invalid or expired OTP code."
                });
            }

            // Find user by phone number
            var user = await _context.Users
                .FirstOrDefaultAsync(u => u.PhoneNumber == request.PhoneNumber);

            if (user == null)
            {
                return NotFound(new AuthResponse
                {
                    Success = false,
                    Message = "Phone number not found. Please register first."
                });
            }

            // Update user verification status
            user.PhoneVerified = true;
            user.LastLoginAt = DateTime.UtcNow;

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
                    FirstName = user.FirstName,
                    LastName = user.LastName,
                    Email = user.Email,
                    PhoneNumber = user.PhoneNumber,
                    Role = user.Role,
                    CreatedAt = user.CreatedAt
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

    [HttpPost("logout")]
    [Authorize]
    public ActionResult Logout()
    {
        try
        {
            // Optional: Add token to blacklist here if implementing token blacklisting
            // For now, we'll just return success as frontend will clear the token
            
            return Ok(new
            {
                Success = true,
                Message = "Logged out successfully"
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during logout");
            return StatusCode(500, new
            {
                Success = false,
                Message = "An error occurred during logout"
            });
        }
    }


} 