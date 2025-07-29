using System.Text.Json;
using GloHorizonApi.Models.Dtos;
using GloHorizonApi.Models.Dtos.Payment;
using GloHorizonApi.Services.Interfaces;
using PayStack.Net;

namespace GloHorizonApi.Services.Providers;

public class PayStackPaymentService : IPayStackPaymentService
{
    private PayStackApi PayStackApi { get; set; }
    private readonly ILogger<PayStackPaymentService> _logger;
    private readonly IConfiguration _configuration;

    public PayStackPaymentService(
        ILogger<PayStackPaymentService> logger,
        IConfiguration configuration
    )
    {
        _logger = logger;
        _configuration = configuration;
        // PayStackApi = new PayStackApi(_configuration.GetValue<string>("PayStackKeys:test_key"));
        PayStackApi = new PayStackApi(_configuration.GetValue<string>("PayStackKeys:live_key"));
    }

    public Task<PayStackResponseDto> CreatePayLink(GenericPaymentRequest request)
    {
        try
        {
            var user = request.User;
            string description;

            // Check if Event is provided (it might be null for direct donations)
            if (request.Event != null)
            {
                var eventObj = request.Event;
                description = $"{request.TicketName} - {eventObj.Name}";
            }
            else
            {
                description = request.TicketName;
            }

            _logger.LogDebug($"Ticket description -- [{description}]");

            // Convert amount to kobo (smallest currency unit)
            var ticketPrice = decimal.Multiply(request.Amount, 100);

            // Use appropriate callback URL based on which key is being used
            var isLiveMode = PayStackApi.ToString().Contains("live") || 
                           _configuration.GetValue<string>("PayStackKeys:live_key")?.Contains(PayStackApi.ToString()) == true;
            var callbackUrl = isLiveMode 
                ? _configuration.GetValue<string>("PayStackKeys:prod_callback")
                : _configuration.GetValue<string>("PayStackKeys:test_callback");

            TransactionInitializeRequest payStackRequest = new()
            {
                Currency = "GHS",
                Email = user.Email,
                Channels = new[] { "mobile_money", "card" },
                AmountInKobo = decimal.ToInt32(ticketPrice),
                Reference = request.ClientReference,
                CallbackUrl = callbackUrl
            };

            // Add custom fields if needed
            payStackRequest.CustomFields.Add(
                CustomField.From("Ticket", "ticket-type", request.TicketName)
            );

            // Initialize the transaction
            var payStackResponse = PayStackApi.Transactions.Initialize(payStackRequest);

            _logger.LogDebug(
                $"PayStack Response after initialization: {JsonSerializer.Serialize(payStackResponse)}"
            );

            if (!payStackResponse.Status)
            {
                            return Task.FromResult(new PayStackResponseDto()
            {
                Message = "An error occurred generating pay link",
                Status = false
            });
            }

            var payLinkUrl = payStackResponse.Data.AuthorizationUrl;

            return Task.FromResult(new PayStackResponseDto()
            {
                Data = new PayStackDataDto
                {
                    AuthorizationUrl = payLinkUrl,
                    Reference = request.ClientReference
                },
                Status = true,
                Message = "Pay link generated"
            });
        }
        catch (Exception e)
        {
            _logger.LogError(e, "An error occurred generating pay link");

            return Task.FromResult(new PayStackResponseDto()
            {
                Status = false,
                Message = "An error occurred generating pay link"
            });
        }
    }

    public TransactionVerifyResponse VerifyTransaction(string reference)
    {
        try
        {
            return PayStackApi.Transactions.Verify(reference);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"Error verifying transaction {reference}");
            return null;
        }
    }

    public async Task<TransactionVerifyResponse> VerifyTransactionAsync(string reference)
    {
        try
        {
            _logger.LogInformation($"Verifying transaction asynchronously: {reference}");
            
            // PayStack.Net library doesn't have async methods, so we'll wrap the sync call
            return await Task.Run(() => PayStackApi.Transactions.Verify(reference));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"Error verifying transaction asynchronously: {reference}");
            throw;
        }
    }
} 