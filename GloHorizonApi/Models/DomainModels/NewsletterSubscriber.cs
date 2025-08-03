using System.ComponentModel.DataAnnotations;

namespace GloHorizonApi.Models.DomainModels;

public class NewsletterSubscriber
{
    public int Id { get; set; }
    
    [Required]
    [Phone]
    public string PhoneNumber { get; set; } = string.Empty;
    
    public DateTime SubscribedAt { get; set; } = DateTime.UtcNow;
    
    public bool IsActive { get; set; } = true;
    
    public DateTime? UnsubscribedAt { get; set; }
    
    public string? Source { get; set; } // Optional: track where subscription came from
}