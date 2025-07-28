using PayStack.Net;
using GloHorizonApi.Models.Dtos.Payment;

namespace GloHorizonApi.Services.Interfaces;

public interface IPayStackPaymentService
{
    Task<PayStackResponseDto> CreatePayLink(GenericPaymentRequest request);
    TransactionVerifyResponse VerifyTransaction(string reference);
    Task<TransactionVerifyResponse> VerifyTransactionAsync(string reference);
}