# Azure Deployment Guide for Betts CTIS

This guide will help you deploy the Betts Client Tax Information System to Azure using your student subscription.

## Prerequisites

1. **Azure Student Subscription**
   - Sign up at: https://azure.microsoft.com/free/students/
   - Provides $100 credit for 12 months

2. **Azure CLI**
   - Already installed ✅
   - Verify: `az --version`

3. **GitHub Account**
   - Repository access with admin permissions

## Quick Start Deployment

### Option 1: Automated Deployment (PowerShell - Windows)

```powershell
# 1. Login to Azure
az login

# 2. Run the deployment script
cd scripts
.\deploy-azure.ps1

# 3. Follow the prompts and save the credentials displayed
```

### Option 2: Automated Deployment (Bash - Linux/Mac)

```bash
# 1. Login to Azure
az login

# 2. Make script executable
chmod +x scripts/deploy-azure.sh

# 3. Run the deployment script
./scripts/deploy-azure.sh

# 4. Follow the prompts and save the credentials displayed
```

## What Gets Deployed

The deployment script creates the following Azure resources:

### 1. **Backend API** (Azure App Service)
- **Service**: App Service with .NET 9.0 runtime
- **SKU**: B1 (Basic) - suitable for development/student
- **Features**:
  - Auto-scaling capabilities
  - Deployment slots
  - Application Insights integration

### 2. **Frontend Web App** (Azure Static Web Apps)
- **Service**: Static Web Apps
- **SKU**: Free tier
- **Features**:
  - Global CDN
  - Automatic HTTPS
  - Custom domains support
  - CI/CD via GitHub Actions

### 3. **Database** (Azure Database for PostgreSQL)
- **Service**: PostgreSQL Flexible Server
- **SKU**: B1ms (Burstable) - cost-effective
- **Storage**: 32GB
- **Version**: PostgreSQL 16
- **Features**:
  - Automatic backups
  - High availability options
  - Network security

### 4. **Resource Group**
- Contains all resources for easy management
- Name: `rg-betts-ctis` (configurable)
- Location: East US (configurable)

## Cost Estimate (Monthly)

Based on Azure Student pricing:

| Service | SKU | Estimated Cost |
|---------|-----|----------------|
| App Service | B1 | ~$13/month |
| Static Web Apps | Free | $0 |
| PostgreSQL | B1ms | ~$12/month |
| **Total** | | **~$25/month** |

**Note**: With Azure Student credit ($100), you can run this for ~4 months free!

## GitHub Actions Setup

After running the deployment script, configure GitHub Actions:

### 1. Add GitHub Secrets

Go to your repository → Settings → Secrets and variables → Actions

Add the following secrets:

```
AZURE_WEBAPP_PUBLISH_PROFILE_BACKEND
AZURE_STATIC_WEB_APPS_API_TOKEN
NEXT_PUBLIC_API_URL
```

### 2. Get Backend Publish Profile

```bash
# Download publish profile
az webapp deployment list-publishing-profiles \
  --name <your-backend-app-name> \
  --resource-group rg-betts-ctis \
  --xml
```

Copy the entire XML output and save as `AZURE_WEBAPP_PUBLISH_PROFILE_BACKEND`

### 3. Get Static Web App Token

This is displayed after deployment. If you need to retrieve it:

```bash
az staticwebapp secrets list \
  --name betts-ctis-web \
  --resource-group rg-betts-ctis \
  --query "properties.apiKey" -o tsv
```

### 4. Set API URL

The backend URL format: `https://<backend-app-name>.azurewebsites.net`

## Database Migration

After deployment, run database migrations:

### Option 1: From Local Machine

```bash
# Set connection string environment variable
export ConnectionStrings__DefaultConnection="Host=<server>.postgres.database.azure.com;Database=betts_ctis;Username=<admin-user>;Password=<password>;SSL Mode=Require"

# Run migrations
cd Betts/BettsTax
dotnet ef database update --project BettsTax.Data --startup-project BettsTax.Web
```

### Option 2: From Azure Portal

1. Go to Azure Portal → App Service (backend)
2. Open SSH/Console
3. Navigate to `/home/site/wwwroot`
4. Run: `dotnet ef database update`

## Environment Configuration

### Backend (App Service)

Configure these app settings in Azure Portal:

