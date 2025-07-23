/**
 * API Client for communicating with the BettsTax backend
 */

const API_BASE_URL = process.env.NEXT_PUBLIC_API_URL || 'http://localhost:5001';

/**
 * Get the authentication token from local storage or cookies
 */
export const getToken = (): string | null => {
  if (typeof window !== 'undefined') {
    // Try localStorage first
    const localToken = localStorage.getItem('auth_token');
    if (localToken) return localToken;

    // Fallback to cookies
    const cookies = document.cookie.split(';');
    for (const cookie of cookies) {
      const [name, value] = cookie.trim().split('=');
      if (name === 'auth_token') {
        return value;
      }
    }
  }
  return null;
};

/**
 * Set the authentication token in local storage and cookies
 */
export const setToken = (token: string): void => {
  if (typeof window !== 'undefined') {
    localStorage.setItem('auth_token', token);
    // Also set as cookie for middleware access
    document.cookie = `auth_token=${token}; path=/; max-age=${7 * 24 * 60 * 60}; SameSite=Lax`;
  }
};

/**
 * Remove the authentication token from local storage and cookies
 */
export const removeToken = (): void => {
  if (typeof window !== 'undefined') {
    localStorage.removeItem('auth_token');
    // Also remove cookie
    document.cookie = 'auth_token=; path=/; expires=Thu, 01 Jan 1970 00:00:00 GMT';
  }
};

/**
 * Check if the user is authenticated
 */
export const isAuthenticated = (): boolean => {
  return !!getToken();
};

type RequestOptions = {
  method?: 'GET' | 'POST' | 'PUT' | 'DELETE';
  body?: any;
  headers?: Record<string, string>;
  isFormData?: boolean;
  onBehalfOf?: number; // Client ID for on-behalf actions
  reason?: string; // Reason for on-behalf actions
  skipPermissionCheck?: boolean; // Skip permission validation
};

/**
 * Make an API request with authentication handling
 */
export const apiRequest = async <T>(
  endpoint: string, 
  options: RequestOptions = {}
): Promise<T> => {
  const { 
    method = 'GET', 
    body,
    headers: customHeaders = {},
    isFormData = false,
    onBehalfOf,
    reason,
    skipPermissionCheck
  } = options;

  const token = getToken();
  const headers: Record<string, string> = {
    ...customHeaders
  };

  if (token) {
    headers['Authorization'] = `Bearer ${token}`;
  }

  // Add on-behalf headers for permission tracking
  if (onBehalfOf) {
    headers['X-On-Behalf-Of'] = onBehalfOf.toString();
  }
  
  if (reason) {
    headers['X-Action-Reason'] = reason;
  }
  
  if (skipPermissionCheck) {
    headers['X-Skip-Permission-Check'] = 'true';
  }

  if (body && !isFormData && !headers['Content-Type']) {
    headers['Content-Type'] = 'application/json';
  }

  const config: RequestInit = {
    method,
    headers,
    credentials: 'include',
  };

  if (body) {
    config.body = isFormData ? body : JSON.stringify(body);
  }

  const response = await fetch(`${API_BASE_URL}${endpoint}`, config);
  
  // Handle unauthorized responses
  if (response.status === 401) {
    removeToken();
    if (typeof window !== 'undefined') {
      window.location.href = '/login';
    }
    throw new Error('Unauthorized');
  }

  // Handle permission denied responses
  if (response.status === 403) {
    const errorData = await response.json().catch(() => ({}));
    const error = new Error(errorData.message || 'Permission denied') as any;
    error.code = 'PERMISSION_DENIED';
    error.details = errorData;
    throw error;
  }

  // For responses like 204 No Content
  if (response.status === 204) {
    return {} as T;
  }

  if (!response.ok) {
    const errorData = await response.json().catch(() => ({}));
    const error = new Error(errorData.message || `API error: ${response.status}`) as any;
    error.code = errorData.code || 'API_ERROR';
    error.status = response.status;
    error.details = errorData;
    throw error;
  }

  // For endpoints that return no content
  if (response.headers.get('content-length') === '0') {
    return {} as T;
  }

  try {
    const data = await response.json();
    return data as T;
  } catch (error) {
    return {} as T;
  }
};

