import { Page, expect } from '@playwright/test';

export class AdminAssociatePage {
  constructor(private page: Page) {}

  // Navigation methods
  async goto() {
    await this.page.goto('/admin/associates');
    await this.expectPageLoaded();
  }

  async expectPageLoaded() {
    await expect(this.page.locator('h1:has-text("Associate Management")')).toBeVisible();
  }

  // Overview tab methods
  async expectOverviewTabVisible() {
    await expect(this.page.locator('text=Overview')).toBeVisible();
    await expect(this.page.locator('text=Associates')).toBeVisible();
    await expect(this.page.locator('text=Permissions')).toBeVisible();
  }

  async clickTab(tabName: string) {
    await this.page.locator(`text=${tabName}`).click();
    await this.page.waitForTimeout(500); // Wait for tab content to load
  }

  async expectSummaryCards() {
    await expect(this.page.locator('text=Total Associates')).toBeVisible();
    await expect(this.page.locator('text=Active Permissions')).toBeVisible();
    await expect(this.page.locator('text=Expiring Soon')).toBeVisible();
    await expect(this.page.locator('text=Expired')).toBeVisible();
  }

  async getSummaryCardValue(cardName: string) {
    const card = this.page.locator(`text=${cardName}`).locator('..').locator('.text-2xl');
    return await card.textContent();
  }

  // Grant Permission Dialog methods
  async openGrantPermissionDialog() {
    await this.page.locator('button:has-text("Grant Permission")').click();
    await this.expectGrantDialogOpen();
  }

  async expectGrantDialogOpen() {
    await expect(this.page.locator('text=Grant Associate Permission')).toBeVisible();
    await expect(this.page.locator('text=Grant permission to an associate')).toBeVisible();
  }

  async expectGrantDialogFormFields() {
    await expect(this.page.locator('text=Associate')).toBeVisible();
    await expect(this.page.locator('text=Client ID')).toBeVisible();
    await expect(this.page.locator('text=Permission Area')).toBeVisible();
    await expect(this.page.locator('text=Permission Level')).toBeVisible();
    await expect(this.page.locator('text=Reason')).toBeVisible();
  }

  async fillGrantPermissionForm(data: {
    associateId?: string;
    clientId?: number;
    permissionArea?: string;
    permissionLevel?: string;
    reason?: string;
  }) {
    if (data.associateId) {
      // Select associate from dropdown
      await this.page.locator('text=Select associate').click();
      await this.page.locator(`text=${data.associateId}`).click();
    }

    if (data.clientId) {
      await this.page.locator('input[name="clientId"]').or(this.page.locator('input[type="number"]')).fill(data.clientId.toString());
    }

    if (data.permissionArea) {
      await this.page.locator('text=Permission Area').locator('..').locator('[role="combobox"]').click();
      await this.page.locator(`text=${data.permissionArea}`).click();
    }

    if (data.permissionLevel) {
      await this.page.locator('text=Permission Level').locator('..').locator('[role="combobox"]').click();
      await this.page.locator(`text=${data.permissionLevel}`).click();
    }

    if (data.reason) {
      await this.page.locator('input[name="reason"]').or(this.page.locator('input[placeholder*="reason"]')).fill(data.reason);
    }
  }

  async submitGrantPermissionForm() {
    await this.page.locator('button:has-text("Grant Permission")').last().click();
  }

  async expectValidationError() {
    await expect(this.page.locator('text=required').or(this.page.locator('text=Associate is required'))).toBeVisible();
  }

  async expectSuccessMessage() {
    await expect(this.page.locator('text=Permission granted successfully').or(this.page.locator('text=Success'))).toBeVisible();
  }

  async closeGrantDialog() {
    await this.page.locator('button:has-text("Cancel")').click();
  }

  // Associates tab methods
  async goToAssociatesTab() {
    await this.clickTab('Associates');
    await this.expectAssociatesTabContent();
  }

  async expectAssociatesTabContent() {
    // Associates tab might show a list or cards of associates
    // The exact content depends on your implementation
    await this.page.waitForTimeout(1000);
  }

  async expectAssociateCard(associateName: string) {
    await expect(this.page.locator(`text=${associateName}`)).toBeVisible();
  }

  async clickAssociateViewButton(associateName: string) {
    const associateCard = this.page.locator(`text=${associateName}`).locator('..').locator('..');
    await associateCard.locator('button:has-text("View")').click();
  }

  // Permissions tab methods
  async goToPermissionsTab() {
    await this.clickTab('Permissions');
    await this.expectPermissionsTabContent();
  }

  async expectPermissionsTabContent() {
    await expect(this.page.locator('text=Associate Permissions')).toBeVisible();
  }

  async expectPermissionRow(clientId: number, area: string) {
    await expect(this.page.locator(`text=Client ${clientId}`)).toBeVisible();
    await expect(this.page.locator(`text=${area}`)).toBeVisible();
  }

  async revokePermission(clientId: number, area: string) {
    // Find the permission row and click revoke
    const permissionRow = this.page.locator(`text=Client ${clientId}`).locator('..').locator('..');
    await permissionRow.locator('button:has-text("Revoke")').click();
    
    // Confirm revocation if there's a confirmation dialog
    const confirmButton = this.page.locator('button:has-text("Confirm")').or(this.page.locator('button:has-text("Yes")'));
    if (await confirmButton.isVisible()) {
      await confirmButton.click();
    }
  }

