/**
 * Authentication service for the BettsTax backend
 */

import { apiRequest, setToken, removeToken } from '../api-client';

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

export interface LoginResponse {
  token: string;
}

export interface AuthUser {
  id: string;
  email: string;
  firstName: string;
  lastName: string;
}

export const AuthService = {
  /**
   * Register a new user
   */
  register: async (data: RegisterDto): Promise<void> => {
    await apiRequest('/api/auth/register', {
      method: 'POST',
      body: data,
    });
  },

  /**
   * Login a user and store the token
   */
  login: async (data: LoginDto): Promise<LoginResponse> => {
    const response = await apiRequest<LoginResponse>('/api/auth/login', {
      method: 'POST',
      body: data,
    });
    if (response.token) {
      setToken(response.token);
    }
    return response;
  },

  /**
   * Logout the current user
   */
  logout: (): void => {
    removeToken();
  }
};
