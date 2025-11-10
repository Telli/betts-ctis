using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using AutoMapper;
using BettsTax.Data;
using BettsTax.Data.Models;
using PaymentTransactionStatus = BettsTax.Data.Models.PaymentTransactionStatus;
using PaymentTransaction = BettsTax.Data.Models.PaymentTransaction;
using BettsTax.Core.DTOs.Payment;
using BettsTax.Core.Services.Interfaces;

namespace BettsTax.Core.Services;

/// <summary>
/// Main payment gateway service for Sierra Leone mobile money integration
/// Handles Orange Money, Africell Money, and other payment methods with comprehensive security
/// </summary>
public class PaymentGatewayService : IPaymentGatewayService
{
    private readonly ApplicationDbContext _context;
    private readonly IMapper _mapper;
    private readonly ILogger<PaymentGatewayService> _logger;
    private readonly IPaymentEncryptionService _encryptionService;
    private readonly IPaymentFraudDetectionService _fraudDetectionService;

    public PaymentGatewayService(
        ApplicationDbContext context,
        IMapper mapper,
        ILogger<PaymentGatewayService> logger,
        IPaymentEncryptionService encryptionService,
        IPaymentFraudDetectionService fraudDetectionService)
    {
        _context = context;
        _mapper = mapper;
        _logger = logger;
        _encryptionService = encryptionService;
        _fraudDetectionService = fraudDetectionService;
    }

    #region Transaction Management

    public async Task<PaymentTransactionDto> InitiatePaymentAsync(CreatePaymentTransactionDto request, string initiatedBy)
    {
        try
        {
            // Validate request
            await ValidatePaymentRequestAsync(request);

            // Get gateway configuration
            var gatewayConfig = await GetActiveGatewayConfigAsync(request.GatewayType);
            if (gatewayConfig == null)
                throw new InvalidOperationException($"No active configuration found for gateway type {request.GatewayType}");

            // Check transaction limits
            var limitsValid = await CheckTransactionLimitsAsync(request.GatewayType, request.Amount, request.PayerPhone);
            if (!limitsValid)
                throw new InvalidOperationException("Transaction exceeds limits");

            // Calculate fees
            var fee = await CalculateFeesAsync(request.GatewayType, request.Amount);

            // Perform fraud detection
            var riskLevel = await _fraudDetectionService.AnalyzeTransactionRiskAsync(
                request, request.IpAddress ?? "", request.UserAgent ?? "");

            // Generate transaction reference
            var transactionReference = await _encryptionService.GenerateTransactionReferenceAsync();

            // Create transaction
            var transaction = new PaymentTransaction
            {
                TransactionReference = transactionReference,
                ClientId = request.ClientId,
                GatewayType = request.GatewayType,
                GatewayConfigId = gatewayConfig.Id,
                Purpose = request.Purpose,
                Amount = request.Amount,
                Fee = fee,
                NetAmount = request.Amount - fee,
                Currency = request.Currency,
                PayerPhone = await FormatPhoneNumberAsync(request.GatewayType, request.PayerPhone),
                PayerName = request.PayerName,
                PayerEmail = request.PayerEmail,
                Status = PaymentTransactionStatus.Initiated,
                Description = request.Description,
                RiskLevel = riskLevel,
                RequiresManualReview = riskLevel >= SecurityRiskLevel.High,
                InitiatedAt = DateTime.UtcNow,
                ExpiresAt = DateTime.UtcNow.AddMinutes(gatewayConfig.TimeoutSeconds / 60)
            };

            _context.PaymentGatewayTransactions.Add(transaction);
            await _context.SaveChangesAsync();

            // Log transaction initiation
            await LogTransactionActionAsync(transaction.Id, "INITIATED", 
                PaymentTransactionStatus.Initiated, PaymentTransactionStatus.Initiated,
                $"Transaction initiated by {initiatedBy}");

            _logger.LogInformation(
                "Payment transaction initiated. Reference: {Reference}, Client: {ClientId}, Amount: {Amount}, Gateway: {Gateway}",
                transactionReference, request.ClientId, request.Amount, request.GatewayType);

            return await MapTransactionToDtoAsync(transaction);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to initiate payment for client {ClientId}", request.ClientId);
            throw new InvalidOperationException("Payment initiation failed", ex);
        }
    }

    public async Task<PaymentTransactionDto> ProcessPaymentAsync(ProcessPaymentDto request, string processedBy)
    {
        try
        {
            var transaction = await _context.PaymentGatewayTransactions
                .Include(t => t.GatewayConfig)
                .FirstOrDefaultAsync(t => t.Id == request.TransactionId);

            if (transaction == null)
                throw new InvalidOperationException($"Transaction with ID {request.TransactionId} not found");

            if (transaction.Status != PaymentTransactionStatus.Initiated)
                throw new InvalidOperationException($"Transaction is in {transaction.Status} status and cannot be processed");

            if (transaction.ExpiresAt < DateTime.UtcNow)
                throw new InvalidOperationException("Transaction has expired");

            // Check if manual review is required
            if (transaction.RequiresManualReview && transaction.ReviewedBy == null)
                throw new InvalidOperationException("Transaction requires manual review before processing");

            var previousStatus = transaction.Status;
            transaction.Status = PaymentTransactionStatus.Processing;
            transaction.ProcessedAt = DateTime.UtcNow;

            // For mobile money, validate PIN if provided
            if (IsPhoneTransaction(transaction.GatewayType) && !string.IsNullOrEmpty(request.Pin))
            {
                // In production, validate PIN with mobile money provider
                _logger.LogDebug("PIN validation for transaction {TransactionId}", transaction.Id);
            }

            await _context.SaveChangesAsync();

            // Log status change
            await LogTransactionActionAsync(transaction.Id, "PROCESSING", 
                previousStatus, PaymentTransactionStatus.Processing,
                $"Transaction processing started by {processedBy}");

            _logger.LogInformation(
                "Payment transaction processing started. Reference: {Reference}, ProcessedBy: {ProcessedBy}",
                transaction.TransactionReference, processedBy);

            return await MapTransactionToDtoAsync(transaction);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to process payment transaction {TransactionId}", request.TransactionId);
            throw new InvalidOperationException("Payment processing failed", ex);
        }
    }

