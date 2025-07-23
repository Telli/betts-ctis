# Betts CTIS - E2E Testing Guide

This document provides comprehensive guidance for running and maintaining end-to-end tests for the Betts Client Tax Information System (CTIS).

## Test Suite Overview

The E2E test suite uses Playwright to test the complete user journey across both frontend and backend systems, ensuring:

- **Authentication flows** for all user roles (Client, Associate, Admin)
- **Client portal functionality** including document management and profile updates
- **Admin interface** for client management and system administration
- **API integration** testing for all endpoints
- **Accessibility compliance** following WCAG guidelines
- **Cross-browser compatibility** (Chromium, Firefox, WebKit)
- **Mobile responsiveness** testing

## Prerequisites

1. **Backend API** running on `http://localhost:5000`
2. **Frontend app** running on `http://localhost:3000`
3. **Test database** with seeded test users
4. **Playwright** installed and configured

## Installation

```bash
# Install Playwright and dependencies
npm install
npx playwright install

# Install browser dependencies (if needed)
npx playwright install-deps
```

## Test Structure

```
tests/
├── e2e/                          # E2E test specifications
│   ├── auth.spec.ts             # Authentication tests
│   ├── client-portal.spec.ts    # Client portal functionality
│   ├── admin-interface.spec.ts  # Admin interface tests
│   ├── api-integration.spec.ts  # API endpoint tests
│   └── accessibility.spec.ts    # Accessibility compliance
├── page-objects/                 # Page Object Models
│   ├── LoginPage.ts             # Login page interactions
│   └── ClientPortalPage.ts      # Client portal interactions
├── utils/                        # Test utilities and helpers
│   ├── auth-helper.ts           # Authentication helper
│   └── test-data.ts             # Test data and constants
├── global-setup.ts              # Global test setup
├── global-teardown.ts           # Global test cleanup
└── README.md                    # This file
```

## Running Tests

### All Tests
```bash
npm run test:e2e
```

### Specific Test Suites
```bash
# Authentication tests only
npx playwright test auth.spec.ts

# Client portal tests only
npx playwright test client-portal.spec.ts

# Admin interface tests only
npx playwright test admin-interface.spec.ts

# API integration tests only
npx playwright test api-integration.spec.ts

# Accessibility tests only
npx playwright test accessibility.spec.ts
```

### Interactive Mode
```bash
# Run tests with UI (interactive)
npm run test:e2e:ui

# Run tests in headed mode (see browser)
npm run test:e2e:headed

# Debug mode (step through tests)
npm run test:e2e:debug
```

### Browser-Specific Tests
```bash
# Chromium only
npx playwright test --project=chromium

# Firefox only
npx playwright test --project=firefox

# WebKit only
npx playwright test --project=webkit

# Mobile Chrome
npx playwright test --project=Mobile_Chrome

# Mobile Safari
npx playwright test --project=Mobile_Safari
```

## Test Configuration

The test configuration is defined in `playwright.config.ts`:

- **Base URL**: `http://localhost:3000`
- **API URL**: `http://localhost:5000`
- **Timeout**: 30 seconds per test
- **Retries**: 2 retries on CI, 0 locally
- **Parallel**: 1 worker (to avoid conflicts)
- **Screenshots**: On failure
- **Video**: On first retry

## Test Users

The following test users are defined in `tests/utils/test-data.ts`:

```typescript
TEST_USERS = {
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
  }
}
```

## Test Categories

### 1. Authentication Tests (`auth.spec.ts`)

Tests user authentication flows:
- ✅ Login form display and validation
- ✅ Successful login for all user roles
- ✅ Invalid credential rejection
- ✅ Role-based redirects
- ✅ Session management
- ✅ Security validations

### 2. Client Portal Tests (`client-portal.spec.ts`)

Tests client-specific functionality:
- ✅ Dashboard display and navigation
- ✅ Document management (upload/download)
- ✅ Profile management and updates
- ✅ Tax filings and payment history
- ✅ Compliance status tracking
- ✅ Data isolation between clients
- ✅ Responsive design testing

### 3. Admin Interface Tests (`admin-interface.spec.ts`)

Tests admin and associate functionality:
- ✅ Admin dashboard access
- ✅ Client management interface
- ✅ Statistics and reporting
- ✅ Document review workflows
- ✅ Role-based access control
- ✅ System settings (admin only)

### 4. API Integration Tests (`api-integration.spec.ts`)

Tests backend API endpoints:
- ✅ Authentication API endpoints
- ✅ Client portal API endpoints
- ✅ Admin API endpoints
- ✅ Data isolation validation
- ✅ Error handling and validation
- ✅ Performance testing

### 5. Accessibility Tests (`accessibility.spec.ts`)

