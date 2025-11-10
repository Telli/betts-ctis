import { API_BASE_URL, authenticatedFetch } from "../auth";

export interface KpiMetric {
  key: string;
  title: string;
  value: string;
  trendDirection: "up" | "down" | "neutral";
  trendValue: string;
  subtitle: string;
  color: string;
}

export interface KpiTrendPoint {
  month: string;
  compliance: number;
  timeliness: number;
  payments: number;
}

export interface ClientPerformance {
  name: string;
  score: number;
}

export interface PerformanceBreakdown {
  metric: string;
  score: number;
  color: string;
}

export interface KpiSummary {
  internalMetrics: KpiMetric[];
  clientMetrics: KpiMetric[];
  monthlyTrend: KpiTrendPoint[];
  clientPerformance: ClientPerformance[];
  performanceBreakdown: PerformanceBreakdown[];
}

interface KpiResponse {
  success: boolean;
  data?: KpiSummary;
  message?: string;
}

export async function fetchKpiSummary(clientId?: number): Promise<KpiSummary> {
  const params = new URLSearchParams();
  if (clientId) {
    params.set("clientId", String(clientId));
  }

  const response = await authenticatedFetch(
    `${API_BASE_URL}/kpis${params.toString() ? `?${params.toString()}` : ""}`,
  );

  const payload = (await response.json()) as KpiResponse;

  if (!response.ok || !payload?.success || !payload.data) {
    const message = payload?.message || `Failed to load KPI summary (status ${response.status})`;
    throw new Error(message);
  }

  const data = payload.data;
  return {
    internalMetrics: data.internalMetrics ?? [],
    clientMetrics: data.clientMetrics ?? [],
    monthlyTrend: data.monthlyTrend ?? [],
    clientPerformance: data.clientPerformance ?? [],
    performanceBreakdown: data.performanceBreakdown ?? [],
  };
}
