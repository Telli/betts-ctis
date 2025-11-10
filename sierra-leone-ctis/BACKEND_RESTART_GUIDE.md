# Backend Restart Guide - Tax Filings 500 Error

## Problem
Getting API Error 500 on `/api/tax-filings` endpoint after code changes.

## Root Cause
The circular reference fix in `BettsTax.Web/Program.cs` requires the backend to be restarted to take effect.

## Solution

### Quick Fix (Restart Backend)

**Option 1: Visual Studio**
1. Stop the backend (Shift+F5 or stop button)
2. Rebuild solution (Ctrl+Shift+B)
3. Start debugging (F5)

**Option 2: Command Line**
```bash
cd c:\Users\telli\Desktop\Betts\Betts\BettsTax\BettsTax.Web
dotnet build
dotnet run
```

**Option 3: VS Code with C# Dev Kit**
1. Open the backend project folder
2. Terminal → Run Build Task (Ctrl+Shift+B)
3. Run → Start Debugging (F5)

### Expected Result After Restart
- ✅ Tax filings API returns 200 OK
- ✅ "New Tax Filing" button is visible on page
- ✅ Tax filings list loads successfully
- ✅ No circular reference errors in console

### Frontend Resilience
The frontend has been updated to handle API failures gracefully:
- Page will render even if API fails
- "New Tax Filing" button will be visible
- Empty state shown with helpful message
- Users can still navigate to create new filings

## Files That Were Changed

### Backend
- ✅ `BettsTax.Web/Program.cs` - Added `ReferenceHandler.IgnoreCycles`

### Frontend  
- ✅ `app/tax-filings/page.tsx` - Improved error handling
- ✅ `app/payments/page.tsx` - Improved error handling
- ✅ `app/documents/page.tsx` - Improved error handling

## Verification Steps

After restarting the backend:

1. **Check backend console** - No serialization errors
2. **Open Tax Filings page** - No 500 errors in browser console
3. **Verify button** - "New Tax Filing" button visible top-right
4. **Test creation** - Click button, should navigate to `/tax-filings/new`

## If Problem Persists

If you still see 500 errors after restart:

1. Check backend logs for specific error details
2. Verify `Program.cs` changes were saved
3. Try a clean rebuild:
   ```bash
   dotnet clean
   dotnet build
   dotnet run
   ```
4. Check if there are compilation errors in the backend

## Note
The frontend will now show pages even when the backend is down, allowing you to:
- See the UI layout
- Access creation pages
- Navigate between pages
- Only the data loading will fail (with clear error messages)
