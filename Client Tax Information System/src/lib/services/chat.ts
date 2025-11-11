import { authenticatedFetch } from "../auth";

const API_BASE_URL = import.meta.env.VITE_API_URL || "http://localhost:5000/api";

export interface Conversation {
  id: number;
  client: string;
  subject: string;
  lastMessage: string;
  timestamp: string;
  status: string;
  unread: number;
  assignedTo: string;
}

export interface Message {
  id: number;
  sender: string;
  name: string;
  content: string;
  timestamp: string;
  isInternal: boolean;
}

interface ApiResponse<T> {
  success: boolean;
  data: T;
  meta?: Record<string, unknown>;
  message?: string;
}

async function parseResponse<T>(response: Response): Promise<ApiResponse<T>> {
  const isJson = response.headers.get("content-type")?.includes("application/json");
  const payload: ApiResponse<T> | undefined = isJson ? await response.json() : undefined;

  if (!payload || !payload.success || !response.ok) {
    const message = payload?.message || `Request failed with status ${response.status}`;
    throw new Error(message);
  }

  return payload;
}

export async function fetchConversations(filters?: {
  searchTerm?: string;
  status?: string;
}): Promise<Conversation[]> {
  const params = new URLSearchParams();
  if (filters?.searchTerm) params.set("search", filters.searchTerm);
  if (filters?.status && filters.status !== "all") params.set("status", filters.status);

  const response = await authenticatedFetch(
    `${API_BASE_URL}/conversations${params.toString() ? `?${params.toString()}` : ""}`
  );
  const payload = await parseResponse<Conversation[]>(response);
  return payload.data;
}

export async function fetchMessages(conversationId: number): Promise<Message[]> {
  const response = await authenticatedFetch(`${API_BASE_URL}/conversations/${conversationId}/messages`);
  const payload = await parseResponse<Message[]>(response);
  return payload.data;
}

export async function sendMessage(
  conversationId: number,
  content: string,
  isInternal: boolean
): Promise<Message> {
  const response = await authenticatedFetch(
    `${API_BASE_URL}/conversations/${conversationId}/messages`,
    {
      method: "POST",
      body: JSON.stringify({ content, isInternal }),
    }
  );
  const payload = await parseResponse<Message>(response);
  return payload.data;
}

export async function updateConversationStatus(
  conversationId: number,
  status: string
): Promise<Conversation> {
  const response = await authenticatedFetch(`${API_BASE_URL}/conversations/${conversationId}`, {
    method: "PATCH",
    body: JSON.stringify({ status }),
  });
  const payload = await parseResponse<Conversation>(response);
  return payload.data;
}

export async function assignConversation(
  conversationId: number,
  userId: string
): Promise<Conversation> {
  const response = await authenticatedFetch(`${API_BASE_URL}/conversations/${conversationId}`, {
    method: "PATCH",
    body: JSON.stringify({ assignedTo: userId }),
  });
  const payload = await parseResponse<Conversation>(response);
  return payload.data;
}
