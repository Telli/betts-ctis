import { test, expect } from '@playwright/test';
import { ClientPortalPage } from '../page-objects/ClientPortalPage';
import { AuthHelper } from '../utils/auth-helper';
import { TEST_CLIENT_DATA, TEST_DOCUMENT } from '../utils/test-data';
import path from 'path';

test.describe('Client Portal', () => {
  let clientPortalPage: ClientPortalPage;
  let authHelper: AuthHelper;

  test.beforeEach(async ({ page }) => {
    clientPortalPage = new ClientPortalPage(page);
    authHelper = new AuthHelper(page);
    
    // Login as client before each test
    await authHelper.login('client');
  });

  test.describe('Dashboard', () => {
    test('should display dashboard components', async ({ page }) => {
      await clientPortalPage.gotoDashboard();
      await clientPortalPage.expectDashboardVisible();
      
      // Check for key dashboard elements
      await expect(page.locator('text=Welcome')).toBeVisible();
      await expect(page.locator('text=Compliance Score')).toBeVisible();
      await expect(page.locator('text=Recent Activity')).toBeVisible();
    });

    test('should show compliance score', async ({ page }) => {
      await clientPortalPage.gotoDashboard();
      
      const complianceScore = await clientPortalPage.getComplianceScore();
      expect(complianceScore).toMatch(/\d+/); // Should contain numbers
    });

    test('should navigate via quick actions', async ({ page }) => {
      await clientPortalPage.gotoDashboard();
      
      // Test quick action navigation
      await clientPortalPage.clickQuickAction('Upload Document');
      await clientPortalPage.expectCurrentPage('My Documents');
    });
  });

  test.describe('Navigation', () => {
    test('should display client sidebar', async ({ page }) => {
      await clientPortalPage.gotoDashboard();
      await clientPortalPage.expectSidebarVisible();
    });

    test('should navigate between pages', async ({ page }) => {
      const pages = [
        'My Documents',
        'Profile', 
        'Tax Filings',
        'Payment History',
        'Compliance Status',
        'Deadlines'
      ];

      for (const pageName of pages) {
        await clientPortalPage.clickSidebarItem(pageName);
        await clientPortalPage.expectCurrentPage(pageName);
      }
    });
  });

  test.describe('Document Management', () => {
    test('should access documents page', async ({ page }) => {
      await clientPortalPage.gotoDocuments();
      
      await expect(page.locator('text=My Documents')).toBeVisible();
      await expect(page.locator('text=Upload Document')).toBeVisible();
    });

    test('should search documents', async ({ page }) => {
      await clientPortalPage.gotoDocuments();
      
      // Test document search functionality
      await clientPortalPage.searchDocuments('tax return');
      
      // Wait for search results to load
      await page.waitForTimeout(2000);
    });

    test.skip('should upload document', async ({ page }) => {
      // Skip this test as it requires actual file handling
      // In a real implementation, you would:
      // 1. Create a test file
      // 2. Upload it using clientPortalPage.uploadDocument()
      // 3. Verify it appears in the document list
    });

    test.skip('should download document', async ({ page }) => {
      // Skip this test as it requires existing documents
      // In a real implementation, you would:
      // 1. Ensure a test document exists
      // 2. Download it using clientPortalPage.downloadDocument()
      // 3. Verify the download completed
    });
  });

  test.describe('Profile Management', () => {
    test('should display profile form', async ({ page }) => {
      await clientPortalPage.gotoProfile();
      
      await expect(page.locator('text=Business Profile')).toBeVisible();
      await expect(page.locator('input[name="businessName"]')).toBeVisible();
      await expect(page.locator('input[name="email"]')).toBeVisible();
    });

    test('should update profile information', async ({ page }) => {
      await clientPortalPage.gotoProfile();
      
      const updatedData = {
        businessName: 'Updated Test Company Ltd',
        contactPerson: 'Jane Smith',
        phoneNumber: '+232 76 987 654'
      };
      
      await clientPortalPage.updateProfile(updatedData);
      
      // Verify the update was successful
      await expect(page.locator('text=Profile updated successfully')).toBeVisible();
    });

    test('should validate profile form', async ({ page }) => {
      await clientPortalPage.gotoProfile();
      
      // Clear required field and try to save
      await page.fill('input[name="businessName"]', '');
      await page.click('[data-testid="save-profile"]');
      
      // Should show validation error
      await expect(
        page.locator('text=Business name is required').or(
          page.locator('text=Please enter')
        )
      ).toBeVisible();
    });
  });

  test.describe('Tax Filings', () => {
    test('should display tax filings page', async ({ page }) => {
      await clientPortalPage.gotoTaxFilings();
      await clientPortalPage.expectTaxFilings();
      
      await expect(page.locator('text=Tax Year')).toBeVisible();
      await expect(page.locator('text=Status')).toBeVisible();
    });
  });

  test.describe('Payment History', () => {
    test('should display payment history', async ({ page }) => {
      await clientPortalPage.gotoPayments();
      await clientPortalPage.expectPaymentHistory();
      
      await expect(page.locator('text=Amount')).toBeVisible();
      await expect(page.locator('text=Date')).toBeVisible();
    });
  });

  test.describe('Compliance Status', () => {
    test('should display compliance information', async ({ page }) => {
      await clientPortalPage.gotoCompliance();
      await clientPortalPage.expectComplianceData();
      
      await expect(page.locator('text=Tax Obligations')).toBeVisible();
    });
  });

  test.describe('Data Isolation', () => {
    test('should only show client-specific data', async ({ page }) => {
      await clientPortalPage.gotoDashboard();
      
      // Verify client can only see their own data
      // This would require seeded test data to verify properly
      await expect(page.locator('text=Test Company')).toBeVisible();
      
      // Should not see other client data
      await expect(page.locator('text=Other Company')).not.toBeVisible();
    });
  });

  test.describe('Responsive Design', () => {
    test('should work on mobile viewport', async ({ page }) => {
      // Set mobile viewport
      await page.setViewportSize({ width: 375, height: 667 });
      
      await clientPortalPage.gotoDashboard();
      
      // Check mobile navigation (hamburger menu)
      const mobileMenuButton = page.locator('[data-testid="mobile-menu-button"]');
      if (await mobileMenuButton.isVisible()) {
        await mobileMenuButton.click();
        await clientPortalPage.expectSidebarVisible();
      }
    });

    test('should work on tablet viewport', async ({ page }) => {
      // Set tablet viewport
      await page.setViewportSize({ width: 768, height: 1024 });
      
      await clientPortalPage.gotoDashboard();
      await clientPortalPage.expectDashboardVisible();
    });
  });
});