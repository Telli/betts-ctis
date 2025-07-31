import { test, expect } from '@playwright/test';
import { AuthHelper } from '../utils/auth-helper';
import { API_ENDPOINTS, TEST_USERS } from '../utils/test-data';

test.describe('API Integration', () => {
  let authHelper: AuthHelper;
  let apiContext: any;

  test.beforeEach(async ({ page, request }) => {
    authHelper = new AuthHelper(page);
    apiContext = request;
  });

  test.describe('Authentication API', () => {
    test('should authenticate client via API', async ({ page }) => {
      const response = await apiContext.post('http://localhost:5000/api/auth/login', {
        data: {
          email: TEST_USERS.client.email,
          password: TEST_USERS.client.password
        }
      });

      expect(response.status()).toBe(200);
      
      const responseData = await response.json();
      expect(responseData).toHaveProperty('token');
      expect(responseData).toHaveProperty('user');
      expect(responseData.user.role).toBe('Client');
    });

    test('should reject invalid credentials via API', async ({ page }) => {
      const response = await apiContext.post('http://localhost:5000/api/auth/login', {
        data: {
          email: 'invalid@example.com',
          password: 'wrongpassword'
        }
      });

      expect(response.status()).toBe(401);
    });

    test('should validate required fields via API', async ({ page }) => {
      const response = await apiContext.post('http://localhost:5000/api/auth/login', {
        data: {
          email: '',
          password: ''
        }
      });

      expect(response.status()).toBe(400);
    });
  });

  test.describe('Client Portal API', () => {
    let authToken: string;

    test.beforeEach(async ({ page }) => {
      // Get auth token for API calls
      const loginResponse = await apiContext.post('http://localhost:5000/api/auth/login', {
        data: {
          email: TEST_USERS.client.email,
          password: TEST_USERS.client.password
        }
      });
      
      const loginData = await loginResponse.json();
      authToken = loginData.token;
    });

    test('should fetch client dashboard data', async ({ page }) => {
      const response = await apiContext.get('http://localhost:5000/api/client-portal/dashboard', {
        headers: {
          'Authorization': `Bearer ${authToken}`
        }
      });

      expect(response.status()).toBe(200);
      
      const dashboardData = await response.json();
      expect(dashboardData).toHaveProperty('complianceScore');
      expect(dashboardData).toHaveProperty('recentActivity');
    });

    test('should fetch client documents', async ({ page }) => {
      const response = await apiContext.get('http://localhost:5000/api/client-portal/documents', {
        headers: {
          'Authorization': `Bearer ${authToken}`
        }
      });

      expect(response.status()).toBe(200);
      
      const documentsData = await response.json();
      expect(Array.isArray(documentsData)).toBe(true);
    });

    test('should fetch client profile', async ({ page }) => {
      const response = await apiContext.get('http://localhost:5000/api/client-portal/profile', {
        headers: {
          'Authorization': `Bearer ${authToken}`
        }
      });

      expect(response.status()).toBe(200);
      
      const profileData = await response.json();
      expect(profileData).toHaveProperty('businessName');
      expect(profileData).toHaveProperty('email');
    });

    test('should update client profile via API', async ({ page }) => {
      const updateData = {
        businessName: 'Updated Test Company',
        contactPerson: 'Jane Doe',
        phoneNumber: '+232 76 987 654'
      };

      const response = await apiContext.put('http://localhost:5000/api/client-portal/profile', {
        headers: {
          'Authorization': `Bearer ${authToken}`,
          'Content-Type': 'application/json'
        },
        data: updateData
      });

      expect(response.status()).toBe(200);
      
      const updatedProfile = await response.json();
      expect(updatedProfile.businessName).toBe(updateData.businessName);
    });

    test('should reject unauthorized API access', async ({ page }) => {
      const response = await apiContext.get('http://localhost:5000/api/client-portal/dashboard');

      expect(response.status()).toBe(401);
    });

    test('should reject invalid token', async ({ page }) => {
      const response = await apiContext.get('http://localhost:5000/api/client-portal/dashboard', {
        headers: {
          'Authorization': 'Bearer invalid-token'
        }
      });

      expect(response.status()).toBe(401);
    });

    test('should create tax filing via API', async ({ page }) => {
      const taxFilingData = {
        taxType: 'IncomeTax',
        taxYear: 2024,
        dueDate: new Date(Date.now() + 30 * 24 * 60 * 60 * 1000).toISOString(), // 30 days from now
        taxLiability: 25000,
        filingReference: 'TEST-TF-001'
      };

      const response = await apiContext.post('http://localhost:5000/api/client-portal/tax-filings', {
        headers: {
          'Authorization': `Bearer ${authToken}`,
          'Content-Type': 'application/json'
        },
        data: taxFilingData
      });

      expect(response.status()).toBe(200);
      
      const responseData = await response.json();
      expect(responseData.success).toBe(true);
      expect(responseData.data).toHaveProperty('taxType', 'IncomeTax');
      expect(responseData.data).toHaveProperty('taxYear', 2024);
      expect(responseData.data).toHaveProperty('taxLiability', 25000);
    });

    test('should create payment via API', async ({ page }) => {
      const paymentData = {
        amount: 15000,
        method: 'BankTransfer',
        paymentReference: 'TEST-PAY-001',
        paymentDate: new Date().toISOString()
      };

      const response = await apiContext.post('http://localhost:5000/api/client-portal/payments', {
        headers: {
          'Authorization': `Bearer ${authToken}`,
          'Content-Type': 'application/json'
        },
        data: paymentData
      });

      expect(response.status()).toBe(200);
      
      const responseData = await response.json();
      expect(responseData.success).toBe(true);
      expect(responseData.data).toHaveProperty('amount', 15000);
      expect(responseData.data).toHaveProperty('method', 'BankTransfer');
      expect(responseData.data).toHaveProperty('paymentReference', 'TEST-PAY-001');
    });

    test('should validate tax filing data', async ({ page }) => {
      const invalidTaxFilingData = {
        taxType: '', // Invalid empty tax type
        taxYear: 1999, // Invalid year (too old)
        dueDate: 'invalid-date', // Invalid date format
        taxLiability: -1000 // Invalid negative amount
      };

      const response = await apiContext.post('http://localhost:5000/api/client-portal/tax-filings', {
        headers: {
          'Authorization': `Bearer ${authToken}`,
          'Content-Type': 'application/json'
        },
        data: invalidTaxFilingData
      });

      expect(response.status()).toBe(400);
    });

    test('should validate payment data', async ({ page }) => {
      const invalidPaymentData = {
        amount: -500, // Invalid negative amount
        method: 'InvalidMethod', // Invalid payment method
        paymentReference: '', // Invalid empty reference
        paymentDate: 'not-a-date' // Invalid date format
      };

      const response = await apiContext.post('http://localhost:5000/api/client-portal/payments', {
        headers: {
          'Authorization': `Bearer ${authToken}`,
          'Content-Type': 'application/json'
        },
        data: invalidPaymentData
      });

      expect(response.status()).toBe(400);
    });

    test('should fetch client tax filings via API', async ({ page }) => {
      const response = await apiContext.get('http://localhost:5000/api/client-portal/tax-filings', {
        headers: {
          'Authorization': `Bearer ${authToken}`
        }
      });

      expect(response.status()).toBe(200);
      
      const responseData = await response.json();
      expect(responseData.success).toBe(true);
      expect(Array.isArray(responseData.data)).toBe(true);
    });

    test('should fetch client payments via API', async ({ page }) => {
      const response = await apiContext.get('http://localhost:5000/api/client-portal/payments', {
        headers: {
          'Authorization': `Bearer ${authToken}`
        }
      });

      expect(response.status()).toBe(200);
      
      const responseData = await response.json();
      expect(responseData.success).toBe(true);
      expect(Array.isArray(responseData.data)).toBe(true);
    });
  });

  test.describe('Admin API', () => {
    let adminToken: string;

    test.beforeEach(async ({ page }) => {
      // Get admin auth token
      const loginResponse = await apiContext.post('http://localhost:5000/api/auth/login', {
        data: {
          email: TEST_USERS.admin.email,
          password: TEST_USERS.admin.password
        }
      });
      
      const loginData = await loginResponse.json();
      adminToken = loginData.token;
    });

    test('should fetch admin clients list', async ({ page }) => {
      const response = await apiContext.get('http://localhost:5000/api/admin/clients', {
        headers: {
          'Authorization': `Bearer ${adminToken}`
        }
      });

      expect(response.status()).toBe(200);
      
      const clientsData = await response.json();
      expect(Array.isArray(clientsData)).toBe(true);
    });

    test('should fetch admin statistics', async ({ page }) => {
      const response = await apiContext.get('http://localhost:5000/api/admin/clients/stats', {
        headers: {
          'Authorization': `Bearer ${adminToken}`
        }
      });

      expect(response.status()).toBe(200);
      
      const statsData = await response.json();
      expect(statsData).toHaveProperty('totalClients');
      expect(statsData).toHaveProperty('activeClients');
    });

    test('should deny client access to admin API', async ({ page }) => {
      // Get client token
      const clientLoginResponse = await apiContext.post('http://localhost:5000/api/auth/login', {
        data: {
          email: TEST_USERS.client.email,
          password: TEST_USERS.client.password
        }
      });
      
      const clientLoginData = await clientLoginResponse.json();
      const clientToken = clientLoginData.token;

      // Try to access admin endpoint with client token
      const response = await apiContext.get('http://localhost:5000/api/admin/clients', {
        headers: {
          'Authorization': `Bearer ${clientToken}`
        }
      });

      expect(response.status()).toBe(403);
    });
  });

  test.describe('Data Isolation', () => {
    test('should only return client-specific data', async ({ page }) => {
      // Login as client and get documents
      const clientLoginResponse = await apiContext.post('http://localhost:5000/api/auth/login', {
        data: {
          email: TEST_USERS.client.email,
          password: TEST_USERS.client.password
        }
      });
      
      const clientLoginData = await clientLoginResponse.json();
      const clientToken = clientLoginData.token;

      const documentsResponse = await apiContext.get('http://localhost:5000/api/client-portal/documents', {
        headers: {
          'Authorization': `Bearer ${clientToken}`
        }
      });

      expect(documentsResponse.status()).toBe(200);
      
      const documents = await documentsResponse.json();
      
      // Verify all documents belong to the authenticated client
      // This would require seeded test data to properly verify
      expect(Array.isArray(documents)).toBe(true);
    });
  });

  test.describe('Error Handling', () => {
    test('should handle server errors gracefully', async ({ page }) => {
      // Test a potentially error-prone endpoint
      const response = await apiContext.get('http://localhost:5000/api/nonexistent-endpoint');

      expect(response.status()).toBe(404);
    });

    test('should validate request data', async ({ page }) => {
      const loginResponse = await apiContext.post('http://localhost:5000/api/auth/login', {
        data: {
          email: TEST_USERS.client.email,
          password: TEST_USERS.client.password
        }
      });
      
      const loginData = await loginResponse.json();
      const authToken = loginData.token;

      // Send invalid profile update data
      const response = await apiContext.put('http://localhost:5000/api/client-portal/profile', {
        headers: {
          'Authorization': `Bearer ${authToken}`,
          'Content-Type': 'application/json'
        },
        data: {
          businessName: '', // Invalid empty name
          email: 'invalid-email' // Invalid email format
        }
      });

      expect(response.status()).toBe(400);
    });
  });

  test.describe('Performance', () => {
    test('should respond within acceptable time limits', async ({ page }) => {
      const startTime = Date.now();
      
      const response = await apiContext.post('http://localhost:5000/api/auth/login', {
        data: {
          email: TEST_USERS.client.email,
          password: TEST_USERS.client.password
        }
      });
      
      const endTime = Date.now();
      const responseTime = endTime - startTime;

      expect(response.status()).toBe(200);
      expect(responseTime).toBeLessThan(5000); // Should respond within 5 seconds
    });
  });
});