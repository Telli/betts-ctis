/**
 * Enhanced Document service for the BettsTax backend
 */

import { apiClient } from '../api-client';

/**
 * Create empty document data for new users or when API is unavailable
 */
const createEmptyDocumentData = (): DocumentDto[] => [];

const createEmptyDocumentStats = (): DocumentStats => ({
  total: 0,
  pending: 0,
  verified: 0,
  rejected: 0,
  processed: 0,
  totalSize: 0,
  byCategory: {},
  byStatus: {}
});

export interface DocumentDto {
  id: string;
  documentId?: number;
  clientId: number;
  taxYearId?: number;
  filename: string;
  originalName: string;
  fileSize: number;
  contentType: string;
  uploadDate: string;
  status: 'pending' | 'verified' | 'rejected' | 'processed';
  category: 'tax-return' | 'financial-statement' | 'supporting-document' | 'receipt' | 'correspondence';
  tags: string[];
  description?: string;
  uploadedBy: string;
  clientName?: string;
  taxYear?: number;
  verifiedBy?: string;
  verifiedDate?: string;
  rejectionReason?: string;
}

export interface DocumentFilters {
  status?: 'pending' | 'verified' | 'rejected' | 'processed' | 'all';
  category?: 'tax-return' | 'financial-statement' | 'supporting-document' | 'receipt' | 'correspondence' | 'all';
  clientId?: number;
  taxYear?: number;
  uploadDateFrom?: string;
  uploadDateTo?: string;
  search?: string;
}

export interface DocumentStats {
  total: number;
  pending: number;
  verified: number;
  rejected: number;
  processed: number;
  totalSize: number;
  byCategory: Record<string, number>;
  byStatus: Record<string, number>;
}

export type DocumentUploadCategory = 'tax-return' | 'financial-statement' | 'supporting-document' | 'receipt' | 'correspondence' | 'invoice' | 'payment-evidence' | 'bank-statement'

export interface DocumentUploadRequest {
  file: File;
  category: DocumentUploadCategory;
  description?: string;
  tags?: string[];
  taxYearId?: number;
  taxFilingId?: number;
}

