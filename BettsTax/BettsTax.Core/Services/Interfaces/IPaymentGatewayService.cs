using BettsTax.Core.DTOs.Payment;
using BettsTax.Data.Models;

namespace BettsTax.Core.Services.Interfaces;

/// <summary>
/// Main payment gateway service for Sierra Leone mobile money integration
/// Handles Orange Money, Africell Money, and other payment methods
/// </summary>
public interface IPaymentGatewayService
{
    // Transaction Management
    Task<PaymentTransactionDto> InitiatePaymentAsync(CreatePaymentTransactionDto request, string initiatedBy);
    Task<PaymentTransactionDto> ProcessPaymentAsync(ProcessPaymentDto request, string processedBy);
    Task<PaymentTransactionDto> GetTransactionAsync(int transactionId);
    Task<PaymentTransactionDto?> GetTransactionByReferenceAsync(string transactionReference);
    Task<List<PaymentTransactionDto>> GetTransactionsAsync(PaymentTransactionSearchDto search);
    Task<List<PaymentTransactionDto>> SearchTransactionsAsync(PaymentTransactionSearchDto search);
    Task<PaymentTransactionDto> UpdateTransactionStatusAsync(int transactionId, PaymentTransactionStatus status, string statusMessage, string updatedBy);
    Task<bool> CancelTransactionAsync(int transactionId, string reason, string cancelledBy);
    Task<bool> CancelPaymentAsync(int transactionId, string reason, string cancelledBy);
    Task<bool> RefundPaymentAsync(int transactionId, decimal amount, string reason, string refundedBy);
    Task<List<string>> GetTransactionLogsAsync(int transactionId);
    Task<bool> ExpireTransactionAsync(int transactionId, string expiredBy);
    
    // Payment Processing
    Task<PaymentTransactionDto> RetryPaymentAsync(int transactionId, string retriedBy);
    Task<PaymentTransactionDto> ConfirmPaymentAsync(int transactionId, string externalReference, string confirmedBy);
    Task<bool> ValidatePaymentAsync(int transactionId);
    Task<decimal> CalculateFeesAsync(PaymentGatewayType gatewayType, decimal amount);
    Task<bool> CheckTransactionLimitsAsync(PaymentGatewayType gatewayType, decimal amount, string payerPhone);
    
    // Refund Management
    Task<PaymentRefundDto> InitiateRefundAsync(CreatePaymentRefundDto request, string initiatedBy);
    Task<PaymentRefundDto> ProcessRefundAsync(int refundId, string processedBy);
    Task<PaymentRefundDto> GetRefundAsync(int refundId);
    Task<List<PaymentRefundDto>> GetRefundsAsync(int? transactionId = null, int page = 1, int pageSize = 20);
    Task<bool> ApproveRefundAsync(int refundId, string approvedBy);
    Task<bool> RejectRefundAsync(int refundId, string reason, string rejectedBy);
    
    // Mobile Money Specific
    Task<bool> ValidatePhoneNumberAsync(PaymentGatewayType gatewayType, string phoneNumber);
    Task<string> FormatPhoneNumberAsync(PaymentGatewayType gatewayType, string phoneNumber);
    Task<bool> CheckAccountBalanceAsync(PaymentGatewayType gatewayType, string phoneNumber);
    Task<string> SendPaymentRequestAsync(int transactionId);
    Task<PaymentTransactionDto> CheckPaymentStatusAsync(int transactionId);
    
    // Reconciliation
    Task<bool> ReconcileTransactionAsync(int transactionId, string reconciledBy);
    Task<List<PaymentTransactionDto>> GetUnreconciledTransactionsAsync(PaymentGatewayType? gatewayType = null);
    Task<bool> BulkReconcileAsync(List<int> transactionIds, string reconciledBy);
    Task<PaymentAnalyticsDto> GetReconciliationReportAsync(DateTime fromDate, DateTime toDate);
    
    // Analytics and Reporting
    Task<PaymentAnalyticsDto> GetPaymentAnalyticsAsync(DateTime fromDate, DateTime toDate, PaymentGatewayType? gatewayType = null);
    Task<List<PaymentGatewayStatsDto>> GetGatewayPerformanceAsync(DateTime fromDate, DateTime toDate);
    Task<List<PaymentErrorStatsDto>> GetErrorAnalyticsAsync(DateTime fromDate, DateTime toDate);
    Task<SecurityStatsDto> GetSecurityAnalyticsAsync(DateTime fromDate, DateTime toDate);
    Task<byte[]> ExportTransactionsAsync(PaymentTransactionSearchDto search, string format = "excel");
}

