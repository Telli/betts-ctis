/**
 * Authentication service for the BettsTax backend
 */

import { apiRequest } from '../api-client';

export interface RegisterDto {
  firstName: string;
  lastName: string;
  email: string;
  password: string;
}

export interface LoginDto {
  Email: string;
  Password: string;
}

export interface ChangePasswordDto {
  currentPassword: string;
  newPassword: string;
}

export interface AuthUser {
  id: string;
  email: string;
  firstName: string;
  lastName: string;
}

export interface AuthSession {
  userId: string;
  email: string;
  roles: string[];
  allClaims?: { type: string; value: string }[];
}

// CSRF token cache
let csrfToken: string | null = null;

export const AuthService = {
  /**
   * Get CSRF token from backend
   * Phase 1: CSRF protection implementation
   */
  getCsrfToken: async (): Promise<string> => {
    if (csrfToken) {
      return csrfToken;
    }
    
    const response = await apiRequest<{ token: string }>('/api/auth/csrf-token', {
      method: 'GET'
    });
    
    csrfToken = response.token;
    return csrfToken;
  },

  /**
   * Clear cached CSRF token (call after logout or token refresh)
   */
  clearCsrfToken: (): void => {
    csrfToken = null;
  },

  /**
   * Register a new user
   * Phase 1: Uses FluentValidation on backend
   */
  register: async (data: RegisterDto): Promise<void> => {
    const token = await AuthService.getCsrfToken();
    await apiRequest('/api/auth/register', {
      method: 'POST',
      body: data,
      headers: {
        'X-CSRF-Token': token
      }
    });
  },

  /**
   * Login a user - JWT stored in HTTP-only cookie
   * Phase 1: Secure cookie-based authentication
   */
  login: async (data: LoginDto): Promise<void> => {
    const token = await AuthService.getCsrfToken();
    await apiRequest('/api/auth/login', {
      method: 'POST',
      body: data,
      headers: {
        'X-CSRF-Token': token
      }
    });
    // Clear CSRF token after login to force refresh
    AuthService.clearCsrfToken();
  },

  /**
   * Refresh access token using refresh token from HTTP-only cookie
   * Phase 1: Automatic token rotation
   */
  refresh: async (): Promise<void> => {
    await apiRequest('/api/auth/refresh', {
      method: 'POST'
    });
    // Clear CSRF token after refresh to get new one
    AuthService.clearCsrfToken();
  },

  /**
   * Logout the current user and revoke refresh token
   * Phase 1: Secure logout with token revocation
   */
  logout: async (): Promise<void> => {
    try {
      await apiRequest('/api/auth/logout', {
        method: 'POST'
      });
    } finally {
      // Always clear CSRF token on logout
      AuthService.clearCsrfToken();
    }
  },

  /**
   * Change user password
   * Phase 1: Password change with token revocation
   */
  changePassword: async (data: ChangePasswordDto): Promise<void> => {
    const token = await AuthService.getCsrfToken();
    await apiRequest('/api/auth/change-password', {
      method: 'POST',
      body: data,
      headers: {
        'X-CSRF-Token': token
      }
    });
    // Clear CSRF token after password change
    AuthService.clearCsrfToken();
  },

  /**
   * Fetch the current authenticated session
   */
  getSession: async (): Promise<AuthSession> => {
    return apiRequest<AuthSession>('/api/auth/me', {
      method: 'GET'
    });
  }
};
