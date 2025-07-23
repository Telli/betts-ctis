import { Page, expect } from '@playwright/test';

export class AssociatePage {
  constructor(private page: Page) {}

  // Navigation methods
  async goToDashboard() {
    await this.page.goto('/associate/dashboard');
    await this.expectDashboardLoaded();
  }

  async goToPermissions() {
    await this.page.goto('/associate/permissions');
    await this.expectPermissionsLoaded();
  }

  async goToClients() {
    await this.page.goto('/associate/clients');
    await this.expectClientsLoaded();
  }

  // Dashboard methods
  async expectDashboardLoaded() {
    await expect(this.page.locator('h1:has-text("Associate Dashboard")')).toBeVisible();
    await expect(this.page.locator('text=Welcome back')).toBeVisible();
  }

  async getDashboardSummaryCard(cardName: string) {
    return this.page.locator(`text=${cardName}`).locator('..').locator('.text-2xl');
  }

  async clickDashboardTab(tabName: string) {
    await this.page.locator(`text=${tabName}`).click();
    await this.page.waitForTimeout(500); // Wait for tab content to load
  }

  async expectQuickActionsVisible() {
    await expect(this.page.locator('text=Quick Actions')).toBeVisible();
    await expect(this.page.locator('text=View Tax Filings').or(this.page.locator('a[href="/tax-filings"]'))).toBeVisible();
    await expect(this.page.locator('text=Manage Documents').or(this.page.locator('a[href="/documents"]'))).toBeVisible();
    await expect(this.page.locator('text=Process Payments').or(this.page.locator('a[href="/payments"]'))).toBeVisible();
  }

  // Permissions methods
  async expectPermissionsLoaded() {
    await expect(this.page.locator('h1:has-text("My Permissions")')).toBeVisible();
    await expect(this.page.locator('text=Manage and view your delegated client permissions')).toBeVisible();
  }

  async getPermissionsSummaryCard(cardName: string) {
    return this.page.locator(`text=${cardName}`).locator('..').locator('.text-2xl');
  }

  async searchPermissions(searchTerm: string) {
    const searchInput = this.page.locator('input[placeholder*="Search"]');
    await searchInput.fill(searchTerm);
    await this.page.waitForTimeout(1000); // Wait for search to apply
  }

  async filterPermissionsByArea(area: string) {
    // Click the area filter dropdown
    const areaFilter = this.page.locator('text=All Areas').or(this.page.locator('[data-testid="area-filter"]'));
    await areaFilter.click();
    
    // Select the specific area
    await this.page.locator(`text=${area}`).click();
    await this.page.waitForTimeout(1000); // Wait for filter to apply
  }

  async filterPermissionsByStatus(status: string) {
    // Click the status filter dropdown
    const statusFilter = this.page.locator('text=All Status').or(this.page.locator('[data-testid="status-filter"]'));
    await statusFilter.click();
    
    // Select the specific status
    await this.page.locator(`text=${status}`).click();
    await this.page.waitForTimeout(1000); // Wait for filter to apply
  }

  async expectPermissionDetailsVisible() {
    await expect(this.page.locator('text=Permission Details')).toBeVisible();
  }

  // Clients methods
  async expectClientsLoaded() {
    await expect(this.page.locator('h1:has-text("My Delegated Clients")')).toBeVisible();
    await expect(this.page.locator('text=Clients you have permissions to manage')).toBeVisible();
  }

  async getClientsSummaryCard(cardName: string) {
    return this.page.locator(`text=${cardName}`).locator('..').locator('.text-2xl');
  }

  async searchClients(searchTerm: string) {
    const searchInput = this.page.locator('input[placeholder*="Search"]');
    await searchInput.fill(searchTerm);
    await this.page.waitForTimeout(1000); // Wait for search to apply
  }

  async filterClientsByPermissionArea(area: string) {
    // Click the permission area filter dropdown
    const areaFilter = this.page.locator('text=Tax Filings').or(this.page.locator('[data-testid="permission-area-filter"]'));
    await areaFilter.click();
    
    // Select the specific area
    await this.page.locator(`text=${area}`).click();
    await this.page.waitForTimeout(1000); // Wait for filter to apply
  }

