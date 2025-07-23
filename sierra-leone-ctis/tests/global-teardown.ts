import { FullConfig } from '@playwright/test';

async function globalTeardown(config: FullConfig) {
  console.log('🧹 E2E test teardown completed');
  // Add any cleanup logic here if needed
}

export default globalTeardown;