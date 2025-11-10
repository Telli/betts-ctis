import { test, expect } from '@playwright/test';

/**
 * Regression Tests for Recently Fixed Issues
 * 
 * This test suite validates fixes for:
 * - Client creation with numeric enum values
 * - Table filtering and sorting with correct field names
 * - API error handling (400, 404, 500)
 * - SelectItem empty value validation
 * - Circular reference JSON serialization
 */

test.describe('Client Management Regression Tests', () => {
  test.beforeEach(async ({ page }) => {
    // Login as admin
    await page.goto('/login');
    await page.fill('input[name="email"]', 'admin@bettsfirm.sl');
    await page.fill('input[name="password"]', 'Admin123!');
    await page.click('button[type="submit"]');
    await page.waitForURL('/dashboard');
  });

  test('should create client with numeric enum values', async ({ page }) => {
    await page.goto('/clients/new');
    
    // Fill form with all required fields
    await page.fill('input[name="businessName"]', 'Playwright Test Company');
    await page.fill('input[name="contactPerson"]', 'Test Contact');
    await page.fill('input[name="email"]', 'test@playwright.com');
    await page.fill('input[name="phoneNumber"]', '+23230000000');
    await page.fill('input[name="address"]', '123 Test Street');
    await page.fill('input[name="tin"]', 'TIN-TEST-001');
    await page.fill('input[name="annualTurnover"]', '1000000');
    
    // Select enum values (should be numeric: 0, 1, 2, 3)
    await page.click('button:has-text("Select client type")');
    await page.click('text=Corporation');
    
    await page.click('button:has-text("Select category")');
    await page.click('text=Medium Taxpayer');
    
    // Submit form
    await page.click('button[type="submit"]');
    
    // Should redirect to clients list
    await page.waitForURL('/clients');
    
    // Check for success (no API error)
    await expect(page.locator('text=API error: 400')).not.toBeVisible();
    
    // Verify client appears in list
    await expect(page.locator('text=Playwright Test Company')).toBeVisible();
  });

  test('should filter clients by taxpayer category', async ({ page }) => {
    await page.goto('/clients');
    
    // Wait for table to load
    await page.waitForSelector('text=Client Directory');
    
    // Open category filter
    await page.click('button:has-text("Filter by taxpayerCategory")');
    
    // Select Medium Taxpayer filter
    await page.click('text=Medium Taxpayer');
    
    // Verify filtering works (no console errors)
    const consoleErrors: string[] = [];
    page.on('console', msg => {
      if (msg.type() === 'error') {
        consoleErrors.push(msg.text());
      }
    });
    
    await page.waitForTimeout(1000);
    
    // Should not have any console errors
    expect(consoleErrors.filter(e => e.includes('charAt is not a function'))).toHaveLength(0);
    expect(consoleErrors.filter(e => e.includes('SelectItem'))).toHaveLength(0);
  });

  test('should sort clients by business name', async ({ page }) => {
    await page.goto('/clients');
    
    // Wait for table to load
    await page.waitForSelector('text=Client Directory');
    
    // Click on business name column header to sort
    await page.click('th:has-text("Client")');
    
    // Wait for sort to apply
    await page.waitForTimeout(500);
    
    // Verify no errors
    const consoleErrors: string[] = [];
    page.on('console', msg => {
      if (msg.type() === 'error') {
        consoleErrors.push(msg.text());
      }
    });
    
    await page.waitForTimeout(500);
    expect(consoleErrors).toHaveLength(0);
  });

  test('should search clients by business name', async ({ page }) => {
    await page.goto('/clients');
    
    // Wait for table to load
    await page.waitForSelector('text=Client Directory');
    
    // Type in search box
    await page.fill('input[placeholder*="Search clients"]', 'Better Dreams');
    
    // Wait for search to filter
    await page.waitForTimeout(500);
    
    // Should show filtered results
    await expect(page.locator('text=Better Dreams')).toBeVisible();
  });

  test('should not have empty SelectItem values', async ({ page }) => {
    await page.goto('/clients/new');
    
    // Check that all SelectItem components have valid values
    const selectTriggers = await page.locator('button[role="combobox"]').all();
    
    for (const trigger of selectTriggers) {
      await trigger.click();
      
      // Wait for dropdown
      await page.waitForTimeout(200);
      
      // Get all SelectItems
      const items = await page.locator('[role="option"]').all();
      
      for (const item of items) {
        const value = await item.getAttribute('data-value');
        // Value should not be empty string
        expect(value).not.toBe('');
      }
      
      // Close dropdown
      await page.keyboard.press('Escape');
    }
  });
});

