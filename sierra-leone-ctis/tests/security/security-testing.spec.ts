import { test, expect } from '@playwright/test';
import { LoginPage } from '../page-objects/LoginPage';

test.describe('Security and Penetration Testing', () => {
  test.describe('Authentication Security', () => {
    test('should prevent brute force attacks', async ({ page }) => {
      const loginPage = new LoginPage(page);
      await loginPage.goto();
      
      // Attempt multiple failed logins
      const failedAttempts = 5;
      for (let i = 0; i < failedAttempts; i++) {
        await page.fill('[data-testid="email"]', 'test@example.com');
        await page.fill('[data-testid="password"]', `wrongpassword${i}`);
        await page.click('[data-testid="login-btn"]');
        
        await expect(page.locator('[data-testid="error-message"]')).toBeVisible();
      }
      
      // Account should be locked after multiple failures
      await page.fill('[data-testid="email"]', 'test@example.com');
      await page.fill('[data-testid="password"]', 'correctpassword');
      await page.click('[data-testid="login-btn"]');
      
      // Should show account locked message
      await expect(page.locator('[data-testid="account-locked"]')).toBeVisible();
      await expect(page.locator('[data-testid="account-locked"]')).toContainText('too many failed attempts');
    });

    test('should enforce strong password requirements', async ({ page }) => {
      await page.goto('/register');
      
      // Test weak passwords
      const weakPasswords = [
        '123456',
        'password',
        'qwerty',
        'abc123',
        'Password1' // Missing special characters
      ];
      
      for (const weakPassword of weakPasswords) {
        await page.fill('[data-testid="email"]', 'newuser@example.com');
        await page.fill('[data-testid="password"]', weakPassword);
        await page.fill('[data-testid="confirm-password"]', weakPassword);
        await page.click('[data-testid="register-btn"]');
        
        // Should show password strength error
        await expect(page.locator('[data-testid="password-strength-error"]')).toBeVisible();
        await expect(page.locator('[data-testid="password-strength-error"]')).toContainText(/weak|requirements/i);
      }
      
      // Test strong password
      await page.fill('[data-testid="password"]', 'StrongP@ssw0rd123!');
      await page.fill('[data-testid="confirm-password"]', 'StrongP@ssw0rd123!');
      await page.click('[data-testid="register-btn"]');
      
      // Should not show password error
      await expect(page.locator('[data-testid="password-strength-error"]')).not.toBeVisible();
    });

    test('should implement secure session management', async ({ page, context }) => {
      const loginPage = new LoginPage(page);
      await loginPage.goto();
      await loginPage.loginAsAdmin();
      
      // Get session cookies
      const cookies = await context.cookies();
      const sessionCookie = cookies.find(cookie => cookie.name.includes('session') || cookie.name.includes('auth'));
      
      if (sessionCookie) {
        // Session cookie should be secure
        expect(sessionCookie.secure).toBe(true);
        expect(sessionCookie.httpOnly).toBe(true);
        expect(sessionCookie.sameSite).toBe('Strict');
        
        // Should have reasonable expiration
        expect(sessionCookie.expires).toBeGreaterThan(Date.now() / 1000);
        expect(sessionCookie.expires).toBeLessThan((Date.now() / 1000) + (24 * 60 * 60)); // Less than 24 hours
      }
      
      // Test session timeout
      await page.goto('/dashboard');
      await expect(page.locator('[data-testid="internal-kpi-dashboard"]')).toBeVisible();
      
      // Simulate session expiry by clearing cookies
      await context.clearCookies();
      
      // Should redirect to login when accessing protected resource
      await page.goto('/dashboard');
      await expect(page.locator('[data-testid="login-form"]')).toBeVisible();
    });

    test('should prevent concurrent sessions from same user', async ({ browser }) => {
      // Create two browser contexts for same user
      const context1 = await browser.newContext();
      const context2 = await browser.newContext();
      
      const page1 = await context1.newPage();
      const page2 = await context2.newPage();
      
      const login1 = new LoginPage(page1);
      const login2 = new LoginPage(page2);
      
      // Login with same credentials in both contexts
      await login1.goto();
      await login1.loginAsAdmin();
      
      await page1.goto('/dashboard');
      await expect(page1.locator('[data-testid="internal-kpi-dashboard"]')).toBeVisible();
      
      // Login same user in second context
      await login2.goto();
      await login2.loginAsAdmin();
      
      await page2.goto('/dashboard');
      await expect(page2.locator('[data-testid="internal-kpi-dashboard"]')).toBeVisible();
      
      // First session should be invalidated
      await page1.reload();
      await expect(page1.locator('[data-testid="session-expired"]')).toBeVisible();
      
      await context1.close();
      await context2.close();
    });
  });

  test.describe('Authorization Security', () => {
    test('should enforce role-based access control', async ({ page }) => {
      const loginPage = new LoginPage(page);
      await loginPage.goto();
      
      // Test client user trying to access admin resources
      await loginPage.loginAsClient();
      
      // Try to access admin dashboard
      await page.goto('/admin/dashboard');
      await expect(page.locator('[data-testid="access-denied"]')).toBeVisible();
      await expect(page.locator('[data-testid="access-denied"]')).toContainText('unauthorized');
      
      // Try to access admin API endpoints
      const response = await page.request.get('/api/admin/users');
      expect(response.status()).toBe(403);
      
      // Try to access other clients' data
      const clientDataResponse = await page.request.get('/api/clients/999'); // Different client ID
      expect(clientDataResponse.status()).toBe(403);
    });

    test('should prevent privilege escalation', async ({ page }) => {
      const loginPage = new LoginPage(page);
      await loginPage.goto();
      await loginPage.loginAsAssociate();
      
      // Associate should not be able to modify admin settings
      const adminSettingsResponse = await page.request.put('/api/admin/settings', {
        data: { maintenanceMode: true }
      });
      expect(adminSettingsResponse.status()).toBe(403);
      
      // Associate should not be able to create other associates
      const createUserResponse = await page.request.post('/api/admin/users', {
        data: {
          email: 'newassociate@example.com',
          role: 'Associate'
        }
      });
      expect(createUserResponse.status()).toBe(403);
      
      // Associate should not access system logs
      await page.goto('/admin/logs');
      await expect(page.locator('[data-testid="access-denied"]')).toBeVisible();
    });

    test('should validate API endpoint permissions', async ({ page }) => {
      const loginPage = new LoginPage(page);
      await loginPage.goto();
      await loginPage.loginAsClient();
      
      // Test various API endpoints that should be restricted
      const restrictedEndpoints = [
        { method: 'GET', url: '/api/admin/users', expectedStatus: 403 },
        { method: 'GET', url: '/api/admin/audit-logs', expectedStatus: 403 },
        { method: 'POST', url: '/api/admin/clients', expectedStatus: 403 },
        { method: 'DELETE', url: '/api/clients/1', expectedStatus: 403 },
        { method: 'GET', url: '/api/kpi/internal', expectedStatus: 403 },
        { method: 'POST', url: '/api/reports/admin', expectedStatus: 403 }
      ];
      
      for (const endpoint of restrictedEndpoints) {
        const response = await page.request.fetch(endpoint.url, {
          method: endpoint.method
        });
        expect(response.status()).toBe(endpoint.expectedStatus);
      }
    });
  });

  test.describe('Input Validation and XSS Prevention', () => {
    test('should prevent XSS attacks in form inputs', async ({ page }) => {
      const loginPage = new LoginPage(page);
      await loginPage.goto();
      await loginPage.loginAsAdmin();
      
      // Test XSS payloads in client creation form
      await page.goto('/clients/new');
      
      const xssPayloads = [
        '<script>alert("XSS")</script>',
        'javascript:alert("XSS")',
        '<img src="x" onerror="alert(\'XSS\')">',
        '"><script>alert("XSS")</script>',
        'data:text/html;base64,PHNjcmlwdD5hbGVydCgiWFNTIik8L3NjcmlwdD4='
      ];
      
      for (const payload of xssPayloads) {
        await page.fill('[data-testid="company-name"]', payload);
        await page.fill('[data-testid="email"]', 'test@example.com');
        await page.selectOption('[data-testid="taxpayer-category"]', 'Small');
        await page.click('[data-testid="submit-client"]');
        
        // Check that payload is escaped in display
        await page.waitForTimeout(1000);
        
        // Should not execute script - check page title hasn't changed
        const title = await page.title();
        expect(title).not.toContain('XSS');
        
        // Script tags should be escaped in DOM
        const companyNameDisplay = await page.locator('[data-testid="company-name-display"]').textContent();
        if (companyNameDisplay) {
          expect(companyNameDisplay).not.toContain('<script>');
          expect(companyNameDisplay).toContain('&lt;script&gt;');
        }
      }
    });

    test('should sanitize rich text editor content', async ({ page }) => {
      const loginPage = new LoginPage(page);
      await loginPage.goto();
      await loginPage.loginAsClient();
      
      await page.goto('/client-portal/documents');
      
      // Test malicious content in document description
      const maliciousContent = `
        <script>alert('XSS')</script>
        <iframe src="javascript:alert('XSS')"></iframe>
        <img src="x" onerror="alert('XSS')">
        <div onclick="alert('XSS')">Click me</div>
      `;
      
      await page.fill('[data-testid="document-description"]', maliciousContent);
      
      // Submit and check sanitization
      const fileInput = page.locator('[data-testid="document-upload-input"]');
      await fileInput.setInputFiles({
        name: 'test.pdf',
        mimeType: 'application/pdf',
        buffer: Buffer.from('Test content')
      });
      
      await page.click('[data-testid="upload-document"]');
      
      // Check that description is sanitized
      const sanitizedDescription = await page.locator('[data-testid="document-description-display"]').innerHTML();
      expect(sanitizedDescription).not.toContain('<script>');
      expect(sanitizedDescription).not.toContain('<iframe>');
      expect(sanitizedDescription).not.toContain('onclick');
      expect(sanitizedDescription).not.toContain('javascript:');
    });

    test('should validate file uploads', async ({ page }) => {
      const loginPage = new LoginPage(page);
      await loginPage.goto();
      await loginPage.loginAsClient();
      
      await page.goto('/client-portal/documents');
      
      const fileInput = page.locator('[data-testid="document-upload-input"]');
      
      // Test malicious file types
      const maliciousFiles = [
        { name: 'malware.exe', content: 'MZ\x90\x00' }, // PE header
        { name: 'script.js', content: 'alert("XSS")' },
        { name: 'shell.php', content: '<?php system($_GET["cmd"]); ?>' },
        { name: 'huge-file.pdf', content: 'A'.repeat(100 * 1024 * 1024) } // 100MB file
      ];
      
      for (const file of maliciousFiles) {
        await fileInput.setInputFiles({
          name: file.name,
          mimeType: 'application/octet-stream',
          buffer: Buffer.from(file.content)
        });
        
        await page.click('[data-testid="upload-document"]');
        
        // Should show appropriate error
        if (file.name.endsWith('.exe') || file.name.endsWith('.js') || file.name.endsWith('.php')) {
          await expect(page.locator('[data-testid="file-type-error"]')).toBeVisible();
        } else if (file.content.length > 50 * 1024 * 1024) {
          await expect(page.locator('[data-testid="file-size-error"]')).toBeVisible();
        }
      }
    });
  });

  test.describe('SQL Injection Prevention', () => {
    test('should prevent SQL injection in search queries', async ({ page }) => {
      const loginPage = new LoginPage(page);
      await loginPage.goto();
      await loginPage.loginAsAdmin();
      
      await page.goto('/clients');
      
      // Test SQL injection payloads in search
      const sqlPayloads = [
        "'; DROP TABLE clients; --",
        "' OR '1'='1",
        "' UNION SELECT * FROM users --",
        "'; INSERT INTO clients (name) VALUES ('hacked'); --",
        "' OR 1=1; DELETE FROM clients; --"
      ];
      
      for (const payload of sqlPayloads) {
        await page.fill('[data-testid="client-search"]', payload);
        await page.waitForTimeout(1000); // Wait for search debounce
        
        // Should not cause database errors or expose data
        await expect(page.locator('[data-testid="sql-error"]')).not.toBeVisible();
        await expect(page.locator('[data-testid="database-error"]')).not.toBeVisible();
        
        // Should return empty results or escaped search
        const results = page.locator('[data-testid="search-results"]');
        if (await results.isVisible()) {
          const resultText = await results.textContent();
          expect(resultText).not.toContain('hacked');
          expect(resultText).not.toContain('users');
        }
      }
    });

    test('should validate API parameters against SQL injection', async ({ page }) => {
      const loginPage = new LoginPage(page);
      await loginPage.goto();
      await loginPage.loginAsAdmin();
      
      // Test SQL injection in API parameters
      const maliciousParams = [
        "1'; DROP TABLE clients; --",
        "1 OR 1=1",
        "1 UNION SELECT password FROM users",
        "-1 OR 1=1"
      ];
      
      for (const param of maliciousParams) {
        // Test various API endpoints with malicious parameters
        const response = await page.request.get(`/api/clients/${encodeURIComponent(param)}`);
        
        // Should return 400 Bad Request or 404 Not Found, not 500 Server Error
        expect([400, 404]).toContain(response.status());
        
        const responseBody = await response.text();
        
        // Response should not contain database schema information
        expect(responseBody.toLowerCase()).not.toContain('table');
        expect(responseBody.toLowerCase()).not.toContain('column');
        expect(responseBody.toLowerCase()).not.toContain('database');
        expect(responseBody.toLowerCase()).not.toContain('password');
      }
    });
  });

  test.describe('CSRF Protection', () => {
    test('should prevent CSRF attacks', async ({ page, context }) => {
      const loginPage = new LoginPage(page);
      await loginPage.goto();
      await loginPage.loginAsAdmin();
      
      // Get CSRF token from legitimate form
      await page.goto('/clients/new');
      const csrfToken = await page.locator('[name="__RequestVerificationToken"]').getAttribute('value');
      
      // Test request without CSRF token
      const responseWithoutToken = await page.request.post('/api/clients', {
        data: {
          companyName: 'CSRF Test Company',
          email: 'csrf@example.com',
          taxpayerCategory: 'Small'
        }
      });
      
      // Should be rejected
      expect(responseWithoutToken.status()).toBe(400);
      
      // Test request with invalid CSRF token
      const responseWithInvalidToken = await page.request.post('/api/clients', {
        data: {
          companyName: 'CSRF Test Company',
          email: 'csrf@example.com',
          taxpayerCategory: 'Small'
        },
        headers: {
          'X-CSRF-Token': 'invalid-token'
        }
      });
      
      expect(responseWithInvalidToken.status()).toBe(400);
      
      // Test legitimate request with valid token
      if (csrfToken) {
        const responseWithValidToken = await page.request.post('/api/clients', {
          data: {
            companyName: 'CSRF Test Company',
            email: 'csrf@example.com',
            taxpayerCategory: 'Small'
          },
          headers: {
            'X-CSRF-Token': csrfToken
          }
        });
        
        expect(responseWithValidToken.status()).toBe(201);
      }
    });
  });

  test.describe('Data Encryption and Privacy', () => {
    test('should encrypt sensitive data in transit', async ({ page }) => {
      const loginPage = new LoginPage(page);
      await loginPage.goto();
      
      // Verify HTTPS is enforced
      expect(page.url()).toMatch(/^https:/);
      
      // Monitor network requests for sensitive data
      const sensitiveDataExposed: string[] = [];
      
      page.on('request', request => {
        const url = request.url();
        const postData = request.postData();
        
        if (postData) {
          // Check if sensitive data is transmitted in plain text
          const sensitivePatterns = [
            /password/i,
            /ssn/i,
            /tax.*id/i,
            /bank.*account/i,
            /\d{4}-\d{4}-\d{4}-\d{4}/, // Credit card pattern
            /\d{3}-\d{2}-\d{4}/ // SSN pattern
          ];
          
          for (const pattern of sensitivePatterns) {
            if (pattern.test(postData)) {
              sensitiveDataExposed.push(`${pattern} in ${url}`);
            }
          }
        }
      });
      
      // Perform operations that involve sensitive data
      await loginPage.loginAsClient();
      await page.goto('/client-portal/profile');
      
      // Update profile with sensitive information
      await page.fill('[data-testid="tax-id"]', '123-45-6789');
      await page.fill('[data-testid="bank-account"]', '1234567890');
      await page.click('[data-testid="save-profile"]');
      
      // Make payment with financial data
      await page.goto('/client-portal/payments');
      await page.click('[data-testid="new-payment-btn"]');
      await page.fill('[data-testid="payment-amount"]', '50000');
      await page.click('[data-testid="payment-method-bank-transfer"]');
      await page.fill('[data-testid="account-number"]', '9876543210');
      await page.click('[data-testid="submit-payment-btn"]');
      
      // Should not expose sensitive data in plain text
      expect(sensitiveDataExposed).toHaveLength(0);
    });

    test('should implement proper data masking', async ({ page }) => {
      const loginPage = new LoginPage(page);
      await loginPage.goto();
      await loginPage.loginAsAdmin();
      
      // Navigate to client details page
      await page.goto('/clients/1');
      
      // Sensitive data should be masked
      const taxIdElement = page.locator('[data-testid="tax-id-display"]');
      if (await taxIdElement.isVisible()) {
        const taxIdText = await taxIdElement.textContent();
        // Should show only last 4 digits: ***-**-1234
        expect(taxIdText).toMatch(/\*+\d{4}$/);
      }
      
      const bankAccountElement = page.locator('[data-testid="bank-account-display"]');
      if (await bankAccountElement.isVisible()) {
        const bankAccountText = await bankAccountElement.textContent();
        // Should be partially masked: ****5678
        expect(bankAccountText).toMatch(/\*+\d{4}$/);
      }
    });
  });

  test.describe('Audit Trail Security', () => {
    test('should log all security-relevant events', async ({ page }) => {
      const loginPage = new LoginPage(page);
      await loginPage.goto();
      
      // Test failed login attempt
      await page.fill('[data-testid="email"]', 'test@example.com');
      await page.fill('[data-testid="password"]', 'wrongpassword');
      await page.click('[data-testid="login-btn"]');
      
      // Successful login
      await loginPage.loginAsAdmin();
      
      // Access admin audit logs
      await page.goto('/admin/audit-logs');
      
      // Should show security events
      await expect(page.locator('[data-testid="audit-logs-table"]')).toBeVisible();
      
      // Look for login failure event
      const loginFailureLog = page.locator('[data-testid="log-event-login-failure"]');
      if (await loginFailureLog.isVisible()) {
        await expect(loginFailureLog).toContainText('Failed login attempt');
        await expect(loginFailureLog).toContainText('test@example.com');
      }
      
      // Look for successful login event
      const loginSuccessLog = page.locator('[data-testid="log-event-login-success"]');
      if (await loginSuccessLog.isVisible()) {
        await expect(loginSuccessLog).toContainText('Successful login');
      }
    });

    test('should detect and log suspicious activities', async ({ page }) => {
      const loginPage = new LoginPage(page);
      await loginPage.goto();
      await loginPage.loginAsAdmin();
      
      // Perform suspicious activities
      // 1. Rapid API calls
      for (let i = 0; i < 10; i++) {
        await page.request.get('/api/clients');
      }
      
      // 2. Access multiple client records quickly
      for (let i = 1; i <= 5; i++) {
        await page.goto(`/clients/${i}`);
        await page.waitForTimeout(100);
      }
      
      // 3. Attempt to access restricted resources
      await page.goto('/api/admin/system-info');
      await page.goto('/api/admin/database-backup');
      
      // Check audit logs for suspicious activity flags
      await page.goto('/admin/audit-logs');
      await page.selectOption('[data-testid="log-filter-severity"]', 'High');
      
      // Should flag rapid access patterns
      const suspiciousActivityLog = page.locator('[data-testid="log-event-suspicious-activity"]');
      if (await suspiciousActivityLog.isVisible()) {
        await expect(suspiciousActivityLog).toContainText('Suspicious activity detected');
      }
    });
  });

  test.describe('Rate Limiting and DDoS Protection', () => {
    test('should implement rate limiting on API endpoints', async ({ page }) => {
      const loginPage = new LoginPage(page);
      await loginPage.goto();
      await loginPage.loginAsClient();
      
      // Make rapid API requests to trigger rate limiting
      const responses: number[] = [];
      
      for (let i = 0; i < 20; i++) {
        const response = await page.request.get('/api/client/dashboard');
        responses.push(response.status());
        
        if (response.status() === 429) {
          break; // Rate limit hit
        }
      }
      
      // Should eventually return 429 Too Many Requests
      expect(responses).toContain(429);
      
      // Verify rate limit headers
      const rateLimitedResponse = await page.request.get('/api/client/dashboard');
      const headers = rateLimitedResponse.headers();
      
      if (rateLimitedResponse.status() === 429) {
        expect(headers['x-ratelimit-limit']).toBeDefined();
        expect(headers['x-ratelimit-remaining']).toBeDefined();
        expect(headers['retry-after']).toBeDefined();
      }
    });

    test('should handle high-frequency login attempts', async ({ page }) => {
      const loginPage = new LoginPage(page);
      
      // Simulate high-frequency login attempts
      for (let i = 0; i < 10; i++) {
        await loginPage.goto();
        await page.fill('[data-testid="email"]', 'attacker@example.com');
        await page.fill('[data-testid="password"]', `password${i}`);
        await page.click('[data-testid="login-btn"]');
        
        // Check if rate limiting kicks in
        const rateLimitError = page.locator('[data-testid="rate-limit-error"]');
        if (await rateLimitError.isVisible()) {
          await expect(rateLimitError).toContainText('too many attempts');
          break;
        }
      }
      
      // Should block further attempts
      await page.fill('[data-testid="email"]', 'attacker@example.com');
      await page.fill('[data-testid="password"]', 'anypassword');
      await page.click('[data-testid="login-btn"]');
      
      await expect(page.locator('[data-testid="rate-limit-error"]')).toBeVisible();
    });
  });

  test.describe('Content Security Policy', () => {
    test('should implement proper CSP headers', async ({ page }) => {
      await page.goto('/');
      
      // Check CSP headers in response
      const response = await page.request.get('/');
      const headers = response.headers();
      
      // Should have CSP header
      expect(headers['content-security-policy'] || headers['content-security-policy-report-only']).toBeDefined();
      
      const csp = headers['content-security-policy'] || headers['content-security-policy-report-only'];
      
      if (csp) {
        // Should restrict inline scripts
        expect(csp).toContain("script-src");
        expect(csp).not.toContain("'unsafe-inline'");
        
        // Should restrict object sources
        expect(csp).toContain("object-src 'none'");
        
        // Should have base URI restrictions
        expect(csp).toContain("base-uri");
      }
    });

    test('should block inline script execution', async ({ page }) => {
      const loginPage = new LoginPage(page);
      await loginPage.goto();
      await loginPage.loginAsAdmin();
      
      // Try to inject inline script via DOM manipulation
      const scriptExecuted = await page.evaluate(() => {
        try {
          const script = document.createElement('script');
          script.innerHTML = 'window.xssExecuted = true;';
          document.head.appendChild(script);
          return (window as any).xssExecuted === true;
        } catch (error) {
          return false;
        }
      });
      
      // CSP should prevent inline script execution
      expect(scriptExecuted).toBe(false);
    });
  });
});