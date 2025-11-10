import { apiClient } from '@/lib/api-client';

// Types for client portal API responses
export interface PaginatedResponse<T> {
  items: T[];
  totalCount: number;
  page: number;
  pageSize: number;
  totalPages: number;
  hasNextPage: boolean;
  hasPreviousPage: boolean;
}

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
  verificationStatus?: 'NotRequested' | 'Requested' | 'Submitted' | 'UnderReview' | 'Rejected' | 'Verified' | 'Filed';
  verificationNotes?: string;
  rejectionReason?: string;
  reviewedAt?: string;
  reviewedBy?: string;
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
  id?: string; // For UI compatibility
  amount: number;
  paymentMethod: string;
  method?: string; // Alias for paymentMethod
  status: string;
  reference: string;
  paymentReference?: string; // Alias for reference
  createdAt: string;
  paymentDate?: string; // Alias for createdAt
  approvedAt?: string;
  taxFilingId?: number;
  taxType?: string;
  taxYear?: number;
  transactionId?: string;
  receiptNumber?: string;
  notes?: string;
  feeAmount?: number;
  currency?: string;
  exchangeRate?: number;
  originalAmount?: number;
  originalCurrency?: string;
  processedDate?: string;
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
  registrationDate: string;
}

export interface CreateClientTaxFilingDto {
  taxYear: number;
  taxType: string;
  grossIncome?: number;
  deductions?: number;
  dueDate: string;
  taxLiability: number;
  filingReference?: string;
}

export interface CreateClientPaymentDto {
  amount: number;
  paymentMethod: string;
  paymentReference: string;
  paymentDate: string;
  taxFilingId?: number;
  description?: string;
}

// Message types for in-app messaging system
export interface Message {
  messageId: number;
  parentMessageId?: number;
  senderId: string;
  senderName: string;
  senderEmail: string;
  senderRole: string;
  recipientId: string;
  recipientName: string;
  recipientEmail: string;
  recipientRole: string;
  clientId?: number;
  clientName?: string;
  clientNumber?: number;
  taxFilingId?: number;
  taxFilingReference?: string;
  documentId?: number;
  documentName?: string;
  subject: string;
  body: string;
  status: 'Sent' | 'Delivered' | 'Read' | 'Archived';
  priority: 'Low' | 'Normal' | 'High' | 'Urgent';
  category: 'General' | 'Document' | 'TaxFiling' | 'Payment' | 'Compliance' | 'Support';
  sentDate: string;
  deliveredDate?: string;
  readDate?: string;
  isStarred: boolean;
  isArchived: boolean;
  hasAttachments: boolean;
  isSystemMessage: boolean;
  replyCount: number;
  replies: Message[];
  attachments: ClientDocument[];
}

export interface SendMessageDto {
  recipientId: string;
  clientId?: number;
  taxFilingId?: number;
  documentId?: number;
  parentMessageId?: number;
  subject: string;
  body: string;
  priority?: 'Low' | 'Normal' | 'High' | 'Urgent';
  category?: 'General' | 'Document' | 'TaxFiling' | 'Payment' | 'Compliance' | 'Support';
  attachmentIds?: number[];
}

export interface MessageReplyDto {
  body: string;
  attachmentIds?: number[];
}

export interface MessageThread {
  rootMessage: Message;
  totalReplies: number;
  participants: string[];
  lastActivityDate: string;
  hasUnreadMessages: boolean;
}

export interface MessageFolder {
  folderName: string;
  unreadCount: number;
  totalCount: number;
}

