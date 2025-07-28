using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using System.Text.Json;
using GloHorizonApi.Data;
using GloHorizonApi.Models.DomainModels;
using GloHorizonApi.Models.Dtos.Package;

namespace GloHorizonApi.Controllers;

[ApiController]
[Route("api/[controller]")]
public class TravelPackageController : ControllerBase
{
    private readonly ApplicationDbContext _context;
    private readonly ILogger<TravelPackageController> _logger;

    public TravelPackageController(
        ApplicationDbContext context,
        ILogger<TravelPackageController> logger)
    {
        _context = context;
        _logger = logger;
    }

    [HttpGet]
    public async Task<ActionResult<List<TravelPackageDto>>> GetPackages(
        [FromQuery] PackageCategory? category = null,
        [FromQuery] string? destination = null,
        [FromQuery] decimal? minPrice = null,
        [FromQuery] decimal? maxPrice = null,
        [FromQuery] int? minDuration = null,
        [FromQuery] int? maxDuration = null,
        [FromQuery] bool? isFeatured = null,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20)
    {
        try
        {
            var query = _context.TravelPackages
                .Where(p => p.IsActive)
                .AsQueryable();

            // Apply filters
            if (category.HasValue)
                query = query.Where(p => p.Category == category.Value);

            if (!string.IsNullOrEmpty(destination))
                query = query.Where(p => p.Destination.ToLower().Contains(destination.ToLower()));

            if (minPrice.HasValue)
                query = query.Where(p => p.BasePrice >= minPrice.Value);

            if (maxPrice.HasValue)
                query = query.Where(p => p.BasePrice <= maxPrice.Value);

            if (minDuration.HasValue)
                query = query.Where(p => p.Duration >= minDuration.Value);

            if (maxDuration.HasValue)
                query = query.Where(p => p.Duration <= maxDuration.Value);

            if (isFeatured.HasValue)
                query = query.Where(p => p.IsFeatured == isFeatured.Value);

            var packages = await query
                .OrderBy(p => p.DisplayOrder)
                .ThenByDescending(p => p.CreatedAt)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .Select(p => new TravelPackageDto
                {
                    Id = p.Id,
                    Name = p.Name,
                    Description = p.Description,
                    Destination = p.Destination,
                    Duration = p.Duration,
                    BasePrice = p.BasePrice,
                    Currency = p.Currency,
                    ImageUrl = p.ImageUrl,
                    Category = p.Category,
                    IsFeatured = p.IsFeatured,
                    AvailableFrom = p.AvailableFrom,
                    AvailableUntil = p.AvailableUntil
                })
                .ToListAsync();

            return Ok(packages);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving travel packages");
            return StatusCode(500, "An error occurred retrieving travel packages");
        }
    }

    [HttpGet("{id}")]
    public async Task<ActionResult<TravelPackageDetailDto>> GetPackage(int id)
    {
        try
        {
            var package = await _context.TravelPackages
                .Where(p => p.Id == id && p.IsActive)
                .Select(p => new TravelPackageDetailDto
                {
                    Id = p.Id,
                    Name = p.Name,
                    Description = p.Description,
                    Destination = p.Destination,
                    Duration = p.Duration,
                    BasePrice = p.BasePrice,
                    Currency = p.Currency,
                    PricingRules = p.PricingRules,
                    PackageDetails = p.PackageDetails,
                    ImageUrl = p.ImageUrl,
                    ImageGallery = p.ImageGallery,
                    Category = p.Category,
                    IsFeatured = p.IsFeatured,
                    AvailableFrom = p.AvailableFrom,
                    AvailableUntil = p.AvailableUntil,
                    SeoTitle = p.SeoTitle,
                    SeoDescription = p.SeoDescription
                })
                .FirstOrDefaultAsync();

            if (package == null)
            {
                return NotFound("Travel package not found");
            }

            return Ok(package);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving travel package {PackageId}", id);
            return StatusCode(500, "An error occurred retrieving the travel package");
        }
    }

    [HttpPost]
    [Authorize]
    public async Task<ActionResult<TravelPackageDto>> CreatePackage([FromBody] CreateTravelPackageRequest request)
    {
        try
        {
            // Verify admin role
            var userRole = User.FindFirst(System.Security.Claims.ClaimTypes.Role)?.Value;
            if (userRole != "admin")
            {
                return Forbid("Only administrators can create travel packages");
            }

            var package = new TravelPackage
            {
                Name = request.Name,
                Description = request.Description,
                Destination = request.Destination,
                Duration = request.Duration,
                BasePrice = request.BasePrice,
                Currency = request.Currency ?? "GHS",
                PricingRules = request.PricingRules != null ? JsonSerializer.Serialize(request.PricingRules) : null,
                PackageDetails = request.PackageDetails != null ? JsonSerializer.Serialize(request.PackageDetails) : null,
                ImageUrl = request.ImageUrl,
                ImageGallery = request.ImageGallery != null ? JsonSerializer.Serialize(request.ImageGallery) : null,
                Category = request.Category,
                IsFeatured = request.IsFeatured,
                DisplayOrder = request.DisplayOrder,
                AvailableFrom = request.AvailableFrom,
                AvailableUntil = request.AvailableUntil,
                SeoTitle = request.SeoTitle,
                SeoDescription = request.SeoDescription,
                CreatedBy = User.FindFirst("Id")?.Value,
                CreatedAt = DateTime.UtcNow
            };

            _context.TravelPackages.Add(package);
            await _context.SaveChangesAsync();

            var packageDto = new TravelPackageDto
            {
                Id = package.Id,
                Name = package.Name,
                Description = package.Description,
                Destination = package.Destination,
                Duration = package.Duration,
                BasePrice = package.BasePrice,
                Currency = package.Currency,
                ImageUrl = package.ImageUrl,
                Category = package.Category,
                IsFeatured = package.IsFeatured,
                AvailableFrom = package.AvailableFrom,
                AvailableUntil = package.AvailableUntil
            };

            return CreatedAtAction(nameof(GetPackage), new { id = package.Id }, packageDto);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating travel package");
            return StatusCode(500, "An error occurred creating the travel package");
        }
    }

