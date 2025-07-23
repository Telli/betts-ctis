using AutoMapper;
using BettsTax.Core.DTOs;
using BettsTax.Data;
using BettsTax.Shared;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using System.Text.Json;

namespace BettsTax.Core.Services
{
    public class PaymentIntegrationService : IPaymentIntegrationService
    {
        private readonly ApplicationDbContext _context;
        private readonly IMapper _mapper;
        private readonly ILogger<PaymentIntegrationService> _logger;
        private readonly IAuditService _auditService;
        private readonly IActivityTimelineService _activityService;
        private readonly ISmsService _smsService;
        private readonly IServiceProvider _serviceProvider;
        private readonly Dictionary<PaymentProvider, IPaymentGatewayProvider> _providers;

        public PaymentIntegrationService(
            ApplicationDbContext context,
            IMapper mapper,
            ILogger<PaymentIntegrationService> logger,
            IAuditService auditService,
            IActivityTimelineService activityService,
            ISmsService smsService,
            IServiceProvider serviceProvider)
        {
            _context = context;
            _mapper = mapper;
            _logger = logger;
            _auditService = auditService;
            _activityService = activityService;
            _smsService = smsService;
            _serviceProvider = serviceProvider;
            _providers = new Dictionary<PaymentProvider, IPaymentGatewayProvider>();
        }

