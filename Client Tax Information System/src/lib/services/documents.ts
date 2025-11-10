import { authenticatedFetch } from "../auth";

const API_BASE_URL = import.meta.env.VITE_API_URL || "http://localhost:5000/api";

export interface Document {
  id: number;
  name: string;
  type: string;
  client: string;
  year: number;
  taxType: string;
  version: number;
  uploadedBy: string;
  uploadDate: string;
  hash: string;
  status: string;
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

export async function fetchDocuments(filters?: {
  searchTerm?: string;
  type?: string;
  year?: string;
  clientId?: number;
}): Promise<Document[]> {
  const params = new URLSearchParams();
  if (filters?.searchTerm) params.set("search", filters.searchTerm);
  if (filters?.type && filters.type !== "all") params.set("type", filters.type);
  if (filters?.year && filters.year !== "all") params.set("year", filters.year);
  if (filters?.clientId) params.set("clientId", String(filters.clientId));

  const response = await authenticatedFetch(
    `${API_BASE_URL}/documents${params.toString() ? `?${params.toString()}` : ""}`
  );
  const payload = await parseResponse<Document[]>(response);
  return payload.data;
}

export async function uploadDocument(
  file: File,
  metadata: {
    type: string;
    clientId: number;
    taxType: string;
    year: number;
  }
): Promise<Document> {
  const formData = new FormData();
  formData.append("file", file);
  formData.append("type", metadata.type);
  formData.append("clientId", String(metadata.clientId));
  formData.append("taxType", metadata.taxType);
  formData.append("year", String(metadata.year));

  const token = (await import("../auth")).getToken();
  const response = await fetch(`${API_BASE_URL}/documents/upload`, {
    method: "POST",
    credentials: "include",
    headers: {
      Authorization: `Bearer ${token}`,
    },
    body: formData,
  });

  const payload = await parseResponse<Document>(response);
  return payload.data;
}

export async function downloadDocument(id: number): Promise<Blob> {
  const response = await authenticatedFetch(`${API_BASE_URL}/documents/${id}/download`);

  if (!response.ok) {
    throw new Error(`Failed to download document: ${response.status}`);
  }

  return await response.blob();
}