    [HttpPut("{id}")]
    [Authorize]
    public async Task<ActionResult> UpdatePackage(int id, [FromBody] UpdateTravelPackageRequest request)
    {
        try
        {
            // Verify admin role
            var userRole = User.FindFirst(System.Security.Claims.ClaimTypes.Role)?.Value;
            if (userRole != "admin")
            {
                return Forbid("Only administrators can update travel packages");
            }

            var package = await _context.TravelPackages.FindAsync(id);
            if (package == null)
            {
                return NotFound("Travel package not found");
            }

            // Update properties
            package.Name = request.Name;
            package.Description = request.Description;
            package.Destination = request.Destination;
            package.Duration = request.Duration;
            package.BasePrice = request.BasePrice;
            package.Currency = request.Currency ?? package.Currency;
            package.PricingRules = request.PricingRules != null ? JsonSerializer.Serialize(request.PricingRules) : package.PricingRules;
            package.PackageDetails = request.PackageDetails != null ? JsonSerializer.Serialize(request.PackageDetails) : package.PackageDetails;
            package.ImageUrl = request.ImageUrl ?? package.ImageUrl;
            package.ImageGallery = request.ImageGallery != null ? JsonSerializer.Serialize(request.ImageGallery) : package.ImageGallery;
            package.Category = request.Category;
            package.IsFeatured = request.IsFeatured;
            package.DisplayOrder = request.DisplayOrder;
            package.AvailableFrom = request.AvailableFrom;
            package.AvailableUntil = request.AvailableUntil;
            package.SeoTitle = request.SeoTitle;
            package.SeoDescription = request.SeoDescription;
            package.UpdatedBy = User.FindFirst("Id")?.Value;
            package.UpdatedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();

            return Ok(new { Success = true, Message = "Travel package updated successfully" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating travel package {PackageId}", id);
            return StatusCode(500, "An error occurred updating the travel package");
        }
    }

    [HttpDelete("{id}")]
    [Authorize]
    public async Task<ActionResult> DeletePackage(int id)
    {
        try
        {
            // Verify admin role
            var userRole = User.FindFirst(System.Security.Claims.ClaimTypes.Role)?.Value;
            if (userRole != "admin")
            {
                return Forbid("Only administrators can delete travel packages");
            }

            var package = await _context.TravelPackages.FindAsync(id);
            if (package == null)
            {
                return NotFound("Travel package not found");
            }

            // Soft delete by setting IsActive to false
            package.IsActive = false;
            package.UpdatedBy = User.FindFirst("Id")?.Value;
            package.UpdatedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();

            return Ok(new { Success = true, Message = "Travel package deleted successfully" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting travel package {PackageId}", id);
            return StatusCode(500, "An error occurred deleting the travel package");
        }
    }

    [HttpGet("featured")]
    public async Task<ActionResult<List<TravelPackageDto>>> GetFeaturedPackages()
    {
        try
        {
            var packages = await _context.TravelPackages
                .Where(p => p.IsActive && p.IsFeatured)
                .OrderBy(p => p.DisplayOrder)
                .Select(p => new TravelPackageDto
                {
                    Id = p.Id,
                    Name = p.Name,
                    Description = p.Description,
                    Destination = p.Destination,
                    Duration = p.Duration,
                    BasePrice = p.BasePrice,
                    Currency = p.Currency,
                    ImageUrl = p.ImageUrl,
                    Category = p.Category,
                    IsFeatured = p.IsFeatured
                })
                .ToListAsync();

            return Ok(packages);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving featured travel packages");
            return StatusCode(500, "An error occurred retrieving featured packages");
        }
    }

    [HttpGet("destinations")]
    public async Task<ActionResult<List<string>>> GetDestinations()
    {
        try
        {
            var destinations = await _context.TravelPackages
                .Where(p => p.IsActive)
                .Select(p => p.Destination)
                .Distinct()
                .OrderBy(d => d)
                .ToListAsync();

            return Ok(destinations);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving destinations");
            return StatusCode(500, "An error occurred retrieving destinations");
        }
    }
} 