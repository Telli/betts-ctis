import { authenticatedFetch } from "../auth";

const API_BASE_URL = import.meta.env.VITE_API_URL || "http://localhost:5000/api";

export interface DeadlineDto {
  id: number;
  clientId?: number;
  clientName: string;
  taxTypeName: string;
  dueDate: string;
  status: string;
  priority: string;
  assignedTo: string;
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

export async function fetchUpcomingDeadlines(days = 30, clientId?: number): Promise<DeadlineDto[]> {
  const params = new URLSearchParams({ days: String(days) });
  if (clientId) {
    params.set("clientId", String(clientId));
  }

  const response = await authenticatedFetch(`${API_BASE_URL}/deadlines/upcoming?${params.toString()}`);
  const payload = await parseResponse<DeadlineDto[]>(response);
  return payload.data;
}

export async function fetchOverdueDeadlines(clientId?: number): Promise<DeadlineDto[]> {
  const params = new URLSearchParams();
  if (clientId) {
    params.set("clientId", String(clientId));
  }

  const response = await authenticatedFetch(
    `${API_BASE_URL}/deadlines/overdue${params.toString() ? `?${params.toString()}` : ""}`,
  );
  const payload = await parseResponse<DeadlineDto[]>(response);
  return payload.data;
}

export interface DeadlineStats {
  total: number;
  upcoming: number;
  dueSoon: number;
  overdue: number;
  thisWeek: number;
  thisMonth: number;
  byPriority: Record<string, number>;
  byType: Record<string, number>;
}

export async function fetchDeadlineStats(clientId?: number): Promise<DeadlineStats> {
  const params = new URLSearchParams();
  if (clientId) {
    params.set("clientId", String(clientId));
  }

  const response = await authenticatedFetch(
    `${API_BASE_URL}/deadlines/stats${params.toString() ? `?${params.toString()}` : ""}`,
  );
  const payload = await parseResponse<DeadlineStats>(response);
  return payload.data;
}
