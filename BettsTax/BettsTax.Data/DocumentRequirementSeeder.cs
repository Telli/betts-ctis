using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace BettsTax.Data
{
    public static class DocumentRequirementSeeder
    {
        public static async Task SeedDocumentRequirementsAsync(IServiceProvider serviceProvider)
        {
            using var scope = serviceProvider.CreateScope();
            var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            
            // Check if already seeded
            if (await context.DocumentRequirements.AnyAsync())
                return;

            var requirements = new List<DocumentRequirement>
            {
                // Personal Income Tax Requirements
                new()
                {
                    RequirementCode = "PIT_PAYSLIPS",
                    Name = "Employment Pay Slips",
                    Description = "Monthly pay slips or P60 for the tax year",
                    ApplicableTaxType = TaxType.IncomeTax,
                    IsRequired = true,
                    DisplayOrder = 1,
                    AcceptedFormats = "pdf,jpg,jpeg,png",
                    MaxFileSizeInBytes = 10485760,
                    MinimumQuantity = 12,
                    MaximumQuantity = 24,
                    RequiredMonthsOfData = 12
                },
                new()
                {
                    RequirementCode = "PIT_BANK_STATEMENTS",
                    Name = "Bank Statements",
                    Description = "Bank statements for all accounts for the tax year",
                    ApplicableTaxType = TaxType.IncomeTax,
                    IsRequired = true,
                    DisplayOrder = 2,
                    AcceptedFormats = "pdf,csv,xls,xlsx",
                    MaxFileSizeInBytes = 20971520,
                    MinimumQuantity = 12,
                    MaximumQuantity = 36,
                    RequiredMonthsOfData = 12
                },
                new()
                {
                    RequirementCode = "PIT_INVESTMENT_INCOME",
                    Name = "Investment Income Statements",
                    Description = "Dividend certificates, interest statements, capital gains reports",
                    ApplicableTaxType = TaxType.IncomeTax,
                    IsRequired = false,
                    DisplayOrder = 3,
                    AcceptedFormats = "pdf,jpg,jpeg,png",
                    MaxFileSizeInBytes = 10485760,
                    MinimumQuantity = 1,
                    MaximumQuantity = 50
                },
                new()
                {
                    RequirementCode = "PIT_RENTAL_INCOME",
                    Name = "Rental Income Documentation",
                    Description = "Rental agreements, receipts, and expense records",
                    ApplicableTaxType = TaxType.IncomeTax,
                    IsRequired = false,
                    DisplayOrder = 4,
                    AcceptedFormats = "pdf,jpg,jpeg,png,doc,docx",
                    MaxFileSizeInBytes = 10485760,
                    MinimumQuantity = 1,
                    MaximumQuantity = 20
                },
                new()
                {
                    RequirementCode = "PIT_TAX_CLEARANCE",
                    Name = "Previous Year Tax Clearance Certificate",
                    Description = "Tax clearance certificate from the previous tax year",
                    ApplicableTaxType = TaxType.IncomeTax,
                    IsRequired = true,
                    DisplayOrder = 5,
                    AcceptedFormats = "pdf",
                    MaxFileSizeInBytes = 5242880,
                    MinimumQuantity = 1,
                    MaximumQuantity = 1
                },

                // Corporate Income Tax Requirements
                new()
                {
                    RequirementCode = "CIT_FINANCIAL_STATEMENTS",
                    Name = "Audited Financial Statements",
                    Description = "Complete audited financial statements including balance sheet, income statement, cash flow statement, and notes",
                    ApplicableTaxType = TaxType.IncomeTax,
                    ApplicableTaxpayerCategory = TaxpayerCategory.Large,
                    IsRequired = true,
                    DisplayOrder = 1,
                    AcceptedFormats = "pdf",
                    MaxFileSizeInBytes = 52428800,
                    MinimumQuantity = 1,
                    MaximumQuantity = 1
                },
                new()
                {
                    RequirementCode = "CIT_TRIAL_BALANCE",
                    Name = "General Ledger and Trial Balance",
                    Description = "Detailed general ledger and trial balance for the tax year",
                    ApplicableTaxType = TaxType.IncomeTax,
                    ApplicableTaxpayerCategory = TaxpayerCategory.Large,
                    IsRequired = true,
                    DisplayOrder = 2,
                    AcceptedFormats = "pdf,xls,xlsx,csv",
                    MaxFileSizeInBytes = 52428800,
                    MinimumQuantity = 1,
                    MaximumQuantity = 2
                },
                new()
                {
                    RequirementCode = "CIT_DIRECTORS_REMUNERATION",
                    Name = "Directors' Remuneration Schedule",
                    Description = "Detailed schedule of all directors' remuneration, benefits, and allowances",
                    ApplicableTaxType = TaxType.IncomeTax,
                    ApplicableTaxpayerCategory = TaxpayerCategory.Large,
                    IsRequired = true,
                    DisplayOrder = 3,
                    AcceptedFormats = "pdf,xls,xlsx",
                    MaxFileSizeInBytes = 10485760,
                    MinimumQuantity = 1,
                    MaximumQuantity = 1
                },
                new()
                {
                    RequirementCode = "CIT_DEPRECIATION",
                    Name = "Depreciation Schedules",
                    Description = "Fixed asset register and depreciation schedules",
                    ApplicableTaxType = TaxType.IncomeTax,
                    ApplicableTaxpayerCategory = TaxpayerCategory.Large,
                    IsRequired = true,
                    DisplayOrder = 4,
                    AcceptedFormats = "pdf,xls,xlsx",
                    MaxFileSizeInBytes = 20971520,
                    MinimumQuantity = 1,
                    MaximumQuantity = 2
                },
                new()
                {
                    RequirementCode = "CIT_COMPANY_REGISTRATION",
                    Name = "Company Registration Certificate",
                    Description = "Certificate of incorporation and business registration documents",
                    ApplicableTaxType = TaxType.IncomeTax,
                    ApplicableTaxpayerCategory = TaxpayerCategory.Large,
                    IsRequired = true,
                    DisplayOrder = 5,
                    AcceptedFormats = "pdf,jpg,jpeg,png",
                    MaxFileSizeInBytes = 5242880,
                    MinimumQuantity = 1,
                    MaximumQuantity = 5
                },

                // GST Requirements
                new()
                {
                    RequirementCode = "GST_SALES_INVOICES",
                    Name = "Sales Invoices and Records",
                    Description = "All sales invoices for the GST period",
                    ApplicableTaxType = TaxType.GST,
                    IsRequired = true,
                    DisplayOrder = 1,
                    AcceptedFormats = "pdf,xls,xlsx,csv,zip",
                    MaxFileSizeInBytes = 104857600,
                    MinimumQuantity = 1,
                    MaximumQuantity = 1000
                },
                new()
                {
                    RequirementCode = "GST_PURCHASE_INVOICES",
                    Name = "Purchase Invoices and Receipts",
                    Description = "All purchase invoices and receipts for GST input claims",
                    ApplicableTaxType = TaxType.GST,
                    IsRequired = true,
                    DisplayOrder = 2,
                    AcceptedFormats = "pdf,xls,xlsx,csv,zip",
                    MaxFileSizeInBytes = 104857600,
                    MinimumQuantity = 1,
                    MaximumQuantity = 1000
                },
                new()
                {
                    RequirementCode = "GST_IMPORT_DOCS",
                    Name = "Import Documentation",
                    Description = "Customs declarations and import documents for GST purposes",
                    ApplicableTaxType = TaxType.GST,
                    IsRequired = false,
                    DisplayOrder = 3,
                    AcceptedFormats = "pdf,jpg,jpeg,png",
                    MaxFileSizeInBytes = 20971520,
                    MinimumQuantity = 1,
                    MaximumQuantity = 100
                },
                new()
                {
                    RequirementCode = "GST_EXPORT_DOCS",
                    Name = "Export Documentation",
                    Description = "Export declarations and shipping documents for zero-rated supplies",
                    ApplicableTaxType = TaxType.GST,
                    IsRequired = false,
                    DisplayOrder = 4,
                    AcceptedFormats = "pdf,jpg,jpeg,png",
                    MaxFileSizeInBytes = 20971520,
                    MinimumQuantity = 1,
                    MaximumQuantity = 100
                },
                new()
                {
                    RequirementCode = "GST_BANK_RECONCILIATION",
                    Name = "Bank Reconciliation Statements",
                    Description = "Bank reconciliation for the GST period",
                    ApplicableTaxType = TaxType.GST,
                    IsRequired = true,
                    DisplayOrder = 5,
                    AcceptedFormats = "pdf,xls,xlsx",
                    MaxFileSizeInBytes = 10485760,
                    MinimumQuantity = 1,
                    MaximumQuantity = 3
                },

                // PAYE Requirements
                new()
                {
                    RequirementCode = "PAYE_PAYROLL",
                    Name = "Employee Payroll Records",
                    Description = "Monthly payroll records showing gross pay, PAYE deductions, and net pay",
                    ApplicableTaxType = TaxType.PayrollTax,
                    IsRequired = true,
                    DisplayOrder = 1,
                    AcceptedFormats = "pdf,xls,xlsx,csv",
                    MaxFileSizeInBytes = 52428800,
                    MinimumQuantity = 1,
                    MaximumQuantity = 12
                },
                new()
                {
                    RequirementCode = "PAYE_NASSIT",
                    Name = "NASSIT Contributions Schedule",
                    Description = "National Social Security and Insurance Trust contribution records",
                    ApplicableTaxType = TaxType.PayrollTax,
                    IsRequired = true,
                    DisplayOrder = 2,
                    AcceptedFormats = "pdf,xls,xlsx",
                    MaxFileSizeInBytes = 20971520,
                    MinimumQuantity = 1,
                    MaximumQuantity = 12
                },
                new()
                {
                    RequirementCode = "PAYE_EMPLOYEE_CONTRACTS",
                    Name = "Employee Contracts",
                    Description = "Employment contracts for new employees during the period",
                    ApplicableTaxType = TaxType.PayrollTax,
                    IsRequired = false,
                    DisplayOrder = 3,
                    AcceptedFormats = "pdf,doc,docx",
                    MaxFileSizeInBytes = 10485760,
                    MinimumQuantity = 1,
                    MaximumQuantity = 100
                },
                new()
                {
                    RequirementCode = "PAYE_TERMINATION_DOCS",
                    Name = "Employee Termination Documentation",
                    Description = "Termination letters and final settlement calculations",
                    ApplicableTaxType = TaxType.PayrollTax,
                    IsRequired = false,
                    DisplayOrder = 4,
                    AcceptedFormats = "pdf,doc,docx",
                    MaxFileSizeInBytes = 10485760,
                    MinimumQuantity = 1,
                    MaximumQuantity = 50
                },

                // General Requirements (All Tax Types)
                new()
                {
                    RequirementCode = "GENERAL_TIN_CERT",
                    Name = "TIN Certificate",
                    Description = "Tax Identification Number certificate from SRA",
                    ApplicableTaxType = null, // Applies to all
                    IsRequired = true,
                    DisplayOrder = 100,
                    AcceptedFormats = "pdf,jpg,jpeg,png",
                    MaxFileSizeInBytes = 5242880,
                    MinimumQuantity = 1,
                    MaximumQuantity = 1
                },
                new()
                {
                    RequirementCode = "GENERAL_NATIONAL_ID",
                    Name = "National Identification",
                    Description = "National ID card or passport for individual taxpayers",
                    ApplicableTaxType = null, // Applies to all
                    IsRequired = true,
                    DisplayOrder = 101,
                    AcceptedFormats = "pdf,jpg,jpeg,png",
                    MaxFileSizeInBytes = 5242880,
                    MinimumQuantity = 1,
                    MaximumQuantity = 2
                }
            };

            context.DocumentRequirements.AddRange(requirements);
            await context.SaveChangesAsync();
        }
    }
}