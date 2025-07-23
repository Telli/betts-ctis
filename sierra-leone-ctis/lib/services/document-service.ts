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

export interface DocumentUploadRequest {
  file: File;
  category: 'tax-return' | 'financial-statement' | 'supporting-document' | 'receipt' | 'correspondence';
  description?: string;
  tags?: string[];
  taxYearId?: number;
}

export const DocumentService = {
  /**
   * Upload a document for a client
   */
  upload: async (clientId: number, uploadRequest: DocumentUploadRequest): Promise<DocumentDto> => {
    const formData = new FormData();
    formData.append('file', uploadRequest.file);
    formData.append('category', uploadRequest.category);
    
    if (uploadRequest.description) {
      formData.append('description', uploadRequest.description);
    }
    
    if (uploadRequest.tags && uploadRequest.tags.length > 0) {
      formData.append('tags', JSON.stringify(uploadRequest.tags));
    }
    
    if (uploadRequest.taxYearId) {
      formData.append('taxYearId', uploadRequest.taxYearId.toString());
    }

    const response = await apiClient.post<{ success: boolean; data: DocumentDto }>(
      `/api/clients/${clientId}/documents`,
      formData,
      {
        headers: {
          'Content-Type': 'multipart/form-data',
        },
      }
    );
    return response.data.data;
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
    const response = await apiClient.get<{ success: boolean; data: DocumentDto[] }>(
      `/api/clients/${clientId}/documents`
    );
    return response.data.data;
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
    const response = await apiClient.get(`/api/documents/${documentId}/download`);
    return response.data as Blob;
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
  getDocumentRequirements: async (taxType: string, clientCategory: string): Promise<Array<{
    category: string;
    required: boolean;
    description: string;
    acceptedFormats: string[];
    maxSize: number;
  }>> => {
    const response = await apiClient.get<{ 
      success: boolean; 
      data: Array<{
        category: string;
        required: boolean;
        description: string;
        acceptedFormats: string[];
        maxSize: number;
      }> 
    }>(`/api/documents/requirements?taxType=${taxType}&clientCategory=${clientCategory}`);
    return response.data.data;
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