    public async Task<PaymentTransactionDto> GetTransactionAsync(int transactionId)
    {
        try
        {
            var transaction = await _context.PaymentGatewayTransactions
                .Include(t => t.Client)
                .Include(t => t.GatewayConfig)
                .Include(t => t.TransactionLogs)
                .Include(t => t.Refunds)
                .FirstOrDefaultAsync(t => t.Id == transactionId);

            if (transaction == null)
                throw new InvalidOperationException($"Transaction with ID {transactionId} not found");

            return await MapTransactionToDtoAsync(transaction);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get payment transaction {TransactionId}", transactionId);
            throw new InvalidOperationException("Failed to retrieve payment transaction", ex);
        }
    }

    public async Task<PaymentTransactionDto?> GetTransactionByReferenceAsync(string transactionReference)
    {
        try
        {
            var transaction = await _context.PaymentGatewayTransactions
                .Include(t => t.Client)
                .Include(t => t.GatewayConfig)
                .FirstOrDefaultAsync(t => t.TransactionReference == transactionReference);

            return transaction == null ? null : await MapTransactionToDtoAsync(transaction);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get payment transaction by reference {Reference}", transactionReference);
            return null;
        }
    }

    public async Task<List<PaymentTransactionDto>> GetTransactionsAsync(PaymentTransactionSearchDto search)
    {
        try
        {
            var query = _context.PaymentGatewayTransactions
                .Include(t => t.Client)
                .AsQueryable();

            // Apply filters
            if (!string.IsNullOrEmpty(search.TransactionReference))
                query = query.Where(t => t.TransactionReference.Contains(search.TransactionReference));

            if (!string.IsNullOrEmpty(search.ExternalReference))
                query = query.Where(t => t.ExternalReference.Contains(search.ExternalReference));

            if (search.ClientId.HasValue)
                query = query.Where(t => t.ClientId == search.ClientId.Value);

            if (search.GatewayType.HasValue)
                query = query.Where(t => t.GatewayType == search.GatewayType.Value);

            if (search.Status.HasValue)
                query = query.Where(t => t.Status == search.Status.Value);

            if (search.MinAmount.HasValue)
                query = query.Where(t => t.Amount >= search.MinAmount.Value);

            if (search.MaxAmount.HasValue)
                query = query.Where(t => t.Amount <= search.MaxAmount.Value);

            if (!string.IsNullOrEmpty(search.PayerPhone))
                query = query.Where(t => t.PayerPhone.Contains(search.PayerPhone));

            if (search.RiskLevel.HasValue)
                query = query.Where(t => t.RiskLevel == search.RiskLevel.Value);

            if (search.RequiresManualReview.HasValue)
                query = query.Where(t => t.RequiresManualReview == search.RequiresManualReview.Value);

            if (search.IsReconciled.HasValue)
                query = query.Where(t => t.IsReconciled == search.IsReconciled.Value);

            if (search.FromDate.HasValue)
                query = query.Where(t => t.InitiatedAt >= search.FromDate.Value);

            if (search.ToDate.HasValue)
                query = query.Where(t => t.InitiatedAt <= search.ToDate.Value);

            // Apply sorting
            query = search.SortBy.ToLower() switch
            {
                "amount" => search.SortDirection.ToLower() == "asc" 
                    ? query.OrderBy(t => t.Amount) 
                    : query.OrderByDescending(t => t.Amount),
                "status" => search.SortDirection.ToLower() == "asc" 
                    ? query.OrderBy(t => t.Status) 
                    : query.OrderByDescending(t => t.Status),
                "gateway" => search.SortDirection.ToLower() == "asc" 
                    ? query.OrderBy(t => t.GatewayType) 
                    : query.OrderByDescending(t => t.GatewayType),
                _ => search.SortDirection.ToLower() == "asc" 
                    ? query.OrderBy(t => t.InitiatedAt) 
                    : query.OrderByDescending(t => t.InitiatedAt)
            };

            // Apply pagination
            var transactions = await query
                .Skip((search.Page - 1) * search.PageSize)
                .Take(search.PageSize)
                .ToListAsync();

            var results = new List<PaymentTransactionDto>();
            foreach (var transaction in transactions)
            {
                results.Add(await MapTransactionToDtoAsync(transaction));
            }

            _logger.LogDebug("Retrieved {Count} transactions for search criteria", results.Count);
            return results;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to search payment transactions");
            throw new InvalidOperationException("Transaction search failed", ex);
        }
    }

