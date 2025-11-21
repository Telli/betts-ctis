'use client';

import { useQuery, useMutation, useQueryClient } from '@tanstack/react-query';
import { apiClient } from '@/lib/api-client';
import { InternalKPIDto, ClientKPIDto, KPIAlertDto, KPIThresholdDto, KpiDashboardSummaryDto } from '@/lib/types/kpi';

// Query keys
const KPI_KEYS = {
  internal: (fromDate?: Date, toDate?: Date) => 
    ['kpi', 'internal', fromDate?.toISOString(), toDate?.toISOString()],
  client: (clientId: number, fromDate?: Date, toDate?: Date) => 
    ['kpi', 'client', clientId, fromDate?.toISOString(), toDate?.toISOString()],
  myKPIs: (fromDate?: Date, toDate?: Date) => 
    ['kpi', 'my-kpis', fromDate?.toISOString(), toDate?.toISOString()],
  alerts: (clientId?: number) => ['kpi', 'alerts', clientId],
  trends: (fromDate: Date, toDate: Date, period: string) => 
    ['kpi', 'trends', fromDate.toISOString(), toDate.toISOString(), period],
};

// Internal KPIs hook
export function useKPIs(fromDate?: Date, toDate?: Date) {
  return useQuery({
    queryKey: KPI_KEYS.internal(fromDate, toDate),
    queryFn: async (): Promise<InternalKPIDto> => {
      try {
        const params = new URLSearchParams();
        if (fromDate) params.append('fromDate', fromDate.toISOString());
        if (toDate) params.append('toDate', toDate.toISOString());
        
        const queryString = params.toString();
        const url = `/api/kpi/internal${queryString ? `?${queryString}` : ''}`;
        
        const response = await apiClient.get<InternalKPIDto>(url);
        return response.data;
      } catch (error: any) {
        console.error('Error fetching internal KPIs:', error);
        throw new Error(error.response?.data?.message || 'Failed to fetch internal KPIs');
      }
    },
    staleTime: 5 * 60 * 1000, // 5 minutes
    gcTime: 10 * 60 * 1000, // 10 minutes
  });
}

// KPI trends hook
export function useKPITrends(fromDate: Date, toDate: Date, period: string = 'Monthly') {
  return useQuery({
    queryKey: KPI_KEYS.trends(fromDate, toDate, period),
    queryFn: async (): Promise<InternalKPIDto[]> => {
      try {
        const params = new URLSearchParams({
          fromDate: fromDate.toISOString(),
          toDate: toDate.toISOString(),
          period
        });
        
        const response = await apiClient.get<InternalKPIDto[]>(`/api/kpi/internal/trends?${params}`);
        return response.data;
      } catch (error: any) {
        console.error('Error fetching KPI trends:', error);
        throw new Error(error.response?.data?.message || 'Failed to fetch KPI trends');
      }
    },
    enabled: !!fromDate && !!toDate,
    staleTime: 10 * 60 * 1000, // 10 minutes
  });
}

// Client KPIs hook
export function useClientKPIs(clientId: number, fromDate?: Date, toDate?: Date) {
  return useQuery({
    queryKey: KPI_KEYS.client(clientId, fromDate, toDate),
    queryFn: async (): Promise<ClientKPIDto> => {
      try {
        const params = new URLSearchParams();
        if (fromDate) params.append('fromDate', fromDate.toISOString());
        if (toDate) params.append('toDate', toDate.toISOString());
        
        const queryString = params.toString();
        const url = `/api/kpi/client/${clientId}${queryString ? `?${queryString}` : ''}`;
        
        const response = await apiClient.get<ClientKPIDto>(url);
        return response.data;
      } catch (error: any) {
        console.error('Error fetching client KPIs:', error);
        throw new Error(error.response?.data?.message || 'Failed to fetch client KPIs');
      }
    },
    enabled: !!clientId,
    staleTime: 5 * 60 * 1000, // 5 minutes
  });
}

// My KPIs hook (for logged-in client)
export function useMyKPIs(fromDate?: Date, toDate?: Date) {
  return useQuery({
    queryKey: KPI_KEYS.myKPIs(fromDate, toDate),
    queryFn: async (): Promise<ClientKPIDto> => {
      try {
        const params = new URLSearchParams();
        if (fromDate) params.append('fromDate', fromDate.toISOString());
        if (toDate) params.append('toDate', toDate.toISOString());
        
        const queryString = params.toString();
        const url = `/api/kpi/my-kpis${queryString ? `?${queryString}` : ''}`;
        
        const response = await apiClient.get<ClientKPIDto>(url);
        return response.data;
      } catch (error: any) {
        console.error('Error fetching my KPIs:', error);
        throw new Error(error.response?.data?.message || 'Failed to fetch your KPIs');
      }
    },
    staleTime: 5 * 60 * 1000, // 5 minutes
  });
}

