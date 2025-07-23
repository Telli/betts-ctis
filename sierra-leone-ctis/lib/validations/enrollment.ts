import { z } from 'zod';

// Client invitation schema (for associates)
export const inviteClientSchema = z.object({
  email: z
    .string()
    .min(1, 'Email address is required')
    .email('Please enter a valid email address'),
});

export type InviteClientFormData = z.infer<typeof inviteClientSchema>;

// Self-registration schema (public registration)
export const selfRegistrationSchema = z.object({
  email: z
    .string()
    .min(1, 'Email address is required')
    .email('Please enter a valid email address'),
});

export type SelfRegistrationFormData = z.infer<typeof selfRegistrationSchema>;

// Client registration schema (completing registration)
export const clientRegistrationSchema = z.object({
  email: z
    .string()
    .min(1, 'Email address is required')
    .email('Please enter a valid email address'),
  
  password: z
    .string()
    .min(8, 'Password must be at least 8 characters long')
    .regex(
      /^(?=.*[a-z])(?=.*[A-Z])(?=.*\d)/,
      'Password must contain at least one uppercase letter, one lowercase letter, and one number'
    ),
  
  confirmPassword: z
    .string()
    .min(1, 'Please confirm your password'),
  
  firstName: z
    .string()
    .min(2, 'First name must be at least 2 characters')
    .max(50, 'First name cannot exceed 50 characters')
    .regex(/^[a-zA-Z\s-']+$/, 'First name can only contain letters, spaces, hyphens, and apostrophes'),
  
  lastName: z
    .string()
    .min(2, 'Last name must be at least 2 characters')
    .max(50, 'Last name cannot exceed 50 characters')
    .regex(/^[a-zA-Z\s-']+$/, 'Last name can only contain letters, spaces, hyphens, and apostrophes'),
  
  businessName: z
    .string()
    .min(2, 'Business name must be at least 2 characters')
    .max(100, 'Business name cannot exceed 100 characters'),
  
  phoneNumber: z
    .string()
    .min(1, 'Phone number is required')
    .regex(
      /^[+]?[\d\s\-()]+$/,
      'Please enter a valid phone number'
    ),
  
  taxpayerCategory: z.enum(['Large', 'Medium', 'Small', 'Micro'], {
    required_error: 'Please select a taxpayer category',
  }),
  
  clientType: z.enum(['Individual', 'Partnership', 'Corporation', 'NGO'], {
    required_error: 'Please select a client type',
  }),
  
  // Optional fields
  taxpayerIdentificationNumber: z
    .string()
    .optional()
    .refine((val) => {
      if (!val || val.length === 0) return true;
      return val.length >= 5 && val.length <= 20;
    }, 'TIN must be between 5 and 20 characters'),
  
  businessAddress: z
    .string()
    .max(250, 'Business address cannot exceed 250 characters')
    .optional(),
  
  contactPersonName: z
    .string()
    .max(100, 'Contact person name cannot exceed 100 characters')
    .optional(),
  
  contactPersonPhone: z
    .string()
    .regex(/^[+]?[\d\s\-()]*$/, 'Please enter a valid phone number')
    .optional()
    .refine((val) => !val || val.length >= 7, 'Phone number must be at least 7 digits'),
  
  annualTurnover: z
    .number()
    .min(0, 'Annual turnover must be a positive number')
    .optional(),
  
  registrationToken: z.string().min(1, 'Registration token is required'),
}).refine((data) => data.password === data.confirmPassword, {
  message: "Passwords don't match",
  path: ['confirmPassword'],
});

export type ClientRegistrationFormData = z.infer<typeof clientRegistrationSchema>;

// Email verification schema
export const emailVerificationSchema = z.object({
  token: z.string().min(1, 'Verification token is required'),
});

export type EmailVerificationFormData = z.infer<typeof emailVerificationSchema>;

// Resend verification schema
export const resendVerificationSchema = z.object({
  email: z
    .string()
    .min(1, 'Email address is required')
    .email('Please enter a valid email address'),
});

export type ResendVerificationFormData = z.infer<typeof resendVerificationSchema>;

// Base schema without the refine for step validation
const baseClientRegistrationSchema = z.object({
  email: z
    .string()
    .min(1, 'Email address is required')
    .email('Please enter a valid email address'),
  
  password: z
    .string()
    .min(8, 'Password must be at least 8 characters long')
    .regex(
      /^(?=.*[a-z])(?=.*[A-Z])(?=.*\d)/,
      'Password must contain at least one uppercase letter, one lowercase letter, and one number'
    ),
  
  confirmPassword: z
    .string()
    .min(1, 'Please confirm your password'),
  
  firstName: z
    .string()
    .min(2, 'First name must be at least 2 characters')
    .max(50, 'First name cannot exceed 50 characters')
    .regex(/^[a-zA-Z\s-']+$/, 'First name can only contain letters, spaces, hyphens, and apostrophes'),
  
  lastName: z
    .string()
    .min(2, 'Last name must be at least 2 characters')
    .max(50, 'Last name cannot exceed 50 characters')
    .regex(/^[a-zA-Z\s-']+$/, 'Last name can only contain letters, spaces, hyphens, and apostrophes'),
  
  businessName: z
    .string()
    .min(2, 'Business name must be at least 2 characters')
    .max(100, 'Business name cannot exceed 100 characters'),
  
  phoneNumber: z
    .string()
    .min(1, 'Phone number is required')
    .regex(
      /^[+]?[\d\s\-()]+$/,
      'Please enter a valid phone number'
    ),
  
  taxpayerCategory: z.enum(['Large', 'Medium', 'Small', 'Micro'], {
    required_error: 'Please select a taxpayer category',
  }),
  
  clientType: z.enum(['Individual', 'Partnership', 'Corporation', 'NGO'], {
    required_error: 'Please select a client type',
  }),
  
  taxpayerIdentificationNumber: z
    .string()
    .optional()
    .refine((val) => {
      if (!val || val.length === 0) return true;
      return val.length >= 5 && val.length <= 20;
    }, 'TIN must be between 5 and 20 characters'),
  
  businessAddress: z
    .string()
    .max(250, 'Business address cannot exceed 250 characters')
    .optional(),
  
  contactPersonName: z
    .string()
    .max(100, 'Contact person name cannot exceed 100 characters')
    .optional(),
  
  contactPersonPhone: z
    .string()
    .regex(/^[+]?[\d\s\-()]*$/, 'Please enter a valid phone number')
    .optional()
    .refine((val) => !val || val.length >= 7, 'Phone number must be at least 7 digits'),
  
  annualTurnover: z
    .number()
    .min(0, 'Annual turnover must be a positive number')
    .optional(),
  
  registrationToken: z.string().min(1, 'Registration token is required'),
});

// Form step validation for multi-step registration
export const personalInfoSchema = baseClientRegistrationSchema.pick({
  firstName: true,
  lastName: true,
  email: true,
  phoneNumber: true,
});

export const passwordSchema = baseClientRegistrationSchema.pick({
  password: true,
  confirmPassword: true,
});

export const businessInfoSchema = baseClientRegistrationSchema.pick({
  businessName: true,
  businessAddress: true,
  clientType: true,
});

export const taxInfoSchema = baseClientRegistrationSchema.pick({
  taxpayerCategory: true,
  taxpayerIdentificationNumber: true,
  annualTurnover: true,
  contactPersonName: true,
  contactPersonPhone: true,
});

export type PersonalInfoFormData = z.infer<typeof personalInfoSchema>;
export type PasswordFormData = z.infer<typeof passwordSchema>;
export type BusinessInfoFormData = z.infer<typeof businessInfoSchema>;
export type TaxInfoFormData = z.infer<typeof taxInfoSchema>;

// Validation messages
export const validationMessages = {
  required: 'This field is required',
  email: 'Please enter a valid email address',
  minLength: (min: number) => `Must be at least ${min} characters`,
  maxLength: (max: number) => `Cannot exceed ${max} characters`,
  passwordMatch: "Passwords don't match",
  phoneInvalid: 'Please enter a valid phone number',
  tinInvalid: 'TIN must be between 5 and 20 characters',
  positiveNumber: 'Must be a positive number',
  businessName: 'Business name is required for tax purposes',
  taxpayerCategory: 'Please select your business size category',
  clientType: 'Please select your business type',
} as const;