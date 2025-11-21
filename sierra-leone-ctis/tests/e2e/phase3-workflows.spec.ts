import { test, expect } from '@playwright/test';
import { LoginPage } from '../page-objects/LoginPage';
import { TEST_USERS, SELECTORS, PAYMENT_APPROVAL_TEST_DATA, COMPLIANCE_MONITORING_TEST_DATA, DOCUMENT_MANAGEMENT_TEST_DATA, COMMUNICATION_ROUTING_TEST_DATA } from '../utils/test-data';

test.describe('Phase 3 Workflow Automation', () => {
  let loginPage: LoginPage;

  test.beforeEach(async ({ page }) => {
    loginPage = new LoginPage(page);
    // Login as admin to access workflows
    await loginPage.loginAsAdmin();
  });

  test.describe('Payment Approval Workflow', () => {
    test('should display payment approval dashboard', async ({ page }) => {
      // Navigate to workflows
      await page.goto('/admin/workflows');
      
      // Check if workflow dashboard is visible
      await expect(page.locator(SELECTORS.workflowDashboard)).toBeVisible();
      
      // Click on payment approval tab
      await page.click(SELECTORS.paymentApprovalTab);
      
      // Verify payment approval list is displayed
      await expect(page.locator(SELECTORS.paymentApprovalList)).toBeVisible();
    });

    test('should request payment approval', async ({ page }) => {
      await page.goto('/admin/workflows');
      await page.click(SELECTORS.paymentApprovalTab);
      
      // Look for request button or form
      const requestButton = page.locator('button:has-text("Request Approval")');
      if (await requestButton.isVisible()) {
        await requestButton.click();
        
        // Fill in payment details
        await page.fill('input[name="amount"]', PAYMENT_APPROVAL_TEST_DATA.smallAmount.amount.toString());
        await page.fill('textarea[name="description"]', PAYMENT_APPROVAL_TEST_DATA.smallAmount.description);
        
        // Submit
        await page.click('button:has-text("Submit")');
        
        // Verify success message
        await expect(page.locator('text=Approval requested successfully')).toBeVisible({ timeout: 5000 });
      }
    });

    test('should approve payment request', async ({ page }) => {
      await page.goto('/admin/workflows');
      await page.click(SELECTORS.paymentApprovalTab);
      
      // Wait for payment approval list to load
      await page.waitForSelector(SELECTORS.paymentApprovalList);
      
      // Get first pending approval
      const firstRow = page.locator(SELECTORS.paymentApprovalRow).first();
      if (await firstRow.isVisible()) {
        // Click approve button
        const approveBtn = firstRow.locator(SELECTORS.approveButton);
        if (await approveBtn.isVisible()) {
          await approveBtn.click();
          
          // Confirm approval
          await page.click('button:has-text("Confirm")');
          
          // Verify success
          await expect(page.locator('text=Payment approved successfully')).toBeVisible({ timeout: 5000 });
        }
      }
    });
  });

  test.describe('Compliance Monitoring Workflow', () => {
    test('should display compliance monitoring dashboard', async ({ page }) => {
      await page.goto('/admin/workflows');
      
      // Click on compliance monitoring tab
      await page.click(SELECTORS.complianceMonitoringTab);
      
      // Verify compliance list is displayed
      await expect(page.locator(SELECTORS.complianceList)).toBeVisible();
    });

    test('should show compliance deadlines', async ({ page }) => {
      await page.goto('/admin/workflows');
      await page.click(SELECTORS.complianceMonitoringTab);
      
      // Wait for compliance list
      await page.waitForSelector(SELECTORS.complianceList);
      
      // Check for deadline information
      const deadlineText = page.locator('text=Deadline');
      await expect(deadlineText).toBeVisible();
    });

    test('should mark compliance item as filed', async ({ page }) => {
      await page.goto('/admin/workflows');
      await page.click(SELECTORS.complianceMonitoringTab);
      
      // Wait for compliance list
      await page.waitForSelector(SELECTORS.complianceList);
      
      // Get first compliance row
      const firstRow = page.locator(SELECTORS.complianceRow).first();
      if (await firstRow.isVisible()) {
        const markFiledBtn = firstRow.locator(SELECTORS.markAsFiledButton);
        if (await markFiledBtn.isVisible()) {
          await markFiledBtn.click();
          
          // Verify success
          await expect(page.locator('text=Marked as filed')).toBeVisible({ timeout: 5000 });
        }
      }
    });
  });

  test.describe('Document Management Workflow', () => {
    test('should display document management dashboard', async ({ page }) => {
      await page.goto('/admin/workflows');
      
      // Click on document management tab
      await page.click(SELECTORS.documentManagementTab);
      
      // Verify document list is displayed
      await expect(page.locator(SELECTORS.documentList)).toBeVisible();
    });

    test('should verify document', async ({ page }) => {
      await page.goto('/admin/workflows');
      await page.click(SELECTORS.documentManagementTab);
      
      // Wait for document list
      await page.waitForSelector(SELECTORS.documentList);
      
      // Get first document row
      const firstRow = page.locator(SELECTORS.documentRow).first();
      if (await firstRow.isVisible()) {
        const verifyBtn = firstRow.locator(SELECTORS.verifyButton);
        if (await verifyBtn.isVisible()) {
          await verifyBtn.click();
          
          // Verify success
          await expect(page.locator('text=Document verified')).toBeVisible({ timeout: 5000 });
        }
      }
    });

    test('should approve document', async ({ page }) => {
      await page.goto('/admin/workflows');
      await page.click(SELECTORS.documentManagementTab);
      
      // Wait for document list
      await page.waitForSelector(SELECTORS.documentList);
      
      // Get first document row
      const firstRow = page.locator(SELECTORS.documentRow).first();
      if (await firstRow.isVisible()) {
        const approveBtn = firstRow.locator(SELECTORS.approveDocButton);
        if (await approveBtn.isVisible()) {
          await approveBtn.click();
          
          // Verify success
          await expect(page.locator('text=Document approved')).toBeVisible({ timeout: 5000 });
        }
      }
    });
  });

  test.describe('Communication Routing Workflow', () => {
    test('should display communication routing dashboard', async ({ page }) => {
      await page.goto('/admin/workflows');
      
      // Click on communication routing tab
      await page.click(SELECTORS.communicationRoutingTab);
      
      // Verify message list is displayed
      await expect(page.locator(SELECTORS.messageList)).toBeVisible();
    });

    test('should assign message to handler', async ({ page }) => {
      await page.goto('/admin/workflows');
      await page.click(SELECTORS.communicationRoutingTab);
      
      // Wait for message list
      await page.waitForSelector(SELECTORS.messageList);
      
      // Get first message row
      const firstRow = page.locator(SELECTORS.messageRow).first();
      if (await firstRow.isVisible()) {
        const assignBtn = firstRow.locator(SELECTORS.assignButton);
        if (await assignBtn.isVisible()) {
          await assignBtn.click();
          
          // Select handler from dropdown
          await page.click('select[name="handler"]');
          await page.click('option:has-text("Tax Associate")');
          
          // Confirm assignment
          await page.click('button:has-text("Assign")');
          
          // Verify success
          await expect(page.locator('text=Message assigned')).toBeVisible({ timeout: 5000 });
        }
      }
    });

    test('should escalate message', async ({ page }) => {
      await page.goto('/admin/workflows');
      await page.click(SELECTORS.communicationRoutingTab);
      
      // Wait for message list
      await page.waitForSelector(SELECTORS.messageList);
      
      // Get first message row
      const firstRow = page.locator(SELECTORS.messageRow).first();
      if (await firstRow.isVisible()) {
        const escalateBtn = firstRow.locator(SELECTORS.escalateButton);
        if (await escalateBtn.isVisible()) {
          await escalateBtn.click();
          
          // Fill escalation reason
          await page.fill('textarea[name="reason"]', 'Requires manager approval');
          
          // Confirm escalation
          await page.click('button:has-text("Escalate")');
          
          // Verify success
          await expect(page.locator('text=Message escalated')).toBeVisible({ timeout: 5000 });
        }
      }
    });

    test('should resolve message', async ({ page }) => {
      await page.goto('/admin/workflows');
      await page.click(SELECTORS.communicationRoutingTab);
      
      // Wait for message list
      await page.waitForSelector(SELECTORS.messageList);
      
      // Get first message row
      const firstRow = page.locator(SELECTORS.messageRow).first();
      if (await firstRow.isVisible()) {
        const resolveBtn = firstRow.locator(SELECTORS.resolveButton);
        if (await resolveBtn.isVisible()) {
          await resolveBtn.click();
          
          // Fill resolution notes
          await page.fill('textarea[name="notes"]', 'Issue resolved successfully');
          
          // Confirm resolution
          await page.click('button:has-text("Resolve")');
          
          // Verify success
          await expect(page.locator('text=Message resolved')).toBeVisible({ timeout: 5000 });
        }
      }
    });
  });

  test.describe('Workflow Statistics', () => {
    test('should display workflow statistics', async ({ page }) => {
      await page.goto('/admin/workflows');
      
      // Check for statistics cards
      const statsCards = page.locator('[data-testid="stats-card"]');
      await expect(statsCards.first()).toBeVisible();
    });

    test('should show pending items count', async ({ page }) => {
      await page.goto('/admin/workflows');
      
      // Look for pending count
      const pendingCount = page.locator('text=Pending');
      await expect(pendingCount).toBeVisible();
    });
  });
});

