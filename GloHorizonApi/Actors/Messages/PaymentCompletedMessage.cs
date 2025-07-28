namespace GloHorizonApi.Actors.Messages;

public class PaymentCompletedMessage
{
    public string TransactionReference { get; }
    public decimal Amount { get; }
    public string Phone { get; }

    public PaymentCompletedMessage(string transactionReference, decimal amount, string phone)
    {
        TransactionReference = transactionReference;
        Amount = amount;
        Phone = phone;
    }
}