  async filterClientsByCategory(category: string) {
    // Click the category filter dropdown
    const categoryFilter = this.page.locator('text=All Categories').or(this.page.locator('[data-testid="category-filter"]'));
    await categoryFilter.click();
    
    // Select the specific category
    await this.page.locator(`text=${category}`).click();
    await this.page.waitForTimeout(1000); // Wait for filter to apply
  }

  async clickClientViewButton(clientName: string) {
    const clientCard = this.page.locator(`text=${clientName}`).locator('..').locator('..');
    await clientCard.locator('button:has-text("View")').click();
  }

  async clickClientFilingsButton(clientName: string) {
    const clientCard = this.page.locator(`text=${clientName}`).locator('..').locator('..');
    await clientCard.locator('button:has-text("Filings")').click();
  }

  async clickClientPaymentsButton(clientName: string) {
    const clientCard = this.page.locator(`text=${clientName}`).locator('..').locator('..');
    await clientCard.locator('button:has-text("Payments")').click();
  }

  // Sidebar navigation methods
  async expectAssociateNavigationVisible() {
    await expect(this.page.locator('text=Associate').or(this.page.locator('[data-testid="associate-nav"]'))).toBeVisible();
    await expect(this.page.locator('text=Associate Dashboard').or(this.page.locator('a[href="/associate/dashboard"]'))).toBeVisible();
    await expect(this.page.locator('text=My Permissions').or(this.page.locator('a[href="/associate/permissions"]'))).toBeVisible();
    await expect(this.page.locator('text=My Clients').or(this.page.locator('a[href="/associate/clients"]'))).toBeVisible();
  }

  async clickSidebarLink(linkText: string) {
    await this.page.locator(`text=${linkText}`).click();
  }

  // Common utility methods
  async expectSummaryCardsVisible(cardNames: string[]) {
    for (const cardName of cardNames) {
      await expect(this.page.locator(`text=${cardName}`)).toBeVisible();
    }
  }

  async expectTabsVisible(tabNames: string[]) {
    for (const tabName of tabNames) {
      await expect(this.page.locator(`text=${tabName}`)).toBeVisible();
    }
  }

  async expectFiltersVisible() {
    await expect(this.page.locator('input[placeholder*="Search"]')).toBeVisible();
    await expect(this.page.locator('[role="combobox"]')).toBeVisible();
  }

  async expectNoDataMessage(message: string) {
    await expect(this.page.locator(`text=${message}`)).toBeVisible();
  }

  // Helper methods for data validation
  async expectNumericValueInCard(cardName: string) {
    const card = await this.getDashboardSummaryCard(cardName);
    const value = await card.textContent();
    expect(value).toMatch(/^\d+$/); // Should be a number
  }

  async expectLoadingComplete() {
    // Wait for any loading spinners to disappear
    await expect(this.page.locator('[data-testid="loading"]').or(this.page.locator('.animate-spin'))).not.toBeVisible({ timeout: 10000 });
  }

  async expectErrorMessageNotVisible() {
    await expect(this.page.locator('[data-testid="error"]').or(this.page.locator('text=Error'))).not.toBeVisible();
  }

  // Mobile-specific methods
  async expectMobileLayout() {
    // Check that content is responsive and visible on mobile
    await expect(this.page.locator('h1')).toBeVisible();
    
    // Check that cards stack vertically (basic responsive check)
    const firstCard = this.page.locator('.text-2xl').first();
    if (await firstCard.isVisible()) {
      const boundingBox = await firstCard.boundingBox();
      expect(boundingBox?.width).toBeLessThan(400); // Should be narrow on mobile
    }
  }

  async toggleSidebar() {
    const sidebarToggle = this.page.locator('[data-testid="sidebar-toggle"]').or(
      this.page.locator('button').filter({ hasText: /toggle|menu/i })
    );
    
    if (await sidebarToggle.isVisible()) {
      await sidebarToggle.click();
      await this.page.waitForTimeout(300); // Wait for animation
    }
  }
}