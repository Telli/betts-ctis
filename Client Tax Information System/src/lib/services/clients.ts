import { authenticatedFetch } from "../auth";

const API_BASE_URL = import.meta.env.VITE_API_URL || "http://localhost:5000/api";

export interface Client {
  id: number;
  name: string;
  tin: string;
  segment: string;
  industry: string;
  status: string;
  complianceScore: number;
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

export async function fetchClients(filters?: {
  searchTerm?: string;
  segment?: string;
  status?: string;
}): Promise<Client[]> {
  const params = new URLSearchParams();
  if (filters?.searchTerm) params.set("search", filters.searchTerm);
  if (filters?.segment && filters.segment !== "all") params.set("segment", filters.segment);
  if (filters?.status && filters.status !== "all") params.set("status", filters.status);

  const response = await authenticatedFetch(
    `${API_BASE_URL}/clients${params.toString() ? `?${params.toString()}` : ""}`
  );
  const payload = await parseResponse<Client[]>(response);
  return payload.data;
}

export async function fetchClientById(id: number): Promise<Client> {
  const response = await authenticatedFetch(`${API_BASE_URL}/clients/${id}`);
  const payload = await parseResponse<Client>(response);
  return payload.data;
}

export async function createClient(client: Omit<Client, "id">): Promise<Client> {
  const response = await authenticatedFetch(`${API_BASE_URL}/clients`, {
    method: "POST",
    body: JSON.stringify(client),
  });
  const payload = await parseResponse<Client>(response);
  return payload.data;
}

export async function updateClient(id: number, client: Partial<Client>): Promise<Client> {
  const response = await authenticatedFetch(`${API_BASE_URL}/clients/${id}`, {
    method: "PUT",
    body: JSON.stringify(client),
  });
  const payload = await parseResponse<Client>(response);
  return payload.data;
}
