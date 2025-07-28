using GloHorizonApi.Models.DomainModels;

namespace GloHorizonApi.Models.Dtos.Payment;

public class GenericPaymentRequest
{
    public decimal Amount { get; set; }
    public string TicketName { get; set; } = string.Empty;
    public PaymentEvent? Event { get; set; }
    public DomainModels.User User { get; set; } = new();
    public Discount? Discount { get; set; }
    public bool IsGroupTicket { get; set; }
    public string ClientReference { get; set; } = string.Empty;
}

public class PaymentEvent
{
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
}