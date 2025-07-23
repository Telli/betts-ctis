"use client"

import React from 'react';
import { useAuth } from '@/context/auth-context';
import { useAuthGuard } from '@/hooks/use-auth-guard';
import { ClientSidebar } from '@/components/client-portal/client-sidebar';
import { ErrorBoundary } from '@/components/ui/error-boundary';

interface ClientPortalLayoutProps {
  children: React.ReactNode;
}

export function ClientPortalLayout({ children }: ClientPortalLayoutProps) {
  const { isLoggedIn } = useAuth();
  const { isLoading: authLoading, isAuthorized } = useAuthGuard({ 
    requireAuth: true,
    requiredRole: 'Client'
  });

  if (authLoading) {
    return (
      <div className="min-h-screen flex items-center justify-center bg-gradient-to-br from-sierra-blue-25 to-white">
        <div className="text-center">
          <div className="animate-spin rounded-full h-8 w-8 border-b-2 border-sierra-blue-600 mx-auto mb-4"></div>
          <p className="text-sierra-blue-600">Loading...</p>
        </div>
      </div>
    );
  }

  if (!isAuthorized) {
    return (
      <div className="min-h-screen flex items-center justify-center bg-gradient-to-br from-sierra-blue-25 to-white">
        <div className="text-center">
          <h1 className="text-2xl font-bold text-sierra-blue-800 mb-4">Access Denied</h1>
          <p className="text-sierra-blue-600">You need client access to view this page.</p>
        </div>
      </div>
    );
  }

  return (
    <div className="flex h-screen bg-gradient-to-br from-sierra-blue-25 to-white">
      <ClientSidebar />
      <main className="flex-1 overflow-auto">
        <ErrorBoundary>
          {children}
        </ErrorBoundary>
      </main>
    </div>
  );
}