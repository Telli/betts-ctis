# Troubleshooting: Tax Filings Page Issues

## Current Issue
Getting 500 API errors and "New Tax Filing" button not visible.

## Step-by-Step Troubleshooting

### Step 1: Clear Browser Cache & Restart Next.js Dev Server

The Next.js dev server may have cached the old version of the page.

**Frontend (Next.js)**:
```bash
# Stop the dev server (Ctrl+C in terminal)
# Then restart:
cd c:\Users\telli\Desktop\Betts\Betts\sierra-leone-ctis
npm run dev
```

**Then in browser**:
1. Open DevTools (F12)
2. Right-click refresh button → "Empty Cache and Hard Reload"
3. Or use Ctrl+Shift+R (hard refresh)

### Step 2: Check Console Logs

After refreshing, check the browser console for these messages:

**Expected console output**:
```
[Tax Filings] Starting to fetch data...
[Tax Filings] Error caught, setting empty state: [error details]
[Tax Filings] Setting loading to false
```

If you see these messages, the page IS rendering and should show the button.

### Step 3: Verify Page Renders (Even with API Error)

After the error, you should see:
- ✅ Page title "Tax Filings"
- ✅ "New Tax Filing" button (top-right, blue color)
- ✅ Error toast message at bottom
- ✅ Empty table or filter section

**If you DON'T see these**, the loading state might be stuck.

### Step 4: Manual Check - Force Page to Render

Open browser DevTools Console and run:
```javascript
// Check if React is rendering
document.querySelector('[data-testid="tax-filings-page"]') // Should not be null

// Check if button exists
document.querySelector('a[href="/tax-filings/new"]') // Should not be null

// Check for loading indicator
document.querySelector('.loading') || document.querySelector('[role="status"]')
```

### Step 5: Restart Backend

The 500 errors are from the backend needing restart:

```bash
cd c:\Users\telli\Desktop\Betts\Betts\BettsTax\BettsTax.Web
dotnet clean
dotnet build
dotnet run
```

Or in Visual Studio:
1. Stop (Shift+F5)
2. Clean Solution
3. Rebuild Solution (Ctrl+Shift+B)
4. Start (F5)

### Step 6: Verify Backend is Running

Check backend console for:
```
Now listening on: http://localhost:5000
Now listening on: https://localhost:5001
```

Try accessing directly:
```
http://localhost:5000/api/tax-filings?page=1&pageSize=20
```

Should return JSON, not 500 error.

## Quick Test Checklist

- [ ] Frontend dev server restarted
- [ ] Browser hard refreshed (Ctrl+Shift+R)
- [ ] Console shows "[Tax Filings] Setting loading to false"
- [ ] Page renders (not stuck on loading screen)
- [ ] "New Tax Filing" button visible (even if table is empty)
- [ ] Backend restarted
- [ ] API endpoint returns 200 OK (not 500)

## Expected Behavior After Fixes

**Before backend restart**:
- Page renders ✅
- Button visible ✅
- Error toast shown ✅
- Cannot load existing data ❌

**After backend restart**:
- Page renders ✅
- Button visible ✅
- No error toast ✅
- Data loads successfully ✅

## Still Not Working?

If button still not visible after all steps:

1. **Check the file was actually saved**:
   ```bash
   Select-String -Path "app\tax-filings\page.tsx" -Pattern "New Tax Filing"
   ```
   Should show line 137: `New Tax Filing`

2. **Check Next.js compiled the change**:
   - Look for "Compiled /tax-filings" in Next.js terminal
   - If not, save the file again or restart dev server

3. **Check for React errors**:
   - Open browser DevTools
   - Look for red error messages in Console
   - Check for error boundaries catching render errors

4. **Try navigating directly**:
   ```
   http://localhost:3000/tax-filings/new
   ```
   If this works, button routing is fine, just visibility issue.

5. **Check CSS/Styling**:
   - Button might be hidden by CSS
   - Use browser inspector to find the button element
   - Check if `display: none` or `visibility: hidden` is applied

## Contact Points

If still stuck, share:
1. Browser console screenshot (with all messages)
2. Network tab (filter by "tax-filings")
3. Elements tab showing the page HTML structure
4. Backend console output