    public async Task<PaymentTransactionDto> UpdateTransactionStatusAsync(
        int transactionId, PaymentTransactionStatus status, string statusMessage, string updatedBy)
    {
        try
        {
            var transaction = await _context.PaymentGatewayTransactions
                .FirstOrDefaultAsync(t => t.Id == transactionId);

            if (transaction == null)
                throw new InvalidOperationException($"Transaction with ID {transactionId} not found");

            var previousStatus = transaction.Status;
            transaction.Status = status;
            transaction.StatusMessage = statusMessage;

            // Update timestamps based on status
            switch (status)
            {
                case PaymentTransactionStatus.Completed:
                    transaction.CompletedAt = DateTime.UtcNow;
                    break;
                case PaymentTransactionStatus.Failed:
                    transaction.FailedAt = DateTime.UtcNow;
                    break;
            }

            await _context.SaveChangesAsync();

            // Log status change
            await LogTransactionActionAsync(transactionId, "STATUS_UPDATE", 
                previousStatus, status, $"Status updated by {updatedBy}: {statusMessage}");

            _logger.LogInformation(
                "Transaction {TransactionId} status updated from {PreviousStatus} to {NewStatus} by {UpdatedBy}",
                transactionId, previousStatus, status, updatedBy);

            return await MapTransactionToDtoAsync(transaction);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to update transaction {TransactionId} status", transactionId);
            throw new InvalidOperationException("Status update failed", ex);
        }
    }

    public async Task<bool> CancelTransactionAsync(int transactionId, string reason, string cancelledBy)
    {
        try
        {
            var transaction = await _context.PaymentGatewayTransactions
                .FirstOrDefaultAsync(t => t.Id == transactionId);

            if (transaction == null)
                return false;

            if (transaction.Status == PaymentTransactionStatus.Completed)
                throw new InvalidOperationException("Cannot cancel completed transaction");

            var previousStatus = transaction.Status;
            transaction.Status = PaymentTransactionStatus.Cancelled;
            transaction.StatusMessage = $"Cancelled: {reason}";
            transaction.FailedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();

            // Log cancellation
            await LogTransactionActionAsync(transactionId, "CANCELLED", 
                previousStatus, PaymentTransactionStatus.Cancelled,
                $"Transaction cancelled by {cancelledBy}. Reason: {reason}");

            _logger.LogInformation(
                "Transaction {TransactionId} cancelled by {CancelledBy}. Reason: {Reason}",
                transactionId, cancelledBy, reason);

            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to cancel transaction {TransactionId}", transactionId);
            return false;
        }
    }

    public async Task<bool> ExpireTransactionAsync(int transactionId, string expiredBy)
    {
        try
        {
            var transaction = await _context.PaymentGatewayTransactions
                .FirstOrDefaultAsync(t => t.Id == transactionId);

            if (transaction == null)
                return false;

            if (transaction.Status == PaymentTransactionStatus.Completed ||
                transaction.Status == PaymentTransactionStatus.Cancelled)
                return false;

            var previousStatus = transaction.Status;
            transaction.Status = PaymentTransactionStatus.Expired;
            transaction.StatusMessage = "Transaction expired due to timeout";
            transaction.FailedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();

            // Log expiration
            await LogTransactionActionAsync(transactionId, "EXPIRED", 
                previousStatus, PaymentTransactionStatus.Expired,
                $"Transaction expired by system timeout. Expired by: {expiredBy}");

            _logger.LogInformation("Transaction {TransactionId} expired", transactionId);
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to expire transaction {TransactionId}", transactionId);
            return false;
        }
    }

    #endregion

    #region Payment Processing

    public async Task<PaymentTransactionDto> RetryPaymentAsync(int transactionId, string retriedBy)
    {
        try
        {
            var transaction = await _context.PaymentGatewayTransactions
                .Include(t => t.GatewayConfig)
                .FirstOrDefaultAsync(t => t.Id == transactionId);

            if (transaction == null)
                throw new InvalidOperationException($"Transaction with ID {transactionId} not found");

            if (transaction.Status != PaymentTransactionStatus.Failed)
                throw new InvalidOperationException("Only failed transactions can be retried");

            if (transaction.RetryCount >= transaction.GatewayConfig.RetryAttempts)
                throw new InvalidOperationException("Maximum retry attempts exceeded");

            var previousStatus = transaction.Status;
            transaction.Status = PaymentTransactionStatus.Initiated;
            transaction.StatusMessage = $"Retry attempt {transaction.RetryCount + 1}";
            transaction.RetryCount++;
            transaction.LastRetryAt = DateTime.UtcNow;
            transaction.NextRetryAt = DateTime.UtcNow.AddSeconds(transaction.GatewayConfig.RetryDelaySeconds);

            await _context.SaveChangesAsync();

            // Log retry
            await LogTransactionActionAsync(transactionId, "RETRY", 
                previousStatus, PaymentTransactionStatus.Initiated,
                $"Payment retry attempt {transaction.RetryCount} by {retriedBy}");

            _logger.LogInformation(
                "Payment transaction {TransactionId} retry attempt {RetryCount} by {RetriedBy}",
                transactionId, transaction.RetryCount, retriedBy);

            return await MapTransactionToDtoAsync(transaction);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to retry payment transaction {TransactionId}", transactionId);
            throw new InvalidOperationException("Payment retry failed", ex);
        }
    }

    public async Task<PaymentTransactionDto> ConfirmPaymentAsync(int transactionId, string externalReference, string confirmedBy)
    {
        try
        {
            var transaction = await _context.PaymentGatewayTransactions
                .FirstOrDefaultAsync(t => t.Id == transactionId);

            if (transaction == null)
                throw new InvalidOperationException($"Transaction with ID {transactionId} not found");

            var previousStatus = transaction.Status;
            transaction.Status = PaymentTransactionStatus.Completed;
            transaction.ExternalReference = externalReference;
            transaction.StatusMessage = "Payment confirmed successfully";
            transaction.CompletedAt = DateTime.UtcNow;
            transaction.IsReconciled = false; // Will be reconciled later

            await _context.SaveChangesAsync();

            // Log confirmation
            await LogTransactionActionAsync(transactionId, "CONFIRMED", 
                previousStatus, PaymentTransactionStatus.Completed,
                $"Payment confirmed by {confirmedBy}. External reference: {externalReference}");

            _logger.LogInformation(
                "Payment transaction {TransactionId} confirmed by {ConfirmedBy}. External reference: {ExternalReference}",
                transactionId, confirmedBy, externalReference);

            return await MapTransactionToDtoAsync(transaction);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to confirm payment transaction {TransactionId}", transactionId);
            throw new InvalidOperationException("Payment confirmation failed", ex);
        }
    }

