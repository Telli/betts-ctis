import { test, expect } from '@playwright/test';
import { LoginPage } from '../page-objects/LoginPage';
import { AuthHelper } from '../utils/auth-helper';
import { TEST_USERS, ROUTES } from '../utils/test-data';

test.describe('Authentication', () => {
  let loginPage: LoginPage;
  let authHelper: AuthHelper;

  test.beforeEach(async ({ page }) => {
    loginPage = new LoginPage(page);
    authHelper = new AuthHelper(page);
  });

  test.describe('Login Flow', () => {
    test('should display login form', async ({ page }) => {
      await loginPage.goto();
      await loginPage.expectLoginForm();
      
      // Check page title and branding
      await expect(page).toHaveTitle(/CTIS - Client Tax Information System/);
      await expect(page.locator('text=CTIS')).toBeVisible();
      await expect(page.locator('text=Betts')).toBeVisible();
    });

    test('should login admin user successfully', async ({ page }) => {
      await loginPage.goto();
      
      const user = TEST_USERS.admin;
      await loginPage.login(user.email, user.password);
      
      // Should redirect to admin dashboard
      await loginPage.expectRedirectToDashboard();
      await authHelper.expectLoggedIn('admin');
      
      // Check admin-specific elements
      await expect(page.locator('text=Dashboard')).toBeVisible();
      await expect(page.locator('text=Clients')).toBeVisible();
    });

    test('should login client user successfully', async ({ page }) => {
      await loginPage.goto();
      
      const user = TEST_USERS.client;
      await loginPage.login(user.email, user.password);
      
      // Should redirect to client portal
      await loginPage.expectRedirectToClientPortal();
      await authHelper.expectLoggedIn('client');
      
      // Check client-specific elements
      await expect(page.locator('text=Client Portal')).toBeVisible();
      await expect(page.locator('text=My Documents')).toBeVisible();
    });

    test('should login associate user successfully', async ({ page }) => {
      await loginPage.goto();
      
      const user = TEST_USERS.associate;
      await loginPage.login(user.email, user.password);
      
      // Should redirect to admin dashboard (associates use admin interface)
      await loginPage.expectRedirectToDashboard();
      await authHelper.expectLoggedIn('associate');
    });

    test('should reject invalid credentials', async ({ page }) => {
      await loginPage.goto();
      
      const invalidUser = TEST_USERS.invalidUser;
      await loginPage.login(invalidUser.email, invalidUser.password);
      
      // Should show error and remain on login page
      await loginPage.expectLoginError();
      await expect(page).toHaveURL('/login');
    });

    test('should validate required fields', async ({ page }) => {
      await loginPage.goto();
      
      // Try to submit empty form
      await loginPage.submit();
      
      // Should show validation errors
      await expect(page.locator('text=Email is required').or(page.locator('text=Please enter'))).toBeVisible();
    });

    test('should validate email format', async ({ page }) => {
      await loginPage.goto();
      
      await loginPage.fillEmail('invalid-email');
      await loginPage.fillPassword('password');
      await loginPage.submit();
      
      // Should show email format error
      await expect(page.locator('text=Please enter a valid email').or(page.locator('text=Invalid email'))).toBeVisible();
    });
  });

  test.describe('Role-based Redirects', () => {
    test('should redirect admin from client portal to admin dashboard', async ({ page }) => {
      // Login as admin
      await authHelper.login('admin');
      
      // Try to access client portal
      await page.goto(ROUTES.clientPortal.dashboard);
      
      // Should redirect to admin dashboard
      await expect(page).toHaveURL(ROUTES.dashboard);
    });

    test('should redirect client from admin dashboard to client portal', async ({ page }) => {
      // Login as client
      await authHelper.login('client');
      
      // Try to access admin dashboard
      await page.goto(ROUTES.dashboard);
      
      // Should redirect to client portal
      await expect(page).toHaveURL(ROUTES.clientPortal.dashboard);
    });

    test('should redirect unauthenticated user to login', async ({ page }) => {
      // Try to access protected route without authentication
      await page.goto(ROUTES.dashboard);
      
      // Should redirect to login
      await expect(page).toHaveURL(/\/login/);
    });

    test('should redirect authenticated user away from login page', async ({ page }) => {
      // Login as client
      await authHelper.login('client');
      
      // Try to access login page
      await page.goto(ROUTES.login);
      
      // Should redirect to appropriate dashboard
      await expect(page).toHaveURL(ROUTES.clientPortal.dashboard);
    });
  });

  test.describe('Session Management', () => {
    test('should maintain session across page refreshes', async ({ page }) => {
      await authHelper.login('client');
      
      // Refresh page
      await page.reload();
      
      // Should still be logged in
      await authHelper.expectLoggedIn('client');
      await expect(page).toHaveURL(ROUTES.clientPortal.dashboard);
    });

    test('should logout successfully', async ({ page }) => {
      await authHelper.login('client');
      
      // Logout
      await authHelper.logout();
      
      // Should redirect to login page
      await authHelper.expectLoggedOut();
    });

    test('should prevent access after logout', async ({ page }) => {
      await authHelper.login('client');
      await authHelper.logout();
      
      // Try to access protected route
      await page.goto(ROUTES.clientPortal.dashboard);
      
      // Should redirect to login
      await expect(page).toHaveURL(/\/login/);
    });
  });

  test.describe('Security', () => {
    test('should not store credentials in local storage', async ({ page }) => {
      await authHelper.login('client');
      
      // Check that password is not stored
      const localStorage = await page.evaluate(() => {
        const items: Record<string, string> = {};
        for (let i = 0; i < window.localStorage.length; i++) {
          const key = window.localStorage.key(i);
          if (key) {
            items[key] = window.localStorage.getItem(key) || '';
          }
        }
        return items;
      });
      
      // Ensure no sensitive data is stored
      const sensitiveData = Object.values(localStorage).join(' ').toLowerCase();
      expect(sensitiveData).not.toContain('password');
      expect(sensitiveData).not.toContain(TEST_USERS.client.password.toLowerCase());
    });

    test('should handle concurrent sessions', async ({ context }) => {
      // Create two pages (simulating two browser tabs)
      const page1 = await context.newPage();
      const page2 = await context.newPage();
      
      const authHelper1 = new AuthHelper(page1);
      const authHelper2 = new AuthHelper(page2);
      
      // Login on both pages
      await authHelper1.login('client');
      await authHelper2.login('client');
      
      // Both should work
      await authHelper1.expectLoggedIn('client');
      await authHelper2.expectLoggedIn('client');
      
      // Logout from one page
      await authHelper1.logout();
      
      // Both pages should be logged out (if implementing single session)
      // Or page2 should still work (if allowing multiple sessions)
      // This depends on your session management strategy
    });
  });
});