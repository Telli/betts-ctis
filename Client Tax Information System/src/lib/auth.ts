/**
 * Authentication utilities and API calls
 */

const API_BASE_URL = import.meta.env.VITE_API_URL || 'http://localhost:5000/api';

export interface LoginRequest {
  email: string;
  password: string;
}

export interface UserInfo {
  userId: string;
  email: string;
  role: string;
  clientId?: number;
  clientName?: string;
}

export interface LoginResponse {
  success: boolean;
  message?: string;
  token?: string;
  refreshToken?: string;
  user?: UserInfo;
  expiresAt?: string;
  refreshTokenExpiresAt?: string;
}

const USER_KEY = 'user_info';
if (typeof window !== 'undefined') {
  try {
    window.localStorage.removeItem('auth_token');
    window.localStorage.removeItem('refresh_token');
  } catch (error) {
    // Silently fail - legacy tokens are optional
  }
}

let accessToken: string | null = null;
let accessTokenExpiresAt: number | null = null;
let refreshInFlight: Promise<string | null> | null = null;

const safeSessionStorage = typeof window !== 'undefined' ? window.sessionStorage : undefined;

function persistUser(user?: UserInfo) {
  if (!user || !safeSessionStorage) return;

  try {
    safeSessionStorage.setItem(USER_KEY, JSON.stringify(user));
  } catch (error) {
    // Silently fail - persistence is optional
  }
}

function clearUser() {
  if (!safeSessionStorage) return;

  try {
    safeSessionStorage.removeItem(USER_KEY);
  } catch (error) {
    // Silently fail - clearing is optional
  }
}

function setSession(token?: string, expiresAt?: string) {
  accessToken = token ?? null;
  accessTokenExpiresAt = expiresAt ? Date.parse(expiresAt) : null;
}

function clearSession() {
  setSession(undefined, undefined);
  clearUser();
}

function shouldRefreshToken() {
  if (!accessToken) return true;
  if (!accessTokenExpiresAt) return false;
  const threshold = accessTokenExpiresAt - Date.now();
  return threshold <= 60_000;
}

async function refreshAccessToken(): Promise<string | null> {
  if (refreshInFlight) {
    return refreshInFlight;
  }

  const pending = (async () => {
    try {
      const response = await fetch(`${API_BASE_URL}/auth/refresh`, {
        method: 'POST',
        credentials: 'include',
        headers: {
          'Content-Type': 'application/json',
        },
        body: JSON.stringify({}),
      });

      const isJson = response.headers.get('content-type')?.includes('application/json');
      const data: LoginResponse | undefined = isJson ? await response.json() : undefined;

      if (!response.ok || !data?.success || !data.token) {
        clearSession();
        return null;
      }

      setSession(data.token, data.expiresAt);
      if (data.user) {
        persistUser(data.user);
      }

      return data.token;
    } catch (error) {
      clearSession();
      return null;
    } finally {
      refreshInFlight = null;
    }
  })();

  refreshInFlight = pending;
  return pending;
}

export async function login(email: string, password: string): Promise<LoginResponse> {
  try {
    const response = await fetch(`${API_BASE_URL}/auth/login`, {
      method: 'POST',
      credentials: 'include',
      headers: {
        'Content-Type': 'application/json',
      },
      body: JSON.stringify({ email, password }),
    });

    const isJson = response.headers.get('content-type')?.includes('application/json');
    const data: LoginResponse | undefined = isJson ? await response.json() : undefined;

    if (!response.ok || !data) {
      const message = data?.message || `Authentication failed with status ${response.status}`;
      return {
        success: false,
        message,
      };
    }

    if (data.success && data.token) {
      setSession(data.token, data.expiresAt);
      if (data.user) {
        persistUser(data.user);
      }
    }

    return data;
  } catch (error) {
    return {
      success: false,
      message: 'Network error. Please check your connection and try again.',
    };
  }
}

export function getToken(): string | null {
  return accessToken;
}

export function getUser(): UserInfo | null {
  if (!safeSessionStorage) return null;
  const raw = safeSessionStorage.getItem(USER_KEY);
  if (!raw) return null;

  try {
    return JSON.parse(raw) as UserInfo;
  } catch (error) {
    return null;
  }
}

export function isAuthenticated(): boolean {
  return !!accessToken;
}

export async function logout(): Promise<void> {
  try {
    await authenticatedFetch(`${API_BASE_URL}/auth/logout`, {
      method: 'POST',
      body: JSON.stringify({}),
    });
  } catch (error) {
    // Silently fail - logout should always clear session
  } finally {
    clearSession();
  }
}

export async function authenticatedFetch(
  url: string,
  options: RequestInit = {},
): Promise<Response> {
  if (!accessToken || shouldRefreshToken()) {
    const refreshed = await refreshAccessToken();
    if (!refreshed) {
      throw new Error('Unable to refresh authentication token');
    }
  }

  const headers = new Headers(options.headers ?? {});
  headers.set('Content-Type', 'application/json');
  headers.set('Authorization', `Bearer ${accessToken}`);

  return fetch(url, {
    ...options,
    credentials: 'include',
    headers,
  });
}

export async function getCurrentUser(): Promise<UserInfo | null> {
  try {
    const response = await authenticatedFetch(`${API_BASE_URL}/auth/me`);

    if (!response.ok) {
      await logout();
      return null;
    }

    const data = await response.json();
    if (data.success && data.data) {
      persistUser(data.data as UserInfo);
      return data.data as UserInfo;
    }

    return null;
  } catch (error) {
    await logout();
    return null;
  }
}
