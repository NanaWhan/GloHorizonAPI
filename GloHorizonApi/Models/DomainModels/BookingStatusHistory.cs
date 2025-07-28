using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace GloHorizonApi.Models.DomainModels;

public class BookingStatusHistory
{
    [Key]
    public int Id { get; set; }
    
    [Required]
    public int BookingRequestId { get; set; }
    
    [Required]
    public BookingStatus FromStatus { get; set; }
    
    [Required] 
    public BookingStatus ToStatus { get; set; }
    
    [MaxLength(500)]
    public string? Notes { get; set; }
    
    [Required]
    [MaxLength(100)]
    public string ChangedBy { get; set; } = string.Empty; // Admin ID or "System"
    
    [Required]
    public DateTime ChangedAt { get; set; } = DateTime.UtcNow;
    
    // Navigation properties
    [ForeignKey("BookingRequestId")]
    public virtual BookingRequest BookingRequest { get; set; } = null!;
} 