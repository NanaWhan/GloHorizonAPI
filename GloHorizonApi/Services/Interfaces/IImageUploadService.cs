namespace GloHorizonApi.Services.Interfaces;

public interface IImageUploadService
{
    Task<ImageUploadResponse> UploadImageAsync(IFormFile file, string folder, string? fileName = null);
    Task<ImageUploadResponse> UploadMultipleImagesAsync(IFormFileCollection files, string folder);
    Task<bool> DeleteImageAsync(string filePath);
    Task<string> GetImageUrlAsync(string filePath);
    string GenerateUniqueFileName(string originalFileName);
}

public class ImageUploadResponse
{
    public bool Success { get; set; }
    public string? PublicUrl { get; set; }
    public string? FilePath { get; set; }
    public string? Error { get; set; }
    public long? FileSize { get; set; }
    public string? ContentType { get; set; }
} 