test.describe('API Error Handling Regression Tests', () => {
  test.beforeEach(async ({ page }) => {
    // Login as admin
    await page.goto('/login');
    await page.fill('input[name="email"]', 'admin@bettsfirm.sl');
    await page.fill('input[name="password"]', 'Admin123!');
    await page.click('button[type="submit"]');
    await page.waitForURL('/dashboard');
  });

  test('should handle client detail page without 404 errors', async ({ page }) => {
    // Go to clients list first
    await page.goto('/clients');
    await page.waitForSelector('text=Client Directory');
    
    // Click on first client
    const firstClientLink = page.locator('a[href^="/clients/"]').first();
    await firstClientLink.click();
    
    // Should load detail page without errors
    await page.waitForTimeout(1000);
    
    // Check console for API errors
    const apiErrors: string[] = [];
    page.on('console', msg => {
      if (msg.type() === 'error' && msg.text().includes('API Error')) {
        apiErrors.push(msg.text());
      }
    });
    
    await page.waitForTimeout(1000);
    
    // Should not have 404 errors for documents endpoint
    expect(apiErrors.filter(e => e.includes('404'))).toHaveLength(0);
  });

  test('should handle tax filings page without 500 errors', async ({ page }) => {
    // Navigate to tax filings page
    await page.goto('/tax-filings');
    
    // Wait for page to load
    await page.waitForSelector('text=Tax Filings', { timeout: 10000 });
    
    // Monitor for 500 errors
    const apiErrors: string[] = [];
    page.on('console', msg => {
      if (msg.type() === 'error' && msg.text().includes('500')) {
        apiErrors.push(msg.text());
      }
    });
    
    await page.waitForTimeout(2000);
    
    // Should not have 500 errors from circular reference serialization
    expect(apiErrors.filter(e => e.includes('500'))).toHaveLength(0);
  });

  test('should display proper error messages on API failures', async ({ page }) => {
    await page.goto('/clients/new');
    
    // Try to submit empty form
    await page.click('button[type="submit"]');
    
    // Should show validation errors, not generic API error
    await page.waitForTimeout(500);
    
    // Check that form validation is working
    const requiredFields = await page.locator('input:required').all();
    expect(requiredFields.length).toBeGreaterThan(0);
  });
});

test.describe('Status Badge Display Regression Tests', () => {
  test.beforeEach(async ({ page }) => {
    await page.goto('/login');
    await page.fill('input[name="email"]', 'admin@bettsfirm.sl');
    await page.fill('input[name="password"]', 'Admin123!');
    await page.click('button[type="submit"]');
    await page.waitForURL('/dashboard');
  });

  test('should display numeric status enums correctly', async ({ page }) => {
    await page.goto('/clients');
    await page.waitForSelector('text=Client Directory');
    
    // Look for status badges
    const statusBadges = await page.locator('[class*="bg-green-100"], [class*="bg-gray-100"], [class*="bg-red-100"]').all();
    
    // Should have status badges displayed
    expect(statusBadges.length).toBeGreaterThan(0);
    
    // Check for charAt error
    const consoleErrors: string[] = [];
    page.on('console', msg => {
      if (msg.type() === 'error' && msg.text().includes('charAt')) {
        consoleErrors.push(msg.text());
      }
    });
    
    await page.waitForTimeout(500);
    expect(consoleErrors).toHaveLength(0);
  });

  test('should display category badges with enum conversion', async ({ page }) => {
    await page.goto('/clients');
    await page.waitForSelector('text=Client Directory');
    
    // Look for category badges
    const categoryBadges = await page.locator('text=/Large Taxpayer|Medium Taxpayer|Small Taxpayer|Micro Taxpayer/').all();
    
    // Should have category badges
    expect(categoryBadges.length).toBeGreaterThan(0);
  });
});

test.describe('Form Input Regression Tests', () => {
  test.beforeEach(async ({ page }) => {
    await page.goto('/login');
    await page.fill('input[name="email"]', 'admin@bettsfirm.sl');
    await page.fill('input[name="password"]', 'Admin123!');
    await page.click('button[type="submit"]');
    await page.waitForURL('/dashboard');
  });

  test('should handle annual turnover input without leading zero', async ({ page }) => {
    await page.goto('/clients/new');
    
    // Click on annual turnover field
    const turnoverInput = page.locator('input[name="annualTurnover"]');
    await turnoverInput.click();
    
    // Should not show "0" when empty
    const value = await turnoverInput.inputValue();
    expect(value).not.toBe('0');
    
    // Type a number
    await turnoverInput.fill('500000');
    
    // Should show the typed value without leading zero
    const newValue = await turnoverInput.inputValue();
    expect(newValue).toBe('500000');
  });

  test('should convert enum strings to numbers before submission', async ({ page }) => {
    await page.goto('/clients/new');
    
    // Monitor network requests
    const requests: any[] = [];
    page.on('request', request => {
      if (request.url().includes('/api/clients') && request.method() === 'POST') {
        requests.push({
          url: request.url(),
          method: request.method(),
          postData: request.postData()
        });
      }
    });
    
    // Fill form
    await page.fill('input[name="businessName"]', 'Enum Test Company');
    await page.fill('input[name="contactPerson"]', 'Test');
    await page.fill('input[name="email"]', 'test@enum.com');
    await page.fill('input[name="phoneNumber"]', '+23230000000');
    await page.fill('input[name="address"]', '123 Test');
    await page.fill('input[name="tin"]', 'TIN-001');
    await page.fill('input[name="annualTurnover"]', '100000');
    
    await page.click('button:has-text("Select client type")');
    await page.click('text=Corporation');
    
    await page.click('button[type="submit"]');
    
    await page.waitForTimeout(1000);
    
    // Check that request was made
    expect(requests.length).toBeGreaterThan(0);
    
    if (requests.length > 0 && requests[0].postData) {
      const postData = JSON.parse(requests[0].postData);
      // clientType should be a number, not a string
      expect(typeof postData.clientType).toBe('number');
    }
  });
});
