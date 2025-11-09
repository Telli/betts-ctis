import { API_BASE_URL, authenticatedFetch } from "../auth";

export interface AdminUser {
  id: number;
  name: string;
  email: string;
  role: string;
  status: string;
}

export interface AuditLogEntry {
  id: number;
  timestamp: string;
  actor: string;
  role: string;
  actingFor?: string | null;
  action: string;
  ipAddress: string;
}

export interface TaxRate {
  type: string;
  rate: string;
  applicableTo: string;
}

export interface JobStatus {
  name: string;
  state: string;
  badgeText: string;
  badgeVariant: string;
}

interface ApiResponse<T> {
  success: boolean;
  data?: T;
  message?: string;
}

async function fetchCollection<T>(path: string): Promise<T> {
  const response = await authenticatedFetch(`${API_BASE_URL}${path}`);
  const payload = (await response.json()) as ApiResponse<T>;

  if (!response.ok || !payload?.success || !payload.data) {
    const message = payload?.message || `Failed to load ${path}`;
    throw new Error(message);
  }

  return payload.data;
}

export function fetchAdminUsers(): Promise<AdminUser[]> {
  return fetchCollection<AdminUser[]>("/admin/users");
}

export function fetchAuditLogs(): Promise<AuditLogEntry[]> {
  return fetchCollection<AuditLogEntry[]>("/admin/audit-logs");
}

export function fetchTaxRates(): Promise<TaxRate[]> {
  return fetchCollection<TaxRate[]>("/admin/tax-rates");
}

export function fetchJobStatuses(): Promise<JobStatus[]> {
  return fetchCollection<JobStatus[]>("/admin/jobs");
}
