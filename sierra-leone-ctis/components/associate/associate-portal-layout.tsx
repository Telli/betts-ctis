"use client"

import React from 'react';
import { useAuth } from '@/context/auth-context';
import { useAuthGuard } from '@/hooks/use-auth-guard';
import { AssociateSidebar } from '@/components/associate/associate-sidebar';
import { ErrorBoundary } from '@/components/ui/error-boundary';

interface AssociatePortalLayoutProps {
  children: React.ReactNode;
}

export function AssociatePortalLayout({ children }: AssociatePortalLayoutProps) {
  const { isLoggedIn } = useAuth();
  const { isLoading: authLoading, isAuthorized } = useAuthGuard({
    requireAuth: true,
    requiredRole: 'Associate'
  });

  if (authLoading) {
    return (
      <div className="min-h-screen flex items-center justify-center bg-gradient-to-br from-sierra-orange-25 to-white">
        <div className="text-center">
          <div className="animate-spin rounded-full h-8 w-8 border-b-2 border-sierra-orange-600 mx-auto mb-4"></div>
          <p className="text-sierra-orange-600">Loading...</p>
        </div>
      </div>
    );
  }

  if (!isAuthorized) {
    return (
      <div className="min-h-screen flex items-center justify-center bg-gradient-to-br from-sierra-orange-25 to-white">
        <div className="text-center">
          <h1 className="text-2xl font-bold text-sierra-orange-800 mb-4">Access Denied</h1>
          <p className="text-sierra-orange-600">You need associate access to view this page.</p>
        </div>
      </div>
    );
  }

  return (
    <div className="flex h-screen bg-gradient-to-br from-sierra-orange-25 to-white">
      <AssociateSidebar />
      <main className="flex-1 overflow-auto">
        <ErrorBoundary>
          {children}
        </ErrorBoundary>
      </main>
    </div>
  );
}