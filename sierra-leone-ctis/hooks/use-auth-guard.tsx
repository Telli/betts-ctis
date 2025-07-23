"use client"

import { useEffect, useState, useCallback } from 'react';
import { useRouter } from 'next/navigation';
import { useAuth } from '@/context/auth-context';

interface AuthGuardOptions {
  redirectTo?: string;
  requireAuth?: boolean;
  requiredRole?: string;
}

export function useAuthGuard(options: AuthGuardOptions = {}) {
  const { redirectTo = '/login', requireAuth = true, requiredRole } = options;
  const { isLoggedIn, user } = useAuth();
  const router = useRouter();
  const [isLoading, setIsLoading] = useState(true);
  const [isAuthorized, setIsAuthorized] = useState(false);

  useEffect(() => {
    const checkAuth = () => {
      try {
        setIsLoading(true);

        if (requireAuth && !isLoggedIn) {
          // User is not authenticated but page requires auth
          router.push(redirectTo);
          return;
        }

        if (!requireAuth && isLoggedIn) {
          // User is authenticated but trying to access public page (like login)
          router.push('/dashboard');
          return;
        }

        // Check role if required
        if (requireAuth && requiredRole && user) {
          const userRole = user.role;
          if (userRole !== requiredRole) {
            // User doesn't have required role
            router.push('/dashboard'); // Redirect to main dashboard
            return;
          }
        }

        setIsAuthorized(true);
      } catch (error) {
        console.error('Auth guard error:', error);
        if (requireAuth) {
          router.push(redirectTo);
        }
      } finally {
        setIsLoading(false);
      }
    };

    checkAuth();
  }, [isLoggedIn, requireAuth, redirectTo, router, requiredRole, user?.role]);

  return {
    isLoading,
    isAuthorized,
    isAuthenticated: isLoggedIn
  };
}

// HOC for protecting components
export function withAuthGuard<P extends object>(
  Component: React.ComponentType<P>,
  options: AuthGuardOptions = {}
) {
  return function AuthGuardedComponent(props: P) {
    const { isLoading, isAuthorized } = useAuthGuard(options);

    if (isLoading) {
      return (
        <div className="flex items-center justify-center min-h-screen">
          <div className="text-center">
            <div className="animate-spin rounded-full h-8 w-8 border-b-2 border-sierra-blue mx-auto mb-4"></div>
            <p className="text-sierra-blue">Loading...</p>
          </div>
        </div>
      );
    }

    if (!isAuthorized) {
      return null; // Will redirect via useAuthGuard
    }

    return <Component {...props} />;
  };
}