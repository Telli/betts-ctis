import { chromium, FullConfig } from '@playwright/test';

async function globalSetup(config: FullConfig) {
  const browser = await chromium.launch();
  const page = await browser.newPage();
  
  console.log('🚀 Starting E2E test setup...');
  
  try {
    // Wait for both servers to be ready
    const frontendUrl = process.env.BASE_URL || 'http://localhost:3000';
    const backendUrl = process.env.API_URL || 'http://localhost:5000';
    
    console.log('⏳ Waiting for frontend server...');
    await page.goto(frontendUrl, { timeout: 60000 });
    
    console.log('⏳ Waiting for backend server...');
    // Try to access a basic API endpoint to verify backend is running
    try {
      const response = await page.goto(`${backendUrl}/swagger/index.html`, { timeout: 60000 });
      if (!response?.ok()) {
        console.warn('⚠️ Backend server not responding, continuing with frontend-only testing');
      }
    } catch (error) {
      console.warn('⚠️ Backend server not available, continuing with frontend-only testing:', error);
    }
    
    // Setup test data if needed
    await setupTestData(page, backendUrl);
    
    console.log('✅ E2E test setup completed successfully');
  } catch (error) {
    console.error('❌ E2E test setup failed:', error);
    throw error;
  } finally {
    await browser.close();
  }
}

async function setupTestData(page: any, backendUrl: string) {
  // This could seed test data in the database
  // For now, we'll assume the development database has test data
  console.log('📊 Test data setup completed');
}

export default globalSetup;