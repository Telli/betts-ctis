import { test, expect } from '@playwright/test';
import { LoginPage } from '../page-objects/LoginPage';

test.describe('Performance and Load Testing', () => {
  test.describe('Page Load Performance', () => {
    test('dashboard should load within performance benchmarks', async ({ page }) => {
      const loginPage = new LoginPage(page);
      await loginPage.goto();
      await loginPage.loginAsAdmin();
      
      // Measure dashboard load time
      const startTime = Date.now();
      await page.goto('/dashboard');
      
      // Wait for all critical elements to load
      await expect(page.locator('[data-testid="internal-kpi-dashboard"]')).toBeVisible();
      await expect(page.locator('[data-testid="compliance-rate"]')).toBeVisible();
      await expect(page.locator('[data-testid="filing-timeliness"]')).toBeVisible();
      await expect(page.locator('[data-testid="payment-completion-rate"]')).toBeVisible();
      
      const loadTime = Date.now() - startTime;
      
      // Dashboard should load within 2 seconds
      expect(loadTime).toBeLessThan(2000);
      console.log(`Dashboard load time: ${loadTime}ms`);
      
      // Measure KPI chart rendering time
      const chartStartTime = Date.now();
      await expect(page.locator('[data-testid="compliance-trend-chart"]')).toBeVisible();
      const chartLoadTime = Date.now() - chartStartTime;
      
      // Charts should render within 1 second
      expect(chartLoadTime).toBeLessThan(1000);
      console.log(`Chart rendering time: ${chartLoadTime}ms`);
    });

    test('client portal should be responsive', async ({ page }) => {
      const loginPage = new LoginPage(page);
      await loginPage.goto();
      await loginPage.loginAsClient();
      
      const startTime = Date.now();
      await page.goto('/client-portal/dashboard');
      
      // Wait for client dashboard elements
      await expect(page.locator('[data-testid="client-kpi-dashboard"]')).toBeVisible();
      await expect(page.locator('[data-testid="client-compliance-score"]')).toBeVisible();
      await expect(page.locator('[data-testid="upcoming-deadlines"]')).toBeVisible();
      
      const loadTime = Date.now() - startTime;
      
      // Client portal should load within 1.5 seconds
      expect(loadTime).toBeLessThan(1500);
      console.log(`Client portal load time: ${loadTime}ms`);
    });

    test('reports page should handle large data sets efficiently', async ({ page }) => {
      const loginPage = new LoginPage(page);
      await loginPage.goto();
      await loginPage.loginAsAdmin();
      
      const startTime = Date.now();
      await page.goto('/reports');
      
      await expect(page.locator('[data-testid="reports-page"]')).toBeVisible();
      const initialLoadTime = Date.now() - startTime;
      
      // Reports page should load within 1 second
      expect(initialLoadTime).toBeLessThan(1000);
      
      // Test report history loading with pagination
      await page.click('[data-testid="report-history-tab"]');
      
      const historyStartTime = Date.now();
      await expect(page.locator('[data-testid="report-history-table"]')).toBeVisible();
      const historyLoadTime = Date.now() - historyStartTime;
      
      // History should load within 2 seconds even with large datasets
      expect(historyLoadTime).toBeLessThan(2000);
      console.log(`Report history load time: ${historyLoadTime}ms`);
    });
  });

  test.describe('API Response Performance', () => {
    test('KPI API should respond quickly under load', async ({ page }) => {
      const loginPage = new LoginPage(page);
      await loginPage.goto();
      await loginPage.loginAsAdmin();
      await page.goto('/dashboard');
      
      // Monitor API response times
      const apiTimes: number[] = [];
      
      page.on('response', response => {
        if (response.url().includes('/api/kpi/internal')) {
          const timing = response.timing();
          if (timing.responseEnd !== -1) {
            apiTimes.push(timing.responseEnd);
          }
        }
      });
      
      // Trigger multiple KPI refreshes
      for (let i = 0; i < 5; i++) {
        await page.click('[data-testid="refresh-kpis-btn"]');
        await page.waitForTimeout(1000);
      }
      
      // All API calls should complete within 500ms
      apiTimes.forEach(time => {
        expect(time).toBeLessThan(500);
      });
      
      const avgResponseTime = apiTimes.reduce((sum, time) => sum + time, 0) / apiTimes.length;
      console.log(`Average KPI API response time: ${avgResponseTime}ms`);
      
      // Average should be under 300ms
      expect(avgResponseTime).toBeLessThan(300);
    });

    test('report generation API should handle concurrent requests', async ({ browser }) => {
      // Create multiple browser contexts for concurrent testing
      const contexts = await Promise.all(
        Array(3).fill(0).map(() => browser.newContext())
      );
      
      const pages = await Promise.all(
        contexts.map(context => context.newPage())
      );
      
      // Set up login for each page
      const logins = pages.map(page => new LoginPage(page));
      
      await Promise.all(logins.map(async (login, index) => {
        await login.goto();
        await login.loginAsAdmin();
      }));
      
      // Track API response times
      const responseTimes: number[] = [];
      
      pages.forEach((page, index) => {
        page.on('response', response => {
          if (response.url().includes('/api/reports/generate')) {
            const timing = response.timing();
            if (timing.responseEnd !== -1) {
              responseTimes.push(timing.responseEnd);
              console.log(`Report API ${index + 1} response time: ${timing.responseEnd}ms`);
            }
          }
        });
      });
      
      // Generate reports concurrently
      const reportOperations = pages.map(async (page, index) => {
        await page.goto('/reports');
        await page.selectOption('[data-testid="report-type-select"]', 'TaxFilingReport');
        await page.fill('[data-testid="from-date"]', '2024-01-01');
        await page.fill('[data-testid="to-date"]', '2024-12-31');
        await page.click('[data-testid="generate-report-btn"]');
        
        // Wait for report to start processing
        await expect(page.locator('[data-testid="report-generation-progress"]')).toBeVisible();
      });
      
      await Promise.all(reportOperations);
      
      // API should handle concurrent requests within reasonable time
      responseTimes.forEach(time => {
        expect(time).toBeLessThan(1000); // Under 1 second for request acknowledgment
      });
      
      // Clean up
      await Promise.all(contexts.map(context => context.close()));
    });
  });

  test.describe('Memory and Resource Usage', () => {
    test('should not have memory leaks during extended usage', async ({ page }) => {
      const loginPage = new LoginPage(page);
      await loginPage.goto();
      await loginPage.loginAsAdmin();
      
      // Initial memory baseline
      const initialMemory = await page.evaluate(() => {
        return (performance as any).memory?.usedJSHeapSize || 0;
      });
      
      // Perform intensive operations
      for (let i = 0; i < 10; i++) {
        // Navigate between pages
        await page.goto('/dashboard');
        await page.waitForTimeout(500);
        
        await page.goto('/clients');
        await page.waitForTimeout(500);
        
        await page.goto('/reports');
        await page.waitForTimeout(500);
        
        // Refresh KPIs
        await page.goto('/dashboard');
        await page.click('[data-testid="refresh-kpis-btn"]');
        await page.waitForTimeout(1000);
      }
      
      // Force garbage collection if available
      await page.evaluate(() => {
        if ((window as any).gc) {
          (window as any).gc();
        }
      });
      
      await page.waitForTimeout(2000);
      
      // Check final memory usage
      const finalMemory = await page.evaluate(() => {
        return (performance as any).memory?.usedJSHeapSize || 0;
      });
      
      if (initialMemory > 0 && finalMemory > 0) {
        const memoryIncrease = finalMemory - initialMemory;
        const increasePercentage = (memoryIncrease / initialMemory) * 100;
        
        console.log(`Initial memory: ${initialMemory} bytes`);
        console.log(`Final memory: ${finalMemory} bytes`);
        console.log(`Memory increase: ${increasePercentage.toFixed(2)}%`);
        
        // Memory increase should be reasonable (less than 50%)
        expect(increasePercentage).toBeLessThan(50);
      }
    });

    test('should handle large datasets without performance degradation', async ({ page }) => {
      const loginPage = new LoginPage(page);
      await loginPage.goto();
      await loginPage.loginAsAdmin();
      
      // Navigate to clients page which may have large datasets
      const startTime = Date.now();
      await page.goto('/clients');
      
      await expect(page.locator('[data-testid="clients-table"]')).toBeVisible();
      const initialLoadTime = Date.now() - startTime;
      
      // Test pagination performance
      const paginationStartTime = Date.now();
      await page.click('[data-testid="next-page-btn"]');
      await expect(page.locator('[data-testid="clients-table"]')).toBeVisible();
      const paginationTime = Date.now() - paginationStartTime;
      
      // Pagination should be fast
      expect(paginationTime).toBeLessThan(1000);
      
      // Test search/filter performance
      const searchStartTime = Date.now();
      await page.fill('[data-testid="client-search"]', 'Test Company');
      await page.waitForTimeout(500); // Debounce time
      
      await expect(page.locator('[data-testid="search-results"]')).toBeVisible();
      const searchTime = Date.now() - searchStartTime;
      
      // Search should be responsive
      expect(searchTime).toBeLessThan(1500);
      
      console.log(`Large dataset handling - Load: ${initialLoadTime}ms, Pagination: ${paginationTime}ms, Search: ${searchTime}ms`);
    });
  });

  test.describe('Network Performance', () => {
    test('should handle slow network conditions gracefully', async ({ page }) => {
      const loginPage = new LoginPage(page);
      await loginPage.goto();
      await loginPage.loginAsAdmin();
      
      // Simulate slow network (2G conditions)
      const client = await page.context().newCDPSession(page);
      await client.send('Network.emulateNetworkConditions', {
        offline: false,
        downloadThroughput: 250 * 1024 / 8, // 250 kbps
        uploadThroughput: 250 * 1024 / 8,
        latency: 300 // 300ms latency
      });
      
      const startTime = Date.now();
      await page.goto('/dashboard');
      
      // Should show loading states appropriately
      await expect(page.locator('[data-testid="loading-skeleton"]')).toBeVisible();
      
      // Eventually load even on slow network
      await expect(page.locator('[data-testid="internal-kpi-dashboard"]')).toBeVisible({ timeout: 15000 });
      
      const loadTime = Date.now() - startTime;
      console.log(`Slow network load time: ${loadTime}ms`);
      
      // Should complete within reasonable time even on slow network
      expect(loadTime).toBeLessThan(15000);
      
      // Reset network conditions
      await client.send('Network.emulateNetworkConditions', {
        offline: false,
        downloadThroughput: -1,
        uploadThroughput: -1,
        latency: 0
      });
    });

    test('should cache resources efficiently', async ({ page }) => {
      const loginPage = new LoginPage(page);
      await loginPage.goto();
      
      // Track network requests
      const networkRequests: string[] = [];
      page.on('request', request => {
        networkRequests.push(request.url());
      });
      
      // First load
      await loginPage.loginAsAdmin();
      await page.goto('/dashboard');
      await expect(page.locator('[data-testid="internal-kpi-dashboard"]')).toBeVisible();
      
      const firstLoadRequests = networkRequests.length;
      networkRequests.length = 0; // Reset
      
      // Second load (should use cache)
      await page.reload();
      await expect(page.locator('[data-testid="internal-kpi-dashboard"]')).toBeVisible();
      
      const secondLoadRequests = networkRequests.length;
      
      console.log(`First load requests: ${firstLoadRequests}, Second load requests: ${secondLoadRequests}`);
      
      // Second load should make fewer requests due to caching
      expect(secondLoadRequests).toBeLessThan(firstLoadRequests * 0.8);
    });
  });

  test.describe('Concurrent User Load', () => {
    test('should handle multiple concurrent users', async ({ browser }) => {
      const numUsers = 5;
      const contexts = await Promise.all(
        Array(numUsers).fill(0).map(() => browser.newContext())
      );
      
      const pages = await Promise.all(
        contexts.map(context => context.newPage())
      );
      
      // Track performance metrics for each user
      const userMetrics: Array<{ userId: number; loadTime: number; errors: number }> = [];
      
      // Simulate concurrent user sessions
      const userSessions = pages.map(async (page, index) => {
        const startTime = Date.now();
        let errors = 0;
        
        try {
          const loginPage = new LoginPage(page);
          await loginPage.goto();
          
          // Vary user types
          if (index % 3 === 0) {
            await loginPage.loginAsAdmin();
            await page.goto('/dashboard');
            await expect(page.locator('[data-testid="internal-kpi-dashboard"]')).toBeVisible();
          } else if (index % 3 === 1) {
            await loginPage.loginAsAssociate();
            await page.goto('/associate/clients');
            await expect(page.locator('[data-testid="clients-list"]')).toBeVisible();
          } else {
            await loginPage.loginAsClient();
            await page.goto('/client-portal/dashboard');
            await expect(page.locator('[data-testid="client-kpi-dashboard"]')).toBeVisible();
          }
          
          // Perform user-specific actions
          for (let i = 0; i < 3; i++) {
            if (index % 3 === 0) {
              // Admin actions
              await page.click('[data-testid="refresh-kpis-btn"]');
              await page.waitForTimeout(500);
            } else if (index % 3 === 1) {
              // Associate actions
              await page.goto('/associate/dashboard');
              await page.waitForTimeout(500);
            } else {
              // Client actions
              await page.goto('/client-portal/documents');
              await page.waitForTimeout(500);
            }
          }
        } catch (error) {
          errors++;
          console.error(`User ${index} encountered error:`, error);
        }
        
        const loadTime = Date.now() - startTime;
        userMetrics.push({ userId: index, loadTime, errors });
      });
      
      await Promise.allSettled(userSessions);
      
      // Analyze performance metrics
      const avgLoadTime = userMetrics.reduce((sum, metric) => sum + metric.loadTime, 0) / userMetrics.length;
      const totalErrors = userMetrics.reduce((sum, metric) => sum + metric.errors, 0);
      const maxLoadTime = Math.max(...userMetrics.map(metric => metric.loadTime));
      
      console.log(`Concurrent users: ${numUsers}`);
      console.log(`Average load time: ${avgLoadTime}ms`);
      console.log(`Max load time: ${maxLoadTime}ms`);
      console.log(`Total errors: ${totalErrors}`);
      
      // Performance benchmarks for concurrent users
      expect(avgLoadTime).toBeLessThan(5000); // Average under 5 seconds
      expect(maxLoadTime).toBeLessThan(10000); // No user waits more than 10 seconds
      expect(totalErrors).toBeLessThan(numUsers * 0.1); // Less than 10% error rate
      
      // Clean up
      await Promise.all(contexts.map(context => context.close()));
    });
  });

  test.describe('Database Performance', () => {
    test('should handle complex queries efficiently', async ({ page }) => {
      const loginPage = new LoginPage(page);
      await loginPage.goto();
      await loginPage.loginAsAdmin();
      
      // Test complex reporting query performance
      await page.goto('/reports');
      
      // Track API response times for complex queries
      const queryTimes: number[] = [];
      
      page.on('response', response => {
        if (response.url().includes('/api/reports/data')) {
          const timing = response.timing();
          if (timing.responseEnd !== -1) {
            queryTimes.push(timing.responseEnd);
          }
        }
      });
      
      // Generate complex report with large date range
      await page.selectOption('[data-testid="report-type-select"]', 'ComplianceReport');
      await page.fill('[data-testid="from-date"]', '2020-01-01');
      await page.fill('[data-testid="to-date"]', '2024-12-31');
      await page.check('[data-testid="include-all-clients"]');
      await page.check('[data-testid="include-charts"]');
      
      const startTime = Date.now();
      await page.click('[data-testid="generate-report-btn"]');
      
      // Should start processing within reasonable time
      await expect(page.locator('[data-testid="report-generation-progress"]')).toBeVisible({ timeout: 5000 });
      
      const responseTime = Date.now() - startTime;
      console.log(`Complex query response time: ${responseTime}ms`);
      
      // Complex queries should start within 5 seconds
      expect(responseTime).toBeLessThan(5000);
    });
  });
});