# CTIS Integration Testing - Quick Reference

## 🚀 Quick Start

### Run Everything
```bash
# Terminal 1: Backend
cd ../../BettsTax/BettsTax.Web && dotnet run

# Terminal 2: Frontend  
npm run dev

# Terminal 3: Integration Tests
npm run test:integration
```

### First Time Setup
```bash
# Install dependencies
npm install

# Create environment file
echo "NEXT_PUBLIC_API_URL=http://localhost:5001" > .env.local
```

---

## 🧪 Testing Commands

```bash
# Integration tests (all endpoints)
npm run test:integration

# E2E tests (full UI flow)
npm run test:e2e

# E2E with UI
npm run test:e2e:ui

# Specific test file
npx playwright test auth.spec.ts
```

---

## 🔌 SignalR Usage

### In Components
```typescript
import { useChat, useNotifications } from '@/hooks/useSignalR';

// Chat
const { isConnected, messages, sendMessage } = useChat(conversationId);
await sendMessage('Hello!');

// Notifications
const { notifications, unreadCount, markAsRead } = useNotifications();
```

### Direct Client
```typescript
import { signalRService } from '@/lib/signalr-client';

// Initialize
await signalRService.initializeChatHub();
await signalRService.initializeNotificationHub();

// Subscribe
const unsubscribe = signalRService.onMessage((msg) => console.log(msg));

// Cleanup
unsubscribe();
```

---

## 📚 Documentation

- **Full Testing Guide:** `../../WEEK1_INTEGRATION_TESTING.md`
- **Quick Start:** `../../QUICKSTART_TESTING.md`
- **Completion Summary:** `../../WEEK1_COMPLETION_SUMMARY.md`
- **Environment Setup:** `env.example.md`

---

## ✅ Health Checks

- Backend: http://localhost:5001/health
- Frontend: http://localhost:3000
- Login: http://localhost:3000/login

---

## 🐛 Common Issues

**"Connection failed"**
→ Check backend is running on port 5001

**"Module not found"**
→ Run `npm install`

**401 errors**
→ Check JWT token in localStorage

**CORS errors**
→ Verify backend CORS allows localhost:3000

---

## 📊 Week 1 Status

- ✅ Frontend-Backend Connection: 100%
- ✅ API Endpoints: 10/10 tested
- ✅ Authentication: Working
- ✅ SignalR: Implemented
- ✅ Testing Infrastructure: Complete

**Overall: 85% Production Ready**

---

**Quick Help:** See `../../QUICKSTART_TESTING.md`
