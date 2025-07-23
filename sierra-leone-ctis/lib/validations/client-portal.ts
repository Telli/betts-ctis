import { z } from "zod"

// Client profile update validation
export const clientProfileSchema = z.object({
  businessName: z.string()
    .min(2, "Business name must be at least 2 characters")
    .max(100, "Business name must not exceed 100 characters"),
  contactPerson: z.string()
    .min(2, "Contact person name must be at least 2 characters")
    .max(50, "Contact person name must not exceed 50 characters"),
  email: z.string()
    .email("Please enter a valid email address"),
  phoneNumber: z.string()
    .min(8, "Phone number must be at least 8 characters")
    .max(20, "Phone number must not exceed 20 characters")
    .regex(/^[+]?[\d\s\-()]+$/, "Please enter a valid phone number"),
  address: z.string()
    .min(10, "Address must be at least 10 characters")
    .max(200, "Address must not exceed 200 characters"),
  tin: z.string()
    .optional()
    .refine((val) => !val || val.length >= 10, {
      message: "TIN must be at least 10 characters if provided"
    })
})

export type ClientProfileFormData = z.infer<typeof clientProfileSchema>

// Document upload validation
export const documentUploadSchema = z.object({
  file: z.instanceof(File)
    .refine((file) => file.size <= 10 * 1024 * 1024, "File size must be less than 10MB")
    .refine(
      (file) => [
        'application/pdf',
        'image/jpeg',
        'image/jpg', 
        'image/png',
        'application/msword',
        'application/vnd.openxmlformats-officedocument.wordprocessingml.document',
        'application/vnd.ms-excel',
        'application/vnd.openxmlformats-officedocument.spreadsheetml.sheet'
      ].includes(file.type),
      "File must be a PDF, image, Word document, or Excel file"
    ),
  documentType: z.enum([
    "tax_return",
    "financial_statement", 
    "supporting_document",
    "receipt",
    "other"
  ], {
    required_error: "Please select a document type"
  }),
  description: z.string()
    .min(3, "Description must be at least 3 characters")
    .max(200, "Description must not exceed 200 characters"),
  taxYear: z.number()
    .int("Tax year must be a whole number")
    .min(2020, "Tax year must be 2020 or later")
    .max(new Date().getFullYear() + 1, "Tax year cannot be more than one year in the future")
    .optional()
})

export type DocumentUploadFormData = z.infer<typeof documentUploadSchema>

// Support request validation
export const supportRequestSchema = z.object({
  subject: z.string()
    .min(5, "Subject must be at least 5 characters")
    .max(100, "Subject must not exceed 100 characters"),
  category: z.enum([
    "technical_issue",
    "tax_question",
    "account_access",
    "document_upload",
    "payment_inquiry",
    "general_inquiry"
  ], {
    required_error: "Please select a category"
  }),
  priority: z.enum(["low", "medium", "high"], {
    required_error: "Please select a priority level"
  }),
  description: z.string()
    .min(20, "Description must be at least 20 characters")
    .max(1000, "Description must not exceed 1000 characters"),
  attachments: z.array(z.instanceof(File))
    .max(3, "Maximum 3 attachments allowed")
    .optional()
})

export type SupportRequestFormData = z.infer<typeof supportRequestSchema>

// Payment request validation
export const paymentRequestSchema = z.object({
  amount: z.number()
    .positive("Amount must be greater than 0")
    .max(1000000, "Amount cannot exceed 1,000,000 SLE"),
  taxFilingId: z.number()
    .int("Tax filing ID must be a whole number")
    .positive("Please select a valid tax filing"),
  paymentMethod: z.enum([
    "bank_transfer",
    "mobile_money",
    "cash_deposit",
    "check"
  ], {
    required_error: "Please select a payment method"
  }),
  reference: z.string()
    .min(5, "Reference must be at least 5 characters")
    .max(50, "Reference must not exceed 50 characters"),
  notes: z.string()
    .max(500, "Notes must not exceed 500 characters")
    .optional()
})

export type PaymentRequestFormData = z.infer<typeof paymentRequestSchema>

// Tax filing submission validation
export const taxFilingSubmissionSchema = z.object({
  taxYear: z.number()
    .int("Tax year must be a whole number")
    .min(2020, "Tax year must be 2020 or later")
    .max(new Date().getFullYear(), "Tax year cannot be in the future"),
  taxType: z.enum([
    "income_tax",
    "gst",
    "payroll_tax", 
    "excise_duty",
    "corporate_tax"
  ], {
    required_error: "Please select a tax type"
  }),
  grossIncome: z.number()
    .nonnegative("Gross income cannot be negative")
    .max(100000000, "Gross income cannot exceed 100,000,000 SLE"),
  deductions: z.number()
    .nonnegative("Deductions cannot be negative")
    .max(100000000, "Deductions cannot exceed 100,000,000 SLE"),
  taxLiability: z.number()
    .nonnegative("Tax liability cannot be negative")
    .max(100000000, "Tax liability cannot exceed 100,000,000 SLE"),
  supportingDocuments: z.array(z.instanceof(File))
    .min(1, "At least one supporting document is required")
    .max(10, "Maximum 10 supporting documents allowed"),
  declaration: z.boolean()
    .refine((val) => val === true, {
      message: "You must agree to the declaration"
    })
})

export type TaxFilingSubmissionFormData = z.infer<typeof taxFilingSubmissionSchema>

// Form validation helpers
export const getFieldError = (
  errors: any,
  fieldName: string
): string | undefined => {
  return errors?.[fieldName]?.message
}

export const hasFieldError = (
  errors: any,
  fieldName: string
): boolean => {
  return !!errors?.[fieldName]
}