export interface MessageSearchDto {
  searchTerm?: string;
  category?: 'General' | 'Document' | 'TaxFiling' | 'Payment' | 'Compliance' | 'Support';
  priority?: 'Low' | 'Normal' | 'High' | 'Urgent';
  status?: 'Sent' | 'Delivered' | 'Read' | 'Archived';
  clientId?: number;
  taxFilingId?: number;
  fromDate?: string;
  toDate?: string;
  isStarred?: boolean;
  hasAttachments?: boolean;
  senderId?: string;
  recipientId?: string;
  page?: number;
  pageSize?: number;
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
      totalPages: response.data.pagination.totalPages,
      hasNextPage: response.data.pagination.currentPage < response.data.pagination.totalPages,
      hasPreviousPage: response.data.pagination.currentPage > 1
    };
  },

  /**
   * Upload a document
   */
  uploadDocument: async (formData: FormData): Promise<ClientDocument> => {
    const response = await apiClient.post<{ success: boolean; data: ClientDocument }>(
      '/api/client-portal/documents/upload',
      formData,
      { isFormData: true }
    );
    return response.data.data;
  },

  /**
   * Download a document
   */
  downloadDocument: async (documentId: number): Promise<Blob> => {
    const response = await apiClient.get<Blob>(
      `/api/client-portal/documents/${documentId}/download`,
      { responseType: 'blob' }
    );
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
      totalPages: response.data.pagination.totalPages,
      hasNextPage: response.data.pagination.currentPage < response.data.pagination.totalPages,
      hasPreviousPage: response.data.pagination.currentPage > 1
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
      totalPages: response.data.pagination.totalPages,
      hasNextPage: response.data.pagination.currentPage < response.data.pagination.totalPages,
      hasPreviousPage: response.data.pagination.currentPage > 1
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
  },

  // ===== MESSAGING METHODS =====

  /**
   * Send a message to an associate
   */
  sendMessage: async (messageData: SendMessageDto): Promise<Message> => {
    const response = await apiClient.post<{ messageId: number }>('/api/messages/send', messageData);
    return response.data as Message;
  },

  /**
   * Get a specific message
   */
  getMessage: async (messageId: number): Promise<Message> => {
    const response = await apiClient.get<Message>(`/api/messages/${messageId}`);
    return response.data;
  },

  /**
   * Reply to a message
   */
  replyToMessage: async (messageId: number, replyData: MessageReplyDto): Promise<Message> => {
    const response = await apiClient.post<Message>(`/api/messages/${messageId}/reply`, replyData);
    return response.data;
  },

  /**
   * Mark a message as read
   */
  markMessageAsRead: async (messageId: number): Promise<void> => {
    await apiClient.put(`/api/messages/${messageId}/read`);
  },

  /**
   * Toggle star status of a message
   */
  toggleMessageStar: async (messageId: number): Promise<void> => {
    await apiClient.put(`/api/messages/${messageId}/star`);
  },

  /**
   * Archive a message
   */
  archiveMessage: async (messageId: number): Promise<void> => {
    await apiClient.put(`/api/messages/${messageId}/archive`);
  },

  /**
   * Delete a message
   */
  deleteMessage: async (messageId: number): Promise<void> => {
    await apiClient.delete(`/api/messages/${messageId}`);
  },

  /**
   * Get message thread
   */
  getMessageThread: async (messageId: number): Promise<MessageThread> => {
    const response = await apiClient.get<MessageThread>(`/api/messages/thread/${messageId}`);
    return response.data;
  },

  /**
   * Get inbox messages
   */
  getInbox: async (page: number = 1, pageSize: number = 20): Promise<PaginatedResponse<Message>> => {
    const response = await apiClient.get<{
      data: Message[];
      pagination: {
        currentPage: number;
        pageSize: number;
        totalCount: number;
        totalPages: number;
      };
    }>(`/api/messages/inbox?page=${page}&pageSize=${pageSize}`);

    return {
      items: response.data.data,
      ...response.data.pagination,
      page: response.data.pagination.currentPage,
      totalPages: response.data.pagination.totalPages,
      hasNextPage: response.data.pagination.currentPage < response.data.pagination.totalPages,
      hasPreviousPage: response.data.pagination.currentPage > 1
    };
  },

  /**
   * Get sent messages
   */
  getSentMessages: async (page: number = 1, pageSize: number = 20): Promise<PaginatedResponse<Message>> => {
    const response = await apiClient.get<{
      data: Message[];
      pagination: {
        currentPage: number;
        pageSize: number;
        totalCount: number;
        totalPages: number;
      };
    }>(`/api/messages/sent?page=${page}&pageSize=${pageSize}`);

    return {
      items: response.data.data,
      ...response.data.pagination,
      page: response.data.pagination.currentPage,
      totalPages: response.data.pagination.totalPages,
      hasNextPage: response.data.pagination.currentPage < response.data.pagination.totalPages,
      hasPreviousPage: response.data.pagination.currentPage > 1
    };
  },

  /**
   * Get archived messages
   */
  getArchivedMessages: async (page: number = 1, pageSize: number = 20): Promise<PaginatedResponse<Message>> => {
    const response = await apiClient.get<{
      data: Message[];
      pagination: {
        currentPage: number;
        pageSize: number;
        totalCount: number;
        totalPages: number;
      };
    }>(`/api/messages/archived?page=${page}&pageSize=${pageSize}`);

    return {
      items: response.data.data,
      ...response.data.pagination,
      page: response.data.pagination.currentPage,
      totalPages: response.data.pagination.totalPages,
      hasNextPage: response.data.pagination.currentPage < response.data.pagination.totalPages,
      hasPreviousPage: response.data.pagination.currentPage > 1
    };
  },

  /**
   * Get starred messages
   */
  getStarredMessages: async (page: number = 1, pageSize: number = 20): Promise<PaginatedResponse<Message>> => {
    const response = await apiClient.get<{
      data: Message[];
      pagination: {
        currentPage: number;
        pageSize: number;
        totalCount: number;
        totalPages: number;
      };
    }>(`/api/messages/starred?page=${page}&pageSize=${pageSize}`);

    return {
      items: response.data.data,
      ...response.data.pagination,
      page: response.data.pagination.currentPage,
      totalPages: response.data.pagination.totalPages,
      hasNextPage: response.data.pagination.currentPage < response.data.pagination.totalPages,
      hasPreviousPage: response.data.pagination.currentPage > 1
    };
  },

  /**
   * Search messages
   */
  searchMessages: async (searchData: MessageSearchDto): Promise<PaginatedResponse<Message>> => {
    const response = await apiClient.post<{
      data: Message[];
      pagination: {
        currentPage: number;
        pageSize: number;
        totalCount: number;
        totalPages: number;
      };
    }>('/api/messages/search', searchData);

    return {
      items: response.data.data,
      ...response.data.pagination,
      page: response.data.pagination.currentPage,
      totalPages: response.data.pagination.totalPages,
      hasNextPage: response.data.pagination.currentPage < response.data.pagination.totalPages,
      hasPreviousPage: response.data.pagination.currentPage > 1
    };
  },

  /**
   * Get messages for a specific client (associate view)
   */
  getClientMessages: async (clientId: number): Promise<Message[]> => {
    const response = await apiClient.get<Message[]>(`/api/messages/client/${clientId}`);
    return response.data;
  },

  /**
   * Get messages for a specific tax filing
   */
  getTaxFilingMessages: async (taxFilingId: number): Promise<Message[]> => {
    const response = await apiClient.get<Message[]>(`/api/messages/tax-filing/${taxFilingId}`);
    return response.data;
  },

  /**
   * Get folder counts
   */
  getFolderCounts: async (): Promise<MessageFolder[]> => {
    const response = await apiClient.get<MessageFolder[]>('/api/messages/folders');
    return response.data;
  },

  /**
   * Get unread message count
   */
  getUnreadCount: async (): Promise<{ count: number }> => {
    const response = await apiClient.get<{ count: number }>('/api/messages/unread-count');
    return response.data;
  },

  /**
   * Get message templates (for associates)
   */
  getMessageTemplates: async (category?: string): Promise<any[]> => {
    const categoryParam = category ? `?category=${category}` : '';
    const response = await apiClient.get<any[]>(`/api/messages/templates${categoryParam}`);
    return response.data;
  },

  /**
   * Get unread notifications
   */
  getUnreadNotifications: async (limit: number = 10): Promise<any[]> => {
    const response = await apiClient.get<any[]>(`/api/messages/notifications?limit=${limit}`);
    return response.data;
  },

  /**
   * Get message statistics
   */
  getMessageStatistics: async (fromDate?: string): Promise<any> => {
    const dateParam = fromDate ? `?fromDate=${fromDate}` : '';
    const response = await apiClient.get<any>(`/api/messages/statistics${dateParam}`);
    return response.data;
  }
};