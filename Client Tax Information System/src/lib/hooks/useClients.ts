import { useQuery, useMutation, useQueryClient } from "@tanstack/react-query";
import {
  fetchClients,
  fetchClientById,
  createClient,
  updateClient,
  type Client,
} from "../services/clients";

/**
 * Hook to fetch all clients with optional filters
 */
export function useClients(filters?: {
  searchTerm?: string;
  segment?: string;
  status?: string;
}) {
  return useQuery({
    queryKey: ["clients", filters],
    queryFn: () => fetchClients(filters),
    staleTime: 5 * 60 * 1000, // Fresh for 5 minutes
  });
}

/**
 * Hook to fetch a single client by ID
 */
export function useClient(id: number) {
  return useQuery({
    queryKey: ["client", id],
    queryFn: () => fetchClientById(id),
    enabled: !!id, // Only fetch if ID exists
    staleTime: 5 * 60 * 1000,
  });
}

/**
 * Hook to create a new client
 */
export function useCreateClient() {
  const queryClient = useQueryClient();

  return useMutation({
    mutationFn: (client: Omit<Client, "id">) => createClient(client),
    onSuccess: () => {
      // Invalidate and refetch clients list
      queryClient.invalidateQueries({ queryKey: ["clients"] });
    },
  });
}

/**
 * Hook to update an existing client
 */
export function useUpdateClient() {
  const queryClient = useQueryClient();

  return useMutation({
    mutationFn: ({ id, data }: { id: number; data: Partial<Client> }) =>
      updateClient(id, data),
    onSuccess: (updatedClient) => {
      // Update the cache for this specific client
      queryClient.setQueryData(["client", updatedClient.id], updatedClient);
      // Invalidate the clients list to refetch
      queryClient.invalidateQueries({ queryKey: ["clients"] });
    },
  });
}
