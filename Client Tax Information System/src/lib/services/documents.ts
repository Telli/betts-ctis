import { API_BASE_URL, authenticatedFetch } from "../auth";

export interface DocumentRecord {
  id: number;
  clientId?: number;
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

interface DocumentsResponse {
  success: boolean;
  data?: DocumentRecord[];
  message?: string;
}

export async function fetchDocuments(clientId?: number): Promise<DocumentRecord[]> {
  const params = new URLSearchParams();
  if (clientId) {
    params.set("clientId", String(clientId));
  }

  const response = await authenticatedFetch(
    `${API_BASE_URL}/documents${params.toString() ? `?${params.toString()}` : ""}`,
  );

  const payload = (await response.json()) as DocumentsResponse;

  if (!response.ok || !payload?.success || !Array.isArray(payload.data)) {
    const message = payload?.message || `Failed to load documents (status ${response.status})`;
    throw new Error(message);
  }

  return payload.data;
}
