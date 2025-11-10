using AutoMapper;
using BettsTax.Core.DTOs;
using BettsTax.Data;
using BettsTax.Shared;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System.Linq;
using System.Net.Http;
using System.Text.Json;
using PaymentGatewayRequest = BettsTax.Core.Services.Payments.PaymentGatewayRequest;
using PaymentGatewayResponse = BettsTax.Core.Services.Payments.PaymentGatewayResponse;

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
        private readonly Dictionary<PaymentProvider, IPaymentGatewayProvider> _providers;
        private readonly Dictionary<PaymentProvider, PaymentProviderConfig> _providerConfigCache;
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly ILoggerFactory _loggerFactory;

        public PaymentIntegrationService(
            ApplicationDbContext context,
            IMapper mapper,
            ILogger<PaymentIntegrationService> logger,
            IAuditService auditService,
            IActivityTimelineService activityService,
            ISmsService smsService,
            IHttpClientFactory httpClientFactory,
            ILoggerFactory loggerFactory)
        {
            _context = context;
            _mapper = mapper;
            _logger = logger;
            _auditService = auditService;
            _activityService = activityService;
            _smsService = smsService;
            _providers = new Dictionary<PaymentProvider, IPaymentGatewayProvider>();
            _providerConfigCache = new Dictionary<PaymentProvider, PaymentProviderConfig>();
            _httpClientFactory = httpClientFactory;
            _loggerFactory = loggerFactory;
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
                    Description = $"Tax payment for {payment.TaxType}",
                    CallbackUrl = "", // Would need to be configured separately
                    ReturnUrl = dto.ReturnUrl ?? "",
                    Metadata = dto.AdditionalData?.ToDictionary(kvp => kvp.Key, kvp => (object)kvp.Value)
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
                        .ThenInclude(p => p!.Client)
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
                        .ThenInclude(p => p!.Client)
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
                        .ThenInclude(p => p!.Client)
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

        public async Task<Result<PaymentTransactionDto>> RefreshPaymentStatusAsync(string providerTransactionId, PaymentProvider provider)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(providerTransactionId))
                    return Result.Failure<PaymentTransactionDto>("Provider transaction id is required");

                var transaction = await _context.PaymentTransactions
                    .Include(pt => pt.Payment)
                        .ThenInclude(p => p!.Client)
                    .FirstOrDefaultAsync(pt => pt.Provider == provider &&
                        (pt.ProviderTransactionId == providerTransactionId || pt.TransactionReference == providerTransactionId));

                if (transaction == null)
                    return Result.Failure<PaymentTransactionDto>("Payment transaction not found");

                if (string.IsNullOrEmpty(transaction.ProviderTransactionId))
                    transaction.ProviderTransactionId = providerTransactionId;

                var gateway = await GetPaymentProviderAsync(provider);
                if (gateway == null)
                    return Result.Failure<PaymentTransactionDto>("Payment provider not available");

                var statusResult = await gateway.CheckPaymentStatusAsync(transaction.ProviderTransactionId!);
                if (!statusResult.IsSuccess)
                    return Result.Failure<PaymentTransactionDto>($"Failed to refresh status: {statusResult.ErrorMessage}");

                await ApplyGatewayResponseAsync(transaction, statusResult.Value, source: "status-refresh");

                var dto = _mapper.Map<PaymentTransactionDto>(transaction);
                dto.ProviderName = GetProviderDisplayName(transaction.Provider);
                dto.StatusDescription = GetStatusDescription(transaction.Status);
                dto.ClientName = transaction.Payment?.Client?.BusinessName;
                dto.PaymentReference = transaction.Payment?.PaymentReference;

                return Result.Success(dto);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error refreshing payment status for {ProviderTransactionId}", providerTransactionId);
                return Result.Failure<PaymentTransactionDto>("An error occurred while refreshing payment status");
            }
        }

        public async Task<Result<bool>> CompletePaymentAsync(int transactionId, string providerReference)
        {
            try
            {
                var transaction = await _context.PaymentTransactions
                    .FirstOrDefaultAsync(pt => pt.PaymentTransactionId == transactionId);

                if (transaction == null)
                    return Result.Failure<bool>("Payment transaction not found");

                if (transaction.Status == PaymentTransactionStatus.Completed)
                    return Result.Success(true);

                transaction.Status = PaymentTransactionStatus.Completed;
                transaction.CompletedDate = DateTime.UtcNow;
                transaction.ProviderReference = providerReference;
                transaction.FailureReason = null;

                await _context.SaveChangesAsync();
                await CompletePaymentProcessing(transaction);

                await _activityService.LogPaymentActivityAsync(
                    transaction.PaymentId,
                    ActivityType.PaymentProcessed,
                    $"Payment manually marked as completed - {transaction.TransactionReference}");

                return Result.Success(true);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error completing payment transaction {TransactionId}", transactionId);
                return Result.Failure<bool>("An error occurred while completing the payment");
            }
        }

        public async Task<Result<bool>> FailPaymentAsync(int transactionId, string reason)
        {
            try
            {
                var transaction = await _context.PaymentTransactions
                    .FirstOrDefaultAsync(pt => pt.PaymentTransactionId == transactionId);

                if (transaction == null)
                    return Result.Failure<bool>("Payment transaction not found");

                transaction.Status = PaymentTransactionStatus.Failed;
                transaction.FailureReason = reason;
                transaction.FailedDate = DateTime.UtcNow;

                await _context.SaveChangesAsync();

                await _activityService.LogPaymentActivityAsync(
                    transaction.PaymentId,
                    ActivityType.PaymentProcessed,
                    $"Payment marked as failed - {transaction.TransactionReference}. Reason: {reason}");

                return Result.Success(true);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error failing payment transaction {TransactionId}", transactionId);
                return Result.Failure<bool>("An error occurred while failing the payment");
            }
        }

        public async Task<Result<bool>> ProcessWebhookAsync(PaymentProvider provider, string webhookData, string signature)
        {
            try
            {
                var gateway = await GetPaymentProviderAsync(provider);
                if (gateway == null)
                    return Result.Failure<bool>("Payment provider not available");

                var signatureResult = await gateway.ValidateWebhookSignatureAsync(webhookData, signature);
                if (!signatureResult.IsSuccess || !signatureResult.Value)
                    return Result.Failure<bool>("Invalid webhook signature");

                var processResult = await gateway.ProcessWebhookAsync(webhookData);
                if (!processResult.IsSuccess)
                    return Result.Failure<bool>($"Failed to process webhook: {processResult.ErrorMessage}");

                if (string.IsNullOrWhiteSpace(processResult.Value.TransactionId))
                    return Result.Success(false);

                var transaction = await _context.PaymentTransactions
                    .Include(pt => pt.Payment)
                    .FirstOrDefaultAsync(pt =>
                        pt.Provider == provider &&
                        (pt.TransactionReference == processResult.Value.TransactionId ||
                         pt.ProviderTransactionId == processResult.Value.TransactionId ||
                         pt.ProviderReference == processResult.Value.ProviderReference));

                if (transaction == null)
                {
                    _logger.LogWarning("Webhook received for unknown transaction. Provider={Provider} TransactionId={TransactionId}",
                        provider, processResult.Value.TransactionId);
                    return Result.Failure<bool>("Associated transaction not found");
                }

                await ApplyGatewayResponseAsync(transaction, processResult.Value, source: "webhook");

                _context.PaymentWebhookLogs.Add(new PaymentWebhookLog
                {
                    PaymentTransactionId = transaction.PaymentTransactionId,
                    Provider = provider,
                    WebhookType = processResult.Value.Status.ToString(),
                    RequestBody = webhookData,
                    ResponseStatus = processResult.Value.StatusMessage,
                    ProcessingResult = processResult.Value.Status.ToString(),
                    IsProcessed = true,
                    ProcessedDate = DateTime.UtcNow
                });

                await _context.SaveChangesAsync();

                return Result.Success(true);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing webhook for provider {Provider}", provider);
                return Result.Failure<bool>("An error occurred while processing the webhook");
            }
        }

        public async Task<Result<PaymentProviderConfigDto>> GetPaymentProviderConfigAsync(PaymentProvider provider)
        {
            try
            {
                var config = await GetProviderConfigEntityAsync(provider, includeInactive: true);
                if (config == null)
                    return Result.Failure<PaymentProviderConfigDto>("Payment provider configuration not found");

                var dto = _mapper.Map<PaymentProviderConfigDto>(config);
                return Result.Success(dto);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving provider configuration for {Provider}", provider);
                return Result.Failure<PaymentProviderConfigDto>("An error occurred while retrieving provider configuration");
            }
        }

        public async Task<Result<bool>> TestPaymentProviderAsync(PaymentProvider provider)
        {
            try
            {
                var gateway = await GetPaymentProviderAsync(provider);
                if (gateway == null)
                    return Result.Failure<bool>("Payment provider not available");

                var testResult = await gateway.TestConnectionAsync();
                if (testResult.IsSuccess && testResult.Value)
                {
                    var config = await _context.PaymentProviderConfigs
                        .FirstOrDefaultAsync(pc => pc.Provider == provider);
                    if (config != null)
                    {
                        config.LastUsedDate = DateTime.UtcNow;
                        await _context.SaveChangesAsync();
                        _providerConfigCache[provider] = config;
                    }
                }

                return testResult;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error testing payment provider {Provider}", provider);
                return Result.Failure<bool>("An error occurred while testing the payment provider");
            }
        }

        public async Task<Result<PagedResult<PaymentTransactionDto>>> GetPaymentTransactionsAsync(
            int page,
            int pageSize,
            PaymentTransactionStatus? status = null,
            PaymentProvider? provider = null,
            DateTime? fromDate = null,
            DateTime? toDate = null)
        {
            try
            {
                page = Math.Max(1, page);
                pageSize = Math.Clamp(pageSize, 1, 200);

                var query = _context.PaymentTransactions
                    .Include(pt => pt.Payment)
                        .ThenInclude(p => p!.Client)
                    .AsQueryable();

                if (status.HasValue)
                    query = query.Where(pt => pt.Status == status.Value);

                if (provider.HasValue)
                    query = query.Where(pt => pt.Provider == provider.Value);

                if (fromDate.HasValue)
                    query = query.Where(pt => pt.CreatedDate >= fromDate.Value);

                if (toDate.HasValue)
                {
                    var inclusiveTo = toDate.Value.Date.AddDays(1);
                    query = query.Where(pt => pt.CreatedDate < inclusiveTo);
                }

                var total = await query.CountAsync();
                var items = await query
                    .OrderByDescending(pt => pt.CreatedDate)
                    .Skip((page - 1) * pageSize)
                    .Take(pageSize)
                    .ToListAsync();

                var dtos = _mapper.Map<List<PaymentTransactionDto>>(items);
                foreach (var dto in dtos)
                {
                    dto.ProviderName = GetProviderDisplayName(dto.Provider);
                    dto.StatusDescription = GetStatusDescription(dto.Status);
                    var entity = items.First(i => i.PaymentTransactionId == dto.PaymentTransactionId);
                    dto.ClientName = entity.Payment?.Client?.BusinessName;
                    dto.PaymentReference = entity.Payment?.PaymentReference;
                }

                var result = new PagedResult<PaymentTransactionDto>
                {
                    Items = dtos,
                    Page = page,
                    PageSize = pageSize,
                    TotalCount = total
                };

                return Result.Success(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving paged payment transactions");
                return Result.Failure<PagedResult<PaymentTransactionDto>>("An error occurred while retrieving transactions");
            }
        }

        public async Task<Result<List<PaymentTransactionDto>>> GetClientPaymentTransactionsAsync(int clientId)
        {
            try
            {
                var transactions = await _context.PaymentTransactions
                    .Include(pt => pt.Payment)
                        .ThenInclude(p => p!.Client)
                    .Where(pt => pt.Payment != null && pt.Payment.ClientId == clientId)
                    .OrderByDescending(pt => pt.CreatedDate)
                    .ToListAsync();

                var dtos = _mapper.Map<List<PaymentTransactionDto>>(transactions);
                foreach (var dto in dtos)
                {
                    var entity = transactions.First(t => t.PaymentTransactionId == dto.PaymentTransactionId);
                    dto.ProviderName = GetProviderDisplayName(dto.Provider);
                    dto.StatusDescription = GetStatusDescription(dto.Status);
                    dto.ClientName = entity.Payment?.Client?.BusinessName;
                    dto.PaymentReference = entity.Payment?.PaymentReference;
                }

                return Result.Success(dtos);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving transactions for client {ClientId}", clientId);
                return Result.Failure<List<PaymentTransactionDto>>("An error occurred while retrieving client transactions");
            }
        }

        public async Task<Result<PaymentTransactionSummaryDto>> GetPaymentTransactionSummaryAsync(DateTime? fromDate = null, DateTime? toDate = null)
        {
            try
            {
                var query = _context.PaymentTransactions.AsQueryable();

                if (fromDate.HasValue)
                    query = query.Where(pt => pt.CreatedDate >= fromDate.Value);

                if (toDate.HasValue)
                {
                    var inclusiveTo = toDate.Value.Date.AddDays(1);
                    query = query.Where(pt => pt.CreatedDate < inclusiveTo);
                }

                var transactions = await query.ToListAsync();

                var summary = new PaymentTransactionSummaryDto
                {
                    TotalTransactions = transactions.Count,
                    CompletedTransactions = transactions.Count(t => t.Status == PaymentTransactionStatus.Completed),
                    PendingTransactions = transactions.Count(t => t.Status == PaymentTransactionStatus.Pending || t.Status == PaymentTransactionStatus.Processing),
                    FailedTransactions = transactions.Count(t => t.Status == PaymentTransactionStatus.Failed),
                    TotalAmount = transactions.Sum(t => t.Amount),
                    CompletedAmount = transactions.Where(t => t.Status == PaymentTransactionStatus.Completed).Sum(t => t.Amount),
                    PendingAmount = transactions.Where(t => t.Status == PaymentTransactionStatus.Pending || t.Status == PaymentTransactionStatus.Processing).Sum(t => t.Amount),
                    TotalFees = transactions.Sum(t => t.ProviderFee ?? 0)
                };

                foreach (var group in transactions.GroupBy(t => t.Provider))
                {
                    summary.TransactionsByProvider[group.Key] = group.Count();
                    summary.AmountByProvider[group.Key] = group.Sum(t => t.Amount);
                }

                foreach (var dayGroup in transactions.GroupBy(t => t.CreatedDate.Date))
                {
                    var key = dayGroup.Key.ToString("yyyy-MM-dd");
                    summary.DailyTransactions[key] = dayGroup.Count();
                    summary.DailyAmounts[key] = dayGroup.Sum(t => t.Amount);
                }

                if (summary.TotalTransactions > 0)
                {
                    summary.SuccessRate = Math.Round((decimal)summary.CompletedTransactions / summary.TotalTransactions * 100, 2);
                    summary.AverageTransactionAmount = Math.Round(summary.TotalAmount / summary.TotalTransactions, 2);
                    var completed = transactions.Where(t => t.Status == PaymentTransactionStatus.Completed && t.InitiatedDate.HasValue && t.CompletedDate.HasValue)
                        .Select(t => (decimal)(t.CompletedDate!.Value - t.InitiatedDate!.Value).TotalMinutes);
                    summary.AverageProcessingTime = completed.Any() ? Math.Round(completed.Average(), 2) : 0;
                }

                return Result.Success(summary);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error generating payment transaction summary");
                return Result.Failure<PaymentTransactionSummaryDto>("An error occurred while generating the summary");
            }
        }

        public async Task<Result<MobileMoneyBalanceDto>> GetMobileMoneyBalanceAsync(PaymentProvider provider)
        {
            try
            {
                var gateway = await GetPaymentProviderAsync(provider) as IMobileMoneyProvider;
                if (gateway == null)
                    return Result.Failure<MobileMoneyBalanceDto>("Mobile money provider not available");

                var balanceResult = await gateway.GetBalanceAsync();
                if (!balanceResult.IsSuccess)
                    return Result.Failure<MobileMoneyBalanceDto>($"Failed to retrieve balance: {balanceResult.ErrorMessage}");

                var configResult = await GetPaymentProviderConfigAsync(provider);
                if (!configResult.IsSuccess)
                    return Result.Failure<MobileMoneyBalanceDto>(configResult.ErrorMessage ?? "Provider config missing");

                var dto = new MobileMoneyBalanceDto
                {
                    Provider = provider,
                    ProviderName = configResult.Value.Name,
                    Balance = balanceResult.Value,
                    Currency = configResult.Value.SupportedCurrency,
                    AccountName = configResult.Value.Name,
                    LastUpdated = DateTime.UtcNow,
                    IsActive = configResult.Value.IsActive
                };

                return Result.Success(dto);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving mobile money balance for provider {Provider}", provider);
                return Result.Failure<MobileMoneyBalanceDto>("An error occurred while retrieving balance");
            }
        }

        public async Task<Result<bool>> ValidatePhoneNumberAsync(string phoneNumber, PaymentProvider provider)
        {
            try
            {
                if (!IsMobileMoneyProvider(provider))
                    return Result.Failure<bool>("Phone validation is only available for mobile money providers");

                var gateway = await GetPaymentProviderAsync(provider) as IMobileMoneyProvider;
                if (gateway == null)
                    return Result.Failure<bool>("Mobile money provider not available");

                var result = await gateway.ValidatePhoneNumberAsync(phoneNumber);
                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error validating phone number for provider {Provider}", provider);
                return Result.Failure<bool>("An error occurred while validating the phone number");
            }
        }

        public async Task<Result<bool>> RetryFailedPaymentAsync(int transactionId)
        {
            try
            {
                var transaction = await _context.PaymentTransactions
                    .FirstOrDefaultAsync(pt => pt.PaymentTransactionId == transactionId);

                if (transaction == null)
                    return Result.Failure<bool>("Payment transaction not found");

                if (transaction.Status != PaymentTransactionStatus.Failed)
                    return Result.Failure<bool>("Only failed transactions can be retried");

                if (transaction.RetryCount >= transaction.MaxRetries)
                    return Result.Failure<bool>("Maximum retry attempts reached");

                transaction.RetryCount++;
                transaction.NextRetryDate = DateTime.UtcNow.AddMinutes(10);
                transaction.Status = PaymentTransactionStatus.Pending;
                transaction.FailureReason = null;

                await _context.SaveChangesAsync();

                await _activityService.LogPaymentActivityAsync(
                    transaction.PaymentId,
                    ActivityType.PaymentProcessed,
                    $"Payment scheduled for retry (attempt {transaction.RetryCount}) - {transaction.TransactionReference}");

                return Result.Success(true);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrying payment transaction {TransactionId}", transactionId);
                return Result.Failure<bool>("An error occurred while retrying the payment");
            }
        }

        public async Task<Result<int>> ProcessPendingPaymentsAsync()
        {
            try
            {
                var dueTransactions = await _context.PaymentTransactions
                    .Where(pt =>
                        pt.Status == PaymentTransactionStatus.Pending ||
                        (pt.Status == PaymentTransactionStatus.Failed && pt.NextRetryDate != null && pt.NextRetryDate <= DateTime.UtcNow && pt.RetryCount < pt.MaxRetries))
                    .ToListAsync();

                int processed = 0;
                foreach (var transaction in dueTransactions)
                {
                    var statusResult = await RefreshPaymentStatusAsync(transaction.ProviderTransactionId ?? transaction.TransactionReference, transaction.Provider);
                    if (statusResult.IsSuccess)
                    {
                        processed++;
                    }
                }

                return Result.Success(processed);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing pending payments");
                return Result.Failure<int>("An error occurred while processing pending payments");
            }
        }

        public async Task<Result<bool>> RefundPaymentAsync(int transactionId, decimal? partialAmount = null, string reason = "")
        {
            try
            {
                var transaction = await _context.PaymentTransactions
                    .Include(pt => pt.Payment)
                    .FirstOrDefaultAsync(pt => pt.PaymentTransactionId == transactionId);

                if (transaction == null)
                    return Result.Failure<bool>("Payment transaction not found");

                if (transaction.Status != PaymentTransactionStatus.Completed)
                    return Result.Failure<bool>("Only completed transactions can be refunded");

                if (string.IsNullOrEmpty(transaction.ProviderTransactionId))
                    return Result.Failure<bool>("Transaction is missing provider reference");

                var gateway = await GetPaymentProviderAsync(transaction.Provider);
                if (gateway == null)
                    return Result.Failure<bool>("Payment provider not available");

                var amountToRefund = partialAmount ?? transaction.Amount;
                var refundResult = await gateway.RefundPaymentAsync(transaction.ProviderTransactionId, amountToRefund);

                if (!refundResult.IsSuccess)
                    return Result.Failure<bool>($"Failed to process refund: {refundResult.ErrorMessage}");

                transaction.Status = refundResult.Value.Status;
                transaction.FailureReason = reason;
                transaction.ProviderResponse = JsonSerializer.Serialize(refundResult.Value);
                await _context.SaveChangesAsync();

                await _activityService.LogPaymentActivityAsync(
                    transaction.PaymentId,
                    ActivityType.PaymentProcessed,
                    $"Payment refund processed ({amountToRefund:C}) - {transaction.TransactionReference}");

                return Result.Success(true);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error refunding payment transaction {TransactionId}", transactionId);
                return Result.Failure<bool>("An error occurred while processing the refund");
            }
        }

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
            if (_providers.TryGetValue(provider, out var cachedProvider))
                return cachedProvider;

            var config = await GetProviderConfigEntityAsync(provider);
            if (config == null)
            {
                _logger.LogWarning("Payment provider configuration not found for {Provider}", provider);
                return null;
            }

            var httpClient = _httpClientFactory.CreateClient(provider.ToString());

            IPaymentGatewayProvider? gateway = provider switch
            {
                PaymentProvider.OrangeMoney => new OrangeMoneyProvider(httpClient, _loggerFactory.CreateLogger<OrangeMoneyProvider>(), config),
                PaymentProvider.AfricellMoney => new AfricellMoneyProvider(httpClient, _loggerFactory.CreateLogger<AfricellMoneyProvider>(), config),
                PaymentProvider.PayPal => new PayPalProvider(httpClient, _loggerFactory.CreateLogger<PayPalProvider>(), config),
                PaymentProvider.Stripe => new StripeProvider(httpClient, _loggerFactory.CreateLogger<StripeProvider>(), config),
                _ => null
            };

            if (gateway == null)
            {
                _logger.LogWarning("No gateway implementation available for provider {Provider}", provider);
                return null;
            }

            _providers[provider] = gateway;
            return gateway;
        }

        private async Task<IBankPaymentProvider?> GetBankPaymentProviderAsync(PaymentProvider provider)
        {
            var gateway = await GetPaymentProviderAsync(provider);
            return gateway as IBankPaymentProvider;
        }

        private async Task<PaymentProviderConfig?> GetProviderConfigEntityAsync(PaymentProvider provider, bool includeInactive = false)
        {
            if (_providerConfigCache.TryGetValue(provider, out var cached) && (includeInactive || cached.IsActive))
                return cached;

            var query = _context.PaymentProviderConfigs.AsNoTracking().Where(pc => pc.Provider == provider);
            if (!includeInactive)
                query = query.Where(pc => pc.IsActive);

            var config = await query.FirstOrDefaultAsync();
            if (config != null)
            {
                _providerConfigCache[provider] = config;
            }

            return config;
        }

        private async Task ApplyGatewayResponseAsync(PaymentTransaction transaction, PaymentGatewayResponse response, string source)
        {
            var previousStatus = transaction.Status;

            if (!string.IsNullOrEmpty(response.TransactionId) && string.IsNullOrEmpty(transaction.ProviderTransactionId))
                transaction.ProviderTransactionId = response.TransactionId;

            if (!string.IsNullOrEmpty(response.ProviderReference))
                transaction.ProviderReference = response.ProviderReference;

            if (response.Amount.HasValue)
                transaction.NetAmount = response.Amount;

            if (response.Fee.HasValue)
                transaction.ProviderFee = response.Fee;

            if (response.ExpiryDate.HasValue)
                transaction.ExpiryDate = response.ExpiryDate;

            transaction.ProviderResponse = JsonSerializer.Serialize(response);
            transaction.Status = response.Status;

            if (response.Status == PaymentTransactionStatus.Completed)
            {
                transaction.CompletedDate ??= DateTime.UtcNow;
                await CompletePaymentProcessing(transaction);
            }
            else if (response.Status == PaymentTransactionStatus.Failed)
            {
                transaction.FailedDate = DateTime.UtcNow;
                transaction.FailureReason = response.StatusMessage ?? response.ErrorMessage;
            }
            else if (response.Status == PaymentTransactionStatus.Pending)
            {
                transaction.NextRetryDate = null;
            }

            await _context.SaveChangesAsync();

            if (previousStatus != transaction.Status)
            {
                await _activityService.LogPaymentActivityAsync(
                    transaction.PaymentId,
                    ActivityType.PaymentProcessed,
                    $"Payment status updated from {previousStatus} to {transaction.Status} via {source} - {transaction.TransactionReference}");
            }
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
                if (payment != null && !string.IsNullOrEmpty(transaction.CustomerPhone))
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
                    PhoneNumber = transaction.CustomerPhone ?? string.Empty,
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

        public async Task<Result<int>> ReconcilePendingPaymentsAsync()
        {
            try
            {
                var pendingTransactions = await _context.PaymentTransactions
                    .Where(pt => pt.Status == PaymentTransactionStatus.Pending || pt.Status == PaymentTransactionStatus.Processing)
                    .ToListAsync();

                int reconciledCount = 0;
                foreach (var transaction in pendingTransactions)
                {
                    var statusResult = await CheckPaymentStatusAsync(transaction.PaymentTransactionId);
                    if (statusResult.IsSuccess)
                    {
                        reconciledCount++;
                    }
                }

                return Result<int>.Success(reconciledCount);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error reconciling pending payments");
                return Result.Failure<int>($"Error reconciling pending payments: {ex.Message}");
            }
        }

        public async Task<Result<int>> ProcessPaymentRetriesAsync()
        {
            try
            {
                var failedTransactions = await _context.PaymentTransactions
                    .Where(pt => pt.Status == PaymentTransactionStatus.Failed && pt.RetryCount < 3)
                    .ToListAsync();

                int retriedCount = 0;
                foreach (var transaction in failedTransactions)
                {
                    var retryResult = await RetryFailedPaymentAsync(transaction.PaymentTransactionId);
                    if (retryResult.IsSuccess)
                    {
                        retriedCount++;
                    }
                }

                return Result<int>.Success(retriedCount);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing payment retries");
                return Result.Failure<int>($"Error processing payment retries: {ex.Message}");
            }
        }
    }
}