import { apiClient } from '@/lib/api-client';

/**
 * Create empty compliance data for new users or when API is unavailable
 */
const createEmptyComplianceData = (): ComplianceItem[] => [];

const createEmptyComplianceOverview = (): ComplianceOverviewData => ({
  totalClients: 0,
  compliant: 0,
  atRisk: 0,
  overdue: 0,
  averageScore: 0,
  totalOutstanding: 0,
  totalAlerts: 0
});

/**
 * Compliance data interfaces
 */
export interface ComplianceItem {
  id: string;
  type: string;
  description: string;
  status: 'compliant' | 'at-risk' | 'overdue' | 'pending';
  dueDate: string;
  lastUpdated: string;
  priority: 'high' | 'medium' | 'low';
  penalty?: number;
  actions: string[];
  clientId?: string;
  clientName?: string;
  taxYear?: number;
  category: string;
  complianceScore?: number;
  daysOverdue?: number;
  alerts?: number;
  taxType?: string;
}

export interface ComplianceScore {
  overall: number;
  breakdown: {
    filingCompliance: number;
    paymentCompliance: number;
    documentCompliance: number;
    deadlineCompliance: number;
  };
  trend: 'improving' | 'stable' | 'declining';
  lastCalculated: string;
}

export interface ComplianceFilters {
  status?: 'compliant' | 'at-risk' | 'overdue' | 'pending' | 'all';
  priority?: 'high' | 'medium' | 'low' | 'all';
  type?: string;
  clientId?: string;
  dateRange?: {
    from: string;
    to: string;
  };
}

export interface ComplianceStats {
  total: number;
  compliant: number;
  atRisk: number;
  overdue: number;
  pending: number;
  complianceRate: number;
  riskRate: number;
  overdueRate: number;
}

export interface ComplianceAlert {
  id: string;
  title: string;
  message: string;
  severity: 'critical' | 'warning' | 'info';
  clientId?: string;
  clientName?: string;
  dueDate?: string;
  createdAt: string;
  isRead: boolean;
  actionRequired: boolean;
}

export interface PenaltyCalculation {
  baseAmount: number;
  penaltyRate: number;
  daysOverdue: number;
  penaltyAmount: number;
  totalAmount: number;
  calculation: string;
  applicableRules: string[];
}

export interface ComplianceOverviewData {
  totalClients: number;
  compliant: number;
  atRisk: number;
  overdue: number;
  averageScore: number;
  totalOutstanding: number;
  totalAlerts: number;
}

/**
 * Compliance Service for tracking tax compliance
 */
