using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace BettsTax.Data
{
    public static class ComplianceSeeder
    {
        public static async Task SeedPenaltyRulesAsync(IServiceProvider serviceProvider)
        {
            using var scope = serviceProvider.CreateScope();
            var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            var logger = scope.ServiceProvider.GetRequiredService<ILogger<ApplicationDbContext>>();

            try
            {
                // Check if penalty rules already exist
                if (await context.Set<PenaltyRule>().AnyAsync())
                {
                    logger.LogInformation("Penalty rules already exist, skipping seeding");
                    return;
                }

                var rules = new List<PenaltyRule>();

                // Income Tax Penalty Rules
                rules.AddRange(CreateIncomeTaxPenaltyRules());
                
                // GST Penalty Rules
                rules.AddRange(CreateGstPenaltyRules());
                
                // Payroll Tax Penalty Rules
                rules.AddRange(CreatePayrollTaxPenaltyRules());
                
                // Excise Duty Penalty Rules
                rules.AddRange(CreateExciseDutyPenaltyRules());

                context.Set<PenaltyRule>().AddRange(rules);
                await context.SaveChangesAsync();

                logger.LogInformation("Seeded {Count} penalty rules", rules.Count);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error seeding penalty rules");
                throw;
            }
        }

        private static List<PenaltyRule> CreateIncomeTaxPenaltyRules()
        {
            return new List<PenaltyRule>
            {
                // Late Filing - Income Tax
                new PenaltyRule
                {
                    TaxType = TaxType.IncomeTax,
                    PenaltyType = PenaltyType.LateFilingPenalty,
                    TaxpayerCategory = null, // Applies to all categories
                    RuleName = "Income Tax Late Filing Penalty - General",
                    Description = "5% of tax liability for late filing of income tax returns",
                    FixedRate = 5m, // 5% of tax liability
                    MinimumAmount = 500m, // 500 SLE minimum
                    MaximumAmount = 50000m, // 50,000 SLE maximum
                    GracePeriodDays = 7, // 7 day grace period
                    LegalReference = "Sierra Leone Finance Act 2020, Section 112",
                    Priority = 1,
                    EffectiveDate = new DateTime(2020, 1, 1),
                    IsActive = true
                },
                
                // Late Filing - Large Taxpayers (higher penalty)
                new PenaltyRule
                {
                    TaxType = TaxType.IncomeTax,
                    PenaltyType = PenaltyType.LateFilingPenalty,
                    TaxpayerCategory = TaxpayerCategory.Large,
                    RuleName = "Income Tax Late Filing Penalty - Large Taxpayers",
                    Description = "10% of tax liability for late filing by large taxpayers",
                    FixedRate = 10m, // 10% of tax liability for large taxpayers
                    MinimumAmount = 2000m, // 2,000 SLE minimum
                    MaximumAmount = 100000m, // 100,000 SLE maximum
                    GracePeriodDays = 5, // Shorter grace period for large taxpayers
                    LegalReference = "Sierra Leone Finance Act 2020, Section 112(2)",
                    Priority = 1,
                    EffectiveDate = new DateTime(2020, 1, 1),
                    IsActive = true
                },
                
                // Late Payment - Income Tax
                new PenaltyRule
                {
                    TaxType = TaxType.IncomeTax,
                    PenaltyType = PenaltyType.LatePaymentPenalty,
                    RuleName = "Income Tax Late Payment Penalty",
                    Description = "2% per month on unpaid income tax",
                    IsTimeBased = true,
                    MonthlyRate = 2m, // 2% per month
                    GracePeriodDays = 30, // 30 day grace period
                    MaximumDays = 365, // Maximum 1 year
                    MinimumAmount = 100m, // 100 SLE minimum
                    LegalReference = "Sierra Leone Finance Act 2020, Section 115",
                    Priority = 1,
                    EffectiveDate = new DateTime(2020, 1, 1),
                    IsActive = true
                },
                
                // Non-Filing - Income Tax
                new PenaltyRule
                {
                    TaxType = TaxType.IncomeTax,
                    PenaltyType = PenaltyType.NonFilingPenalty,
                    RuleName = "Income Tax Non-Filing Penalty",
                    Description = "20% of estimated tax liability for failure to file",
                    FixedRate = 20m, // 20% of estimated liability
                    MinimumAmount = 2000m, // 2,000 SLE minimum
                    MaximumAmount = 100000m, // 100,000 SLE maximum
                    ThresholdDays = 30, // Penalty applies after 30 days
                    LegalReference = "Sierra Leone Finance Act 2020, Section 118",
                    Priority = 1,
                    EffectiveDate = new DateTime(2020, 1, 1),
                    IsActive = true
                },
                
                // Under-Declaration - Income Tax
                new PenaltyRule
                {
                    TaxType = TaxType.IncomeTax,
                    PenaltyType = PenaltyType.UnderDeclarationPenalty,
                    RuleName = "Income Tax Under-Declaration Penalty",
                    Description = "25% of the under-declared amount",
                    FixedRate = 25m, // 25% of under-declared amount
                    MinimumAmount = 1000m, // 1,000 SLE minimum
                    ThresholdAmount = 5000m, // Applies if under-declaration > 5,000 SLE
                    LegalReference = "Sierra Leone Finance Act 2020, Section 120",
                    Priority = 1,
                    EffectiveDate = new DateTime(2020, 1, 1),
                    IsActive = true
                }
            };
        }

        private static List<PenaltyRule> CreateGstPenaltyRules()
        {
            return new List<PenaltyRule>
            {
                // Late Filing - GST
                new PenaltyRule
                {
                    TaxType = TaxType.GST,
                    PenaltyType = PenaltyType.LateFilingPenalty,
                    RuleName = "GST Late Filing Penalty",
                    Description = "10% of GST liability for late filing",
                    FixedRate = 10m, // 10% of GST liability
                    MinimumAmount = 200m, // 200 SLE minimum
                    MaximumAmount = 25000m, // 25,000 SLE maximum
                    GracePeriodDays = 5, // 5 day grace period
                    LegalReference = "Sierra Leone Finance Act 2020, Section 142",
                    Priority = 1,
                    EffectiveDate = new DateTime(2020, 1, 1),
                    IsActive = true
                },
                
                // Late Payment - GST
                new PenaltyRule
                {
                    TaxType = TaxType.GST,
                    PenaltyType = PenaltyType.LatePaymentPenalty,
                    RuleName = "GST Late Payment Penalty",
                    Description = "3% per month on unpaid GST",
                    IsTimeBased = true,
                    MonthlyRate = 3m, // 3% per month
                    GracePeriodDays = 15, // 15 day grace period
                    MaximumDays = 365, // Maximum 1 year
                    MinimumAmount = 50m, // 50 SLE minimum
                    LegalReference = "Sierra Leone Finance Act 2020, Section 145",
                    Priority = 1,
                    EffectiveDate = new DateTime(2020, 1, 1),
                    IsActive = true
                },
                
                // Non-Filing - GST
                new PenaltyRule
                {
                    TaxType = TaxType.GST,
                    PenaltyType = PenaltyType.NonFilingPenalty,
                    RuleName = "GST Non-Filing Penalty",
                    Description = "Fixed penalty for failure to file GST returns",
                    FixedAmount = 5000m, // 5,000 SLE fixed penalty
                    ThresholdDays = 15, // Penalty applies after 15 days
                    LegalReference = "Sierra Leone Finance Act 2020, Section 148",
                    Priority = 1,
                    EffectiveDate = new DateTime(2020, 1, 1),
                    IsActive = true
                }
            };
        }

        private static List<PenaltyRule> CreatePayrollTaxPenaltyRules()
        {
            return new List<PenaltyRule>
            {
                // Late Filing - Payroll Tax
                new PenaltyRule
                {
                    TaxType = TaxType.PayrollTax,
                    PenaltyType = PenaltyType.LateFilingPenalty,
                    RuleName = "Payroll Tax Late Filing Penalty",
                    Description = "Fixed penalty for late payroll tax filing",
                    FixedAmount = 1000m, // 1,000 SLE fixed penalty
                    GracePeriodDays = 5, // 5 day grace period
                    LegalReference = "Sierra Leone Finance Act 2020, Section 162",
                    Priority = 1,
                    EffectiveDate = new DateTime(2020, 1, 1),
                    IsActive = true
                },
                
                // Late Payment - Payroll Tax
                new PenaltyRule
                {
                    TaxType = TaxType.PayrollTax,
                    PenaltyType = PenaltyType.LatePaymentPenalty,
                    RuleName = "Payroll Tax Late Payment Penalty",
                    Description = "5% per month on unpaid payroll tax",
                    IsTimeBased = true,
                    MonthlyRate = 5m, // 5% per month
                    GracePeriodDays = 10, // 10 day grace period
                    MaximumDays = 180, // Maximum 6 months
                    MinimumAmount = 200m, // 200 SLE minimum
                    LegalReference = "Sierra Leone Finance Act 2020, Section 165",
                    Priority = 1,
                    EffectiveDate = new DateTime(2020, 1, 1),
                    IsActive = true
                },
                
                // Non-Remittance - Payroll Tax
                new PenaltyRule
                {
                    TaxType = TaxType.PayrollTax,
                    PenaltyType = PenaltyType.NonFilingPenalty,
                    RuleName = "Payroll Tax Non-Remittance Penalty",
                    Description = "50% of the amount not remitted",
                    FixedRate = 50m, // 50% of amount not remitted
                    MinimumAmount = 5000m, // 5,000 SLE minimum
                    ThresholdDays = 30, // Penalty applies after 30 days
                    LegalReference = "Sierra Leone Finance Act 2020, Section 168",
                    Priority = 1,
                    EffectiveDate = new DateTime(2020, 1, 1),
                    IsActive = true
                }
            };
        }

        private static List<PenaltyRule> CreateExciseDutyPenaltyRules()
        {
            return new List<PenaltyRule>
            {
                // Late Filing - Excise Duty
                new PenaltyRule
                {
                    TaxType = TaxType.ExciseDuty,
                    PenaltyType = PenaltyType.LateFilingPenalty,
                    RuleName = "Excise Duty Late Filing Penalty",
                    Description = "15% of excise duty liability for late filing",
                    FixedRate = 15m, // 15% of excise duty liability
                    MinimumAmount = 1000m, // 1,000 SLE minimum
                    MaximumAmount = 75000m, // 75,000 SLE maximum
                    GracePeriodDays = 7, // 7 day grace period
                    LegalReference = "Sierra Leone Finance Act 2020, Section 182",
                    Priority = 1,
                    EffectiveDate = new DateTime(2020, 1, 1),
                    IsActive = true
                },
                
                // Late Payment - Excise Duty
                new PenaltyRule
                {
                    TaxType = TaxType.ExciseDuty,
                    PenaltyType = PenaltyType.LatePaymentPenalty,
                    RuleName = "Excise Duty Late Payment Penalty",
                    Description = "4% per month on unpaid excise duty",
                    IsTimeBased = true,
                    MonthlyRate = 4m, // 4% per month
                    GracePeriodDays = 20, // 20 day grace period
                    MaximumDays = 270, // Maximum 9 months
                    MinimumAmount = 300m, // 300 SLE minimum
                    LegalReference = "Sierra Leone Finance Act 2020, Section 185",
                    Priority = 1,
                    EffectiveDate = new DateTime(2020, 1, 1),
                    IsActive = true
                }
            };
        }

        public static async Task SeedComplianceInsightsAsync(IServiceProvider serviceProvider)
        {
            using var scope = serviceProvider.CreateScope();
            var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            var logger = scope.ServiceProvider.GetRequiredService<ILogger<ApplicationDbContext>>();

            try
            {
                // Check if compliance insights already exist
                if (await context.Set<ComplianceInsight>().AnyAsync())
                {
                    logger.LogInformation("Compliance insights already exist, skipping seeding");
                    return;
                }

                var insights = new List<ComplianceInsight>
                {
                    // General system-wide insights
                    new ComplianceInsight
                    {
                        Title = "Early Filing Benefits",
                        Description = "Filing tax returns early helps avoid penalties and reduces compliance risk.",
                        Recommendation = "Encourage clients to file returns at least 7 days before the deadline to avoid any last-minute issues.",
                        RiskLevel = ComplianceRiskLevel.Low,
                        PotentialSavings = 5000m,
                        Category = "Tax Planning",
                        Tags = "early-filing,planning,deadline",
                        IsSystemGenerated = true,
                        GeneratedBy = "System",
                        IsActive = true
                    },
                    
                    new ComplianceInsight
                    {
                        Title = "Payment Plan Options",
                        Description = "Clients with large tax liabilities can benefit from installment payment plans.",
                        Recommendation = "Consider setting up payment plans for clients with liabilities over 100,000 SLE to manage cash flow.",
                        RiskLevel = ComplianceRiskLevel.Medium,
                        PotentialSavings = 15000m,
                        Category = "Payment Planning",
                        Tags = "payment-plan,cash-flow,large-liability",
                        IsSystemGenerated = true,
                        GeneratedBy = "System",
                        IsActive = true
                    },
                    
                    new ComplianceInsight
                    {
                        Title = "GST Registration Threshold",
                        Description = "Businesses approaching the GST registration threshold should prepare for compliance requirements.",
                        Recommendation = "Monitor client revenue and prepare GST registration documentation when approaching the configured GST registration threshold.",
                        RiskLevel = ComplianceRiskLevel.Medium,
                        PotentialSavings = 25000m,
                        Category = "Registration",
                        Tags = "gst,registration,threshold,revenue",
                        IsSystemGenerated = true,
                        GeneratedBy = "System",
                        IsActive = true
                    },
                    
                    new ComplianceInsight
                    {
                        Title = "Penalty Avoidance Strategy",
                        Description = "Regular compliance monitoring can prevent costly penalties and interest charges.",
                        Recommendation = "Implement monthly compliance reviews to identify and address potential issues before they become penalties.",
                        RiskLevel = ComplianceRiskLevel.High,
                        PotentialSavings = 50000m,
                        Category = "Penalty Avoidance",
                        Tags = "penalties,monitoring,prevention,review",
                        IsSystemGenerated = true,
                        GeneratedBy = "System",
                        IsActive = true
                    },
                    
                    new ComplianceInsight
                    {
                        Title = "Document Organization",
                        Description = "Well-organized documentation speeds up tax preparation and reduces errors.",
                        Recommendation = "Encourage clients to maintain digital records and categorize expenses monthly throughout the year.",
                        RiskLevel = ComplianceRiskLevel.Low,
                        PotentialSavings = 8000m,
                        Category = "Documentation",
                        Tags = "documentation,organization,digital-records,preparation",
                        IsSystemGenerated = true,
                        GeneratedBy = "System",
                        IsActive = true
                    }
                };

                context.Set<ComplianceInsight>().AddRange(insights);
                await context.SaveChangesAsync();

                logger.LogInformation("Seeded {Count} compliance insights", insights.Count);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error seeding compliance insights");
                throw;
            }
        }
    }
}