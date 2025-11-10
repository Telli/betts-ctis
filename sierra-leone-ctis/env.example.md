# Environment Variables Configuration

Copy this to `.env.local` and update the values:

```bash
# Backend API Configuration
# The backend API URL - update this to match your backend port
NEXT_PUBLIC_API_URL=http://localhost:5001

# For production
# NEXT_PUBLIC_API_URL=https://api.bettsfirm.sl
```

## Backend Configuration

Make sure your backend is running on the port specified in `NEXT_PUBLIC_API_URL`.

Check your backend `launchSettings.json` or `Program.cs` to verify the port.
