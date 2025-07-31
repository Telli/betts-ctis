using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using BettsTax.Data;
using BettsTax.Data.Models;
using PaymentTransactionStatus = BettsTax.Data.Models.PaymentTransactionStatus;
using BettsTax.Core.DTOs.Payment;
using BettsTax.Core.Services.Interfaces;

namespace BettsTax.Core.Services;

/// <summary>
/// Advanced fraud detection service for Sierra Leone mobile money payments
/// Implements risk analysis, rule-based detection, and security monitoring
/// </summary>
public class PaymentFraudDetectionService : IPaymentFraudDetectionService
{
    private readonly ApplicationDbContext _context;
    private readonly ILogger<PaymentFraudDetectionService> _logger;

    public PaymentFraudDetectionService(
        ApplicationDbContext context,
        ILogger<PaymentFraudDetectionService> logger)
    {
        _context = context;
        _logger = logger;
    }

    #region Fraud Detection

    public async Task<SecurityRiskLevel> AnalyzeTransactionRiskAsync(
        CreatePaymentTransactionDto transaction, 
        string ipAddress, 
        string userAgent)
    {
        try
        {
            var riskFactors = new List<string>();
            var riskScore = 0;

            // Get active fraud rules
            var activeRules = await _context.PaymentFraudRules
                .Where(r => r.IsActive)
                .OrderByDescending(r => r.Priority)
                .ToListAsync();

            // Analyze transaction amount patterns
            riskScore += await AnalyzeAmountRiskAsync(transaction.Amount, transaction.ClientId, riskFactors);

            // Analyze frequency patterns
            riskScore += await AnalyzeFrequencyRiskAsync(transaction.ClientId, transaction.PayerPhone, riskFactors);

            // Analyze geographic/IP patterns
            riskScore += await AnalyzeLocationRiskAsync(ipAddress, userAgent, riskFactors);

            // Analyze time patterns
            riskScore += await AnalyzeTimeRiskAsync(transaction.ClientId, riskFactors);

            // Apply custom fraud rules
            foreach (var rule in activeRules)
            {
                var ruleResult = await EvaluateFraudRuleAsync(rule, transaction, ipAddress, userAgent);
                if (ruleResult.IsTriggered)
                {
                    riskScore += GetRiskScoreForLevel(rule.RiskLevel);
                    riskFactors.AddRange(ruleResult.Factors);
                    
                    // Update rule statistics
                    rule.TriggerCount++;
                    rule.LastTriggered = DateTime.UtcNow;
                }
            }

            var riskLevel = DetermineRiskLevel(riskScore);
            
            _logger.LogInformation(
                "Transaction risk analysis completed. ClientId: {ClientId}, Amount: {Amount}, " +
                "RiskScore: {RiskScore}, RiskLevel: {RiskLevel}, Factors: {FactorCount}",
                transaction.ClientId, transaction.Amount, riskScore, riskLevel, riskFactors.Count);

            await _context.SaveChangesAsync();
            return riskLevel;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to analyze transaction risk for client {ClientId}", transaction.ClientId);
            return SecurityRiskLevel.Medium; // Default to medium risk on error
        }
    }

    public async Task<List<string>> GetRiskFactorsAsync(int transactionId)
    {
        try
        {
            var transaction = await _context.PaymentGatewayTransactions
                .FirstOrDefaultAsync(t => t.Id == transactionId);

            if (transaction == null)
                return new List<string>();

            var riskFactors = string.IsNullOrEmpty(transaction.RiskFactors) 
                ? new List<string>() 
                : JsonSerializer.Deserialize<List<string>>(transaction.RiskFactors) ?? new List<string>();

            _logger.LogDebug("Retrieved {Count} risk factors for transaction {TransactionId}", 
                riskFactors.Count, transactionId);

            return riskFactors;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get risk factors for transaction {TransactionId}", transactionId);
            return new List<string>();
        }
    }

    public async Task<bool> IsTransactionSuspiciousAsync(int transactionId)
    {
        try
        {
            var transaction = await _context.PaymentGatewayTransactions
                .FirstOrDefaultAsync(t => t.Id == transactionId);

            if (transaction == null)
                return false;

            var isSuspicious = transaction.RiskLevel >= SecurityRiskLevel.High ||
                              transaction.RequiresManualReview;

            _logger.LogDebug("Transaction {TransactionId} suspicious check: {IsSuspicious}", 
                transactionId, isSuspicious);

            return isSuspicious;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to check if transaction {TransactionId} is suspicious", transactionId);
            return true; // Default to suspicious on error for safety
        }
    }

