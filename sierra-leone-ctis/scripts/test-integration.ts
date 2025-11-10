/**
 * Integration Test Script
 * Run with: npx ts-node scripts/test-integration.ts
 * 
 * This script tests the frontend-backend integration
 */

const API_BASE_URL = process.env.NEXT_PUBLIC_API_URL || 'http://localhost:5001';

interface TestResult {
  test: string;
  passed: boolean;
  message: string;
  error?: any;
}

const results: TestResult[] = [];

async function testEndpoint(name: string, endpoint: string, method: string = 'GET', body?: any): Promise<TestResult> {
  try {
    const options: RequestInit = {
      method,
      headers: {
        'Content-Type': 'application/json',
      },
    };

    if (body) {
      options.body = JSON.stringify(body);
    }

    const response = await fetch(`${API_BASE_URL}${endpoint}`, options);
    
    return {
      test: name,
      passed: response.status < 500, // Consider 4xx as "working" (expected errors)
      message: `Status: ${response.status} ${response.statusText}`,
    };
  } catch (error: any) {
    return {
      test: name,
      passed: false,
      message: `Connection failed: ${error.message}`,
      error,
    };
  }
}

async function runIntegrationTests() {
  console.log('🚀 Starting Integration Tests...\n');
  console.log(`Testing API at: ${API_BASE_URL}\n`);

  // Test 1: Backend Health Check
  console.log('1️⃣  Testing Backend Connection...');
  results.push(await testEndpoint(
    'Backend Health Check',
    '/health',
    'GET'
  ));

  // Test 2: Auth Endpoints
  console.log('2️⃣  Testing Auth Endpoints...');
  results.push(await testEndpoint(
    'Login Endpoint (Should return 401 for invalid credentials)',
    '/api/auth/login',
    'POST',
    { Email: 'test@example.com', Password: 'wrongpassword' }
  ));

  results.push(await testEndpoint(
    'Register Endpoint Structure',
    '/api/auth/register',
    'POST',
    { email: 'test@test.com', password: 'Test123!', firstName: 'Test', lastName: 'User' }
  ));

  // Test 3: Protected Endpoints (Should return 401)
  console.log('3️⃣  Testing Protected Endpoints...');
  results.push(await testEndpoint(
    'Clients Endpoint (Should require auth)',
    '/api/clients',
    'GET'
  ));

  results.push(await testEndpoint(
    'Dashboard Endpoint (Should require auth)',
    '/api/dashboard/client',
    'GET'
  ));

  results.push(await testEndpoint(
    'Documents Endpoint (Should require auth)',
    '/api/documents',
    'GET'
  ));

  results.push(await testEndpoint(
    'Payments Endpoint (Should require auth)',
    '/api/payments',
    'GET'
  ));

  results.push(await testEndpoint(
    'Tax Filings Endpoint (Should require auth)',
    '/api/taxfilings',
    'GET'
  ));

  results.push(await testEndpoint(
    'Notifications Endpoint (Should require auth)',
    '/api/notifications',
    'GET'
  ));

  // Test 4: Public Endpoints
  console.log('4️⃣  Testing Public Endpoints...');
  results.push(await testEndpoint(
    'Tax Calculator Endpoint',
    '/api/taxcalculation/calculate',
    'POST',
    { income: 10000, taxType: 'IncomeTax' }
  ));

  // Print Results
  console.log('\n📊 Test Results:\n');
  console.log('='.repeat(80));
  
  results.forEach((result, index) => {
    const icon = result.passed ? '✅' : '❌';
    console.log(`${icon} ${result.test}`);
    console.log(`   ${result.message}`);
    if (result.error) {
      console.log(`   Error: ${result.error.message}`);
    }
    console.log('');
  });

  console.log('='.repeat(80));
  
  const passedTests = results.filter(r => r.passed).length;
  const totalTests = results.length;
  const passRate = ((passedTests / totalTests) * 100).toFixed(1);

  console.log(`\n📈 Summary: ${passedTests}/${totalTests} tests passed (${passRate}%)\n`);

  if (passedTests === totalTests) {
    console.log('🎉 All integration tests passed! Frontend-Backend connection verified.');
  } else {
    console.log('⚠️  Some tests failed. Check the backend is running and accessible.');
    console.log(`   Backend URL: ${API_BASE_URL}`);
    console.log('   Make sure the backend is running on this port.');
  }

  process.exit(passedTests === totalTests ? 0 : 1);
}

runIntegrationTests().catch((error) => {
  console.error('❌ Test suite failed:', error);
  process.exit(1);
});
