import { apiClient } from '@/lib/api-client';

// DTOs and interfaces
export interface OnBehalfActionDto {
  id: number;
  associateId: string;
  associateName: string;
  clientId: number;
  clientName: string;
  action: string; // Create, Update, Delete, Submit, etc.
  entityType: string; // TaxFiling, Payment, Document, etc.
  entityId: number;
  oldValues?: string; // JSON string
  newValues?: string; // JSON string
  actionDate: string;
  reason?: string;
  clientNotified: boolean;
  clientNotificationDate?: string;
  ipAddress?: string;
  userAgent?: string;
}

export interface ActionStatisticsDto {
  totalActions: number;
  actionsByType: Record<string, number>;
  actionsByEntityType: Record<string, number>;
  actionsByClient: Record<string, number>;
  actionsPerDay: Record<string, number>;
  notificationsPending: number;
}

export interface LogActionRequest {
  clientId: number;
  action: string;
  entityType: string;
  entityId: number;
  oldValues?: any;
  newValues?: any;
  reason?: string;
}

// Response types
export interface OnBehalfActionListResponse {
  success: boolean;
  data: OnBehalfActionDto[];
  pagination?: {
    currentPage: number;
    pageSize: number;
    totalCount: number;
    totalPages: number;
  };
}

export interface ActionStatisticsResponse {
  success: boolean;
  data: ActionStatisticsDto;
}

export const OnBehalfActionService = {
  /**
   * Log an on-behalf action (typically called automatically by other services)
   */
  logAction: async (request: LogActionRequest): Promise<{ success: boolean; message: string }> => {
    const response = await apiClient.post<{ success: boolean; message: string }>(
      '/api/on-behalf-actions/log',
      request
    );
    return response.data;
  },

  /**
   * Get actions performed on behalf of a specific client
   */
  getClientActions: async (
    clientId: number,
    fromDate?: string,
    toDate?: string,
    page: number = 1,
    pageSize: number = 20
  ): Promise<OnBehalfActionListResponse> => {
    const params = new URLSearchParams({
      page: page.toString(),
      pageSize: pageSize.toString(),
    });

    if (fromDate) params.append('from', fromDate);
    if (toDate) params.append('to', toDate);

    const response = await apiClient.get<OnBehalfActionListResponse>(
      `/api/on-behalf-actions/clients/${clientId}?${params}`
    );
    return response.data;
  },

  /**
   * Get actions performed by a specific associate
   */
  getAssociateActions: async (
    associateId: string,
    fromDate?: string,
    toDate?: string,
    page: number = 1,
    pageSize: number = 20
  ): Promise<OnBehalfActionListResponse> => {
    const params = new URLSearchParams({
      page: page.toString(),
      pageSize: pageSize.toString(),
    });

    if (fromDate) params.append('from', fromDate);
    if (toDate) params.append('to', toDate);

    const response = await apiClient.get<OnBehalfActionListResponse>(
      `/api/on-behalf-actions/associates/${associateId}?${params}`
    );
    return response.data;
  },

  /**
   * Get actions performed on a specific entity
   */
  getEntityActions: async (
    entityType: string,
    entityId: number
  ): Promise<OnBehalfActionListResponse> => {
    const response = await apiClient.get<OnBehalfActionListResponse>(
      `/api/on-behalf-actions/entities/${entityType}/${entityId}`
    );
    return response.data;
  },

  /**
   * Get recent actions for an associate (for dashboard)
   */
  getRecentActions: async (
    associateId: string,
    limit: number = 10
  ): Promise<OnBehalfActionListResponse> => {
    const response = await apiClient.get<OnBehalfActionListResponse>(
      `/api/on-behalf-actions/associates/${associateId}/recent?limit=${limit}`
    );
    return response.data;
  },

  /**
   * Get action statistics for an associate
   */
  getActionStatistics: async (
    associateId: string,
    fromDate?: string,
    toDate?: string
  ): Promise<ActionStatisticsResponse> => {
    const params = new URLSearchParams();
    if (fromDate) params.append('from', fromDate);
    if (toDate) params.append('to', toDate);

    const response = await apiClient.get<ActionStatisticsResponse>(
      `/api/on-behalf-actions/associates/${associateId}/statistics?${params}`
    );
    return response.data;
  },

  /**
   * Get unnotified actions for a client
   */
  getUnnotifiedActions: async (clientId: number): Promise<OnBehalfActionListResponse> => {
    const response = await apiClient.get<OnBehalfActionListResponse>(
      `/api/on-behalf-actions/clients/${clientId}/unnotified`
    );
    return response.data;
  },

  /**
   * Mark an action as notified to client
   */
  markActionAsNotified: async (actionId: number): Promise<{ success: boolean; message: string }> => {
    const response = await apiClient.put<{ success: boolean; message: string }>(
      `/api/on-behalf-actions/${actionId}/notify`
    );
    return response.data;
  },

  /**
   * Bulk notify client of multiple actions
   */
  bulkNotifyClient: async (
    clientId: number,
    actionIds: number[]
  ): Promise<{ success: boolean; message: string }> => {
    const response = await apiClient.post<{ success: boolean; message: string }>(
      `/api/on-behalf-actions/clients/${clientId}/bulk-notify`,
      { actionIds }
    );
    return response.data;
  },

  /**
   * Notify client of a specific action
   */
  notifyClientOfAction: async (
    clientId: number,
    actionId: number
  ): Promise<{ success: boolean; message: string }> => {
    const response = await apiClient.post<{ success: boolean; message: string }>(
      `/api/on-behalf-actions/clients/${clientId}/actions/${actionId}/notify`
    );
    return response.data;
  }
};

