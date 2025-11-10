import { apiClient } from '@/lib/api-client'

export interface AssociateDashboardSummary {
  totalClients: number
  totalPermissions: number
  expiringPermissions: number
  recentActions: number
  upcomingDeadlines: number
}

export interface AssociateDashboardData {
  summary: AssociateDashboardSummary
  delegatedClients: Array<{
    clientId: number
    businessName: string
    contactPerson?: string
    taxpayerCategory?: string
    hasUpcomingDeadlines: boolean
  }>
  recentActions: Array<{
    id: number
    action: string
    entityType: string
    entityId: number
    clientName?: string
    actionDate: string
    reason?: string
  }>
  upcomingDeadlines: Array<{
    taxFilingId: number
    clientName: string
    taxType: string
    dueDate: string
    status: string
    daysUntilDue: number
  }>
  permissionAlerts: {
    expiringPermissions: Array<{
      id: number
      clientName?: string
      permissionArea: string
      expiryDate?: string
      daysUntilExpiry?: number
    }>
  }
  statistics: {
    permissionsByArea: Record<string, number>
    actionsByType: Record<string, number>
    actionsByEntityType: Record<string, number>
    actionsPerDay: Record<string, number>
  }
}

export interface Paged<T> {
  success: boolean
  data: T
  pagination?: {
    currentPage: number
    pageSize: number
    totalCount: number
    totalPages: number
  }
}

export const AssociateDashboardService = {
  getDashboard: async (associateId: string): Promise<{ success: boolean; data: AssociateDashboardData }> => {
    const res = await apiClient.get<{ success: boolean; data: AssociateDashboardData }>(`/api/associate-dashboard/${associateId}`)
    return res.data
  },

  getDelegatedClients: async (
    associateId: string,
    page: number = 1,
    pageSize: number = 20,
    area?: string
  ): Promise<Paged<any[]>> => {
    const params = new URLSearchParams({ page: String(page), pageSize: String(pageSize) })
    if (area) params.append('area', area)
    const res = await apiClient.get<Paged<any[]>>(`/api/associate-dashboard/${associateId}/clients?${params}`)
    return res.data
  },

  getRecentActions: async (
    associateId: string,
    limit: number = 20,
    since?: string
  ): Promise<{ success: boolean; data: any[] }> => {
    const params = new URLSearchParams({ limit: String(limit) })
    if (since) params.append('since', since)
    const res = await apiClient.get<{ success: boolean; data: any[] }>(`/api/associate-dashboard/${associateId}/recent-actions?${params}`)
    return res.data
  },

  getPermissionAlerts: async (
    associateId: string
  ): Promise<{ success: boolean; data: { alerts: any[]; summary: Record<string, number> } }> => {
    const res = await apiClient.get<{ success: boolean; data: { alerts: any[]; summary: Record<string, number> } }>(
      `/api/associate-dashboard/${associateId}/permission-alerts`
    )
    return res.data
  },

  getWorkload: async (
    associateId: string,
    fromDate?: string,
    toDate?: string
  ): Promise<{ success: boolean; data: any }> => {
    const params = new URLSearchParams()
    if (fromDate) params.append('fromDate', fromDate)
    if (toDate) params.append('toDate', toDate)
    const suffix = params.toString() ? `?${params}` : ''
    const res = await apiClient.get<{ success: boolean; data: any }>(`/api/associate-dashboard/${associateId}/workload${suffix}`)
    return res.data
  },
}

export default AssociateDashboardService
