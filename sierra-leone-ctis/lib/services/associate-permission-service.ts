import { apiClient } from '@/lib/api-client';

// Enums matching backend
export enum AssociatePermissionLevel {
  None = 0,
  Read = 1,
  Create = 2,
  Update = 4,
  Delete = 8,
  Submit = 16,
  Approve = 32,
  All = 63 // Read | Create | Update | Delete | Submit | Approve
}

// DTOs and interfaces
export interface AssociateClientPermissionDto {
  id: number;
  associateId: string;
  clientId: number;
  clientName: string;
  permissionArea: string;
  level: AssociatePermissionLevel;
  grantedDate: string;
  expiryDate?: string;
  grantedByAdminId: string;
  grantedByAdminName: string;
  isActive: boolean;
  notes?: string;
  amountThreshold?: number;
  requiresApproval: boolean;
}

export interface GrantPermissionRequest {
  associateId: string;
  clientIds: number[];
  permissionArea: string;
  level: AssociatePermissionLevel;
  expiryDate?: string;
  amountThreshold?: number;
  requiresApproval?: boolean;
  notes?: string;
}

export interface BulkPermissionRequest {
  associateIds: string[];
  clientIds: number[];
  rules: PermissionRule[];
  expiryDate?: string;
  notes?: string;
}

export interface PermissionRule {
  permissionArea: string;
  level: AssociatePermissionLevel;
  amountThreshold?: number;
  requiresApproval: boolean;
}

export interface AssociatePermissionAuditLogDto {
  id: number;
  associateId: string;
  associateName: string;
  clientId: number;
  clientName: string;
  action: string; // Grant, Revoke, Update, Expire
  permissionArea: string;
  oldLevel?: AssociatePermissionLevel;
  newLevel?: AssociatePermissionLevel;
  changedByAdminId: string;
  changedByAdminName: string;
  changeDate: string;
  reason: string;
}

export interface ClientSummaryDto {
  clientId: number;
  clientName: string;
  businessName: string;
  permissionAreas: string[];
  effectivePermissions: Record<string, AssociatePermissionLevel>;
  lastAccessDate?: string;
}

// Response types
export interface AssociatePermissionListResponse {
  success: boolean;
  data: AssociateClientPermissionDto[];
  pagination?: {
    currentPage: number;
    pageSize: number;
    totalCount: number;
    totalPages: number;
  };
}

export interface AssociatePermissionResponse {
  success: boolean;
  data: AssociateClientPermissionDto;
}

export interface ClientListResponse {
  success: boolean;
  data: ClientSummaryDto[];
}

export interface AuditLogResponse {
  success: boolean;
  data: AssociatePermissionAuditLogDto[];
  pagination?: {
    currentPage: number;
    pageSize: number;
    totalCount: number;
    totalPages: number;
  };
}

export interface PermissionCheckResponse {
  success: boolean;
  data: {
    hasPermission: boolean;
    effectiveLevel: AssociatePermissionLevel;
    restrictions?: {
      amountThreshold?: number;
      requiresApproval: boolean;
      expiryDate?: string;
    };
  };
}

