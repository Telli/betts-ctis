using Microsoft.Extensions.DependencyInjection;
using System.Text.Json;

namespace BettsTax.Data
{
    public static class MessageTemplateSeeder
    {
        public static async Task SeedMessageTemplatesAsync(IServiceProvider serviceProvider)
        {
            using var scope = serviceProvider.CreateScope();
            var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

            // Check if templates already exist
            if (context.MessageTemplates.Any())
                return;

            var templates = new List<MessageTemplate>
            {
                // Document Request Template
                new MessageTemplate
                {
                    TemplateCode = "DOC_REQUEST",
                    Name = "Document Request",
                    Description = "Request specific documents from a client",
                    Subject = "Documents Required - {ClientName}",
                    Body = @"Dear {ClientFirstName} {ClientLastName},

We require the following documents to proceed with your tax filing for {TaxYear}:

{DocumentList}

Please upload these documents to your portal by {DueDate}.

If you have any questions or need assistance, please don't hesitate to contact us.

Best regards,
{AssociateName}
The Betts Firm",
                    Category = MessageCategory.DocumentRequest,
                    AvailableVariables = JsonSerializer.Serialize(new Dictionary<string, string>
                    {
                        ["ClientName"] = "Business name of the client",
                        ["ClientFirstName"] = "Client's first name",
                        ["ClientLastName"] = "Client's last name",
                        ["TaxYear"] = "Tax year",
                        ["DocumentList"] = "List of required documents",
                        ["DueDate"] = "Due date for documents",
                        ["AssociateName"] = "Name of the assigned associate"
                    })
                },

                // Document Rejection Template
                new MessageTemplate
                {
                    TemplateCode = "DOC_REJECTED",
                    Name = "Document Rejection",
                    Description = "Notify client that a document has been rejected",
                    Subject = "Document Rejected - {DocumentName}",
                    Body = @"Dear {ClientFirstName} {ClientLastName},

Your document '{DocumentName}' has been reviewed and requires resubmission.

Reason for rejection:
{RejectionReason}

Please upload a corrected version of this document as soon as possible.

Best regards,
{AssociateName}
The Betts Firm",
                    Category = MessageCategory.DocumentReview,
                    AvailableVariables = JsonSerializer.Serialize(new Dictionary<string, string>
                    {
                        ["ClientFirstName"] = "Client's first name",
                        ["ClientLastName"] = "Client's last name",
                        ["DocumentName"] = "Name of the rejected document",
                        ["RejectionReason"] = "Reason for rejection",
                        ["AssociateName"] = "Name of the reviewing associate"
                    })
                },

                // Tax Filing Submission Confirmation
                new MessageTemplate
                {
                    TemplateCode = "TAX_SUBMITTED",
                    Name = "Tax Filing Submitted",
                    Description = "Confirm tax filing submission to authorities",
                    Subject = "Tax Filing Submitted - {TaxType} for {TaxYear}",
                    Body = @"Dear {ClientFirstName} {ClientLastName},

Your {TaxType} filing for {TaxYear} has been successfully submitted to the National Revenue Authority.

Filing Details:
- Filing Reference: {FilingReference}
- Submission Date: {SubmissionDate}
- Tax Liability: SLE {TaxLiability}
- Payment Due Date: {PaymentDueDate}

Please ensure payment is made before the due date to avoid penalties.

Best regards,
{AssociateName}
The Betts Firm",
                    Category = MessageCategory.TaxFiling,
                    AvailableVariables = JsonSerializer.Serialize(new Dictionary<string, string>
                    {
                        ["ClientFirstName"] = "Client's first name",
                        ["ClientLastName"] = "Client's last name",
                        ["TaxType"] = "Type of tax",
                        ["TaxYear"] = "Tax year",
                        ["FilingReference"] = "Filing reference number",
                        ["SubmissionDate"] = "Date of submission",
                        ["TaxLiability"] = "Tax amount due",
                        ["PaymentDueDate"] = "Payment due date",
                        ["AssociateName"] = "Name of the assigned associate"
                    })
                },

                // Payment Reminder
                new MessageTemplate
                {
                    TemplateCode = "PAYMENT_REMINDER",
                    Name = "Payment Reminder",
                    Description = "Remind client about upcoming payment",
                    Subject = "Payment Reminder - {TaxType} Due {DueDate}",
                    Body = @"Dear {ClientFirstName} {ClientLastName},

This is a reminder that your {TaxType} payment is due on {DueDate}.

Payment Details:
- Amount Due: SLE {AmountDue}
- Tax Period: {TaxYear}
- Days Until Due: {DaysUntilDue}

Payment Options:
- Orange Money: 076-123456
- Africell Money: 088-123456
- Bank Transfer: Account details in your portal
- In-person at our office

Late payments may incur penalties and interest charges.

Best regards,
The Betts Firm",
                    Category = MessageCategory.Payment,
                    AvailableVariables = JsonSerializer.Serialize(new Dictionary<string, string>
                    {
                        ["ClientFirstName"] = "Client's first name",
                        ["ClientLastName"] = "Client's last name",
                        ["TaxType"] = "Type of tax",
                        ["DueDate"] = "Payment due date",
                        ["AmountDue"] = "Amount to be paid",
                        ["TaxYear"] = "Tax year/period",
                        ["DaysUntilDue"] = "Number of days until due date"
                    })
                },

                // Deadline Approaching
                new MessageTemplate
                {
                    TemplateCode = "DEADLINE_WARNING",
                    Name = "Deadline Warning",
                    Description = "Warn about approaching deadline",
                    Subject = "URGENT: {DeadlineType} Deadline - {DaysRemaining} Days",
                    Body = @"Dear {ClientFirstName} {ClientLastName},

URGENT REMINDER: You have an important deadline approaching.

Deadline: {DeadlineType}
Due Date: {DueDate}
Days Remaining: {DaysRemaining}

Outstanding Requirements:
{OutstandingItems}

Please take immediate action to avoid penalties.

If you need assistance, please contact us immediately.

Best regards,
The Betts Firm",
                    Category = MessageCategory.Deadline,
                    AvailableVariables = JsonSerializer.Serialize(new Dictionary<string, string>
                    {
                        ["ClientFirstName"] = "Client's first name",
                        ["ClientLastName"] = "Client's last name",
                        ["DeadlineType"] = "Type of deadline",
                        ["DueDate"] = "Due date",
                        ["DaysRemaining"] = "Days until deadline",
                        ["OutstandingItems"] = "List of outstanding items"
                    })
                },

                // Welcome Message
                new MessageTemplate
                {
                    TemplateCode = "WELCOME",
                    Name = "Welcome Message",
                    Description = "Welcome new client to the system",
                    Subject = "Welcome to The Betts Firm Client Portal",
                    Body = @"Dear {ClientFirstName} {ClientLastName},

Welcome to The Betts Firm's Client Tax Information System!

Your account has been successfully created. Here are your next steps:

1. Complete your business profile
2. Upload required registration documents
3. Set up your tax calendar preferences

Your assigned tax associate is {AssociateName}, who will be your primary point of contact.

Portal Access:
- Username: {Username}
- Portal URL: https://ctis.bettsfirmsl.com

If you have any questions, please don't hesitate to reach out.

Best regards,
The Betts Firm Team",
                    Category = MessageCategory.General,
                    AvailableVariables = JsonSerializer.Serialize(new Dictionary<string, string>
                    {
                        ["ClientFirstName"] = "Client's first name",
                        ["ClientLastName"] = "Client's last name",
                        ["AssociateName"] = "Name of assigned associate",
                        ["Username"] = "Client's username/email"
                    })
                },

                // Compliance Update
                new MessageTemplate
                {
                    TemplateCode = "COMPLIANCE_UPDATE",
                    Name = "Compliance Status Update",
                    Description = "Update client on compliance status",
                    Subject = "Compliance Status Update - {ComplianceStatus}",
                    Body = @"Dear {ClientFirstName} {ClientLastName},

Your tax compliance status has been updated.

Current Status: {ComplianceStatus}
Compliance Score: {ComplianceScore}%

{StatusDetails}

Next Actions:
{NextActions}

Maintaining good compliance helps avoid penalties and ensures smooth business operations.

Best regards,
{AssociateName}
The Betts Firm",
                    Category = MessageCategory.Compliance,
                    AvailableVariables = JsonSerializer.Serialize(new Dictionary<string, string>
                    {
                        ["ClientFirstName"] = "Client's first name",
                        ["ClientLastName"] = "Client's last name",
                        ["ComplianceStatus"] = "Current compliance status",
                        ["ComplianceScore"] = "Compliance percentage",
                        ["StatusDetails"] = "Details about the status",
                        ["NextActions"] = "Recommended next actions",
                        ["AssociateName"] = "Name of assigned associate"
                    })
                }
            };

            context.MessageTemplates.AddRange(templates);
            await context.SaveChangesAsync();
        }
    }
}