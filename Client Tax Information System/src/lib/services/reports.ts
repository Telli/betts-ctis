import { API_BASE_URL, authenticatedFetch } from "../auth";

export interface ReportType {
  id: string;
  name: string;
  description: string;
  iconKey: string;
}

export interface FilterOption {
  value: string;
  label: string;
}

export interface ReportFilters {
  clients: FilterOption[];
  taxTypes: FilterOption[];
}

interface ReportTypeResponse {
  success: boolean;
  data?: ReportType[];
  message?: string;
}

interface ReportFiltersResponse {
  success: boolean;
  data?: ReportFilters;
  message?: string;
}

export async function fetchReportTypes(): Promise<ReportType[]> {
  const response = await authenticatedFetch(`${API_BASE_URL}/reports/types`);
  const payload = (await response.json()) as ReportTypeResponse;

  if (!response.ok || !payload?.success || !Array.isArray(payload.data)) {
    const message = payload?.message || `Failed to load report types (status ${response.status})`;
    throw new Error(message);
  }

  return payload.data;
}

export async function fetchReportFilters(): Promise<ReportFilters> {
  const response = await authenticatedFetch(`${API_BASE_URL}/reports/filters`);
  const payload = (await response.json()) as ReportFiltersResponse;

  if (!response.ok || !payload?.success || !payload.data) {
    const message = payload?.message || `Failed to load report filters (status ${response.status})`;
    throw new Error(message);
  }

  return {
    clients: payload.data.clients ?? [],
    taxTypes: payload.data.taxTypes ?? [],
  };
}
