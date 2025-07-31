import { test, expect } from '@playwright/test';
import { LoginPage } from '../page-objects/LoginPage';

test.describe('Payment Gateway Integration', () => {
  let loginPage: LoginPage;

  test.beforeEach(async ({ page }) => {
    loginPage = new LoginPage(page);
    await loginPage.goto();
  });

  test.describe('Orange Money Integration', () => {
    test('should process Orange Money payment successfully', async ({ page }) => {
      await loginPage.loginAsClient();
      await page.goto('/client-portal/payments');
      
      // Wait for payments page to load
      await expect(page.locator('[data-testid="payments-page"]')).toBeVisible();
      
      // Start new payment
      await page.click('[data-testid="new-payment-btn"]');
      
      // Fill payment form
      await page.fill('[data-testid="payment-amount"]', '50000');
      await page.selectOption('[data-testid="tax-type"]', 'GST');
      await page.selectOption('[data-testid="tax-year"]', '2024');
      
      // Select Orange Money as payment method
      await page.click('[data-testid="payment-method-orange-money"]');
      
      // Fill Orange Money specific details
      await page.fill('[data-testid="orange-money-phone"]', '076123456');
      
      // Submit payment
      await page.click('[data-testid="submit-payment-btn"]');
      
      // Should redirect to Orange Money payment page
      await expect(page.locator('[data-testid="orange-money-payment-form"]')).toBeVisible();
      
      // Verify payment details are correct
      const displayedAmount = await page.locator('[data-testid="payment-amount-display"]').textContent();
      expect(displayedAmount).toContain('50,000');
      
      const displayedPhone = await page.locator('[data-testid="phone-display"]').textContent();
      expect(displayedPhone).toContain('076123456');
      
      // Simulate successful Orange Money confirmation
      await page.fill('[data-testid="orange-money-pin"]', '1234');
      await page.click('[data-testid="confirm-orange-payment-btn"]');
      
      // Wait for payment processing
      await expect(page.locator('[data-testid="payment-processing"]')).toBeVisible();
      
      // Should show success confirmation
      await expect(page.locator('[data-testid="payment-success"]')).toBeVisible({ timeout: 10000 });
      await expect(page.locator('[data-testid="transaction-id"]')).toBeVisible();
      
      // Verify payment appears in history
      await page.goto('/client-portal/payments');
      await expect(page.locator('[data-testid="payment-history-table"]')).toBeVisible();
      
      const latestPayment = page.locator('[data-testid="payment-row-1"]');
      await expect(latestPayment.locator('[data-testid="payment-method"]')).toContainText('Orange Money');
      await expect(latestPayment.locator('[data-testid="payment-status"]')).toContainText('Completed');
    });

    test('should handle Orange Money payment failures', async ({ page }) => {
      await loginPage.loginAsClient();
      await page.goto('/client-portal/payments');
      
      // Mock Orange Money API failure
      await page.route('**/api/payments/orange-money/**', route => {
        route.fulfill({
          status: 400,
          contentType: 'application/json',
          body: JSON.stringify({ error: 'Insufficient funds', code: 'INSUFFICIENT_FUNDS' })
        });
      });
      
      // Start payment process
      await page.click('[data-testid="new-payment-btn"]');
      await page.fill('[data-testid="payment-amount"]', '100000');
      await page.selectOption('[data-testid="tax-type"]', 'IncomeTax');
      await page.click('[data-testid="payment-method-orange-money"]');
      await page.fill('[data-testid="orange-money-phone"]', '076123456');
      await page.click('[data-testid="submit-payment-btn"]');
      
      // Simulate payment failure
      await page.fill('[data-testid="orange-money-pin"]', '1234');
      await page.click('[data-testid="confirm-orange-payment-btn"]');
      
      // Should show error message
      await expect(page.locator('[data-testid="payment-error"]')).toBeVisible();
      await expect(page.locator('[data-testid="error-message"]')).toContainText('Insufficient funds');
      
      // Should offer retry option
      await expect(page.locator('[data-testid="retry-payment-btn"]')).toBeVisible();
      
      // Test retry functionality
      await page.unroute('**/api/payments/orange-money/**');
      await page.click('[data-testid="retry-payment-btn"]');
      
      // Should return to payment form
      await expect(page.locator('[data-testid="orange-money-payment-form"]')).toBeVisible();
    });
  });

  test.describe('Africell Money Integration', () => {
    test('should process Africell Money payment successfully', async ({ page }) => {
      await loginPage.loginAsClient();
      await page.goto('/client-portal/payments');
      
      // Start new payment with Africell Money
      await page.click('[data-testid="new-payment-btn"]');
      await page.fill('[data-testid="payment-amount"]', '75000');
      await page.selectOption('[data-testid="tax-type"]', 'PayrollTax');
      await page.click('[data-testid="payment-method-africell-money"]');
      
      // Fill Africell Money specific details
      await page.fill('[data-testid="africell-money-phone"]', '077987654');
      
      // Submit payment
      await page.click('[data-testid="submit-payment-btn"]');
      
      // Should show Africell Money payment interface
      await expect(page.locator('[data-testid="africell-money-payment-form"]')).toBeVisible();
      
      // Verify payment details
      const amount = await page.locator('[data-testid="payment-amount-display"]').textContent();
      expect(amount).toContain('75,000');
      
      // Complete Africell Money payment
      await page.fill('[data-testid="africell-money-pin"]', '5678');
      await page.click('[data-testid="confirm-africell-payment-btn"]');
      
      // Wait for processing and success
      await expect(page.locator('[data-testid="payment-processing"]')).toBeVisible();
      await expect(page.locator('[data-testid="payment-success"]')).toBeVisible({ timeout: 10000 });
      
      // Verify transaction details
      const transactionId = await page.locator('[data-testid="transaction-id"]').textContent();
      expect(transactionId).toMatch(/^AFC-\d+$/);
    });

    test('should validate Africell Money phone numbers', async ({ page }) => {
      await loginPage.loginAsClient();
      await page.goto('/client-portal/payments');
      
      await page.click('[data-testid="new-payment-btn"]');
      await page.fill('[data-testid="payment-amount"]', '25000');
      await page.click('[data-testid="payment-method-africell-money"]');
      
      // Test invalid phone number formats
      const invalidNumbers = ['123456', '078123456', '077abc123'];
      
      for (const number of invalidNumbers) {
        await page.fill('[data-testid="africell-money-phone"]', number);
        await page.click('[data-testid="submit-payment-btn"]');
        
        // Should show validation error
        await expect(page.locator('[data-testid="phone-validation-error"]')).toBeVisible();
        await expect(page.locator('[data-testid="phone-validation-error"]')).toContainText('Invalid Africell Money number');
      }
      
      // Test valid number
      await page.fill('[data-testid="africell-money-phone"]', '077123456');
      await page.click('[data-testid="submit-payment-btn"]');
      
      // Should proceed without validation error
      await expect(page.locator('[data-testid="phone-validation-error"]')).not.toBeVisible();
      await expect(page.locator('[data-testid="africell-money-payment-form"]')).toBeVisible();
    });
  });

  test.describe('Bank Transfer Integration', () => {
    test('should process bank transfer payment', async ({ page }) => {
      await loginPage.loginAsClient();
      await page.goto('/client-portal/payments');
      
      // Start bank transfer payment
      await page.click('[data-testid="new-payment-btn"]');
      await page.fill('[data-testid="payment-amount"]', '150000');
      await page.selectOption('[data-testid="tax-type"]', 'IncomeTax');
      await page.click('[data-testid="payment-method-bank-transfer"]');
      
      // Fill bank details
      await page.selectOption('[data-testid="bank-select"]', 'Rokel Commercial Bank');
      await page.fill('[data-testid="account-number"]', '1234567890');
      await page.fill('[data-testid="account-holder-name"]', 'Test Company Ltd');
      
      // Submit payment
      await page.click('[data-testid="submit-payment-btn"]');
      
      // Should show bank transfer instructions
      await expect(page.locator('[data-testid="bank-transfer-instructions"]')).toBeVisible();
      
      // Verify bank details are displayed
      await expect(page.locator('[data-testid="recipient-bank"]')).toContainText('Rokel Commercial Bank');
      await expect(page.locator('[data-testid="payment-reference"]')).toBeVisible();
      
      // Mark payment as completed (admin action simulation)
      const paymentReference = await page.locator('[data-testid="payment-reference"]').textContent();
      
      // Navigate to admin panel (simulate admin user)
      await loginPage.loginAsAdmin();
      await page.goto('/admin/payments/pending');
      
      // Find and confirm the bank transfer payment
      const pendingPayment = page.locator(`[data-testid="payment-${paymentReference}"]`);
      await expect(pendingPayment).toBeVisible();
      
      await pendingPayment.locator('[data-testid="confirm-payment-btn"]').click();
      
      // Should update payment status
      await expect(pendingPayment.locator('[data-testid="payment-status"]')).toContainText('Completed');
    });
  });

  test.describe('Payment Gateway Security', () => {
    test('should encrypt sensitive payment data', async ({ page }) => {
      await loginPage.loginAsClient();
      await page.goto('/client-portal/payments');
      
      // Monitor network requests for sensitive data exposure
      const requests: any[] = [];
      page.on('request', request => {
        if (request.url().includes('/api/payments/')) {
          requests.push({
            url: request.url(),
            method: request.method(),
            postData: request.postData()
          });
        }
      });
      
      // Submit payment with sensitive data
      await page.click('[data-testid="new-payment-btn"]');
      await page.fill('[data-testid="payment-amount"]', '50000');
      await page.click('[data-testid="payment-method-orange-money"]');
      await page.fill('[data-testid="orange-money-phone"]', '076123456');
      await page.click('[data-testid="submit-payment-btn"]');
      
      // Wait for request to complete
      await page.waitForTimeout(2000);
      
      // Verify sensitive data is not transmitted in plain text
      const paymentRequests = requests.filter(r => r.method === 'POST');
      expect(paymentRequests.length).toBeGreaterThan(0);
      
      for (const request of paymentRequests) {
        if (request.postData) {
          // Phone numbers should not appear in plain text in request body
          expect(request.postData).not.toContain('076123456');
          
          // Should contain encrypted or hashed data indicators
          expect(request.postData).toMatch(/(encrypted|hash|token)/i);
        }
      }
    });

    test('should implement payment amount limits', async ({ page }) => {
      await loginPage.loginAsClient();
      await page.goto('/client-portal/payments');
      
      // Test maximum payment limit
      await page.click('[data-testid="new-payment-btn"]');
      await page.fill('[data-testid="payment-amount"]', '10000000'); // Very large amount
      await page.click('[data-testid="payment-method-orange-money"]');
      await page.click('[data-testid="submit-payment-btn"]');
      
      // Should show amount limit error
      await expect(page.locator('[data-testid="amount-limit-error"]')).toBeVisible();
      await expect(page.locator('[data-testid="amount-limit-error"]')).toContainText('exceeds maximum allowed amount');
      
      // Test minimum payment limit
      await page.fill('[data-testid="payment-amount"]', '100'); // Very small amount
      await page.click('[data-testid="submit-payment-btn"]');
      
      // Should show minimum amount error
      await expect(page.locator('[data-testid="minimum-amount-error"]')).toBeVisible();
    });

    test('should prevent duplicate payment submissions', async ({ page }) => {
      await loginPage.loginAsClient();
      await page.goto('/client-portal/payments');
      
      // Start payment process
      await page.click('[data-testid="new-payment-btn"]');
      await page.fill('[data-testid="payment-amount"]', '50000');
      await page.selectOption('[data-testid="tax-type"]', 'GST');
      await page.click('[data-testid="payment-method-orange-money"]');
      await page.fill('[data-testid="orange-money-phone"]', '076123456');
      
      // Submit payment
      await page.click('[data-testid="submit-payment-btn"]');
      
      // Button should be disabled during processing
      await expect(page.locator('[data-testid="submit-payment-btn"]')).toBeDisabled();
      
      // Try to submit again (rapid clicking simulation)
      await page.click('[data-testid="submit-payment-btn"]', { force: true });
      
      // Should not create duplicate payment
      await page.waitForTimeout(2000);
      
      // Verify only one payment was created
      await page.goto('/client-portal/payments');
      const paymentRows = await page.locator('[data-testid^="payment-row-"]').count();
      
      // Should have only one new payment
      expect(paymentRows).toBeLessThanOrEqual(1);
    });
  });

  test.describe('Payment Status Tracking', () => {
    test('should track payment status changes in real-time', async ({ page }) => {
      await loginPage.loginAsClient();
      await page.goto('/client-portal/payments');
      
      // Start payment
      await page.click('[data-testid="new-payment-btn"]');
      await page.fill('[data-testid="payment-amount"]', '30000');
      await page.click('[data-testid="payment-method-orange-money"]');
      await page.fill('[data-testid="orange-money-phone"]', '076123456');
      await page.click('[data-testid="submit-payment-btn"]');
      
      // Should show "Pending" status initially
      await expect(page.locator('[data-testid="payment-status"]')).toContainText('Pending');
      
      // Simulate payment processing
      await page.fill('[data-testid="orange-money-pin"]', '1234');
      await page.click('[data-testid="confirm-orange-payment-btn"]');
      
      // Status should change to "Processing"
      await expect(page.locator('[data-testid="payment-status"]')).toContainText('Processing');
      
      // Should eventually show "Completed"
      await expect(page.locator('[data-testid="payment-status"]')).toContainText('Completed', { timeout: 15000 });
      
      // Verify final status in payment history
      await page.goto('/client-portal/payments');
      const latestPayment = page.locator('[data-testid="payment-row-1"]');
      await expect(latestPayment.locator('[data-testid="payment-status"]')).toContainText('Completed');
    });

    test('should handle webhook status updates', async ({ page }) => {
      await loginPage.loginAsClient();
      await page.goto('/client-portal/payments');
      
      // Create payment that will be updated via webhook
      await page.click('[data-testid="new-payment-btn"]');
      await page.fill('[data-testid="payment-amount"]', '40000');
      await page.click('[data-testid="payment-method-africell-money"]');
      await page.fill('[data-testid="africell-money-phone"]', '077123456');
      await page.click('[data-testid="submit-payment-btn"]');
      
      // Get payment ID for webhook simulation
      const paymentId = await page.locator('[data-testid="payment-id"]').textContent();
      
      // Simulate webhook status update (this would normally come from payment provider)
      await page.evaluate(async (id) => {
        await fetch('/api/payments/webhook/africell', {
          method: 'POST',
          headers: { 'Content-Type': 'application/json' },
          body: JSON.stringify({
            paymentId: id,
            status: 'completed',
            transactionId: 'AFC-789012',
            timestamp: new Date().toISOString()
          })
        });
      }, paymentId);
      
      // Status should update automatically via WebSocket/SignalR
      await expect(page.locator('[data-testid="payment-status"]')).toContainText('Completed', { timeout: 10000 });
      
      // Transaction ID should be populated
      await expect(page.locator('[data-testid="transaction-id"]')).toContainText('AFC-789012');
    });
  });

  test.describe('Payment Gateway Failover', () => {
    test('should fallback to alternative payment methods when primary fails', async ({ page }) => {
      await loginPage.loginAsClient();
      await page.goto('/client-portal/payments');
      
      // Mock Orange Money service unavailable
      await page.route('**/api/payments/orange-money/**', route => {
        route.fulfill({
          status: 503,
          contentType: 'application/json',
          body: JSON.stringify({ error: 'Service temporarily unavailable' })
        });
      });
      
      // Attempt Orange Money payment
      await page.click('[data-testid="new-payment-btn"]');
      await page.fill('[data-testid="payment-amount"]', '35000');
      await page.click('[data-testid="payment-method-orange-money"]');
      await page.fill('[data-testid="orange-money-phone"]', '076123456');
      await page.click('[data-testid="submit-payment-btn"]');
      
      // Should show service unavailable message
      await expect(page.locator('[data-testid="service-unavailable"]')).toBeVisible();
      
      // Should suggest alternative payment methods
      await expect(page.locator('[data-testid="alternative-methods"]')).toBeVisible();
      await expect(page.locator('[data-testid="suggest-africell-money"]')).toBeVisible();
      await expect(page.locator('[data-testid="suggest-bank-transfer"]')).toBeVisible();
      
      // Test switching to alternative method
      await page.click('[data-testid="suggest-africell-money"]');
      
      // Should redirect to Africell Money payment form with same amount
      await expect(page.locator('[data-testid="africell-money-payment-form"]')).toBeVisible();
      const amount = await page.locator('[data-testid="payment-amount-display"]').textContent();
      expect(amount).toContain('35,000');
    });
  });
});