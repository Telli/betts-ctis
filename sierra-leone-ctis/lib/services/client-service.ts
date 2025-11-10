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
  // Backend enums: Individual=0, Partnership=1, Corporation=2, NGO=3
  clientType: string | number;
  // Backend enums: Large=0, Medium=1, Small=2, Micro=3
  taxpayerCategory: string | number;
  annualTurnover: number;
  tin?: string;
  // Backend enum: Active=0, Inactive=1, Suspended=2
  status: string | number;
  // Computed properties from backend
  firstName?: string;
  lastName?: string;
  // Legacy properties for compatibility
  name?: string;
  type?: string;
  category?: string;
  contact?: string;
  lastFiling?: string;
  taxLiability?: string | number;
  complianceScore?: number;
}

export const ClientService = {
  /**
   * Get all clients
   */
  getAll: async (): Promise<ClientDto[]> => {
    const data = await apiRequest<any[]>('/api/clients');
    const normalized = (Array.isArray(data) ? data : []).map((c: any) => {
      const fn = c.firstName ?? c.FirstName ?? ''
      const ln = c.lastName ?? c.LastName ?? ''
      const nm = c.name ?? c.Name ?? ''
      const bn = c.businessName ?? c.BusinessName ?? c.CompanyName ?? ''
      const displayName = bn || nm || [fn, ln].filter(Boolean).join(' ')
      const contact = c.contactPerson ?? c.ContactPerson ?? c.contact ?? c.Contact ?? [fn, ln].filter(Boolean).join(' ')
      return {
        clientId: c.clientId ?? c.ClientId,
        clientNumber: c.clientNumber ?? c.ClientNumber ?? '',
        businessName: displayName,
        contactPerson: contact ?? '',
        email: c.email ?? c.Email ?? '',
        phoneNumber: c.phoneNumber ?? c.PhoneNumber ?? '',
        address: c.address ?? c.Address ?? '',
        clientType: c.clientType ?? c.ClientType ?? c.type ?? '',
        taxpayerCategory: c.taxpayerCategory ?? c.TaxpayerCategory ?? c.category ?? '',
        annualTurnover: c.annualTurnover ?? c.AnnualTurnover ?? 0,
        tin: c.tin ?? c.TIN ?? c.Tin,
        status: c.status ?? c.Status ?? c.kpiStatus ?? '',
        firstName: fn,
        lastName: ln,
        name: nm,
        category: c.category,
        type: c.type,
        contact: c.contact,
        lastFiling: c.lastFiling,
        taxLiability: c.taxLiability,
        complianceScore: c.complianceScore ?? c.ComplianceScore,
      } as ClientDto
    })
    return normalized
  },

  /**
   * Get client by id
   */
  getById: async (id: number): Promise<ClientDto> => {
    const c = await apiRequest<any>(`/api/clients/${id}`);
    const fn = c.firstName ?? c.FirstName ?? ''
    const ln = c.lastName ?? c.LastName ?? ''
    const nm = c.name ?? c.Name ?? ''
    const bn = c.businessName ?? c.BusinessName ?? c.CompanyName ?? ''
    const displayName = bn || nm || [fn, ln].filter(Boolean).join(' ')
    const contact = c.contactPerson ?? c.ContactPerson ?? c.contact ?? c.Contact ?? [fn, ln].filter(Boolean).join(' ')
    return {
      clientId: c.clientId ?? c.ClientId,
      clientNumber: c.clientNumber ?? c.ClientNumber ?? '',
      businessName: displayName,
      contactPerson: contact ?? '',
      email: c.email ?? c.Email ?? '',
      phoneNumber: c.phoneNumber ?? c.PhoneNumber ?? '',
      address: c.address ?? c.Address ?? '',
      clientType: c.clientType ?? c.ClientType ?? c.type ?? '',
      taxpayerCategory: c.taxpayerCategory ?? c.TaxpayerCategory ?? c.category ?? '',
      annualTurnover: c.annualTurnover ?? c.AnnualTurnover ?? 0,
      tin: c.tin ?? c.TIN ?? c.Tin,
      status: c.status ?? c.Status ?? c.kpiStatus ?? '',
      firstName: fn,
      lastName: ln,
      name: nm,
      category: c.category,
      type: c.type,
      contact: c.contact,
      lastFiling: c.lastFiling,
      taxLiability: c.taxLiability,
      complianceScore: c.complianceScore ?? c.ComplianceScore,
    } as ClientDto
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
