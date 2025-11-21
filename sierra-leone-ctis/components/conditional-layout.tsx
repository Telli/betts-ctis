"use client"

import React from 'react';
import { usePathname } from 'next/navigation';
import { useAuth } from '@/context/auth-context';
import { Sidebar } from '@/components/sidebar';
import { ErrorBoundary } from '@/components/ui/error-boundary';

interface ConditionalLayoutProps {
  children: React.ReactNode;
}

export function ConditionalLayout({ children }: ConditionalLayoutProps) {
  const { isLoggedIn, user } = useAuth();
  const pathname = usePathname();

  // Define routes that should never show the sidebar, even when authenticated
  const publicRoutes = ['/login', '/register', '/forgot-password'];
  const isPublicRoute = publicRoutes.includes(pathname);

  // Client portal routes have their own layout (ClientPortalLayout) with ClientSidebar
  // Don't show admin sidebar for client portal routes
  const isClientPortalRoute = pathname.startsWith('/client-portal');
  
  // Associate routes have their own layout
  const isAssociateRoute = pathname.startsWith('/associate');

  // Only show admin sidebar for admin/staff routes (not client portal or associate routes)
  const shouldShowAdminSidebar = isLoggedIn && !isPublicRoute && !isClientPortalRoute && !isAssociateRoute;
  
  // Prevent clients from seeing admin sidebar even if they somehow access admin routes
  const isClientUser = user?.role === 'Client';
  const shouldShowSidebar = shouldShowAdminSidebar && !isClientUser;

  if (shouldShowSidebar) {
    // Authenticated admin/staff layout with sidebar
    return (
      <div className="flex h-screen bg-gradient-to-br from-sierra-blue-25 to-white">
        <Sidebar />
        <main 
          className="flex-1 overflow-auto"
          id="main-content"
          role="main"
          aria-label="Main content"
        >
          <ErrorBoundary>
            {children}
          </ErrorBoundary>
        </main>
      </div>
    );
  }
  
  // Client portal and associate routes have their own layouts, so just render children
  // The nested layout will handle the appropriate sidebar
  if (isClientPortalRoute || isAssociateRoute) {
    return (
      <ErrorBoundary>
        {children}
      </ErrorBoundary>
    );
  }

  // Public layout without sidebar (login, register, home page for unauthenticated users)
  return (
    <div className="min-h-screen">
      <main 
        id="main-content"
        role="main"
        aria-label="Main content"
      >
        <ErrorBoundary>
          {children}
        </ErrorBoundary>
      </main>
    </div>
  );
}