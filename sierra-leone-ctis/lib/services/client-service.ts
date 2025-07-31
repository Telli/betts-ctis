/**
 * Client service for the BettsTax backend
 */

import { apiRequest } from '../api-client';

export interface ClientDto {
  clientId?: number;
  clientNumber: string;
  businessName: string;
  contactPerson: string;
  email: string;
  phoneNumber: string;
  address: string;
  clientType: string;
  taxpayerCategory: string;
  annualTurnover: number;
  tin?: string;
  status: string;
  // Computed properties from backend
  firstName?: string;
  lastName?: string;
  // Legacy properties for compatibility
  name?: string;
  type?: string;
  category?: string;
  contact?: string;
  lastFiling?: string;
  taxLiability?: string;
  complianceScore?: number;
}

export const ClientService = {
  /**
   * Get all clients
   */
  getAll: async (): Promise<ClientDto[]> => {
    return await apiRequest<ClientDto[]>('/api/clients');
  },

  /**
   * Get client by id
   */
  getById: async (id: number): Promise<ClientDto> => {
    return await apiRequest<ClientDto>(`/api/clients/${id}`);
  },

  /**
   * Create a new client
   */
  create: async (client: ClientDto): Promise<ClientDto> => {
    return await apiRequest<ClientDto>('/api/clients', {
      method: 'POST',
      body: client,
    });
  },

  /**
   * Update an existing client
   */
  update: async (id: number, client: ClientDto): Promise<ClientDto> => {
    return await apiRequest<ClientDto>(`/api/clients/${id}`, {
      method: 'PUT',
      body: client,
    });
  },

  /**
   * Delete a client
   */
  delete: async (id: number): Promise<void> => {
    await apiRequest(`/api/clients/${id}`, {
      method: 'DELETE',
    });
  },
};
