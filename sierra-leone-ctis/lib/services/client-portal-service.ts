import { apiClient } from '@/lib/api-client';

// Types for client portal API responses
export interface ClientDashboardData {
  businessInfo: {
    clientId: number;
    businessName: string;
    contactPerson: string;
    email: string;
    phoneNumber: string;
    tin: string;
    taxpayerCategory: string;
    clientType: string;
    status: string;
  };
  complianceOverview: {
    totalFilings: number;
    completedFilings: number;
    pendingFilings: number;
    lateFilings: number;
    complianceScore: number;
    complianceStatus: string;
    taxTypeBreakdown: Record<string, number>;
    monthlyPayments: Record<string, number>;
  };
  recentActivity: Array<{
    id: number;
    type: string;
    action: string;
    description: string;
    entityName: string;
    timestamp: string;
  }>;
  upcomingDeadlines: Array<{
    id: number;
    title: string;
    description: string;
    dueDate: string;
    type: string;
    isUrgent: boolean;
    daysRemaining: number;
  }>;
  quickActions: {
    canUploadDocuments: boolean;
    canSubmitTaxFiling: boolean;
    canMakePayment: boolean;
    hasPendingFilings: boolean;
    hasOverduePayments: boolean;
    pendingDocumentCount: number;
    upcomingDeadlineCount: number;
  };
}

export interface ClientDocument {
  documentId: number;
  fileName: string;
  originalFileName: string;
  documentType: string;
  description: string;
  uploadedAt: string;
  fileSize: number;
  taxYear?: number;
}

export interface ClientTaxFiling {
  taxFilingId: number;
  taxYear: number;
  taxType: string;
  status: string;
  grossIncome: number;
  deductions: number;
  taxLiability: number;
  filingDate?: string;
  dueDate?: string;
}

export interface ClientPayment {
  paymentId: number;
  amount: number;
  paymentMethod: string;
  status: string;
  reference: string;
  createdAt: string;
  approvedAt?: string;
  taxFilingId?: number;
}

export interface ClientProfile {
  clientId: number;
  businessName: string;
  contactPerson: string;
  email: string;
  phoneNumber: string;
  address: string;
  tin?: string;
  taxpayerCategory: string;
  clientType: string;
  status: string;
}

export interface CreateClientTaxFilingDto {
  taxType: 'IncomeTax' | 'GST' | 'PayrollTax' | 'ExciseDuty';
  taxYear: number;
  dueDate: string;
  taxLiability: number;
  filingReference?: string;
}

export interface CreateClientPaymentDto {
  taxFilingId?: number;
  amount: number;
  method: 'BankTransfer' | 'Cash' | 'Check' | 'OnlinePayment' | 'MobileMoney';
  paymentReference: string;
  paymentDate: string;
}

export interface PaginatedResponse<T> {
  items: T[];
  totalCount: number;
  page: number;
  pageSize: number;
  totalPages: number;
}

