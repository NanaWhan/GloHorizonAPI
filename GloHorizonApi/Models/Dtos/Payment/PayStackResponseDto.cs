namespace GloHorizonApi.Models.Dtos.Payment;

public class PayStackResponseDto
{
    public bool Status { get; set; }
    public string? Message { get; set; }
    public PayStackDataDto? Data { get; set; }
}

public class PayStackDataDto
{
    public string? AuthorizationUrl { get; set; }
    public string? AccessCode { get; set; }
    public string? Reference { get; set; }
    public string? Domain { get; set; }
    public string? Status { get; set; }
    public string? Gateway { get; set; }
    public string? Channel { get; set; }
    public string? Currency { get; set; }
    public decimal? Amount { get; set; }
    public string? PaidAt { get; set; }
    public string? CreatedAt { get; set; }
    public string? UpdatedAt { get; set; }
}