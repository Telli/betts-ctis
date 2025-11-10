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
  const { isLoggedIn } = useAuth();
  const pathname = usePathname();

  // Define routes that should never show the sidebar, even when authenticated
  const publicRoutes = ['/login', '/register', '/forgot-password'];
  const isPublicRoute = publicRoutes.includes(pathname);

  // Define routes that should show sidebar when authenticated
  const shouldShowSidebar = isLoggedIn && !isPublicRoute;

  if (shouldShowSidebar) {
    // Authenticated layout with sidebar
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