import { apiClient } from '@/lib/api-client';

/**
 * Create empty/default analytics data for new users or when API is unavailable
 */
const createEmptyAnalyticsData = (): AnalyticsData => ({
  kpis: {
    currentRevenue: 0,
    revenueGrowth: 0,
    totalClients: 0,
    clientGrowth: 0,
    totalRevenue: 0,
    avgComplianceRate: 0,
    targetAchievement: 0
  },
  revenue: [],
  clientGrowth: [],
  taxTypeDistribution: [],
  complianceMetrics: [],
  regionPerformance: [],
  lastUpdated: new Date().toISOString()
});

/**
 * Analytics data interfaces
 */
export interface RevenueData {
  month: string;
  amount: number;
  target: number;
  year: number;
}

export interface ClientGrowthData {
  month: string;
  newClients: number;
  totalClients: number;
  churnedClients?: number;
  retentionRate?: number;
}

export interface TaxTypeDistribution {
  name: string;
  value: number;
  percentage: number;
  color: string;
}

export interface ComplianceMetrics {
  category: string;
  compliant: number;
  nonCompliant: number;
  totalClients: number;
  complianceRate: number;
}

export interface RegionPerformance {
  region: string;
  clients: number;
  revenue: number;
  compliance: number;
  averageRevenue: number;
  growthRate?: number;
}

export interface KPIData {
  currentRevenue: number;
  revenueGrowth: number;
  totalClients: number;
  clientGrowth: number;
  totalRevenue: number;
  avgComplianceRate: number;
  targetAchievement: number;
}

export interface AnalyticsFilters {
  timeRange: '3m' | '6m' | '12m' | '24m';
  region?: string;
  clientCategory?: 'large' | 'medium' | 'small' | 'micro';
  taxType?: string;
}

export interface AnalyticsData {
  revenue: RevenueData[];
  clientGrowth: ClientGrowthData[];
  taxTypeDistribution: TaxTypeDistribution[];
  complianceMetrics: ComplianceMetrics[];
  regionPerformance: RegionPerformance[];
  kpis: KPIData;
  lastUpdated: string;
}

/**
 * Analytics Service for business intelligence data
 */
export const AnalyticsService = {
  /**
   * Get comprehensive analytics data
   */
  getAnalyticsData: async (filters: AnalyticsFilters = { timeRange: '12m' }): Promise<AnalyticsData> => {
    try {
      const params = new URLSearchParams();
      params.append('timeRange', filters.timeRange);
      if (filters.region) params.append('region', filters.region);
      if (filters.clientCategory) params.append('clientCategory', filters.clientCategory);
      if (filters.taxType) params.append('taxType', filters.taxType);

      const response = await apiClient.get<{ success: boolean; data: AnalyticsData }>(
        `/api/analytics?${params.toString()}`
      );
      return response.data.data;
    } catch (error) {
      console.warn('Analytics API unavailable, returning empty data for new user experience:', error);
      const emptyData = createEmptyAnalyticsData();
      return {
        ...emptyData,
        lastUpdated: new Date().toISOString()
      };
    }
  },

  /**
   * Get revenue trends data
   */
  getRevenueTrends: async (timeRange: string = '12m'): Promise<RevenueData[]> => {
    const response = await apiClient.get<{ success: boolean; data: RevenueData[] }>(
      `/api/analytics/revenue?timeRange=${timeRange}`
    );
    return response.data.data;
  },

  /**
   * Get client growth data
   */
  getClientGrowth: async (timeRange: string = '12m'): Promise<ClientGrowthData[]> => {
    const response = await apiClient.get<{ success: boolean; data: ClientGrowthData[] }>(
      `/api/analytics/client-growth?timeRange=${timeRange}`
    );
    return response.data.data;
  },

  /**
   * Get tax type distribution
   */
  getTaxTypeDistribution: async (): Promise<TaxTypeDistribution[]> => {
    const response = await apiClient.get<{ success: boolean; data: TaxTypeDistribution[] }>(
      '/api/analytics/tax-distribution'
    );
    return response.data.data;
  },

  /**
   * Get compliance metrics by client category
   */
  getComplianceMetrics: async (): Promise<ComplianceMetrics[]> => {
    const response = await apiClient.get<{ success: boolean; data: ComplianceMetrics[] }>(
      '/api/analytics/compliance-metrics'
    );
    return response.data.data;
  },

  /**
   * Get regional performance data
   */
  getRegionalPerformance: async (): Promise<RegionPerformance[]> => {
    const response = await apiClient.get<{ success: boolean; data: RegionPerformance[] }>(
      '/api/analytics/regional-performance'
    );
    return response.data.data;
  },

  /**
   * Get key performance indicators
   */
  getKPIs: async (timeRange: string = '12m'): Promise<KPIData> => {
    const response = await apiClient.get<{ success: boolean; data: KPIData }>(
      `/api/analytics/kpis?timeRange=${timeRange}`
    );
    return response.data.data;
  },

  /**
   * Get available regions for filtering
   */
  getAvailableRegions: async (): Promise<string[]> => {
    const response = await apiClient.get<{ success: boolean; data: string[] }>(
      '/api/analytics/regions'
    );
    return response.data.data;
  },

  /**
   * Get available tax types for filtering
   */
  getAvailableTaxTypes: async (): Promise<string[]> => {
    const response = await apiClient.get<{ success: boolean; data: string[] }>(
      '/api/analytics/tax-types'
    );
    return response.data.data;
  },

  /**
   * Export analytics data
   */
  exportAnalyticsData: async (
    format: 'excel' | 'csv' | 'pdf', 
    filters: AnalyticsFilters = { timeRange: '12m' }
  ): Promise<Blob> => {
    const params = new URLSearchParams();
    params.append('format', format);
    params.append('timeRange', filters.timeRange);
    if (filters.region) params.append('region', filters.region);
    if (filters.clientCategory) params.append('clientCategory', filters.clientCategory);
    if (filters.taxType) params.append('taxType', filters.taxType);

    const response = await apiClient.get(`/api/analytics/export?${params.toString()}`);
    return response.data as Blob;
  }
};