    public async Task<bool> ValidatePaymentAsync(int transactionId)
    {
        try
        {
            var transaction = await _context.PaymentGatewayTransactions
                .Include(t => t.GatewayConfig)
                .FirstOrDefaultAsync(t => t.Id == transactionId);

            if (transaction == null)
                return false;

            // Validate transaction is in correct state
            if (transaction.Status != PaymentTransactionStatus.Processing)
                return false;

            // Validate amount is within limits
            if (transaction.Amount < transaction.GatewayConfig.MinAmount ||
                transaction.Amount > transaction.GatewayConfig.MaxAmount)
                return false;

            // Validate transaction hasn't expired
            if (transaction.ExpiresAt < DateTime.UtcNow)
                return false;

            // Validate phone number format
            var phoneValid = await ValidatePhoneNumberAsync(transaction.GatewayType, transaction.PayerPhone);
            if (!phoneValid)
                return false;

            _logger.LogDebug("Payment validation passed for transaction {TransactionId}", transactionId);
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to validate payment transaction {TransactionId}", transactionId);
            return false;
        }
    }

    public async Task<decimal> CalculateFeesAsync(PaymentGatewayType gatewayType, decimal amount)
    {
        try
        {
            var config = await GetActiveGatewayConfigAsync(gatewayType);
            if (config == null)
                return 0;

            var percentageFee = amount * config.FeePercentage;
            var totalFee = percentageFee + config.FixedFee;

            // Apply min/max fee constraints
            if (totalFee < config.MinFee)
                totalFee = config.MinFee;
            else if (totalFee > config.MaxFee)
                totalFee = config.MaxFee;

            _logger.LogDebug("Calculated fee {Fee} for amount {Amount} on gateway {Gateway}", 
                totalFee, amount, gatewayType);

            return totalFee;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to calculate fees for gateway {Gateway}", gatewayType);
            return 0;
        }
    }

    public async Task<bool> CheckTransactionLimitsAsync(PaymentGatewayType gatewayType, decimal amount, string payerPhone)
    {
        try
        {
            var config = await GetActiveGatewayConfigAsync(gatewayType);
            if (config == null)
                return false;

            // Check amount limits
            if (amount < config.MinAmount || amount > config.MaxAmount)
                return false;

            // Check daily limits for this phone number
            var today = DateTime.UtcNow.Date;
            var dailyTotal = await _context.PaymentGatewayTransactions
                .Where(t => t.PayerPhone == payerPhone &&
                           t.GatewayType == gatewayType &&
                           t.InitiatedAt >= today &&
                           t.Status == PaymentTransactionStatus.Completed)
                .SumAsync(t => t.Amount);

            if (dailyTotal + amount > config.DailyLimit)
                return false;

            // Check monthly limits
            var monthStart = new DateTime(DateTime.UtcNow.Year, DateTime.UtcNow.Month, 1);
            var monthlyTotal = await _context.PaymentGatewayTransactions
                .Where(t => t.PayerPhone == payerPhone &&
                           t.GatewayType == gatewayType &&
                           t.InitiatedAt >= monthStart &&
                           t.Status == PaymentTransactionStatus.Completed)
                .SumAsync(t => t.Amount);

            if (monthlyTotal + amount > config.MonthlyLimit)
                return false;

            _logger.LogDebug("Transaction limits check passed for {Phone} on gateway {Gateway}", 
                payerPhone, gatewayType);

            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to check transaction limits for gateway {Gateway}", gatewayType);
            return false;
        }
    }

    #endregion

    #region Mobile Money Specific

