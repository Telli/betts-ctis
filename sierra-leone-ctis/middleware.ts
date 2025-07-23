import { NextResponse } from 'next/server';
import type { NextRequest } from 'next/server';

export function middleware(request: NextRequest) {
  // Get the pathname
  const path = request.nextUrl.pathname;

  // Define admin/associate routes that require admin or associate role
  const adminRoutes = [
    '/dashboard',
    '/clients', 
    '/tax-filings',
    '/payments',
    '/compliance',
    '/calculator',
    '/documents', 
    '/deadlines',
    '/analytics',
    '/notifications',
    '/profile', 
    '/settings'
  ];

  // Define client portal routes that require client role
  const clientPortalRoutes = [
    '/client-portal'
  ];

  // All protected routes (admin + client portal)
  const protectedRoutes = [...adminRoutes, ...clientPortalRoutes];
  
  // Define public routes that don't require redirection even if not authenticated
  const publicRoutes = ['/login', '/register', '/forgot-password'];
  
  // Check which type of protected route this is
  const isAdminRoute = adminRoutes.some((route) => 
    path === route || path.startsWith(`${route}/`)
  );
  
  const isClientPortalRoute = clientPortalRoutes.some((route) => 
    path === route || path.startsWith(`${route}/`)
  );
  
  const isProtectedRoute = isAdminRoute || isClientPortalRoute;
  
  // Get the token from Authorization header or cookies (prefer header for localStorage compatibility)
  const authHeader = request.headers.get('authorization');
  const cookieToken = request.cookies.get('auth_token')?.value;
  
  // Extract token from Authorization header (Bearer token)
  const headerToken = authHeader?.startsWith('Bearer ') ? authHeader.slice(7) : null;
  
  // Use header token first, fallback to cookie
  const token = headerToken || cookieToken;

  // Helper function to decode JWT token
  const decodeToken = (token: string) => {
    try {
      const payload = JSON.parse(atob(token.split('.')[1]));
      return payload;
    } catch {
      return null;
    }
  };

  // If trying to access a protected route without a token, redirect to login
  if (isProtectedRoute && !token) {
    const url = new URL('/login', request.url);
    url.searchParams.set('callbackUrl', encodeURI(request.url));
    return NextResponse.redirect(url);
  }

  // Role-based access control for authenticated users
  if (token && isProtectedRoute) {
    const payload = decodeToken(token);
    if (payload && payload.role) {
      const userRole = payload.role;

      // If client trying to access admin routes, redirect to client portal
      if (userRole === 'Client' && isAdminRoute) {
        const url = new URL('/client-portal/dashboard', request.url);
        return NextResponse.redirect(url);
      }

      // If admin/associate trying to access client portal, redirect to admin dashboard
      if ((userRole === 'Admin' || userRole === 'Associate' || userRole === 'SystemAdmin') && isClientPortalRoute) {
        const url = new URL('/dashboard', request.url);
        return NextResponse.redirect(url);
      }
    }
  }
  
  // Role-based redirection for public routes and homepage
  if (token && (publicRoutes.includes(path) || path === '/')) {
    const payload = decodeToken(token);
    if (payload && payload.role) {
      const userRole = payload.role;
      
      // Redirect based on user role
      if (userRole === 'Client') {
        const url = new URL('/client-portal/dashboard', request.url);
        return NextResponse.redirect(url);
      } else {
        const url = new URL('/dashboard', request.url);
        return NextResponse.redirect(url);
      }
    } else {
      // Default to admin dashboard if role cannot be determined
      const url = new URL('/dashboard', request.url);
      return NextResponse.redirect(url);
    }
  }

  return NextResponse.next();
}

export const config = {
  matcher: ['/((?!api|_next/static|_next/image|favicon.ico|.*\\.svg).*)']
};
