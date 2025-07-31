using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace GloHorizonApi.Models.DomainModels;

public class BookingDocument
{
    [Key]
    public int Id { get; set; }
    
    [Required]
    public int BookingRequestId { get; set; }
    
    [Required]
    [MaxLength(100)]
    public string DocumentType { get; set; } = string.Empty;
    
    [Required]
    [MaxLength(255)]
    public string FileName { get; set; } = string.Empty;
    
    [Required]
    public string FileUrl { get; set; } = string.Empty;
    
    [MaxLength(50)]
    public string FileSize { get; set; } = string.Empty;
    
    [MaxLength(20)]
    public string ContentType { get; set; } = string.Empty;
    
    public DateTime UploadedAt { get; set; } = DateTime.UtcNow;
    
    [MaxLength(255)]
    public string? UploadedBy { get; set; }
    
    public bool IsRequired { get; set; } = false;
    
    public bool IsVerified { get; set; } = false;
    
    // Navigation properties
    [ForeignKey("BookingRequestId")]
    public virtual BookingRequest BookingRequest { get; set; } = null!;
} 