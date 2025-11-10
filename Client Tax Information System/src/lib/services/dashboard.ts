import { API_BASE_URL, authenticatedFetch } from "../auth";

export interface DashboardMetric {
  key: string;
  title: string;
  value: string;
  trendDirection: "up" | "down" | "neutral";
  trendValue: string;
  subtitle: string;
  color: string;
}

export interface DashboardTrendPoint {
  month: string;
  onTime: number;
  late: number;
}

export interface DistributionSlice {
  name: string;
  value: number;
  color: string;
}

export interface DashboardDeadline {
  id: number;
  client: string;
  taxType: string;
  dueDate: string;
  status: string;
}

export interface DashboardActivity {
  timeDescription: string;
  action: string;
  user: string;
}

export interface DashboardSummary {
  metrics: DashboardMetric[];
  filingTrends: DashboardTrendPoint[];
  complianceDistribution: DistributionSlice[];
  upcomingDeadlines: DashboardDeadline[];
  recentActivity: DashboardActivity[];
}

interface DashboardResponse {
  success: boolean;
  data?: DashboardSummary;
  message?: string;
}

export async function fetchDashboardSummary(options: { clientId?: number; daysAhead?: number } = {}): Promise<DashboardSummary> {
  const params = new URLSearchParams();
  if (options.clientId) {
    params.set("clientId", String(options.clientId));
  }
  if (options.daysAhead) {
    params.set("daysAhead", String(options.daysAhead));
  }

  const query = params.toString();
  const response = await authenticatedFetch(
    `${API_BASE_URL}/dashboard/summary${query ? `?${query}` : ""}`,
  );

  const payload = (await response.json()) as DashboardResponse;

  if (!response.ok || !payload?.success || !payload.data) {
    const message = payload?.message || `Failed to load dashboard summary (status ${response.status})`;
    throw new Error(message);
  }

  const data = payload.data;
  return {
    metrics: data.metrics ?? [],
    filingTrends: data.filingTrends ?? [],
    complianceDistribution: data.complianceDistribution ?? [],
    upcomingDeadlines: data.upcomingDeadlines ?? [],
    recentActivity: data.recentActivity ?? [],
  };
}