// Helper functions
export const ActionHelpers = {
  /**
   * Format action type for display
   */
  formatActionType: (action: string): string => {
    switch (action.toLowerCase()) {
      case 'create':
        return '📝 Created';
      case 'update':
        return '✏️ Updated';
      case 'delete':
        return '🗑️ Deleted';
      case 'submit':
        return '📤 Submitted';
      case 'approve':
        return '✅ Approved';
      case 'reject':
        return '❌ Rejected';
      default:
        return `🔄 ${action}`;
    }
  },

  /**
   * Format entity type for display
   */
  formatEntityType: (entityType: string): string => {
    switch (entityType.toLowerCase()) {
      case 'taxfiling':
        return '📊 Tax Filing';
      case 'payment':
        return '💰 Payment';
      case 'document':
        return '📄 Document';
      case 'client':
        return '👤 Client';
      default:
        return entityType.replace(/([A-Z])/g, ' $1').trim();
    }
  },

  /**
   * Get action color for UI display
   */
  getActionColor: (action: string): string => {
    switch (action.toLowerCase()) {
      case 'create':
        return 'bg-green-100 text-green-800';
      case 'update':
        return 'bg-blue-100 text-blue-800';
      case 'delete':
        return 'bg-red-100 text-red-800';
      case 'submit':
        return 'bg-purple-100 text-purple-800';
      case 'approve':
        return 'bg-emerald-100 text-emerald-800';
      case 'reject':
        return 'bg-orange-100 text-orange-800';
      default:
        return 'bg-gray-100 text-gray-800';
    }
  },

  /**
   * Parse JSON values safely
   */
  parseValues: (jsonString?: string): any => {
    if (!jsonString) return null;
    try {
      return JSON.parse(jsonString);
    } catch {
      return jsonString;
    }
  },

  /**
   * Format date for display
   */
  formatActionDate: (dateString: string): string => {
    const date = new Date(dateString);
    const now = new Date();
    const diffInMs = now.getTime() - date.getTime();
    const diffInHours = diffInMs / (1000 * 60 * 60);
    const diffInDays = diffInHours / 24;

    if (diffInHours < 1) {
      const minutes = Math.floor(diffInMs / (1000 * 60));
      return `${minutes} minute${minutes !== 1 ? 's' : ''} ago`;
    } else if (diffInHours < 24) {
      const hours = Math.floor(diffInHours);
      return `${hours} hour${hours !== 1 ? 's' : ''} ago`;
    } else if (diffInDays < 7) {
      const days = Math.floor(diffInDays);
      return `${days} day${days !== 1 ? 's' : ''} ago`;
    } else {
      return date.toLocaleDateString();
    }
  },

  /**
   * Generate summary text for an action
   */
  generateActionSummary: (action: OnBehalfActionDto): string => {
    const entityDisplay = ActionHelpers.formatEntityType(action.entityType);
    const actionDisplay = action.action.toLowerCase();
    
    return `${actionDisplay} ${entityDisplay.toLowerCase()} for ${action.clientName}`;
  },

  /**
   * Check if action needs client notification
   */
  needsNotification: (action: OnBehalfActionDto): boolean => {
    return !action.clientNotified && 
           ['create', 'update', 'delete', 'submit'].includes(action.action.toLowerCase());
  },

  /**
   * Get notification priority based on action type
   */
  getNotificationPriority: (action: string): 'high' | 'medium' | 'low' => {
    switch (action.toLowerCase()) {
      case 'delete':
      case 'submit':
        return 'high';
      case 'update':
      case 'approve':
      case 'reject':
        return 'medium';
      default:
        return 'low';
    }
  }
};

export default OnBehalfActionService;