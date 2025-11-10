import { API_BASE_URL, authenticatedFetch } from "../auth";

export interface PaymentRecord {
  id: number;
  clientId?: number;
  client: string;
  taxType: string;
  period: string;
  amount: number;
  method: string;
  status: string;
  date: string;
  receiptNumber: string;
}

export interface PaymentSummary {
  paid: number;
  pending: number;
  overdue: number;
}

export interface PaymentsPayload {
  items: PaymentRecord[];
  summary: PaymentSummary;
}

interface PaymentsResponse {
  success: boolean;
  data?: PaymentsPayload;
  message?: string;
}

export async function fetchPayments(clientId?: number): Promise<PaymentsPayload> {
  const params = new URLSearchParams();
  if (clientId) {
    params.set("clientId", String(clientId));
  }

  const response = await authenticatedFetch(
    `${API_BASE_URL}/payments${params.toString() ? `?${params.toString()}` : ""}`,
  );
  const payload = (await response.json()) as PaymentsResponse;

  if (!response.ok || !payload?.success || !payload.data) {
    const message = payload?.message || `Failed to load payments (status ${response.status})`;
    throw new Error(message);
  }

  return {
    items: payload.data.items ?? [],
    summary: payload.data.summary ?? { paid: 0, pending: 0, overdue: 0 },
  };
}
