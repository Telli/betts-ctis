using BettsTax.Data;
using BettsTax.Shared;
using Microsoft.Extensions.Logging;

namespace BettsTax.Core.Services.Payments;

public class OrangeMoneyGatewayAdapter : IPaymentGateway
{
    private readonly OrangeMoneyProvider _provider;
    private readonly ILogger<OrangeMoneyGatewayAdapter> _logger;

    public OrangeMoneyGatewayAdapter(OrangeMoneyProvider provider, ILogger<OrangeMoneyGatewayAdapter> logger)
    {
        _provider = provider;
        _logger = logger;
    }

    public PaymentProvider Provider => PaymentProvider.OrangeMoney;

    public Task<Result<PaymentGatewayResponse>> InitiateAsync(PaymentGatewayRequest request, CancellationToken ct = default)
        => _provider.InitiatePaymentAsync(request);

    public Task<Result<PaymentGatewayResponse>> GetStatusAsync(string providerTransactionId, CancellationToken ct = default)
        => _provider.CheckPaymentStatusAsync(providerTransactionId);

    public Task<Result<PaymentGatewayResponse>> RefundAsync(string providerTransactionId, decimal amount, CancellationToken ct = default)
        => _provider.RefundPaymentAsync(providerTransactionId, amount);

    public async Task<Result<bool>> ValidateWebhookAsync(string rawBody, string signatureHeader, CancellationToken ct = default)
        => await _provider.ValidateWebhookSignatureAsync(rawBody, signatureHeader);

    public Task<Result<PaymentGatewayResponse>> ProcessWebhookAsync(string rawBody, CancellationToken ct = default)
        => _provider.ProcessWebhookAsync(rawBody);
}
