import { apiClient } from '@/lib/api-client';

export interface PaymentDto {
  paymentId: number;
  clientId: number;
  clientName: string;
  clientNumber: string;
  taxYearId?: number;
  taxFilingId?: number;
  amount: number;
  method: PaymentMethod;
  paymentReference: string;
  paymentDate: string;
  status: PaymentStatus;
  approvedAt?: string;
  approvedBy?: string;
  approvedByName?: string;
  rejectionReason?: string;
  approvalWorkflow: string;
  createdAt: string;
}

export interface CreatePaymentDto {
  clientId: number;
  taxYearId?: number;
  taxFilingId?: number;
  amount: number;
  method: PaymentMethod;
  paymentReference: string;
  paymentDate: string;
}

export interface ApprovePaymentDto {
  comments?: string;
}

export interface RejectPaymentDto {
  rejectionReason: string;
}

export enum PaymentMethod {
  BankTransfer = 'BankTransfer',
  Cash = 'Cash',
  Check = 'Check',
  OnlinePayment = 'OnlinePayment',
  MobileMoney = 'MobileMoney'
}

export enum PaymentStatus {
  Pending = 'Pending',
  Approved = 'Approved',
  Rejected = 'Rejected',
  Processing = 'Processing',
  Completed = 'Completed',
  Failed = 'Failed'
}

export interface PaymentListResponse {
  success: boolean;
  data: PaymentDto[];
  pagination: {
    currentPage: number;
    pageSize: number;
    totalCount: number;
    totalPages: number;
  };
}

export interface PaymentResponse {
  success: boolean;
  data: PaymentDto;
}

export const PaymentService = {
  /**
   * Get paginated list of payments with optional filtering
   */
  getPayments: async (
    page: number = 1,
    pageSize: number = 20,
    search?: string,
    status?: PaymentStatus,
    clientId?: number
  ): Promise<PaymentListResponse> => {
    const params = new URLSearchParams({
      page: page.toString(),
      pageSize: pageSize.toString(),
    });

    if (search) params.append('search', search);
    if (status) params.append('status', status);
    if (clientId) params.append('clientId', clientId.toString());

    const response = await apiClient.get<PaymentListResponse>(`/api/payments?${params}`);
    return response.data;
  },

  /**
   * Get specific payment by ID
   */
  getPaymentById: async (id: number): Promise<PaymentResponse> => {
    const response = await apiClient.get<PaymentResponse>(`/api/payments/${id}`);
    return response.data;
  },

  /**
   * Get payments for a specific client
   */
  getClientPayments: async (clientId: number): Promise<{ success: boolean; data: PaymentDto[] }> => {
    const response = await apiClient.get<{ success: boolean; data: PaymentDto[] }>(`/api/payments/client/${clientId}`);
    return response.data;
  },

  /**
   * Get pending payment approvals
   */
  getPendingApprovals: async (): Promise<{ success: boolean; data: PaymentDto[] }> => {
    const response = await apiClient.get<{ success: boolean; data: PaymentDto[] }>('/api/payments/pending-approvals');
    return response.data;
  },

  /**
   * Get payments by tax filing
   */
  getPaymentsByTaxFiling: async (taxFilingId: number): Promise<{ success: boolean; data: PaymentDto[] }> => {
    const response = await apiClient.get<{ success: boolean; data: PaymentDto[] }>(`/api/payments/tax-filing/${taxFilingId}`);
    return response.data;
  },

  /**
   * Create a new payment
   */
  createPayment: async (payment: CreatePaymentDto): Promise<PaymentResponse> => {
    const response = await apiClient.post<PaymentResponse>('/api/payments', payment);
    return response.data;
  },

  /**
   * Update an existing payment
   */
  updatePayment: async (id: number, payment: CreatePaymentDto): Promise<PaymentResponse> => {
    const response = await apiClient.put<PaymentResponse>(`/api/payments/${id}`, payment);
    return response.data;
  },

  /**
   * Delete a payment (only pending payments)
   */
  deletePayment: async (id: number): Promise<{ success: boolean; message: string }> => {
    const response = await apiClient.delete<{ success: boolean; message: string }>(`/api/payments/${id}`);
    return response.data;
  },

  /**
   * Approve a payment
   */
  approvePayment: async (id: number, approval: ApprovePaymentDto): Promise<PaymentResponse> => {
    const response = await apiClient.post<PaymentResponse>(`/api/payments/${id}/approve`, approval);
    return response.data;
  },

  /**
   * Reject a payment
   */
  rejectPayment: async (id: number, rejection: RejectPaymentDto): Promise<PaymentResponse> => {
    const response = await apiClient.post<PaymentResponse>(`/api/payments/${id}/reject`, rejection);
    return response.data;
  }
};

export default PaymentService;