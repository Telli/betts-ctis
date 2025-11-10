using BettsTax.Data;
using BettsTax.Shared;
using Microsoft.Extensions.Logging;

namespace BettsTax.Core.Services.Payments;

public class LocalPaymentGateway : IPaymentGateway
{
    private readonly ILogger<LocalPaymentGateway> _logger;

    public LocalPaymentGateway(ILogger<LocalPaymentGateway> logger)
    {
        _logger = logger;
    }

    public PaymentProvider Provider => PaymentProvider.BankTransfer; // Represents local manual methods group

    public Task<Result<PaymentGatewayResponse>> InitiateAsync(PaymentGatewayRequest request, CancellationToken ct = default)
    {
        // Offline/local payment: immediately mark as Initiated (requires manual approval)
        var resp = new PaymentGatewayResponse
        {
            Success = true,
            TransactionId = request.TransactionReference,
            ProviderReference = request.TransactionReference,
            Status = PaymentTransactionStatus.Initiated,
            StatusMessage = "Local payment recorded. Awaiting manual confirmation.",
            Amount = request.Amount
        };
        _logger.LogInformation("Local payment initiated {Ref}", request.TransactionReference);
        return Task.FromResult(Result.Success(resp));
    }

    public Task<Result<PaymentGatewayResponse>> GetStatusAsync(string providerTransactionId, CancellationToken ct = default)
    {
        // For local methods status transitions are manual; return pending
        var resp = new PaymentGatewayResponse
        {
            Success = true,
            TransactionId = providerTransactionId,
            ProviderReference = providerTransactionId,
            Status = PaymentTransactionStatus.Pending,
            StatusMessage = "Awaiting manual reconciliation"
        };
        return Task.FromResult(Result.Success(resp));
    }

    public Task<Result<PaymentGatewayResponse>> RefundAsync(string providerTransactionId, decimal amount, CancellationToken ct = default)
    {
        var resp = new PaymentGatewayResponse
        {
            Success = true,
            TransactionId = providerTransactionId,
            ProviderReference = providerTransactionId,
            Status = PaymentTransactionStatus.Refunded,
            StatusMessage = "Manual refund recorded",
            Amount = amount
        };
        return Task.FromResult(Result.Success(resp));
    }

    public Task<Result<bool>> ValidateWebhookAsync(string rawBody, string signatureHeader, CancellationToken ct = default)
        => Task.FromResult(Result.Success(true)); // No webhooks for local methods

    public Task<Result<PaymentGatewayResponse>> ProcessWebhookAsync(string rawBody, CancellationToken ct = default)
        => Task.FromResult(Result.Failure<PaymentGatewayResponse>("No webhooks supported for local methods"));
}