Tests WCAG compliance:
- ✅ Keyboard navigation
- ✅ Screen reader support
- ✅ Focus management
- ✅ Color contrast and visibility
- ✅ Alternative text for images
- ✅ Mobile accessibility
- ✅ Error message accessibility

## Page Object Models

### LoginPage (`page-objects/LoginPage.ts`)
Handles login page interactions:
- `goto()` - Navigate to login page
- `login(email, password)` - Perform login
- `expectLoginForm()` - Verify form is displayed
- `expectValidationError()` - Check validation messages

### ClientPortalPage (`page-objects/ClientPortalPage.ts`)
Handles client portal interactions:
- `gotoDashboard()` - Navigate to dashboard
- `uploadDocument()` - Upload a document
- `updateProfile()` - Update profile information
- `clickSidebarItem()` - Navigate via sidebar

## Test Utilities

### AuthHelper (`utils/auth-helper.ts`)
Provides authentication utilities:
- `login(userType)` - Login as specific user type
- `logout()` - Logout current user
- `expectLoggedIn()` - Verify user is logged in
- `setupAuthState()` - Save authentication state

### Test Data (`utils/test-data.ts`)
Contains test constants:
- `TEST_USERS` - Test user credentials
- `ROUTES` - Application routes
- `SELECTORS` - CSS/Test ID selectors
- `TIMEOUTS` - Test timeout values

## Best Practices

### 1. Test Independence
- Each test should be independent and isolated
- Use `beforeEach` to set up clean state
- Don't rely on test execution order

### 2. Data Management
- Use test-specific data that won't conflict
- Clean up test data when necessary
- Avoid modifying shared test data

### 3. Selectors
- Use `data-testid` attributes for stable selectors
- Avoid CSS selectors that may change
- Use semantic selectors when possible

### 4. Assertions
- Use specific assertions (`toHaveText` vs `toContain`)
- Add timeout handling for async operations
- Provide meaningful error messages

### 5. Error Handling
- Handle expected errors gracefully
- Use `test.skip()` for tests requiring specific setup
- Add retries for flaky tests

## Debugging Tests

### Visual Debugging
```bash
# Run with browser visible
npm run test:e2e:headed

# Interactive debugging
npm run test:e2e:debug

# Step through specific test
npx playwright test auth.spec.ts --debug
```

### Screenshots and Videos
- Screenshots are captured on test failure
- Videos are recorded on first retry
- Output saved to `test-results/` directory

### Logs and Traces
```bash
# Enable trace collection
npx playwright test --trace on

# View trace in browser
npx playwright show-trace trace.zip
```

## Continuous Integration

### GitHub Actions Setup
```yaml
- name: Install dependencies
  run: npm ci

- name: Install Playwright
  run: npx playwright install --with-deps

- name: Start backend
  run: |
    cd BettsTax/BettsTax.Web
    dotnet run &
    
- name: Start frontend
  run: |
    npm run dev &
    
- name: Run E2E tests
  run: npm run test:e2e
```

### Environment Variables
```bash
# Test environment configuration
PLAYWRIGHT_BASE_URL=http://localhost:3000
API_BASE_URL=http://localhost:5000
TEST_DATABASE_URL=postgresql://test_db
```

## Maintenance and Updates

### Adding New Tests
1. Create test file in appropriate category
2. Use existing page objects when possible
3. Add new selectors to `test-data.ts`
4. Follow naming conventions

### Updating Selectors
1. Update `SELECTORS` in `test-data.ts`
2. Update page object methods
3. Run full test suite to verify changes

### Test Data Updates
1. Update test users in backend seeding
2. Update `TEST_USERS` in `test-data.ts`
3. Verify all tests pass with new data

## Troubleshooting

### Common Issues

**Tests timing out:**
- Increase timeout values in `playwright.config.ts`
- Check if servers are running
- Verify network connectivity

**Element not found:**
- Check if selectors have changed
- Verify page is fully loaded
- Add explicit waits for dynamic content

**Authentication failures:**
- Verify test user credentials
- Check if backend is seeded with test data
- Ensure JWT configuration is correct

**Flaky tests:**
- Add explicit waits for async operations
- Use `waitForLoadState('networkidle')`
- Increase retry count for unstable tests

### Getting Help

1. Check test logs and screenshots
2. Run individual tests to isolate issues
3. Use debug mode to step through tests
4. Review recent changes to application code

## Performance Considerations

- Tests run in parallel when possible
- Use shared authentication state to reduce login overhead
- Skip unnecessary setup in tests
- Optimize selectors for speed

## Security Testing

The test suite includes security validations:
- Password handling and storage
- JWT token validation
- Role-based access control
- Session management
- Data isolation between clients

This ensures the Betts CTIS application maintains security standards across all user interactions.