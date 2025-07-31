using BettsTax.Data.Models;

namespace BettsTax.Core.DTOs.Payment;

// Fraud rule DTOs
public class PaymentFraudRuleDto
{
    public int Id { get; set; }
    public string RuleName { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public bool IsActive { get; set; }
    public string Conditions { get; set; } = string.Empty;
    public string Action { get; set; } = string.Empty;
    public SecurityRiskLevel RiskLevel { get; set; }
    public string RiskLevelName { get; set; } = string.Empty;
    public int Priority { get; set; }
    public int TriggerCount { get; set; }
    public DateTime? LastTriggered { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
    public string CreatedBy { get; set; } = string.Empty;
    public string? UpdatedBy { get; set; }
}

public class CreatePaymentFraudRuleDto
{
    public string RuleName { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public bool IsActive { get; set; } = true;
    public string Conditions { get; set; } = string.Empty;
    public string Action { get; set; } = string.Empty;
    public SecurityRiskLevel RiskLevel { get; set; }
    public int Priority { get; set; } = 1;
}