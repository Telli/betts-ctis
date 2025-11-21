using BettsTax.Data.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace BettsTax.Data.Seeders
{
    /// <summary>
    /// Seeds default deadline rules and Sierra Leone public holidays
    /// Phase 3: Configurable Deadline Rules
    /// </summary>
    public static class DeadlineRuleSeeder
    {
        public static async Task SeedDeadlineRulesAsync(IServiceProvider serviceProvider)
        {
            using var scope = serviceProvider.CreateScope();
            var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            var logger = scope.ServiceProvider.GetRequiredService<ILogger<ApplicationDbContext>>();
            
            try
            {
                // Check if rules already exist
                if (await context.Set<DeadlineRuleConfiguration>().AnyAsync())
                {
                    logger.LogInformation("Deadline rules already seeded");
                    return;
                }
                
                logger.LogInformation("Seeding default deadline rules...");
                
                var rules = new List<DeadlineRuleConfiguration>
                {
                    // GST - 21 days from period end
                    new DeadlineRuleConfiguration
                    {
                        TaxType = TaxType.GST,
                        RuleName = "GST Standard Filing Deadline",
                        Description = "GST returns must be filed within 21 days of the end of the tax period",
                        DaysFromTrigger = 21,
                        TriggerType = "PeriodEnd",
                        AdjustForWeekends = true,
                        AdjustForHolidays = true,
                        StatutoryMinimumDays = 21,
                        IsDefault = true,
                        IsActive = true,
                        CreatedById = "System",
                        EffectiveDate = new DateTime(2024, 1, 1)
                    },
                    
                    // Corporate Income Tax - 4 months from year end
                    new DeadlineRuleConfiguration
                    {
                        TaxType = TaxType.CorporateIncomeTax,
                        RuleName = "Corporate Income Tax Annual Return Deadline",
                        Description = "Corporate income tax returns must be filed within 4 months of the end of the accounting period",
                        DaysFromTrigger = 120, // Approximately 4 months
                        TriggerType = "YearEnd",
                        AdjustForWeekends = true,
                        AdjustForHolidays = true,
                        StatutoryMinimumDays = 120,
                        IsDefault = true,
                        IsActive = true,
                        CreatedById = "System",
                        EffectiveDate = new DateTime(2024, 1, 1)
                    },
                    
                    // Personal Income Tax - 3 months from year end
                    new DeadlineRuleConfiguration
                    {
                        TaxType = TaxType.PersonalIncomeTax,
                        RuleName = "Personal Income Tax Annual Return Deadline",
                        Description = "Personal income tax returns must be filed within 3 months of the end of the tax year",
                        DaysFromTrigger = 90, // 3 months
                        TriggerType = "YearEnd",
                        AdjustForWeekends = true,
                        AdjustForHolidays = true,
                        StatutoryMinimumDays = 90,
                        IsDefault = true,
                        IsActive = true,
                        CreatedById = "System",
                        EffectiveDate = new DateTime(2024, 1, 1)
                    },
                    
                    // PAYE - Monthly (21 days from month end)
                    new DeadlineRuleConfiguration
                    {
                        TaxType = TaxType.PAYE,
                        RuleName = "PAYE Monthly Remittance Deadline",
                        Description = "PAYE must be remitted within 21 days of the end of each month",
                        DaysFromTrigger = 21,
                        TriggerType = "MonthEnd",
                        AdjustForWeekends = true,
                        AdjustForHolidays = true,
                        StatutoryMinimumDays = 21,
                        IsDefault = true,
                        IsActive = true,
                        CreatedById = "System",
                        EffectiveDate = new DateTime(2024, 1, 1)
                    },
                    
                    // Payroll Tax - Annual return by January 31
                    new DeadlineRuleConfiguration
                    {
                        TaxType = TaxType.PayrollTax,
                        RuleName = "Payroll Tax Annual Return Deadline",
                        Description = "Annual payroll tax returns must be filed by January 31 of the following year",
                        DaysFromTrigger = 31, // Days into new year
                        TriggerType = "YearEnd",
                        AdjustForWeekends = true,
                        AdjustForHolidays = true,
                        StatutoryMinimumDays = 31,
                        IsDefault = true,
                        IsActive = true,
                        CreatedById = "System",
                        EffectiveDate = new DateTime(2024, 1, 1)
                    },
                    
                    // Payroll Tax - Foreign employees (1 month from employment start)
                    new DeadlineRuleConfiguration
                    {
                        TaxType = TaxType.PayrollTax,
                        RuleName = "Payroll Tax Foreign Employee Registration",
                        Description = "Foreign employee payroll tax registration must be completed within 1 month of employment start",
                        DaysFromTrigger = 30,
                        TriggerType = "EmploymentStart",
                        AdjustForWeekends = true,
                        AdjustForHolidays = true,
                        StatutoryMinimumDays = 30,
                        IsDefault = false,
                        IsActive = true,
                        CreatedById = "System",
                        EffectiveDate = new DateTime(2024, 1, 1)
                    },
                    
                    // Excise Duty - 21 days from delivery/import
                    new DeadlineRuleConfiguration
                    {
                        TaxType = TaxType.ExciseDuty,
                        RuleName = "Excise Duty Payment Deadline",
                        Description = "Excise duty must be paid within 21 days of goods delivery or import",
                        DaysFromTrigger = 21,
                        TriggerType = "DeliveryDate",
                        AdjustForWeekends = true,
                        AdjustForHolidays = true,
                        StatutoryMinimumDays = 21,
                        IsDefault = true,
                        IsActive = true,
                        CreatedById = "System",
                        EffectiveDate = new DateTime(2024, 1, 1)
                    },
                    
                    // Withholding Tax - 21 days from month end
                    new DeadlineRuleConfiguration
                    {
                        TaxType = TaxType.WithholdingTax,
                        RuleName = "Withholding Tax Monthly Remittance",
                        Description = "Withholding tax must be remitted within 21 days of the end of each month",
                        DaysFromTrigger = 21,
                        TriggerType = "MonthEnd",
                        AdjustForWeekends = true,
                        AdjustForHolidays = true,
                        StatutoryMinimumDays = 21,
                        IsDefault = true,
                        IsActive = true,
                        CreatedById = "System",
                        EffectiveDate = new DateTime(2024, 1, 1)
                    }
                };
                
                await context.Set<DeadlineRuleConfiguration>().AddRangeAsync(rules);
                await context.SaveChangesAsync();
                
                logger.LogInformation("Successfully seeded {Count} deadline rules", rules.Count);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error seeding deadline rules");
                throw;
            }
        }
        
        public static async Task SeedPublicHolidaysAsync(IServiceProvider serviceProvider, int year = 2025)
        {
            using var scope = serviceProvider.CreateScope();
            var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            var logger = scope.ServiceProvider.GetRequiredService<ILogger<ApplicationDbContext>>();
            
            try
            {
                // Check if holidays already exist for this year
                if (await context.Set<PublicHoliday>().AnyAsync(h => h.Year == year))
                {
                    logger.LogInformation("Public holidays for {Year} already seeded", year);
                    return;
                }
                
                logger.LogInformation("Seeding Sierra Leone public holidays for {Year}...", year);
                
                var holidays = new List<PublicHoliday>
                {
                    // Fixed annual holidays
                    new PublicHoliday
                    {
                        Name = "New Year's Day",
                        Date = new DateTime(year, 1, 1),
                        Year = year,
                        IsRecurring = true,
                        RecurringMonth = 1,
                        RecurringDay = 1,
                        IsNational = true,
                        Description = "First day of the year",
                        CreatedById = "System"
                    },
                    new PublicHoliday
                    {
                        Name = "Independence Day",
                        Date = new DateTime(year, 4, 27),
                        Year = year,
                        IsRecurring = true,
                        RecurringMonth = 4,
                        RecurringDay = 27,
                        IsNational = true,
                        Description = "Sierra Leone Independence Day",
                        CreatedById = "System"
                    },
                    new PublicHoliday
                    {
                        Name = "Christmas Day",
                        Date = new DateTime(year, 12, 25),
                        Year = year,
                        IsRecurring = true,
                        RecurringMonth = 12,
                        RecurringDay = 25,
                        IsNational = true,
                        Description = "Christmas celebration",
                        CreatedById = "System"
                    },
                    new PublicHoliday
                    {
                        Name = "Boxing Day",
                        Date = new DateTime(year, 12, 26),
                        Year = year,
                        IsRecurring = true,
                        RecurringMonth = 12,
                        RecurringDay = 26,
                        IsNational = true,
                        Description = "Day after Christmas",
                        CreatedById = "System"
                    },
                    
                    // 2025 specific holidays (these would need to be updated annually for variable dates)
                    new PublicHoliday
                    {
                        Name = "Good Friday",
                        Date = new DateTime(2025, 4, 18),
                        Year = 2025,
                        IsRecurring = false,
                        IsNational = true,
                        Description = "Good Friday observance",
                        CreatedById = "System"
                    },
                    new PublicHoliday
                    {
                        Name = "Easter Monday",
                        Date = new DateTime(2025, 4, 21),
                        Year = 2025,
                        IsRecurring = false,
                        IsNational = true,
                        Description = "Easter Monday observance",
                        CreatedById = "System"
                    },
                    new PublicHoliday
                    {
                        Name = "Eid al-Fitr",
                        Date = new DateTime(2025, 3, 31), // Approximate - varies by lunar calendar
                        Year = 2025,
                        IsRecurring = false,
                        IsNational = true,
                        Description = "End of Ramadan celebration",
                        CreatedById = "System"
                    },
                    new PublicHoliday
                    {
                        Name = "Eid al-Adha",
                        Date = new DateTime(2025, 6, 7), // Approximate - varies by lunar calendar
                        Year = 2025,
                        IsRecurring = false,
                        IsNational = true,
                        Description = "Feast of Sacrifice",
                        CreatedById = "System"
                    }
                };
                
                await context.Set<PublicHoliday>().AddRangeAsync(holidays);
                await context.SaveChangesAsync();
                
                logger.LogInformation("Successfully seeded {Count} public holidays for {Year}", holidays.Count, year);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error seeding public holidays");
                throw;
            }
        }
    }
}
