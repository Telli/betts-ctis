import { API_BASE_URL, authenticatedFetch } from "../auth";

export interface ClientSummary {
  id: number;
  name: string;
  tin: string;
  segment: string;
  industry: string;
  status: string;
  complianceScore: number;
  assignedTo: string;
}

interface ClientsResponse {
  success: boolean;
  data?: ClientSummary[];
  message?: string;
}

export async function fetchClients(): Promise<ClientSummary[]> {
  const response = await authenticatedFetch(`${API_BASE_URL}/clients`);
  const payload = (await response.json()) as ClientsResponse;

  if (!response.ok || !payload?.success || !Array.isArray(payload.data)) {
    const message = payload?.message || `Failed to load clients (status ${response.status})`;
    throw new Error(message);
  }

  return payload.data;
}
