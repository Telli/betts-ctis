import { apiClient } from '@/lib/api-client';

// DTOs and interfaces
export interface ClientDto {
  clientId: number;
  businessName: string;
  contactPerson: string;
  email: string;
  phoneNumber: string;
  tin: string;
  taxpayerCategory: string;
  clientType: string;
  annualTurnover: number;
  isActive: boolean;
  registrationDate: string;
}

export interface AssociateDto {
  id: string;
  firstName: string;
  lastName: string;
  email: string;
  phoneNumber?: string;
  isActive: boolean;
  registrationDate: string;
  clientCount?: number;
  permissionCount?: number;
}

export interface DelegationStatisticsDto {
  totalClients: number;
  totalPermissions: number;
  permissionsByArea: Record<string, number>;
  expiringPermissions: number;
  recentlyGranted: number;
}

export interface ClientConsentRequestDto {
  id: number;
  clientId: number;
  clientName: string;
  associateId: string;
  associateName: string;
  permissionAreas: string[];
  requestReason: string;
  requestDate: string;
  status: 'Pending' | 'Approved' | 'Rejected';
  responseDate?: string;
  responseReason?: string;
}

export interface ClientAccessLogDto {
  id: number;
  associateId: string;
  associateName: string;
  clientId: number;
  accessType: string;
  accessDate: string;
  ipAddress?: string;
  userAgent?: string;
}

export interface RequestConsentRequest {
  clientId: number;
  associateId: string;
  permissionAreas: string[];
  requestReason?: string;
}

export interface ProcessConsentRequest {
  approved: boolean;
  reason: string;
}

// Response types
export interface DelegatedClientListResponse {
  success: boolean;
  data: ClientDto[];
}

export interface AssociateListResponse {
  success: boolean;
  data: AssociateDto[];
}

export interface DelegationStatisticsResponse {
  success: boolean;
  data: DelegationStatisticsDto;
}

export interface ConsentRequestListResponse {
  success: boolean;
  data: ClientConsentRequestDto[];
  pagination?: {
    currentPage: number;
    pageSize: number;
    totalCount: number;
    totalPages: number;
  };
}

export interface AccessLogResponse {
  success: boolean;
  data: ClientAccessLogDto[];
  pagination?: {
    currentPage: number;
    pageSize: number;
    totalCount: number;
    totalPages: number;
  };
}

