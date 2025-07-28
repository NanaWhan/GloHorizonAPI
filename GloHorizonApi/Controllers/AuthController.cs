using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using GloHorizonApi.Data;
using GloHorizonApi.Models.DomainModels;
using GloHorizonApi.Models.Dtos.Auth;
using GloHorizonApi.Services.Providers;
using GloHorizonApi.Services.Interfaces;
using BCrypt.Net;

namespace GloHorizonApi.Controllers;

[ApiController]
[Route("api/[controller]")]
public class AuthController : ControllerBase
{
    private readonly ApplicationDbContext _context;
    private readonly JwtTokenGenerator _jwtTokenGenerator;
    private readonly IEmailService _emailService;
    private readonly ILogger<AuthController> _logger;

    public AuthController(
        ApplicationDbContext context,
        JwtTokenGenerator jwtTokenGenerator,
        IEmailService emailService,
        ILogger<AuthController> logger)
    {
        _context = context;
        _jwtTokenGenerator = jwtTokenGenerator;
        _emailService = emailService;
        _logger = logger;
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
    public Task<ActionResult<OtpResponse>> RequestOtp([FromBody] OtpLoginRequest request)
    {
        try
        {
            // Generate 6-digit OTP
            var otpCode = new Random().Next(100000, 999999).ToString();
            
            // TODO: Send OTP via SMS (integrate with mnotify)
            // For development, log the OTP (remove in production)
            _logger.LogInformation($"OTP for {request.PhoneNumber}: {otpCode}");

            // TODO: Store OTP in cache/database for verification
            // For now, return success response
            
            return Task.FromResult<ActionResult<OtpResponse>>(Ok(new OtpResponse
            {
                Success = true,
                Message = "OTP sent successfully to your phone",
                OtpId = Guid.NewGuid().ToString() // Tracking ID for this OTP session
            }));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error sending OTP");
            return Task.FromResult<ActionResult<OtpResponse>>(StatusCode(500, new OtpResponse
            {
                Success = false,
                Message = "Failed to send OTP. Please try again."
            }));
        }
    }

    [HttpPost("verify-otp")]
    public async Task<ActionResult<AuthResponse>> VerifyOtp([FromBody] VerifyOtpRequest request)
    {
        try
        {
            // TODO: Verify OTP against stored value
            // For development, accept any 6-digit code
            if (request.OtpCode.Length != 6 || !request.OtpCode.All(char.IsDigit))
            {
                return BadRequest(new AuthResponse
                {
                    Success = false,
                    Message = "Invalid OTP code. Please enter a 6-digit code."
                });
            }

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

    [HttpPost("test-email")]
    public async Task<IActionResult> TestEmail([FromBody] string toEmail)
    {
        try
        {
            var result = await _emailService.SendEmailAsync(
                toEmail, 
                "Test Email - GloHorizon", 
                "<h1>🎉 Email Service Working!</h1><p>Your SMTP configuration is successful.</p><p>This test was sent from your GloHorizon API.</p>", 
                true
            );
            if (result.Success)
            {
                return Ok(new { message = "Email sent successfully!", recipient = toEmail });
            }
            else
            {
                return BadRequest(new { error = result.Error });
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to send test email to {Email}", toEmail);
            return StatusCode(500, new { error = ex.Message });
        }
    }

    [HttpPost("test-booking-confirmation")]
    public async Task<IActionResult> TestBookingConfirmation([FromBody] BookingTestRequest request)
    {
        try
        {
            var result = await _emailService.SendBookingConfirmationAsync(
                request.ToEmail,
                request.CustomerName,
                request.ReferenceNumber,
                request.ServiceType
            );

            if (result.Success)
            {
                return Ok(new { message = "Booking confirmation email sent!", recipient = request.ToEmail });
            }
            else
            {
                return BadRequest(new { error = result.Error });
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to send booking confirmation to {Email}", request.ToEmail);
            return StatusCode(500, new { error = ex.Message });
        }
    }

    public class BookingTestRequest
    {
        public string ToEmail { get; set; } = string.Empty;
        public string CustomerName { get; set; } = string.Empty;
        public string ReferenceNumber { get; set; } = string.Empty;
        public string ServiceType { get; set; } = string.Empty;
    }
} 