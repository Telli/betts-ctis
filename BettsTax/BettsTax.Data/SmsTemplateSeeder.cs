using Microsoft.Extensions.DependencyInjection;
using System.Text.Json;

namespace BettsTax.Data
{
    public static class SmsTemplateSeeder
    {
        public static async Task SeedSmsTemplatesAsync(IServiceProvider serviceProvider)
        {
            using var scope = serviceProvider.CreateScope();
            var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

            // Check if templates already exist
            if (context.SmsTemplates.Any())
                return;

            var templates = new List<SmsTemplate>
            {
                // Deadline Reminder Template
                new SmsTemplate
                {
                    TemplateCode = "DEADLINE_REMINDER",
                    Name = "Tax Deadline Reminder",
                    Description = "Remind clients about upcoming tax deadlines",
                    MessageTemplate = "{ClientName}: {TaxType} due {DueDate}. Submit by deadline to avoid penalties. Login: ctis.bettsfirmsl.com - Betts Firm",
                    Type = SmsType.DeadlineReminder,
                    AvailableVariables = JsonSerializer.Serialize(new Dictionary<string, string>
                    {
                        ["ClientName"] = "Client business name",
                        ["TaxType"] = "Type of tax (GST, Income Tax, etc.)",
                        ["DueDate"] = "Due date in MMM dd format"
                    }),
                    CharacterCount = 120
                },

                // Payment Confirmation Template
                new SmsTemplate
                {
                    TemplateCode = "PAYMENT_CONFIRM",
                    Name = "Payment Confirmation",
                    Description = "Confirm payment receipt to client",
                    MessageTemplate = "Payment Confirmed: SLE {Amount} received for {TaxType}. Ref: {Reference}. Thank you! - Betts Firm",
                    Type = SmsType.PaymentConfirmation,
                    AvailableVariables = JsonSerializer.Serialize(new Dictionary<string, string>
                    {
                        ["Amount"] = "Payment amount",
                        ["TaxType"] = "Type of tax paid",
                        ["Reference"] = "Payment reference number"
                    }),
                    CharacterCount = 95
                },

                // Document Request Template
                new SmsTemplate
                {
                    TemplateCode = "DOC_REQUEST",
                    Name = "Document Request",
                    Description = "Request documents from client",
                    MessageTemplate = "Documents needed: {DocumentList}. Upload by {DueDate}. Login: ctis.bettsfirmsl.com - Betts Firm",
                    Type = SmsType.DocumentRequest,
                    AvailableVariables = JsonSerializer.Serialize(new Dictionary<string, string>
                    {
                        ["DocumentList"] = "List of required documents",
                        ["DueDate"] = "Due date for documents"
                    }),
                    CharacterCount = 100
                },

                // Tax Filing Confirmation
                new SmsTemplate
                {
                    TemplateCode = "FILING_CONFIRM",
                    Name = "Tax Filing Confirmation",
                    Description = "Confirm tax filing submission",
                    MessageTemplate = "Tax Filed: {TaxType} for {TaxYear} submitted. Ref: {Reference}. Payment due: {PaymentDue} - Betts Firm",
                    Type = SmsType.TaxFilingConfirmation,
                    AvailableVariables = JsonSerializer.Serialize(new Dictionary<string, string>
                    {
                        ["TaxType"] = "Type of tax filed",
                        ["TaxYear"] = "Tax year/period",
                        ["Reference"] = "Filing reference number",
                        ["PaymentDue"] = "Payment due date"
                    }),
                    CharacterCount = 110
                },

                // Password Reset Template
                new SmsTemplate
                {
                    TemplateCode = "PASSWORD_RESET",
                    Name = "Password Reset Code",
                    Description = "Send password reset code",
                    MessageTemplate = "Password Reset Code: {Code}. Valid for 15 minutes. Do not share. - Betts Firm",
                    Type = SmsType.PasswordReset,
                    AvailableVariables = JsonSerializer.Serialize(new Dictionary<string, string>
                    {
                        ["Code"] = "Reset code"
                    }),
                    CharacterCount = 80
                },

                // Two-Factor Authentication
                new SmsTemplate
                {
                    TemplateCode = "2FA_CODE",
                    Name = "Two-Factor Authentication",
                    Description = "Send 2FA verification code",
                    MessageTemplate = "Security Code: {Code} for Betts Firm account. Valid 5 minutes. Do not share.",
                    Type = SmsType.TwoFactorAuth,
                    AvailableVariables = JsonSerializer.Serialize(new Dictionary<string, string>
                    {
                        ["Code"] = "2FA code"
                    }),
                    CharacterCount = 75
                },

                // General Notification
                new SmsTemplate
                {
                    TemplateCode = "GENERAL",
                    Name = "General Message",
                    Description = "General purpose SMS template",
                    MessageTemplate = "{Message} - Betts Firm",
                    Type = SmsType.General,
                    AvailableVariables = JsonSerializer.Serialize(new Dictionary<string, string>
                    {
                        ["Message"] = "Custom message content"
                    }),
                    CharacterCount = 25
                }
            };

            context.SmsTemplates.AddRange(templates);
            await context.SaveChangesAsync();
        }

