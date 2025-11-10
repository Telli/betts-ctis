using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using BettsTax.Core.DTOs.Payment;
using BettsTax.Core.Services.Interfaces;
using BettsTax.Data;
using BettsTax.Data.Models;
using GatewayTransaction = BettsTax.Data.Models.PaymentTransaction;
using GatewayTransactionStatus = BettsTax.Data.Models.PaymentTransactionStatus;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Quartz;

namespace BettsTax.Core.Jobs;

/// <summary>
/// Background poller that keeps pending payment gateway transactions moving forward.
/// It checks for expired sessions, calls into the provider-specific services when available,
/// and marks transactions as failed when they time out.
/// </summary>
[DisallowConcurrentExecution]
public class PaymentGatewayPollingJob : IJob
{
    private static readonly TimeSpan InitiatedDispatchDelay = TimeSpan.FromSeconds(15);
    private static readonly TimeSpan PendingTimeout = TimeSpan.FromMinutes(10);
    private static readonly TimeSpan ProcessingTimeout = TimeSpan.FromMinutes(5);
    private static readonly int MaxBatchSize = 50;

    private readonly ApplicationDbContext _dbContext;
    private readonly IPaymentGatewayService _paymentGatewayService;
    private readonly IMobileMoneyProviderService _mobileMoneyProviderService;
    private readonly ILogger<PaymentGatewayPollingJob> _logger;

    public PaymentGatewayPollingJob(
        ApplicationDbContext dbContext,
        IPaymentGatewayService paymentGatewayService,
        IMobileMoneyProviderService mobileMoneyProviderService,
        ILogger<PaymentGatewayPollingJob> logger)
    {
        _dbContext = dbContext;
        _paymentGatewayService = paymentGatewayService;
        _mobileMoneyProviderService = mobileMoneyProviderService;
        _logger = logger;
    }

