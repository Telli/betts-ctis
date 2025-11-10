import { apiClient } from '@/lib/api-client'

export interface AdminAssociateDto {
  userId: string
  fullName: string
  email: string
  isActive: boolean
  assignedClientsCount: number
}

export interface PagedResponse<T> {
  success: boolean
  data: T
  pagination?: {
    currentPage: number
    pageSize: number
    totalCount: number
    totalPages: number
  }
}

export interface ClientOverviewDto {
  clientId: number
  businessName: string
  contactPerson?: string
  email?: string
  phoneNumber?: string
  taxpayerCategory?: string
  status?: string
}

export const AdminAssociateService = {
  // List associates with optional search/pagination
  getAssociates: async (
    search?: string,
    page: number = 1,
    pageSize: number = 20
  ): Promise<PagedResponse<AdminAssociateDto[]>> => {
    const params = new URLSearchParams({ page: page.toString(), pageSize: pageSize.toString() })
    if (search) params.append('search', search)
    const res = await apiClient.get<PagedResponse<AdminAssociateDto[]>>(`/api/admin/clients/associates?${params}`)
    return res.data
  },

  // Get single associate details (uses same endpoint, filters server-side)
  getAssociateById: async (associateId: string): Promise<{ success: boolean; data: AdminAssociateDto } | null> => {
    // No dedicated endpoint existed initially; use list and find to keep UI working.
    const res = await apiClient.get<PagedResponse<AdminAssociateDto[]>>(`/api/admin/clients/associates?page=1&pageSize=1000`)
    const found = res.data.data.find(a => a.userId === associateId)
    return found ? { success: true, data: found } : null
  },

  // Search admin client overview to select clients when granting permissions
  searchClients: async (
    search: string,
    page: number = 1,
    pageSize: number = 10
  ): Promise<PagedResponse<ClientOverviewDto[]>> => {
    const params = new URLSearchParams({ page: page.toString(), pageSize: pageSize.toString(), search })
    const res = await apiClient.get<PagedResponse<ClientOverviewDto[]>>(`/api/admin/clients?${params}`)
    return res.data
  },

  // Assign associate as the primary assigned associate to a client
  assignAssociateToClient: async (
    clientId: number,
    associateUserId: string
  ): Promise<{ success: boolean; message: string }> => {
    const res = await apiClient.post<{ success: boolean; message: string }>(
      `/api/admin/clients/${clientId}/assign-associate`,
      { associateUserId }
    )
    return res.data
  },
}

export default AdminAssociateService