    public async Task<bool> RequiresManualReviewAsync(int transactionId)
    {
        try
        {
            var transaction = await _context.PaymentGatewayTransactions
                .FirstOrDefaultAsync(t => t.Id == transactionId);

            return transaction?.RequiresManualReview ?? false;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to check manual review requirement for transaction {TransactionId}", transactionId);
            return true; // Default to requiring review on error
        }
    }

    public async Task<bool> BlockTransactionAsync(int transactionId, string reason, string blockedBy)
    {
        try
        {
            var transaction = await _context.PaymentGatewayTransactions
                .FirstOrDefaultAsync(t => t.Id == transactionId);

            if (transaction == null)
                return false;

            transaction.Status = PaymentTransactionStatus.Cancelled;
            transaction.StatusMessage = $"Blocked: {reason}";
            transaction.RequiresManualReview = true;

            // Log the blocking action
            var log = new PaymentTransactionLog
            {
                TransactionId = transactionId,
                Action = "BLOCKED",
                PreviousStatus = transaction.Status,
                NewStatus = PaymentTransactionStatus.Cancelled,
                Details = $"Transaction blocked by {blockedBy}. Reason: {reason}",
                CreatedAt = DateTime.UtcNow
            };

            _context.PaymentTransactionLogs.Add(log);
            await _context.SaveChangesAsync();

            _logger.LogWarning("Transaction {TransactionId} blocked by {BlockedBy}. Reason: {Reason}", 
                transactionId, blockedBy, reason);

            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to block transaction {TransactionId}", transactionId);
            return false;
        }
    }

    public async Task<bool> FlagTransactionAsync(int transactionId, string reason, string flaggedBy)
    {
        try
        {
            var transaction = await _context.PaymentGatewayTransactions
                .FirstOrDefaultAsync(t => t.Id == transactionId);

            if (transaction == null)
                return false;

            transaction.RequiresManualReview = true;
            
            var riskFactors = await GetRiskFactorsAsync(transactionId);
            riskFactors.Add($"Flagged by {flaggedBy}: {reason}");
            transaction.RiskFactors = JsonSerializer.Serialize(riskFactors);

            // Log the flagging action
            var log = new PaymentTransactionLog
            {
                TransactionId = transactionId,
                Action = "FLAGGED",
                PreviousStatus = transaction.Status,
                NewStatus = transaction.Status,
                Details = $"Transaction flagged by {flaggedBy}. Reason: {reason}",
                CreatedAt = DateTime.UtcNow
            };

            _context.PaymentTransactionLogs.Add(log);
            await _context.SaveChangesAsync();

            _logger.LogWarning("Transaction {TransactionId} flagged by {FlaggedBy}. Reason: {Reason}", 
                transactionId, flaggedBy, reason);

            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to flag transaction {TransactionId}", transactionId);
            return false;
        }
    }

    #endregion

    #region Fraud Rules Management

