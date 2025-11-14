# Alternative Deployment Options for Betts CTIS

Your Azure Student subscription has restrictive policies preventing deployment of App Service Plans and PostgreSQL databases in standard regions. Here are alternative deployment options:

## Option 1: Railway.app (Recommended for Students)
**Pros:** Free tier, PostgreSQL included, GitHub integration, simple deployment
**Cost:** Free tier available

### Steps:
1. Sign up at https://railway.app (use GitHub account)
2. Create new project → Deploy from GitHub
3. Add PostgreSQL database (click "New" → "Database" → "PostgreSQL")
4. Deploy backend:
   - Connect GitHub repo
   - Set root directory: `Betts/BettsTax`
   - Build command: `dotnet publish -c Release`
   - Start command: `dotnet BettsTax.Web.dll`
5. Deploy frontend:
   - Add new service from same repo
   - Set root directory: `Betts/sierra-leone-ctis`
   - Build command: `pnpm install && pnpm build`
   - Start command: `pnpm start`

## Option 2: Render.com
**Pros:** Free tier for web services, PostgreSQL included
**Cost:** Free (with limitations)

### Steps:
1. Sign up at https://render.com
2. Create PostgreSQL database
3. Create web service for backend (.NET)
4. Create static site for frontend (Next.js)

## Option 3: Vercel (Frontend) + Supabase (Backend DB)
**Pros:** Excellent Next.js support, free PostgreSQL
**Cost:** Free tier generous

### Steps:
Frontend (Vercel):
1. Sign up at https://vercel.com
2. Import GitHub repo
3. Configure: Root Directory = `Betts/sierra-leone-ctis`
4. Deploy automatically

Backend Database (Supabase):
1. Sign up at https://supabase.com
2. Create new project → Get PostgreSQL connection string
3. Run migrations locally pointing to Supabase DB

Backend API - Deploy to:
- **Railway** (easiest)
- **Fly.io** (free tier available)
- **Heroku** (limited free tier)

## Option 4: Local Development + ngrok (Testing Only)
**Pros:** Free, works immediately, good for testing
**Cons:** Not suitable for production

### Steps:
1. Install ngrok: `choco install ngrok` or download from https://ngrok.com
2. Run backend locally: `dotnet run` (port 5001)
3. Expose with ngrok: `ngrok http 5001`
4. Use ngrok URL as `NEXT_PUBLIC_API_URL` in frontend
5. Deploy frontend to Vercel/Netlify

## Option 5: Docker + Azure Container Instances (If Allowed)
If Azure Container Instances are allowed in your subscription:

```powershell
# Build and push Docker images
docker build -t betts-ctis-api ./Betts/BettsTax
docker build -t betts-ctis-web ./Betts/sierra-leone-ctis

# Push to Azure Container Registry
az acr create --name bettscr --resource-group rg-betts-ctis --sku Basic
az acr login --name bettscr
docker tag betts-ctis-api bettscr.azurecr.io/api:latest
docker push bettscr.azurecr.io/api:latest

# Deploy to Container Instances
az container create \
  --resource-group rg-betts-ctis \
  --name betts-api \
  --image bettscr.azurecr.io/api:latest \
  --dns-name-label betts-api \
  --ports 80
```

## Option 6: Contact Azure Support
If you specifically need Azure deployment:

1. Open Azure Portal → Help + Support
2. Create support ticket
3. Issue: "Student subscription region restrictions preventing deployments"
4. Request: Enable App Service Plan and PostgreSQL deployment
5. Reference: Policy error "RequestDisallowedByAzure"

## Recommended Path for Students

**Best Option: Railway**
```bash
# 1. Push your code to GitHub (already done)
# 2. Sign up at railway.app with GitHub
# 3. New Project → Deploy from GitHub → Select bett-ctis repo
# 4. Add PostgreSQL database service
# 5. Configure environment variables
# 6. Deploy!
```

**Time to deploy:** ~10 minutes
**Cost:** $0/month (free tier)
**Features:** Auto-deploy on push, PostgreSQL included, custom domains

---

## Next Steps

Choose one of the options above based on your needs:
- **For quick testing**: Option 4 (ngrok)
- **For production on budget**: Option 1 (Railway)
- **For best Next.js experience**: Option 3 (Vercel + Supabase)
- **If you must use Azure**: Option 6 (Contact support)

Would you like detailed instructions for any specific option?
