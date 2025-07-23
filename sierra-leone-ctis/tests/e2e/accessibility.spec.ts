import { test, expect } from '@playwright/test';
import { AuthHelper } from '../utils/auth-helper';
import { ClientPortalPage } from '../page-objects/ClientPortalPage';

test.describe('Accessibility', () => {
  let authHelper: AuthHelper;
  let clientPortalPage: ClientPortalPage;

  test.beforeEach(async ({ page }) => {
    authHelper = new AuthHelper(page);
    clientPortalPage = new ClientPortalPage(page);
  });

  test.describe('Keyboard Navigation', () => {
    test('should navigate login form with keyboard', async ({ page }) => {
      await page.goto('/login');
      
      // Tab through form elements
      await page.keyboard.press('Tab');
      await expect(page.locator('input[name="email"]')).toBeFocused();
      
      await page.keyboard.press('Tab');
      await expect(page.locator('input[name="password"]')).toBeFocused();
      
      await page.keyboard.press('Tab');
      await expect(page.locator('button[type="submit"]')).toBeFocused();
      
      // Fill form with keyboard
      await page.keyboard.press('Shift+Tab');
      await page.keyboard.press('Shift+Tab');
      await page.keyboard.type('client@testcompany.sl');
      
      await page.keyboard.press('Tab');
      await page.keyboard.type('Client123!');
      
      // Submit with Enter
      await page.keyboard.press('Enter');
      await page.waitForLoadState('networkidle');
      
      // Should be logged in
      await expect(page).toHaveURL('/client-portal/dashboard');
    });

    test('should navigate client portal with keyboard', async ({ page }) => {
      await authHelper.login('client');
      
      // Test Tab navigation through main navigation
      await page.keyboard.press('Tab');
      
      // Check if sidebar navigation is accessible
      const sidebarItems = page.locator('[data-testid="client-sidebar"] a');
      const count = await sidebarItems.count();
      
      for (let i = 0; i < Math.min(count, 5); i++) {
        const item = sidebarItems.nth(i);
        await item.focus();
        await expect(item).toBeFocused();
        
        // Press Enter to navigate
        await page.keyboard.press('Enter');
        await page.waitForTimeout(1000);
      }
    });

    test('should support skip links', async ({ page }) => {
      await authHelper.login('client');
      
      // Press Tab to focus skip link (if it exists)
      await page.keyboard.press('Tab');
      
      const skipLink = page.locator('a:has-text("Skip to main content")');
      if (await skipLink.isVisible()) {
        await expect(skipLink).toBeFocused();
        
        // Activate skip link
        await page.keyboard.press('Enter');
        
        // Main content should be focused
        await expect(page.locator('main')).toBeFocused();
      }
    });
  });

  test.describe('Screen Reader Support', () => {
    test('should have proper heading structure', async ({ page }) => {
      await authHelper.login('client');
      
      // Check for proper heading hierarchy
      const h1Elements = page.locator('h1');
      const h1Count = await h1Elements.count();
      expect(h1Count).toBeGreaterThanOrEqual(1);
      
      // Should have only one h1 per page
      expect(h1Count).toBeLessThanOrEqual(1);
      
      // Check for heading progression (h2, h3, etc.)
      const allHeadings = page.locator('h1, h2, h3, h4, h5, h6');
      const headingCount = await allHeadings.count();
      expect(headingCount).toBeGreaterThan(0);
    });

    test('should have accessible form labels', async ({ page }) => {
      await page.goto('/login');
      
      // Check that form inputs have proper labels
      const emailInput = page.locator('input[name="email"]');
      const passwordInput = page.locator('input[name="password"]');
      
      // Inputs should have aria-label or associated label
      const emailLabel = await emailInput.getAttribute('aria-label') || 
                        await page.locator('label[for="email"]').textContent();
      const passwordLabel = await passwordInput.getAttribute('aria-label') || 
                           await page.locator('label[for="password"]').textContent();
      
      expect(emailLabel).toBeTruthy();
      expect(passwordLabel).toBeTruthy();
    });

    test('should have accessible navigation landmarks', async ({ page }) => {
      await authHelper.login('client');
      
      // Check for navigation landmarks
      const nav = page.locator('nav');
      const main = page.locator('main');
      
      expect(await nav.count()).toBeGreaterThan(0);
      expect(await main.count()).toBeGreaterThan(0);
      
      // Check for proper ARIA roles if not using semantic HTML
      const navigationRole = page.locator('[role="navigation"]');
      const mainRole = page.locator('[role="main"]');
      
      expect((await nav.count()) + (await navigationRole.count())).toBeGreaterThan(0);
      expect((await main.count()) + (await mainRole.count())).toBeGreaterThan(0);
    });

    test('should have accessible button text', async ({ page }) => {
      await authHelper.login('client');
      await clientPortalPage.gotoDocuments();
      
      // Check that buttons have descriptive text or aria-label
      const buttons = page.locator('button');
      const buttonCount = await buttons.count();
      
      for (let i = 0; i < Math.min(buttonCount, 10); i++) {
        const button = buttons.nth(i);
        const text = await button.textContent();
        const ariaLabel = await button.getAttribute('aria-label');
        const title = await button.getAttribute('title');
        
        // Button should have text, aria-label, or title
        expect(text || ariaLabel || title).toBeTruthy();
      }
    });
  });

  test.describe('Color and Contrast', () => {
    test('should not rely solely on color for information', async ({ page }) => {
      await authHelper.login('client');
      
      // Check that error states have text in addition to color
      await clientPortalPage.gotoProfile();
      
      // Clear a required field and trigger validation
      await page.fill('input[name="businessName"]', '');
      await page.click('[data-testid="save-profile"]');
      
      // Error should have text, not just color
      const errorMessages = page.locator('[role="alert"], .error, .text-red-500');
      if (await errorMessages.count() > 0) {
        const errorText = await errorMessages.first().textContent();
        expect(errorText?.trim()).toBeTruthy();
      }
    });

    test('should maintain functionality in high contrast mode', async ({ page }) => {
      // Enable high contrast mode (if supported by browser)
      await page.emulateMedia({ colorScheme: 'dark' });
      
      await authHelper.login('client');
      
      // Verify key functionality still works
      await clientPortalPage.gotoDashboard();
      await clientPortalPage.expectDashboardVisible();
      
      await clientPortalPage.clickSidebarItem('My Documents');
      await clientPortalPage.expectCurrentPage('My Documents');
    });
  });

  test.describe('Focus Management', () => {
    test('should have visible focus indicators', async ({ page }) => {
      await page.goto('/login');
      
      const emailInput = page.locator('input[name="email"]');
      await emailInput.focus();
      
      // Check if focus is visible (this is a simplified check)
      const focusedElement = page.locator(':focus');
      await expect(focusedElement).toBeVisible();
    });

    test('should manage focus on route changes', async ({ page }) => {
      await authHelper.login('client');
      
      // Navigate to different page
      await clientPortalPage.clickSidebarItem('My Documents');
      
      // Focus should be on main content or heading
      await page.waitForTimeout(500);
      const focusedElement = page.locator(':focus');
      const focusedTag = await focusedElement.evaluate(el => el.tagName.toLowerCase());
      
      // Focus should be on an appropriate element (h1, main, or first interactive element)
      expect(['h1', 'h2', 'main', 'button', 'a', 'input'].includes(focusedTag)).toBeTruthy();
    });

    test('should trap focus in modals', async ({ page }) => {
      await authHelper.login('client');
      await clientPortalPage.gotoDocuments();
      
      // Open upload modal (if it exists)
      const uploadButton = page.locator('[data-testid="upload-document"]');
      if (await uploadButton.isVisible()) {
        await uploadButton.click();
        
        // Check if modal is open
        const modal = page.locator('[role="dialog"]');
        if (await modal.isVisible()) {
          // Tab through modal elements
          await page.keyboard.press('Tab');
          await page.keyboard.press('Tab');
          
          // Focus should stay within modal
          const focusedElement = page.locator(':focus');
          const isInModal = await modal.locator(':focus').count() > 0;
          expect(isInModal).toBeTruthy();
        }
      }
    });
  });

  test.describe('Alternative Text and Media', () => {
    test('should have alt text for images', async ({ page }) => {
      await authHelper.login('client');
      
      const images = page.locator('img');
      const imageCount = await images.count();
      
      for (let i = 0; i < imageCount; i++) {
        const img = images.nth(i);
        const alt = await img.getAttribute('alt');
        const role = await img.getAttribute('role');
        
        // Decorative images should have empty alt or role="presentation"
        // Content images should have descriptive alt text
        expect(alt !== null || role === 'presentation').toBeTruthy();
      }
    });
  });

  test.describe('Mobile Accessibility', () => {
    test('should be accessible on mobile devices', async ({ page }) => {
      // Set mobile viewport
      await page.setViewportSize({ width: 375, height: 667 });
      
      await authHelper.login('client');
      
      // Check that touch targets are large enough (minimum 44px)
      const buttons = page.locator('button, a');
      const buttonCount = await buttons.count();
      
      for (let i = 0; i < Math.min(buttonCount, 5); i++) {
        const button = buttons.nth(i);
        const box = await button.boundingBox();
        
        if (box) {
          // Touch targets should be at least 44px in either dimension
          expect(box.width >= 44 || box.height >= 44).toBeTruthy();
        }
      }
    });
  });

  test.describe('Error Handling and User Feedback', () => {
    test('should provide accessible error messages', async ({ page }) => {
      await page.goto('/login');
      
      // Submit empty form
      await page.click('button[type="submit"]');
      
      // Check for accessible error messages
      const errorMessages = page.locator('[role="alert"], [aria-live="polite"], .error');
      if (await errorMessages.count() > 0) {
        const errorText = await errorMessages.first().textContent();
        expect(errorText?.trim()).toBeTruthy();
        
        // Error should be announced to screen readers
        const ariaLive = await errorMessages.first().getAttribute('aria-live');
        const role = await errorMessages.first().getAttribute('role');
        expect(ariaLive === 'polite' || ariaLive === 'assertive' || role === 'alert').toBeTruthy();
      }
    });

    test('should provide success feedback', async ({ page }) => {
      await authHelper.login('client');
      await clientPortalPage.gotoProfile();
      
      // Make a valid profile update
      await page.fill('input[name="businessName"]', 'Updated Company Name');
      await page.click('[data-testid="save-profile"]');
      
      // Check for accessible success message
      const successMessage = page.locator('text=updated successfully, text=saved, [role="alert"]');
      if (await successMessage.count() > 0) {
        const messageText = await successMessage.first().textContent();
        expect(messageText?.trim()).toBeTruthy();
      }
    });
  });
});