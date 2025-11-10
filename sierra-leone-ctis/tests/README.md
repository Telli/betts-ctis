# Playwright E2E Testing Guide

## Overview

This directory contains comprehensive end-to-end tests using Playwright to catch and prevent regressions in the CTIS application.

## Recent Regression Tests

The `regression-fixes.spec.ts` file contains tests for recently fixed issues:

### Issues Tested

1. **Client Creation with Enum Values** ✅
   - Validates numeric enum values (clientType, taxpayerCategory, status)
   - Ensures form submits with correct data types
   - Prevents 400 API errors from type mismatches

2. **Table Filtering & Sorting** ✅
   - Tests filtering by taxpayer category
   - Tests sorting by business name
   - Validates column keys match backend DTO fields
   - Prevents `charAt is not a function` errors

3. **SelectItem Empty Value Validation** ✅
   - Ensures no SelectItem components have empty string values
   - Prevents React Select errors

4. **API Error Handling** ✅
   - Tests client detail page (404 prevention)
   - Tests tax filings page (500 prevention)
   - Validates circular reference JSON serialization

5. **Status Badge Display** ✅
   - Tests numeric enum to string conversion
   - Validates badge display without errors

6. **Form Input Handling** ✅
   - Annual turnover input without leading zero
   - Enum string to number conversion before submission

## Running Tests

### Prerequisites

1. **Backend must be running**:
   ```bash
   cd c:/Users/telli/Desktop/Betts/Betts/BettsTax/BettsTax.Web
   dotnet run
   ```

2. **Frontend will auto-start** (configured in `playwright.config.ts`)

### Test Commands

```bash
# Run all regression tests
npm run test:regression

# Run with browser visible
npm run test:regression:headed

# Debug mode with Playwright Inspector
npm run test:regression:debug

# Run all E2E tests
npm run test:e2e

# Run with UI Mode (recommended for development)
npm run test:e2e:ui

# Run specific test file
npx playwright test tests/e2e/regression-fixes.spec.ts

# Run specific test by name
npx playwright test -g "should create client with numeric enum values"

# Run in headed mode for all browsers
npm run test:e2e:headed

# View test report
npm run test:e2e:report
```

### CI/CD Integration

```bash
# Run tests in CI mode (with retries)
CI=true npm run test:e2e
```

## Test Structure

```
tests/
├── e2e/
│   ├── regression-fixes.spec.ts    # Regression tests for recent fixes
│   ├── admin-interface.spec.ts     # Admin features
│   ├── client-portal.spec.ts       # Client portal
│   └── ...other test files
├── helpers/
│   └── auth.ts                     # Authentication helpers
├── global-setup.ts                 # Global test setup
└── global-teardown.ts              # Global test cleanup
```

## Demo User Credentials

Tests use these credentials (from backend seed data):

- **Admin**: `admin@bettsfirm.sl` / `Admin123!`
- **Associate**: `associate@bettsfirm.sl` / `Associate123!`
- **Client**: `client@testcompany.sl` / `Client123!`
- **System Admin**: `admin@thebettsfirmsl.com` / `AdminPass123!`

## Writing New Tests

### Template

```typescript
import { test, expect } from '@playwright/test';
import { login } from '../helpers/auth';

test.describe('Feature Name', () => {
  test.beforeEach(async ({ page }) => {
    await login(page, 'admin');
  });

  test('should do something', async ({ page }) => {
    await page.goto('/some-page');
    
    // Your test assertions
    await expect(page.locator('text=Expected')).toBeVisible();
  });
});
```

### Best Practices

1. **Use helpers**: Import and use helper functions from `helpers/`
2. **Wait strategically**: Use `waitForSelector` instead of arbitrary timeouts
3. **Check console errors**: Monitor console for runtime errors
4. **Network monitoring**: Listen for API responses to validate data
5. **Cleanup**: Tests should be independent and not affect each other

## Debugging Failed Tests

### View Trace

```bash
npx playwright show-trace test-results/[test-name]/trace.zip
```

### Screenshots

Failed tests automatically capture screenshots in `test-results/`

### Videos

Failed tests record video in `test-results/`

### Debug Mode

```bash
npm run test:e2e:debug
```

This opens Playwright Inspector where you can:
- Step through test execution
- Inspect elements
- View console logs
- Debug network requests

## Common Issues

### Backend Not Running

```
Error: page.goto: net::ERR_CONNECTION_REFUSED
```

**Solution**: Start the backend server:
```bash
cd BettsTax/BettsTax.Web
dotnet run
```

### Test Data Issues

If tests fail due to missing data:

1. Stop backend
2. Delete `BettsTax.db*` files
3. Restart backend (auto-seeds demo data)

### Timeout Issues

Increase timeout in test or `playwright.config.ts`:

```typescript
test.setTimeout(120000); // 2 minutes
```

## Continuous Monitoring

### Watch Mode

```bash
npx playwright test --watch
```

Automatically reruns tests when files change.

### Parallel Execution

Tests run in parallel by default. Adjust workers in `playwright.config.ts`:

```typescript
workers: process.env.CI ? 1 : 4
```

## Integration with Development Workflow

### Pre-commit Hook (Recommended)

Add to `.git/hooks/pre-commit`:

```bash
#!/bin/sh
npm run test:regression
```

### GitHub Actions (Example)

```yaml
name: E2E Tests
on: [push, pull_request]
jobs:
  test:
    runs-on: windows-latest
    steps:
      - uses: actions/checkout@v3
      - uses: actions/setup-node@v3
      - run: npm ci
      - run: npx playwright install
      - run: npm run test:e2e
      - uses: actions/upload-artifact@v3
        if: always()
        with:
          name: playwright-report
          path: playwright-report/
```

## Reporting Bugs Found by Tests

When a test catches a bug:

1. **Document it**: Add details to `FIXES_APPLIED.md`
2. **Create regression test**: Add test case to prevent future occurrence
3. **Tag PR**: Link PR to test that validates the fix

## Test Coverage

Current coverage areas:
- ✅ Authentication & Authorization
- ✅ Client CRUD operations
- ✅ Table filtering/sorting/search
- ✅ Form validation
- ✅ API error handling
- ✅ Status badge display
- ✅ Enum value conversion
- ⏳ Payment processing (partial)
- ⏳ Tax filing workflows (partial)
- ⏳ Document management (partial)

## Future Enhancements

- [ ] Visual regression testing with Percy/Applitools
- [ ] API contract testing
- [ ] Performance testing
- [ ] Accessibility testing (a11y)
- [ ] Mobile responsive testing
- [ ] Cross-browser compatibility matrix

## Questions?

See official Playwright documentation: https://playwright.dev/
