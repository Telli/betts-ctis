# Security Testing Guide

This guide provides instructions for running and validating the security fixes implemented in the Betts Tax CTIS application.

## Table of Contents

1. [Prerequisites](#prerequisites)
2. [Running Backend Tests](#running-backend-tests)
3. [Running Frontend Tests](#running-frontend-tests)
4. [Manual Security Testing](#manual-security-testing)
5. [Security Checklist](#security-checklist)

---

## Prerequisites

### Backend (C#)
- .NET SDK 6.0 or later
- xUnit test runner
- NuGet packages: `xunit`, `Moq`, `Microsoft.Extensions.Logging.Abstractions`

### Frontend (TypeScript/React)
- Node.js 16+ and npm/yarn
- Vit test framework
- Testing dependencies installed

---

## Running Backend Tests

### Authentication Tests

The authentication service tests verify:
- Valid credential acceptance
- Invalid credential rejection
- Empty field validation
- JWT token generation and validation
- Role-based user information extraction

```bash
# Navigate to test project
cd BettsTax/BettsTax.Tests

# Run all authentication tests
dotnet test --filter "FullyQualifiedName~AuthenticationServiceTests"

# Run specific test
dotnet test --filter "FullyQualifiedName~AuthenticationServiceTests.AuthenticateAsync_ValidCredentials_ReturnsSuccess"
```

### Authorization Tests

The authorization service tests verify:
- Admin/Staff access to all clients
- Client access restricted to own data
- Unauthorized access prevention and logging
- Role detection (case-insensitive)
- Client ID extraction from claims

```bash
# Run all authorization tests
dotnet test --filter "FullyQualifiedName~AuthorizationServiceTests"

# Run specific authorization test
dotnet test --filter "FullyQualifiedName~AuthorizationServiceTests.CanAccessClientData_ClientUser_CannotAccessOtherClientData"
```

### Run All Backend Security Tests

```bash
cd BettsTax/BettsTax.Tests
dotnet test --filter "FullyQualifiedName~Security"
```

---

## Running Frontend Tests

### XSS Prevention Tests

The chart sanitization tests verify:
- Valid CSS color acceptance
- Malicious script injection blocking
- HTML tag prevention
- JavaScript protocol blocking
- CSS variable key sanitization

```bash
# Navigate to frontend project
cd "Client Tax Information System"

# Install dependencies (if not already installed)
npm install

# Run security tests
npm test -- src/tests/security/chartSanitization.test.ts

# Run with coverage
npm test -- --coverage src/tests/security/
```

---

## Manual Security Testing

### 1. Authentication Testing

#### Test Invalid Credentials

1. Navigate to the login page
2. Enter invalid credentials:
   - Email: `test@example.com`
   - Password: `wrongpassword`
3. **Expected**: Error message "Invalid email or password"
4. **Verify**: No token stored in localStorage

#### Test Valid Credentials

1. Enter valid credentials:
   - Email: `staff@bettsfirm.com`
   - Password: `password`
2. **Expected**: Successful login, token stored in localStorage
3. **Verify**: Check browser DevTools > Application > Local Storage for `auth_token`

#### Test Empty Fields

1. Leave email or password empty
2. Submit form
3. **Expected**: Validation error before submission

### 2. Authorization Testing

#### Test Client Data Isolation

**Prerequisites**:
- Two browser windows/profiles OR private browsing mode
- Demo accounts for two different clients

**Steps**:

1. **Window 1**: Login as `client@example.com` (ClientId: 1)
   - Navigate to API endpoint (if available): `/api/deadlines?clientId=1`
   - **Expected**: Success - shows Client 1's data

2. **Window 1**: Try to access another client's data
   - Navigate to: `/api/deadlines?clientId=2`
   - **Expected**: 403 Forbidden error with message "Access denied"

3. **Window 2**: Login as Staff (`staff@bettsfirm.com`)
   - Navigate to: `/api/deadlines?clientId=1`
   - **Expected**: Success - staff can access client data
   - Navigate to: `/api/deadlines?clientId=2`
   - **Expected**: Success - staff can access any client data

#### Test Token Validation

1. Login successfully
2. Open browser DevTools > Application > Local Storage
3. Modify the `auth_token` value (change a few characters)
4. Try to access a protected API endpoint
5. **Expected**: 401 Unauthorized error

### 3. XSS Prevention Testing

#### Test Chart Component

1. **If you have access to chart configuration**:
   - Try to inject malicious color value:
     ```javascript
     {
       color: 'red;}</style><script>alert("XSS")</script>'
     }
     ```
   - **Expected**: Color is sanitized to empty string, no script execution

2. **Test CSS Variable Injection**:
   ```javascript
   {
     'my-color";onerror="alert(1)"': '#ff0000'
   }
   ```
   - **Expected**: Special characters removed from key

### 4. API Security Testing (Using cURL or Postman)

#### Test Unauthenticated Access

```bash
# Try to access protected endpoint without token
curl -X GET http://localhost:5000/api/deadlines/upcoming

# Expected: 401 Unauthorized
```

#### Test With Valid Token

```bash
# 1. Login to get token
curl -X POST http://localhost:5000/api/auth/login \
  -H "Content-Type: application/json" \
  -d '{"email":"staff@bettsfirm.com","password":"password"}'

# 2. Extract token from response and use it
curl -X GET http://localhost:5000/api/deadlines/upcoming \
  -H "Authorization: Bearer YOUR_TOKEN_HERE"

# Expected: 200 OK with data
```

#### Test Client Access Restriction

```bash
# 1. Login as client
curl -X POST http://localhost:5000/api/auth/login \
  -H "Content-Type: application/json" \
  -d '{"email":"client@example.com","password":"password"}'

# 2. Try to access another client's data
curl -X GET "http://localhost:5000/api/deadlines/upcoming?clientId=2" \
  -H "Authorization: Bearer CLIENT_TOKEN_HERE"

# Expected: 403 Forbidden
```

---

## Security Checklist

### Authentication ✓
- [ ] Invalid credentials are rejected
- [ ] Valid credentials generate JWT token
- [ ] Empty fields are validated
- [ ] JWT tokens are properly signed
- [ ] Tokens expire after configured time
- [ ] Token validation works correctly
- [ ] User information is correctly extracted from tokens

### Authorization ✓
- [ ] Clients cannot access other clients' data
- [ ] Staff can access all client data
- [ ] Admin can access all client data
- [ ] Unauthorized access attempts are logged
- [ ] 403 responses include descriptive error messages
- [ ] Client auto-filtering works when no clientId specified

### XSS Prevention ✓
- [ ] Chart component sanitizes CSS colors
- [ ] Script tags are blocked
- [ ] JavaScript protocol is blocked
- [ ] HTML tags are prevented
- [ ] CSS variable keys are sanitized
- [ ] All user inputs in dangerouslySetInnerHTML are sanitized

### General Security ✓
- [ ] Passwords are not logged
- [ ] Sensitive data is not exposed in error messages
- [ ] HTTPS is enforced (production)
- [ ] CORS is properly configured
- [ ] Rate limiting is implemented (if applicable)
- [ ] Security headers are set (production)

---

## Automated Security Testing

### Using GitHub Actions (CI/CD)

Create `.github/workflows/security-tests.yml`:

```yaml
name: Security Tests

on: [push, pull_request]

jobs:
  backend-security:
    runs-on: ubuntu-latest
    steps:
      - uses: actions/checkout@v3
      - name: Setup .NET
        uses: actions/setup-dotnet@v3
        with:
          dotnet-version: '6.0.x'
      - name: Run Security Tests
        run: |
          cd BettsTax/BettsTax.Tests
          dotnet test --filter "FullyQualifiedName~Security"

  frontend-security:
    runs-on: ubuntu-latest
    steps:
      - uses: actions/checkout@v3
      - name: Setup Node.js
        uses: actions/setup-node@v3
        with:
          node-version: '18'
      - name: Install dependencies
        run: |
          cd "Client Tax Information System"
          npm install
      - name: Run Security Tests
        run: |
          cd "Client Tax Information System"
          npm test -- src/tests/security/
```

---

## Reporting Security Issues

If you discover a security vulnerability:

1. **DO NOT** create a public GitHub issue
2. Email security concerns to: security@bettsfirm.com
3. Include:
   - Description of the vulnerability
   - Steps to reproduce
   - Potential impact
   - Suggested fix (if any)

---

## Additional Resources

- [OWASP Testing Guide](https://owasp.org/www-project-web-security-testing-guide/)
- [JWT Security Best Practices](https://tools.ietf.org/html/rfc8725)
- [React Security Best Practices](https://snyk.io/blog/10-react-security-best-practices/)
- [ASP.NET Core Security Documentation](https://docs.microsoft.com/en-us/aspnet/core/security/)

---

**Last Updated**: 2025-11-09
**Version**: 1.0
