using GloHorizonApi.Services.Interfaces;
using Supabase;
using System.Text;

namespace GloHorizonApi.Services.Providers;

public class SupabaseImageUploadService : IImageUploadService
{
    private readonly Client _supabaseClient;
    private readonly ILogger<SupabaseImageUploadService> _logger;
    private readonly IConfiguration _configuration;
    
    // Allowed image file extensions
    private readonly string[] _allowedExtensions = { ".jpg", ".jpeg", ".png", ".gif", ".webp", ".bmp" };
    
    // Maximum file size (5MB)
    private readonly long _maxFileSize = 5 * 1024 * 1024;
    
    // Supabase Storage bucket name
    private readonly string _bucketName = "travel-images";

    public SupabaseImageUploadService(
        Client supabaseClient,
        ILogger<SupabaseImageUploadService> logger,
        IConfiguration configuration)
    {
        _supabaseClient = supabaseClient;
        _logger = logger;
        _configuration = configuration;
    }

    public async Task<ImageUploadResponse> UploadImageAsync(IFormFile file, string folder, string? fileName = null)
    {
        try
        {
            // Validate file
            var validationResult = ValidateFile(file);
            if (!validationResult.Success)
            {
                return validationResult;
            }

            // Generate unique filename if not provided
            var finalFileName = fileName ?? GenerateUniqueFileName(file.FileName);
            var filePath = $"{folder}/{finalFileName}";

            _logger.LogInformation($"Uploading image to Supabase Storage: {filePath}");

            // Convert IFormFile to byte array
            using var memoryStream = new MemoryStream();
            await file.CopyToAsync(memoryStream);
            var fileBytes = memoryStream.ToArray();

            // Upload to Supabase Storage
            var result = await _supabaseClient.Storage
                .From(_bucketName)
                .Upload(fileBytes, filePath, new Supabase.Storage.FileOptions 
                { 
                    ContentType = file.ContentType,
                    Upsert = true // Allow overwriting if file exists
                });

            if (result == null)
            {
                _logger.LogError($"Failed to upload image to Supabase Storage: {filePath}");
                return new ImageUploadResponse
                {
                    Success = false,
                    Error = "Failed to upload image to storage"
                };
            }

            // Get public URL
            var publicUrl = _supabaseClient.Storage
                .From(_bucketName)
                .GetPublicUrl(filePath);

            _logger.LogInformation($"Image uploaded successfully: {filePath}, URL: {publicUrl}");

            return new ImageUploadResponse
            {
                Success = true,
                PublicUrl = publicUrl,
                FilePath = filePath,
                FileSize = file.Length,
                ContentType = file.ContentType
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"Error uploading image: {file.FileName}");
            return new ImageUploadResponse
            {
                Success = false,
                Error = $"Upload failed: {ex.Message}"
            };
        }
    }

    public async Task<ImageUploadResponse> UploadMultipleImagesAsync(IFormFileCollection files, string folder)
    {
        var uploadResults = new List<string>();
        var errors = new List<string>();

        foreach (var file in files)
        {
            var result = await UploadImageAsync(file, folder);
            if (result.Success && result.PublicUrl != null)
            {
                uploadResults.Add(result.PublicUrl);
            }
            else
            {
                errors.Add($"{file.FileName}: {result.Error}");
            }
        }

        if (errors.Any())
        {
            return new ImageUploadResponse
            {
                Success = false,
                Error = string.Join("; ", errors)
            };
        }

        return new ImageUploadResponse
        {
            Success = true,
            PublicUrl = string.Join(",", uploadResults), // Return comma-separated URLs
            FileSize = files.Sum(f => f.Length)
        };
    }

    public async Task<bool> DeleteImageAsync(string filePath)
    {
        try
        {
            _logger.LogInformation($"Deleting image from Supabase Storage: {filePath}");

            var result = await _supabaseClient.Storage
                .From(_bucketName)
                .Remove(new List<string> { filePath });

            _logger.LogInformation($"Image deleted successfully: {filePath}");
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"Error deleting image: {filePath}");
            return false;
        }
    }

    public async Task<string> GetImageUrlAsync(string filePath)
    {
        try
        {
            return await Task.FromResult(_supabaseClient.Storage
                .From(_bucketName)
                .GetPublicUrl(filePath));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"Error getting image URL: {filePath}");
            return string.Empty;
        }
    }

    public string GenerateUniqueFileName(string originalFileName)
    {
        var extension = Path.GetExtension(originalFileName).ToLowerInvariant();
        var uniqueFileName = $"{DateTime.UtcNow:yyyyMMdd_HHmmss}_{Guid.NewGuid():N}{extension}";
        return uniqueFileName;
    }

    private ImageUploadResponse ValidateFile(IFormFile file)
    {
        // Check if file is null or empty
        if (file == null || file.Length == 0)
        {
            return new ImageUploadResponse
            {
                Success = false,
                Error = "No file provided or file is empty"
            };
        }

        // Check file size
        if (file.Length > _maxFileSize)
        {
            return new ImageUploadResponse
            {
                Success = false,
                Error = $"File size exceeds maximum allowed size of {_maxFileSize / (1024 * 1024)}MB"
            };
        }

        // Check file extension
        var extension = Path.GetExtension(file.FileName).ToLowerInvariant();
        if (!_allowedExtensions.Contains(extension))
        {
            return new ImageUploadResponse
            {
                Success = false,
                Error = $"File type not allowed. Allowed types: {string.Join(", ", _allowedExtensions)}"
            };
        }

        // Check MIME type
        if (!file.ContentType.StartsWith("image/"))
        {
            return new ImageUploadResponse
            {
                Success = false,
                Error = "File must be an image"
            };
        }

        return new ImageUploadResponse { Success = true };
    }
} 