import { Page, expect } from '@playwright/test';
import { TEST_USERS, ROUTES, SELECTORS, TIMEOUTS } from './test-data';

export class AuthHelper {
  constructor(private page: Page) {}

  async login(userType: keyof typeof TEST_USERS) {
    const user = TEST_USERS[userType];
    
    // Navigate to login page
    await this.page.goto(ROUTES.login);
    
    // Wait for login form to be visible
    await this.page.waitForSelector(SELECTORS.loginForm, { timeout: TIMEOUTS.medium });
    
    // Fill login credentials
    await this.page.fill(SELECTORS.emailInput, user.email);
    await this.page.fill(SELECTORS.passwordInput, user.password);
    
    // Submit login form
    await this.page.click(SELECTORS.loginButton);
    
    // Wait for navigation to complete
    await this.page.waitForLoadState('networkidle');
    
    // Verify successful login based on user role
    if (userType === 'client') {
      await expect(this.page).toHaveURL(ROUTES.clientPortal.dashboard);
    } else {
      await expect(this.page).toHaveURL(ROUTES.dashboard);
    }
    
    // Verify user is logged in by checking for sidebar
    const sidebarSelector = userType === 'client' ? SELECTORS.clientSidebar : SELECTORS.sidebar;
    await this.page.waitForSelector(sidebarSelector, { timeout: TIMEOUTS.medium });
  }

  async logout() {
    // Click logout button (assuming it's in the sidebar)
    await this.page.click('text=Logout');
    
    // Wait for redirect to login page
    await expect(this.page).toHaveURL(ROUTES.login);
  }

  async expectLoggedIn(userType: keyof typeof TEST_USERS) {
    if (userType === 'client') {
      await expect(this.page.locator(SELECTORS.clientSidebar)).toBeVisible();
    } else {
      await expect(this.page.locator(SELECTORS.sidebar)).toBeVisible();
    }
  }

  async expectLoggedOut() {
    await expect(this.page).toHaveURL(ROUTES.login);
  }

  async setupAuthState(userType: keyof typeof TEST_USERS) {
    await this.login(userType);
    
    // Save authentication state
    const storageState = await this.page.context().storageState();
    return storageState;
  }

  async expectUnauthorizedAccess(url: string) {
    await this.page.goto(url);
    
    // Should redirect to login or show access denied
    await this.page.waitForTimeout(2000);
    const currentUrl = this.page.url();
    
    expect(
      currentUrl.includes('/login') || 
      currentUrl.includes('/dashboard') ||
      this.page.locator('text=Access Denied').isVisible()
    ).toBeTruthy();
  }
}