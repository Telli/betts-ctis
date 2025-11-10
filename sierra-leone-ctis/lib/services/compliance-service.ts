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

      const response = await apiClient.get<any>(
        `/api/compliance/items?${params.toString()}`
      );

      const raw = response.data as any[];
      if (!Array.isArray(raw)) return createEmptyComplianceData();

      const mapped: ComplianceItem[] = raw.map((r: any) => {
        const id = String(r.id ?? r.Id ?? Math.random().toString(36).slice(2, 10));
        const title = r.title ?? r.Title ?? r.name ?? r.Name ?? '';
        const category = r.category ?? r.Category ?? r.type ?? r.Type ?? 'general';
        const due = r.dueDate ?? r.DueDate ?? r.deadline ?? r.Deadline;
        const statusRaw = (r.status ?? r.Status ?? 'pending').toString().toLowerCase();
        const status: ComplianceItem['status'] =
          statusRaw.includes('risk') ? 'at-risk' :
          statusRaw.includes('overdue') ? 'overdue' :
          statusRaw.includes('compliant') ? 'compliant' : 'pending';
        const priorityRaw = (r.priority ?? r.Priority ?? 'medium').toString().toLowerCase();
        const priority: ComplianceItem['priority'] =
          priorityRaw.startsWith('h') ? 'high' : priorityRaw.startsWith('l') ? 'low' : 'medium';
        const dueDateStr = due ? new Date(due).toISOString() : new Date().toISOString();
        const daysOverdue = due ? Math.max(0, Math.ceil((Date.now() - new Date(due).getTime()) / 86400000)) : 0;

        return {
          id,
          type: category,
          description: title,
          status,
          dueDate: dueDateStr,
          lastUpdated: new Date().toISOString(),
          priority,
          actions: [],
          clientId: r.clientId?.toString() ?? r.ClientId?.toString(),
          clientName: r.clientName ?? r.ClientName,
          taxYear: r.taxYear ?? r.TaxYear,
          category,
          complianceScore: r.complianceScore ?? r.ComplianceScore,
          daysOverdue,
          alerts: r.alerts ?? r.Alerts ?? 0,
          taxType: r.taxType ?? r.TaxType
        } as ComplianceItem;
      });

      return mapped;
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
    const response = await apiClient.get<Blob>(
      `/api/compliance/export?${params.toString()}`,
      { responseType: 'blob' }
    );
    return response.data;
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
   * Get compliance overview/summary data with backend shape tolerance
   */
  getComplianceOverview: async (): Promise<ComplianceOverviewData> => {
    try {
      // ComplianceController returns a plain object (not wrapped)
      const resp = await apiClient.get<any>('/api/compliance/overview');
      const src = resp.data as any;

      const overview: ComplianceOverviewData = {
        totalClients: src?.totalClients ?? 0,
        compliant: src?.compliant ?? 0,
        atRisk: src?.atRisk ?? 0,
        overdue: src?.overdue ?? 0,
        averageScore: Number(src?.ComplianceScore ?? src?.averageScore ?? 0) || 0,
        totalOutstanding: Number(src?.totalOutstanding ?? 0) || 0,
        totalAlerts: Number(src?.PendingTasks ?? src?.totalAlerts ?? 0) || 0,
      };

      // Fallback to ComplianceTracker dashboard for richer stats if empty
      if (
        overview.totalClients === 0 &&
        overview.compliant === 0 &&
        overview.atRisk === 0 &&
        overview.overdue === 0 &&
        overview.averageScore === 0
      ) {
        const dashResp = await apiClient.get<any>('/api/ComplianceTracker/dashboard');
        const d = dashResp.data as any;
        if (d) {
          const recentAlerts = d.RecentAlerts ?? d.recentAlerts;
          if (Array.isArray(recentAlerts)) {
            overview.totalAlerts = recentAlerts.length;
          }
        }
      }

      return overview;
    } catch (error) {
      console.warn('Compliance overview API unavailable, returning empty data for new user experience:', error);
      return createEmptyComplianceOverview();
    }
  }
};