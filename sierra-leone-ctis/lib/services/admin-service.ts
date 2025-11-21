import { apiClient } from '@/lib/api-client'

export interface User {
  id: string
  name: string
  email: string
  role: string
  isActive: boolean
  companyName?: string
  lastLogin?: string
  createdAt: string
}

export interface InviteUserDto {
  email: string
  name: string
  role: string
}

export interface UpdateUserDto {
  name?: string
  role?: string
}

export interface TaxRate {
  id: number
  type: string
  rate: number
  effectiveFrom: string
  effectiveTo?: string
  applicableTo: string
}

export interface PenaltyRule {
  id: number
  taxType: string
  condition: string
  amount: number
  percentage?: number
  description: string
}

export interface AuditLogEntry {
  id: number
  timestamp: string
  actor: string
  role: string
  action: string
  ipAddress: string
  details: string
}

export interface JobStatus {
  name: string
  status: 'Running' | 'Stopped' | 'Error'
  lastRun?: string
  nextRun?: string
  queueSize: number
}

export const AdminService = {
  // User Management
  getUsers: async (): Promise<User[]> => {
    const response = await apiClient.get<{ success: boolean; data: User[] }>('/api/admin/users')
    return response.data.data
  },

  inviteUser: async (data: InviteUserDto): Promise<void> => {
    await apiClient.post('/api/admin/users/invite', data)
  },

  getUser: async (userId: string): Promise<User> => {
    const response = await apiClient.get<{ success: boolean; data: User }>(`/api/admin/users/${userId}`)
    return response.data.data
  },

  updateUser: async (userId: string, data: UpdateUserDto): Promise<void> => {
    await apiClient.put(`/api/admin/users/${userId}`, data)
  },

  deleteUser: async (userId: string): Promise<void> => {
    await apiClient.delete(`/api/admin/users/${userId}`)
  },

  updateUserRole: async (userId: string, role: string): Promise<void> => {
    await apiClient.patch(`/api/admin/users/${userId}/role`, { role })
  },

  updateUserStatus: async (userId: string, isActive: boolean): Promise<void> => {
    await apiClient.patch(`/api/admin/users/${userId}/status`, { isActive })
  },

  // Tax Rates
  getTaxRates: async (): Promise<TaxRate[]> => {
    const response = await apiClient.get<{ success: boolean; data: TaxRate[] }>('/api/admin/tax-rates')
    return response.data.data
  },

  getTaxRate: async (type: string): Promise<TaxRate> => {
    const response = await apiClient.get<{ success: boolean; data: TaxRate }>(`/api/admin/tax-rates/${type}`)
    return response.data.data
  },

  updateTaxRate: async (type: string, rate: number): Promise<void> => {
    await apiClient.put(`/api/admin/tax-rates/${type}`, { rate })
  },

  getTaxRateHistory: async (type: string): Promise<TaxRate[]> => {
    const response = await apiClient.get<{ success: boolean; data: TaxRate[] }>(
      `/api/admin/tax-rates/${type}/history`
    )
    return response.data.data
  },

  // Penalties
  getPenalties: async (): Promise<PenaltyRule[]> => {
    const response = await apiClient.get<{ success: boolean; data: PenaltyRule[] }>('/api/admin/penalties')
    return response.data.data
  },

  createPenalty: async (data: Omit<PenaltyRule, 'id'>): Promise<void> => {
    await apiClient.post('/api/admin/penalties', data)
  },

  updatePenalty: async (id: number, data: Partial<PenaltyRule>): Promise<void> => {
    await apiClient.put(`/api/admin/penalties/${id}`, data)
  },

  deletePenalty: async (id: number): Promise<void> => {
    await apiClient.delete(`/api/admin/penalties/${id}`)
  },

  importExciseTable: async (file: File): Promise<void> => {
    const formData = new FormData()
    formData.append('file', file)
    await apiClient.post('/api/admin/penalties/import', formData, { isFormData: true })
  },

  // Audit Logs
  getAuditLogs: async (filters?: {
    fromDate?: string
    toDate?: string
    actor?: string
    action?: string
  }): Promise<AuditLogEntry[]> => {
    const params = new URLSearchParams()
    if (filters?.fromDate) params.append('fromDate', filters.fromDate)
    if (filters?.toDate) params.append('toDate', filters.toDate)
    if (filters?.actor) params.append('actor', filters.actor)
    if (filters?.action) params.append('action', filters.action)

    const response = await apiClient.get<{ success: boolean; data: AuditLogEntry[] }>(
      `/api/admin/audit-logs?${params}`
    )
    return response.data.data
  },

  exportAuditLogs: async (): Promise<void> => {
    const response = await apiClient.get('/api/admin/audit-logs/export', {
      responseType: 'blob',
    })
    const url = window.URL.createObjectURL(new Blob([response.data]))
    const link = document.createElement('a')
    link.href = url
    link.setAttribute('download', `audit-logs-${new Date().toISOString()}.csv`)
    document.body.appendChild(link)
    link.click()
    link.remove()
  },

  // Jobs Monitor
  getJobs: async (): Promise<JobStatus[]> => {
    const response = await apiClient.get<{ success: boolean; data: JobStatus[] }>('/api/admin/jobs')
    return response.data.data
  },

  getJob: async (name: string): Promise<JobStatus> => {
    const response = await apiClient.get<{ success: boolean; data: JobStatus }>(`/api/admin/jobs/${name}`)
    return response.data.data
  },

  startJob: async (name: string): Promise<void> => {
    await apiClient.post(`/api/admin/jobs/${name}/start`)
  },

  stopJob: async (name: string): Promise<void> => {
    await apiClient.post(`/api/admin/jobs/${name}/stop`)
  },

  restartJob: async (name: string): Promise<void> => {
    await apiClient.post(`/api/admin/jobs/${name}/restart`)
  },
}

export default AdminService

