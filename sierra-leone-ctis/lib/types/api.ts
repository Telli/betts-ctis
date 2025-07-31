// API Response Types for BettsTax Sierra Leone

export interface ApiResponse<T = any> {
  success: boolean
  data?: T
  message?: string
  error?: string
  errors?: Record<string, string[]>
}

export interface PaginatedResponse<T> extends ApiResponse<T[]> {
  pagination: {
    page: number
    pageSize: number
    totalPages: number
    totalItems: number
    hasNext: boolean
    hasPrevious: boolean
  }
}

export interface ApiError {
  message: string
  statusCode: number
  details?: any
}

// Common filter and sorting types
export interface BaseFilter {
  page?: number
  pageSize?: number
  sortBy?: string
  sortOrder?: 'asc' | 'desc'
  search?: string
}

export interface DateRangeFilter {
  dateFrom?: string
  dateTo?: string
}

// Sierra Leone specific types
export interface SierraLeoneApiContext {
  nraCompliance: boolean
  financeAct2025: boolean
  currency: 'SLE'
  timezone: 'Africa/Freetown'
}

// Authentication and authorization
export interface AuthToken {
  token: string
  refreshToken: string
  expiresAt: string
  tokenType: 'Bearer'
}

export interface UserPermissions {
  canView: boolean
  canCreate: boolean
  canEdit: boolean
  canDelete: boolean
  canApprove: boolean
  canExport: boolean
}

// File upload types
export interface FileUploadResponse {
  fileId: string
  fileName: string
  fileSize: number
  contentType: string
  uploadUrl?: string
  downloadUrl?: string
}

// Validation error types
export interface ValidationError {
  field: string
  message: string
  code?: string
  value?: any
}

export interface FieldValidationErrors {
  [fieldName: string]: string[]
}