/// <summary>
/// Payment gateway configuration service
/// </summary>
public interface IPaymentGatewayConfigService
{
    // Gateway Configuration
    Task<PaymentGatewayConfigDto> CreateGatewayConfigAsync(CreatePaymentGatewayConfigDto request, string createdBy);
    Task<PaymentGatewayConfigDto> UpdateGatewayConfigAsync(int configId, CreatePaymentGatewayConfigDto request, string updatedBy);
    Task<PaymentGatewayConfigDto> GetGatewayConfigAsync(int configId);
    Task<PaymentGatewayConfigDto?> GetGatewayConfigByTypeAsync(PaymentGatewayType gatewayType);
    Task<List<PaymentGatewayConfigDto>> GetActiveGatewayConfigsAsync();
    Task<List<PaymentGatewayConfigDto>> GetAllGatewayConfigsAsync();
    Task<bool> ActivateGatewayAsync(int configId, string activatedBy);
    Task<bool> DeactivateGatewayAsync(int configId, string reason, string deactivatedBy);
    Task<bool> TestGatewayConnectionAsync(int configId);
    
    // Mobile Money Providers
    Task<List<MobileMoneyProviderDto>> GetMobileMoneyProvidersAsync();
    Task<MobileMoneyProviderDto> GetMobileMoneyProviderAsync(int providerId);
    Task<MobileMoneyProviderDto?> GetProviderByCodeAsync(string providerCode);
    
    // Configuration Validation
    Task<bool> ValidateApiCredentialsAsync(int configId);
    Task<bool> ValidateWebhookConfigurationAsync(int configId);
    Task<List<string>> GetConfigurationIssuesAsync(int configId);
}

/// <summary>
/// Payment fraud detection and security service
/// </summary>
public interface IPaymentFraudDetectionService
{
    // Fraud Detection
    Task<SecurityRiskLevel> AnalyzeTransactionRiskAsync(CreatePaymentTransactionDto transaction, string ipAddress, string userAgent);
    Task<List<string>> GetRiskFactorsAsync(int transactionId);
    Task<bool> IsTransactionSuspiciousAsync(int transactionId);
    Task<bool> RequiresManualReviewAsync(int transactionId);
    Task<bool> BlockTransactionAsync(int transactionId, string reason, string blockedBy);
    Task<bool> FlagTransactionAsync(int transactionId, string reason, string flaggedBy);
    
    // Fraud Rules Management
    Task<PaymentFraudRuleDto> CreateFraudRuleAsync(CreatePaymentFraudRuleDto request, string createdBy);
    Task<PaymentFraudRuleDto> UpdateFraudRuleAsync(int ruleId, CreatePaymentFraudRuleDto request, string updatedBy);
    Task<PaymentFraudRuleDto> GetFraudRuleAsync(int ruleId);
    Task<List<PaymentFraudRuleDto>> GetActiveFraudRulesAsync();
    Task<List<PaymentFraudRuleDto>> GetAllFraudRulesAsync();
    Task<bool> ActivateFraudRuleAsync(int ruleId, string activatedBy);
    Task<bool> DeactivateFraudRuleAsync(int ruleId, string deactivatedBy);
    Task<bool> TestFraudRuleAsync(int ruleId, CreatePaymentTransactionDto testTransaction);
    
    // Security Monitoring
    Task<SecurityStatsDto> GetSecurityDashboardAsync(DateTime fromDate, DateTime toDate);
    Task<List<PaymentTransactionDto>> GetHighRiskTransactionsAsync(int page = 1, int pageSize = 20);
    Task<List<PaymentTransactionDto>> GetTransactionsRequiringReviewAsync(int page = 1, int pageSize = 20);
    Task<bool> ReviewTransactionAsync(int transactionId, bool approve, string reviewNotes, string reviewedBy);
    
    // Compliance and Reporting
    Task<byte[]> GenerateSecurityReportAsync(DateTime fromDate, DateTime toDate);
    Task<bool> LogSecurityEventAsync(int transactionId, string eventType, string details);
    Task<List<string>> GetSuspiciousActivityPatternsAsync(DateTime fromDate, DateTime toDate);
}

/// <summary>
/// Payment webhook handling service
/// </summary>
public interface IPaymentWebhookService
{
    // Webhook Processing
    Task<bool> ProcessWebhookAsync(int gatewayConfigId, string requestBody, Dictionary<string, string> headers, string ipAddress, string userAgent);
    Task<bool> ValidateWebhookSignatureAsync(int gatewayConfigId, string requestBody, string signature);
    Task<PaymentTransactionDto?> ProcessPaymentWebhookAsync(WebhookEventType eventType, string transactionReference, string webhookData);
    Task<bool> HandlePaymentCompletedWebhookAsync(string transactionReference, string externalReference, string webhookData);
    Task<bool> HandlePaymentFailedWebhookAsync(string transactionReference, string errorCode, string errorMessage);
    