  // Filter and search methods
  async searchAssociates(searchTerm: string) {
    const searchInput = this.page.locator('input[placeholder*="Search"]');
    if (await searchInput.isVisible()) {
      await searchInput.fill(searchTerm);
      await this.page.waitForTimeout(1000);
    }
  }

  async filterByPermissionArea(area: string) {
    const areaFilter = this.page.locator('[role="combobox"]').filter({ hasText: /area/i });
    if (await areaFilter.isVisible()) {
      await areaFilter.click();
      await this.page.locator(`text=${area}`).click();
      await this.page.waitForTimeout(1000);
    }
  }

  // Admin navigation methods
  async expectAdminNavigationVisible() {
    await expect(this.page.locator('text=Admin').or(this.page.locator('[data-testid="admin-nav"]'))).toBeVisible();
    await expect(this.page.locator('text=Associate Management').or(this.page.locator('a[href="/admin/associates"]'))).toBeVisible();
    await expect(this.page.locator('text=Admin Settings').or(this.page.locator('a[href="/admin/settings"]'))).toBeVisible();
  }

  async clickAdminNavigationLink(linkText: string) {
    await this.page.locator(`text=${linkText}`).click();
  }

  // Bulk operations methods
  async selectMultiplePermissions(clientIds: number[]) {
    for (const clientId of clientIds) {
      const checkbox = this.page.locator(`text=Client ${clientId}`).locator('..').locator('input[type="checkbox"]');
      if (await checkbox.isVisible()) {
        await checkbox.check();
      }
    }
  }

  async clickBulkRevokeButton() {
    await this.page.locator('button:has-text("Bulk Revoke")').click();
  }

  async confirmBulkOperation() {
    await this.page.locator('button:has-text("Confirm")').click();
  }

  // Statistics and analytics methods
  async expectStatisticsVisible() {
    // Check for charts or statistics displays
    await expect(this.page.locator('text=Statistics').or(this.page.locator('[data-testid="statistics"]'))).toBeVisible();
  }

  async getStatisticValue(statName: string) {
    const stat = this.page.locator(`text=${statName}`).locator('..').locator('.font-bold');
    return await stat.textContent();
  }

  // Permission alerts methods
  async expectPermissionAlerts() {
    // Check for expiring permissions alerts
    const alertSection = this.page.locator('text=Expiring Soon').locator('..');
    if (await alertSection.isVisible()) {
      const alertCount = await alertSection.locator('.text-2xl').textContent();
      if (alertCount && parseInt(alertCount) > 0) {
        await expect(this.page.locator('text=expiring').or(this.page.locator('text=expire'))).toBeVisible();
      }
    }
  }

  // Error handling methods
  async expectErrorMessage(message?: string) {
    if (message) {
      await expect(this.page.locator(`text=${message}`)).toBeVisible();
    } else {
      await expect(this.page.locator('text=Error').or(this.page.locator('[data-testid="error"]'))).toBeVisible();
    }
  }

  async expectNoErrorMessage() {
    await expect(this.page.locator('text=Error').or(this.page.locator('[data-testid="error"]'))).not.toBeVisible();
  }

  // Loading states methods
  async expectLoadingComplete() {
    await expect(this.page.locator('[data-testid="loading"]').or(this.page.locator('.animate-spin'))).not.toBeVisible({ timeout: 10000 });
  }

  async waitForDataLoad() {
    // Wait for main content to be visible
    await this.expectSummaryCards();
    await this.expectLoadingComplete();
  }

  // Data validation methods
  async expectValidNumericValues() {
    const summaryCards = ['Total Associates', 'Active Permissions', 'Expiring Soon', 'Expired'];
    
    for (const cardName of summaryCards) {
      const value = await this.getSummaryCardValue(cardName);
      if (value) {
        expect(value.trim()).toMatch(/^\d+$/); // Should be a number
      }
    }
  }

  // Permission level methods
  async expectPermissionLevelBadges() {
    const permissionLevels = ['Read', 'Create', 'Update', 'Delete', 'Submit', 'Approve'];
    
    // Check that at least one permission level badge is visible
    const badgeVisible = await Promise.all(
      permissionLevels.map(level => 
        this.page.locator(`text=${level}`).isVisible()
      )
    );
    
    expect(badgeVisible.some(visible => visible)).toBeTruthy();
  }

  // Responsive design methods
  async expectMobileLayout() {
    // Check that content is responsive on mobile
    await expect(this.page.locator('h1')).toBeVisible();
    
    // Summary cards should stack on mobile
    const cards = this.page.locator('.text-2xl');
    const cardCount = await cards.count();
    
    if (cardCount > 0) {
      // Check first card position
      const firstCard = cards.first();
      const boundingBox = await firstCard.boundingBox();
      expect(boundingBox?.width).toBeLessThan(400); // Should be narrow on mobile
    }
  }

  // Export/import methods (if implemented)
  async clickExportButton() {
    const exportButton = this.page.locator('button:has-text("Export")');
    if (await exportButton.isVisible()) {
      await exportButton.click();
    }
  }

  async expectDownloadStarted() {
    // This would require setting up download handling in the test
    // For now, just check that no error occurred
    await this.expectNoErrorMessage();
  }
}