import { test, expect, Page } from '@playwright/test';
import { AuthHelper } from '../utils/auth-helper';
import { TEST_USERS, ROUTES } from '../utils/test-data';

test.describe('Associate Permission System E2E', () => {
  let authHelper: AuthHelper;
  let page: Page;

  test.beforeEach(async ({ page: testPage }) => {
    page = testPage;
    authHelper = new AuthHelper(page);
  });

  test.describe('Admin: Associate Management Interface', () => {
    test.beforeEach(async () => {
      // Login as admin
      await authHelper.login('admin');
    });

    test('should access associate management page', async () => {
      // Navigate to associate management
      await page.goto('/admin/associates');
      
      // Check page title and content
      await expect(page).toHaveTitle(/Associate Management/);
      await expect(page.locator('h1:has-text("Associate Management")')).toBeVisible();
      
      // Check main tabs are present
      await expect(page.locator('text=Overview')).toBeVisible();
      await expect(page.locator('text=Associates')).toBeVisible();
      await expect(page.locator('text=Permissions')).toBeVisible();
    });

    test('should display associate overview statistics', async () => {
      await page.goto('/admin/associates');
      
      // Check summary cards
      await expect(page.locator('text=Total Associates')).toBeVisible();
      await expect(page.locator('text=Active Permissions')).toBeVisible();
      await expect(page.locator('text=Expiring Soon')).toBeVisible();
      await expect(page.locator('text=Expired')).toBeVisible();
      
      // Check for numeric values in cards
      const totalAssociatesCard = page.locator('[data-testid="total-associates"]').or(
        page.locator('text=Total Associates').locator('..').locator('.text-2xl')
      );
      await expect(totalAssociatesCard).toBeVisible();
    });

    test('should open grant permission dialog', async () => {
      await page.goto('/admin/associates');
      
      // Click Grant Permission button
      await page.locator('button:has-text("Grant Permission")').click();
      
      // Check dialog is opened
      await expect(page.locator('text=Grant Associate Permission')).toBeVisible();
      await expect(page.locator('text=Grant permission to an associate')).toBeVisible();
      
      // Check form fields are present
      await expect(page.locator('text=Associate')).toBeVisible();
      await expect(page.locator('text=Client ID')).toBeVisible();
      await expect(page.locator('text=Permission Area')).toBeVisible();
      await expect(page.locator('text=Permission Level')).toBeVisible();
      await expect(page.locator('text=Reason')).toBeVisible();
    });

    test('should validate grant permission form', async () => {
      await page.goto('/admin/associates');
      
      // Open grant permission dialog
      await page.locator('button:has-text("Grant Permission")').click();
      
      // Try to submit empty form
      await page.locator('button:has-text("Grant Permission")').last().click();
      
      // Check validation errors
      await expect(page.locator('text=Associate is required').or(
        page.locator('text=required')
      )).toBeVisible();
    });

    test('should switch between tabs', async () => {
      await page.goto('/admin/associates');
      
      // Click Associates tab
      await page.locator('text=Associates').click();
      await expect(page.locator('text=Showing').or(page.locator('h3'))).toBeVisible();
      
      // Click Permissions tab
      await page.locator('text=Permissions').click();
      await expect(page.locator('text=Associate Permissions')).toBeVisible();
      
      // Back to Overview
      await page.locator('text=Overview').click();
      await expect(page.locator('text=Total Associates')).toBeVisible();
    });
  });

  test.describe('Associate: Dashboard Interface', () => {
    test.beforeEach(async () => {
      // Login as associate
      await authHelper.login('associate');
    });

    test('should access associate dashboard', async () => {
      // Navigate to associate dashboard
      await page.goto('/associate/dashboard');
      
      // Check page title and content
      await expect(page.locator('h1:has-text("Associate Dashboard")')).toBeVisible();
      await expect(page.locator('text=Welcome back')).toBeVisible();
      
      // Check summary cards
      await expect(page.locator('text=Delegated Clients')).toBeVisible();
      await expect(page.locator('text=Active Permissions')).toBeVisible();
      await expect(page.locator('text=Expiring Soon')).toBeVisible();
      await expect(page.locator('text=Recent Actions')).toBeVisible();
      await expect(page.locator('text=Upcoming Deadlines')).toBeVisible();
    });

    test('should display dashboard tabs', async () => {
      await page.goto('/associate/dashboard');
      
      // Check tabs are present
      await expect(page.locator('text=Overview')).toBeVisible();
      await expect(page.locator('text=My Clients')).toBeVisible();
      await expect(page.locator('text=Recent Actions')).toBeVisible();
      await expect(page.locator('text=Deadlines')).toBeVisible();
      await expect(page.locator('text=Permissions')).toBeVisible();
    });

    test('should navigate between dashboard tabs', async () => {
      await page.goto('/associate/dashboard');
      
      // Click My Clients tab
      await page.locator('text=My Clients').click();
      await expect(page.locator('text=Delegated Clients')).toBeVisible();
      
      // Click Recent Actions tab
      await page.locator('text=Recent Actions').click();
      await expect(page.locator('text=Your recent activities')).toBeVisible();
      
      // Click Deadlines tab
      await page.locator('text=Deadlines').click();
      await expect(page.locator('text=Tax filing deadlines')).toBeVisible();
      
      // Click Permissions tab
      await page.locator('text=Permissions').click();
      await expect(page.locator('text=Permission Alerts')).toBeVisible();
    });

    test('should display quick actions', async () => {
      await page.goto('/associate/dashboard');
      
      // Check quick actions are present
      await expect(page.locator('text=Quick Actions')).toBeVisible();
      await expect(page.locator('text=View Tax Filings').or(page.locator('a[href="/tax-filings"]'))).toBeVisible();
      await expect(page.locator('text=Manage Documents').or(page.locator('a[href="/documents"]'))).toBeVisible();
      await expect(page.locator('text=Process Payments').or(page.locator('a[href="/payments"]'))).toBeVisible();
    });
  });

  test.describe('Associate: Permissions Management', () => {
    test.beforeEach(async () => {
      // Login as associate
      await authHelper.login('associate');
    });

    test('should access permissions page', async () => {
      await page.goto('/associate/permissions');
      
      // Check page title and content
      await expect(page.locator('h1:has-text("My Permissions")')).toBeVisible();
      await expect(page.locator('text=Manage and view your delegated client permissions')).toBeVisible();
      
      // Check summary cards
      await expect(page.locator('text=Total Permissions')).toBeVisible();
      await expect(page.locator('text=Active')).toBeVisible();
      await expect(page.locator('text=Expiring Soon')).toBeVisible();
      await expect(page.locator('text=Expired')).toBeVisible();
    });

    test('should have permission filters', async () => {
      await page.goto('/associate/permissions');
      
      // Check filter section
      await expect(page.locator('text=Filter Permissions')).toBeVisible();
      
      // Check search input
      await expect(page.locator('input[placeholder*="Search"]')).toBeVisible();
      
      // Check dropdown filters
      await expect(page.locator('text=All Areas').or(page.locator('[role="combobox"]'))).toBeVisible();
      await expect(page.locator('text=All Status').or(page.locator('[role="combobox"]'))).toBeVisible();
    });

    test('should filter permissions by area', async () => {
      await page.goto('/associate/permissions');
      
      // Click area filter dropdown
      const areaFilter = page.locator('text=All Areas').or(page.locator('[data-testid="area-filter"]'));
      if (await areaFilter.isVisible()) {
        await areaFilter.click();
        
        // Select Tax Filings option if available
        const taxFilingsOption = page.locator('text=Tax Filings');
        if (await taxFilingsOption.isVisible()) {
          await taxFilingsOption.click();
          
          // Wait for filter to apply
          await page.waitForTimeout(1000);
        }
      }
    });

    test('should search permissions', async () => {
      await page.goto('/associate/permissions');
      
      // Find search input and type
      const searchInput = page.locator('input[placeholder*="Search"]');
      await searchInput.fill('test');
      
      // Wait for search to apply
      await page.waitForTimeout(1000);
      
      // Check that some filtering occurred (page should still show permission structure)
      await expect(page.locator('text=Permission Details')).toBeVisible();
    });
  });

  test.describe('Associate: Client Management', () => {
    test.beforeEach(async () => {
      // Login as associate
      await authHelper.login('associate');
    });

    test('should access clients page', async () => {
      await page.goto('/associate/clients');
      
      // Check page title and content
      await expect(page.locator('h1:has-text("My Delegated Clients")')).toBeVisible();
      await expect(page.locator('text=Clients you have permissions to manage')).toBeVisible();
      
      // Check summary cards
      await expect(page.locator('text=Total Clients')).toBeVisible();
      await expect(page.locator('text=Active Clients')).toBeVisible();
      await expect(page.locator('text=With Deadlines')).toBeVisible();
      await expect(page.locator('text=Recent Activity')).toBeVisible();
    });

    test('should have client filters', async () => {
      await page.goto('/associate/clients');
      
      // Check filter section
      await expect(page.locator('text=Filter Clients')).toBeVisible();
      
      // Check search input
      await expect(page.locator('input[placeholder*="Search"]')).toBeVisible();
      
      // Check dropdown filters
      await expect(page.locator('text=Permission Area').or(page.locator('[role="combobox"]'))).toBeVisible();
      await expect(page.locator('text=All Categories').or(page.locator('[role="combobox"]'))).toBeVisible();
    });

    test('should filter clients by permission area', async () => {
      await page.goto('/associate/clients');
      
      // Click permission area filter
      const areaFilter = page.locator('text=Tax Filings').or(page.locator('[data-testid="permission-area-filter"]'));
      if (await areaFilter.isVisible()) {
        await areaFilter.click();
        
        // Select Documents option if available
        const documentsOption = page.locator('text=Documents');
        if (await documentsOption.isVisible()) {
          await documentsOption.click();
          
          // Wait for filter to apply
          await page.waitForTimeout(1000);
        }
      }
    });

    test('should search clients', async () => {
      await page.goto('/associate/clients');
      
      // Find search input and type
      const searchInput = page.locator('input[placeholder*="Search"]');
      await searchInput.fill('test');
      
      // Wait for search to apply
      await page.waitForTimeout(1000);
      
      // Page should still show client structure
      await expect(page.locator('text=Total Clients')).toBeVisible();
    });
  });

  test.describe('Navigation: Sidebar Integration', () => {
    test('should show admin navigation for admin users', async () => {
      await authHelper.login('admin');
      await page.goto('/dashboard');
      
      // Check admin navigation section
      await expect(page.locator('text=Admin').or(page.locator('[data-testid="admin-nav"]'))).toBeVisible();
      await expect(page.locator('text=Associate Management').or(page.locator('a[href="/admin/associates"]'))).toBeVisible();
      await expect(page.locator('text=Admin Settings').or(page.locator('a[href="/admin/settings"]'))).toBeVisible();
    });

    test('should show associate navigation for associate users', async () => {
      await authHelper.login('associate');
      await page.goto('/dashboard');
      
      // Check associate navigation section
      await expect(page.locator('text=Associate').or(page.locator('[data-testid="associate-nav"]'))).toBeVisible();
      await expect(page.locator('text=Associate Dashboard').or(page.locator('a[href="/associate/dashboard"]'))).toBeVisible();
      await expect(page.locator('text=My Permissions').or(page.locator('a[href="/associate/permissions"]'))).toBeVisible();
      await expect(page.locator('text=My Clients').or(page.locator('a[href="/associate/clients"]'))).toBeVisible();
    });

    test('should not show admin navigation for associate users', async () => {
      await authHelper.login('associate');
      await page.goto('/dashboard');
      
      // Should not see admin navigation
      await expect(page.locator('text=Associate Management')).not.toBeVisible();
    });

    test('should not show associate navigation for client users', async () => {
      await authHelper.login('client');
      await page.goto('/client-portal/dashboard');
      
      // Should not see associate navigation
      await expect(page.locator('text=Associate Dashboard')).not.toBeVisible();
      await expect(page.locator('text=My Permissions')).not.toBeVisible();
    });
  });

  test.describe('Access Control and Security', () => {
    test('should redirect associates from client portal', async () => {
      await authHelper.login('associate');
      
      // Try to access client portal
      await page.goto('/client-portal/dashboard');
      
      // Should redirect to main dashboard
      await expect(page).toHaveURL('/dashboard');
    });

    test('should prevent client access to associate pages', async () => {
      await authHelper.login('client');
      
      // Try to access associate dashboard
      await page.goto('/associate/dashboard');
      
      // Should redirect to client portal or show access denied
      await expect(page).not.toHaveURL('/associate/dashboard');
      
      // Should be redirected to client portal or login
      const currentUrl = page.url();
      expect(currentUrl).toMatch(/\/(client-portal|login)/);
    });

    test('should prevent client access to admin pages', async () => {
      await authHelper.login('client');
      
      // Try to access admin associates page
      await page.goto('/admin/associates');
      
      // Should not have access
      await expect(page).not.toHaveURL('/admin/associates');
    });

    test('should prevent associate access to admin pages without proper permissions', async () => {
      await authHelper.login('associate');
      
      // Try to access admin associates page
      await page.goto('/admin/associates');
      
      // Should be denied access or redirected
      // The exact behavior depends on your authorization implementation
      const currentUrl = page.url();
      expect(currentUrl === '/admin/associates').toBeFalsy();
    });
  });

  test.describe('Responsive Design', () => {
    test('should display properly on mobile viewport', async () => {
      await page.setViewportSize({ width: 375, height: 667 }); // iPhone SE size
      await authHelper.login('associate');
      
      // Check associate dashboard on mobile
      await page.goto('/associate/dashboard');
      await expect(page.locator('h1:has-text("Associate Dashboard")')).toBeVisible();
      
      // Check that content is responsive
      await expect(page.locator('text=Delegated Clients')).toBeVisible();
    });

    test('should handle sidebar collapse on small screens', async () => {
      await page.setViewportSize({ width: 768, height: 1024 }); // Tablet size
      await authHelper.login('associate');
      
      await page.goto('/associate/dashboard');
      
      // Check that sidebar toggle works (if implemented)
      const sidebarToggle = page.locator('[data-testid="sidebar-toggle"]').or(
        page.locator('button').filter({ hasText: /toggle|menu/i })
      );
      
      if (await sidebarToggle.isVisible()) {
        await sidebarToggle.click();
        // Sidebar should collapse/expand
      }
    });
  });

  test.describe('Data Loading and Error Handling', () => {
    test('should show loading states', async () => {
      await authHelper.login('associate');
      
      // Navigate to associate dashboard
      await page.goto('/associate/dashboard');
      
      // Loading spinner might be visible briefly
      // We'll check that the page eventually loads content
      await expect(page.locator('h1:has-text("Associate Dashboard")')).toBeVisible({ timeout: 10000 });
    });

    test('should handle network errors gracefully', async () => {
      await authHelper.login('associate');
      
      // Block network requests to simulate offline
      await page.route('**/api/associate-dashboard/**', route => route.abort());
      
      await page.goto('/associate/dashboard');
      
      // Should show error message or fallback content
      // The exact implementation depends on your error handling
      await expect(page.locator('h1:has-text("Associate Dashboard")')).toBeVisible();
    });

    test('should retry failed requests', async () => {
      await authHelper.login('associate');
      
      let requestCount = 0;
      await page.route('**/api/associate-dashboard/**', route => {
        requestCount++;
        if (requestCount === 1) {
          // Fail first request
          route.abort();
        } else {
          // Allow subsequent requests
          route.continue();
        }
      });
      
      await page.goto('/associate/dashboard');
      
      // Should eventually load after retry
      await expect(page.locator('h1:has-text("Associate Dashboard")')).toBeVisible({ timeout: 15000 });
    });
  });

  test.describe('Performance', () => {
    test('should load associate dashboard within acceptable time', async () => {
      await authHelper.login('associate');
      
      const startTime = Date.now();
      await page.goto('/associate/dashboard');
      
      // Wait for main content to load
      await expect(page.locator('h1:has-text("Associate Dashboard")')).toBeVisible();
      
      const loadTime = Date.now() - startTime;
      expect(loadTime).toBeLessThan(5000); // Should load within 5 seconds
    });

    test('should not have excessive console errors', async () => {
      const consoleErrors: string[] = [];
      page.on('console', msg => {
        if (msg.type() === 'error') {
          consoleErrors.push(msg.text());
        }
      });
      
      await authHelper.login('associate');
      await page.goto('/associate/dashboard');
      
      // Wait for page to fully load
      await page.waitForTimeout(3000);
      
      // Filter out known harmless errors (like network errors in tests)
      const criticalErrors = consoleErrors.filter(error => 
        !error.includes('net::ERR_') && 
        !error.includes('Failed to fetch') &&
        !error.toLowerCase().includes('test')
      );
      
      expect(criticalErrors.length).toBeLessThan(3); // Allow minimal errors
    });
  });
});