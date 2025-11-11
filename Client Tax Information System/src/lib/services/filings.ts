import { authenticatedFetch } from "../auth";

const API_BASE_URL = import.meta.env.VITE_API_URL || "http://localhost:5000/api";

export interface Filing {
  id: number;
  clientId: number;
  taxType: string;
  period: string;
  status: string;
  totalSales?: number;
  taxableSales?: number;
  gstRate?: number;
  outputTax?: number;
  inputTaxCredit?: number;
  netGstPayable?: number;
  notes?: string;
}

export interface ScheduleRow {
  id: number;
  description: string;
  amount: number;
  taxable: number;
}

export interface FilingDocument {
  id: number;
  name: string;
  version: number;
  uploadedBy: string;
  date: string;
}

export interface FilingHistory {
  date: string;
  user: string;
  action: string;
  changes: string;
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

export async function fetchFiling(id: number): Promise<Filing> {
  const response = await authenticatedFetch(`${API_BASE_URL}/filings/${id}`);
  const payload = await parseResponse<Filing>(response);
  return payload.data;
}

export async function fetchFilingSchedules(filingId: number): Promise<ScheduleRow[]> {
  const response = await authenticatedFetch(`${API_BASE_URL}/filings/${filingId}/schedules`);
  const payload = await parseResponse<ScheduleRow[]>(response);
  return payload.data;
}

export async function fetchFilingDocuments(filingId: number): Promise<FilingDocument[]> {
  const response = await authenticatedFetch(`${API_BASE_URL}/filings/${filingId}/documents`);
  const payload = await parseResponse<FilingDocument[]>(response);
  return payload.data;
}

export async function fetchFilingHistory(filingId: number): Promise<FilingHistory[]> {
  const response = await authenticatedFetch(`${API_BASE_URL}/filings/${filingId}/history`);
  const payload = await parseResponse<FilingHistory[]>(response);
  return payload.data;
}

export async function updateFiling(id: number, filing: Partial<Filing>): Promise<Filing> {
  const response = await authenticatedFetch(`${API_BASE_URL}/filings/${id}`, {
    method: "PUT",
    body: JSON.stringify(filing),
  });
  const payload = await parseResponse<Filing>(response);
  return payload.data;
}

export async function submitFiling(id: number): Promise<Filing> {
  const response = await authenticatedFetch(`${API_BASE_URL}/filings/${id}/submit`, {
    method: "POST",
    body: JSON.stringify({}),
  });
  const payload = await parseResponse<Filing>(response);
  return payload.data;
}