export const ClientDelegationService = {
  /**
   * Get clients available for delegation to an associate
   */
  getAvailableClientsForDelegation: async (associateId: string): Promise<DelegatedClientListResponse> => {
    const response = await apiClient.get<DelegatedClientListResponse>(
      `/api/client-delegation/associates/${associateId}/available-clients`
    );
    return response.data;
  },

  /**
   * Check if associate can access a specific client
   */
  canAccessClient: async (associateId: string, clientId: number): Promise<{ success: boolean; data: { canAccess: boolean } }> => {
    const response = await apiClient.get<{ success: boolean; data: { canAccess: boolean } }>(
      `/api/client-delegation/associates/${associateId}/clients/${clientId}/can-access`
    );
    return response.data;
  },

  /**
   * Get associates assigned to a specific client
   */
  getClientAssociates: async (clientId: number): Promise<AssociateListResponse> => {
    const response = await apiClient.get<AssociateListResponse>(
      `/api/client-delegation/clients/${clientId}/associates`
    );
    return response.data;
  },

  /**
   * Get all available associates for assignment
   */
  getAvailableAssociates: async (): Promise<AssociateListResponse> => {
    const response = await apiClient.get<AssociateListResponse>(
      '/api/client-delegation/available-associates'
    );
    return response.data;
  },

  /**
   * Request client consent for associate access
   */
  requestClientConsent: async (request: RequestConsentRequest): Promise<{ success: boolean; message: string }> => {
    const response = await apiClient.post<{ success: boolean; message: string }>(
      '/api/client-delegation/request-consent',
      request
    );
    return response.data;
  },

  /**
   * Process client consent response
   */
  processClientConsent: async (
    clientId: number,
    associateId: string,
    request: ProcessConsentRequest
  ): Promise<{ success: boolean; message: string }> => {
    const response = await apiClient.post<{ success: boolean; message: string }>(
      `/api/client-delegation/clients/${clientId}/associates/${associateId}/process-consent`,
      request
    );
    return response.data;
  },

  /**
   * Get pending consent requests for a client
   */
  getPendingConsentRequests: async (
    clientId: number,
    page: number = 1,
    pageSize: number = 20
  ): Promise<ConsentRequestListResponse> => {
    const params = new URLSearchParams({
      page: page.toString(),
      pageSize: pageSize.toString(),
    });

    const response = await apiClient.get<ConsentRequestListResponse>(
      `/api/client-delegation/clients/${clientId}/consent-requests?${params}`
    );
    return response.data;
  },

  /**
   * Get consent requests for an associate
   */
  getAssociateConsentRequests: async (
    associateId: string,
    page: number = 1,
    pageSize: number = 20
  ): Promise<ConsentRequestListResponse> => {
    const params = new URLSearchParams({
      page: page.toString(),
      pageSize: pageSize.toString(),
    });

    const response = await apiClient.get<ConsentRequestListResponse>(
      `/api/client-delegation/associates/${associateId}/consent-requests?${params}`
    );
    return response.data;
  },

  /**
   * Get delegation statistics for an associate
   */
  getDelegationStatistics: async (associateId: string): Promise<DelegationStatisticsResponse> => {
    const response = await apiClient.get<DelegationStatisticsResponse>(
      `/api/client-delegation/associates/${associateId}/statistics`
    );
    return response.data;
  },

  /**
   * Get recently accessed clients for an associate
   */
  getRecentAccessedClients: async (
    associateId: string,
    limit: number = 5
  ): Promise<DelegatedClientListResponse> => {
    const response = await apiClient.get<DelegatedClientListResponse>(
      `/api/client-delegation/associates/${associateId}/recent-clients?limit=${limit}`
    );
    return response.data;
  },

  /**
   * Log client access by associate
   */
  logClientAccess: async (
    associateId: string,
    clientId: number,
    accessType: string = 'General'
  ): Promise<{ success: boolean; message: string }> => {
    const response = await apiClient.post<{ success: boolean; message: string }>(
      `/api/client-delegation/associates/${associateId}/clients/${clientId}/log-access`,
      { accessType }
    );
    return response.data;
  },

  /**
   * Get client access log
   */
  getClientAccessLog: async (
    clientId: number,
    fromDate?: string,
    toDate?: string,
    page: number = 1,
    pageSize: number = 20
  ): Promise<AccessLogResponse> => {
    const params = new URLSearchParams({
      page: page.toString(),
      pageSize: pageSize.toString(),
    });

    if (fromDate) params.append('from', fromDate);
    if (toDate) params.append('to', toDate);

    const response = await apiClient.get<AccessLogResponse>(
      `/api/client-delegation/clients/${clientId}/access-log?${params}`
    );
    return response.data;
  },

  /**
   * Get associate access log
   */
  getAssociateAccessLog: async (
    associateId: string,
    fromDate?: string,
    toDate?: string,
    page: number = 1,
    pageSize: number = 20
  ): Promise<AccessLogResponse> => {
    const params = new URLSearchParams({
      page: page.toString(),
      pageSize: pageSize.toString(),
    });

    if (fromDate) params.append('from', fromDate);
    if (toDate) params.append('to', toDate);

    const response = await apiClient.get<AccessLogResponse>(
      `/api/client-delegation/associates/${associateId}/access-log?${params}`
    );
    return response.data;
  }
};

