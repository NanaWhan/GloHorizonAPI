using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using GloHorizonApi.Services.Interfaces;

namespace GloHorizonApi.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize] // Require authentication for image uploads
public class ImageController : ControllerBase
{
    private readonly IImageUploadService _imageUploadService;
    private readonly ILogger<ImageController> _logger;

    public ImageController(
        IImageUploadService imageUploadService,
        ILogger<ImageController> logger)
    {
        _imageUploadService = imageUploadService;
        _logger = logger;
    }

    /// <summary>
    /// Upload a single image for travel packages, user profiles, etc.
    /// </summary>
    [HttpPost("upload")]
    public async Task<IActionResult> UploadImage(
        IFormFile file, 
        [FromQuery] string folder = "general",
        [FromQuery] string? fileName = null)
    {
        try
        {
            if (file == null)
            {
                return BadRequest(new { Success = false, Error = "No file provided" });
            }

            _logger.LogInformation($"Uploading image: {file.FileName} to folder: {folder}");

            var result = await _imageUploadService.UploadImageAsync(file, folder, fileName);

            if (result.Success)
            {
                return Ok(new
                {
                    Success = true,
                    Message = "Image uploaded successfully",
                    Data = new
                    {
                        Url = result.PublicUrl,
                        FilePath = result.FilePath,
                        FileSize = result.FileSize,
                        ContentType = result.ContentType
                    }
                });
            }
            else
            {
                return BadRequest(new { Success = false, Error = result.Error });
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error uploading image");
            return StatusCode(500, new { Success = false, Error = "Internal server error" });
        }
    }

    /// <summary>
    /// Upload multiple images (for travel packages, galleries, etc.)
    /// </summary>
    [HttpPost("upload-multiple")]
    public async Task<IActionResult> UploadMultipleImages(
        IFormFileCollection files,
        [FromQuery] string folder = "general")
    {
        try
        {
            if (files == null || files.Count == 0)
            {
                return BadRequest(new { Success = false, Error = "No files provided" });
            }

            _logger.LogInformation($"Uploading {files.Count} images to folder: {folder}");

            var result = await _imageUploadService.UploadMultipleImagesAsync(files, folder);

            if (result.Success)
            {
                var urls = result.PublicUrl?.Split(',') ?? Array.Empty<string>();
                return Ok(new
                {
                    Success = true,
                    Message = $"Successfully uploaded {urls.Length} images",
                    Data = new
                    {
                        Urls = urls,
                        TotalFileSize = result.FileSize,
                        Count = urls.Length
                    }
                });
            }
            else
            {
                return BadRequest(new { Success = false, Error = result.Error });
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error uploading multiple images");
            return StatusCode(500, new { Success = false, Error = "Internal server error" });
        }
    }

    /// <summary>
    /// Upload user profile image
    /// </summary>
    [HttpPost("upload-profile")]
    public async Task<IActionResult> UploadProfileImage(IFormFile file)
    {
        try
        {
            var userId = User.Identity?.Name; // Assuming JWT contains user ID
            if (string.IsNullOrEmpty(userId))
            {
                return Unauthorized(new { Success = false, Error = "User not authenticated" });
            }

            var fileName = $"profile_{userId}_{DateTime.UtcNow:yyyyMMdd}";
            var result = await _imageUploadService.UploadImageAsync(file, "profiles", fileName);

            if (result.Success)
            {
                return Ok(new
                {
                    Success = true,
                    Message = "Profile image uploaded successfully",
                    Data = new
                    {
                        Url = result.PublicUrl,
                        FilePath = result.FilePath
                    }
                });
            }
            else
            {
                return BadRequest(new { Success = false, Error = result.Error });
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error uploading profile image");
            return StatusCode(500, new { Success = false, Error = "Internal server error" });
        }
    }

    /// <summary>
    /// Upload travel package images
    /// </summary>
    [HttpPost("upload-package")]
    public async Task<IActionResult> UploadPackageImages(
        IFormFileCollection files,
        [FromQuery] string packageId)
    {
        try
        {
            if (string.IsNullOrEmpty(packageId))
            {
                return BadRequest(new { Success = false, Error = "Package ID is required" });
            }

            var folder = $"packages/{packageId}";
            var result = await _imageUploadService.UploadMultipleImagesAsync(files, folder);

            if (result.Success)
            {
                var urls = result.PublicUrl?.Split(',') ?? Array.Empty<string>();
                return Ok(new
                {
                    Success = true,
                    Message = $"Successfully uploaded {urls.Length} package images",
                    Data = new
                    {
                        PackageId = packageId,
                        Urls = urls,
                        Count = urls.Length
                    }
                });
            }
            else
            {
                return BadRequest(new { Success = false, Error = result.Error });
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error uploading package images");
            return StatusCode(500, new { Success = false, Error = "Internal server error" });
        }
    }

    /// <summary>
    /// Delete an image
    /// </summary>
    [HttpDelete("delete")]
    public async Task<IActionResult> DeleteImage([FromQuery] string filePath)
    {
        try
        {
            if (string.IsNullOrEmpty(filePath))
            {
                return BadRequest(new { Success = false, Error = "File path is required" });
            }

            var result = await _imageUploadService.DeleteImageAsync(filePath);

            if (result)
            {
                return Ok(new
                {
                    Success = true,
                    Message = "Image deleted successfully"
                });
            }
            else
            {
                return BadRequest(new { Success = false, Error = "Failed to delete image" });
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting image");
            return StatusCode(500, new { Success = false, Error = "Internal server error" });
        }
    }

    /// <summary>
    /// Get image URL by file path
    /// </summary>
    [HttpGet("url")]
    [AllowAnonymous] // Allow public access to get image URLs
    public async Task<IActionResult> GetImageUrl([FromQuery] string filePath)
    {
        try
        {
            if (string.IsNullOrEmpty(filePath))
            {
                return BadRequest(new { Success = false, Error = "File path is required" });
            }

            var url = await _imageUploadService.GetImageUrlAsync(filePath);

            if (!string.IsNullOrEmpty(url))
            {
                return Ok(new
                {
                    Success = true,
                    Data = new { Url = url }
                });
            }
            else
            {
                return NotFound(new { Success = false, Error = "Image not found" });
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting image URL");
            return StatusCode(500, new { Success = false, Error = "Internal server error" });
        }
    }
} 