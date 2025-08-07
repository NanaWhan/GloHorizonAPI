using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using System.Text.Json;
using GloHorizonApi.Data;
using GloHorizonApi.Models.DomainModels;
using GloHorizonApi.Models.Dtos.Admin;
using GloHorizonApi.Models.Dtos.Auth;
using GloHorizonApi.Models.Dtos.Booking;
using GloHorizonApi.Services.Providers;
using GloHorizonApi.Services.Interfaces;
using BCrypt.Net;

namespace GloHorizonApi.Controllers;

[ApiController]
[Route("api/[controller]")]
public class AdminController : ControllerBase
{
    private readonly ApplicationDbContext _context;
    private readonly JwtTokenGenerator _jwtTokenGenerator;
    private readonly ILogger<AdminController> _logger;
    private readonly ISmsService _smsService;

    public AdminController(
        ApplicationDbContext context,
        JwtTokenGenerator jwtTokenGenerator,
        ILogger<AdminController> logger,
        ISmsService smsService)
    {
        _context = context;
        _jwtTokenGenerator = jwtTokenGenerator;
        _logger = logger;
        _smsService = smsService;
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
                    FirstName = admin.FullName.Split(' ', 2).FirstOrDefault() ?? "",
                    LastName = admin.FullName.Split(' ', 2).Skip(1).FirstOrDefault() ?? "",
                    Email = admin.Email,
                    PhoneNumber = admin.PhoneNumber,
                    Role = admin.Role.ToString(),
                    CreatedAt = admin.CreatedAt
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
    public async Task<ActionResult<BookingListResponse>> GetAllBookings([FromQuery] BookingFilterDto filter)
    {
        try
        {
            var query = _context.BookingRequests
                .Include(b => b.User)
                .Include(b => b.StatusHistory)
                .Include(b => b.Documents)
                .AsQueryable();

            // Apply filters
            if (filter.Status.HasValue)
                query = query.Where(b => b.Status == filter.Status.Value);

            if (filter.ServiceType.HasValue)
                query = query.Where(b => b.ServiceType == filter.ServiceType.Value);

            if (filter.Urgency.HasValue)
                query = query.Where(b => b.Urgency == filter.Urgency.Value);

            if (filter.FromDate.HasValue)
                query = query.Where(b => b.CreatedAt >= filter.FromDate.Value);

            if (filter.ToDate.HasValue)
                query = query.Where(b => b.CreatedAt <= filter.ToDate.Value);

            if (!string.IsNullOrEmpty(filter.SearchTerm))
            {
                var searchTerm = filter.SearchTerm.ToLower();
                query = query.Where(b => 
                    b.ReferenceNumber.ToLower().Contains(searchTerm) ||
                    b.ContactEmail.ToLower().Contains(searchTerm) ||
                    b.ContactPhone.Contains(searchTerm) ||
                    (b.Destination != null && b.Destination.ToLower().Contains(searchTerm)) ||
                    (b.User.FirstName + " " + b.User.LastName).ToLower().Contains(searchTerm) ||
                    (b.SpecialRequests != null && b.SpecialRequests.ToLower().Contains(searchTerm))
                );
            }

            // Get total count before pagination
            var totalCount = await query.CountAsync();

            // Apply pagination
            var bookings = await query
                .OrderByDescending(b => b.CreatedAt)
                .Skip((filter.PageNumber - 1) * filter.PageSize)
                .Take(filter.PageSize)
                .Select(b => new BookingTrackingDto
                {
                    Id = b.Id,
                    ReferenceNumber = b.ReferenceNumber,
                    ServiceType = b.ServiceType,
                    Status = b.Status,
                    Urgency = b.Urgency,
                    CreatedAt = b.CreatedAt,
                    QuotedAmount = b.QuotedAmount,
                    FinalAmount = b.FinalAmount,
                    Currency = b.Currency,
                    ContactEmail = b.ContactEmail,
                    ContactPhone = b.ContactPhone,
                    Destination = b.Destination,
                    TravelDate = b.TravelDate,
                    SpecialRequests = b.SpecialRequests,
                    AdminNotes = b.AdminNotes,
                    StatusHistory = b.StatusHistory.OrderByDescending(h => h.ChangedAt).Take(3).Select(h => new BookingStatusHistoryDto
                    {
                        FromStatus = h.FromStatus,
                        ToStatus = h.ToStatus,
                        Notes = h.Notes,
                        ChangedAt = h.ChangedAt,
                        ChangedBy = h.ChangedBy
                    }).ToList(),
                    Documents = b.Documents.Select(d => new BookingDocumentDto
                    {
                        Id = d.Id,
                        DocumentType = d.DocumentType,
                        FileName = d.FileName,
                        FileUrl = d.FileUrl,
                        UploadedAt = d.UploadedAt
                    }).ToList()
                })
                .ToListAsync();

            var totalPages = (int)Math.Ceiling((double)totalCount / filter.PageSize);

            return Ok(new BookingListResponse
            {
                Success = true,
                Message = "Bookings retrieved successfully",
                Bookings = bookings,
                TotalCount = totalCount,
                PageNumber = filter.PageNumber,
                PageSize = filter.PageSize,
                TotalPages = totalPages
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving bookings for admin");
            return StatusCode(500, new BookingListResponse { Success = false, Message = "An error occurred retrieving bookings" });
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
                booking.QuotedAmount = request.EstimatedPrice.Value;

            if (request.FinalPrice.HasValue)
                booking.FinalAmount = request.FinalPrice.Value;

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
                EstimatedPrice = booking.QuotedAmount,
                FinalPrice = booking.FinalAmount,
                Currency = booking.Currency,
                AdminNotes = booking.AdminNotes,
                User = new UserInfo
                {
                    Id = booking.User.Id,
                    FirstName = booking.User.FirstName,
                    LastName = booking.User.LastName,
                    Email = booking.User.Email,
                    PhoneNumber = booking.User.PhoneNumber,
                    Role = booking.User.Role,
                    CreatedAt = booking.User.CreatedAt
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
                // Parse booking details from JSON based on service type
                BookingDetails = GetBookingDetailsAsDictionary(booking)
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

            var oldEstimatedPrice = booking.QuotedAmount;
            var oldFinalPrice = booking.FinalAmount;

            // Update pricing
            if (request.EstimatedPrice.HasValue)
                booking.QuotedAmount = request.EstimatedPrice.Value;

            if (request.FinalPrice.HasValue)
                booking.FinalAmount = request.FinalPrice.Value;

            if (!string.IsNullOrEmpty(request.Currency))
                booking.Currency = request.Currency;

            booking.UpdatedAt = DateTime.UtcNow;

            // Add status history for pricing update
            var statusHistory = new BookingStatusHistory
            {
                BookingRequestId = booking.Id,
                FromStatus = booking.Status,
                ToStatus = booking.Status, // Status remains same, just pricing updated
                Notes = $"Pricing updated: Est: {oldEstimatedPrice} → {booking.QuotedAmount}, Final: {oldFinalPrice} → {booking.FinalAmount}. Reason: {request.Notes}",
                ChangedBy = User.FindFirst("FullName")?.Value ?? "Admin",
                ChangedAt = DateTime.UtcNow
            };

            _context.BookingStatusHistories.Add(statusHistory);
            await _context.SaveChangesAsync();

            _logger.LogInformation($"Pricing updated for booking {booking.ReferenceNumber} by {statusHistory.ChangedBy}");

            return Ok(new { 
                Success = true, 
                Message = "Booking pricing updated successfully",
                EstimatedPrice = booking.QuotedAmount,
                FinalPrice = booking.FinalAmount,
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
            if (booking.Status != BookingStatus.QuoteProvided)
            {
                return BadRequest(new { 
                    Success = false, 
                    Message = $"Booking must be in 'QuoteAccepted' status to generate payment link. Current status: {booking.Status}" 
                });
            }

            if (!booking.FinalAmount.HasValue || booking.FinalAmount <= 0)
            {
                return BadRequest(new { 
                    Success = false, 
                    Message = "Final price must be set before generating payment link" 
                });
            }

            // Create payment request
            var paymentRequest = new Models.Dtos.Payment.GenericPaymentRequest
            {
                Amount = booking.FinalAmount.Value,
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
                Notes = $"Payment link generated. Amount: {booking.FinalAmount:C}",
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
                Amount = booking.FinalAmount,
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
                .CountAsync(b => b.Status == BookingStatus.Submitted);
            var completedBookings = await _context.BookingRequests
                .CountAsync(b => b.Status == BookingStatus.Completed);
            var totalUsers = await _context.Users.CountAsync();

            var recentBookings = await _context.BookingRequests
                .Include(b => b.User)
                .OrderByDescending(b => b.CreatedAt)
                .Take(10)
                .Select(b => new BookingTrackingDto
                {
                    Id = b.Id,
                    ReferenceNumber = b.ReferenceNumber,
                    ServiceType = b.ServiceType,
                    Status = b.Status,
                    Urgency = b.Urgency,
                    CreatedAt = b.CreatedAt,
                    ContactEmail = b.ContactEmail,
                    ContactPhone = b.ContactPhone,
                    Destination = b.Destination,
                    TravelDate = b.TravelDate,
                    QuotedAmount = b.QuotedAmount,
                    FinalAmount = b.FinalAmount,
                    Currency = b.Currency,
                    SpecialRequests = b.SpecialRequests,
                    AdminNotes = b.AdminNotes
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

    [HttpPost("create-test-admin")]
    [AllowAnonymous] // TEMPORARY - for development only
    public async Task<ActionResult> CreateTestAdmin()
    {
        try
        {
            // Check if admin already exists
            var existingAdmin = await _context.Admins
                .FirstOrDefaultAsync(a => a.Email == "admin@globalhorizons.com");

            if (existingAdmin != null)
            {
                return Ok(new 
                { 
                    Success = true, 
                    Message = "Test admin already exists",
                    Email = "admin@globalhorizons.com",
                    Role = existingAdmin.Role.ToString()
                });
            }

            // Create new test admin
            var hashedPassword = BCrypt.Net.BCrypt.HashPassword("TestAdmin123!");
            
            var testAdmin = new Admin
            {
                FullName = "Test Admin",
                Email = "admin@globalhorizons.com",
                PhoneNumber = "0205078908",
                PasswordHash = hashedPassword,
                Role = AdminRole.SuperAdmin,
                IsActive = true,
                ReceiveEmailNotifications = true,
                ReceiveSmsNotifications = true,
                CreatedBy = "Development Script"
            };

            _context.Admins.Add(testAdmin);
            await _context.SaveChangesAsync();

            return Ok(new 
            { 
                Success = true, 
                Message = "Test admin created successfully!",
                Email = "admin@globalhorizons.com",
                Password = "TestAdmin123!",
                Role = "SuperAdmin",
                AdminId = testAdmin.Id
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating test admin");
            return StatusCode(500, new { Success = false, Message = "Error creating test admin: " + ex.Message });
        }
    }

    [HttpPost("broadcast-sms")]
    [Authorize]
    public async Task<ActionResult<BroadcastSmsResponse>> SendBroadcastSms([FromBody] BroadcastSmsRequest request)
    {
        try
        {
            // Verify the requesting user is an admin with appropriate permissions
            var currentAdminRole = User.FindFirst("Role")?.Value;
            if (currentAdminRole != AdminRole.SuperAdmin.ToString() && currentAdminRole != AdminRole.Admin.ToString())
            {
                return Forbid("Only Admins can send broadcast SMS messages");
            }

            if (string.IsNullOrWhiteSpace(request.Message))
            {
                return BadRequest(new { Success = false, Message = "Message cannot be empty" });
            }

            if (request.Message.Length > 1000)
            {
                return BadRequest(new { Success = false, Message = "Message cannot exceed 1000 characters" });
            }

            List<string> phoneNumbers;

            // Determine recipients based on request type
            switch (request.RecipientType)
            {
                case BroadcastRecipientType.AllUsers:
                    phoneNumbers = await _context.Users
                        .Where(u => !string.IsNullOrEmpty(u.PhoneNumber))
                        .Select(u => u.PhoneNumber)
                        .ToListAsync();
                    break;

                case BroadcastRecipientType.NewsletterSubscribers:
                    phoneNumbers = await _context.NewsletterSubscribers
                        .Where(n => n.IsActive && !string.IsNullOrEmpty(n.PhoneNumber))
                        .Select(n => n.PhoneNumber)
                        .ToListAsync();
                    break;

                case BroadcastRecipientType.CustomList:
                    phoneNumbers = request.PhoneNumbers ?? new List<string>();
                    break;

                case BroadcastRecipientType.RecentBookers:
                    var thirtyDaysAgo = DateTime.UtcNow.AddDays(-30);
                    phoneNumbers = await _context.BookingRequests
                        .Where(b => b.CreatedAt >= thirtyDaysAgo && !string.IsNullOrEmpty(b.ContactPhone))
                        .Select(b => b.ContactPhone)
                        .Distinct()
                        .ToListAsync();
                    break;

                default:
                    return BadRequest(new { Success = false, Message = "Invalid recipient type" });
            }

            if (!phoneNumbers.Any())
            {
                return BadRequest(new { Success = false, Message = "No recipients found for the specified criteria" });
            }

            // Add admin signature to message
            var adminName = User.FindFirst("FullName")?.Value ?? "Admin";
            var finalMessage = $"{request.Message}\n\n- {adminName}, Global Horizons Travel";

            // Send broadcast SMS
            var result = await _smsService.SendBroadcastSmsAsync(phoneNumbers, finalMessage);

            // Log the broadcast activity
            _logger.LogInformation("Broadcast SMS sent by {AdminName} to {RecipientType}: {Successful}/{Total} successful", 
                adminName, request.RecipientType, result.SuccessfulSends, result.TotalRecipients);

            return Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error sending broadcast SMS");
            return StatusCode(500, new BroadcastSmsResponse
            {
                Success = false,
                Message = "An error occurred while sending broadcast SMS"
            });
        }
    }

    // ===============================
    // QUOTE MANAGEMENT ENDPOINTS  
    // ===============================

    [HttpGet("quotes")]
    [Authorize]
    public async Task<ActionResult<QuoteListResponse>> GetAllQuotes([FromQuery] QuoteFilterDto filter)
    {
        try
        {
            var query = _context.QuoteRequests
                .Include(q => q.StatusHistory)
                .AsQueryable();

            // Apply filters
            if (filter.Status.HasValue)
                query = query.Where(q => q.Status == filter.Status.Value);

            if (filter.ServiceType.HasValue)
                query = query.Where(q => q.ServiceType == filter.ServiceType.Value);

            if (filter.Urgency.HasValue)
                query = query.Where(q => q.Urgency == filter.Urgency.Value);

            if (filter.FromDate.HasValue)
                query = query.Where(q => q.CreatedAt >= filter.FromDate.Value);

            if (filter.ToDate.HasValue)
                query = query.Where(q => q.CreatedAt <= filter.ToDate.Value);

            if (!string.IsNullOrEmpty(filter.SearchTerm))
            {
                var searchTerm = filter.SearchTerm.ToLower();
                query = query.Where(q => 
                    q.ReferenceNumber.ToLower().Contains(searchTerm) ||
                    q.ContactEmail.ToLower().Contains(searchTerm) ||
                    q.ContactPhone.Contains(searchTerm) ||
                    q.ContactName.ToLower().Contains(searchTerm) ||
                    (q.Destination != null && q.Destination.ToLower().Contains(searchTerm)) ||
                    (q.SpecialRequests != null && q.SpecialRequests.ToLower().Contains(searchTerm))
                );
            }

            // Get total count before pagination
            var totalCount = await query.CountAsync();

            // Apply pagination
            var quotes = await query
                .OrderByDescending(q => q.CreatedAt)
                .Skip((filter.PageNumber - 1) * filter.PageSize)
                .Take(filter.PageSize)
                .Select(q => new AdminQuoteDto
                {
                    Id = q.Id,
                    ReferenceNumber = q.ReferenceNumber,
                    ServiceType = q.ServiceType,
                    Status = q.Status,
                    Urgency = q.Urgency,
                    CreatedAt = q.CreatedAt,
                    ContactEmail = q.ContactEmail,
                    ContactPhone = q.ContactPhone,
                    ContactName = q.ContactName,
                    Destination = q.Destination,
                    SpecialRequests = q.SpecialRequests,
                    AdminNotes = q.AdminNotes,
                    EstimatedPrice = q.QuotedAmount,
                    Currency = q.Currency
                })
                .ToListAsync();

            var totalPages = (int)Math.Ceiling((double)totalCount / filter.PageSize);

            return Ok(new QuoteListResponse
            {
                Success = true,
                Message = "Quotes retrieved successfully",
                Quotes = quotes,
                TotalCount = totalCount,
                PageNumber = filter.PageNumber,
                PageSize = filter.PageSize,
                TotalPages = totalPages
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving quotes for admin");
            return StatusCode(500, new QuoteListResponse { Success = false, Message = "An error occurred retrieving quotes" });
        }
    }

    [HttpGet("quotes/{id}")]
    [Authorize]
    public async Task<ActionResult<DetailedQuoteInfo>> GetQuote(int id)
    {
        try
        {
            var quote = await _context.QuoteRequests
                .Include(q => q.StatusHistory)
                .FirstOrDefaultAsync(q => q.Id == id);

            if (quote == null)
            {
                return NotFound($"Quote with ID {id} not found");
            }

            var detailedQuote = new DetailedQuoteInfo
            {
                Id = quote.Id,
                ReferenceNumber = quote.ReferenceNumber,
                ServiceType = quote.ServiceType,
                Status = quote.Status,
                Urgency = quote.Urgency,
                CreatedAt = quote.CreatedAt,
                UpdatedAt = quote.UpdatedAt,
                ContactEmail = quote.ContactEmail,
                ContactPhone = quote.ContactPhone,
                ContactName = quote.ContactName,
                Destination = quote.Destination,
                SpecialRequests = quote.SpecialRequests,
                AdminNotes = quote.AdminNotes,
                EstimatedPrice = quote.QuotedAmount,
                Currency = quote.Currency,
                StatusHistory = quote.StatusHistory
                    .OrderByDescending(h => h.ChangedAt)
                    .Select(h => new AdminQuoteStatusHistoryInfo
                    {
                        FromStatus = h.FromStatus,
                        ToStatus = h.ToStatus,
                        Notes = h.Notes,
                        ChangedBy = h.ChangedBy,
                        ChangedAt = h.ChangedAt
                    }).ToList(),
                QuoteDetails = GetQuoteDetailsAsDictionary(quote)
            };

            return Ok(detailedQuote);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"Error retrieving quote details for ID: {id}");
            return StatusCode(500, "An error occurred retrieving quote details");
        }
    }

    [HttpPut("quotes/{id}/status")]
    [Authorize]
    public async Task<ActionResult> UpdateQuoteStatus(int id, [FromBody] UpdateQuoteStatusRequest request)
    {
        try
        {
            var quote = await _context.QuoteRequests
                .FirstOrDefaultAsync(q => q.Id == id);

            if (quote == null)
            {
                return NotFound("Quote not found");
            }

            var oldStatus = quote.Status;
            quote.Status = request.NewStatus;
            quote.UpdatedAt = DateTime.UtcNow;
            quote.AdminNotes = request.AdminNotes;

            if (request.EstimatedPrice.HasValue)
                quote.QuotedAmount = request.EstimatedPrice.Value;

            // Add status history
            var statusHistory = new QuoteStatusHistory
            {
                QuoteRequestId = quote.Id,
                FromStatus = oldStatus,
                ToStatus = request.NewStatus,
                Notes = request.Notes,
                ChangedBy = User.FindFirst("FullName")?.Value ?? "Admin",
                ChangedAt = DateTime.UtcNow
            };

            _context.QuoteStatusHistories.Add(statusHistory);
            await _context.SaveChangesAsync();

            _logger.LogInformation($"Quote {quote.ReferenceNumber} status updated from {oldStatus} to {request.NewStatus}");

            return Ok(new { Success = true, Message = "Quote status updated successfully" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating quote status");
            return StatusCode(500, "An error occurred updating quote status");
        }
    }

    [HttpPut("quotes/{id}/pricing")]
    [Authorize]
    public async Task<ActionResult> UpdateQuotePricing(int id, [FromBody] UpdateQuotePricingRequest request)
    {
        try
        {
            var quote = await _context.QuoteRequests
                .FirstOrDefaultAsync(q => q.Id == id);

            if (quote == null)
            {
                return NotFound($"Quote with ID {id} not found");
            }

            var oldEstimatedPrice = quote.QuotedAmount;

            // Update pricing
            if (request.EstimatedPrice.HasValue)
                quote.QuotedAmount = request.EstimatedPrice.Value;

            if (!string.IsNullOrEmpty(request.Currency))
                quote.Currency = request.Currency;

            quote.UpdatedAt = DateTime.UtcNow;

            // Add status history for pricing update
            var statusHistory = new QuoteStatusHistory
            {
                QuoteRequestId = quote.Id,
                FromStatus = quote.Status,
                ToStatus = quote.Status, // Status remains same, just pricing updated
                Notes = $"Pricing updated: {oldEstimatedPrice} → {quote.QuotedAmount} {quote.Currency}. Reason: {request.Notes}",
                ChangedBy = User.FindFirst("FullName")?.Value ?? "Admin",
                ChangedAt = DateTime.UtcNow
            };

            _context.QuoteStatusHistories.Add(statusHistory);
            await _context.SaveChangesAsync();

            _logger.LogInformation($"Pricing updated for quote {quote.ReferenceNumber} by {statusHistory.ChangedBy}");

            return Ok(new { 
                Success = true, 
                Message = "Quote pricing updated successfully",
                EstimatedPrice = quote.QuotedAmount,
                Currency = quote.Currency
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"Error updating pricing for quote ID: {id}");
            return StatusCode(500, "An error occurred updating quote pricing");
        }
    }

    [HttpPost("quotes/{id}/notes")]
    [Authorize]
    public async Task<ActionResult> AddQuoteNote(int id, [FromBody] AddNoteRequest request)
    {
        try
        {
            var quote = await _context.QuoteRequests
                .FirstOrDefaultAsync(q => q.Id == id);

            if (quote == null)
            {
                return NotFound($"Quote with ID {id} not found");
            }

            // Append new note to existing admin notes
            var adminName = User.FindFirst("FullName")?.Value ?? "Admin";
            var timestamp = DateTime.UtcNow.ToString("yyyy-MM-dd HH:mm:ss UTC");
            var newNote = $"[{timestamp}] {adminName}: {request.Note}";

            if (string.IsNullOrEmpty(quote.AdminNotes))
            {
                quote.AdminNotes = newNote;
            }
            else
            {
                quote.AdminNotes += "\n" + newNote;
            }

            quote.UpdatedAt = DateTime.UtcNow;

            // Add status history for note addition
            var statusHistory = new QuoteStatusHistory
            {
                QuoteRequestId = quote.Id,
                FromStatus = quote.Status,
                ToStatus = quote.Status, // Status remains same
                Notes = $"Admin note added: {request.Note}",
                ChangedBy = adminName,
                ChangedAt = DateTime.UtcNow
            };

            _context.QuoteStatusHistories.Add(statusHistory);
            await _context.SaveChangesAsync();

            _logger.LogInformation($"Note added to quote {quote.ReferenceNumber} by {adminName}");

            return Ok(new { 
                Success = true, 
                Message = "Note added successfully",
                AdminNotes = quote.AdminNotes
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"Error adding note to quote ID: {id}");
            return StatusCode(500, "An error occurred adding note to quote");
        }
    }

    // Helper method to extract quote details as dictionary
    private Dictionary<string, object> GetQuoteDetailsAsDictionary(QuoteRequest quote)
    {
        try
        {
            return quote.ServiceType switch
            {
                QuoteType.Hotel when !string.IsNullOrEmpty(quote.HotelDetails) =>
                    JsonSerializer.Deserialize<Dictionary<string, object>>(quote.HotelDetails) ?? new(),
                QuoteType.Flight when !string.IsNullOrEmpty(quote.FlightDetails) =>
                    JsonSerializer.Deserialize<Dictionary<string, object>>(quote.FlightDetails) ?? new(),
                QuoteType.Tour when !string.IsNullOrEmpty(quote.TourDetails) =>
                    JsonSerializer.Deserialize<Dictionary<string, object>>(quote.TourDetails) ?? new(),
                QuoteType.Visa when !string.IsNullOrEmpty(quote.VisaDetails) =>
                    JsonSerializer.Deserialize<Dictionary<string, object>>(quote.VisaDetails) ?? new(),
                QuoteType.CompletePackage when !string.IsNullOrEmpty(quote.PackageDetails) =>
                    JsonSerializer.Deserialize<Dictionary<string, object>>(quote.PackageDetails) ?? new(),
                _ => new Dictionary<string, object>()
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error parsing quote details for quote {ReferenceNumber}", quote.ReferenceNumber);
            return new Dictionary<string, object>();
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
    public List<BookingTrackingDto> RecentBookings { get; set; } = new();
}

// New DTOs for Week 1 endpoints
public class DetailedBookingInfo
{
    public int Id { get; set; }
    public string ReferenceNumber { get; set; } = string.Empty;
    public BookingType ServiceType { get; set; }
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

    // Helper method to extract booking details as dictionary
    private Dictionary<string, object> GetBookingDetailsAsDictionary(BookingRequest booking)
    {
        var details = new Dictionary<string, object>();
        
        switch (booking.ServiceType)
        {
            case BookingType.Flight when !string.IsNullOrEmpty(booking.FlightDetails):
                var flightDetails = JsonSerializer.Deserialize<Dictionary<string, object>>(booking.FlightDetails);
                return flightDetails ?? new Dictionary<string, object>();
                
            case BookingType.Hotel when !string.IsNullOrEmpty(booking.HotelDetails):
                var hotelDetails = JsonSerializer.Deserialize<Dictionary<string, object>>(booking.HotelDetails);
                return hotelDetails ?? new Dictionary<string, object>();
                
            case BookingType.Tour when !string.IsNullOrEmpty(booking.TourDetails):
                var tourDetails = JsonSerializer.Deserialize<Dictionary<string, object>>(booking.TourDetails);
                return tourDetails ?? new Dictionary<string, object>();
                
            case BookingType.Visa when !string.IsNullOrEmpty(booking.VisaDetails):
                var visaDetails = JsonSerializer.Deserialize<Dictionary<string, object>>(booking.VisaDetails);
                return visaDetails ?? new Dictionary<string, object>();
                
            case BookingType.CompletePackage when !string.IsNullOrEmpty(booking.PackageDetails):
                var packageDetails = JsonSerializer.Deserialize<Dictionary<string, object>>(booking.PackageDetails);
                return packageDetails ?? new Dictionary<string, object>();
                
            default:
                return new Dictionary<string, object>();
        }
    }
}

public class AddNoteRequest
{
    public string Note { get; set; } = string.Empty;
}

public class BroadcastSmsRequest
{
    public string Message { get; set; } = string.Empty;
    public BroadcastRecipientType RecipientType { get; set; }
    public List<string>? PhoneNumbers { get; set; }
}

public enum BroadcastRecipientType
{
    AllUsers,
    NewsletterSubscribers,
    CustomList,
    RecentBookers
}

// Quote Management DTOs
public class QuoteFilterDto
{
    public QuoteStatus? Status { get; set; }
    public QuoteType? ServiceType { get; set; }
    public UrgencyLevel? Urgency { get; set; }
    public DateTime? FromDate { get; set; }
    public DateTime? ToDate { get; set; }
    public string? SearchTerm { get; set; }
    public int PageNumber { get; set; } = 1;
    public int PageSize { get; set; } = 20;
}

public class QuoteListResponse
{
    public bool Success { get; set; }
    public string Message { get; set; } = string.Empty;
    public List<AdminQuoteDto> Quotes { get; set; } = new();
    public int TotalCount { get; set; }
    public int PageNumber { get; set; }
    public int PageSize { get; set; }
    public int TotalPages { get; set; }
}

public class AdminQuoteDto
{
    public int Id { get; set; }
    public string ReferenceNumber { get; set; } = string.Empty;
    public QuoteType ServiceType { get; set; }
    public QuoteStatus Status { get; set; }
    public UrgencyLevel Urgency { get; set; }
    public DateTime CreatedAt { get; set; }
    public string ContactEmail { get; set; } = string.Empty;
    public string ContactPhone { get; set; } = string.Empty;
    public string ContactName { get; set; } = string.Empty;
    public string? Destination { get; set; }
    public string? SpecialRequests { get; set; }
    public string? AdminNotes { get; set; }
    public decimal? EstimatedPrice { get; set; }
    public string? Currency { get; set; }
}

public class DetailedQuoteInfo
{
    public int Id { get; set; }
    public string ReferenceNumber { get; set; } = string.Empty;
    public QuoteType ServiceType { get; set; }
    public QuoteStatus Status { get; set; }
    public UrgencyLevel Urgency { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
    public string ContactEmail { get; set; } = string.Empty;
    public string ContactPhone { get; set; } = string.Empty;
    public string ContactName { get; set; } = string.Empty;
    public string? Destination { get; set; }
    public string? SpecialRequests { get; set; }
    public string? AdminNotes { get; set; }
    public decimal? EstimatedPrice { get; set; }
    public string? Currency { get; set; }
    public List<AdminQuoteStatusHistoryInfo> StatusHistory { get; set; } = new();
    public Dictionary<string, object> QuoteDetails { get; set; } = new();
}

public class AdminQuoteStatusHistoryInfo
{
    public QuoteStatus FromStatus { get; set; }
    public QuoteStatus ToStatus { get; set; }
    public string? Notes { get; set; }
    public string ChangedBy { get; set; } = string.Empty;
    public DateTime ChangedAt { get; set; }
}

public class UpdateQuoteStatusRequest
{
    public QuoteStatus NewStatus { get; set; }
    public string? Notes { get; set; }
    public string? AdminNotes { get; set; }
    public decimal? EstimatedPrice { get; set; }
}

public class UpdateQuotePricingRequest
{
    public decimal? EstimatedPrice { get; set; }
    public string? Currency { get; set; }
    public string? Notes { get; set; }
} 