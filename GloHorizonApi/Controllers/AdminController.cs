using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using GloHorizonApi.Data;
using GloHorizonApi.Models.DomainModels;
using GloHorizonApi.Models.Dtos.Admin;
using GloHorizonApi.Models.Dtos.Auth;
using GloHorizonApi.Models.Dtos.Booking;
using GloHorizonApi.Services.Providers;
using BCrypt.Net;

namespace GloHorizonApi.Controllers;

[ApiController]
[Route("api/[controller]")]
public class AdminController : ControllerBase
{
    private readonly ApplicationDbContext _context;
    private readonly JwtTokenGenerator _jwtTokenGenerator;
    private readonly ILogger<AdminController> _logger;

    public AdminController(
        ApplicationDbContext context,
        JwtTokenGenerator jwtTokenGenerator,
        ILogger<AdminController> logger)
    {
        _context = context;
        _jwtTokenGenerator = jwtTokenGenerator;
        _logger = logger;
    }

    [HttpPost("login")]
    public async Task<ActionResult<AuthResponse>> Login([FromBody] LoginAdminRequest request)
    {
        try
        {
            var admin = await _context.Admins
                .FirstOrDefaultAsync(a => a.Email == request.Email && a.IsActive);

            if (admin == null || !BCrypt.Net.BCrypt.Verify(request.Password, admin.PasswordHash))
            {
                return Unauthorized(new AuthResponse
                {
                    Success = false,
                    Message = "Invalid email or password"
                });
            }

            admin.LastLoginAt = DateTime.UtcNow;
            await _context.SaveChangesAsync();

            var token = _jwtTokenGenerator.GenerateToken(admin);

            return Ok(new AuthResponse
            {
                Success = true,
                Message = "Admin login successful",
                Token = token,
                User = new UserInfo
                {
                    Id = admin.Id,
                    FullName = admin.FullName,
                    Email = admin.Email,
                    PhoneNumber = admin.PhoneNumber
                }
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during admin login");
            return StatusCode(500, new AuthResponse
            {
                Success = false,
                Message = "An error occurred during login"
            });
        }
    }

    [HttpPost("create")]
    [Authorize]
    public async Task<ActionResult<AuthResponse>> CreateAdmin([FromBody] CreateAdminRequest request)
    {
        try
        {
            // Verify the requesting user is a super admin
            var currentAdminRole = User.FindFirst("Role")?.Value;
            if (currentAdminRole != AdminRole.SuperAdmin.ToString())
            {
                return Forbid("Only Super Admins can create new admin accounts");
            }

            // Check if admin already exists
            var existingAdmin = await _context.Admins
                .FirstOrDefaultAsync(a => a.Email == request.Email);

            if (existingAdmin != null)
            {
                return BadRequest(new AuthResponse
                {
                    Success = false,
                    Message = "Admin with this email already exists"
                });
            }

            // Create new admin
            var admin = new Admin
            {
                FullName = request.FullName,
                Email = request.Email,
                PhoneNumber = request.PhoneNumber,
                PasswordHash = BCrypt.Net.BCrypt.HashPassword(request.Password),
                Role = request.Role,
                CreatedBy = User.FindFirst("Id")?.Value
            };

            _context.Admins.Add(admin);
            await _context.SaveChangesAsync();

            return Ok(new AuthResponse
            {
                Success = true,
                Message = "Admin account created successfully"
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating admin account");
            return StatusCode(500, new AuthResponse
            {
                Success = false,
                Message = "An error occurred creating admin account"
            });
        }
    }

    [HttpGet("bookings")]
    [Authorize]
    public async Task<ActionResult<List<BookingInfo>>> GetAllBookings(
        [FromQuery] BookingStatus? status = null,
        [FromQuery] ServiceType? serviceType = null,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20)
    {
        try
        {
            var query = _context.BookingRequests
                .Include(b => b.User)
                .AsQueryable();

            if (status.HasValue)
                query = query.Where(b => b.Status == status.Value);

            if (serviceType.HasValue)
                query = query.Where(b => b.ServiceType == serviceType.Value);

            var bookings = await query
                .OrderByDescending(b => b.CreatedAt)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .Select(b => new BookingInfo
                {
                    Id = b.Id,
                    ReferenceNumber = b.ReferenceNumber,
                    ServiceType = b.ServiceType,
                    Status = b.Status,
                    Urgency = b.Urgency,
                    CreatedAt = b.CreatedAt,
                    EstimatedPrice = b.EstimatedPrice,
                    FinalPrice = b.FinalPrice,
                    Currency = b.Currency
                })
                .ToListAsync();

            return Ok(bookings);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving bookings for admin");
            return StatusCode(500, "An error occurred retrieving bookings");
        }
    }

    [HttpPut("bookings/{id}/status")]
    [Authorize]
    public async Task<ActionResult> UpdateBookingStatus(
        int id, 
        [FromBody] UpdateBookingStatusRequest request)
    {
        try
        {
            var booking = await _context.BookingRequests
                .FirstOrDefaultAsync(b => b.Id == id);

            if (booking == null)
            {
                return NotFound("Booking not found");
            }

            var oldStatus = booking.Status;
            booking.Status = request.NewStatus;
            booking.UpdatedAt = DateTime.UtcNow;
            booking.AdminNotes = request.AdminNotes;

            if (request.EstimatedPrice.HasValue)
                booking.EstimatedPrice = request.EstimatedPrice.Value;

            if (request.FinalPrice.HasValue)
                booking.FinalPrice = request.FinalPrice.Value;

            // Add status history
            var statusHistory = new BookingStatusHistory
            {
                BookingRequestId = booking.Id,
                FromStatus = oldStatus,
                ToStatus = request.NewStatus,
                Notes = request.Notes,
                ChangedBy = User.FindFirst("FullName")?.Value ?? "Admin",
                ChangedAt = DateTime.UtcNow
            };

            _context.BookingStatusHistories.Add(statusHistory);
            await _context.SaveChangesAsync();

            _logger.LogInformation($"Booking {booking.ReferenceNumber} status updated from {oldStatus} to {request.NewStatus}");

            return Ok(new { Success = true, Message = "Booking status updated successfully" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating booking status");
            return StatusCode(500, "An error occurred updating booking status");
        }
    }

    [HttpGet("dashboard")]
    [Authorize]
    public async Task<ActionResult<AdminDashboardResponse>> GetDashboard()
    {
        try
        {
            var totalBookings = await _context.BookingRequests.CountAsync();
            var pendingBookings = await _context.BookingRequests
                .CountAsync(b => b.Status == BookingStatus.Pending);
            var completedBookings = await _context.BookingRequests
                .CountAsync(b => b.Status == BookingStatus.Completed);
            var totalUsers = await _context.Users.CountAsync();

            var recentBookings = await _context.BookingRequests
                .Include(b => b.User)
                .OrderByDescending(b => b.CreatedAt)
                .Take(10)
                .Select(b => new BookingInfo
                {
                    Id = b.Id,
                    ReferenceNumber = b.ReferenceNumber,
                    ServiceType = b.ServiceType,
                    Status = b.Status,
                    Urgency = b.Urgency,
                    CreatedAt = b.CreatedAt
                })
                .ToListAsync();

            return Ok(new AdminDashboardResponse
            {
                TotalBookings = totalBookings,
                PendingBookings = pendingBookings,
                CompletedBookings = completedBookings,
                TotalUsers = totalUsers,
                RecentBookings = recentBookings
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving admin dashboard data");
            return StatusCode(500, "An error occurred retrieving dashboard data");
        }
    }
}

// Additional DTOs for admin operations
public class UpdateBookingStatusRequest
{
    public BookingStatus NewStatus { get; set; }
    public string? Notes { get; set; }
    public string? AdminNotes { get; set; }
    public decimal? EstimatedPrice { get; set; }
    public decimal? FinalPrice { get; set; }
}

public class AdminDashboardResponse
{
    public int TotalBookings { get; set; }
    public int PendingBookings { get; set; }
    public int CompletedBookings { get; set; }
    public int TotalUsers { get; set; }
    public List<BookingInfo> RecentBookings { get; set; } = new();
} 