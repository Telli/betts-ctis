# CTIS Quick Start Guide - Integration Testing

**Ready to test?** Follow these steps to verify your frontend-backend integration.

---

## 🚀 Quick Start (5 Minutes)

### Step 1: Start the Backend (Terminal 1)

```bash
cd BettsTax/BettsTax.Web
dotnet run
```

**Expected output:**
```
info: Microsoft.Hosting.Lifetime[14]
      Now listening on: http://localhost:5001
info: Microsoft.Hosting.Lifetime[0]
      Application started. Press Ctrl+C to shut down.
```

✅ **Backend is running!**

---

### Step 2: Configure Frontend Environment (One-time setup)

```bash
cd sierra-leone-ctis

# Create .env.local file
echo "NEXT_PUBLIC_API_URL=http://localhost:5001" > .env.local
```

Or manually create `.env.local`:
```
NEXT_PUBLIC_API_URL=http://localhost:5001
```

---

### Step 3: Install Frontend Dependencies (One-time setup)

```bash
cd sierra-leone-ctis
npm install
```

This will install:
- `@microsoft/signalr` - Real-time communication
- `ts-node` - TypeScript execution for test scripts
- All other dependencies

---

### Step 4: Start the Frontend (Terminal 2)

```bash
cd sierra-leone-ctis
npm run dev
```

**Expected output:**
```
  ▲ Next.js 15.2.4
  - Local:        http://localhost:3000
  - Ready in 2.3s
```

✅ **Frontend is running!**

---

### Step 5: Run Integration Tests (Terminal 3)

```bash
cd sierra-leone-ctis
npm run test:integration
```

**Expected output:**
```
🚀 Starting Integration Tests...

Testing API at: http://localhost:5001

1️⃣  Testing Backend Connection...
2️⃣  Testing Auth Endpoints...
3️⃣  Testing Protected Endpoints...
4️⃣  Testing Public Endpoints...

📊 Test Results:
================================================================================
✅ Backend Health Check
   Status: 200 OK

✅ Login Endpoint (Should return 401 for invalid credentials)
   Status: 401 Unauthorized

✅ Register Endpoint Structure
   Status: 400/200 (validated)

✅ Clients Endpoint (Should require auth)
   Status: 401 Unauthorized

✅ Dashboard Endpoint (Should require auth)
   Status: 401 Unauthorized

✅ Documents Endpoint (Should require auth)
   Status: 401 Unauthorized

✅ Payments Endpoint (Should require auth)
   Status: 401 Unauthorized

✅ Tax Filings Endpoint (Should require auth)
   Status: 401 Unauthorized

✅ Notifications Endpoint (Should require auth)
   Status: 401 Unauthorized

✅ Tax Calculator Endpoint
   Status: 200 OK

================================================================================

📈 Summary: 10/10 tests passed (100%)

🎉 All integration tests passed! Frontend-Backend connection verified.
```

✅ **Integration tests passed!**

---

### Step 6: Test the Application Manually

1. **Open browser:** http://localhost:3000
2. **You should see:** Login page
3. **Try to access protected route:** http://localhost:3000/dashboard
4. **You should be:** Redirected to login

---

## 🧪 Test Authentication Flow

### Create a test user (Backend Terminal)

The backend automatically creates an admin user on first run. Check the console output for default credentials, or register a new user via the frontend.

### Test Login

1. Go to http://localhost:3000/login
2. Enter credentials
3. Click "Log in"
4. **Expected:** Redirect to dashboard based on role
   - Client → `/client-portal/dashboard`
   - Admin/Associate → `/dashboard`

---

## ✅ Verification Checklist

After running all steps, verify:

- [ ] Backend running on port 5001
- [ ] Frontend running on port 3000
- [ ] Integration tests pass (10/10)
- [ ] Login page accessible
- [ ] Login redirects to correct dashboard
- [ ] Protected routes redirect to login
- [ ] No console errors in browser
- [ ] No errors in backend console

---

## 🐛 Troubleshooting

### Issue: "Connection failed" in integration tests

**Solution:**
- Check backend is running: `http://localhost:5001/health`
- Verify port 5001 in backend console output
- Update `.env.local` if backend is on different port

### Issue: "Module not found @microsoft/signalr"

**Solution:**
```bash
cd sierra-leone-ctis
npm install
```

### Issue: Frontend not loading

**Solution:**
```bash
# Clear Next.js cache
cd sierra-leone-ctis
rm -rf .next
npm run dev
```

### Issue: 401 errors after login

**Solution:**
- Check JWT token in browser dev tools → Application → Local Storage
- Token should exist with key `auth_token`
- Verify token expiration hasn't passed

### Issue: CORS errors

**Solution:**
Backend CORS should allow `localhost:3000`. Check backend `Program.cs`:
```csharp
builder.Services.AddCors(options =>
{
    options.AddPolicy("SierraLeonePolicy", policy =>
    {
        policy.WithOrigins("http://localhost:3000", "https://localhost:3000")
              .AllowAnyMethod()
              .AllowAnyHeader()
              .AllowCredentials();
    });
});
```

---

## 📚 Additional Testing

### Run E2E Tests (Playwright)

```bash
cd sierra-leone-ctis

# Run all E2E tests
npm run test:e2e

# Run with UI
npm run test:e2e:ui

# Run specific test
npx playwright test auth.spec.ts
```

### Test SignalR Real-Time Features

```bash
# In browser console (after login):
import { signalRService } from '/lib/signalr-client';

// Test chat
await signalRService.initializeChatHub();
console.log('Chat connected:', signalRService.isChatConnected());

// Test notifications
await signalRService.initializeNotificationHub();
console.log('Notifications connected:', signalRService.isNotificationConnected());
```

---

## 🎯 What's Next?

After verifying integration:

1. **Week 2:** Payment Gateway Integration
   - Register Orange Money merchant account
   - Register Africell Money merchant account
   - Configure live credentials
   - Test payment flows

2. **Week 3:** Security Hardening
   - Configure SMS gateway
   - Enforce MFA
   - Security audit
   - Penetration testing

3. **Week 4:** Production Deployment
   - Set up CI/CD
   - Deploy to Azure/AWS
   - Configure monitoring
   - Go live!

---

## 📞 Need Help?

**Common Commands:**
```bash
# Backend
cd BettsTax/BettsTax.Web
dotnet run

# Frontend
cd sierra-leone-ctis
npm run dev

# Integration Tests
cd sierra-leone-ctis
npm run test:integration

# E2E Tests
cd sierra-leone-ctis
npm run test:e2e
```

**Check Status:**
- Backend: http://localhost:5001/health
- Frontend: http://localhost:3000
- API Docs: http://localhost:5001/swagger (if configured)

---

**Document Version:** 1.0  
**Last Updated:** 2025-09-30  
**Status:** Ready for Testing ✅
