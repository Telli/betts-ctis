import { apiClient } from '@/lib/api-client';

export interface ClientSummary {
  totalClients: number;
  compliantClients: number;
  pendingClients: number;
  warningClients: number;
  overdueClients: number;
}

export interface ComplianceOverview {
  totalFilings: number;
  completedFilings: number;
  pendingFilings: number;
  lateFilings: number;
  taxTypeBreakdown: Record<string, number>;
  monthlyRevenue: Record<string, number>;
}

export interface RecentActivity {
  id: number;
  type: string;
  action: string;
  description: string;
  entityName: string;
  clientId?: number;
  clientName?: string;
  timestamp: string;
  userId?: string;
  userName?: string;
}

export interface UpcomingDeadline {
  id: number;
  title: string;
  description: string;
  dueDate: string;
  type: string;
  clientId?: number;
  clientName?: string;
  isUrgent: boolean;
  daysRemaining: number;
}

export interface PendingApproval {
  id: number;
  clientId: number;
  clientName: string;
  amount: number;
  description: string;
  submittedDate: string;
  submittedBy: string;
  type: string;
  status: string;
}

export interface NavigationCounts {
  totalClients: number;
  totalTaxFilings: number;
  upcomingDeadlines: number;
  unreadNotifications: number;
}

export interface DashboardMetrics {
  complianceRate: number;
  complianceRateTrend: string;
  complianceRateTrendUp: boolean;
  filingTimelinessAvgDays: number;
  filingTimelinessTrend: string;
  filingTimelinessTrendUp: boolean;
  paymentOnTimeRate: number;
  paymentOnTimeRateTrend: string;
  paymentOnTimeRateTrendUp: boolean;
  documentSubmissionRate: number;
  documentSubmissionRateTrend: string;
  documentSubmissionRateTrendUp: boolean;
}

export interface DashboardData {
  clientSummary: ClientSummary;
  complianceOverview: ComplianceOverview;
  recentActivity: RecentActivity[];
  upcomingDeadlines: UpcomingDeadline[];
  pendingApprovals: PendingApproval[];
  metrics: DashboardMetrics;
}

export const DashboardService = {
  /**
   * Get all dashboard data
   */
  getDashboard: async (): Promise<DashboardData> => {
    const response = await apiClient.get<{ success: boolean; data: DashboardData }>('/api/dashboard');
    return response.data.data;
  },

  /**
   * Get client summary data
   */
  getClientSummary: async (): Promise<ClientSummary> => {
    const response = await apiClient.get<{ success: boolean; data: ClientSummary }>('/api/dashboard/client-summary');
    return response.data.data;
  },

  /**
   * Get compliance overview data
   */
  getComplianceOverview: async (): Promise<ComplianceOverview> => {
    const response = await apiClient.get<{ success: boolean; data: ComplianceOverview }>('/api/dashboard/compliance');
    return response.data.data;
  },

  /**
   * Get recent activity data
   */
  getRecentActivity: async (count: number = 10): Promise<RecentActivity[]> => {
    const response = await apiClient.get<{ success: boolean; data: RecentActivity[] }>(`/api/dashboard/recent-activity?count=${count}`);
    return response.data.data;
  },

  /**
   * Get upcoming deadlines
   */
  getUpcomingDeadlines: async (days: number = 30): Promise<UpcomingDeadline[]> => {
    const response = await apiClient.get<{ success: boolean; data: UpcomingDeadline[] }>(`/api/dashboard/deadlines?days=${days}`);
    return response.data.data;
  },

  /**
   * Get pending approvals
   */
  getPendingApprovals: async (): Promise<PendingApproval[]> => {
    const response = await apiClient.get<{ success: boolean; data: PendingApproval[] }>('/api/dashboard/pending-approvals');
    return response.data.data;
  },

  /**
   * Get navigation badge counts
   */
  getNavigationCounts: async (): Promise<NavigationCounts> => {
    const response = await apiClient.get<{ success: boolean; data: NavigationCounts }>('/api/dashboard/navigation-counts');
    return response.data.data;
  }
};
