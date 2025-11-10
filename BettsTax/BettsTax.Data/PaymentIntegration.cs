using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using BettsTax.Shared; // metrics
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace BettsTax.Data
{
    public enum PaymentProvider
    {
        // Mobile Money Providers
        OrangeMoney,
        AfricellMoney,
    SalonePaymentSwitch,
        
        // Local Banks
        SierraLeoneCommercialBank,
        RoyalBankSL,
        FirstBankSL,
        UnionTrustBank,
        AccessBankSL,
        
        // International
        PayPal,
        Stripe,
        
        // Traditional
        BankTransfer,
        Cash,
        Cheque
    }

    public enum PaymentTransactionStatus
    {
        Initiated,
        Pending,
        Processing,
        Completed,
        Failed,
        Cancelled,
        Expired,
        Refunded,
        PartialRefund
    }

    public enum MobileMoneyOperatorCode
    {
        Orange = 76, // Orange SL prefix
        Africell = 77 // Africell SL prefix (77, 78, 79)
    }

    // Enhanced Payment entity to support integrated payments
    public class PaymentTransaction
    {
        public int PaymentTransactionId { get; set; }
        
        // Link to existing payment
        public int PaymentId { get; set; }
        
        // Provider and transaction details
        public PaymentProvider Provider { get; set; }
        public PaymentTransactionStatus Status { get; set; } = PaymentTransactionStatus.Initiated;
        
        [Required]
        [MaxLength(100)]
        public string TransactionReference { get; set; } = string.Empty;
        
        [MaxLength(100)]
        public string? ProviderTransactionId { get; set; }
        
        [MaxLength(100)]
        public string? ProviderReference { get; set; }
        
        // Amount and currency
        [Column(TypeName = "decimal(18,2)")]
        public decimal Amount { get; set; }
        
        [MaxLength(3)]
        public string Currency { get; set; } = "SLE"; // Sierra Leone Leone
        
        [Column(TypeName = "decimal(18,4)")]
        public decimal? ExchangeRate { get; set; }
        
        [Column(TypeName = "decimal(18,2)")]
        public decimal? ProviderFee { get; set; }
        
        [Column(TypeName = "decimal(18,2)")]
        public decimal? NetAmount { get; set; }
        
        // Customer details for mobile money
        [MaxLength(20)]
        public string? CustomerPhone { get; set; }
        
        [MaxLength(100)]
        public string? CustomerName { get; set; }
        
        [MaxLength(50)]
        public string? CustomerAccountNumber { get; set; }
        
        // Bank transfer details
        [MaxLength(50)]
        public string? BankCode { get; set; }
        
        [MaxLength(100)]
        public string? BankName { get; set; }
        
        [MaxLength(50)]
        public string? BankAccountNumber { get; set; }
        
        [MaxLength(100)]
        public string? BankAccountName { get; set; }
        
        // Transaction metadata
        [MaxLength(2000)]
        public string? ProviderResponse { get; set; }
        
        [MaxLength(500)]
        public string? FailureReason { get; set; }
        
        [MaxLength(1000)]
        public string? Metadata { get; set; } // JSON for additional data
        
        // Timestamps
        public DateTime CreatedDate { get; set; } = DateTime.UtcNow;
        public DateTime? InitiatedDate { get; set; }
        public DateTime? CompletedDate { get; set; }
        public DateTime? FailedDate { get; set; }
        public DateTime? ExpiryDate { get; set; }
        
        // Retry and webhook handling
        public int RetryCount { get; set; } = 0;
        public int MaxRetries { get; set; } = 3;
        public DateTime? NextRetryDate { get; set; }
        public DateTime? LastWebhookDate { get; set; }
        
        // Navigation properties
        public Payment? Payment { get; set; }
        public List<PaymentWebhookLog> WebhookLogs { get; set; } = new();
    }

    // Payment provider configurations
    public class PaymentProviderConfig
    {
        public int PaymentProviderConfigId { get; set; }
        
        public PaymentProvider Provider { get; set; }
        
        [Required]
        [MaxLength(100)]
        public string Name { get; set; } = string.Empty;
        
        [MaxLength(200)]
        public string? Description { get; set; }
        
        [MaxLength(200)]
        public string? ApiUrl { get; set; }
        
        [MaxLength(100)]
        public string? ApiKey { get; set; }
        
        [MaxLength(100)]
        public string? ApiSecret { get; set; }
        
        [MaxLength(50)]
        public string? MerchantId { get; set; }
        
        [MaxLength(100)]
        public string? WebhookSecret { get; set; }
        
        [MaxLength(200)]
        public string? WebhookUrl { get; set; }
        
        // Configuration as JSON
        [MaxLength(2000)]
        public string? AdditionalSettings { get; set; }
        
        // Fees and limits
        [Column(TypeName = "decimal(5,4)")]
        public decimal FeePercentage { get; set; } = 0;
        
        [Column(TypeName = "decimal(18,2)")]
        public decimal FixedFee { get; set; } = 0;
        
        [Column(TypeName = "decimal(18,2)")]
        public decimal? MinAmount { get; set; }
        
        [Column(TypeName = "decimal(18,2)")]
        public decimal? MaxAmount { get; set; }
        
        [Column(TypeName = "decimal(18,2)")]
        public decimal? DailyLimit { get; set; }
        
        [Column(TypeName = "decimal(18,2)")]
        public decimal? MonthlyLimit { get; set; }
        
        // Status and priority
        public bool IsActive { get; set; } = true;
        public bool IsTestMode { get; set; } = true;
        public int Priority { get; set; } = 1; // Lower number = higher priority
        
        [MaxLength(3)]
        public string SupportedCurrency { get; set; } = "SLE";
        
        // Usage tracking
        [Column(TypeName = "decimal(18,2)")]
        public decimal DailyUsage { get; set; } = 0;
        
        [Column(TypeName = "decimal(18,2)")]
        public decimal MonthlyUsage { get; set; } = 0;
        
        public DateTime? LastUsedDate { get; set; }
        
        // Audit
        public DateTime CreatedDate { get; set; } = DateTime.UtcNow;
        public DateTime? UpdatedDate { get; set; }
        
        public List<PaymentTransaction> Transactions { get; set; } = new();
    }

    // Webhook logging for payment callbacks
    public class PaymentWebhookLog
    {
        public int PaymentWebhookLogId { get; set; }
        
        public int PaymentTransactionId { get; set; }
        
        public PaymentProvider Provider { get; set; }
        
        [MaxLength(50)]
        public string WebhookType { get; set; } = string.Empty; // payment.completed, payment.failed, etc.
        
        [MaxLength(100)]
        public string? IdempotencyKey { get; set; }
        
        [MaxLength(5000)]
        public string RequestBody { get; set; } = string.Empty;
        
        [MaxLength(1000)]
        public string RequestHeaders { get; set; } = string.Empty;
        
        [MaxLength(500)]
        public string? ResponseStatus { get; set; }
        
        [MaxLength(2000)]
        public string? ProcessingResult { get; set; }
        
        public bool IsProcessed { get; set; } = false;
        public DateTime ReceivedDate { get; set; } = DateTime.UtcNow;
        public DateTime? ProcessedDate { get; set; }

    [MaxLength(128)]
    public string? WebhookHash { get; set; } // SHA256 hash for idempotency
        
        [MaxLength(39)] // IPv6 max length
        public string? IpAddress { get; set; }
        
        [MaxLength(500)]
        public string? UserAgent { get; set; }
        
        // Navigation properties
        public PaymentTransaction? PaymentTransaction { get; set; }
    }

    // Payment method configurations for different regions
    public class PaymentMethodConfig
    {
        public int PaymentMethodConfigId { get; set; }
        
        [Required]
        [MaxLength(50)]
        public string Name { get; set; } = string.Empty;
        
        [MaxLength(200)]
        public string Description { get; set; } = string.Empty;
        
        public PaymentProvider Provider { get; set; }
        
        [MaxLength(100)]
        public string? IconUrl { get; set; }
        
        [MaxLength(7)] // #FFFFFF
        public string? BrandColor { get; set; }
        
        // Regional availability
        [MaxLength(2)]
        public string CountryCode { get; set; } = "SL"; // Sierra Leone
        
        [MaxLength(3)]
        public string Currency { get; set; } = "SLE";
        
        // UI configuration
        public int DisplayOrder { get; set; } = 1;
        public bool IsVisible { get; set; } = true;
        public bool IsEnabled { get; set; } = true;
        public bool RequiresPhone { get; set; } = false;
        public bool RequiresAccount { get; set; } = false;
        
        // Limits
        [Column(TypeName = "decimal(18,2)")]
        public decimal? MinAmount { get; set; }
        
        [Column(TypeName = "decimal(18,2)")]
        public decimal? MaxAmount { get; set; }
        
        // Target audience
        public bool AvailableForClients { get; set; } = true;
        public bool AvailableForDiaspora { get; set; } = false;
        
        public DateTime CreatedDate { get; set; } = DateTime.UtcNow;
    }

    // Payment gateway responses and status mapping
    public class PaymentStatusMapping
    {
        public int PaymentStatusMappingId { get; set; }
        
        public PaymentProvider Provider { get; set; }
        
        [MaxLength(100)]
        public string ProviderStatus { get; set; } = string.Empty;
        
        [MaxLength(200)]
        public string? ProviderMessage { get; set; }
        
        public PaymentTransactionStatus MappedStatus { get; set; }
        
        public bool IsSuccess { get; set; }
        public bool IsFinal { get; set; } // If this status is final (no more updates expected)
        
        [MaxLength(500)]
        public string? Description { get; set; }
        
        public DateTime CreatedDate { get; set; } = DateTime.UtcNow;
    }
    // Processor to handle webhook requests with idempotency
    public class PaymentWebhookProcessor
    {
        private readonly ApplicationDbContext _db;
        private readonly ILogger<PaymentWebhookProcessor> _logger;

        public PaymentWebhookProcessor(ApplicationDbContext db, ILogger<PaymentWebhookProcessor> logger)
        {
            _db = db;
            _logger = logger;
        }

        public async Task<bool> ProcessAsync(string provider, string requestBody, IDictionary<string, string?> headers, CancellationToken ct = default)
        {
            // Deterministic hash for idempotency: provider + body + sorted headers subset
            var normalizedHeaders = string.Join("|", headers
                .OrderBy(k => k.Key, StringComparer.OrdinalIgnoreCase)
                .Select(k => $"{k.Key}:{k.Value}"));
            var raw = provider + "\n" + requestBody + "\n" + normalizedHeaders;
            var hash = ComputeSha256(raw);

            var existing = await _db.PaymentWebhookLogs
                .AsNoTracking()
                .Where(w => w.WebhookHash == hash && w.Provider.ToString() == provider)
                .Select(w => new { w.PaymentWebhookLogId, w.IsProcessed })
                .FirstOrDefaultAsync(ct);

            if (existing != null && existing.IsProcessed)
            {
                _logger.LogInformation("Duplicate processed webhook ignored. Provider={Provider} Hash={Hash}", provider, hash);
                PaymentMetrics.WebhooksDuplicate.Add(1);
                return true; // idempotent success
            }

            var log = new PaymentWebhookLog
            {
                Provider = Enum.TryParse<PaymentProvider>(provider, out var parsed) ? parsed : PaymentProvider.OrangeMoney,
                RequestBody = requestBody,
                RequestHeaders = System.Text.Json.JsonSerializer.Serialize(headers),
                WebhookHash = hash,
                PaymentTransactionId = 0 // will be linked after parsing if possible
            };

            _db.PaymentWebhookLogs.Add(log);
            await _db.SaveChangesAsync(ct);

            try
            {
                // Parse and update transaction for supported providers
                if (Enum.TryParse<PaymentProvider>(provider, out var prov))
                {
                    if (prov == PaymentProvider.SalonePaymentSwitch)
                    {
                        // Basic pain.002 parsing for TxSts and EndToEndId
                        try
                        {
                            var x = System.Xml.Linq.XDocument.Parse(requestBody);
                            var refId = x.Descendants().FirstOrDefault(e => e.Name.LocalName == "EndToEndId")?.Value;
                            var sts = x.Descendants().FirstOrDefault(e => e.Name.LocalName == "TxSts")?.Value ?? "PENDING";
                            if (!string.IsNullOrEmpty(refId))
                            {
                                var txn = await _db.PaymentTransactions.FirstOrDefaultAsync(t => t.TransactionReference == refId, ct);
                                if (txn != null)
                                {
                                    var mapped = await _db.PaymentStatusMappings.AsNoTracking()
                                        .Where(m => m.Provider == PaymentProvider.SalonePaymentSwitch && m.ProviderStatus == sts)
                                        .Select(m => m.MappedStatus)
                                        .FirstOrDefaultAsync(ct);
                                    if (mapped == default && sts == "PENDING") mapped = PaymentTransactionStatus.Pending; // fallback
                                    if (mapped == default && sts == "PDNG") mapped = PaymentTransactionStatus.Pending;
                                    if (mapped == default && (sts == "ACSC" || sts == "COMPLETED")) mapped = PaymentTransactionStatus.Completed;
                                    if (mapped == default && (sts == "RJCT" || sts == "FAILED")) mapped = PaymentTransactionStatus.Failed;
                                    if (mapped == default) mapped = PaymentTransactionStatus.Processing;
                                    txn.Status = mapped;
                                    txn.LastWebhookDate = DateTime.UtcNow;
                                    await _db.SaveChangesAsync(ct);
                                    log.PaymentTransactionId = txn.PaymentTransactionId;
                                }
                            }
                        }
                        catch (Exception parseEx)
                        {
                            _logger.LogWarning(parseEx, "Failed parsing Salone switch webhook");
                        }
                    }
                }
                log.IsProcessed = true;
                log.ProcessedDate = DateTime.UtcNow;
                log.ProcessingResult = "OK";
                await _db.SaveChangesAsync(ct);
                PaymentMetrics.WebhooksProcessed.Add(1);
                return true;
            }
            catch (Exception ex)
            {
                log.ProcessingResult = ex.Message;
                await _db.SaveChangesAsync(ct);
                _logger.LogError(ex, "Failed processing webhook {WebhookId}", log.PaymentWebhookLogId);
                return false;
            }
        }

        private static string ComputeSha256(string input)
        {
            using var sha = SHA256.Create();
            var bytes = sha.ComputeHash(Encoding.UTF8.GetBytes(input));
            var sb = new StringBuilder(bytes.Length * 2);
            foreach (var b in bytes)
                sb.Append(b.ToString("x2"));
            return sb.ToString();
        }
    }
}