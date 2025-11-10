using BettsTax.Data;
using BettsTax.Shared;
using Microsoft.Extensions.Logging;

namespace BettsTax.Core.Services.Payments;

public class AfricellMoneyGatewayAdapter : IPaymentGateway
{
    private readonly AfricellMoneyProvider _provider;
    private readonly ILogger<AfricellMoneyGatewayAdapter> _logger;

    public AfricellMoneyGatewayAdapter(AfricellMoneyProvider provider, ILogger<AfricellMoneyGatewayAdapter> logger)
    {
        _provider = provider;
        _logger = logger;
    }

    public PaymentProvider Provider => PaymentProvider.AfricellMoney;

    public Task<Result<PaymentGatewayResponse>> InitiateAsync(PaymentGatewayRequest request, CancellationToken ct = default)
        => _provider.InitiatePaymentAsync(request);

    public Task<Result<PaymentGatewayResponse>> GetStatusAsync(string providerTransactionId, CancellationToken ct = default)
        => _provider.CheckPaymentStatusAsync(providerTransactionId);

    public Task<Result<PaymentGatewayResponse>> RefundAsync(string providerTransactionId, decimal amount, CancellationToken ct = default)
        => _provider.RefundPaymentAsync(providerTransactionId, amount);

    public Task<Result<bool>> ValidateWebhookAsync(string rawBody, string signatureHeader, CancellationToken ct = default)
        => _provider.ValidateWebhookSignatureAsync(rawBody, signatureHeader);

    public Task<Result<PaymentGatewayResponse>> ProcessWebhookAsync(string rawBody, CancellationToken ct = default)
        => _provider.ProcessWebhookAsync(rawBody);
}