/**
 * Axios-like API client for easier integration with components
 */
export const api = {
  /**
   * Perform a GET request
   */
  get: async <T>(url: string, options: Omit<RequestOptions, 'method'> = {}): Promise<{ data: T }> => {
    const data = await apiRequest<T>(url, { ...options, method: 'GET' });
    return { data };
  },
  
  /**
   * Perform a POST request
   */
  post: async <T>(url: string, body?: any, options: Omit<RequestOptions, 'method' | 'body'> = {}): Promise<{ data: T }> => {
    const data = await apiRequest<T>(url, { ...options, body, method: 'POST' });
    return { data };
  },
  
  /**
   * Perform a PUT request
   */
  put: async <T>(url: string, body?: any, options: Omit<RequestOptions, 'method' | 'body'> = {}): Promise<{ data: T }> => {
    const data = await apiRequest<T>(url, { ...options, body, method: 'PUT' });
    return { data };
  },
  
  /**
   * Perform a DELETE request
   */
  delete: async <T>(url: string, options: Omit<RequestOptions, 'method'> = {}): Promise<{ data: T }> => {
    const data = await apiRequest<T>(url, { ...options, method: 'DELETE' });
    return { data };
  }
};

// Export the API client as apiClient for compatibility with existing code
export const apiClient = api;

// Permission-aware API utilities
export const PermissionAwareAPI = {
  /**
   * Make an API call on behalf of a client
   */
  onBehalfOf: (clientId: number, reason?: string) => ({
    get: async <T>(url: string, options: Omit<RequestOptions, 'method' | 'onBehalfOf' | 'reason'> = {}): Promise<{ data: T }> => {
      return api.get<T>(url, { ...options, onBehalfOf: clientId, reason });
    },
    post: async <T>(url: string, body?: any, options: Omit<RequestOptions, 'method' | 'body' | 'onBehalfOf' | 'reason'> = {}): Promise<{ data: T }> => {
      return api.post<T>(url, body, { ...options, onBehalfOf: clientId, reason });
    },
    put: async <T>(url: string, body?: any, options: Omit<RequestOptions, 'method' | 'body' | 'onBehalfOf' | 'reason'> = {}): Promise<{ data: T }> => {
      return api.put<T>(url, body, { ...options, onBehalfOf: clientId, reason });
    },
    delete: async <T>(url: string, options: Omit<RequestOptions, 'method' | 'onBehalfOf' | 'reason'> = {}): Promise<{ data: T }> => {
      return api.delete<T>(url, { ...options, onBehalfOf: clientId, reason });
    }
  }),

  /**
   * Check if API error is permission-related
   */
  isPermissionError: (error: any): boolean => {
    return error?.code === 'PERMISSION_DENIED' || error?.status === 403;
  },

  /**
   * Get permission error details
   */
  getPermissionErrorDetails: (error: any): { message: string, requiredLevel?: string, area?: string } => {
    return {
      message: error?.message || 'Permission denied',
      requiredLevel: error?.details?.requiredLevel,
      area: error?.details?.area
    };
  },

  /**
   * Retry API call with permission check bypass (admin only)
   */
  retryWithBypass: async <T>(
    originalCall: () => Promise<{ data: T }>,
    retryCount: number = 1
  ): Promise<{ data: T }> => {
    try {
      return await originalCall();
    } catch (error) {
      if (PermissionAwareAPI.isPermissionError(error) && retryCount > 0) {
        // Could implement retry logic with permission bypass header
        // This would need additional backend support
        throw error;
      }
      throw error;
    }
  }
};

// Error types for better type safety
export interface ApiError extends Error {
  code: string;
  status?: number;
  details?: any;
}

export interface PermissionError extends ApiError {
  code: 'PERMISSION_DENIED';
  details: {
    requiredLevel?: string;
    area?: string;
    clientId?: number;
    associateId?: string;
  };
}