        public async Task<Result<PaymentTransactionDto>> InitiatePaymentAsync(InitiatePaymentDto dto)
        {
            try
            {
                // Validate payment exists and is approved
                var payment = await _context.Payments
                    .Include(p => p.Client)
                    .FirstOrDefaultAsync(p => p.PaymentId == dto.PaymentId);

                if (payment == null)
                    return Result.Failure<PaymentTransactionDto>("Payment not found");

                if (payment.Status != PaymentStatus.Approved)
                    return Result.Failure<PaymentTransactionDto>("Payment must be approved before initiating transaction");

                // Check if payment already has a successful transaction
                var existingTransaction = await _context.Set<PaymentTransaction>()
                    .FirstOrDefaultAsync(pt => pt.PaymentId == dto.PaymentId && 
                                              pt.Status == PaymentTransactionStatus.Completed);

                if (existingTransaction != null)
                    return Result.Failure<PaymentTransactionDto>("Payment already has a completed transaction");

                // Get payment provider config
                var providerConfig = await GetPaymentProviderConfigAsync(dto.Provider);
                if (!providerConfig.IsSuccess || !providerConfig.Value.IsActive)
                    return Result.Failure<PaymentTransactionDto>("Payment provider is not available");

                var config = providerConfig.Value;

                // Validate amount limits
                if (config.MinAmount.HasValue && payment.Amount < config.MinAmount)
                    return Result.Failure<PaymentTransactionDto>($"Amount is below minimum limit of {config.MinAmount:C}");

                if (config.MaxAmount.HasValue && payment.Amount > config.MaxAmount)
                    return Result.Failure<PaymentTransactionDto>($"Amount exceeds maximum limit of {config.MaxAmount:C}");

                // Create payment transaction record
                var transaction = new PaymentTransaction
                {
                    PaymentId = dto.PaymentId,
                    Provider = dto.Provider,
                    Status = PaymentTransactionStatus.Initiated,
                    TransactionReference = GenerateTransactionReference(dto.Provider),
                    Amount = payment.Amount,
                    Currency = "SLE",
                    CustomerPhone = dto.CustomerPhone,
                    CustomerName = dto.CustomerName,
                    CustomerAccountNumber = dto.CustomerAccountNumber,
                    ExpiryDate = DateTime.UtcNow.AddHours(24), // 24-hour expiry
                    InitiatedDate = DateTime.UtcNow
                };

                _context.Set<PaymentTransaction>().Add(transaction);
                await _context.SaveChangesAsync();

                // Get payment provider and initiate payment
                var provider = await GetPaymentProviderAsync(dto.Provider);
                if (provider == null)
                    return Result.Failure<PaymentTransactionDto>("Payment provider not configured");

                var gatewayRequest = new PaymentGatewayRequest
                {
                    TransactionReference = transaction.TransactionReference,
                    Amount = payment.Amount,
                    Currency = "SLE",
                    CustomerPhone = dto.CustomerPhone ?? "",
                    CustomerName = dto.CustomerName,
                    CustomerEmail = dto.CustomerEmail,
                    Description = $"Tax payment for {payment.Client?.BusinessName ?? "Client"}",
                    CallbackUrl = $"/api/payments/webhook/{dto.Provider.ToString().ToLower()}",
                    ReturnUrl = dto.ReturnUrl,
                    AdditionalData = dto.AdditionalData.ToDictionary(kvp => kvp.Key, kvp => (object)kvp.Value)
                };

                var gatewayResult = await provider.InitiatePaymentAsync(gatewayRequest);

                if (gatewayResult.IsSuccess)
                {
                    var response = gatewayResult.Value;
                    
                    // Update transaction with provider response
                    transaction.ProviderTransactionId = response.TransactionId;
                    transaction.ProviderReference = response.ProviderReference;
                    transaction.Status = response.Status;
                    transaction.ProviderFee = response.Fee;
                    transaction.NetAmount = response.Amount;
                    transaction.ProviderResponse = JsonSerializer.Serialize(response);
                    
                    if (response.ExpiryDate.HasValue)
                        transaction.ExpiryDate = response.ExpiryDate;

                    await _context.SaveChangesAsync();

                    // Log activity
                    await _activityService.LogPaymentActivityAsync(
                        payment.PaymentId, 
                        ActivityType.PaymentCreated,
                        $"Payment transaction initiated via {dto.Provider} - {transaction.TransactionReference}");

                    // Send SMS notification if mobile money
                    if (IsMobileMoneyProvider(dto.Provider) && !string.IsNullOrEmpty(dto.CustomerPhone))
                    {
                        await SendPaymentInitiationSms(payment, transaction, dto.CustomerPhone);
                    }

                    var result = _mapper.Map<PaymentTransactionDto>(transaction);
                    result.ProviderName = GetProviderDisplayName(dto.Provider);
                    result.StatusDescription = GetStatusDescription(transaction.Status);
                    
                    return Result.Success(result);
                }
                else
                {
                    transaction.Status = PaymentTransactionStatus.Failed;
                    transaction.FailureReason = gatewayResult.ErrorMessage;
                    transaction.FailedDate = DateTime.UtcNow;
                    await _context.SaveChangesAsync();

                    _logger.LogError("Payment initiation failed for transaction {TransactionId}: {Error}",
                        transaction.TransactionReference, gatewayResult.ErrorMessage);

                    return Result.Failure<PaymentTransactionDto>($"Failed to initiate payment: {gatewayResult.ErrorMessage}");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error initiating payment for PaymentId {PaymentId}", dto.PaymentId);
                return Result.Failure<PaymentTransactionDto>("An error occurred while initiating the payment");
            }
        }

        public async Task<Result<PaymentTransactionDto>> InitiateMobileMoneyPaymentAsync(InitiateMobileMoneyPaymentDto dto)
        {
            try
            {
                // Validate phone number format for Sierra Leone
                var phoneValidation = await ValidatePhoneNumberAsync(dto.PhoneNumber, dto.Provider);
                if (!phoneValidation.IsSuccess)
                    return Result.Failure<PaymentTransactionDto>(phoneValidation.ErrorMessage);

                var initiateDto = new InitiatePaymentDto
                {
                    PaymentId = dto.PaymentId,
                    Provider = dto.Provider,
                    CustomerPhone = dto.PhoneNumber,
                    CustomerName = dto.CustomerName
                };

                return await InitiatePaymentAsync(initiateDto);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error initiating mobile money payment for PaymentId {PaymentId}", dto.PaymentId);
                return Result.Failure<PaymentTransactionDto>("An error occurred while initiating the mobile money payment");
            }
        }

        public async Task<Result<PaymentTransactionDto>> InitiateBankTransferAsync(InitiateBankTransferDto dto)
        {
            try
            {
                var bankProvider = await GetBankPaymentProviderAsync(dto.Provider);
                if (bankProvider == null)
                    return Result.Failure<PaymentTransactionDto>("Bank payment provider not available");

                // Validate bank account
                var accountValidation = await bankProvider.ValidateAccountAsync(dto.AccountNumber, dto.BankCode);
                if (!accountValidation.IsSuccess)
                    return Result.Failure<PaymentTransactionDto>("Invalid bank account details");

                var initiateDto = new InitiatePaymentDto
                {
                    PaymentId = dto.PaymentId,
                    Provider = dto.Provider,
                    CustomerName = dto.AccountName,
                    CustomerAccountNumber = dto.AccountNumber
                };

                var result = await InitiatePaymentAsync(initiateDto);
                
                if (result.IsSuccess)
                {
                    // Update transaction with bank-specific details
                    var transaction = await _context.Set<PaymentTransaction>()
                        .FirstOrDefaultAsync(pt => pt.PaymentTransactionId == result.Value.PaymentTransactionId);
                    
                    if (transaction != null)
                    {
                        transaction.BankCode = dto.BankCode;
                        transaction.BankAccountNumber = dto.AccountNumber;
                        transaction.BankAccountName = dto.AccountName;
                        
                        var bankInfo = await bankProvider.GetAccountInfoAsync(dto.AccountNumber, dto.BankCode);
                        if (bankInfo.IsSuccess)
                        {
                            transaction.BankName = bankInfo.Value.BankName;
                        }
                        
                        await _context.SaveChangesAsync();
                        
                        result.Value.BankCode = dto.BankCode;
                        result.Value.BankAccountNumber = dto.AccountNumber;
                        result.Value.BankAccountName = dto.AccountName;
                        result.Value.BankName = transaction.BankName;
                    }
                }

                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error initiating bank transfer for PaymentId {PaymentId}", dto.PaymentId);
                return Result.Failure<PaymentTransactionDto>("An error occurred while initiating the bank transfer");
            }
        }

        public async Task<Result<PaymentTransactionDto>> GetPaymentTransactionAsync(int transactionId)
        {
            try
            {
                var transaction = await _context.Set<PaymentTransaction>()
                    .Include(pt => pt.Payment)
                        .ThenInclude(p => p.Client)
                    .FirstOrDefaultAsync(pt => pt.PaymentTransactionId == transactionId);

                if (transaction == null)
                    return Result.Failure<PaymentTransactionDto>("Payment transaction not found");

                var dto = _mapper.Map<PaymentTransactionDto>(transaction);
                dto.ProviderName = GetProviderDisplayName(transaction.Provider);
                dto.StatusDescription = GetStatusDescription(transaction.Status);
                dto.ClientName = transaction.Payment?.Client?.BusinessName;
                dto.PaymentReference = transaction.Payment?.PaymentReference;

                return Result.Success(dto);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving payment transaction {TransactionId}", transactionId);
                return Result.Failure<PaymentTransactionDto>("An error occurred while retrieving the payment transaction");
            }
        }

        public async Task<Result<PaymentTransactionDto>> GetPaymentTransactionByReferenceAsync(string reference)
        {
            try
            {
                var transaction = await _context.Set<PaymentTransaction>()
                    .Include(pt => pt.Payment)
                        .ThenInclude(p => p.Client)
                    .FirstOrDefaultAsync(pt => pt.TransactionReference == reference || pt.ProviderReference == reference);

                if (transaction == null)
                    return Result.Failure<PaymentTransactionDto>("Payment transaction not found");

                var dto = _mapper.Map<PaymentTransactionDto>(transaction);
                dto.ProviderName = GetProviderDisplayName(transaction.Provider);
                dto.StatusDescription = GetStatusDescription(transaction.Status);
                dto.ClientName = transaction.Payment?.Client?.BusinessName;
                dto.PaymentReference = transaction.Payment?.PaymentReference;

                return Result.Success(dto);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving payment transaction by reference {Reference}", reference);
                return Result.Failure<PaymentTransactionDto>("An error occurred while retrieving the payment transaction");
            }
        }

        public async Task<Result<PaymentTransactionDto>> CheckPaymentStatusAsync(int transactionId)
        {
            try
            {
                var transaction = await _context.Set<PaymentTransaction>()
                    .Include(pt => pt.Payment)
                        .ThenInclude(p => p.Client)
                    .FirstOrDefaultAsync(pt => pt.PaymentTransactionId == transactionId);

                if (transaction == null)
                    return Result.Failure<PaymentTransactionDto>("Payment transaction not found");

                if (string.IsNullOrEmpty(transaction.ProviderTransactionId))
                {
                    var dto = _mapper.Map<PaymentTransactionDto>(transaction);
                    dto.ProviderName = GetProviderDisplayName(transaction.Provider);
                    dto.StatusDescription = GetStatusDescription(transaction.Status);
                    return Result.Success(dto);
                }

                var provider = await GetPaymentProviderAsync(transaction.Provider);
                if (provider == null)
                    return Result.Failure<PaymentTransactionDto>("Payment provider not available");

                var statusResult = await provider.CheckPaymentStatusAsync(transaction.ProviderTransactionId);
                
                if (statusResult.IsSuccess)
                {
                    var response = statusResult.Value;
                    
                    // Update transaction status if changed
                    var oldStatus = transaction.Status;
                    transaction.Status = response.Status;
                    
                    if (response.Amount.HasValue)
                        transaction.NetAmount = response.Amount;
                        
                    if (response.Fee.HasValue)
                        transaction.ProviderFee = response.Fee;

                    if (oldStatus != response.Status)
                    {
                        if (response.Status == PaymentTransactionStatus.Completed)
                        {
                            transaction.CompletedDate = DateTime.UtcNow;
                            await CompletePaymentProcessing(transaction);
                        }
                        else if (response.Status == PaymentTransactionStatus.Failed)
                        {
                            transaction.FailedDate = DateTime.UtcNow;
                            transaction.FailureReason = response.StatusMessage ?? response.ErrorMessage;
                        }
                        
                        await _context.SaveChangesAsync();
                        
                        // Log status change
                        await _activityService.LogPaymentActivityAsync(
                            transaction.PaymentId,
                            ActivityType.PaymentProcessed,
                            $"Payment status updated from {oldStatus} to {response.Status} - {transaction.TransactionReference}");
                    }
                }

                var result = _mapper.Map<PaymentTransactionDto>(transaction);
                result.ProviderName = GetProviderDisplayName(transaction.Provider);
                result.StatusDescription = GetStatusDescription(transaction.Status);
                result.ClientName = transaction.Payment?.Client?.BusinessName;
                result.PaymentReference = transaction.Payment?.PaymentReference;

                return Result.Success(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error checking payment status for transaction {TransactionId}", transactionId);
                return Result.Failure<PaymentTransactionDto>("An error occurred while checking payment status");
            }
        }

        public async Task<Result<List<PaymentMethodConfigDto>>> GetAvailablePaymentMethodsAsync(decimal amount, string countryCode = "SL")
        {
            try
            {
                var paymentMethods = await _context.Set<PaymentMethodConfig>()
                    .Where(pmc => pmc.IsEnabled && pmc.IsVisible && pmc.CountryCode == countryCode)
                    .Where(pmc => !pmc.MinAmount.HasValue || amount >= pmc.MinAmount)
                    .Where(pmc => !pmc.MaxAmount.HasValue || amount <= pmc.MaxAmount)
                    .OrderBy(pmc => pmc.DisplayOrder)
                    .ToListAsync();

                var dtos = new List<PaymentMethodConfigDto>();
                
                foreach (var method in paymentMethods)
                {
                    var dto = _mapper.Map<PaymentMethodConfigDto>(method);
                    
                    // Calculate estimated fee
                    var feeResult = await CalculateTransactionFeeAsync(amount, method.Provider);
                    if (feeResult.IsSuccess)
                    {
                        dto.EstimatedFee = feeResult.Value;
                        dto.FeeDescription = $"Fee: {feeResult.Value:C}";
                    }
                    
                    dtos.Add(dto);
                }

                return Result.Success(dtos);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving available payment methods for amount {Amount}", amount);
                return Result.Failure<List<PaymentMethodConfigDto>>("An error occurred while retrieving payment methods");
            }
        }

        public async Task<Result<decimal>> CalculateTransactionFeeAsync(decimal amount, PaymentProvider provider)
        {
            try
            {
                var configResult = await GetPaymentProviderConfigAsync(provider);
                if (!configResult.IsSuccess)
                    return Result.Failure<decimal>("Provider configuration not found");

                var config = configResult.Value;
                var percentageFee = amount * (config.FeePercentage / 100);
                var totalFee = percentageFee + config.FixedFee;

                return Result.Success(totalFee);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error calculating fee for provider {Provider}", provider);
                return Result.Failure<decimal>("An error occurred while calculating transaction fee");
            }
        }

        // Placeholder implementations for remaining interface methods
        public Task<Result<PaymentTransactionDto>> RefreshPaymentStatusAsync(string providerTransactionId, PaymentProvider provider) => throw new NotImplementedException();
        public Task<Result<bool>> CompletePaymentAsync(int transactionId, string providerReference) => throw new NotImplementedException();
        public Task<Result<bool>> FailPaymentAsync(int transactionId, string reason) => throw new NotImplementedException();
        public Task<Result<bool>> ProcessWebhookAsync(PaymentProvider provider, string webhookData, string signature) => throw new NotImplementedException();
        public Task<Result<PaymentProviderConfigDto>> GetPaymentProviderConfigAsync(PaymentProvider provider) => throw new NotImplementedException();
        public Task<Result<bool>> TestPaymentProviderAsync(PaymentProvider provider) => throw new NotImplementedException();
        public Task<Result<PagedResult<PaymentTransactionDto>>> GetPaymentTransactionsAsync(int page, int pageSize, PaymentTransactionStatus? status = null, PaymentProvider? provider = null, DateTime? fromDate = null, DateTime? toDate = null) => throw new NotImplementedException();
        public Task<Result<List<PaymentTransactionDto>>> GetClientPaymentTransactionsAsync(int clientId) => throw new NotImplementedException();
        public Task<Result<PaymentTransactionSummaryDto>> GetPaymentTransactionSummaryAsync(DateTime? fromDate = null, DateTime? toDate = null) => throw new NotImplementedException();
        public Task<Result<MobileMoneyBalanceDto>> GetMobileMoneyBalanceAsync(PaymentProvider provider) => throw new NotImplementedException();
        public Task<Result<bool>> ValidatePhoneNumberAsync(string phoneNumber, PaymentProvider provider) => throw new NotImplementedException();
        public Task<Result<bool>> RetryFailedPaymentAsync(int transactionId) => throw new NotImplementedException();
        public Task<Result<int>> ProcessPendingPaymentsAsync() => throw new NotImplementedException();
        public Task<Result<bool>> RefundPaymentAsync(int transactionId, decimal? partialAmount = null, string reason = "") => throw new NotImplementedException();

        // Helper methods
        private string GenerateTransactionReference(PaymentProvider provider)
        {
            var prefix = provider switch
            {
                PaymentProvider.OrangeMoney => "OM",
                PaymentProvider.AfricellMoney => "AM",
                _ => "TX"
            };
            
            return $"{prefix}-{DateTime.UtcNow:yyyyMMddHHmmss}-{Random.Shared.Next(1000, 9999)}";
        }

        private async Task<IPaymentGatewayProvider?> GetPaymentProviderAsync(PaymentProvider provider)
        {
            // This would be implemented to resolve providers from DI container
            // For now, return null to indicate not implemented
            return await Task.FromResult<IPaymentGatewayProvider?>(null);
        }

        private async Task<IBankPaymentProvider?> GetBankPaymentProviderAsync(PaymentProvider provider)
        {
            // This would be implemented to resolve bank providers from DI container
            return await Task.FromResult<IBankPaymentProvider?>(null);
        }

        private bool IsMobileMoneyProvider(PaymentProvider provider)
        {
            return provider == PaymentProvider.OrangeMoney || provider == PaymentProvider.AfricellMoney;
        }

        private string GetProviderDisplayName(PaymentProvider provider)
        {
            return provider switch
            {
                PaymentProvider.OrangeMoney => "Orange Money",
                PaymentProvider.AfricellMoney => "Africell Money",
                PaymentProvider.SierraLeoneCommercialBank => "Sierra Leone Commercial Bank",
                PaymentProvider.RoyalBankSL => "Royal Bank SL",
                PaymentProvider.FirstBankSL => "First Bank SL",
                PaymentProvider.UnionTrustBank => "Union Trust Bank",
                PaymentProvider.AccessBankSL => "Access Bank SL",
                PaymentProvider.PayPal => "PayPal",
                PaymentProvider.Stripe => "Stripe",
                _ => provider.ToString()
            };
        }

        private string GetStatusDescription(PaymentTransactionStatus status)
        {
            return status switch
            {
                PaymentTransactionStatus.Initiated => "Payment initiated",
                PaymentTransactionStatus.Pending => "Awaiting payment confirmation",
                PaymentTransactionStatus.Processing => "Processing payment",
                PaymentTransactionStatus.Completed => "Payment completed successfully",
                PaymentTransactionStatus.Failed => "Payment failed",
                PaymentTransactionStatus.Cancelled => "Payment cancelled",
                PaymentTransactionStatus.Expired => "Payment expired",
                PaymentTransactionStatus.Refunded => "Payment refunded",
                _ => status.ToString()
            };
        }

        private async Task SendPaymentInitiationSms(Payment payment, PaymentTransaction transaction, string phoneNumber)
        {
            try
            {
                var message = $"Payment request initiated for {payment.Amount:C}. " +
                            $"Reference: {transaction.TransactionReference}. " +
                            $"Follow the prompts on your phone to complete payment.";

                await _smsService.SendSmsAsync(new SendSmsDto
                {
                    PhoneNumber = phoneNumber,
                    Message = message,
                    Type = SmsType.PaymentConfirmation,
                    ClientId = payment.ClientId,
                    PaymentId = payment.PaymentId
                });
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to send payment initiation SMS for transaction {Reference}", 
                    transaction.TransactionReference);
            }
        }

        private async Task CompletePaymentProcessing(PaymentTransaction transaction)
        {
            try
            {
                // Update the main payment status if needed
                var payment = await _context.Payments.FindAsync(transaction.PaymentId);
                if (payment != null && payment.Status == PaymentStatus.Approved)
                {
                    // Payment remains approved but is now processed
                    payment.ApprovalWorkflow += $" | Completed via {transaction.Provider} on {DateTime.UtcNow:yyyy-MM-dd}";
                    await _context.SaveChangesAsync();
                }

                // Send completion SMS
                if (!string.IsNullOrEmpty(transaction.CustomerPhone))
                {
                    await SendPaymentCompletionSms(payment, transaction);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in payment completion processing for transaction {Reference}", 
                    transaction.TransactionReference);
            }
        }

        private async Task SendPaymentCompletionSms(Payment payment, PaymentTransaction transaction)
        {
            try
            {
                var message = $"Payment of {transaction.NetAmount ?? transaction.Amount:C} completed successfully. " +
                            $"Reference: {transaction.TransactionReference}. Thank you!";

                await _smsService.SendSmsAsync(new SendSmsDto
                {
                    PhoneNumber = transaction.CustomerPhone,
                    Message = message,
                    Type = SmsType.PaymentConfirmation,
                    ClientId = payment.ClientId,
                    PaymentId = payment.PaymentId
                });
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to send payment completion SMS for transaction {Reference}", 
                    transaction.TransactionReference);
            }
        }
    }
}