    public async Task<PaymentFraudRuleDto> CreateFraudRuleAsync(CreatePaymentFraudRuleDto request, string createdBy)
    {
        try
        {
            var fraudRule = new PaymentFraudRule
            {
                RuleName = request.RuleName,
                Description = request.Description,
                IsActive = request.IsActive,
                Conditions = request.Conditions,
                Action = request.Action,
                RiskLevel = request.RiskLevel,
                Priority = request.Priority,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow,
                CreatedBy = createdBy
            };

            _context.PaymentFraudRules.Add(fraudRule);
            await _context.SaveChangesAsync();

            _logger.LogInformation("Created fraud rule {RuleName} by {CreatedBy}", request.RuleName, createdBy);

            return new PaymentFraudRuleDto
            {
                Id = fraudRule.Id,
                RuleName = fraudRule.RuleName,
                Description = fraudRule.Description,
                IsActive = fraudRule.IsActive,
                Conditions = fraudRule.Conditions,
                Action = fraudRule.Action,
                RiskLevel = fraudRule.RiskLevel,
                RiskLevelName = fraudRule.RiskLevel.ToString(),
                Priority = fraudRule.Priority,
                TriggerCount = fraudRule.TriggerCount,
                LastTriggered = fraudRule.LastTriggered,
                CreatedAt = fraudRule.CreatedAt,
                UpdatedAt = fraudRule.UpdatedAt,
                CreatedBy = fraudRule.CreatedBy
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to create fraud rule {RuleName}", request.RuleName);
            throw new InvalidOperationException("Failed to create fraud rule", ex);
        }
    }

    public async Task<PaymentFraudRuleDto> UpdateFraudRuleAsync(int ruleId, CreatePaymentFraudRuleDto request, string updatedBy)
    {
        try
        {
            var fraudRule = await _context.PaymentFraudRules
                .FirstOrDefaultAsync(r => r.Id == ruleId);

            if (fraudRule == null)
                throw new InvalidOperationException($"Fraud rule with ID {ruleId} not found");

            fraudRule.RuleName = request.RuleName;
            fraudRule.Description = request.Description;
            fraudRule.IsActive = request.IsActive;
            fraudRule.Conditions = request.Conditions;
            fraudRule.Action = request.Action;
            fraudRule.RiskLevel = request.RiskLevel;
            fraudRule.Priority = request.Priority;
            fraudRule.UpdatedAt = DateTime.UtcNow;
            fraudRule.UpdatedBy = updatedBy;

            await _context.SaveChangesAsync();

            _logger.LogInformation("Updated fraud rule {RuleName} (ID: {RuleId}) by {UpdatedBy}", 
                request.RuleName, ruleId, updatedBy);

            return new PaymentFraudRuleDto
            {
                Id = fraudRule.Id,
                RuleName = fraudRule.RuleName,
                Description = fraudRule.Description,
                IsActive = fraudRule.IsActive,
                Conditions = fraudRule.Conditions,
                Action = fraudRule.Action,
                RiskLevel = fraudRule.RiskLevel,
                RiskLevelName = fraudRule.RiskLevel.ToString(),
                Priority = fraudRule.Priority,
                TriggerCount = fraudRule.TriggerCount,
                LastTriggered = fraudRule.LastTriggered,
                CreatedAt = fraudRule.CreatedAt,
                UpdatedAt = fraudRule.UpdatedAt,
                CreatedBy = fraudRule.CreatedBy,
                UpdatedBy = fraudRule.UpdatedBy
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to update fraud rule {RuleId}", ruleId);
            throw new InvalidOperationException("Failed to update fraud rule", ex);
        }
    }

    public async Task<PaymentFraudRuleDto> GetFraudRuleAsync(int ruleId)
    {
        try
        {
            var fraudRule = await _context.PaymentFraudRules
                .FirstOrDefaultAsync(r => r.Id == ruleId);

            if (fraudRule == null)
                throw new InvalidOperationException($"Fraud rule with ID {ruleId} not found");

            return new PaymentFraudRuleDto
            {
                Id = fraudRule.Id,
                RuleName = fraudRule.RuleName,
                Description = fraudRule.Description,
                IsActive = fraudRule.IsActive,
                Conditions = fraudRule.Conditions,
                Action = fraudRule.Action,
                RiskLevel = fraudRule.RiskLevel,
                RiskLevelName = fraudRule.RiskLevel.ToString(),
                Priority = fraudRule.Priority,
                TriggerCount = fraudRule.TriggerCount,
                LastTriggered = fraudRule.LastTriggered,
                CreatedAt = fraudRule.CreatedAt,
                UpdatedAt = fraudRule.UpdatedAt,
                CreatedBy = fraudRule.CreatedBy,
                UpdatedBy = fraudRule.UpdatedBy
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get fraud rule {RuleId}", ruleId);
            throw new InvalidOperationException("Failed to retrieve fraud rule", ex);
        }
    }

    public async Task<List<PaymentFraudRuleDto>> GetActiveFraudRulesAsync()
    {
        try
        {
            var rules = await _context.PaymentFraudRules
                .Where(r => r.IsActive)
                .OrderByDescending(r => r.Priority)
                .Select(r => new PaymentFraudRuleDto
                {
                    Id = r.Id,
                    RuleName = r.RuleName,
                    Description = r.Description,
                    IsActive = r.IsActive,
                    Conditions = r.Conditions,
                    Action = r.Action,
                    RiskLevel = r.RiskLevel,
                    RiskLevelName = r.RiskLevel.ToString(),
                    Priority = r.Priority,
                    TriggerCount = r.TriggerCount,
                    LastTriggered = r.LastTriggered,
                    CreatedAt = r.CreatedAt,
                    UpdatedAt = r.UpdatedAt,
                    CreatedBy = r.CreatedBy,
                    UpdatedBy = r.UpdatedBy
                })
                .ToListAsync();

            _logger.LogDebug("Retrieved {Count} active fraud rules", rules.Count);
            return rules;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get active fraud rules");
            throw new InvalidOperationException("Failed to retrieve active fraud rules", ex);
        }
    }

    public async Task<List<PaymentFraudRuleDto>> GetAllFraudRulesAsync()
    {
        try
        {
            var rules = await _context.PaymentFraudRules
                .OrderByDescending(r => r.CreatedAt)
                .Select(r => new PaymentFraudRuleDto
                {
                    Id = r.Id,
                    RuleName = r.RuleName,
                    Description = r.Description,
                    IsActive = r.IsActive,
                    Conditions = r.Conditions,
                    Action = r.Action,
                    RiskLevel = r.RiskLevel,
                    RiskLevelName = r.RiskLevel.ToString(),
                    Priority = r.Priority,
                    TriggerCount = r.TriggerCount,
                    LastTriggered = r.LastTriggered,
                    CreatedAt = r.CreatedAt,
                    UpdatedAt = r.UpdatedAt,
                    CreatedBy = r.CreatedBy,
                    UpdatedBy = r.UpdatedBy
                })
                .ToListAsync();

            _logger.LogDebug("Retrieved {Count} total fraud rules", rules.Count);
            return rules;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get all fraud rules");
            throw new InvalidOperationException("Failed to retrieve fraud rules", ex);
        }
    }

    public async Task<bool> ActivateFraudRuleAsync(int ruleId, string activatedBy)
    {
        try
        {
            var rule = await _context.PaymentFraudRules
                .FirstOrDefaultAsync(r => r.Id == ruleId);

            if (rule == null)
                return false;

            rule.IsActive = true;
            rule.UpdatedAt = DateTime.UtcNow;
            rule.UpdatedBy = activatedBy;

            await _context.SaveChangesAsync();

            _logger.LogInformation("Activated fraud rule {RuleName} (ID: {RuleId}) by {ActivatedBy}", 
                rule.RuleName, ruleId, activatedBy);

            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to activate fraud rule {RuleId}", ruleId);
            return false;
        }
    }

    public async Task<bool> DeactivateFraudRuleAsync(int ruleId, string deactivatedBy)
    {
        try
        {
            var rule = await _context.PaymentFraudRules
                .FirstOrDefaultAsync(r => r.Id == ruleId);

            if (rule == null)
                return false;

            rule.IsActive = false;
            rule.UpdatedAt = DateTime.UtcNow;
            rule.UpdatedBy = deactivatedBy;

            await _context.SaveChangesAsync();

            _logger.LogInformation("Deactivated fraud rule {RuleName} (ID: {RuleId}) by {DeactivatedBy}", 
                rule.RuleName, ruleId, deactivatedBy);

            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to deactivate fraud rule {RuleId}", ruleId);
            return false;
        }
    }

    public async Task<bool> TestFraudRuleAsync(int ruleId, CreatePaymentTransactionDto testTransaction)
    {
        try
        {
            var rule = await _context.PaymentFraudRules
                .FirstOrDefaultAsync(r => r.Id == ruleId);

            if (rule == null)
                return false;

            var result = await EvaluateFraudRuleAsync(rule, testTransaction, "127.0.0.1", "Test Agent");
            
            _logger.LogInformation("Fraud rule {RuleName} test result: {IsTriggered}", 
                rule.RuleName, result.IsTriggered);

            return result.IsTriggered;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to test fraud rule {RuleId}", ruleId);
            return false;
        }
    }

    #endregion

    #region Security Monitoring

    public async Task<SecurityStatsDto> GetSecurityDashboardAsync(DateTime fromDate, DateTime toDate)
    {
        try
        {
            var transactions = await _context.PaymentGatewayTransactions
                .Where(t => t.InitiatedAt >= fromDate && t.InitiatedAt <= toDate)
                .ToListAsync();

            var stats = new SecurityStatsDto
            {
                TotalSecurityIncidents = transactions.Count(t => t.RiskLevel >= SecurityRiskLevel.High),
                HighRiskTransactions = transactions.Count(t => t.RiskLevel == SecurityRiskLevel.High),
                BlockedTransactions = transactions.Count(t => t.Status == PaymentTransactionStatus.Cancelled),
                ManualReviewRequired = transactions.Count(t => t.RequiresManualReview),
                FraudRuleTriggered = transactions.Count(t => !string.IsNullOrEmpty(t.RiskFactors)),
                RiskLevelDistribution = transactions
                    .GroupBy(t => t.RiskLevel)
                    .ToDictionary(g => g.Key, g => g.Count()),
                TopRiskFactors = await GetTopRiskFactorsAsync(fromDate, toDate)
            };

            _logger.LogDebug("Generated security dashboard stats for period {FromDate} to {ToDate}", 
                fromDate, toDate);

            return stats;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get security dashboard stats");
            throw new InvalidOperationException("Failed to retrieve security statistics", ex);
        }
    }

    public async Task<List<PaymentTransactionDto>> GetHighRiskTransactionsAsync(int page = 1, int pageSize = 20)
    {
        try
        {
            var transactions = await _context.PaymentGatewayTransactions
                .Where(t => t.RiskLevel >= SecurityRiskLevel.High)
                .OrderByDescending(t => t.InitiatedAt)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .Select(t => new PaymentTransactionDto
                {
                    Id = t.Id,
                    TransactionReference = t.TransactionReference,
                    ExternalReference = t.ExternalReference,
                    ClientId = t.ClientId,
                    GatewayType = t.GatewayType,
                    Amount = t.Amount,
                    PayerPhone = t.PayerPhone,
                    Status = t.Status,
                    RiskLevel = t.RiskLevel,
                    RequiresManualReview = t.RequiresManualReview,
                    InitiatedAt = t.InitiatedAt
                })
                .ToListAsync();

            _logger.LogDebug("Retrieved {Count} high-risk transactions for page {Page}", 
                transactions.Count, page);

            return transactions;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get high-risk transactions");
            throw new InvalidOperationException("Failed to retrieve high-risk transactions", ex);
        }
    }

    public async Task<List<PaymentTransactionDto>> GetTransactionsRequiringReviewAsync(int page = 1, int pageSize = 20)
    {
        try
        {
            var transactions = await _context.PaymentGatewayTransactions
                .Where(t => t.RequiresManualReview && t.ReviewedBy == null)
                .OrderByDescending(t => t.InitiatedAt)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .Select(t => new PaymentTransactionDto
                {
                    Id = t.Id,
                    TransactionReference = t.TransactionReference,
                    ExternalReference = t.ExternalReference,
                    ClientId = t.ClientId,
                    GatewayType = t.GatewayType,
                    Amount = t.Amount,
                    PayerPhone = t.PayerPhone,
                    Status = t.Status,
                    RiskLevel = t.RiskLevel,
                    RequiresManualReview = t.RequiresManualReview,
                    InitiatedAt = t.InitiatedAt
                })
                .ToListAsync();

            _logger.LogDebug("Retrieved {Count} transactions requiring review for page {Page}", 
                transactions.Count, page);

            return transactions;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get transactions requiring review");
            throw new InvalidOperationException("Failed to retrieve transactions requiring review", ex);
        }
    }

    public async Task<bool> ReviewTransactionAsync(int transactionId, bool approve, string reviewNotes, string reviewedBy)
    {
        try
        {
            var transaction = await _context.PaymentGatewayTransactions
                .FirstOrDefaultAsync(t => t.Id == transactionId);

            if (transaction == null)
                return false;

            transaction.RequiresManualReview = false;
            transaction.ReviewedBy = reviewedBy;
            transaction.ReviewedAt = DateTime.UtcNow;

            if (!approve)
            {
                transaction.Status = PaymentTransactionStatus.Cancelled;
                transaction.StatusMessage = $"Manual review rejected: {reviewNotes}";
            }

            // Log the review action
            var log = new PaymentTransactionLog
            {
                TransactionId = transactionId,
                Action = approve ? "APPROVED" : "REJECTED",
                PreviousStatus = transaction.Status,
                NewStatus = transaction.Status,
                Details = $"Manual review by {reviewedBy}. Decision: {(approve ? "Approved" : "Rejected")}. Notes: {reviewNotes}",
                CreatedAt = DateTime.UtcNow
            };

            _context.PaymentTransactionLogs.Add(log);
            await _context.SaveChangesAsync();

            _logger.LogInformation("Transaction {TransactionId} reviewed by {ReviewedBy}. Decision: {Decision}", 
                transactionId, reviewedBy, approve ? "Approved" : "Rejected");

            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to review transaction {TransactionId}", transactionId);
            return false;
        }
    }

    #endregion

    #region Compliance and Reporting

    public async Task<byte[]> GenerateSecurityReportAsync(DateTime fromDate, DateTime toDate)
    {
        try
        {
            var stats = await GetSecurityDashboardAsync(fromDate, toDate);
            
            // This is a simplified implementation
            // In production, you would use a proper report generator
            var reportContent = JsonSerializer.Serialize(stats, new JsonSerializerOptions 
            { 
                WriteIndented = true 
            });

            _logger.LogInformation("Generated security report for period {FromDate} to {ToDate}", 
                fromDate, toDate);

            return System.Text.Encoding.UTF8.GetBytes(reportContent);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to generate security report");
            throw new InvalidOperationException("Failed to generate security report", ex);
        }
    }

    public async Task<bool> LogSecurityEventAsync(int transactionId, string eventType, string details)
    {
        try
        {
            var log = new PaymentTransactionLog
            {
                TransactionId = transactionId,
                Action = eventType,
                PreviousStatus = PaymentTransactionStatus.Initiated,
                NewStatus = PaymentTransactionStatus.Initiated,
                Details = details,
                CreatedAt = DateTime.UtcNow
            };

            _context.PaymentTransactionLogs.Add(log);
            await _context.SaveChangesAsync();

            _logger.LogInformation("Logged security event {EventType} for transaction {TransactionId}", 
                eventType, transactionId);

            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to log security event for transaction {TransactionId}", transactionId);
            return false;
        }
    }

    public async Task<List<string>> GetSuspiciousActivityPatternsAsync(DateTime fromDate, DateTime toDate)
    {
        try
        {
            var patterns = new List<string>();

            // Analyze transaction patterns
            var transactions = await _context.PaymentGatewayTransactions
                .Where(t => t.InitiatedAt >= fromDate && t.InitiatedAt <= toDate)
                .ToListAsync();

            // Pattern 1: Multiple transactions from same phone in short time
            var phoneGroups = transactions
                .GroupBy(t => t.PayerPhone)
                .Where(g => g.Count() > 5)
                .ToList();

            foreach (var group in phoneGroups)
            {
                patterns.Add($"Multiple transactions ({group.Count()}) from phone {group.Key}");
            }

            // Pattern 2: High-value transactions outside normal hours
            var afterHours = transactions
                .Where(t => t.Amount > 1000000 && (t.InitiatedAt.Hour < 6 || t.InitiatedAt.Hour > 22))
                .ToList();

            if (afterHours.Any())
            {
                patterns.Add($"High-value transactions outside normal hours: {afterHours.Count}");
            }

            _logger.LogDebug("Identified {Count} suspicious activity patterns", patterns.Count);
            return patterns;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get suspicious activity patterns");
            return new List<string>();
        }
    }

    #endregion

    #region Private Helper Methods

    private async Task<int> AnalyzeAmountRiskAsync(decimal amount, int clientId, List<string> riskFactors)
    {
        var riskScore = 0;
        
        // Check for unusually high amounts
        if (amount > 5000000) // 5M SLE
        {
            riskScore += 30;
            riskFactors.Add("High transaction amount");
        }

        // Check client's transaction history
        var recentTransactions = await _context.PaymentGatewayTransactions
            .Where(t => t.ClientId == clientId && t.InitiatedAt > DateTime.UtcNow.AddDays(-30))
            .Select(t => t.Amount)
            .ToListAsync();

        if (recentTransactions.Any())
        {
            var avgAmount = recentTransactions.Average();
            if (amount > avgAmount * 5)
            {
                riskScore += 20;
                riskFactors.Add("Amount significantly higher than client average");
            }
        }

        return riskScore;
    }

    private async Task<int> AnalyzeFrequencyRiskAsync(int clientId, string payerPhone, List<string> riskFactors)
    {
        var riskScore = 0;
        var cutoff = DateTime.UtcNow.AddMinutes(-10);

        // Check for rapid successive transactions
        var recentCount = await _context.PaymentGatewayTransactions
            .CountAsync(t => (t.ClientId == clientId || t.PayerPhone == payerPhone) && 
                           t.InitiatedAt > cutoff);

        if (recentCount > 3)
        {
            riskScore += 25;
            riskFactors.Add($"Multiple transactions in short timeframe ({recentCount})");
        }

        return riskScore;
    }

    private async Task<int> AnalyzeLocationRiskAsync(string ipAddress, string userAgent, List<string> riskFactors)
    {
        var riskScore = 0;

        // Simple IP validation - in production, use proper geolocation services
        if (string.IsNullOrEmpty(ipAddress) || ipAddress == "127.0.0.1")
        {
            riskScore += 10;
            riskFactors.Add("Invalid or local IP address");
        }

        // User agent analysis
        if (string.IsNullOrEmpty(userAgent) || userAgent.Length < 10)
        {
            riskScore += 15;
            riskFactors.Add("Suspicious or missing user agent");
        }

        return await Task.FromResult(riskScore);
    }

    private async Task<int> AnalyzeTimeRiskAsync(int clientId, List<string> riskFactors)
    {
        var riskScore = 0;
        var currentHour = DateTime.UtcNow.Hour;

        // Check for transactions outside normal business hours
        if (currentHour < 6 || currentHour > 22)
        {
            riskScore += 10;
            riskFactors.Add("Transaction outside normal business hours");
        }

        return await Task.FromResult(riskScore);
    }

    private async Task<(bool IsTriggered, List<string> Factors)> EvaluateFraudRuleAsync(
        PaymentFraudRule rule, 
        CreatePaymentTransactionDto transaction, 
        string ipAddress, 
        string userAgent)
    {
        try
        {
            // Simplified rule evaluation - in production, implement proper rule engine
            var factors = new List<string>();
            var isTriggered = false;

            // Parse conditions (simplified JSON-based conditions)
            var conditions = JsonSerializer.Deserialize<Dictionary<string, object>>(rule.Conditions);
            
            if (conditions?.ContainsKey("maxAmount") == true)
            {
                if (decimal.TryParse(conditions["maxAmount"].ToString(), out var maxAmount))
                {
                    if (transaction.Amount > maxAmount)
                    {
                        isTriggered = true;
                        factors.Add($"Amount exceeds rule limit: {maxAmount}");
                    }
                }
            }

            if (conditions?.ContainsKey("suspiciousPatterns") == true)
            {
                // Add more sophisticated pattern matching here
                isTriggered = true;
                factors.Add("Matched suspicious pattern");
            }

            return await Task.FromResult((isTriggered, factors));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to evaluate fraud rule {RuleName}", rule.RuleName);
            return (false, new List<string>());
        }
    }

    private SecurityRiskLevel DetermineRiskLevel(int riskScore)
    {
        return riskScore switch
        {
            >= 50 => SecurityRiskLevel.Critical,
            >= 30 => SecurityRiskLevel.High,
            >= 15 => SecurityRiskLevel.Medium,
            _ => SecurityRiskLevel.Low
        };
    }

    private int GetRiskScoreForLevel(SecurityRiskLevel level)
    {
        return level switch
        {
            SecurityRiskLevel.Critical => 50,
            SecurityRiskLevel.High => 30,
            SecurityRiskLevel.Medium => 15,
            SecurityRiskLevel.Low => 5,
            _ => 0
        };
    }

    private async Task<List<string>> GetTopRiskFactorsAsync(DateTime fromDate, DateTime toDate)
    {
        try
        {
            var transactions = await _context.PaymentGatewayTransactions
                .Where(t => t.InitiatedAt >= fromDate && t.InitiatedAt <= toDate && 
                           !string.IsNullOrEmpty(t.RiskFactors))
                .Select(t => t.RiskFactors)
                .ToListAsync();

            var allFactors = new List<string>();
            foreach (var factorsJson in transactions)
            {
                try
                {
                    var factors = JsonSerializer.Deserialize<List<string>>(factorsJson);
                    if (factors != null)
                        allFactors.AddRange(factors);
                }
                catch
                {
                    // Ignore invalid JSON
                }
            }

            return allFactors
                .GroupBy(f => f)
                .OrderByDescending(g => g.Count())
                .Take(10)
                .Select(g => $"{g.Key} ({g.Count()})")
                .ToList();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get top risk factors");
            return new List<string>();
        }
    }

    #endregion
}