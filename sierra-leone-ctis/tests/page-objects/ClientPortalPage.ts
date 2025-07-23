import { Page, expect } from '@playwright/test';
import { SELECTORS, TIMEOUTS, ROUTES } from '../utils/test-data';

export class ClientPortalPage {
  constructor(private page: Page) {}

  // Dashboard methods
  async gotoDashboard() {
    await this.page.goto(ROUTES.clientPortal.dashboard);
    await this.page.waitForSelector(SELECTORS.dashboardTitle, { timeout: TIMEOUTS.medium });
  }

  async expectDashboardVisible() {
    await expect(this.page.locator(SELECTORS.dashboardTitle)).toBeVisible();
    await expect(this.page.locator(SELECTORS.complianceScore)).toBeVisible();
    await expect(this.page.locator(SELECTORS.quickActions)).toBeVisible();
  }

  async getComplianceScore(): Promise<string> {
    return await this.page.locator(SELECTORS.complianceScore).textContent() || '0';
  }

  async clickQuickAction(actionName: string) {
    await this.page.click(`[data-testid="quick-actions"] >> text=${actionName}`);
  }

  // Documents methods
  async gotoDocuments() {
    await this.page.goto(ROUTES.clientPortal.documents);
    await this.page.waitForLoadState('networkidle');
  }

  async uploadDocument(filePath: string, documentType: string, description: string) {
    // Click upload button
    await this.page.click(SELECTORS.uploadButton);
    
    // Fill upload form
    await this.page.setInputFiles(SELECTORS.fileInput, filePath);
    await this.page.selectOption('select[name="documentType"]', documentType);
    await this.page.fill('textarea[name="description"]', description);
    
    // Submit form
    await this.page.click('button:has-text("Upload Document")');
    
    // Wait for upload to complete
    await this.page.waitForSelector('text=Document uploaded successfully', { timeout: TIMEOUTS.upload });
  }

  async downloadDocument(documentName: string) {
    // Find document in list and click download
    const documentRow = this.page.locator(`text=${documentName}`).locator('..').locator('..');
    await documentRow.locator(SELECTORS.downloadButton).click();
    
    // Wait for download to start
    await this.page.waitForTimeout(2000);
  }

  async expectDocumentInList(documentName: string) {
    await expect(this.page.locator(`text=${documentName}`)).toBeVisible();
  }

  async searchDocuments(searchTerm: string) {
    await this.page.fill('input[placeholder*="Search documents"]', searchTerm);
    await this.page.waitForTimeout(1000); // Wait for search to filter
  }

  // Profile methods
  async gotoProfile() {
    await this.page.goto(ROUTES.clientPortal.profile);
    await this.page.waitForSelector(SELECTORS.profileForm, { timeout: TIMEOUTS.medium });
  }

  async updateProfile(data: {
    businessName?: string;
    contactPerson?: string;
    email?: string;
    phoneNumber?: string;
    address?: string;
  }) {
    if (data.businessName) {
      await this.page.fill(SELECTORS.businessNameInput, data.businessName);
    }
    if (data.contactPerson) {
      await this.page.fill('input[name="contactPerson"]', data.contactPerson);
    }
    if (data.email) {
      await this.page.fill('input[name="email"]', data.email);
    }
    if (data.phoneNumber) {
      await this.page.fill('input[name="phoneNumber"]', data.phoneNumber);
    }
    if (data.address) {
      await this.page.fill('textarea[name="address"]', data.address);
    }
    
    // Save changes
    await this.page.click(SELECTORS.saveButton);
    
    // Wait for success message
    await this.page.waitForSelector('text=Profile updated successfully', { timeout: TIMEOUTS.medium });
  }

  async expectProfileData(data: {
    businessName?: string;
    contactPerson?: string;
    email?: string;
  }) {
    if (data.businessName) {
      await expect(this.page.locator(SELECTORS.businessNameInput)).toHaveValue(data.businessName);
    }
    if (data.contactPerson) {
      await expect(this.page.locator('input[name="contactPerson"]')).toHaveValue(data.contactPerson);
    }
    if (data.email) {
      await expect(this.page.locator('input[name="email"]')).toHaveValue(data.email);
    }
  }

  // Navigation methods
  async clickSidebarItem(itemName: string) {
    await this.page.click(`[data-testid="client-sidebar"] >> text=${itemName}`);
    await this.page.waitForLoadState('networkidle');
  }

  async expectSidebarVisible() {
    await expect(this.page.locator(SELECTORS.clientSidebar)).toBeVisible();
  }

  async expectCurrentPage(pageName: string) {
    const pageMap: Record<string, string> = {
      'Dashboard': ROUTES.clientPortal.dashboard,
      'My Documents': ROUTES.clientPortal.documents,
      'Profile': ROUTES.clientPortal.profile,
      'Tax Filings': ROUTES.clientPortal.taxFilings,
      'Payment History': ROUTES.clientPortal.payments,
      'Compliance Status': ROUTES.clientPortal.compliance,
      'Deadlines': ROUTES.clientPortal.deadlines
    };
    
    const expectedUrl = pageMap[pageName];
    if (expectedUrl) {
      await expect(this.page).toHaveURL(expectedUrl);
    }
  }

  // Compliance methods
  async gotoCompliance() {
    await this.page.goto(ROUTES.clientPortal.compliance);
    await this.page.waitForLoadState('networkidle');
  }

  async expectComplianceData() {
    await expect(this.page.locator('text=Tax Compliance Overview')).toBeVisible();
    await expect(this.page.locator('text=Compliance Score')).toBeVisible();
  }

  // Payments methods
  async gotoPayments() {
    await this.page.goto(ROUTES.clientPortal.payments);
    await this.page.waitForLoadState('networkidle');
  }

  async expectPaymentHistory() {
    await expect(this.page.locator('text=Payment History')).toBeVisible();
  }

  // Tax filings methods
  async gotoTaxFilings() {
    await this.page.goto(ROUTES.clientPortal.taxFilings);
    await this.page.waitForLoadState('networkidle');
  }

  async expectTaxFilings() {
    await expect(this.page.locator('text=Tax Filings')).toBeVisible();
  }
}