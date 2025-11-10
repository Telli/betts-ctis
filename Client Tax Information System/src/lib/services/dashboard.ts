import { authenticatedFetch } from "../auth";

const API_BASE_URL = import.meta.env.VITE_API_URL || "http://localhost:5000/api";

export interface FilingTrend {
  month: string;
  onTime: number;
  late: number;
}

export interface ComplianceDistribution {
  name: string;
  value: number;
  color: string;
}

export interface UpcomingDeadline {
  client: string;
  type: string;
  dueDate: string;
  daysLeft: number;
  status: string;
}

export interface RecentActivity {
  time: string;
  action: string;
  user: string;
}

export interface DashboardMetrics {
  clientComplianceRate: number;
  filingTimeliness: number;
  paymentCompletion: number;
  documentCompliance: number;
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

export async function fetchDashboardMetrics(clientId?: number): Promise<DashboardMetrics> {
  const params = new URLSearchParams();
  if (clientId) params.set("clientId", String(clientId));

  const response = await authenticatedFetch(
    `${API_BASE_URL}/dashboard/metrics${params.toString() ? `?${params.toString()}` : ""}`
  );
  const payload = await parseResponse<DashboardMetrics>(response);
  return payload.data;
}

export async function fetchFilingTrends(clientId?: number, months = 6): Promise<FilingTrend[]> {
  const params = new URLSearchParams({ months: String(months) });
  if (clientId) params.set("clientId", String(clientId));

  const response = await authenticatedFetch(
    `${API_BASE_URL}/dashboard/filing-trends?${params.toString()}`
  );
  const payload = await parseResponse<FilingTrend[]>(response);
  return payload.data;
}

export async function fetchComplianceDistribution(
  clientId?: number
): Promise<ComplianceDistribution[]> {
  const params = new URLSearchParams();
  if (clientId) params.set("clientId", String(clientId));

  const response = await authenticatedFetch(
    `${API_BASE_URL}/dashboard/compliance-distribution${
      params.toString() ? `?${params.toString()}` : ""
    }`
  );
  const payload = await parseResponse<ComplianceDistribution[]>(response);
  return payload.data;
}

export async function fetchUpcomingDeadlines(
  clientId?: number,
  limit = 10
): Promise<UpcomingDeadline[]> {
  const params = new URLSearchParams({ limit: String(limit) });
  if (clientId) params.set("clientId", String(clientId));

  const response = await authenticatedFetch(
    `${API_BASE_URL}/dashboard/upcoming-deadlines?${params.toString()}`
  );
  const payload = await parseResponse<UpcomingDeadline[]>(response);
  return payload.data;
}

export async function fetchRecentActivity(
  clientId?: number,
  limit = 10
): Promise<RecentActivity[]> {
  const params = new URLSearchParams({ limit: String(limit) });
  if (clientId) params.set("clientId", String(clientId));

  const response = await authenticatedFetch(
    `${API_BASE_URL}/dashboard/recent-activity?${params.toString()}`
  );
  const payload = await parseResponse<RecentActivity[]>(response);
  return payload.data;
}