export const DocumentService = {
  /**
   * Upload a document for a client
   */
  upload: async (clientId: number, uploadRequest: DocumentUploadRequest): Promise<DocumentDto> => {
    const formData = new FormData();
    formData.append('file', uploadRequest.file);

    // Map frontend category to backend DocumentCategory enum
    const categoryMap: Record<string, string> = {
      'tax-return': 'TaxReturn',
      'financial-statement': 'FinancialStatement',
      'receipt': 'Receipt',
      'invoice': 'Invoice',
      'payment-evidence': 'PaymentEvidence',
      'bank-statement': 'BankStatement',
      'supporting-document': 'Other',
      'correspondence': 'Other',
    };

    const serverCategory = categoryMap[uploadRequest.category] || 'Other';

    // Backend expects PascalCase keys due to DTO binder
    formData.append('ClientId', clientId.toString());
    if (uploadRequest.taxYearId) formData.append('TaxYearId', uploadRequest.taxYearId.toString());
    if (uploadRequest.taxFilingId) formData.append('TaxFilingId', uploadRequest.taxFilingId.toString());
    formData.append('Category', serverCategory);
    if (uploadRequest.description) formData.append('Description', uploadRequest.description);

    const response = await apiClient.post<{ success: boolean; data: DocumentDto }>(
      `/api/documents/upload`,
      formData,
      { isFormData: true }
    );
    return response.data.data;
  },

  /**
   * Upload a document with progress callback (client-side only)
   */
  uploadWithProgress: async (
    clientId: number,
    uploadRequest: DocumentUploadRequest,
    onProgress: (percent: number) => void
  ): Promise<DocumentDto> => {
    const formData = new FormData();
    formData.append('file', uploadRequest.file);

    const categoryMap: Record<string, string> = {
      'tax-return': 'TaxReturn',
      'financial-statement': 'FinancialStatement',
      'receipt': 'Receipt',
      'invoice': 'Invoice',
      'payment-evidence': 'PaymentEvidence',
      'bank-statement': 'BankStatement',
      'supporting-document': 'Other',
      'correspondence': 'Other',
    };

    const serverCategory = categoryMap[uploadRequest.category] || 'Other';
    formData.append('ClientId', clientId.toString());
    if (uploadRequest.taxYearId) formData.append('TaxYearId', uploadRequest.taxYearId.toString());
    if (uploadRequest.taxFilingId) formData.append('TaxFilingId', uploadRequest.taxFilingId.toString());
    formData.append('Category', serverCategory);
    if (uploadRequest.description) formData.append('Description', uploadRequest.description);

    const RAW_API_BASE_URL = process.env.NEXT_PUBLIC_API_URL || 'http://localhost:5001';
    const buildAbsoluteUrl = (endpoint: string): string => {
      try {
        const base = RAW_API_BASE_URL;
        if (base.startsWith('http://') || base.startsWith('https://')) {
          return new URL(endpoint, base.endsWith('/') ? base : base + '/').toString();
        }
        if (typeof window !== 'undefined') {
          const originBase = new URL(base, window.location.origin);
          return new URL(endpoint, originBase.toString().endsWith('/') ? originBase.toString() : originBase.toString() + '/').toString();
        }
        return `${base}${endpoint}`;
      } catch {
        return `${RAW_API_BASE_URL}${endpoint}`;
      }
    };

    const url = buildAbsoluteUrl('/api/documents/upload');

    return await new Promise<DocumentDto>((resolve, reject) => {
      const xhr = new XMLHttpRequest();
      xhr.open('POST', url, true);
      xhr.withCredentials = true;
      xhr.upload.onprogress = (event: ProgressEvent) => {
        if (event.lengthComputable) {
          const percent = Math.round((event.loaded / event.total) * 100);
          try { onProgress(percent); } catch {}
        }
      };
      xhr.onreadystatechange = () => {
        if (xhr.readyState === 4) {
          if (xhr.status >= 200 && xhr.status < 300) {
            try {
              const parsed = JSON.parse(xhr.responseText);
              resolve(parsed.data as DocumentDto);
            } catch (e) {
              // Some backends may return raw DTO
              try {
                resolve(JSON.parse(xhr.responseText) as DocumentDto);
              } catch (err) {
                reject(err);
              }
            }
          } else if (xhr.status === 401) {
            reject(new Error('Unauthorized'));
          } else {
            try {
              const err = JSON.parse(xhr.responseText);
              reject(new Error(err?.message || `Upload failed: ${xhr.status}`));
            } catch {
              reject(new Error(`Upload failed: ${xhr.status}`));
            }
          }
        }
      };
      xhr.onerror = () => reject(new Error('Network error during upload'));
      xhr.send(formData);
    });
  },

  /**
   * Get documents with filtering
   */
  getDocuments: async (filters: DocumentFilters = {}): Promise<DocumentDto[]> => {
    try {
      const params = new URLSearchParams();
      if (filters.status && filters.status !== 'all') params.append('status', filters.status);
      if (filters.category && filters.category !== 'all') params.append('category', filters.category);
      if (filters.clientId) params.append('clientId', filters.clientId.toString());
      if (filters.taxYear) params.append('taxYear', filters.taxYear.toString());
      if (filters.uploadDateFrom) params.append('uploadDateFrom', filters.uploadDateFrom);
      if (filters.uploadDateTo) params.append('uploadDateTo', filters.uploadDateTo);
      if (filters.search) params.append('search', filters.search);

      const response = await apiClient.get<{ success: boolean; data: DocumentDto[] }>(
        `/api/documents?${params.toString()}`
      );
      return response.data.data;
    } catch (error) {
      console.warn('Documents API unavailable, returning empty data for new user experience:', error);
      return createEmptyDocumentData();
    }
  },

  /**
   * Get all documents for a client
   */
  getClientDocuments: async (clientId: number): Promise<DocumentDto[]> => {
    // Backend doesn't have client-specific document endpoint yet
    // Use the generic getDocuments with clientId filter
    try {
      return await DocumentService.getDocuments({ clientId });
    } catch (error) {
      console.warn('Failed to fetch client documents, returning empty array:', error);
      return [];
    }
  },

  /**
   * Get documents for a specific tax filing
   */
  getDocumentsByFiling: async (taxFilingId: number): Promise<DocumentDto[]> => {
    try {
      const response = await apiClient.get<{ success: boolean; data: DocumentDto[] }>(
        `/api/documents/tax-filing/${taxFilingId}`
      );
      // Backend response is wrapped { success, data }
      return response.data?.data ?? [];
    } catch (error) {
      console.warn('Failed to fetch tax filing documents, returning empty array:', error);
      return [];
    }
  },

  /**
   * Get document statistics
   */
  getDocumentStats: async (clientId?: number): Promise<DocumentStats> => {
    try {
      const url = clientId 
        ? `/api/documents/stats?clientId=${clientId}`
        : '/api/documents/stats';
      
      const response = await apiClient.get<{ success: boolean; data: DocumentStats }>(url);
      return response.data.data;
    } catch (error) {
      console.warn('Document stats API unavailable, returning empty data for new user experience:', error);
      return createEmptyDocumentStats();
    }
  },

  /**
   * Get single document details
   */
  getDocument: async (documentId: string): Promise<DocumentDto> => {
    const response = await apiClient.get<{ success: boolean; data: DocumentDto }>(
      `/api/documents/${documentId}`
    );
    return response.data.data;
  },

  /**
   * Update document metadata
   */
  updateDocument: async (documentId: string, updates: Partial<DocumentDto>): Promise<DocumentDto> => {
    const response = await apiClient.put<{ success: boolean; data: DocumentDto }>(
      `/api/documents/${documentId}`,
      updates
    );
    return response.data.data;
  },

  /**
   * Verify document (admin/associate action)
   */
  verifyDocument: async (documentId: string, notes?: string): Promise<DocumentDto> => {
    const response = await apiClient.put<{ success: boolean; data: DocumentDto }>(
      `/api/documents/${documentId}/verify`,
      { notes }
    );
    return response.data.data;
  },

  /**
   * Reject document (admin/associate action)
   */
  rejectDocument: async (documentId: string, reason: string): Promise<DocumentDto> => {
    const response = await apiClient.put<{ success: boolean; data: DocumentDto }>(
      `/api/documents/${documentId}/reject`,
      { reason }
    );
    return response.data.data;
  },

  /**
   * Download document
   */
  downloadDocument: async (documentId: string): Promise<Blob> => {
    const response = await apiClient.get<Blob>(
      `/api/documents/${documentId}/download`,
      { responseType: 'blob' }
    );
    return response.data as Blob;
  },

  /**
   * Replace the binary file of an existing document
   */
  replace: async (
    documentId: string,
    request: { file: File; description?: string; taxYearId?: number; taxFilingId?: number }
  ): Promise<DocumentDto> => {
    const formData = new FormData();
    formData.append('file', request.file);
    if (request.taxYearId) formData.append('TaxYearId', request.taxYearId.toString());
    if (request.taxFilingId) formData.append('TaxFilingId', request.taxFilingId.toString());
    if (request.description) formData.append('Description', request.description);

    const response = await apiClient.put<{ success: boolean; data: DocumentDto }>(
      `/api/documents/${documentId}/replace`,
      formData,
      { isFormData: true }
    );
    return response.data.data;
  },

  /**
   * Delete a document
   */
  delete: async (documentId: string): Promise<void> => {
    await apiClient.delete(`/api/documents/${documentId}`);
  },

  /**
   * Bulk delete documents
   */
  bulkDelete: async (documentIds: string[]): Promise<void> => {
    await apiClient.post('/api/documents/bulk-delete', { documentIds });
  },

  /**
   * Get available document categories
   */
  getDocumentCategories: async (): Promise<Array<{
    value: string;
    label: string;
    description: string;
    requiredForTaxTypes: string[];
  }>> => {
    const response = await apiClient.get<{ 
      success: boolean; 
      data: Array<{
        value: string;
        label: string;
        description: string;
        requiredForTaxTypes: string[];
      }> 
    }>('/api/documents/categories');
    return response.data.data;
  },

  /**
   * Get document requirements for a tax type
   */
  getDocumentRequirements: async (
    taxType: string,
    clientCategory: string
  ): Promise<Array<{
    category: string;
    required: boolean;
    description: string;
    acceptedFormats: string[];
    maxSizeMb: number;
  }>> => {
    const response = await apiClient.get<Array<Record<string, any>>>(
      `/api/documentverification/requirements?taxType=${taxType}&category=${clientCategory}`
    );

    return (response.data || []).map((req) => {
      const acceptedRaw = req.acceptedFormats ?? req.AcceptedFormats ?? [];
      const acceptedList = Array.isArray(acceptedRaw)
        ? acceptedRaw
        : typeof acceptedRaw === 'string'
          ? acceptedRaw.split(',').map((f: string) => f.trim()).filter(Boolean)
          : [];
      // Ensure dot-prefixed extensions for file inputs
      const acceptedFormats = acceptedList.map((f: string) => f.startsWith('.') ? f.toLowerCase() : `.${f.toLowerCase()}`);

      const maxSizeCandidate = req.maxSize ?? req.MaxSize ?? req.maxFileSize ?? req.MaxFileSize ?? req.maxFileSizeInBytes ?? req.MaxFileSizeInBytes;
      let maxSizeMb = 15;
      if (typeof maxSizeCandidate === 'number') {
        // Treat large values as bytes and smaller ones as already in MB
        maxSizeMb = maxSizeCandidate > 1024 ? Math.max(1, Math.round((maxSizeCandidate / 1024 / 1024) * 10) / 10) : maxSizeCandidate;
      }

      return {
        // Prefer DocumentType when available, then RequirementCode
        category: req.documentType ?? req.DocumentType ?? req.requirementCode ?? req.RequirementCode ?? 'supporting-document',
        required: Boolean(req.isRequired ?? req.IsRequired ?? req.required ?? req.Required),
        description: req.description ?? req.Description ?? '',
        acceptedFormats,
        maxSizeMb,
      };
    });
  },

  /**
   * Search documents
   */
  searchDocuments: async (query: string, filters: DocumentFilters = {}): Promise<DocumentDto[]> => {
    const params = new URLSearchParams();
    params.append('q', query);
    if (filters.status && filters.status !== 'all') params.append('status', filters.status);
    if (filters.category && filters.category !== 'all') params.append('category', filters.category);
    if (filters.clientId) params.append('clientId', filters.clientId.toString());

    const response = await apiClient.get<{ success: boolean; data: DocumentDto[] }>(
      `/api/documents/search?${params.toString()}`
    );
    return response.data.data;
  }
};
