import { apiRequest } from '../api-client';

export interface ClientInvitationDto {
  email: string;
}

export interface ClientRegistrationDto {
  email: string;
  password: string;
  confirmPassword: string;
  firstName: string;
  lastName: string;
  businessName: string;
  phoneNumber: string;
  taxpayerIdentificationNumber?: string;
  taxpayerCategory: 'Large' | 'Medium' | 'Small' | 'Micro';
  clientType: 'Individual' | 'Partnership' | 'Corporation' | 'NGO';
  registrationToken: string;
  businessAddress?: string;
  contactPersonName?: string;
  contactPersonPhone?: string;
  annualTurnover?: number;
}

export interface SelfRegistrationDto {
  email: string;
}

export interface EmailVerificationDto {
  token: string;
}

export interface TokenValidationResult {
  isValid: boolean;
  email?: string;
  errorMessage?: string;
  expirationDate?: string;
}

export interface ResendVerificationDto {
  email: string;
}

export const EnrollmentService = {
  // Associate invites a client
  sendInvitation: async (data: ClientInvitationDto): Promise<void> => {
    await apiRequest('/api/clientenrollment/invite', {
      method: 'POST',
      body: data,
    });
  },

  // Validate registration token
  validateToken: async (token: string): Promise<TokenValidationResult> => {
    return await apiRequest<TokenValidationResult>(`/api/clientenrollment/validate-token/${token}`);
  },

  // Complete client registration
  completeRegistration: async (data: ClientRegistrationDto): Promise<void> => {
    await apiRequest('/api/clientenrollment/register', {
      method: 'POST',
      body: data,
    });
  },

  // Initiate self-registration
  initiateSelfRegistration: async (data: SelfRegistrationDto): Promise<void> => {
    await apiRequest('/api/clientenrollment/self-register', {
      method: 'POST',
      body: data,
    });
  },

  // Verify email address
  verifyEmail: async (data: EmailVerificationDto): Promise<void> => {
    await apiRequest('/api/clientenrollment/verify-email', {
      method: 'POST',
      body: data,
    });
  },

  // Resend email verification
  resendVerification: async (data: ResendVerificationDto): Promise<void> => {
    await apiRequest('/api/clientenrollment/resend-verification', {
      method: 'POST',
      body: data,
    });
  },

  // Cancel invitation (associates/admins only)
  cancelInvitation: async (invitationId: number): Promise<void> => {
    await apiRequest(`/api/clientenrollment/cancel-invitation/${invitationId}`, {
      method: 'POST',
    });
  },

  // Get pending invitations (associates/admins only)
  getPendingInvitations: async (): Promise<any[]> => {
    return await apiRequest<any[]>('/api/clientenrollment/pending-invitations');
  },
};