    public async Task Execute(IJobExecutionContext context)
    {
        var cancellationToken = context.CancellationToken;
        var now = DateTime.UtcNow;

        var transactions = await _dbContext.PaymentGatewayTransactions
            .Where(t => t.Status == GatewayTransactionStatus.Initiated
                     || t.Status == GatewayTransactionStatus.Pending
                     || t.Status == GatewayTransactionStatus.Processing)
            .OrderBy(t => t.InitiatedAt)
            .Take(MaxBatchSize)
            .ToListAsync(cancellationToken);

        if (transactions.Count == 0)
        {
            _logger.LogDebug("Payment gateway polling job found no transactions to process.");
            return;
        }

        _logger.LogInformation("Payment gateway polling job inspecting {Count} transactions", transactions.Count);

        var actionsTaken = 0;

        foreach (var transaction in transactions)
        {
            if (cancellationToken.IsCancellationRequested)
            {
                _logger.LogWarning("Payment gateway polling job cancelled before completing batch.");
                break;
            }

            try
            {
                if (transaction.Status == GatewayTransactionStatus.Initiated)
                {
                    await HandleInitiatedTransactionAsync(transaction, now, cancellationToken);
                    continue;
                }

                if (transaction.Status == GatewayTransactionStatus.Pending ||
                    transaction.Status == GatewayTransactionStatus.Processing)
                {
                    var actionTaken = await HandleActiveTransactionAsync(transaction, now, cancellationToken);
                    if (actionTaken)
                    {
                        actionsTaken++;
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex,
                    "Error while polling transaction {TransactionId} (Reference: {Reference})",
                    transaction.Id,
                    transaction.TransactionReference);
            }
        }

        if (actionsTaken > 0)
        {
            _logger.LogInformation("Payment gateway polling job applied {Actions} updates", actionsTaken);
        }
    }

    private async Task HandleInitiatedTransactionAsync(GatewayTransaction transaction, DateTime now, CancellationToken cancellationToken)
    {
        // Allow a short grace period before we attempt to transition initiated transactions.
        if (transaction.InitiatedAt > now - InitiatedDispatchDelay)
        {
            return;
        }

        // For initiated transactions we simply record that the poller saw them.
        // Actual processing should be triggered by the interactive flow (mobile money PIN submission).
        _logger.LogDebug(
            "Transaction {TransactionId} is still in Initiated state after grace period; waiting for client action.",
            transaction.Id);

        // If the transaction has already expired, mark it as such.
        if (transaction.ExpiresAt <= now)
        {
            var expired = await _paymentGatewayService.ExpireTransactionAsync(
                transaction.Id,
                nameof(PaymentGatewayPollingJob));

            if (expired)
            {
                _logger.LogInformation(
                    "Transaction {TransactionId} expired while waiting in Initiated state.",
                    transaction.Id);
            }
        }
    }

    private async Task<bool> HandleActiveTransactionAsync(GatewayTransaction transaction, DateTime now, CancellationToken cancellationToken)
    {
        // Expire transactions whose session has elapsed
        if (transaction.ExpiresAt <= now)
        {
            var expired = await _paymentGatewayService.ExpireTransactionAsync(
                transaction.Id,
                nameof(PaymentGatewayPollingJob));

            if (expired)
            {
                _logger.LogInformation(
                    "Transaction {TransactionId} expired after reaching gateway timeout.",
                    transaction.Id);
            }

            return expired;
        }

        PaymentTransactionDto? latest = null;

        // Poll provider specific APIs when possible (Orange/Africell mobile money supports active checks)
        if (!string.IsNullOrWhiteSpace(transaction.ExternalReference) &&
            (transaction.GatewayType == PaymentGatewayType.OrangeMoney ||
             transaction.GatewayType == PaymentGatewayType.AfricellMoney))
        {
            latest = await _mobileMoneyProviderService.CheckMobileMoneyPaymentStatusAsync(
                transaction.GatewayType,
                transaction.ExternalReference);
        }
        else
        {
            latest = await _paymentGatewayService.CheckPaymentStatusAsync(transaction.Id);
        }

        if (latest is null)
        {
            return false;
        }

        // Completed / failed statuses are handled by the provider; nothing to do beyond logging
        if (latest.Status == GatewayTransactionStatus.Completed ||
            latest.Status == GatewayTransactionStatus.Failed ||
            latest.Status == GatewayTransactionStatus.Cancelled)
        {
            _logger.LogDebug(
                "Transaction {TransactionId} already in terminal state {Status}.",
                transaction.Id,
                latest.Status);
            return false;
        }

        // Guard against long-running pending/processing states
        var elapsedSinceInitiation = now - transaction.InitiatedAt;
        var elapsedSinceProcessing = transaction.ProcessedAt.HasValue
            ? now - transaction.ProcessedAt.Value
            : TimeSpan.Zero;

        if (transaction.Status == GatewayTransactionStatus.Pending && elapsedSinceInitiation >= PendingTimeout)
        {
            await _paymentGatewayService.UpdateTransactionStatusAsync(
                transaction.Id,
                    GatewayTransactionStatus.Failed,
                "Timed out waiting for payment confirmation from gateway.",
                nameof(PaymentGatewayPollingJob));

            _logger.LogWarning(
                "Transaction {TransactionId} marked as failed after pending timeout ({Elapsed} minutes).",
                transaction.Id,
                elapsedSinceInitiation.TotalMinutes.ToString("F1"));
            return true;
        }

        if (transaction.Status == GatewayTransactionStatus.Processing &&
            transaction.ProcessedAt.HasValue &&
            elapsedSinceProcessing >= ProcessingTimeout)
        {
            await _paymentGatewayService.UpdateTransactionStatusAsync(
                transaction.Id,
                    GatewayTransactionStatus.Failed,
                "Gateway did not confirm processing within the expected window.",
                nameof(PaymentGatewayPollingJob));

            _logger.LogWarning(
                "Transaction {TransactionId} marked as failed after processing timeout ({Elapsed} minutes).",
                transaction.Id,
                elapsedSinceProcessing.TotalMinutes.ToString("F1"));
            return true;
        }

        return false;
    }
}