    public async Task<bool> ValidatePhoneNumberAsync(PaymentGatewayType gatewayType, string phoneNumber)
    {
        try
        {
            if (string.IsNullOrEmpty(phoneNumber))
                return false;

            var provider = await GetMobileMoneyProviderAsync(gatewayType);
            if (provider == null)
                return false;

            // Clean phone number
            var cleanPhone = phoneNumber.Replace("+", "").Replace("-", "").Replace(" ", "");

            // Check length
            if (cleanPhone.Length < provider.MinPhoneLength || cleanPhone.Length > provider.MaxPhoneLength)
                return false;

            // Check prefix if specified
            if (!string.IsNullOrEmpty(provider.PhonePrefix) && !cleanPhone.StartsWith(provider.PhonePrefix))
                return false;

            // Use regex if provided
            if (!string.IsNullOrEmpty(provider.PhoneValidationRegex))
            {
                var regex = new System.Text.RegularExpressions.Regex(provider.PhoneValidationRegex);
                if (!regex.IsMatch(cleanPhone))
                    return false;
            }

            _logger.LogDebug("Phone number validation passed for {Phone} on gateway {Gateway}", 
                phoneNumber, gatewayType);

            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to validate phone number {Phone} for gateway {Gateway}", 
                phoneNumber, gatewayType);
            return false;
        }
    }

    public async Task<string> FormatPhoneNumberAsync(PaymentGatewayType gatewayType, string phoneNumber)
    {
        try
        {
            if (string.IsNullOrEmpty(phoneNumber))
                return phoneNumber;

            var provider = await GetMobileMoneyProviderAsync(gatewayType);
            if (provider == null)
                return phoneNumber;

            // Clean phone number
            var cleanPhone = phoneNumber.Replace("+", "").Replace("-", "").Replace(" ", "");

            // Add country code if missing
            if (!string.IsNullOrEmpty(provider.PhonePrefix) && !cleanPhone.StartsWith(provider.PhonePrefix))
            {
                cleanPhone = provider.PhonePrefix + cleanPhone;
            }

            _logger.LogDebug("Formatted phone number {Original} to {Formatted} for gateway {Gateway}", 
                phoneNumber, cleanPhone, gatewayType);

            return cleanPhone;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to format phone number {Phone} for gateway {Gateway}", 
                phoneNumber, gatewayType);
            return phoneNumber;
        }
    }

    public async Task<bool> CheckAccountBalanceAsync(PaymentGatewayType gatewayType, string phoneNumber)
    {
        try
        {
            // This would integrate with actual mobile money provider APIs
            // For now, return true as a placeholder
            _logger.LogDebug("Account balance check for {Phone} on gateway {Gateway}", 
                phoneNumber, gatewayType);

            return await Task.FromResult(true);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to check account balance for {Phone} on gateway {Gateway}", 
                phoneNumber, gatewayType);
            return false;
        }
    }

    public async Task<string> SendPaymentRequestAsync(int transactionId)
    {
        try
        {
            var transaction = await _context.PaymentGatewayTransactions
                .Include(t => t.GatewayConfig)
                .FirstOrDefaultAsync(t => t.Id == transactionId);

            if (transaction == null)
                throw new InvalidOperationException($"Transaction with ID {transactionId} not found");

            // This would integrate with actual mobile money provider APIs
            // For now, generate a mock external reference
            var externalReference = $"{transaction.GatewayType}_{DateTime.UtcNow:yyyyMMddHHmmss}_{transactionId}";

            transaction.ExternalReference = externalReference;
            transaction.Status = PaymentTransactionStatus.Pending;
            transaction.StatusMessage = "Payment request sent to mobile money provider";

            await _context.SaveChangesAsync();

            // Log payment request
            await LogTransactionActionAsync(transactionId, "PAYMENT_REQUEST_SENT", 
                PaymentTransactionStatus.Processing, PaymentTransactionStatus.Pending,
                $"Payment request sent to {transaction.GatewayType}. External reference: {externalReference}");

            _logger.LogInformation(
                "Payment request sent for transaction {TransactionId}. External reference: {ExternalReference}",
                transactionId, externalReference);

            return externalReference;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to send payment request for transaction {TransactionId}", transactionId);
            throw new InvalidOperationException("Failed to send payment request", ex);
        }
    }

    public async Task<PaymentTransactionDto> CheckPaymentStatusAsync(int transactionId)
    {
        try
        {
            var transaction = await _context.PaymentGatewayTransactions
                .FirstOrDefaultAsync(t => t.Id == transactionId);

            if (transaction == null)
                throw new InvalidOperationException($"Transaction with ID {transactionId} not found");

            // This would integrate with actual mobile money provider APIs to check status
            // For now, return the current transaction state
            _logger.LogDebug("Checking payment status for transaction {TransactionId}", transactionId);

            return await MapTransactionToDtoAsync(transaction);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to check payment status for transaction {TransactionId}", transactionId);
            throw new InvalidOperationException("Failed to check payment status", ex);
        }
    }

    #endregion

    #region Reconciliation

    public async Task<bool> ReconcileTransactionAsync(int transactionId, string reconciledBy)
    {
        try
        {
            var transaction = await _context.PaymentGatewayTransactions
                .FirstOrDefaultAsync(t => t.Id == transactionId);

            if (transaction == null)
                return false;

            if (transaction.Status != PaymentTransactionStatus.Completed)
                return false;

            transaction.IsReconciled = true;
            transaction.ReconciledAt = DateTime.UtcNow;
            transaction.ReconciledBy = reconciledBy;

            await _context.SaveChangesAsync();

            // Log reconciliation
            await LogTransactionActionAsync(transactionId, "RECONCILED", 
                transaction.Status, transaction.Status,
                $"Transaction reconciled by {reconciledBy}");

            _logger.LogInformation("Transaction {TransactionId} reconciled by {ReconciledBy}", 
                transactionId, reconciledBy);

            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to reconcile transaction {TransactionId}", transactionId);
            return false;
        }
    }

    public async Task<List<PaymentTransactionDto>> GetUnreconciledTransactionsAsync(PaymentGatewayType? gatewayType = null)
    {
        try
        {
            var query = _context.PaymentGatewayTransactions
                .Where(t => !t.IsReconciled && t.Status == PaymentTransactionStatus.Completed);

            if (gatewayType.HasValue)
                query = query.Where(t => t.GatewayType == gatewayType.Value);

            var transactions = await query
                .OrderBy(t => t.CompletedAt)
                .ToListAsync();

            var results = new List<PaymentTransactionDto>();
            foreach (var transaction in transactions)
            {
                results.Add(await MapTransactionToDtoAsync(transaction));
            }

            _logger.LogDebug("Retrieved {Count} unreconciled transactions", results.Count);
            return results;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get unreconciled transactions");
            throw new InvalidOperationException("Failed to retrieve unreconciled transactions", ex);
        }
    }

    public async Task<bool> BulkReconcileAsync(List<int> transactionIds, string reconciledBy)
    {
        try
        {
            var transactions = await _context.PaymentGatewayTransactions
                .Where(t => transactionIds.Contains(t.Id) && 
                           t.Status == PaymentTransactionStatus.Completed && 
                           !t.IsReconciled)
                .ToListAsync();

            foreach (var transaction in transactions)
            {
                transaction.IsReconciled = true;
                transaction.ReconciledAt = DateTime.UtcNow;
                transaction.ReconciledBy = reconciledBy;

                // Log reconciliation
                await LogTransactionActionAsync(transaction.Id, "BULK_RECONCILED", 
                    transaction.Status, transaction.Status,
                    $"Transaction bulk reconciled by {reconciledBy}");
            }

            await _context.SaveChangesAsync();

            _logger.LogInformation("Bulk reconciled {Count} transactions by {ReconciledBy}", 
                transactions.Count, reconciledBy);

            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to bulk reconcile transactions");
            return false;
        }
    }

    public async Task<PaymentAnalyticsDto> GetReconciliationReportAsync(DateTime fromDate, DateTime toDate)
    {
        try
        {
            var transactions = await _context.PaymentGatewayTransactions
                .Where(t => t.InitiatedAt >= fromDate && t.InitiatedAt <= toDate)
                .ToListAsync();

            var analytics = new PaymentAnalyticsDto
            {
                FromDate = fromDate,
                ToDate = toDate,
                TotalTransactions = transactions.Count,
                TotalAmount = transactions.Sum(t => t.Amount),
                TotalFees = transactions.Sum(t => t.Fee),
                TotalNetAmount = transactions.Sum(t => t.NetAmount),
                SuccessRate = transactions.Count > 0 
                    ? (decimal)transactions.Count(t => t.Status == PaymentTransactionStatus.Completed) / transactions.Count * 100 
                    : 0,
                AverageTransactionAmount = transactions.Count > 0 
                    ? transactions.Average(t => t.Amount) 
                    : 0,
                StatusDistribution = transactions
                    .GroupBy(t => t.Status)
                    .ToDictionary(g => g.Key, g => g.Count()),
                GatewayStats = transactions
                    .GroupBy(t => t.GatewayType)
                    .ToDictionary(g => g.Key, g => new PaymentGatewayStatsDto
                    {
                        GatewayType = g.Key,
                        GatewayName = g.Key.ToString(),
                        TransactionCount = g.Count(),
                        TotalAmount = g.Sum(t => t.Amount),
                        TotalFees = g.Sum(t => t.Fee),
                        SuccessfulTransactions = g.Count(t => t.Status == PaymentTransactionStatus.Completed),
                        FailedTransactions = g.Count(t => t.Status == PaymentTransactionStatus.Failed),
                        PendingTransactions = g.Count(t => t.Status == PaymentTransactionStatus.Pending),
                        SuccessRate = g.Count() > 0 
                            ? (decimal)g.Count(t => t.Status == PaymentTransactionStatus.Completed) / g.Count() * 100 
                            : 0
                    })
            };

            _logger.LogDebug("Generated reconciliation report for period {FromDate} to {ToDate}", 
                fromDate, toDate);

            return analytics;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to generate reconciliation report");
            throw new InvalidOperationException("Failed to generate reconciliation report", ex);
        }
    }

    #endregion

    #region Analytics and Reporting

    public async Task<PaymentAnalyticsDto> GetPaymentAnalyticsAsync(DateTime fromDate, DateTime toDate, PaymentGatewayType? gatewayType = null)
    {
        try
        {
            var query = _context.PaymentGatewayTransactions
                .Where(t => t.InitiatedAt >= fromDate && t.InitiatedAt <= toDate);

            if (gatewayType.HasValue)
                query = query.Where(t => t.GatewayType == gatewayType.Value);

            var transactions = await query.ToListAsync();

            var analytics = new PaymentAnalyticsDto
            {
                FromDate = fromDate,
                ToDate = toDate,
                TotalTransactions = transactions.Count,
                TotalAmount = transactions.Sum(t => t.Amount),
                TotalFees = transactions.Sum(t => t.Fee),
                TotalNetAmount = transactions.Sum(t => t.NetAmount),
                SuccessRate = transactions.Count > 0 
                    ? (decimal)transactions.Count(t => t.Status == PaymentTransactionStatus.Completed) / transactions.Count * 100 
                    : 0,
                AverageTransactionAmount = transactions.Count > 0 
                    ? transactions.Average(t => t.Amount) 
                    : 0,
                StatusDistribution = transactions
                    .GroupBy(t => t.Status)
                    .ToDictionary(g => g.Key, g => g.Count()),
                PurposeAmounts = transactions
                    .GroupBy(t => t.Purpose)
                    .ToDictionary(g => g.Key, g => g.Sum(t => t.Amount)),
                DailyAmounts = transactions
                    .GroupBy(t => t.InitiatedAt.Date)
                    .ToDictionary(g => g.Key.ToString("yyyy-MM-dd"), g => g.Sum(t => t.Amount)),
                DailyTransactionCounts = transactions
                    .GroupBy(t => t.InitiatedAt.Date)
                    .ToDictionary(g => g.Key.ToString("yyyy-MM-dd"), g => g.Count()),
                SecurityStats = new SecurityStatsDto
                {
                    HighRiskTransactions = transactions.Count(t => t.RiskLevel >= SecurityRiskLevel.High),
                    ManualReviewRequired = transactions.Count(t => t.RequiresManualReview),
                    RiskLevelDistribution = transactions
                        .GroupBy(t => t.RiskLevel)
                        .ToDictionary(g => g.Key, g => g.Count())
                }
            };

            _logger.LogDebug("Generated payment analytics for period {FromDate} to {ToDate}", 
                fromDate, toDate);

            return analytics;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to generate payment analytics");
            throw new InvalidOperationException("Failed to generate payment analytics", ex);
        }
    }

    public async Task<List<PaymentGatewayStatsDto>> GetGatewayPerformanceAsync(DateTime fromDate, DateTime toDate)
    {
        try
        {
            var transactions = await _context.PaymentGatewayTransactions
                .Where(t => t.InitiatedAt >= fromDate && t.InitiatedAt <= toDate)
                .ToListAsync();

            var stats = transactions
                .GroupBy(t => t.GatewayType)
                .Select(g => new PaymentGatewayStatsDto
                {
                    GatewayType = g.Key,
                    GatewayName = g.Key.ToString(),
                    TransactionCount = g.Count(),
                    TotalAmount = g.Sum(t => t.Amount),
                    TotalFees = g.Sum(t => t.Fee),
                    SuccessfulTransactions = g.Count(t => t.Status == PaymentTransactionStatus.Completed),
                    FailedTransactions = g.Count(t => t.Status == PaymentTransactionStatus.Failed),
                    PendingTransactions = g.Count(t => t.Status == PaymentTransactionStatus.Pending),
                    SuccessRate = g.Count() > 0 
                        ? (decimal)g.Count(t => t.Status == PaymentTransactionStatus.Completed) / g.Count() * 100 
                        : 0,
                    AverageProcessingTime = (decimal)g.Where(t => t.ProcessedAt.HasValue)
                        .Select(t => (t.ProcessedAt!.Value - t.InitiatedAt).TotalSeconds)
                        .DefaultIfEmpty(0)
                        .Average()
                })
                .OrderByDescending(s => s.TransactionCount)
                .ToList();

            _logger.LogDebug("Generated gateway performance stats for {Count} gateways", stats.Count);
            return stats;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get gateway performance stats");
            throw new InvalidOperationException("Failed to retrieve gateway performance statistics", ex);
        }
    }

    public async Task<List<PaymentErrorStatsDto>> GetErrorAnalyticsAsync(DateTime fromDate, DateTime toDate)
    {
        try
        {
            var failedTransactions = await _context.PaymentGatewayTransactions
                .Where(t => t.InitiatedAt >= fromDate && t.InitiatedAt <= toDate &&
                           (t.Status == PaymentTransactionStatus.Failed || 
                            t.Status == PaymentTransactionStatus.Cancelled))
                .ToListAsync();

            var totalFailed = failedTransactions.Count;
            
            var errorStats = failedTransactions
                .Where(t => !string.IsNullOrEmpty(t.ErrorCode))
                .GroupBy(t => new { t.ErrorCode, t.GatewayType })
                .Select(g => new PaymentErrorStatsDto
                {
                    ErrorCode = g.Key.ErrorCode,
                    ErrorMessage = g.FirstOrDefault()?.StatusMessage ?? "Unknown error",
                    Count = g.Count(),
                    Percentage = totalFailed > 0 ? (decimal)g.Count() / totalFailed * 100 : 0,
                    GatewayType = g.Key.GatewayType,
                    GatewayName = g.Key.GatewayType.ToString()
                })
                .OrderByDescending(s => s.Count)
                .ToList();

            _logger.LogDebug("Generated error analytics for {Count} error types", errorStats.Count);
            return errorStats;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get error analytics");
            throw new InvalidOperationException("Failed to retrieve error analytics", ex);
        }
    }

    public async Task<SecurityStatsDto> GetSecurityAnalyticsAsync(DateTime fromDate, DateTime toDate)
    {
        try
        {
            return await _fraudDetectionService.GetSecurityDashboardAsync(fromDate, toDate);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get security analytics");
            throw new InvalidOperationException("Failed to retrieve security analytics", ex);
        }
    }

    public async Task<byte[]> ExportTransactionsAsync(PaymentTransactionSearchDto search, string format = "excel")
    {
        try
        {
            var transactions = await GetTransactionsAsync(search);
            
            // This is a simplified implementation
            // In production, use proper export libraries like EPPlus for Excel
            var jsonData = JsonSerializer.Serialize(transactions, new JsonSerializerOptions 
            { 
                WriteIndented = true 
            });

            _logger.LogInformation("Exported {Count} transactions in {Format} format", 
                transactions.Count, format);

            return System.Text.Encoding.UTF8.GetBytes(jsonData);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to export transactions");
            throw new InvalidOperationException("Transaction export failed", ex);
        }
    }

    #endregion

    #region Refund Management

    public Task<PaymentRefundDto> InitiateRefundAsync(CreatePaymentRefundDto request, string initiatedBy)
    {
        // TODO: Implement refund initiation logic
        throw new NotImplementedException("Refund initiation is not yet implemented");
    }

    public Task<PaymentRefundDto> ProcessRefundAsync(int refundId, string processedBy)
    {
        // TODO: Implement refund processing logic
        throw new NotImplementedException("Refund processing is not yet implemented");
    }

    public Task<PaymentRefundDto> GetRefundAsync(int refundId)
    {
        // TODO: Implement refund retrieval logic
        throw new NotImplementedException("Refund retrieval is not yet implemented");
    }

    public Task<List<PaymentRefundDto>> GetRefundsAsync(int? transactionId = null, int page = 1, int pageSize = 20)
    {
        // TODO: Implement refunds list retrieval logic
        throw new NotImplementedException("Refunds list retrieval is not yet implemented");
    }

    public Task<bool> ApproveRefundAsync(int refundId, string approvedBy)
    {
        // TODO: Implement refund approval logic
        throw new NotImplementedException("Refund approval is not yet implemented");
    }

    public Task<bool> RejectRefundAsync(int refundId, string reason, string rejectedBy)
    {
        // TODO: Implement refund rejection logic
        throw new NotImplementedException("Refund rejection is not yet implemented");
    }

    #endregion

    #region Private Helper Methods

    private async Task<PaymentGatewayConfig?> GetActiveGatewayConfigAsync(PaymentGatewayType gatewayType)
    {
        return await _context.PaymentGatewayConfigs
            .FirstOrDefaultAsync(c => c.GatewayType == gatewayType && c.IsActive);
    }

    private async Task<MobileMoneyProvider?> GetMobileMoneyProviderAsync(PaymentGatewayType gatewayType)
    {
        var providerCode = gatewayType switch
        {
            PaymentGatewayType.OrangeMoney => "OM",
            PaymentGatewayType.AfricellMoney => "AM",
            _ => null
        };

        if (providerCode == null)
            return null;

        return await _context.MobileMoneyProviders
            .FirstOrDefaultAsync(p => p.Code == providerCode && p.IsActive);
    }

    private async Task ValidatePaymentRequestAsync(CreatePaymentTransactionDto request)
    {
        if (request.Amount <= 0)
            throw new ArgumentException("Amount must be greater than zero");

        if (string.IsNullOrEmpty(request.PayerPhone))
            throw new ArgumentException("Payer phone number is required");

        // Validate client exists
        var clientExists = await _context.Clients.AnyAsync(c => c.ClientId == request.ClientId);
        if (!clientExists)
            throw new ArgumentException($"Client with ID {request.ClientId} not found");
    }

    private bool IsPhoneTransaction(PaymentGatewayType gatewayType)
    {
        return gatewayType == PaymentGatewayType.OrangeMoney || 
               gatewayType == PaymentGatewayType.AfricellMoney;
    }

    private async Task LogTransactionActionAsync(int transactionId, string action, 
        PaymentTransactionStatus previousStatus, PaymentTransactionStatus newStatus, string details)
    {
        try
        {
            var log = new PaymentTransactionLog
            {
                TransactionId = transactionId,
                Action = action,
                PreviousStatus = previousStatus,
                NewStatus = newStatus,
                Details = details,
                CreatedAt = DateTime.UtcNow
            };

            _context.PaymentTransactionLogs.Add(log);
            await _context.SaveChangesAsync();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to log transaction action {Action} for transaction {TransactionId}", 
                action, transactionId);
        }
    }

    private async Task<PaymentTransactionDto> MapTransactionToDtoAsync(PaymentTransaction transaction)
    {
        var riskFactors = string.IsNullOrEmpty(transaction.RiskFactors) 
            ? new List<string>() 
            : JsonSerializer.Deserialize<List<string>>(transaction.RiskFactors) ?? new List<string>();

        return new PaymentTransactionDto
        {
            Id = transaction.Id,
            TransactionReference = transaction.TransactionReference,
            ExternalReference = transaction.ExternalReference,
            ProviderTransactionId = transaction.ProviderTransactionId,
            ClientId = transaction.ClientId,
            ClientName = transaction.Client?.BusinessName ?? "",
            ClientNumber = transaction.Client?.ClientNumber ?? "",
            GatewayType = transaction.GatewayType,
            GatewayName = transaction.GatewayType.ToString(),
            Purpose = transaction.Purpose,
            PurposeName = transaction.Purpose.ToString(),
            Amount = transaction.Amount,
            Fee = transaction.Fee,
            NetAmount = transaction.NetAmount,
            Currency = transaction.Currency,
            PayerPhone = transaction.PayerPhone,
            PayerName = transaction.PayerName,
            PayerEmail = transaction.PayerEmail,
            Status = transaction.Status,
            StatusName = transaction.Status.ToString(),
            Description = transaction.Description,
            StatusMessage = transaction.StatusMessage,
            ErrorCode = transaction.ErrorCode,
            RiskLevel = transaction.RiskLevel,
            RiskLevelName = transaction.RiskLevel.ToString(),
            RiskFactors = riskFactors,
            RequiresManualReview = transaction.RequiresManualReview,
            ReviewedBy = transaction.ReviewedBy,
            ReviewedAt = transaction.ReviewedAt,
            InitiatedAt = transaction.InitiatedAt,
            ProcessedAt = transaction.ProcessedAt,
            CompletedAt = transaction.CompletedAt,
            FailedAt = transaction.FailedAt,
            ExpiresAt = transaction.ExpiresAt,
            RetryCount = transaction.RetryCount,
            LastRetryAt = transaction.LastRetryAt,
            NextRetryAt = transaction.NextRetryAt,
            IsReconciled = transaction.IsReconciled,
            ReconciledAt = transaction.ReconciledAt,
            ReconciledBy = transaction.ReconciledBy
        };
    }

    #endregion

    #region Additional Interface Methods

    public async Task<List<PaymentTransactionDto>> SearchTransactionsAsync(PaymentTransactionSearchDto search)
    {
        // Delegate to existing GetTransactionsAsync method
        return await GetTransactionsAsync(search);
    }

    public async Task<bool> CancelPaymentAsync(int transactionId, string reason, string cancelledBy)
    {
        // Delegate to existing CancelTransactionAsync method
        return await CancelTransactionAsync(transactionId, reason, cancelledBy);
    }

    public async Task<bool> RefundPaymentAsync(int transactionId, decimal amount, string reason, string refundedBy)
    {
        // TODO: Implement refund logic
        _logger.LogInformation("Refund requested for transaction {TransactionId}, amount {Amount}", transactionId, amount);
        return await Task.FromResult(false);
    }

    public async Task<List<string>> GetTransactionLogsAsync(int transactionId)
    {
        // TODO: Implement transaction logs retrieval
        return await Task.FromResult(new List<string> { $"Transaction {transactionId} initiated", $"Transaction {transactionId} processed" });
    }

    #endregion
}