    // Webhook Logs
    Task<PaymentWebhookLogDto> LogWebhookAsync(int gatewayConfigId, WebhookEventType eventType, string requestBody, Dictionary<string, string> headers, string ipAddress, string userAgent);
    Task<PaymentWebhookLogDto> GetWebhookLogAsync(int logId);
    Task<List<PaymentWebhookLogDto>> GetWebhookLogsAsync(PaymentWebhookSearchDto search);
    Task<bool> MarkWebhookProcessedAsync(int logId, bool success, string? errorMessage = null);
    Task<bool> ReprocessWebhookAsync(int logId, string reprocessedBy);
    
    // Webhook Management
    Task<bool> TestWebhookEndpointAsync(int gatewayConfigId);
    Task<string> GenerateWebhookSignatureAsync(int gatewayConfigId, string payload);
    Task<List<PaymentWebhookLogDto>> GetFailedWebhooksAsync(int? gatewayConfigId = null);
    Task<bool> RetryFailedWebhooksAsync(List<int> webhookLogIds, string retriedBy);
}

/// <summary>
/// Mobile money provider specific service
/// </summary>
public interface IMobileMoneyProviderService
{
    // Orange Money Integration
    Task<PaymentTransactionDto> ProcessOrangeMoneyPaymentAsync(int transactionId, string pin);
    Task<bool> ValidateOrangeMoneyAccountAsync(string phoneNumber);
    Task<decimal> GetOrangeMoneyBalanceAsync(string phoneNumber);
    Task<string> SendOrangeMoneyPaymentRequestAsync(int transactionId);
    Task<PaymentTransactionDto> CheckOrangeMoneyPaymentStatusAsync(string externalReference);
    
    // Africell Money Integration
    Task<PaymentTransactionDto> ProcessAfricellMoneyPaymentAsync(int transactionId, string pin);
    Task<bool> ValidateAfricellMoneyAccountAsync(string phoneNumber);
    Task<decimal> GetAfricellMoneyBalanceAsync(string phoneNumber);
    Task<string> SendAfricellMoneyPaymentRequestAsync(int transactionId);
    Task<PaymentTransactionDto> CheckAfricellMoneyPaymentStatusAsync(string externalReference);
    
    // Generic Mobile Money Operations
    Task<PaymentTransactionDto> ProcessMobileMoneyPaymentAsync(PaymentGatewayType gatewayType, int transactionId, string pin);
    Task<bool> ValidateMobileMoneyAccountAsync(PaymentGatewayType gatewayType, string phoneNumber);
    Task<string> SendMobileMoneyPaymentRequestAsync(PaymentGatewayType gatewayType, int transactionId);
    Task<PaymentTransactionDto> CheckMobileMoneyPaymentStatusAsync(PaymentGatewayType gatewayType, string externalReference);
    
    // Provider Management
    Task<List<MobileMoneyProviderDto>> GetAvailableProvidersAsync();
    Task<MobileMoneyProviderDto?> GetProviderForPhoneNumberAsync(string phoneNumber);
    Task<bool> IsProviderAvailableAsync(PaymentGatewayType gatewayType);
    Task<decimal> GetProviderTransactionLimitAsync(PaymentGatewayType gatewayType);
    Task<bool> ValidatePhoneNumberAsync(string phoneNumber);
    Task<string> GetProviderStatusAsync(PaymentGatewayType gatewayType);
}

/// <summary>
/// Payment encryption and security service
/// </summary>
public interface IPaymentEncryptionService
{
    // Data Encryption
    Task<string> EncryptSensitiveDataAsync(string data);
    Task<string> DecryptSensitiveDataAsync(string encryptedData);
    Task<string> EncryptPaymentPinAsync(string pin);
    Task<bool> VerifyPaymentPinAsync(string pin, string encryptedPin);
    
    // API Key Management
    Task<string> EncryptApiKeyAsync(string apiKey);
    Task<string> DecryptApiKeyAsync(string encryptedApiKey);
    Task<string> GenerateApiKeyAsync(int length = 32);
    Task<string> HashApiKeyAsync(string apiKey);
    
    // Token Management
    Task<string> GenerateSecureTokenAsync(int length = 16);
    Task<string> GenerateTransactionReferenceAsync();
    Task<string> GenerateWebhookSecretAsync();
    Task<bool> ValidateTokenFormatAsync(string token);
    
    // Digital Signatures
    Task<string> SignWebhookPayloadAsync(string payload, string secret);
    Task<bool> VerifyWebhookSignatureAsync(string payload, string signature, string secret);
    Task<string> GenerateTransactionHashAsync(int transactionId, decimal amount, string phoneNumber);
    
    // Certificate Management
    Task<bool> ValidateSslCertificateAsync(string url);
    Task<DateTime?> GetCertificateExpiryAsync(string url);
    Task<bool> IsCertificateValidAsync(string url);
}