using System.ComponentModel.DataAnnotations;

namespace GloHorizonApi.Models.Dtos.Newsletter;

public class NewsletterSubscriptionRequest
{
    [Required(ErrorMessage = "Phone number is required")]
    [Phone(ErrorMessage = "Please enter a valid phone number")]
    [RegularExpression(@"^(\+233|0)[2-9]\d{8}$", ErrorMessage = "Please enter a valid Ghanaian phone number")]
    public string PhoneNumber { get; set; } = string.Empty;
    
    public string? Source { get; set; } // Optional: website, mobile app, etc.
}

public class NewsletterSubscriptionResponse
{
    public bool Success { get; set; }
    public string Message { get; set; } = string.Empty;
    public string? PhoneNumber { get; set; }
    public DateTime? SubscribedAt { get; set; }
}

public class NewsletterUnsubscribeRequest
{
    [Required(ErrorMessage = "Phone number is required")]
    [Phone(ErrorMessage = "Please enter a valid phone number")]
    public string PhoneNumber { get; set; } = string.Empty;
}