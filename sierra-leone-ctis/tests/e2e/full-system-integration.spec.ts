import { test, expect } from '@playwright/test';
import { LoginPage } from '../page-objects/LoginPage';

test.describe('Full System Integration Tests', () => {
  let loginPage: LoginPage;

  test.beforeEach(async ({ page }) => {
    loginPage = new LoginPage(page);
    await loginPage.goto();
  });

  test.describe('End-to-End Tax Filing Workflow', () => {
    test('should complete full tax filing workflow from client creation to payment', async ({ page }) => {
      // 1. Admin creates new client
      await loginPage.loginAsAdmin();
      await page.goto('/clients/new');
      
      const testCompany = `E2E Test Company ${Date.now()}`;
      const testEmail = `e2e-test-${Date.now()}@example.com`;
      
      await page.fill('[data-testid="company-name"]', testCompany);
      await page.fill('[data-testid="email"]', testEmail);
      await page.fill('[data-testid="phone"]', '076123456');
      await page.selectOption('[data-testid="taxpayer-category"]', 'Medium');
      await page.fill('[data-testid="address"]', '123 Test Street, Freetown');
      
      await page.click('[data-testid="submit-client"]');
      await expect(page.locator('[data-testid="success-message"]')).toBeVisible();
      
      // Get client ID from URL or response
      const clientId = await page.locator('[data-testid="client-id"]').textContent();
      
      // 2. Associate creates tax year and filing requirements
      await page.goto(`/clients/${clientId}`);
      await page.click('[data-testid="add-tax-year-btn"]');
      
      await page.selectOption('[data-testid="tax-year"]', '2024');
      await page.check('[data-testid="tax-type-gst"]');
      await page.check('[data-testid="tax-type-income-tax"]');
      
      await page.click('[data-testid="create-tax-year"]');
      await expect(page.locator('[data-testid="tax-year-created"]')).toBeVisible();
      
      // 3. Client receives invitation and sets up account
      // Simulate client registration process
      await page.goto('/enroll/register/test-token');
      
      await page.fill('[data-testid="password"]', 'SecurePassword123!');
      await page.fill('[data-testid="confirm-password"]', 'SecurePassword123!');
      await page.click('[data-testid="complete-registration"]');
      
      await expect(page.locator('[data-testid="registration-success"]')).toBeVisible();
      
      // 4. Client logs in and uploads documents
      await loginPage.loginAs(testEmail, 'SecurePassword123!');
      await page.goto('/client-portal/documents');
      
      // Upload required documents
      const fileInput = page.locator('[data-testid="document-upload-input"]');
      await fileInput.setInputFiles({
        name: 'tax-return-2024.pdf',
        mimeType: 'application/pdf',
        buffer: Buffer.from('Mock PDF content for testing')
      });
      
      await page.selectOption('[data-testid="document-type"]', 'TaxReturn');
      await page.fill('[data-testid="document-description"]', 'Annual tax return for 2024');
      await page.click('[data-testid="upload-document"]');
      
      await expect(page.locator('[data-testid="document-uploaded"]')).toBeVisible();
      
      // Upload financial statements
      await fileInput.setInputFiles({
        name: 'financial-statements-2024.pdf',
        mimeType: 'application/pdf',
        buffer: Buffer.from('Mock financial statements content')
      });
      
      await page.selectOption('[data-testid="document-type"]', 'FinancialStatements');
      await page.click('[data-testid="upload-document"]');
      
      // 5. Client creates tax filing
      await page.goto('/client-portal/tax-filings');
      await page.click('[data-testid="new-tax-filing-btn"]');
      
      await page.selectOption('[data-testid="tax-type"]', 'GST');
      await page.selectOption('[data-testid="tax-year"]', '2024');
      await page.fill('[data-testid="gross-revenue"]', '1000000');
      await page.fill('[data-testid="taxable-income"]', '800000');
      await page.fill('[data-testid="tax-amount"]', '120000');
      
      await page.click('[data-testid="submit-tax-filing"]');
      await expect(page.locator('[data-testid="filing-submitted"]')).toBeVisible();
      
      // 6. Associate reviews and approves filing
      await loginPage.loginAsAssociate();
      await page.goto('/associate/clients');
      
      await page.click(`[data-testid="client-${clientId}"]`);
      await page.click('[data-testid="pending-filings-tab"]');
      
      const pendingFiling = page.locator('[data-testid="pending-filing-1"]');
      await pendingFiling.click();
      
      // Review filing details
      await expect(page.locator('[data-testid="filing-details"]')).toBeVisible();
      await expect(page.locator('[data-testid="tax-amount-review"]')).toContainText('120,000');
      
      // Add review notes
      await page.fill('[data-testid="review-notes"]', 'Filing reviewed and approved. All documents verified.');
      await page.click('[data-testid="approve-filing-btn"]');
      
      await expect(page.locator('[data-testid="filing-approved"]')).toBeVisible();
      
      // 7. Client makes payment
      await loginPage.loginAs(testEmail, 'SecurePassword123!');
      await page.goto('/client-portal/payments');
      
      await page.click('[data-testid="new-payment-btn"]');
      await page.fill('[data-testid="payment-amount"]', '120000');
      await page.selectOption('[data-testid="tax-type"]', 'GST');
      await page.selectOption('[data-testid="tax-year"]', '2024');
      
      // Use Orange Money for payment
      await page.click('[data-testid="payment-method-orange-money"]');
      await page.fill('[data-testid="orange-money-phone"]', '076123456');
      
      await page.click('[data-testid="submit-payment-btn"]');
      
      // Complete payment
      await page.fill('[data-testid="orange-money-pin"]', '1234');
      await page.click('[data-testid="confirm-orange-payment-btn"]');
      
      await expect(page.locator('[data-testid="payment-success"]')).toBeVisible({ timeout: 15000 });
      
      // 8. Verify compliance score update
      await page.goto('/client-portal/dashboard');
      
      // Compliance score should improve after successful filing and payment
      const complianceScore = await page.locator('[data-testid="compliance-score-value"]').textContent();
      expect(parseFloat(complianceScore!)).toBeGreaterThan(80);
      
      await expect(page.locator('[data-testid="compliance-level"]')).toContainText('Green');
      
      // 9. Admin verifies system-wide metrics update
      await loginPage.loginAsAdmin();
      await page.goto('/dashboard');
      
      // KPIs should reflect the new filing and payment
      await page.click('[data-testid="refresh-kpis-btn"]');
      await page.waitForTimeout(2000);
      
      const filingTimeliness = await page.locator('[data-testid="filing-timeliness-value"]').textContent();
      const paymentCompletionRate = await page.locator('[data-testid="payment-completion-rate-value"]').textContent();
      
      expect(parseFloat(filingTimeliness!)).toBeGreaterThan(0);
      expect(parseFloat(paymentCompletionRate!)).toBeGreaterThan(0);
      
      // 10. Generate and verify reports include new data
      await page.goto('/reports');
      await page.selectOption('[data-testid="report-type-select"]', 'TaxFilingReport');
      await page.fill('[data-testid="from-date"]', '2024-01-01');
      await page.fill('[data-testid="to-date"]', '2024-12-31');
      
      await page.click('[data-testid="generate-report-btn"]');
      await expect(page.locator('[data-testid="report-completed"]')).toBeVisible({ timeout: 30000 });
      
      // The generated report should include the new client's filing
      await page.click('[data-testid="preview-report-btn"]');
      await expect(page.locator('[data-testid="report-preview-content"]')).toContainText(testCompany);
    });
  });

  test.describe('Multi-User Collaboration Workflow', () => {
    test('should handle concurrent user actions without conflicts', async ({ browser }) => {
      // Create multiple browser contexts for different users
      const adminContext = await browser.newContext();
      const associateContext = await browser.newContext();
      const clientContext = await browser.newContext();
      
      const adminPage = await adminContext.newPage();
      const associatePage = await associateContext.newPage();
      const clientPage = await clientContext.newPage();
      
      // Set up login pages for each user
      const adminLogin = new LoginPage(adminPage);
      const associateLogin = new LoginPage(associatePage);
      const clientLogin = new LoginPage(clientPage);
      
      await adminLogin.goto();
      await associateLogin.goto();
      await clientLogin.goto();
      
      // All users log in simultaneously
      await Promise.all([
        adminLogin.loginAsAdmin(),
        associateLogin.loginAsAssociate(),
        clientLogin.loginAsClient()
      ]);
      
      // Admin monitors dashboard while others work
      await adminPage.goto('/dashboard');
      await expect(adminPage.locator('[data-testid="internal-kpi-dashboard"]')).toBeVisible();
      
      // Associate reviews client documents
      await associatePage.goto('/associate/clients');
      await associatePage.click('[data-testid="client-1"]');
      await associatePage.click('[data-testid="documents-tab"]');
      
      // Client uploads new document simultaneously
      await clientPage.goto('/client-portal/documents');
      
      const fileInput = clientPage.locator('[data-testid="document-upload-input"]');
      await fileInput.setInputFiles({
        name: 'concurrent-test-doc.pdf',
        mimeType: 'application/pdf',
        buffer: Buffer.from('Concurrent upload test content')
      });
      
      await clientPage.selectOption('[data-testid="document-type"]', 'TaxReturn');
      await clientPage.click('[data-testid="upload-document"]');
      
      // Associate should see the new document appear
      await associatePage.click('[data-testid="refresh-documents"]');
      await expect(associatePage.locator('[data-testid="document-concurrent-test-doc"]')).toBeVisible({ timeout: 10000 });
      
      // Associate approves document while client is still online
      await associatePage.click('[data-testid="document-concurrent-test-doc"]');
      await associatePage.click('[data-testid="approve-document-btn"]');
      
      // Client should see approval notification in real-time
      await expect(clientPage.locator('[data-testid="document-approved-notification"]')).toBeVisible({ timeout: 5000 });
      
      // Admin should see updated metrics
      await adminPage.click('[data-testid="refresh-kpis-btn"]');
      await adminPage.waitForTimeout(2000);
      
      // Verify no conflicts occurred
      await expect(adminPage.locator('[data-testid="system-error"]')).not.toBeVisible();
      await expect(associatePage.locator('[data-testid="conflict-error"]')).not.toBeVisible();
      await expect(clientPage.locator('[data-testid="sync-error"]')).not.toBeVisible();
      
      // Clean up contexts
      await adminContext.close();
      await associateContext.close();
      await clientContext.close();
    });
  });

  test.describe('System Resilience and Recovery', () => {
    test('should handle database connection failures gracefully', async ({ page }) => {
      await loginPage.loginAsAdmin();
      await page.goto('/dashboard');
      
      // Initial page load should work
      await expect(page.locator('[data-testid="internal-kpi-dashboard"]')).toBeVisible();
      
      // Simulate database connection failure
      await page.route('**/api/**', route => {
        if (Math.random() < 0.5) { // 50% chance of failure
          route.fulfill({
            status: 503,
            contentType: 'application/json',
            body: JSON.stringify({ error: 'Database connection failed' })
          });
        } else {
          route.continue();
        }
      });
      
      // Try to refresh KPIs multiple times
      for (let i = 0; i < 5; i++) {
        await page.click('[data-testid="refresh-kpis-btn"]');
        await page.waitForTimeout(1000);
      }
      
      // System should either show loading state or graceful error
      const hasError = await page.locator('[data-testid="system-error"]').isVisible();
      const hasLoading = await page.locator('[data-testid="loading-state"]').isVisible();
      const hasData = await page.locator('[data-testid="compliance-rate"]').isVisible();
      
      // One of these states should be present (no crashes)
      expect(hasError || hasLoading || hasData).toBe(true);
      
      // Remove network simulation
      await page.unroute('**/api/**');
      
      // System should recover
      await page.click('[data-testid="refresh-kpis-btn"]');
      await expect(page.locator('[data-testid="compliance-rate"]')).toBeVisible({ timeout: 10000 });
    });

    test('should maintain data consistency during high load', async ({ browser }) => {
      // Create multiple contexts to simulate high load
      const contexts = await Promise.all(
        Array(5).fill(0).map(() => browser.newContext())
      );
      
      const pages = await Promise.all(
        contexts.map(context => context.newPage())
      );
      
      // All users perform concurrent operations
      const operations = pages.map(async (page, index) => {
        const login = new LoginPage(page);
        await login.goto();
        await login.loginAsAdmin();
        
        // Perform different operations simultaneously
        switch (index % 3) {
          case 0:
            // Create clients
            await page.goto('/clients/new');
            await page.fill('[data-testid="company-name"]', `Load Test Company ${index}`);
            await page.fill('[data-testid="email"]', `loadtest${index}@example.com`);
            await page.selectOption('[data-testid="taxpayer-category"]', 'Small');
            await page.click('[data-testid="submit-client"]');
            break;
            
          case 1:
            // Generate reports
            await page.goto('/reports');
            await page.selectOption('[data-testid="report-type-select"]', 'ComplianceReport');
            await page.fill('[data-testid="from-date"]', '2024-01-01');
            await page.fill('[data-testid="to-date"]', '2024-12-31');
            await page.click('[data-testid="generate-report-btn"]');
            break;
            
          case 2:
            // Refresh KPIs
            await page.goto('/dashboard');
            for (let i = 0; i < 3; i++) {
              await page.click('[data-testid="refresh-kpis-btn"]');
              await page.waitForTimeout(500);
            }
            break;
        }
      });
      
      // Wait for all operations to complete
      await Promise.allSettled(operations);
      
      // Verify system stability
      const adminPage = pages[0];
      await adminPage.goto('/dashboard');
      
      // Dashboard should still be functional
      await expect(adminPage.locator('[data-testid="internal-kpi-dashboard"]')).toBeVisible();
      
      // Data should be consistent
      const clientCount = await adminPage.locator('[data-testid="total-active-clients-value"]').textContent();
      expect(parseInt(clientCount!)).toBeGreaterThan(0);
      
      // Clean up contexts
      await Promise.all(contexts.map(context => context.close()));
    });
  });

  test.describe('Cross-Browser Compatibility', () => {
    test('should work consistently across different browsers', async ({ browserName }) => {
      // This test will run on Chrome, Firefox, and Safari as configured
      await loginPage.loginAsAdmin();
      await page.goto('/dashboard');
      
      // Core functionality should work regardless of browser
      await expect(page.locator('[data-testid="internal-kpi-dashboard"]')).toBeVisible();
      
      // Test interactive elements
      await page.click('[data-testid="refresh-kpis-btn"]');
      await page.waitForTimeout(2000);
      
      await expect(page.locator('[data-testid="compliance-rate"]')).toBeVisible();
      
      // Test form functionality
      await page.goto('/clients/new');
      await page.fill('[data-testid="company-name"]', `${browserName} Test Company`);
      await page.fill('[data-testid="email"]', `${browserName.toLowerCase()}test@example.com`);
      await page.selectOption('[data-testid="taxpayer-category"]', 'Medium');
      
      // Form should be functional
      await page.click('[data-testid="submit-client"]');
      await expect(page.locator('[data-testid="success-message"]')).toBeVisible();
      
      // Test responsive design
      await page.setViewportSize({ width: 768, height: 1024 }); // Tablet
      await page.goto('/dashboard');
      await expect(page.locator('[data-testid="mobile-navigation"]')).toBeVisible();
      
      await page.setViewportSize({ width: 375, height: 667 }); // Mobile
      await expect(page.locator('[data-testid="mobile-menu-btn"]')).toBeVisible();
    });
  });

  test.describe('Data Migration and Backup Verification', () => {
    test('should maintain data integrity during system updates', async ({ page }) => {
      await loginPage.loginAsAdmin();
      
      // Create test data
      await page.goto('/clients/new');
      const testCompany = `Migration Test Company ${Date.now()}`;
      
      await page.fill('[data-testid="company-name"]', testCompany);
      await page.fill('[data-testid="email"]', `migration-test-${Date.now()}@example.com`);
      await page.selectOption('[data-testid="taxpayer-category"]', 'Large');
      await page.click('[data-testid="submit-client"]');
      
      const clientId = await page.locator('[data-testid="client-id"]').textContent();
      
      // Simulate system maintenance/update
      await page.route('**/api/system/maintenance', route => {
        route.fulfill({
          status: 200,
          contentType: 'application/json',
          body: JSON.stringify({ message: 'Maintenance mode activated' })
        });
      });
      
      // Trigger maintenance mode
      await page.goto('/admin/settings/system');
      await page.click('[data-testid="maintenance-mode-btn"]');
      
      await expect(page.locator('[data-testid="maintenance-active"]')).toBeVisible();
      
      // Exit maintenance
      await page.click('[data-testid="exit-maintenance-btn"]');
      await expect(page.locator('[data-testid="system-operational"]')).toBeVisible();
      
      // Verify data integrity after maintenance
      await page.goto(`/clients/${clientId}`);
      await expect(page.locator('[data-testid="company-name"]')).toContainText(testCompany);
      
      // Verify system functions normally
      await page.goto('/dashboard');
      await expect(page.locator('[data-testid="internal-kpi-dashboard"]')).toBeVisible();
    });
  });
});