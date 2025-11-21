/**
 * API Client for communicating with the BettsTax backend
 */

const RAW_API_BASE_URL = process.env.NEXT_PUBLIC_API_URL || 'http://localhost:5001';

// Build a robust absolute URL from base + endpoint, avoiding double paths like /api/api/...
const buildUrl = (endpoint: string): string => {
  try {
    const base = RAW_API_BASE_URL;
    // If base is absolute (http/https), rely on WHATWG URL resolution
    if (base.startsWith('http://') || base.startsWith('https://')) {
      return new URL(endpoint, base.endsWith('/') ? base : base + '/').toString();
    }
    // If base is relative (e.g., '/api'), resolve against window origin in the browser
    if (typeof window !== 'undefined') {
      const originBase = new URL(base, window.location.origin);
      return new URL(endpoint, originBase.toString().endsWith('/') ? originBase.toString() : originBase.toString() + '/').toString();
    }
    // Fallback: simple concat (SSR without window)
    return `${base}${endpoint}`;
  } catch {
    return `${RAW_API_BASE_URL}${endpoint}`;
  }
};

type RequestOptions = {
  method?: 'GET' | 'POST' | 'PUT' | 'DELETE';
  body?: any;
  headers?: Record<string, string>;
  isFormData?: boolean;
  onBehalfOf?: number; // Client ID for on-behalf actions
  reason?: string; // Reason for on-behalf actions
  skipPermissionCheck?: boolean; // Skip permission validation
  responseType?: 'json' | 'blob' | 'text'; // How to parse the response body
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
    skipPermissionCheck,
    responseType = 'json'
  } = options;

  const headers: Record<string, string> = {
    ...customHeaders
  };


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

  const url = buildUrl(endpoint);
  let response = await fetch(url, config);
  
  // Handle unauthorized responses with automatic token refresh
  // Phase 1: Automatic refresh token rotation
  if (response.status === 401 && !endpoint.includes('/auth/refresh') && !endpoint.includes('/auth/login')) {
    try {
      // Attempt to refresh the token
      const refreshResponse = await fetch(buildUrl('/api/auth/refresh'), {
        method: 'POST',
        credentials: 'include'
      });
      
      if (refreshResponse.ok) {
        // Retry the original request with new token
        response = await fetch(url, config);
      } else {
        // Refresh failed, throw unauthorized error
        const error = new Error('Session expired') as ApiError;
        error.code = 'UNAUTHORIZED';
        error.status = 401;
        throw error;
      }
    } catch (refreshError) {
      // Refresh attempt failed
      const error = new Error('Unauthorized') as ApiError;
      error.code = 'UNAUTHORIZED';
      error.status = 401;
      throw error;
    }
  }
  
  // If still unauthorized after refresh attempt, throw error
  if (response.status === 401) {
    const error = new Error('Unauthorized') as ApiError;
    error.code = 'UNAUTHORIZED';
    error.status = 401;
    throw error;
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
    console.error(`API Error ${response.status} at ${endpoint}:`, errorData);
    console.error('Request URL:', url);
    console.error('Request body was:', body);
    if (errorData.errors) {
      console.error('Validation errors:', errorData.errors);
    }
    const error = new Error(errorData.message || errorData.title || `API error: ${response.status}`) as any;
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
    if (responseType === 'blob') {
      const data = await response.blob();
      return data as unknown as T;
    }
    if (responseType === 'text') {
      const data = await response.text();
      return data as unknown as T;
    }
    const ct = response.headers.get('content-type') || '';
    if (ct.includes('application/json') || responseType === 'json') {
      const data = await response.json();
      return data as T;
    }
    // Fallback: try text if not JSON
    const data = await response.text();
    return data as unknown as T;
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