export const AssociatePermissionService = {
  /**
   * Get all permissions for a specific associate
   */
  getAssociatePermissions: async (
    associateId: string,
    page: number = 1,
    pageSize: number = 20
  ): Promise<AssociatePermissionListResponse> => {
    const params = new URLSearchParams({
      page: page.toString(),
      pageSize: pageSize.toString(),
    });

    const response = await apiClient.get<AssociatePermissionListResponse>(
      `/api/associate-permissions/${associateId}?${params}`
    );
    return response.data;
  },

  /**
   * Check if associate has specific permission for client
   */
  checkPermission: async (
    associateId: string,
    clientId: number,
    area: string,
    level: AssociatePermissionLevel
  ): Promise<PermissionCheckResponse> => {
    const response = await apiClient.get<PermissionCheckResponse>(
      `/api/associate-permissions/${associateId}/check?clientId=${clientId}&area=${area}&level=${level}`
    );
    return response.data;
  },

  /**
   * Get clients delegated to associate for specific area
   */
  getDelegatedClients: async (
    associateId: string,
    area?: string
  ): Promise<ClientListResponse> => {
    const params = new URLSearchParams();
    if (area) params.append('area', area);

    const response = await apiClient.get<ClientListResponse>(
      `/api/associate-permissions/${associateId}/clients?${params}`
    );
    return response.data;
  },

  /**
   * Grant permission to associate for specific clients
   */
  grantPermission: async (request: GrantPermissionRequest): Promise<{ success: boolean; message: string }> => {
    const response = await apiClient.post<{ success: boolean; message: string }>(
      '/api/associate-permissions/grant',
      request
    );
    return response.data;
  },

  /**
   * Revoke permission from associate
   */
  revokePermission: async (
    associateId: string,
    clientId: number,
    area: string,
    reason?: string
  ): Promise<{ success: boolean; message: string }> => {
    const response = await apiClient.delete<{ success: boolean; message: string }>(
      `/api/associate-permissions/${associateId}/clients/${clientId}/areas/${area}${reason ? `?reason=${encodeURIComponent(reason)}` : ''}`
    );
    return response.data;
  },

  /**
   * Bulk grant permissions to multiple associates
   */
  bulkGrantPermissions: async (request: BulkPermissionRequest): Promise<{ success: boolean; message: string }> => {
    const response = await apiClient.post<{ success: boolean; message: string }>(
      '/api/associate-permissions/bulk-grant',
      request
    );
    return response.data;
  },

  /**
   * Bulk revoke permissions
   */
  bulkRevokePermissions: async (
    permissionIds: number[],
    reason?: string
  ): Promise<{ success: boolean; message: string }> => {
    const response = await apiClient.post<{ success: boolean; message: string }>(
      '/api/associate-permissions/bulk-revoke',
      { permissionIds, reason }
    );
    return response.data;
  },

  /**
   * Set expiry date for permission
   */
  setPermissionExpiry: async (
    permissionId: number,
    expiryDate?: string
  ): Promise<{ success: boolean; message: string }> => {
    const response = await apiClient.put<{ success: boolean; message: string }>(
      `/api/associate-permissions/${permissionId}/expiry`,
      { expiryDate }
    );
    return response.data;
  },

  /**
   * Renew permission with new expiry date
   */
  renewPermission: async (
    permissionId: number,
    newExpiryDate: string
  ): Promise<{ success: boolean; message: string }> => {
    const response = await apiClient.put<{ success: boolean; message: string }>(
      `/api/associate-permissions/${permissionId}/renew`,
      { newExpiryDate }
    );
    return response.data;
  },

  /**
   * Get permissions expiring soon
   */
  getExpiringPermissions: async (days: number = 7): Promise<AssociatePermissionListResponse> => {
    const response = await apiClient.get<AssociatePermissionListResponse>(
      `/api/associate-permissions/expiring?days=${days}`
    );
    return response.data;
  },

  /**
   * Get associates assigned to a specific client
   */
  getClientAssociates: async (clientId: number): Promise<AssociatePermissionListResponse> => {
    const response = await apiClient.get<AssociatePermissionListResponse>(
      `/api/associate-permissions/clients/${clientId}/associates`
    );
    return response.data;
  },

  /**
   * Get permission audit log for associate
   */
  getPermissionAuditLog: async (
    associateId: string,
    fromDate?: string,
    toDate?: string,
    page: number = 1,
    pageSize: number = 20
  ): Promise<AuditLogResponse> => {
    const params = new URLSearchParams({
      page: page.toString(),
      pageSize: pageSize.toString(),
    });

    if (fromDate) params.append('from', fromDate);
    if (toDate) params.append('to', toDate);

    const response = await apiClient.get<AuditLogResponse>(
      `/api/associate-permissions/${associateId}/audit?${params}`
    );
    return response.data;
  },

  /**
   * Get effective permission level for associate-client-area combination
   */
  getEffectivePermissionLevel: async (
    associateId: string,
    clientId: number,
    area: string
  ): Promise<{ success: boolean; data: { level: AssociatePermissionLevel } }> => {
    const response = await apiClient.get<{ success: boolean; data: { level: AssociatePermissionLevel } }>(
      `/api/associate-permissions/${associateId}/clients/${clientId}/areas/${area}/effective-level`
    );
    return response.data;
  },

  /**
   * Validate if permission is still active and valid
   */
  validatePermission: async (permissionId: number): Promise<{ success: boolean; data: { isValid: boolean } }> => {
    const response = await apiClient.get<{ success: boolean; data: { isValid: boolean } }>(
      `/api/associate-permissions/${permissionId}/validate`
    );
    return response.data;
  }
};

// Helper functions
export const PermissionHelpers = {
  /**
   * Check if a permission level includes another level
   */
  hasPermissionLevel: (current: AssociatePermissionLevel, required: AssociatePermissionLevel): boolean => {
    return (current & required) === required;
  },

  /**
   * Get human-readable permission level names
   */
  getPermissionLevelNames: (level: AssociatePermissionLevel): string[] => {
    const names: string[] = [];
    if (level & AssociatePermissionLevel.Read) names.push('Read');
    if (level & AssociatePermissionLevel.Create) names.push('Create');
    if (level & AssociatePermissionLevel.Update) names.push('Update');
    if (level & AssociatePermissionLevel.Delete) names.push('Delete');
    if (level & AssociatePermissionLevel.Submit) names.push('Submit');
    if (level & AssociatePermissionLevel.Approve) names.push('Approve');
    return names;
  },

  /**
   * Get permission level color for UI display
   */
  getPermissionLevelColor: (level: AssociatePermissionLevel): string => {
    if (level === AssociatePermissionLevel.All) return 'bg-green-500';
    if (level & AssociatePermissionLevel.Approve) return 'bg-blue-500';
    if (level & AssociatePermissionLevel.Submit) return 'bg-purple-500';
    if (level & AssociatePermissionLevel.Delete) return 'bg-red-500';
    if (level & AssociatePermissionLevel.Update) return 'bg-orange-500';
    if (level & AssociatePermissionLevel.Create) return 'bg-yellow-500';
    if (level & AssociatePermissionLevel.Read) return 'bg-gray-500';
    return 'bg-gray-300';
  },

  /**
   * Check if permission is expiring soon
   */
  isExpiringSoon: (expiryDate?: string, days: number = 7): boolean => {
    if (!expiryDate) return false;
    const expiry = new Date(expiryDate);
    const threshold = new Date();
    threshold.setDate(threshold.getDate() + days);
    return expiry <= threshold;
  },

  /**
   * Format permission area for display
   */
  formatPermissionArea: (area: string): string => {
    return area
      .replace(/([A-Z])/g, ' $1')
      .trim()
      .replace(/^./, str => str.toUpperCase());
  }
};

export default AssociatePermissionService;