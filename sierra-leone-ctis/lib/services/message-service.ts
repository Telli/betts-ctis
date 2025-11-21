import { apiClient } from '@/lib/api-client'

export type ConversationStatus = 'Open' | 'Pending' | 'Urgent' | 'Closed'

export interface Conversation {
  id: number
  clientId: number
  clientName: string
  subject: string
  status: ConversationStatus
  assignedTo?: string
  assignedToName?: string
  unreadCount: number
  lastMessageAt: string
  createdAt: string
}

export interface Message {
  id: number
  conversationId: number
  senderId: string
  senderName: string
  body: string
  isInternal: boolean
  sentAt: string
}

export interface ConversationFilters {
  status?: ConversationStatus
  assignedTo?: string
}

export interface SendMessageDto {
  body: string
  isInternal: boolean
}

export const MessageService = {
  /**
   * Get conversations with optional filters
   */
  getConversations: async (filters?: ConversationFilters): Promise<Conversation[]> => {
    const params = new URLSearchParams()
    if (filters?.status) params.append('status', filters.status)
    if (filters?.assignedTo) params.append('assignedTo', filters.assignedTo)

    const response = await apiClient.get<{ success: boolean; data: Conversation[] }>(
      `/api/messages/conversations?${params}`
    )
    return response.data.data
  },

  /**
   * Get messages for a conversation
   */
  getMessages: async (conversationId: number): Promise<Message[]> => {
    const response = await apiClient.get<{ success: boolean; data: Message[] }>(
      `/api/messages/conversations/${conversationId}/messages`
    )
    return response.data.data
  },

  /**
   * Send a message or internal note
   */
  sendMessage: async (conversationId: number, message: SendMessageDto): Promise<void> => {
    await apiClient.post(`/api/messages/conversations/${conversationId}/messages`, message)
  },

  /**
   * Assign conversation to a staff member
   */
  assignConversation: async (conversationId: number, userId: string): Promise<void> => {
    await apiClient.post(`/api/messages/conversations/${conversationId}/assign`, { userId })
  },

  /**
   * Update conversation status
   */
  updateConversationStatus: async (conversationId: number, status: ConversationStatus): Promise<void> => {
    await apiClient.patch(`/api/messages/conversations/${conversationId}/status`, { status })
  },

  /**
   * Get staff users for assignment
   */
  getStaffUsers: async (): Promise<Array<{ id: string; name: string }>> => {
    const response = await apiClient.get<{ success: boolean; data: Array<{ id: string; name: string }> }>(
      '/api/messages/staff-users'
    )
    return response.data.data
  },
}

export default MessageService