export const ComplianceService = {
  /**
   * Get compliance items with optional filtering
   */
  getComplianceItems: async (filters: ComplianceFilters = {}): Promise<ComplianceItem[]> => {
    try {
      const params = new URLSearchParams();
      if (filters.status && filters.status !== 'all') params.append('status', filters.status);
      if (filters.priority && filters.priority !== 'all') params.append('priority', filters.priority);
      if (filters.type) params.append('type', filters.type);
      if (filters.clientId) params.append('clientId', filters.clientId);
      if (filters.dateRange) {
        params.append('fromDate', filters.dateRange.from);
        params.append('toDate', filters.dateRange.to);
      }

      const response = await apiClient.get<{ success: boolean; data: ComplianceItem[] }>(
        `/api/compliance/items?${params.toString()}`
      );
      return response.data.data;
    } catch (error) {
      console.warn('Compliance items API unavailable, returning empty data for new user experience:', error);
      return createEmptyComplianceData();
    }
  },

  /**
   * Get compliance score for a client or overall
   */
  getComplianceScore: async (clientId?: string): Promise<ComplianceScore> => {
    const url = clientId 
      ? `/api/compliance/score?clientId=${clientId}`
      : '/api/compliance/score';
    
    const response = await apiClient.get<{ success: boolean; data: ComplianceScore }>(url);
    return response.data.data;
  },

  /**
   * Get compliance statistics
   */
  getComplianceStats: async (clientId?: string): Promise<ComplianceStats> => {
    const url = clientId 
      ? `/api/compliance/stats?clientId=${clientId}`
      : '/api/compliance/stats';
    
    const response = await apiClient.get<{ success: boolean; data: ComplianceStats }>(url);
    return response.data.data;
  },

  /**
   * Get compliance alerts
   */
  getComplianceAlerts: async (clientId?: string): Promise<ComplianceAlert[]> => {
    const url = clientId 
      ? `/api/compliance/alerts?clientId=${clientId}`
      : '/api/compliance/alerts';
    
    const response = await apiClient.get<{ success: boolean; data: ComplianceAlert[] }>(url);
    return response.data.data;
  },

  /**
   * Update compliance item status
   */
  updateComplianceItem: async (id: string, updates: Partial<ComplianceItem>): Promise<ComplianceItem> => {
    const response = await apiClient.put<{ success: boolean; data: ComplianceItem }>(
      `/api/compliance/items/${id}`,
      updates
    );
    return response.data.data;
  },

  /**
   * Mark compliance alert as read
   */
  markAlertAsRead: async (alertId: string): Promise<void> => {
    await apiClient.put(`/api/compliance/alerts/${alertId}/read`);
  },

  /**
   * Calculate penalty for overdue compliance
   */
  calculatePenalty: async (
    complianceItemId: string,
    daysOverdue: number
  ): Promise<PenaltyCalculation> => {
    const response = await apiClient.post<{ success: boolean; data: PenaltyCalculation }>(
      `/api/compliance/calculate-penalty`,
      { complianceItemId, daysOverdue }
    );
    return response.data.data;
  },

  /**
   * Get compliance trends over time
   */
  getComplianceTrends: async (
    timeRange: '3m' | '6m' | '12m' = '12m',
    clientId?: string
  ): Promise<Array<{
    month: string;
    compliantRate: number;
    atRiskRate: number;
    overdueRate: number;
    totalItems: number;
  }>> => {
    const params = new URLSearchParams();
    params.append('timeRange', timeRange);
    if (clientId) params.append('clientId', clientId);

    const response = await apiClient.get<{ 
      success: boolean; 
      data: Array<{
        month: string;
        compliantRate: number;
        atRiskRate: number;
        overdueRate: number;
        totalItems: number;
      }> 
    }>(`/api/compliance/trends?${params.toString()}`);
    return response.data.data;
  },

  /**
   * Get compliance by tax type breakdown
   */
  getComplianceByTaxType: async (clientId?: string): Promise<Array<{
    taxType: string;
    compliant: number;
    nonCompliant: number;
    complianceRate: number;
  }>> => {
    const url = clientId 
      ? `/api/compliance/by-tax-type?clientId=${clientId}`
      : '/api/compliance/by-tax-type';
    
    const response = await apiClient.get<{ 
      success: boolean; 
      data: Array<{
        taxType: string;
        compliant: number;
        nonCompliant: number;
        complianceRate: number;
      }> 
    }>(url);
    return response.data.data;
  },

  /**
   * Export compliance report
   */
  exportComplianceReport: async (
    format: 'excel' | 'csv' | 'pdf',
    filters: ComplianceFilters = {}
  ): Promise<Blob> => {
    const params = new URLSearchParams();
    params.append('format', format);
    if (filters.status && filters.status !== 'all') params.append('status', filters.status);
    if (filters.priority && filters.priority !== 'all') params.append('priority', filters.priority);
    if (filters.type) params.append('type', filters.type);
    if (filters.clientId) params.append('clientId', filters.clientId);
    if (filters.dateRange) {
      params.append('fromDate', filters.dateRange.from);
      params.append('toDate', filters.dateRange.to);
    }

    const response = await apiClient.get(`/api/compliance/export?${params.toString()}`);
    return response.data as Blob;
  },

  /**
   * Get Sierra Leone specific compliance requirements
   */
  getSierraLeoneRequirements: async (): Promise<Array<{
    category: string;
    requirements: string[];
    deadlines: string[];
    penalties: string[];
    financeActReference: string;
  }>> => {
    const response = await apiClient.get<{ 
      success: boolean; 
      data: Array<{
        category: string;
        requirements: string[];
        deadlines: string[];
        penalties: string[];
        financeActReference: string;
      }> 
    }>('/api/compliance/sierra-leone-requirements');
    return response.data.data;
  },

  /**
   * Get compliance data (alias for getComplianceItems)
   */
  getComplianceData: async (filters: ComplianceFilters = {}): Promise<ComplianceItem[]> => {
    try {
      return await ComplianceService.getComplianceItems(filters);
    } catch (error) {
      console.warn('Compliance API unavailable, returning empty data for new user experience:', error);
      return createEmptyComplianceData();
    }
  },

  /**
   * Get compliance overview/summary data
   */
  getComplianceOverview: async (): Promise<ComplianceOverviewData> => {
    try {
      const response = await apiClient.get<{ success: boolean; data: ComplianceOverviewData }>(
        '/api/compliance/overview'
      );
      return response.data.data;
    } catch (error) {
      console.warn('Compliance overview API unavailable, returning empty data for new user experience:', error);
      return createEmptyComplianceOverview();
    }
  }
};