export const ClientPortalService = {
  /**
   * Get client dashboard data
   */
  getDashboard: async (): Promise<ClientDashboardData> => {
    const response = await apiClient.get<{ success: boolean; data: ClientDashboardData }>('/api/client-portal/dashboard');
    return response.data.data;
  },

  /**
   * Get client documents with pagination
   */
  getDocuments: async (page: number = 1, pageSize: number = 20): Promise<PaginatedResponse<ClientDocument>> => {
    const response = await apiClient.get<{ 
      success: boolean; 
      data: ClientDocument[];
      pagination: {
        currentPage: number;
        pageSize: number;
        totalCount: number;
        totalPages: number;
      };
    }>(`/api/client-portal/documents?page=${page}&pageSize=${pageSize}`);
    
    return {
      items: response.data.data,
      ...response.data.pagination,
      page: response.data.pagination.currentPage,
      totalPages: response.data.pagination.totalPages
    };
  },

  /**
   * Upload a document
   */
  uploadDocument: async (formData: FormData): Promise<ClientDocument> => {
    const response = await apiClient.post<{ success: boolean; data: ClientDocument }>('/api/client-portal/documents/upload', formData, {
      headers: {
        'Content-Type': 'multipart/form-data',
      },
    });
    return response.data.data;
  },

  /**
   * Download a document
   */
  downloadDocument: async (documentId: number): Promise<Blob> => {
    const response = await apiClient.get(`/api/client-portal/documents/${documentId}/download`);
    return response.data as Blob;
  },

  /**
   * Get client tax filings with pagination
   */
  getTaxFilings: async (page: number = 1, pageSize: number = 20): Promise<PaginatedResponse<ClientTaxFiling>> => {
    const response = await apiClient.get<{
      success: boolean;
      data: ClientTaxFiling[];
      pagination: {
        currentPage: number;
        pageSize: number;
        totalCount: number;
        totalPages: number;
      };
    }>(`/api/client-portal/tax-filings?page=${page}&pageSize=${pageSize}`);
    
    return {
      items: response.data.data,
      ...response.data.pagination,
      page: response.data.pagination.currentPage,
      totalPages: response.data.pagination.totalPages
    };
  },

  /**
   * Get client payments with pagination
   */
  getPayments: async (page: number = 1, pageSize: number = 20): Promise<PaginatedResponse<ClientPayment>> => {
    const response = await apiClient.get<{
      success: boolean;
      data: ClientPayment[];
      pagination: {
        currentPage: number;
        pageSize: number;
        totalCount: number;
        totalPages: number;
      };
    }>(`/api/client-portal/payments?page=${page}&pageSize=${pageSize}`);
    
    return {
      items: response.data.data,
      ...response.data.pagination,
      page: response.data.pagination.currentPage,
      totalPages: response.data.pagination.totalPages
    };
  },

  /**
   * Get client profile
   */
  getProfile: async (): Promise<ClientProfile> => {
    const response = await apiClient.get<{ success: boolean; data: ClientProfile }>('/api/client-portal/profile');
    return response.data.data;
  },

  /**
   * Update client profile
   */
  updateProfile: async (profileData: Partial<ClientProfile>): Promise<ClientProfile> => {
    const response = await apiClient.put<{ success: boolean; data: ClientProfile }>('/api/client-portal/profile', profileData);
    return response.data.data;
  },

  /**
   * Get compliance overview
   */
  getCompliance: async (): Promise<ClientDashboardData['complianceOverview']> => {
    const response = await apiClient.get<{ success: boolean; data: ClientDashboardData['complianceOverview'] }>('/api/client-portal/compliance');
    return response.data.data;
  },

  /**
   * Get upcoming deadlines
   */
  getDeadlines: async (days: number = 30): Promise<ClientDashboardData['upcomingDeadlines']> => {
    const response = await apiClient.get<{ success: boolean; data: ClientDashboardData['upcomingDeadlines'] }>(`/api/client-portal/deadlines?days=${days}`);
    return response.data.data;
  },

  /**
   * Submit support request
   */
  submitSupportRequest: async (requestData: {
    subject: string;
    category: string;
    priority: string;
    description: string;
    attachments?: File[];
  }): Promise<{ id: number; message: string }> => {
    const formData = new FormData();
    formData.append('subject', requestData.subject);
    formData.append('category', requestData.category);
    formData.append('priority', requestData.priority);
    formData.append('description', requestData.description);
    
    if (requestData.attachments) {
      requestData.attachments.forEach((file, index) => {
        formData.append(`attachments[${index}]`, file);
      });
    }

    const response = await apiClient.post<{ success: boolean; data: { id: number; message: string } }>('/api/client-portal/support', formData, {
      headers: {
        'Content-Type': 'multipart/form-data',
      },
    });
    return response.data.data;
  },

  /**
   * Create a new tax filing for the current client
   */
  createTaxFiling: async (filingData: CreateClientTaxFilingDto): Promise<ClientTaxFiling> => {
    const response = await apiClient.post<{ success: boolean; data: ClientTaxFiling }>('/api/client-portal/tax-filings', filingData);
    return response.data.data;
  },

  /**
   * Create a new payment for the current client
   */
  createPayment: async (paymentData: CreateClientPaymentDto): Promise<ClientPayment> => {
    const response = await apiClient.post<{ success: boolean; data: ClientPayment }>('/api/client-portal/payments', paymentData);
    return response.data.data;
  }
};