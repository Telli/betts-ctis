import { authenticatedFetch } from "../auth";

const API_BASE_URL = import.meta.env.VITE_API_URL || "http://localhost:5000/api";

export interface User {
  id: number;
  name: string;
  email: string;
  role: string;
  status: string;
}

export interface AuditLog {
  id: number;
  timestamp: string;
  actor: string;
  role: string;
  actingFor: string | null;
  action: string;
  ip: string;
}

export interface TaxRate {
  type: string;
  rate: number | string;
  applicableTo: string;
}

export interface JobStatus {
  name: string;
  status: string;
  queueSize?: number;
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

export async function fetchUsers(): Promise<User[]> {
  const response = await authenticatedFetch(`${API_BASE_URL}/admin/users`);
  const payload = await parseResponse<User[]>(response);
  return payload.data;
}

export async function createUser(user: Omit<User, "id">): Promise<User> {
  const response = await authenticatedFetch(`${API_BASE_URL}/admin/users`, {
    method: "POST",
    body: JSON.stringify(user),
  });
  const payload = await parseResponse<User>(response);
  return payload.data;
}

export async function updateUser(id: number, user: Partial<User>): Promise<User> {
  const response = await authenticatedFetch(`${API_BASE_URL}/admin/users/${id}`, {
    method: "PUT",
    body: JSON.stringify(user),
  });
  const payload = await parseResponse<User>(response);
  return payload.data;
}

export async function fetchAuditLogs(filters?: {
  searchTerm?: string;
  startDate?: string;
  endDate?: string;
}): Promise<AuditLog[]> {
  const params = new URLSearchParams();
  if (filters?.searchTerm) params.set("search", filters.searchTerm);
  if (filters?.startDate) params.set("startDate", filters.startDate);
  if (filters?.endDate) params.set("endDate", filters.endDate);

  const response = await authenticatedFetch(
    `${API_BASE_URL}/admin/audit-logs${params.toString() ? `?${params.toString()}` : ""}`
  );
  const payload = await parseResponse<AuditLog[]>(response);
  return payload.data;
}

export async function fetchTaxRates(): Promise<TaxRate[]> {
  const response = await authenticatedFetch(`${API_BASE_URL}/admin/tax-rates`);
  const payload = await parseResponse<TaxRate[]>(response);
  return payload.data;
}

export async function updateTaxRate(type: string, rate: TaxRate): Promise<TaxRate> {
  const response = await authenticatedFetch(`${API_BASE_URL}/admin/tax-rates/${type}`, {
    method: "PUT",
    body: JSON.stringify(rate),
  });
  const payload = await parseResponse<TaxRate>(response);
  return payload.data;
}

export async function fetchJobStatuses(): Promise<JobStatus[]> {
  const response = await authenticatedFetch(`${API_BASE_URL}/admin/jobs`);
  const payload = await parseResponse<JobStatus[]>(response);
  return payload.data;
}
