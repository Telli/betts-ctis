import { API_BASE_URL, authenticatedFetch } from "../auth";

export interface ChatConversation {
  id: number;
  clientId?: number;
  client: string;
  subject: string;
  lastMessagePreview: string;
  timestampDisplay: string;
  status: string;
  unreadCount: number;
  assignedTo: string;
}

export interface ChatMessage {
  id: number;
  senderType: string;
  senderName: string;
  content: string;
  sentAt: string;
  isInternal: boolean;
}

interface ConversationResponse {
  success: boolean;
  data?: ChatConversation[];
  message?: string;
}

interface MessagesResponse {
  success: boolean;
  data?: ChatMessage[];
  message?: string;
}

export async function fetchConversations(): Promise<ChatConversation[]> {
  const response = await authenticatedFetch(`${API_BASE_URL}/chat/conversations`);
  const payload = (await response.json()) as ConversationResponse;

  if (!response.ok || !payload?.success || !Array.isArray(payload.data)) {
    const message = payload?.message || `Failed to load conversations (status ${response.status})`;
    throw new Error(message);
  }

  return payload.data;
}

export async function fetchMessages(conversationId: number): Promise<ChatMessage[]> {
  const response = await authenticatedFetch(`${API_BASE_URL}/chat/conversations/${conversationId}/messages`);
  const payload = (await response.json()) as MessagesResponse;

  if (!response.ok || !payload?.success || !Array.isArray(payload.data)) {
    const message = payload?.message || `Failed to load messages (status ${response.status})`;
    throw new Error(message);
  }

  return payload.data;
}