// Helper functions
export const DelegationHelpers = {
  /**
   * Format client name for display
   */
  formatClientName: (client: ClientDto): string => {
    return `${client.businessName} (${client.tin})`;
  },

  /**
   * Format associate name for display
   */
  formatAssociateName: (associate: AssociateDto): string => {
    return `${associate.firstName} ${associate.lastName}`;
  },

  /**
   * Get client type color for UI
   */
  getClientTypeColor: (clientType: string): string => {
    switch (clientType.toLowerCase()) {
      case 'individual':
        return 'bg-blue-100 text-blue-800';
      case 'business':
        return 'bg-green-100 text-green-800';
      case 'partnership':
        return 'bg-purple-100 text-purple-800';
      case 'corporation':
        return 'bg-orange-100 text-orange-800';
      default:
        return 'bg-gray-100 text-gray-800';
    }
  },

  /**
   * Get taxpayer category color for UI
   */
  getTaxpayerCategoryColor: (category: string): string => {
    switch (category.toLowerCase()) {
      case 'large':
        return 'bg-red-100 text-red-800';
      case 'medium':
        return 'bg-orange-100 text-orange-800';
      case 'small':
        return 'bg-yellow-100 text-yellow-800';
      case 'micro':
        return 'bg-green-100 text-green-800';
      default:
        return 'bg-gray-100 text-gray-800';
    }
  },

  /**
   * Format consent request status
   */
  formatConsentStatus: (status: string): string => {
    switch (status.toLowerCase()) {
      case 'pending':
        return '⏳ Pending';
      case 'approved':
        return '✅ Approved';
      case 'rejected':
        return '❌ Rejected';
      default:
        return status;
    }
  },

  /**
   * Get consent status color
   */
  getConsentStatusColor: (status: string): string => {
    switch (status.toLowerCase()) {
      case 'pending':
        return 'bg-yellow-100 text-yellow-800';
      case 'approved':
        return 'bg-green-100 text-green-800';
      case 'rejected':
        return 'bg-red-100 text-red-800';
      default:
        return 'bg-gray-100 text-gray-800';
    }
  },

  /**
   * Calculate delegation workload level
   */
  calculateWorkloadLevel: (clientCount: number): 'Low' | 'Medium' | 'High' | 'Very High' => {
    if (clientCount <= 5) return 'Low';
    if (clientCount <= 15) return 'Medium';
    if (clientCount <= 30) return 'High';
    return 'Very High';
  },

  /**
   * Get workload level color
   */
  getWorkloadLevelColor: (level: string): string => {
    switch (level.toLowerCase()) {
      case 'low':
        return 'bg-green-100 text-green-800';
      case 'medium':
        return 'bg-yellow-100 text-yellow-800';
      case 'high':
        return 'bg-orange-100 text-orange-800';
      case 'very high':
        return 'bg-red-100 text-red-800';
      default:
        return 'bg-gray-100 text-gray-800';
    }
  },

  /**
   * Format access type for display
   */
  formatAccessType: (accessType: string): string => {
    switch (accessType.toLowerCase()) {
      case 'general':
        return '👁️ View';
      case 'taxfiling':
        return '📊 Tax Filing';
      case 'payment':
        return '💰 Payment';
      case 'document':
        return '📄 Document';
      default:
        return `🔄 ${accessType}`;
    }
  },

  /**
   * Check if associate is overloaded
   */
  isAssociateOverloaded: (associate: AssociateDto): boolean => {
    const clientCount = associate.clientCount || 0;
    return clientCount > 25; // Threshold for overloaded associate
  },

  /**
   * Generate delegation summary text
   */
  generateDelegationSummary: (statistics: DelegationStatisticsDto): string => {
    const { totalClients, totalPermissions, expiringPermissions } = statistics;
    let summary = `Managing ${totalClients} client${totalClients !== 1 ? 's' : ''} with ${totalPermissions} permission${totalPermissions !== 1 ? 's' : ''}`;
    
    if (expiringPermissions > 0) {
      summary += `. ${expiringPermissions} permission${expiringPermissions !== 1 ? 's' : ''} expiring soon`;
    }
    
    return summary;
  }
};

export default ClientDelegationService;