        public static async Task SeedSmsProviderConfigsAsync(IServiceProvider serviceProvider)
        {
            using var scope = serviceProvider.CreateScope();
            var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

            // Check if configs already exist
            if (context.SmsProviderConfigs.Any())
                return;

            var configs = new List<SmsProviderConfig>
            {
                // Orange SL Configuration
                new SmsProviderConfig
                {
                    Provider = SmsProvider.OrangeSL,
                    Name = "Orange Sierra Leone",
                    ApiUrl = "https://api.orange.sl/sms/v1/send", // Example URL
                    SenderId = "BETTSFIRM",
                    CostPerSms = 500, // SLE 500 per SMS (example)
                    Currency = "SLE",
                    Priority = 1,
                    DailyLimit = 1000,
                    MonthlyLimit = 20000,
                    IsActive = true,
                    IsDefault = true,
                    AdditionalSettings = JsonSerializer.Serialize(new Dictionary<string, string>
                    {
                        ["SupportedPrefixes"] = "76,77,78,79",
                        ["MaxMessageLength"] = "160"
                    })
                },

                // Africell SL Configuration
                new SmsProviderConfig
                {
                    Provider = SmsProvider.AfricellSL,
                    Name = "Africell Sierra Leone",
                    ApiUrl = "https://api.africell.sl/sms/send", // Example URL
                    SenderId = "BETTSFIRM",
                    CostPerSms = 450, // SLE 450 per SMS (example)
                    Currency = "SLE",
                    Priority = 2,
                    DailyLimit = 1000,
                    MonthlyLimit = 20000,
                    IsActive = false, // Not active by default
                    IsDefault = false,
                    AdditionalSettings = JsonSerializer.Serialize(new Dictionary<string, string>
                    {
                        ["SupportedPrefixes"] = "88,30,31,32,33,34,80",
                        ["MaxMessageLength"] = "160"
                    })
                }
            };

            context.SmsProviderConfigs.AddRange(configs);
            await context.SaveChangesAsync();
        }

        public static async Task SeedSmsSchedulesAsync(IServiceProvider serviceProvider)
        {
            using var scope = serviceProvider.CreateScope();
            var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

            // Check if schedules already exist
            if (context.SmsSchedules.Any())
                return;

            // Get templates
            var deadlineTemplate = context.SmsTemplates.FirstOrDefault(t => t.TemplateCode == "DEADLINE_REMINDER");
            if (deadlineTemplate == null)
                return;

            var schedules = new List<SmsSchedule>
            {
                // GST Monthly Deadline Reminder - 3 days before
                new SmsSchedule
                {
                    Name = "GST Monthly Reminder - 3 Days",
                    Description = "Remind clients 3 days before GST monthly filing deadline",
                    Type = SmsType.DeadlineReminder,
                    DaysBefore = 3,
                    TimeOfDay = "09:00",
                    IsRecurring = true,
                    RecurrenceIntervalDays = 30,
                    SmsTemplateId = deadlineTemplate.SmsTemplateId,
                    TaxType = TaxType.GST,
                    OnlyActiveClients = true,
                    IsActive = true
                },

                // Income Tax Quarterly Reminder - 7 days before
                new SmsSchedule
                {
                    Name = "Income Tax Quarterly Reminder - 7 Days",
                    Description = "Remind clients 7 days before quarterly income tax deadline",
                    Type = SmsType.DeadlineReminder,
                    DaysBefore = 7,
                    TimeOfDay = "09:00",
                    IsRecurring = true,
                    RecurrenceIntervalDays = 90,
                    SmsTemplateId = deadlineTemplate.SmsTemplateId,
                    TaxType = TaxType.IncomeTax,
                    OnlyActiveClients = true,
                    IsActive = true
                },

                // Payroll Tax Monthly Reminder - 2 days before
                new SmsSchedule
                {
                    Name = "Payroll Tax Reminder - 2 Days",
                    Description = "Remind clients 2 days before payroll tax deadline",
                    Type = SmsType.DeadlineReminder,
                    DaysBefore = 2,
                    TimeOfDay = "14:00",
                    IsRecurring = true,
                    RecurrenceIntervalDays = 30,
                    SmsTemplateId = deadlineTemplate.SmsTemplateId,
                    TaxType = TaxType.PayrollTax,
                    OnlyActiveClients = true,
                    IsActive = true
                }
            };

            context.SmsSchedules.AddRange(schedules);
            await context.SaveChangesAsync();
        }
    }
}