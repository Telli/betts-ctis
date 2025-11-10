import { authenticatedFetch } from "../auth";

const API_BASE_URL = import.meta.env.VITE_API_URL || "http://localhost:5000/api";

export interface Payment {
  id: number;
  client: string;
  taxType: string;
  period: string;
  amount: number;
  method: string;
  status: string;
  date: string;
  receiptNo: string;
}

export interface PaymentSummary {
  totalPaid: number;
  totalPending: number;
  totalOverdue: number;
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

export async function fetchPayments(filters?: {
  searchTerm?: string;
  status?: string;
  taxType?: string;
  clientId?: number;
}): Promise<Payment[]> {
  const params = new URLSearchParams();
  if (filters?.searchTerm) params.set("search", filters.searchTerm);
  if (filters?.status && filters.status !== "all") params.set("status", filters.status);
  if (filters?.taxType && filters.taxType !== "all") params.set("taxType", filters.taxType);
  if (filters?.clientId) params.set("clientId", String(filters.clientId));

  const response = await authenticatedFetch(
    `${API_BASE_URL}/payments${params.toString() ? `?${params.toString()}` : ""}`
  );
  const payload = await parseResponse<Payment[]>(response);
  return payload.data;
}

export async function fetchPaymentSummary(clientId?: number): Promise<PaymentSummary> {
  const params = new URLSearchParams();
  if (clientId) params.set("clientId", String(clientId));

  const response = await authenticatedFetch(
    `${API_BASE_URL}/payments/summary${params.toString() ? `?${params.toString()}` : ""}`
  );
  const payload = await parseResponse<PaymentSummary>(response);
  return payload.data;
}

export async function createPayment(
  payment: Omit<Payment, "id" | "receiptNo" | "date">
): Promise<Payment> {
  const response = await authenticatedFetch(`${API_BASE_URL}/payments`, {
    method: "POST",
    body: JSON.stringify(payment),
  });
  const payload = await parseResponse<Payment>(response);
  return payload.data;
}