```json
{
  "ASPNETCORE_ENVIRONMENT": "Production",
  "ConnectionStrings__DefaultConnection": "<from deployment output>",
  "JwtSettings__Secret": "<generate secure key>",
  "JwtSettings__Issuer": "BettsCTIS",
  "JwtSettings__Audience": "BettsCTIS",
  "EmailSettings__SmtpServer": "<your-smtp-server>",
  "EmailSettings__SmtpPort": "587",
  "EmailSettings__SenderEmail": "<your-email>",
  "EmailSettings__SenderName": "Betts CTIS",
  "WEBSITE_RUN_FROM_PACKAGE": "1"
}
```

### Frontend (Static Web App)

Configure in `staticwebapp.config.json` or environment variables:

```json
{
  "NEXT_PUBLIC_API_URL": "<backend-url-from-deployment>"
}
```

## Monitoring and Troubleshooting

### Application Insights

1. Enable Application Insights in Azure Portal
2. View logs, metrics, and performance data
3. Set up alerts for errors

### View Logs

```bash
# Backend logs
az webapp log tail \
  --name <backend-app-name> \
  --resource-group rg-betts-ctis

# Download logs
az webapp log download \
  --name <backend-app-name> \
  --resource-group rg-betts-ctis
```

### Common Issues

#### 1. Database Connection Fails
- Check firewall rules allow Azure services
- Verify connection string format
- Ensure SSL mode is set to "Require"

#### 2. Frontend Can't Connect to Backend
- Check CORS settings in backend
- Verify `NEXT_PUBLIC_API_URL` is correct
- Check Azure App Service is running

#### 3. Deployment Fails
- Check GitHub Actions logs
- Verify all secrets are set correctly
- Ensure resource names are unique

## Security Best Practices

1. **Enable Authentication**
   ```bash
   az webapp auth update \
     --name <backend-app-name> \
     --resource-group rg-betts-ctis \
     --enabled true \
     --action LoginWithAzureActiveDirectory
   ```

2. **Configure Firewall**
   - Restrict database access to App Service only
   - Enable network rules in PostgreSQL

3. **Use Key Vault** (Optional)
   ```bash
   az keyvault create \
     --name betts-ctis-vault \
     --resource-group rg-betts-ctis \
     --location eastus
   ```

4. **Enable HTTPS Only**
   ```bash
   az webapp update \
     --name <backend-app-name> \
     --resource-group rg-betts-ctis \
     --https-only true
   ```

## Scaling Options

### Backend Scaling

```bash
# Scale up (increase instance size)
az appservice plan update \
  --name asp-betts-ctis \
  --resource-group rg-betts-ctis \
  --sku S1

# Scale out (increase instances)
az webapp scale \
  --name <backend-app-name> \
  --resource-group rg-betts-ctis \
  --instance-count 2
```

### Database Scaling

```bash
# Upgrade database tier
az postgres flexible-server update \
  --name <db-server-name> \
  --resource-group rg-betts-ctis \
  --sku-name Standard_B2s
```

## Cleanup (Delete Resources)

To avoid charges when done testing:

```bash
# Delete entire resource group
az group delete \
  --name rg-betts-ctis \
  --yes --no-wait

# Or delete individual resources
az webapp delete --name <app-name> --resource-group rg-betts-ctis
az staticwebapp delete --name <app-name> --resource-group rg-betts-ctis
az postgres flexible-server delete --name <db-name> --resource-group rg-betts-ctis
```

## Useful Azure CLI Commands

```bash
# List all resources in group
az resource list --resource-group rg-betts-ctis -o table

# Check app service status
az webapp show --name <app-name> --resource-group rg-betts-ctis

# Restart app service
az webapp restart --name <app-name> --resource-group rg-betts-ctis

# View deployment history
az webapp deployment list --name <app-name> --resource-group rg-betts-ctis

# Check database server status
az postgres flexible-server show --name <db-name> --resource-group rg-betts-ctis
```

## Support Resources

- **Azure Documentation**: https://docs.microsoft.com/azure
- **Azure Student Portal**: https://portal.azure.com/#blade/Microsoft_Azure_Education/EducationMenuBlade/overview
- **Azure CLI Reference**: https://docs.microsoft.com/cli/azure
- **GitHub Actions Docs**: https://docs.github.com/actions

## Next Steps

1. ✅ Run deployment script
2. ✅ Save credentials securely
3. ✅ Configure GitHub secrets
4. ✅ Run database migrations
5. ✅ Configure environment variables
6. ✅ Test deployment
7. ✅ Set up monitoring
8. ✅ Configure custom domain (optional)

---

**Need Help?** Check the troubleshooting section or Azure documentation.
