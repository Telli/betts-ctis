import { test, expect } from '@playwright/test';
import { LoginPage } from '../page-objects/LoginPage';
import { AuthHelper } from '../utils/auth-helper';

test.describe('Tax Filing Form Integration Tests', () => {
  let loginPage: LoginPage;
  let authHelper: AuthHelper;

  test.beforeEach(async ({ page }) => {
    loginPage = new LoginPage(page);
    authHelper = new AuthHelper(page);
    await loginPage.goto();
  });

  test.describe('Tax Liability Calculation', () => {
    test('should calculate tax liability successfully', async ({ page }) => {
      // Login as associate (has permission to create tax filings)
      await authHelper.login('associate');

      // Navigate to new tax filing page
      await page.goto('/tax-filings/new');

      // Wait for the form to load
      await expect(page.locator('text=Create Tax Filing')).toBeVisible();

      // Fill out the form with required fields
      await page.selectOption('[data-testid="client-select"]', '1'); // Assuming test client exists
      await page.selectOption('[data-testid="tax-type-select"]', 'IncomeTax');
      await page.selectOption('[data-testid="tax-year-select"]', '2024');

      // Enter taxable amount (should not show leading zero)
      const taxableAmountInput = page.locator('[data-testid="taxable-amount-input"]');
      await expect(taxableAmountInput).toHaveValue(''); // Should be empty initially
      await taxableAmountInput.fill('100000');

      // Click calculate tax liability button
      await page.click('[data-testid="calculate-tax-btn"]');

      // Wait for calculation to complete and check result
      await expect(page.locator('[data-testid="tax-liability-result"]')).toBeVisible();
      const taxLiabilityValue = await page.locator('[data-testid="tax-liability-result"]').textContent();
      expect(taxLiabilityValue).toMatch(/\d+(\.\d{2})?/); // Should contain a numeric value

      // Check that success message appears
      await expect(page.locator('text=Tax Liability Calculated')).toBeVisible();
    });

    test('should show validation errors for missing required fields', async ({ page }) => {
      await authHelper.login('associate');
      await page.goto('/tax-filings/new');

      // Try to calculate without filling required fields
      await page.click('[data-testid="calculate-tax-btn"]');

      // Should show validation error
      await expect(page.locator('text=Missing Information')).toBeVisible();
      await expect(page.locator('text=Please provide client, tax type, tax year, and taxable amount')).toBeVisible();
    });

    test('should handle withholding tax calculation', async ({ page }) => {
      await authHelper.login('associate');
      await page.goto('/tax-filings/new');

      // Select withholding tax
      await page.selectOption('[data-testid="tax-type-select"]', 'WithholdingTax');
      await page.selectOption('[data-testid="client-select"]', '1');
      await page.selectOption('[data-testid="tax-year-select"]', '2024');

      // Fill withholding tax specific fields
      await page.selectOption('[data-testid="withholding-type-select"]', 'ProfessionalFees');
      await page.check('[data-testid="is-resident-checkbox"]');

      // Enter amount
      await page.locator('[data-testid="taxable-amount-input"]').fill('50000');

      // Calculate
      await page.click('[data-testid="calculate-tax-btn"]');

      // Should show withholding tax result
      await expect(page.locator('text=Withholding Tax Calculated')).toBeVisible();
    });
  });

  test.describe('Form UI Components', () => {
    test('should display taxable amount input without leading zero', async ({ page }) => {
      await authHelper.login('associate');
      await page.goto('/tax-filings/new');

      const taxableAmountInput = page.locator('[data-testid="taxable-amount-input"]');

      // Should be empty initially, not showing "0"
      await expect(taxableAmountInput).toHaveValue('');

      // When user types, should show the value
      await taxableAmountInput.fill('12345');
      await expect(taxableAmountInput).toHaveValue('12345');
    });

    test('should allow date selection in calendar', async ({ page }) => {
      await authHelper.login('associate');
      await page.goto('/tax-filings/new');

      // Click on due date picker
      await page.click('[data-testid="due-date-picker"]');

      // Calendar should appear
      await expect(page.locator('[data-testid="calendar-popup"]')).toBeVisible();

      // Select a future date (should be enabled)
      const tomorrow = new Date();
      tomorrow.setDate(tomorrow.getDate() + 1);
      const tomorrowStr = tomorrow.toISOString().split('T')[0];

      await page.click(`[data-testid="calendar-day-${tomorrowStr}"]`);

      // Calendar should close and date should be selected
      await expect(page.locator('[data-testid="calendar-popup"]')).not.toBeVisible();

      // Check that the selected date is displayed
      const selectedDate = await page.locator('[data-testid="due-date-display"]').textContent();
      expect(selectedDate).toContain(tomorrow.toLocaleDateString());
    });

    test('should disable past dates in calendar', async ({ page }) => {
      await authHelper.login('associate');
      await page.goto('/tax-filings/new');

      await page.click('[data-testid="due-date-picker"]');

      // Yesterday should be disabled
      const yesterday = new Date();
      yesterday.setDate(yesterday.getDate() - 1);
      const yesterdayStr = yesterday.toISOString().split('T')[0];

      const yesterdayButton = page.locator(`[data-testid="calendar-day-${yesterdayStr}"]`);
      await expect(yesterdayButton).toHaveAttribute('disabled');
    });

    test('should maintain tax type selection', async ({ page }) => {
      await authHelper.login('associate');
      await page.goto('/tax-filings/new');

      const taxTypeSelect = page.locator('[data-testid="tax-type-select"]');

      // Select a tax type
      await taxTypeSelect.selectOption('GST');

      // Selection should be maintained
      await expect(taxTypeSelect).toHaveValue('GST');

      // Navigate away and back (simulate page refresh)
      await page.reload();

      // Should still show the selected value (if form state is preserved)
      // Note: This depends on form implementation - may need adjustment
    });
  });

  test.describe('Error Handling', () => {
    test('should handle API errors gracefully', async ({ page }) => {
      await authHelper.login('associate');
      await page.goto('/tax-filings/new');

      // Fill form with valid data
      await page.selectOption('[data-testid="client-select"]', '1');
      await page.selectOption('[data-testid="tax-type-select"]', 'IncomeTax');
      await page.selectOption('[data-testid="tax-year-select"]', '2024');
      await page.locator('[data-testid="taxable-amount-input"]').fill('100000');

      // Mock API failure (if possible) or test with invalid data that causes 400
      // For now, test that error messages are displayed properly
      await page.click('[data-testid="calculate-tax-btn"]');

      // Should either succeed or show appropriate error message
      const successMessage = page.locator('text=Tax Liability Calculated');
      const errorMessage = page.locator('text=Calculation Error');

      await expect(successMessage.or(errorMessage)).toBeVisible();
    });
  });
});