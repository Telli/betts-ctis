import { Page } from '@playwright/test';

/**
 * Test user credentials from demo data seeding
 */
export const TEST_USERS = {
  admin: {
    email: 'admin@bettsfirm.sl',
    password: 'Admin123!',
    role: 'Admin'
  },
  associate: {
    email: 'associate@bettsfirm.sl',
    password: 'Associate123!',
    role: 'Associate'
  },
  client: {
    email: 'client@testcompany.sl',
    password: 'Client123!',
    role: 'Client'
  },
  systemAdmin: {
    email: 'admin@thebettsfirmsl.com',
    password: 'AdminPass123!',
    role: 'SystemAdmin'
  }
};

/**
 * Login helper function
 */
export async function login(page: Page, userType: keyof typeof TEST_USERS = 'admin') {
  const user = TEST_USERS[userType];
  
  await page.goto('/login');
  await page.fill('input[name="email"]', user.email);
  await page.fill('input[name="password"]', user.password);
  await page.click('button[type="submit"]');
  
  // Wait for successful login redirect
  await page.waitForURL(/\/(dashboard|client-portal)/, { timeout: 10000 });
  
  return user;
}

/**
 * Logout helper function
 */
export async function logout(page: Page) {
  // Click user menu or logout button
  await page.click('button[aria-label="User menu"]').catch(() => {});
  await page.click('text=Logout').catch(() => {});
  
  // Wait for redirect to login
  await page.waitForURL('/login', { timeout: 5000 }).catch(() => {});
}

/**
 * Check if user is logged in
 */
export async function isLoggedIn(page: Page): Promise<boolean> {
  try {
    // Check for presence of dashboard or user menu
    await page.waitForSelector('[aria-label="User menu"], text=Dashboard', { timeout: 2000 });
    return true;
  } catch {
    return false;
  }
}

/**
 * Get auth token from storage
 */
export async function getAuthToken(page: Page): Promise<string | null> {
  return await page.evaluate(() => {
    return localStorage.getItem('authToken');
  });
}
