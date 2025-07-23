import { Page, expect } from '@playwright/test';
import { SELECTORS, TIMEOUTS } from '../utils/test-data';

export class LoginPage {
  constructor(private page: Page) {}

  async goto() {
    await this.page.goto('/login');
    await this.page.waitForSelector(SELECTORS.loginForm, { timeout: TIMEOUTS.medium });
  }

  async fillEmail(email: string) {
    await this.page.fill(SELECTORS.emailInput, email);
  }

  async fillPassword(password: string) {
    await this.page.fill(SELECTORS.passwordInput, password);
  }

  async submit() {
    await this.page.click(SELECTORS.loginButton);
    await this.page.waitForLoadState('networkidle');
  }

  async login(email: string, password: string) {
    await this.fillEmail(email);
    await this.fillPassword(password);
    await this.submit();
  }

  async expectLoginForm() {
    await expect(this.page.locator(SELECTORS.loginForm)).toBeVisible();
    await expect(this.page.locator(SELECTORS.emailInput)).toBeVisible();
    await expect(this.page.locator(SELECTORS.passwordInput)).toBeVisible();
    await expect(this.page.locator(SELECTORS.loginButton)).toBeVisible();
  }

  async expectValidationError(message: string) {
    await expect(this.page.locator(`text=${message}`)).toBeVisible();
  }

  async expectLoginError() {
    // Look for error message or toast
    await expect(
      this.page.locator('text=Invalid credentials').or(
        this.page.locator('text=Login failed')
      )
    ).toBeVisible({ timeout: TIMEOUTS.short });
  }

  async expectRedirectToDashboard() {
    await expect(this.page).toHaveURL('/dashboard');
  }

  async expectRedirectToClientPortal() {
    await expect(this.page).toHaveURL('/client-portal/dashboard');
  }
}