// KPI alerts hook
export function useKPIAlerts(clientId?: number) {
  return useQuery({
    queryKey: KPI_KEYS.alerts(clientId),
    queryFn: async (): Promise<KPIAlertDto[]> => {
      try {
        const params = new URLSearchParams();
        if (clientId) params.append('clientId', clientId.toString());
        
        const queryString = params.toString();
        const url = `/api/kpi/alerts${queryString ? `?${queryString}` : ''}`;
        
        const response = await apiClient.get<KPIAlertDto[]>(url);
        return response.data;
      } catch (error: any) {
        console.error('Error fetching KPI alerts:', error);
        throw new Error(error.response?.data?.message || 'Failed to fetch KPI alerts');
      }
    },
    staleTime: 2 * 60 * 1000, // 2 minutes
    refetchInterval: 5 * 60 * 1000, // Refetch every 5 minutes
  });
}

// Update KPI thresholds mutation
export function useUpdateKPIThresholds() {
  const queryClient = useQueryClient();

  return useMutation({
    mutationFn: async (thresholds: KPIThresholdDto) => {
      try {
        const response = await apiClient.put('/api/kpi/thresholds', thresholds);
        return response.data;
      } catch (error: any) {
        console.error('Error updating KPI thresholds:', error);
        throw new Error(error.response?.data?.message || 'Failed to update KPI thresholds');
      }
    },
    onSuccess: () => {
      // Invalidate all KPI queries to trigger refetch
      queryClient.invalidateQueries({ queryKey: ['kpi'] });
    },
  });
}

// Refresh KPIs mutation - temporarily disabled until backend endpoint is implemented
export function useRefreshKPIs() {
  const queryClient = useQueryClient();

  return useMutation({
    mutationFn: async () => {
      try {
        const response = await apiClient.post<{ success: boolean; message?: string }>('/api/kpi/refresh');
        return response.data || { success: true };
      } catch (err) {
        // Graceful fallback: simulate success if endpoint not available
        await new Promise(resolve => setTimeout(resolve, 800));
        return { success: true } as any;
      }
    },
    onSuccess: () => {
      // Invalidate all KPI queries to trigger refetch
      queryClient.invalidateQueries({ queryKey: ['kpi'] });
    },
  });
}

// Mark alert as read mutation
export function useMarkAlertAsRead() {
  const queryClient = useQueryClient();

  return useMutation({
    mutationFn: async (alertId: number) => {
      try {
        const response = await apiClient.put(`/api/kpi/alerts/${alertId}/read`);
        return response.data;
      } catch (error: any) {
        console.error('Error marking alert as read:', error);
        throw new Error(error.response?.data?.message || 'Failed to mark alert as read');
      }
    },
    onSuccess: () => {
      // Invalidate alerts queries
      queryClient.invalidateQueries({ queryKey: ['kpi', 'alerts'] });
    },
  });
}

// Create KPI alert mutation
export function useCreateKPIAlert() {
  const queryClient = useQueryClient();

  return useMutation({
    mutationFn: async (alert: Omit<KPIAlertDto, 'id' | 'createdAt'>) => {
      try {
        const response = await apiClient.post('/api/kpi/alerts', alert);
        return response.data;
      } catch (error: any) {
        console.error('Error creating KPI alert:', error);
        throw new Error(error.response?.data?.message || 'Failed to create KPI alert');
      }
    },
    onSuccess: () => {
      // Invalidate alerts queries
      queryClient.invalidateQueries({ queryKey: ['kpi', 'alerts'] });
    },
  });
}

export function useKpiDashboardSummary(fromDate?: Date, toDate?: Date) {
  return useQuery({
    queryKey: ['kpi', 'dashboard-summary', fromDate?.toISOString(), toDate?.toISOString()],
    queryFn: async (): Promise<KpiDashboardSummaryDto> => {
      try {
        const params = new URLSearchParams();
        if (fromDate) params.append('fromDate', fromDate.toISOString());
        if (toDate) params.append('toDate', toDate.toISOString());

        const queryString = params.toString();
        const url = `/api/kpi/dashboard${queryString ? `?${queryString}` : ''}`;

        const response = await apiClient.get<KpiDashboardSummaryDto>(url);
        return response.data;
      } catch (error: any) {
        console.error('Error fetching KPI dashboard summary:', error);
        throw new Error(error.response?.data?.message || 'Failed to fetch KPI dashboard summary');
      }
    },
    staleTime: 5 * 60 * 1000,
  });
}