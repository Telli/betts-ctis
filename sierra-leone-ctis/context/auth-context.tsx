"use client";

import { createContext, useContext, useState, useEffect, ReactNode, useCallback } from 'react';
import { AuthService, AuthSession } from '@/lib/services/auth-service';

interface User {
  id: string;
  email: string;
  name: string;
  role: string;
  roles: string[];
}

interface AuthContextType {
  isLoggedIn: boolean;
  user: User | null;
  logout: () => Promise<void>;
  checkAuthStatus: () => Promise<boolean>;
}

const AuthContext = createContext<AuthContextType | undefined>(undefined);

const mapSessionToUser = (session: AuthSession): User => {
  const findClaim = (keyword: string): string | undefined => {
    return session.allClaims?.find((claim) =>
      claim.type?.toLowerCase().includes(keyword)
    )?.value;
  };

  const givenName = findClaim('givenname') || findClaim('given_name') || '';
  const surname = findClaim('surname') || findClaim('familyname') || '';
  const displayName = `${givenName} ${surname}`.trim() || session.email;
  const roles = session.roles ?? [];

  return {
    id: session.userId,
    email: session.email,
    name: displayName,
    role: roles[0] ?? 'User',
    roles
  };
};

export function AuthProvider({ children }: { children: ReactNode }) {
  const [isLoggedIn, setIsLoggedIn] = useState<boolean>(false);
  const [user, setUser] = useState<User | null>(null);

  const syncSession = useCallback(async (): Promise<User | null> => {
    try {
      const session = await AuthService.getSession();
      if (!session || !session.userId) {
        setIsLoggedIn(false);
        setUser(null);
        return null;
      }

      const mappedUser = mapSessionToUser(session);
      setIsLoggedIn(true);
      setUser(mappedUser);
      return mappedUser;
    } catch (error: any) {
      if (error?.code !== 'UNAUTHORIZED' && error?.status !== 401) {
        console.warn('Failed to refresh auth session', error);
      }
      setIsLoggedIn(false);
      setUser(null);
      return null;
    }
  }, []);

  useEffect(() => {
    let isMounted = true;

    const initialize = async () => {
      if (!isMounted) return;
      await syncSession();
    };

    void initialize();

    const interval = setInterval(() => {
      void syncSession();
    }, 60000);

    return () => {
      isMounted = false;
      clearInterval(interval);
    };
  }, [syncSession]);

  const logout = useCallback(async () => {
    try {
      await AuthService.logout();
    } catch (error) {
      console.warn('Logout request failed', error);
    } finally {
      setIsLoggedIn(false);
      setUser(null);
    }
  }, []);

  const checkAuthStatus = useCallback(async (): Promise<boolean> => {
    const currentUser = await syncSession();
    return !!currentUser;
  }, [syncSession]);

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
