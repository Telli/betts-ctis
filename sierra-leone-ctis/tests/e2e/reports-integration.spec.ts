import { test, expect } from '@playwright/test';
import { LoginPage } from '../page-objects/LoginPage';

test.describe('Reports Integration', () => {
  let loginPage: LoginPage;

  test.beforeEach(async ({ page }) => {
    loginPage = new LoginPage(page);
    await loginPage.goto();
  });

  test.describe('Report Generation', () => {
    test('should generate tax filing report successfully', async ({ page }) => {
      await loginPage.loginAsAdmin();
      await page.goto('/reports');
      
      // Wait for reports page to load
      await expect(page.locator('[data-testid="reports-page"]')).toBeVisible();
      
      // Select report type
      await page.selectOption('[data-testid="report-type-select"]', 'TaxFilingReport');
      
      // Select format
      await page.selectOption('[data-testid="report-format-select"]', 'PDF');
      
      // Set date range
      await page.fill('[data-testid="from-date"]', '2024-01-01');
      await page.fill('[data-testid="to-date"]', '2024-12-31');
      
      // Select client (optional filter)
      await page.click('[data-testid="client-filter-dropdown"]');
      await page.click('[data-testid="client-option-1"]');
      
      // Generate report
      await page.click('[data-testid="generate-report-btn"]');
      
      // Wait for report generation to start
      await expect(page.locator('[data-testid="report-generation-progress"]')).toBeVisible();
      
      // Wait for completion (with timeout)
      await expect(page.locator('[data-testid="report-completed"]')).toBeVisible({ timeout: 30000 });
      
      // Verify download link is available
      await expect(page.locator('[data-testid="download-report-btn"]')).toBeVisible();
      
      // Test download functionality
      const downloadPromise = page.waitForEvent('download');
      await page.click('[data-testid="download-report-btn"]');
      const download = await downloadPromise;
      
      expect(download.suggestedFilename()).toMatch(/tax-filing-report.*\.pdf$/);
    });

    test('should generate compliance report with charts', async ({ page }) => {
      await loginPage.loginAsAdmin();
      await page.goto('/reports');
      
      // Select compliance report
      await page.selectOption('[data-testid="report-type-select"]', 'ComplianceReport');
      await page.selectOption('[data-testid="report-format-select"]', 'PDF');
      
      // Set parameters
      await page.fill('[data-testid="from-date"]', '2024-01-01');
      await page.fill('[data-testid="to-date"]', '2024-12-31');
      
      // Include charts option
      await page.check('[data-testid="include-charts-checkbox"]');
      
      // Generate report
      await page.click('[data-testid="generate-report-btn"]');
      
      // Monitor progress
      await expect(page.locator('[data-testid="report-progress-bar"]')).toBeVisible();
      
      // Wait for completion
      await expect(page.locator('[data-testid="report-completed"]')).toBeVisible({ timeout: 45000 });
      
      // Verify report preview is available
      await expect(page.locator('[data-testid="preview-report-btn"]')).toBeVisible();
      
      // Test preview functionality
      await page.click('[data-testid="preview-report-btn"]');
      await expect(page.locator('[data-testid="report-preview-modal"]')).toBeVisible();
      
      // Verify preview content
      await expect(page.locator('[data-testid="preview-content"]')).toContainText('Compliance Report');
    });

    test('should generate Excel payment report', async ({ page }) => {
      await loginPage.loginAsAdmin();
      await page.goto('/reports');
      
      // Select payment report in Excel format
      await page.selectOption('[data-testid="report-type-select"]', 'PaymentReport');
      await page.selectOption('[data-testid="report-format-select"]', 'Excel');
      
      // Set date range
      await page.fill('[data-testid="from-date"]', '2024-01-01');
      await page.fill('[data-testid="to-date"]', '2024-12-31');
      
      // Select payment status filter
      await page.selectOption('[data-testid="payment-status-filter"]', 'Completed');
      
      // Generate report
      await page.click('[data-testid="generate-report-btn"]');
      
      // Wait for completion
      await expect(page.locator('[data-testid="report-completed"]')).toBeVisible({ timeout: 30000 });
      
      // Download Excel file
      const downloadPromise = page.waitForEvent('download');
      await page.click('[data-testid="download-report-btn"]');
      const download = await downloadPromise;
      
      expect(download.suggestedFilename()).toMatch(/payment-report.*\.xlsx$/);
    });
  });

  test.describe('Report History and Management', () => {
    test('should display report history', async ({ page }) => {
      await loginPage.loginAsAdmin();
      await page.goto('/reports');
      
      // Navigate to report history
      await page.click('[data-testid="report-history-tab"]');
      
      // Wait for history to load
      await expect(page.locator('[data-testid="report-history-table"]')).toBeVisible();
      
      // Verify history columns
      await expect(page.locator('[data-testid="history-header-report-type"]')).toBeVisible();
      await expect(page.locator('[data-testid="history-header-generated-date"]')).toBeVisible();
      await expect(page.locator('[data-testid="history-header-status"]')).toBeVisible();
      await expect(page.locator('[data-testid="history-header-actions"]')).toBeVisible();
      
      // Test sorting by date
      await page.click('[data-testid="sort-by-date"]');
      
      // Verify reports are sorted (most recent first)
      const dates = await page.locator('[data-testid^="report-date-"]').allTextContents();
      const sortedDates = [...dates].sort((a, b) => new Date(b).getTime() - new Date(a).getTime());
      expect(dates).toEqual(sortedDates);
    });

    test('should filter report history', async ({ page }) => {
      await loginPage.loginAsAdmin();
      await page.goto('/reports');
      await page.click('[data-testid="report-history-tab"]');
      
      // Apply report type filter
      await page.selectOption('[data-testid="history-filter-type"]', 'TaxFilingReport');
      
      // Wait for filtered results
      await page.waitForTimeout(1000);
      
      // Verify all visible reports are of selected type
      const reportTypes = await page.locator('[data-testid^="report-type-"]').allTextContents();
      reportTypes.forEach(type => {
        expect(type).toBe('Tax Filing Report');
      });
      
      // Test status filter
      await page.selectOption('[data-testid="history-filter-status"]', 'Completed');
      await page.waitForTimeout(1000);
      
      // Verify all visible reports are completed
      const statuses = await page.locator('[data-testid^="report-status-"]').allTextContents();
      statuses.forEach(status => {
        expect(status).toBe('Completed');
      });
    });

    test('should delete report from history', async ({ page }) => {
      await loginPage.loginAsAdmin();
      await page.goto('/reports');
      await page.click('[data-testid="report-history-tab"]');
      
      // Wait for history to load
      await expect(page.locator('[data-testid="report-history-table"]')).toBeVisible();
      
      const initialRowCount = await page.locator('[data-testid^="report-row-"]').count();
      
      if (initialRowCount > 0) {
        // Delete first report
        await page.click('[data-testid="delete-report-1"]');
        
        // Confirm deletion
        await expect(page.locator('[data-testid="delete-confirmation-modal"]')).toBeVisible();
        await page.click('[data-testid="confirm-delete-btn"]');
        
        // Wait for deletion to complete
        await expect(page.locator('[data-testid="report-deleted-success"]')).toBeVisible();
        
        // Verify row count decreased
        const finalRowCount = await page.locator('[data-testid^="report-row-"]').count();
        expect(finalRowCount).toBe(initialRowCount - 1);
      }
    });
  });

  test.describe('Scheduled Reports', () => {
    test('should create scheduled report', async ({ page }) => {
      await loginPage.loginAsAdmin();
      await page.goto('/reports');
      
      // Navigate to scheduled reports
      await page.click('[data-testid="scheduled-reports-tab"]');
      
      // Create new schedule
      await page.click('[data-testid="create-schedule-btn"]');
      
      // Fill schedule form
      await page.selectOption('[data-testid="schedule-report-type"]', 'ComplianceReport');
      await page.selectOption('[data-testid="schedule-format"]', 'PDF');
      await page.selectOption('[data-testid="schedule-frequency"]', 'Monthly');
      
      // Set schedule time
      await page.fill('[data-testid="schedule-time"]', '09:00');
      
      // Set email recipients
      await page.fill('[data-testid="email-recipients"]', 'admin@example.com,manager@example.com');
      
      // Save schedule
      await page.click('[data-testid="save-schedule-btn"]');
      
      // Verify success
      await expect(page.locator('[data-testid="schedule-created-success"]')).toBeVisible();
      
      // Verify schedule appears in list
      await expect(page.locator('[data-testid="scheduled-report-1"]')).toBeVisible();
    });

    test('should edit scheduled report', async ({ page }) => {
      await loginPage.loginAsAdmin();
      await page.goto('/reports');
      await page.click('[data-testid="scheduled-reports-tab"]');
      
      // Edit first scheduled report
      await page.click('[data-testid="edit-schedule-1"]');
      
      // Update frequency
      await page.selectOption('[data-testid="schedule-frequency"]', 'Weekly');
      
      // Update recipients
      await page.fill('[data-testid="email-recipients"]', 'admin@example.com');
      
      // Save changes
      await page.click('[data-testid="save-schedule-btn"]');
      
      // Verify success
      await expect(page.locator('[data-testid="schedule-updated-success"]')).toBeVisible();
      
      // Verify changes are reflected
      await expect(page.locator('[data-testid="schedule-frequency-1"]')).toContainText('Weekly');
    });

    test('should disable/enable scheduled report', async ({ page }) => {
      await loginPage.loginAsAdmin();
      await page.goto('/reports');
      await page.click('[data-testid="scheduled-reports-tab"]');
      
      // Toggle schedule status
      await page.click('[data-testid="toggle-schedule-1"]');
      
      // Verify status changed
      await expect(page.locator('[data-testid="schedule-status-1"]')).toContainText('Disabled');
      
      // Toggle back
      await page.click('[data-testid="toggle-schedule-1"]');
      await expect(page.locator('[data-testid="schedule-status-1"]')).toContainText('Active');
    });
  });

  test.describe('Report Error Handling', () => {
    test('should handle report generation failures gracefully', async ({ page }) => {
      await loginPage.loginAsAdmin();
      await page.goto('/reports');
      
      // Simulate server error during report generation
      await page.route('**/api/reports/generate', route => {
        route.fulfill({
          status: 500,
          contentType: 'application/json',
          body: JSON.stringify({ error: 'Internal server error' })
        });
      });
      
      // Attempt to generate report
      await page.selectOption('[data-testid="report-type-select"]', 'TaxFilingReport');
      await page.click('[data-testid="generate-report-btn"]');
      
      // Verify error handling
      await expect(page.locator('[data-testid="report-generation-error"]')).toBeVisible();
      await expect(page.locator('[data-testid="error-message"]')).toContainText('Failed to generate report');
      
      // Test retry functionality
      await page.unroute('**/api/reports/generate');
      await page.click('[data-testid="retry-generation-btn"]');
      
      // Should proceed normally after retry
      await expect(page.locator('[data-testid="report-generation-progress"]')).toBeVisible();
    });

    test('should validate report parameters', async ({ page }) => {
      await loginPage.loginAsAdmin();
      await page.goto('/reports');
      
      // Try to generate report without required parameters
      await page.selectOption('[data-testid="report-type-select"]', 'TaxFilingReport');
      await page.click('[data-testid="generate-report-btn"]');
      
      // Should show validation errors
      await expect(page.locator('[data-testid="validation-error-from-date"]')).toBeVisible();
      await expect(page.locator('[data-testid="validation-error-to-date"]')).toBeVisible();
      
      // Fix validation errors
      await page.fill('[data-testid="from-date"]', '2024-01-01');
      await page.fill('[data-testid="to-date"]', '2024-12-31');
      
      // Should proceed without errors
      await page.click('[data-testid="generate-report-btn"]');
      await expect(page.locator('[data-testid="report-generation-progress"]')).toBeVisible();
    });

    test('should handle large report generation timeouts', async ({ page }) => {
      await loginPage.loginAsAdmin();
      await page.goto('/reports');
      
      // Select parameters that would generate a large report
      await page.selectOption('[data-testid="report-type-select"]', 'ComplianceReport');
      await page.fill('[data-testid="from-date"]', '2020-01-01');
      await page.fill('[data-testid="to-date"]', '2024-12-31');
      
      // Check include all clients option
      await page.check('[data-testid="include-all-clients"]');
      
      // Start generation
      await page.click('[data-testid="generate-report-btn"]');
      
      // Should show progress and allow cancellation
      await expect(page.locator('[data-testid="report-generation-progress"]')).toBeVisible();
      await expect(page.locator('[data-testid="cancel-generation-btn"]')).toBeVisible();
      
      // Test cancellation
      await page.click('[data-testid="cancel-generation-btn"]');
      await expect(page.locator('[data-testid="generation-cancelled"]')).toBeVisible();
    });
  });

  test.describe('Client Report Access', () => {
    test('should allow clients to generate their own reports', async ({ page }) => {
      await loginPage.loginAsClient();
      await page.goto('/client-portal/reports');
      
      // Verify client can only see their own data
      await expect(page.locator('[data-testid="client-reports-page"]')).toBeVisible();
      
      // Client should not see client selection dropdown
      await expect(page.locator('[data-testid="client-filter-dropdown"]')).not.toBeVisible();
      
      // Generate client-specific report
      await page.selectOption('[data-testid="report-type-select"]', 'TaxFilingReport');
      await page.fill('[data-testid="from-date"]', '2024-01-01');
      await page.fill('[data-testid="to-date"]', '2024-12-31');
      
      await page.click('[data-testid="generate-report-btn"]');
      
      // Verify generation proceeds
      await expect(page.locator('[data-testid="report-generation-progress"]')).toBeVisible();
      await expect(page.locator('[data-testid="report-completed"]')).toBeVisible({ timeout: 30000 });
    });

    test('should restrict client access to appropriate report types', async ({ page }) => {
      await loginPage.loginAsClient();
      await page.goto('/client-portal/reports');
      
      // Verify limited report type options for clients
      const reportOptions = await page.locator('[data-testid="report-type-select"] option').allTextContents();
      
      // Clients should not have access to internal reports
      expect(reportOptions).not.toContain('Internal KPI Report');
      expect(reportOptions).not.toContain('System Audit Report');
      
      // Clients should have access to their relevant reports
      expect(reportOptions).toContain('Tax Filing Report');
      expect(reportOptions).toContain('Payment Report');
      expect(reportOptions).toContain('Compliance Report');
    });
  });
});