import { useQuery, useMutation, useQueryClient } from "@tanstack/react-query";
import {
  fetchPayments,
  fetchPaymentSummary,
  createPayment,
  type Payment,
} from "../services/payments";

/**
 * Hook to fetch payments with optional filters
 */
export function usePayments(filters?: {
  searchTerm?: string;
  status?: string;
  taxType?: string;
  clientId?: number;
}) {
  return useQuery({
    queryKey: ["payments", filters],
    queryFn: () => fetchPayments(filters),
    staleTime: 2 * 60 * 1000, // Fresh for 2 minutes
  });
}

/**
 * Hook to fetch payment summary statistics
 */
export function usePaymentSummary(clientId?: number) {
  return useQuery({
    queryKey: ["paymentSummary", clientId],
    queryFn: () => fetchPaymentSummary(clientId),
    staleTime: 2 * 60 * 1000,
  });
}

/**
 * Hook to create a new payment
 */
export function useCreatePayment() {
  const queryClient = useQueryClient();

  return useMutation({
    mutationFn: (payment: Omit<Payment, "id" | "receiptNo" | "date">) =>
      createPayment(payment),
    onSuccess: () => {
      // Invalidate and refetch both payments and summary
      queryClient.invalidateQueries({ queryKey: ["payments"] });
      queryClient.invalidateQueries({ queryKey: ["paymentSummary"] });
    },
  });
}

/**
 * Hook to fetch payments data (payments + summary)
 */
export function usePaymentsData(filters?: {
  searchTerm?: string;
  status?: string;
  taxType?: string;
  clientId?: number;
}) {
  const payments = usePayments(filters);
  const summary = usePaymentSummary(filters?.clientId);

  return {
    payments,
    summary,
    isLoading: payments.isLoading || summary.isLoading,
    isError: payments.isError || summary.isError,
    error: payments.error || summary.error,
  };
}
