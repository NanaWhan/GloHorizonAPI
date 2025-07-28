using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using GloHorizonApi.Data;
using GloHorizonApi.Models.DomainModels;
using GloHorizonApi.Models.Dtos.User;
using BCrypt.Net;

namespace GloHorizonApi.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class UserController : ControllerBase
{
    private readonly ApplicationDbContext _context;
    private readonly ILogger<UserController> _logger;

    public UserController(
        ApplicationDbContext context,
        ILogger<UserController> logger)
    {
        _context = context;
        _logger = logger;
    }

    [HttpGet("profile")]
    public async Task<ActionResult<UserProfileResponse>> GetProfile()
    {
        try
        {
            var userId = User.FindFirst("Id")?.Value;
            if (string.IsNullOrEmpty(userId))
            {
                return Unauthorized("Invalid authentication token");
            }

            var user = await _context.Users.FindAsync(userId);
            if (user == null)
            {
                return NotFound("User not found");
            }

            return Ok(new UserProfileResponse
            {
                Id = user.Id,
                FullName = user.FullName,
                Email = user.Email,
                PhoneNumber = user.PhoneNumber,
                EmailVerified = user.EmailVerified,
                PhoneVerified = user.PhoneVerified,
                CreatedAt = user.CreatedAt,
                LastLoginAt = user.LastLoginAt
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving user profile");
            return StatusCode(500, "An error occurred retrieving your profile");
        }
    }

    [HttpPut("profile")]
    public async Task<ActionResult> UpdateProfile([FromBody] UpdateUserProfileRequest request)
    {
        try
        {
            var userId = User.FindFirst("Id")?.Value;
            if (string.IsNullOrEmpty(userId))
            {
                return Unauthorized("Invalid authentication token");
            }

            var user = await _context.Users.FindAsync(userId);
            if (user == null)
            {
                return NotFound("User not found");
            }

            // Check if email is being changed and if it already exists
            if (request.Email != user.Email)
            {
                var existingUser = await _context.Users
                    .FirstOrDefaultAsync(u => u.Email == request.Email && u.Id != userId);
                
                if (existingUser != null)
                {
                    return BadRequest("Email address is already in use");
                }
                
                user.Email = request.Email;
                user.EmailVerified = false; // Require re-verification if email changes
            }

            // Check if phone is being changed and if it already exists
            if (request.PhoneNumber != user.PhoneNumber)
            {
                var existingUser = await _context.Users
                    .FirstOrDefaultAsync(u => u.PhoneNumber == request.PhoneNumber && u.Id != userId);
                
                if (existingUser != null)
                {
                    return BadRequest("Phone number is already in use");
                }
                
                user.PhoneNumber = request.PhoneNumber;
                user.PhoneVerified = false; // Require re-verification if phone changes
            }

            var nameParts = request.FullName.Trim().Split(' ', 2);
            user.FirstName = nameParts[0];
            user.LastName = nameParts.Length > 1 ? nameParts[1] : "";
            
            await _context.SaveChangesAsync();

            return Ok(new { Success = true, Message = "Profile updated successfully" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating user profile");
            return StatusCode(500, "An error occurred updating your profile");
        }
    }

    [HttpPut("change-password")]
    public async Task<ActionResult> ChangePassword([FromBody] ChangePasswordRequest request)
    {
        try
        {
            var userId = User.FindFirst("Id")?.Value;
            if (string.IsNullOrEmpty(userId))
            {
                return Unauthorized("Invalid authentication token");
            }

            var user = await _context.Users.FindAsync(userId);
            if (user == null)
            {
                return NotFound("User not found");
            }

            // Verify current password
            if (string.IsNullOrEmpty(user.PasswordHash) || !BCrypt.Net.BCrypt.Verify(request.CurrentPassword, user.PasswordHash))
            {
                return BadRequest("Current password is incorrect");
            }

            // Update password
            user.PasswordHash = BCrypt.Net.BCrypt.HashPassword(request.NewPassword);
            await _context.SaveChangesAsync();

            return Ok(new { Success = true, Message = "Password changed successfully" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error changing user password");
            return StatusCode(500, "An error occurred changing your password");
        }
    }

    [HttpGet("booking-history")]
    public async Task<ActionResult<List<UserBookingHistoryDto>>> GetBookingHistory(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 10)
    {
        try
        {
            var userId = User.FindFirst("Id")?.Value;
            if (string.IsNullOrEmpty(userId))
            {
                return Unauthorized("Invalid authentication token");
            }

            var bookings = await _context.BookingRequests
                .Where(b => b.UserId == userId)
                .OrderByDescending(b => b.CreatedAt)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .Select(b => new UserBookingHistoryDto
                {
                    Id = b.Id,
                    ReferenceNumber = b.ReferenceNumber,
                    ServiceType = b.ServiceType.ToString(),
                    Status = b.Status.ToString(),
                    Urgency = b.Urgency.ToString(),
                    CreatedAt = b.CreatedAt,
                    UpdatedAt = b.UpdatedAt,
                    EstimatedPrice = b.EstimatedPrice,
                    FinalPrice = b.FinalPrice,
                    Currency = b.Currency
                })
                .ToListAsync();

            return Ok(bookings);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving user booking history");
            return StatusCode(500, "An error occurred retrieving your booking history");
        }
    }

    [HttpDelete("account")]
    public async Task<ActionResult> DeleteAccount([FromBody] DeleteAccountRequest request)
    {
        try
        {
            var userId = User.FindFirst("Id")?.Value;
            if (string.IsNullOrEmpty(userId))
            {
                return Unauthorized("Invalid authentication token");
            }

            var user = await _context.Users.FindAsync(userId);
            if (user == null)
            {
                return NotFound("User not found");
            }

            // Verify password for account deletion
            if (string.IsNullOrEmpty(user.PasswordHash) || !BCrypt.Net.BCrypt.Verify(request.Password, user.PasswordHash))
            {
                return BadRequest("Password is incorrect");
            }

            // Check for active bookings
            var activeBookings = await _context.BookingRequests
                .Where(b => b.UserId == userId && 
                           (b.Status == BookingStatus.Pending || 
                            b.Status == BookingStatus.UnderReview ||
                            b.Status == BookingStatus.Processing))
                .CountAsync();

            if (activeBookings > 0)
            {
                return BadRequest("Cannot delete account with active bookings. Please contact support.");
            }

            // Soft delete user account
            user.Email = $"deleted_{user.Id}@deleted.com";
            user.PhoneNumber = $"deleted_{user.Id}";
            user.FirstName = "Deleted";
            user.LastName = "User";
            user.PasswordHash = "";
            
            await _context.SaveChangesAsync();

            return Ok(new { Success = true, Message = "Account deleted successfully" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting user account");
            return StatusCode(500, "An error occurred deleting your account");
        }
    }
} 