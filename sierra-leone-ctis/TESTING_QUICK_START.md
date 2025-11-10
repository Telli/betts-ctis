# 🚀 Quick Start: Playwright Testing

## Run Regression Tests (Fastest Way)

**These tests catch all the issues we've been fixing:**

```bash
# Make sure backend is running first
cd c:/Users/telli/Desktop/Betts/Betts/BettsTax/BettsTax.Web
dotnet run

# In another terminal, run the regression tests
cd c:/Users/telli/Desktop/Betts/Betts/sierra-leone-ctis
npm run test:regression
```

## What Gets Tested?

✅ **Client creation** - No more 400 errors from enum mismatches  
✅ **Table filtering** - Works with numeric enum values  
✅ **Table sorting** - Uses correct field names (businessName, not name)  
✅ **Search functionality** - Searches in actual database fields  
✅ **Status badges** - Handles numeric enums without charAt errors  
✅ **SelectItem validation** - No empty value errors  
✅ **API endpoints** - No 404 or 500 errors  
✅ **Form inputs** - Annual turnover without leading zero  
✅ **Type conversion** - Enums converted to numbers before API calls

## Watch Tests While Developing

```bash
# UI Mode - Best for development
npm run test:e2e:ui

# Or watch mode (auto-reruns)
npx playwright test --watch
```

## Debug a Failing Test

```bash
# Run in headed mode (see browser)
npm run test:regression:headed

# Or full debug mode with inspector
npm run test:regression:debug
```

## Quick Commands

| Command | What It Does |
|---------|-------------|
| `npm run test:regression` | Run all regression tests (headless) |
| `npm run test:regression:headed` | Run with browser visible |
| `npm run test:regression:debug` | Debug with Playwright Inspector |
| `npm run test:e2e` | Run ALL e2e tests |
| `npm run test:e2e:ui` | UI Mode (recommended for dev) |
| `npm run test:e2e:report` | View HTML report of last run |

## Test User Accounts

Use these in manual testing or when writing tests:

```
Admin:     admin@bettsfirm.sl / Admin123!
Associate: associate@bettsfirm.sl / Associate123!
Client:    client@testcompany.sl / Client123!
```

## Common Issues

### ❌ "Connection refused"
**Solution**: Start the backend server first
```bash
cd BettsTax/BettsTax.Web && dotnet run
```

### ❌ "User not found" 
**Solution**: Reset demo data
```bash
# In BettsTax.Web folder
Remove-Item BettsTax.db* -Force
dotnet run  # Will auto-seed demo data
```

### ❌ Tests are slow
**Solution**: Run specific tests
```bash
npx playwright test -g "should create client"
```

## View Test Results

After running tests:

```bash
npm run test:e2e:report
```

This opens an interactive HTML report with:
- ✅ Passed/failed tests
- 📸 Screenshots of failures
- 🎥 Video recordings
- 📊 Execution timeline
- 🔍 Trace viewer

## Add Your Own Tests

Create a new file in `tests/e2e/`:

```typescript
import { test, expect } from '@playwright/test';
import { login } from '../helpers/auth';

test('my feature works', async ({ page }) => {
  await login(page, 'admin');
  await page.goto('/my-page');
  await expect(page.locator('text=Success')).toBeVisible();
});
```

Then run it:
```bash
npx playwright test tests/e2e/my-test.spec.ts
```

## Pro Tips

💡 **Use UI Mode during development**: `npm run test:e2e:ui`  
💡 **Record tests**: `npx playwright codegen http://localhost:3000`  
💡 **Test one browser**: `npx playwright test --project=chromium`  
💡 **Grep for specific tests**: `npx playwright test -g "client"`  
💡 **Update snapshots**: `npx playwright test --update-snapshots`

## Next Steps

📖 Full documentation: `tests/README.md`  
🐛 Report issues: Add to `FIXES_APPLIED.md`  
✨ Best practices: https://playwright.dev/docs/best-practices

---

**Need help?** Check the full testing guide in `tests/README.md`
