import { authenticatedFetch } from "../auth";

const API_BASE_URL = import.meta.env.VITE_API_URL || "http://localhost:5000/api";

export interface MonthlyTrend {
  month: string;
  compliance: number;
  timeliness: number;
  payments: number;
}

export interface ClientPerformance {
  name: string;
  score: number;
}

export interface KPIMetrics {
  complianceRate: number;
  avgTimeliness: number;
  paymentCompletion: number;
  docSubmission: number;
  engagementRate: number;
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

export async function fetchKPIMetrics(clientId?: number): Promise<KPIMetrics> {
  const params = new URLSearchParams();
  if (clientId) params.set("clientId", String(clientId));

  const response = await authenticatedFetch(
    `${API_BASE_URL}/kpis/metrics${params.toString() ? `?${params.toString()}` : ""}`
  );
  const payload = await parseResponse<KPIMetrics>(response);
  return payload.data;
}

export async function fetchMonthlyTrends(
  clientId?: number,
  months = 6
): Promise<MonthlyTrend[]> {
  const params = new URLSearchParams({ months: String(months) });
  if (clientId) params.set("clientId", String(clientId));

  const response = await authenticatedFetch(
    `${API_BASE_URL}/kpis/monthly-trends?${params.toString()}`
  );
  const payload = await parseResponse<MonthlyTrend[]>(response);
  return payload.data;
}

export async function fetchClientPerformance(limit = 10): Promise<ClientPerformance[]> {
  const params = new URLSearchParams({ limit: String(limit) });

  const response = await authenticatedFetch(
    `${API_BASE_URL}/kpis/client-performance?${params.toString()}`
  );
  const payload = await parseResponse<ClientPerformance[]>(response);
  return payload.data;
}
