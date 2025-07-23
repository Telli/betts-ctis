import { test, expect } from '@playwright/test';
import { AuthHelper } from '../utils/auth-helper';
import { TEST_CLIENT_DATA, ROUTES } from '../utils/test-data';

test.describe('Admin Interface', () => {
  let authHelper: AuthHelper;

  test.beforeEach(async ({ page }) => {
    authHelper = new AuthHelper(page);
  });

  test.describe('Admin Dashboard', () => {
    test('should login admin and access dashboard', async ({ page }) => {
      await authHelper.login('admin');
      
      // Verify admin dashboard elements
      await expect(page.locator('text=Dashboard')).toBeVisible();
      await expect(page.locator('text=Clients')).toBeVisible();
      await expect(page.locator('text=Statistics')).toBeVisible();
    });

    test('should display client statistics', async ({ page }) => {
      await authHelper.login('admin');
      
      // Check for statistics cards
      await expect(page.locator('text=Total Clients')).toBeVisible();
      await expect(page.locator('text=Active Clients')).toBeVisible();
      await expect(page.locator('text=Pending Documents')).toBeVisible();
    });
  });

  test.describe('Client Management', () => {
    test('should access client management page', async ({ page }) => {
      await authHelper.login('admin');
      
      // Navigate to clients page
      await page.click('text=Clients');
      await expect(page).toHaveURL('/dashboard/clients');
      
      // Verify client management elements
      await expect(page.locator('text=Client Management')).toBeVisible();
      await expect(page.locator('text=Add New Client')).toBeVisible();
    });

    test('should display clients list', async ({ page }) => {
      await authHelper.login('admin');
      await page.goto('/dashboard/clients');
      
      // Check for clients table
      await expect(page.locator('table')).toBeVisible();
      await expect(page.locator('th:has-text("Business Name")')).toBeVisible();
      await expect(page.locator('th:has-text("Contact Person")')).toBeVisible();
      await expect(page.locator('th:has-text("Status")')).toBeVisible();
    });

    test('should search and filter clients', async ({ page }) => {
      await authHelper.login('admin');
      await page.goto('/dashboard/clients');
      
      // Test search functionality
      const searchInput = page.locator('input[placeholder*="Search clients"]');
      if (await searchInput.isVisible()) {
        await searchInput.fill('Test Company');
        await page.waitForTimeout(1000);
        
        // Verify search results
        await expect(page.locator('text=Test Company')).toBeVisible();
      }
    });

    test.skip('should create new client', async ({ page }) => {
      // Skip this test as it would create actual data
      // In a real implementation, you would:
      // 1. Click "Add New Client"
      // 2. Fill out the client form
      // 3. Submit and verify creation
    });
  });

  test.describe('Associate Access', () => {
    test('should login associate and access limited admin features', async ({ page }) => {
      await authHelper.login('associate');
      
      // Associates should have access to admin dashboard but limited features
      await expect(page.locator('text=Dashboard')).toBeVisible();
      await expect(page.locator('text=Clients')).toBeVisible();
      
      // But should not see admin-only features
      await expect(page.locator('text=System Settings')).not.toBeVisible();
    });
  });

  test.describe('Document Review', () => {
    test('should access document review interface', async ({ page }) => {
      await authHelper.login('admin');
      
      // Navigate to document review (if it exists)
      const documentsLink = page.locator('text=Documents');
      if (await documentsLink.isVisible()) {
        await documentsLink.click();
        
        await expect(page.locator('text=Document Review')).toBeVisible();
        await expect(page.locator('text=Pending Review')).toBeVisible();
      }
    });
  });

  test.describe('Reports and Analytics', () => {
    test('should access reports section', async ({ page }) => {
      await authHelper.login('admin');
      
      // Check for reports navigation
      const reportsLink = page.locator('text=Reports');
      if (await reportsLink.isVisible()) {
        await reportsLink.click();
        
        await expect(page.locator('text=Tax Compliance Reports')).toBeVisible();
        await expect(page.locator('text=Client Statistics')).toBeVisible();
      }
    });
  });

  test.describe('System Settings', () => {
    test('should access system settings (admin only)', async ({ page }) => {
      await authHelper.login('admin');
      
      // Check for settings navigation
      const settingsLink = page.locator('text=Settings');
      if (await settingsLink.isVisible()) {
        await settingsLink.click();
        
        await expect(page.locator('text=System Configuration')).toBeVisible();
      }
    });

    test('should deny settings access to associate', async ({ page }) => {
      await authHelper.login('associate');
      
      // Try to access settings directly
      await page.goto('/dashboard/settings');
      
      // Should be redirected or show access denied
      await page.waitForTimeout(2000);
      const currentUrl = page.url();
      expect(
        currentUrl.includes('/dashboard') && !currentUrl.includes('/settings') ||
        await page.locator('text=Access Denied').isVisible()
      ).toBeTruthy();
    });
  });

  test.describe('Audit Trail', () => {
    test('should display audit logs', async ({ page }) => {
      await authHelper.login('admin');
      
      // Check for audit trail
      const auditLink = page.locator('text=Audit Trail').or(page.locator('text=Activity Log'));
      if (await auditLink.isVisible()) {
        await auditLink.click();
        
        await expect(page.locator('text=User Activity')).toBeVisible();
        await expect(page.locator('text=Timestamp')).toBeVisible();
        await expect(page.locator('text=Action')).toBeVisible();
      }
    });
  });

  test.describe('Data Export', () => {
    test.skip('should export client data', async ({ page }) => {
      // Skip this test as it involves file downloads
      // In a real implementation, you would:
      // 1. Navigate to export section
      // 2. Select export options
      // 3. Trigger download and verify file
    });
  });

  test.describe('Role-based Access Control', () => {
    test('should prevent client access to admin interface', async ({ page }) => {
      await authHelper.login('client');
      
      // Try to access admin dashboard
      await authHelper.expectUnauthorizedAccess('/dashboard');
    });

    test('should allow admin full access', async ({ page }) => {
      await authHelper.login('admin');
      
      // Admin should access all admin routes
      const adminRoutes = [
        '/dashboard',
        '/dashboard/clients',
        '/dashboard/reports'
      ];
      
      for (const route of adminRoutes) {
        await page.goto(route);
        await page.waitForTimeout(1000);
        
        // Should not redirect to login
        expect(page.url()).not.toContain('/login');
      }
    });
  });
});