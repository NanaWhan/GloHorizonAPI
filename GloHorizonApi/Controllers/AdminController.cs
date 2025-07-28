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

    [HttpGet("bookings/{id}")]
    [Authorize]
    public async Task<ActionResult<DetailedBookingInfo>> GetBooking(int id)
    {
        try
        {
            var booking = await _context.BookingRequests
                .Include(b => b.User)
                .Include(b => b.StatusHistory)
                .FirstOrDefaultAsync(b => b.Id == id);

            if (booking == null)
            {
                return NotFound($"Booking with ID {id} not found");
            }

            var detailedBooking = new DetailedBookingInfo
            {
                Id = booking.Id,
                ReferenceNumber = booking.ReferenceNumber,
                ServiceType = booking.ServiceType,
                Status = booking.Status,
                Urgency = booking.Urgency,
                CreatedAt = booking.CreatedAt,
                UpdatedAt = booking.UpdatedAt,
                EstimatedPrice = booking.EstimatedPrice,
                FinalPrice = booking.FinalPrice,
                Currency = booking.Currency,
                AdminNotes = booking.AdminNotes,
                User = new UserInfo
                {
                    Id = booking.User.Id,
                    FullName = booking.User.FullName,
                    Email = booking.User.Email,
                    PhoneNumber = booking.User.PhoneNumber
                },
                StatusHistory = booking.StatusHistory
                    .OrderByDescending(h => h.ChangedAt)
                    .Select(h => new AdminStatusHistoryInfo
                    {
                        FromStatus = h.FromStatus,
                        ToStatus = h.ToStatus,
                        Notes = h.Notes,
                        ChangedBy = h.ChangedBy,
                        ChangedAt = h.ChangedAt
                    }).ToList(),
                // Parse booking details from JSON
                BookingDetails = string.IsNullOrEmpty(booking.BookingData) 
                    ? new Dictionary<string, object>() 
                    : System.Text.Json.JsonSerializer.Deserialize<Dictionary<string, object>>(booking.BookingData) ?? new Dictionary<string, object>()
            };

            return Ok(detailedBooking);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"Error retrieving booking details for ID: {id}");
            return StatusCode(500, "An error occurred retrieving booking details");
        }
    }

    [HttpPut("bookings/{id}/pricing")]
    [Authorize]
    public async Task<ActionResult> UpdateBookingPricing(int id, [FromBody] UpdatePricingRequest request)
    {
        try
        {
            var booking = await _context.BookingRequests
                .FirstOrDefaultAsync(b => b.Id == id);

            if (booking == null)
            {
                return NotFound($"Booking with ID {id} not found");
            }

            var oldEstimatedPrice = booking.EstimatedPrice;
            var oldFinalPrice = booking.FinalPrice;

            // Update pricing
            if (request.EstimatedPrice.HasValue)
                booking.EstimatedPrice = request.EstimatedPrice.Value;

            if (request.FinalPrice.HasValue)
                booking.FinalPrice = request.FinalPrice.Value;

            if (!string.IsNullOrEmpty(request.Currency))
                booking.Currency = request.Currency;

            booking.UpdatedAt = DateTime.UtcNow;

            // Add status history for pricing update
            var statusHistory = new BookingStatusHistory
            {
                BookingRequestId = booking.Id,
                FromStatus = booking.Status,
                ToStatus = booking.Status, // Status remains same, just pricing updated
                Notes = $"Pricing updated: Est: {oldEstimatedPrice} → {booking.EstimatedPrice}, Final: {oldFinalPrice} → {booking.FinalPrice}. Reason: {request.Notes}",
                ChangedBy = User.FindFirst("FullName")?.Value ?? "Admin",
                ChangedAt = DateTime.UtcNow
            };

            _context.BookingStatusHistories.Add(statusHistory);
            await _context.SaveChangesAsync();

            _logger.LogInformation($"Pricing updated for booking {booking.ReferenceNumber} by {statusHistory.ChangedBy}");

            return Ok(new { 
                Success = true, 
                Message = "Booking pricing updated successfully",
                EstimatedPrice = booking.EstimatedPrice,
                FinalPrice = booking.FinalPrice,
                Currency = booking.Currency
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"Error updating pricing for booking ID: {id}");
            return StatusCode(500, "An error occurred updating booking pricing");
        }
    }

    [HttpPost("bookings/{id}/notes")]
    [Authorize]
    public async Task<ActionResult> AddBookingNote(int id, [FromBody] AddNoteRequest request)
    {
        try
        {
            var booking = await _context.BookingRequests
                .FirstOrDefaultAsync(b => b.Id == id);

            if (booking == null)
            {
                return NotFound($"Booking with ID {id} not found");
            }

            // Append new note to existing admin notes
            var adminName = User.FindFirst("FullName")?.Value ?? "Admin";
            var timestamp = DateTime.UtcNow.ToString("yyyy-MM-dd HH:mm:ss UTC");
            var newNote = $"[{timestamp}] {adminName}: {request.Note}";

            if (string.IsNullOrEmpty(booking.AdminNotes))
            {
                booking.AdminNotes = newNote;
            }
            else
            {
                booking.AdminNotes += "\n" + newNote;
            }

            booking.UpdatedAt = DateTime.UtcNow;

            // Add status history for note addition
            var statusHistory = new BookingStatusHistory
            {
                BookingRequestId = booking.Id,
                FromStatus = booking.Status,
                ToStatus = booking.Status, // Status remains same
                Notes = $"Admin note added: {request.Note}",
                ChangedBy = adminName,
                ChangedAt = DateTime.UtcNow
            };

            _context.BookingStatusHistories.Add(statusHistory);
            await _context.SaveChangesAsync();

            _logger.LogInformation($"Note added to booking {booking.ReferenceNumber} by {adminName}");

            return Ok(new { 
                Success = true, 
                Message = "Note added successfully",
                AdminNotes = booking.AdminNotes
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"Error adding note to booking ID: {id}");
            return StatusCode(500, "An error occurred adding note to booking");
        }
    }

    [HttpPost("bookings/{id}/payment-link")]
    [Authorize]
    public async Task<ActionResult> GeneratePaymentLink(int id)
    {
        try
        {
            var booking = await _context.BookingRequests
                .Include(b => b.User)
                .FirstOrDefaultAsync(b => b.Id == id);

            if (booking == null)
            {
                return NotFound($"Booking with ID {id} not found");
            }

            // Check if booking is ready for payment
            if (booking.Status != BookingStatus.QuoteAccepted)
            {
                return BadRequest(new { 
                    Success = false, 
                    Message = $"Booking must be in 'QuoteAccepted' status to generate payment link. Current status: {booking.Status}" 
                });
            }

            if (!booking.FinalPrice.HasValue || booking.FinalPrice <= 0)
            {
                return BadRequest(new { 
                    Success = false, 
                    Message = "Final price must be set before generating payment link" 
                });
            }

            // Create payment request
            var paymentRequest = new Models.Dtos.Payment.GenericPaymentRequest
            {
                Amount = booking.FinalPrice.Value,
                ClientReference = booking.ReferenceNumber,
                TicketName = $"{booking.ServiceType} Booking",
                User = booking.User
            };

            // Generate PayStack payment link
            var payStackService = HttpContext.RequestServices.GetRequiredService<Services.Interfaces.IPayStackPaymentService>();
            var paymentResult = await payStackService.CreatePayLink(paymentRequest);

            if (!paymentResult.Status)
            {
                return BadRequest(new { 
                    Success = false, 
                    Message = "Failed to generate payment link: " + paymentResult.Message 
                });
            }

            // Update booking status to PaymentPending
            var oldStatus = booking.Status;
            booking.Status = BookingStatus.PaymentPending;
            booking.UpdatedAt = DateTime.UtcNow;

            // Add status history
            var statusHistory = new BookingStatusHistory
            {
                BookingRequestId = booking.Id,
                FromStatus = oldStatus,
                ToStatus = BookingStatus.PaymentPending,
                Notes = $"Payment link generated. Amount: {booking.FinalPrice:C}",
                ChangedBy = User.FindFirst("FullName")?.Value ?? "Admin",
                ChangedAt = DateTime.UtcNow
            };

            _context.BookingStatusHistories.Add(statusHistory);
            await _context.SaveChangesAsync();

            _logger.LogInformation($"Payment link generated for booking {booking.ReferenceNumber}");

            return Ok(new { 
                Success = true, 
                Message = "Payment link generated successfully",
                PaymentUrl = paymentResult.Data?.AuthorizationUrl,
                Reference = paymentResult.Data?.Reference,
                Amount = booking.FinalPrice,
                Currency = booking.Currency ?? "GHS"
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"Error generating payment link for booking ID: {id}");
            return StatusCode(500, "An error occurred generating payment link");
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

// New DTOs for Week 1 endpoints
public class DetailedBookingInfo
{
    public int Id { get; set; }
    public string ReferenceNumber { get; set; } = string.Empty;
    public ServiceType ServiceType { get; set; }
    public BookingStatus Status { get; set; }
    public UrgencyLevel Urgency { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
    public decimal? EstimatedPrice { get; set; }
    public decimal? FinalPrice { get; set; }
    public string? Currency { get; set; }
    public string? AdminNotes { get; set; }
    public UserInfo User { get; set; } = new();
    public List<AdminStatusHistoryInfo> StatusHistory { get; set; } = new();
    public Dictionary<string, object> BookingDetails { get; set; } = new();
}

public class AdminStatusHistoryInfo
{
    public BookingStatus FromStatus { get; set; }
    public BookingStatus ToStatus { get; set; }
    public string? Notes { get; set; }
    public string ChangedBy { get; set; } = string.Empty;
    public DateTime ChangedAt { get; set; }
}

public class UpdatePricingRequest
{
    public decimal? EstimatedPrice { get; set; }
    public decimal? FinalPrice { get; set; }
    public string? Currency { get; set; }
    public string? Notes { get; set; }
}

public class AddNoteRequest
{
    public string Note { get; set; } = string.Empty;
} 