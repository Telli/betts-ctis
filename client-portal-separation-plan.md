# Client Portal Separation and RBAC Implementation Plan

## Overview

This plan ensures complete separation between the client portal and admin/staff portal, with proper role-based access control (RBAC) to prevent clients from accessing admin features.

## Current State Analysis

### What's Already Implemented:
1. ✅ **Middleware** (`middleware.ts`): Redirects clients away from admin routes and admins away from client portal routes
2. ✅ **Client Portal Layout** (`app/client-portal/layout.tsx`): Uses `ClientPortalLayout` with role checking
3. ✅ **Client Sidebar** (`components/client-portal/client-sidebar.tsx`): Client-specific navigation
4. ✅ **Admin Sidebar** (`components/sidebar.tsx`): Admin navigation with role-based sections
5. ✅ **Backend API Endpoints**: Client portal endpoints use `ClientPortal` policy
6. ✅ **Client Portal Pages**: All pages exist under `/client-portal/*`

### Issues Identified:
1. ❌ **ConditionalLayout**: Shows admin `Sidebar` for all authenticated routes, including client portal routes
2. ❌ **Client Portal Pages**: Don't have explicit role guards (rely on layout and middleware)
3. ❌ **Sidebar Navigation**: Admin sidebar doesn't exclude client users explicitly
4. ❌ **Data Access**: Need to verify all client portal API calls only return client's own data

## Phase 1: Fix Layout Separation

### 1.1 Update ConditionalLayout
**File:** `components/conditional-layout.tsx`

**Issue:** Currently shows admin `Sidebar` for all authenticated routes, including client portal routes.

**Fix:**
- Check if current route is a client portal route (`/client-portal/*`)
- If client portal route, don't show admin sidebar (let ClientPortalLayout handle it)
- Only show admin sidebar for admin/staff routes

### 1.2 Verify Client Portal Layout Isolation
**File:** `app/client-portal/layout.tsx`

**Verify:**
- ClientPortalLayout properly checks for Client role
- Redirects non-clients appropriately
- Uses ClientSidebar (not admin Sidebar)

## Phase 2: Add Role Guards to Client Portal Pages

### 2.1 Add useAuthGuard to Client Portal Pages
**Files:**
- `app/client-portal/dashboard/page.tsx`
- `app/client-portal/documents/page.tsx`
- `app/client-portal/payments/page.tsx`
- `app/client-portal/tax-filings/page.tsx`
- `app/client-portal/compliance/page.tsx`
- `app/client-portal/reports/page.tsx`
- `app/client-portal/messages/page.tsx`
- `app/client-portal/profile/page.tsx`
- `app/client-portal/settings/page.tsx`
- `app/client-portal/help/page.tsx`
- `app/client-portal/deadlines/page.tsx`

**Action:**
- Add `useAuthGuard({ requireAuth: true, requiredRole: 'Client' })` to each page
- Show loading state during auth check
- Show access denied if not authorized

## Phase 3: Enhance Sidebar Security

### 3.1 Update Admin Sidebar
**File:** `components/sidebar.tsx`

**Enhancement:**
- Add explicit check to prevent rendering for Client users
- Redirect Client users if they somehow access admin sidebar
- Add role check before rendering admin navigation sections

### 3.2 Verify Client Sidebar
**File:** `components/client-portal/client-sidebar.tsx`

**Verify:**
- Only shows client-specific navigation items
- No admin features visible
- Proper logout functionality

## Phase 4: Verify Data Access Control

### 4.1 Verify API Service Calls
**Files:**
- `lib/services/client-portal-service.ts`

**Verify:**
- All endpoints use `/api/client-portal/*` routes
- No direct access to admin endpoints
- Backend enforces client-only data access

### 4.2 Test Data Filtering
**Action:**
- Verify client portal pages only show current user's data
- Test that clients cannot access other clients' data
- Verify backend returns 403/Forbidden for unauthorized access

## Phase 5: Add Additional Security Measures

### 5.1 Route Protection
**Enhancement:**
- Add route-level guards using Next.js route handlers
- Ensure direct URL access is blocked for unauthorized users
- Add client-side route protection hooks

### 5.2 Error Handling
**Enhancement:**
- Show appropriate error messages for unauthorized access attempts
- Log security violations for monitoring
- Provide clear feedback to users

## Phase 6: Testing and Verification

### 6.1 Test Client Access
- Login as client user
- Verify redirect to `/client-portal/dashboard`
- Verify only client portal navigation visible
- Verify cannot access admin routes (should redirect)
- Verify only own data displayed

### 6.2 Test Admin Access
- Login as admin/staff user
- Verify redirect to `/dashboard`
- Verify admin navigation visible
- Verify cannot access client portal routes (should redirect)
- Verify full data access

### 6.3 Test Unauthorized Access
- Try accessing client portal routes as admin (should redirect)
- Try accessing admin routes as client (should redirect)
- Verify proper error messages shown

## Implementation Order

1. **Phase 1**: Fix layout separation (highest priority - prevents UI confusion)
2. **Phase 2**: Add role guards to pages (security layer)
3. **Phase 3**: Enhance sidebar security (prevent navigation confusion)
4. **Phase 4**: Verify data access (ensure backend security)
5. **Phase 5**: Additional security measures (defense in depth)
6. **Phase 6**: Testing and verification (ensure everything works)

## Success Criteria

- ✅ Clients can only see client portal interface
- ✅ Clients cannot access admin routes (redirected)
- ✅ Clients only see their own data
- ✅ Admin/staff cannot access client portal routes (redirected)
- ✅ Proper error messages for unauthorized access
- ✅ No UI elements from wrong portal visible
- ✅ All navigation items role-appropriate
- ✅ Backend enforces data access restrictions

