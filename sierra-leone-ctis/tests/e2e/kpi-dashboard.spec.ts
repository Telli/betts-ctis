import { test, expect } from '@playwright/test';
import { LoginPage } from '../page-objects/LoginPage';

test.describe('KPI Dashboard Integration', () => {
  let loginPage: LoginPage;

  test.beforeEach(async ({ page }) => {
    loginPage = new LoginPage(page);
    await loginPage.goto();
  });

  test.describe('Internal KPI Dashboard', () => {
    test('should display internal KPIs for admin users', async ({ page }) => {
      // Login as admin
      await loginPage.loginAsAdmin();
      
      // Navigate to dashboard
      await page.goto('/dashboard');
      
      // Wait for KPI cards to load
      await expect(page.locator('[data-testid="internal-kpi-dashboard"]')).toBeVisible();
      
      // Verify key KPI metrics are displayed
      await expect(page.locator('[data-testid="total-active-clients"]')).toBeVisible();
      await expect(page.locator('[data-testid="compliance-rate"]')).toBeVisible();
      await expect(page.locator('[data-testid="filing-timeliness"]')).toBeVisible();
      await expect(page.locator('[data-testid="payment-completion-rate"]')).toBeVisible();
      
      // Verify KPI values are numeric and reasonable
      const complianceRate = await page.locator('[data-testid="compliance-rate-value"]').textContent();
      expect(complianceRate).toMatch(/^\d+(\.\d+)?%?$/);
      
      // Test KPI trend charts
      await expect(page.locator('[data-testid="compliance-trend-chart"]')).toBeVisible();
      await expect(page.locator('[data-testid="filing-timeliness-chart"]')).toBeVisible();
      
      // Test real-time updates (simulate data change)
      const initialValue = await page.locator('[data-testid="total-active-clients-value"]').textContent();
      
      // Trigger KPI refresh
      await page.click('[data-testid="refresh-kpis-btn"]');
      await page.waitForTimeout(2000);
      
      // Verify data has been refreshed (timestamp should update)
      await expect(page.locator('[data-testid="kpi-last-updated"]')).toContainText('Just now');
    });

    test('should filter KPIs by date range', async ({ page }) => {
      await loginPage.loginAsAdmin();
      await page.goto('/dashboard');
      
      // Open date range picker
      await page.click('[data-testid="kpi-date-range-picker"]');
      
      // Select last 30 days
      await page.click('[data-testid="date-range-30-days"]');
      
      // Wait for KPIs to update
      await page.waitForResponse(/\/api\/kpi\/internal/);
      
      // Verify KPIs have updated
      await expect(page.locator('[data-testid="date-range-indicator"]')).toContainText('Last 30 days');
      
      // Test custom date range
      await page.click('[data-testid="kpi-date-range-picker"]');
      await page.click('[data-testid="date-range-custom"]');
      
      // Set custom dates
      await page.fill('[data-testid="from-date"]', '2024-01-01');
      await page.fill('[data-testid="to-date"]', '2024-12-31');
      await page.click('[data-testid="apply-date-range"]');
      
      // Verify custom range is applied
      await expect(page.locator('[data-testid="date-range-indicator"]')).toContainText('2024-01-01 to 2024-12-31');
    });

    test('should display KPI alerts and notifications', async ({ page }) => {
      await loginPage.loginAsAdmin();
      await page.goto('/dashboard');
      
      // Check for KPI alert indicators
      const alertsExist = await page.locator('[data-testid="kpi-alerts"]').count();
      
      if (alertsExist > 0) {
        // Click on alerts panel
        await page.click('[data-testid="kpi-alerts"]');
        
        // Verify alert details
        await expect(page.locator('[data-testid="alert-list"]')).toBeVisible();
        
        // Test alert severity indicators
        const highSeverityAlerts = page.locator('[data-testid^="alert-high-"]');
        const mediumSeverityAlerts = page.locator('[data-testid^="alert-medium-"]');
        
        if (await highSeverityAlerts.count() > 0) {
          await expect(highSeverityAlerts.first()).toHaveClass(/alert-high/);
        }
        
        // Test alert actions
        const firstAlert = page.locator('[data-testid^="alert-"]').first();
        if (await firstAlert.count() > 0) {
          await firstAlert.click();
          await expect(page.locator('[data-testid="alert-details-modal"]')).toBeVisible();
          
          // Test acknowledge alert
          await page.click('[data-testid="acknowledge-alert-btn"]');
          await expect(page.locator('[data-testid="alert-acknowledged"]')).toBeVisible();
        }
      }
    });
  });

  test.describe('Client KPI Dashboard', () => {
    test('should display client-specific KPIs', async ({ page }) => {
      // Login as client user
      await loginPage.loginAsClient();
      
      // Navigate to client portal dashboard
      await page.goto('/client-portal/dashboard');
      
      // Wait for client KPI dashboard to load
      await expect(page.locator('[data-testid="client-kpi-dashboard"]')).toBeVisible();
      
      // Verify client-specific metrics
      await expect(page.locator('[data-testid="client-compliance-score"]')).toBeVisible();
      await expect(page.locator('[data-testid="client-filing-status"]')).toBeVisible();
      await expect(page.locator('[data-testid="client-payment-status"]')).toBeVisible();
      await expect(page.locator('[data-testid="client-document-status"]')).toBeVisible();
      
      // Verify compliance score visualization
      const complianceScore = await page.locator('[data-testid="compliance-score-value"]').textContent();
      expect(complianceScore).toMatch(/^\d+(\.\d+)?$/);
      
      // Check compliance level indicator
      const complianceLevel = page.locator('[data-testid="compliance-level"]');
      await expect(complianceLevel).toBeVisible();
      
      const levelText = await complianceLevel.textContent();
      expect(['Green', 'Yellow', 'Red']).toContain(levelText);
    });

    test('should show upcoming deadlines and priorities', async ({ page }) => {
      await loginPage.loginAsClient();
      await page.goto('/client-portal/dashboard');
      
      // Check upcoming deadlines section
      await expect(page.locator('[data-testid="upcoming-deadlines"]')).toBeVisible();
      
      const deadlines = page.locator('[data-testid^="deadline-"]');
      const deadlineCount = await deadlines.count();
      
      if (deadlineCount > 0) {
        // Verify deadline information
        const firstDeadline = deadlines.first();
        await expect(firstDeadline.locator('[data-testid="deadline-date"]')).toBeVisible();
        await expect(firstDeadline.locator('[data-testid="deadline-type"]')).toBeVisible();
        await expect(firstDeadline.locator('[data-testid="deadline-priority"]')).toBeVisible();
        
        // Test deadline priority colors
        const priorityElement = firstDeadline.locator('[data-testid="deadline-priority"]');
        const priorityClass = await priorityElement.getAttribute('class');
        expect(priorityClass).toMatch(/(high|medium|low)/);
      }
    });

    test('should display compliance trends and history', async ({ page }) => {
      await loginPage.loginAsClient();
      await page.goto('/client-portal/dashboard');
      
      // Check compliance trend chart
      await expect(page.locator('[data-testid="client-compliance-trend"]')).toBeVisible();
      
      // Test trend period selection
      await page.click('[data-testid="trend-period-selector"]');
      await page.click('[data-testid="trend-6-months"]');
      
      // Wait for chart to update
      await page.waitForTimeout(1000);
      
      // Verify chart has data points
      const chartPoints = page.locator('[data-testid="chart-data-point"]');
      expect(await chartPoints.count()).toBeGreaterThan(0);
      
      // Test chart interactivity
      if (await chartPoints.count() > 0) {
        await chartPoints.first().hover();
        await expect(page.locator('[data-testid="chart-tooltip"]')).toBeVisible();
      }
    });
  });

  test.describe('KPI Performance and Responsiveness', () => {
    test('should load KPIs within acceptable time limits', async ({ page }) => {
      await loginPage.loginAsAdmin();
      
      const startTime = Date.now();
      await page.goto('/dashboard');
      
      // Wait for all KPI cards to be visible
      await expect(page.locator('[data-testid="internal-kpi-dashboard"]')).toBeVisible();
      await expect(page.locator('[data-testid="compliance-rate"]')).toBeVisible();
      await expect(page.locator('[data-testid="filing-timeliness"]')).toBeVisible();
      
      const loadTime = Date.now() - startTime;
      
      // KPI dashboard should load within 3 seconds
      expect(loadTime).toBeLessThan(3000);
    });

    test('should handle KPI refresh gracefully', async ({ page }) => {
      await loginPage.loginAsAdmin();
      await page.goto('/dashboard');
      
      // Wait for initial load
      await expect(page.locator('[data-testid="internal-kpi-dashboard"]')).toBeVisible();
      
      // Test rapid refresh clicks
      for (let i = 0; i < 3; i++) {
        await page.click('[data-testid="refresh-kpis-btn"]');
        await page.waitForTimeout(100);
      }
      
      // Verify no errors and data still displays
      await expect(page.locator('[data-testid="compliance-rate"]')).toBeVisible();
      await expect(page.locator('.error')).toHaveCount(0);
    });

    test('should handle network failures gracefully', async ({ page }) => {
      await loginPage.loginAsAdmin();
      await page.goto('/dashboard');
      
      // Initial load
      await expect(page.locator('[data-testid="internal-kpi-dashboard"]')).toBeVisible();
      
      // Simulate network failure
      await page.route('**/api/kpi/**', route => route.abort());
      
      // Try to refresh KPIs
      await page.click('[data-testid="refresh-kpis-btn"]');
      
      // Should show error state gracefully
      await expect(page.locator('[data-testid="kpi-error-state"]')).toBeVisible({ timeout: 5000 });
      await expect(page.locator('[data-testid="retry-kpis-btn"]')).toBeVisible();
      
      // Test retry functionality
      await page.unroute('**/api/kpi/**');
      await page.click('[data-testid="retry-kpis-btn"]');
      
      // Should recover and show data
      await expect(page.locator('[data-testid="compliance-rate"]')).toBeVisible({ timeout: 5000 });
    });
  });

  test.describe('KPI Data Accuracy', () => {
    test('should display consistent data across different views', async ({ page }) => {
      await loginPage.loginAsAdmin();
      await page.goto('/dashboard');
      
      // Get compliance rate from main dashboard
      const dashboardComplianceRate = await page.locator('[data-testid="compliance-rate-value"]').textContent();
      
      // Navigate to detailed compliance view
      await page.click('[data-testid="view-compliance-details"]');
      await expect(page.locator('[data-testid="detailed-compliance-rate"]')).toBeVisible();
      
      // Verify the same value is displayed
      const detailedComplianceRate = await page.locator('[data-testid="detailed-compliance-rate"]').textContent();
      expect(dashboardComplianceRate).toBe(detailedComplianceRate);
    });

    test('should update KPIs when underlying data changes', async ({ page }) => {
      await loginPage.loginAsAdmin();
      await page.goto('/dashboard');
      
      // Record initial client count
      const initialClientCount = await page.locator('[data-testid="total-active-clients-value"]').textContent();
      
      // Navigate to clients page and add a new client (simulate)
      await page.goto('/clients/new');
      await page.fill('[data-testid="company-name"]', 'Test KPI Integration Company');
      await page.fill('[data-testid="email"]', 'kpitest@example.com');
      await page.selectOption('[data-testid="taxpayer-category"]', 'Medium');
      await page.click('[data-testid="submit-client"]');
      
      // Wait for success confirmation
      await expect(page.locator('[data-testid="success-message"]')).toBeVisible();
      
      // Go back to dashboard
      await page.goto('/dashboard');
      
      // Wait for KPI refresh
      await page.waitForTimeout(2000);
      
      // Verify client count has increased
      const updatedClientCount = await page.locator('[data-testid="total-active-clients-value"]').textContent();
      expect(parseInt(updatedClientCount!)).toBeGreaterThan(parseInt(initialClientCount!));
    });
  });
});