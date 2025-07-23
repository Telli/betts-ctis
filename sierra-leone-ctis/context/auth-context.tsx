"use client";

import { createContext, useContext, useState, useEffect, ReactNode, useCallback } from 'react';
import { getToken, isAuthenticated, removeToken } from '@/lib/api-client';
import { useRouter } from 'next/navigation';

interface User {
  id: string;
  email: string;
  name: string;
  role: string;
}

interface AuthContextType {
  isLoggedIn: boolean;
  user: User | null;
  logout: () => void;
  checkAuthStatus: () => boolean;
}

const AuthContext = createContext<AuthContextType | undefined>(undefined);

export function AuthProvider({ children }: { children: ReactNode }) {
  const [isLoggedIn, setIsLoggedIn] = useState<boolean>(false);
  const [user, setUser] = useState<User | null>(null);
  const router = useRouter();

  // Decode JWT token to get user information
  const decodeToken = useCallback((token: string): User | null => {
    try {
      const payload = JSON.parse(atob(token.split('.')[1]));

      // Handle role claim - it might be under different keys or as an array
      let role = payload.role || payload['http://schemas.microsoft.com/ws/2008/06/identity/claims/role'];
      if (Array.isArray(role)) {
        role = role[0]; // Take the first role if multiple
      }

      return {
        id: payload.nameid || payload.sub,
        email: payload.email,
        name: `${payload.given_name || ''} ${payload.family_name || ''}`.trim() || payload.email,
        role: role || 'User'
      };
    } catch (error) {
      console.error('Error decoding token:', error);
      return null;
    }
  }, []);

  // Check authentication status on initial load
  useEffect(() => {
    const checkAuth = () => {
      const authStatus = isAuthenticated();
      setIsLoggedIn(authStatus);

      if (authStatus) {
        const token = getToken();
        if (token) {
          const userData = decodeToken(token);
          setUser(userData);
        }
      } else {
        setUser(null);
      }

      return authStatus;
    };

    checkAuth();

    // Also check periodically in case token expires
    const interval = setInterval(checkAuth, 60000); // Check every minute

    return () => clearInterval(interval);
  }, [decodeToken]);

  const logout = useCallback(() => {
    removeToken();
    setIsLoggedIn(false);
    setUser(null);
    router.push('/login');
  }, [router]);

  const checkAuthStatus = useCallback((): boolean => {
    const status = isAuthenticated();
    setIsLoggedIn(status);

    if (status) {
      const token = getToken();
      if (token) {
        const userData = decodeToken(token);
        setUser(userData);
      }
    } else {
      setUser(null);
    }

    return status;
  }, [decodeToken]);

  return (
    <AuthContext.Provider value={{ isLoggedIn, user, logout, checkAuthStatus }}>
      {children}
    </AuthContext.Provider>
  );
}

export const useAuth = (): AuthContextType => {
  const context = useContext(AuthContext);
  if (context === undefined) {
    throw new Error('useAuth must be used within an AuthProvider');
  }
  return context;
};
