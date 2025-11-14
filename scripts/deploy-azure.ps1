# Azure Deployment Script for Betts CTIS
# Prerequisites: Azure CLI installed and logged in

param(
    [Parameter(Mandatory=$false)]
    [string]$ResourceGroupName = "rg-betts-ctis",
    
    [Parameter(Mandatory=$false)]
    [string]$Location = "westus2",
    
    [Parameter(Mandatory=$false)]
    [string]$Environment = "production"
)

Write-Host "🚀 Deploying Betts CTIS to Azure..." -ForegroundColor Cyan
Write-Host "Resource Group: $ResourceGroupName" -ForegroundColor Yellow
Write-Host "Location: $Location" -ForegroundColor Yellow
Write-Host "Environment: $Environment" -ForegroundColor Yellow
Write-Host ""
Write-Host "💡 Tip: If deployment fails due to region restrictions, try:" -ForegroundColor Yellow
Write-Host "   .\deploy-azure.ps1 -Location westus2" -ForegroundColor Gray
Write-Host "   .\deploy-azure.ps1 -Location westus" -ForegroundColor Gray
Write-Host "   .\deploy-azure.ps1 -Location eastus2" -ForegroundColor Gray

# Check if logged in to Azure
Write-Host "`n📋 Checking Azure CLI login status..." -ForegroundColor Cyan
$account = az account show 2>$null | ConvertFrom-Json
if (-not $account) {
    Write-Host "❌ Not logged in to Azure CLI. Please run 'az login' first." -ForegroundColor Red
    exit 1
}
Write-Host "✅ Logged in as: $($account.user.name)" -ForegroundColor Green
Write-Host "   Subscription: $($account.name)" -ForegroundColor Green

# Create or verify Resource Group
Write-Host "`n📦 Creating/Verifying Resource Group..." -ForegroundColor Cyan
$existingRG = az group show --name $ResourceGroupName 2>$null | ConvertFrom-Json
if ($existingRG) {
    Write-Host "ℹ️  Resource Group already exists in location: $($existingRG.location)" -ForegroundColor Yellow
    $Location = $existingRG.location
    Write-Host "   Using existing location: $Location" -ForegroundColor Yellow
} else {
    az group create --name $ResourceGroupName --location $Location
    if ($LASTEXITCODE -ne 0) {
        Write-Host "❌ Failed to create resource group" -ForegroundColor Red
        exit 1
    }
    Write-Host "✅ Resource Group created" -ForegroundColor Green
}

# Create App Service Plan (Free/Basic tier for student subscription)
$AppServicePlan = "asp-betts-ctis"
Write-Host "`n📱 Creating App Service Plan..." -ForegroundColor Cyan
az appservice plan create `
    --name $AppServicePlan `
    --resource-group $ResourceGroupName `
    --location $Location `
    --sku B1 `
    --is-linux
if ($LASTEXITCODE -ne 0) {
    Write-Host "❌ Failed to create App Service Plan" -ForegroundColor Red
    exit 1
}
Write-Host "✅ App Service Plan created" -ForegroundColor Green

# Create Web App for Backend (.NET)
$BackendAppName = "betts-ctis-api-$(Get-Random -Minimum 1000 -Maximum 9999)"
Write-Host "`n🔧 Creating Backend Web App: $BackendAppName..." -ForegroundColor Cyan
az webapp create `
    --name $BackendAppName `
    --resource-group $ResourceGroupName `
    --plan $AppServicePlan `
    --runtime "DOTNET|9.0"
if ($LASTEXITCODE -ne 0) {
    Write-Host "❌ Failed to create Backend Web App" -ForegroundColor Red
    exit 1
}
Write-Host "✅ Backend Web App created" -ForegroundColor Green

# Configure Backend App Settings
Write-Host "`n⚙️  Configuring Backend App Settings..." -ForegroundColor Cyan
az webapp config appsettings set `
    --name $BackendAppName `
    --resource-group $ResourceGroupName `
    --settings `
        ASPNETCORE_ENVIRONMENT=$Environment `
        WEBSITE_RUN_FROM_PACKAGE=1
Write-Host "✅ Backend App Settings configured" -ForegroundColor Green

# Create Azure Database for PostgreSQL (Flexible Server - suitable for student)
$DbServerName = "betts-ctis-db-$(Get-Random -Minimum 1000 -Maximum 9999)"
$DbAdminUser = "betts_admin"
$DbAdminPassword = "$(New-Guid)!Aa1" # Generate strong password
Write-Host "`n🗄️  Creating PostgreSQL Database Server: $DbServerName..." -ForegroundColor Cyan
Write-Host "   Admin User: $DbAdminUser" -ForegroundColor Yellow
Write-Host "   ⚠️  Password will be displayed once - save it securely!" -ForegroundColor Yellow

az postgres flexible-server create `
    --name $DbServerName `
    --resource-group $ResourceGroupName `
    --location $Location `
    --admin-user $DbAdminUser `
    --admin-password $DbAdminPassword `
    --sku-name Standard_B1ms `
    --tier Burstable `
    --storage-size 32 `
    --version 16 `
    --public-access 0.0.0.0-255.255.255.255
if ($LASTEXITCODE -ne 0) {
    Write-Host "❌ Failed to create PostgreSQL Server" -ForegroundColor Red
    exit 1
}
Write-Host "✅ PostgreSQL Server created" -ForegroundColor Green

# Create Database
$DbName = "betts_ctis"
Write-Host "`n📊 Creating Database: $DbName..." -ForegroundColor Cyan
az postgres flexible-server db create `
    --resource-group $ResourceGroupName `
    --server-name $DbServerName `
    --database-name $DbName
Write-Host "✅ Database created" -ForegroundColor Green

# Configure Connection String
$ConnectionString = "Host=$DbServerName.postgres.database.azure.com;Database=$DbName;Username=$DbAdminUser;Password=$DbAdminPassword;SSL Mode=Require"
az webapp config connection-string set `
    --name $BackendAppName `
    --resource-group $ResourceGroupName `
    --connection-string-type PostgreSQL `
    --settings DefaultConnection="$ConnectionString"
Write-Host "✅ Connection String configured" -ForegroundColor Green

# Create Static Web App for Frontend
$FrontendAppName = "betts-ctis-web"
Write-Host "`n🌐 Creating Static Web App: $FrontendAppName..." -ForegroundColor Cyan
az staticwebapp create `
    --name $FrontendAppName `
    --resource-group $ResourceGroupName `
    --location $Location `
    --sku Free
if ($LASTEXITCODE -ne 0) {
    Write-Host "❌ Failed to create Static Web App" -ForegroundColor Red
    exit 1
}
Write-Host "✅ Static Web App created" -ForegroundColor Green

# Get deployment token for Static Web App
$StaticWebAppToken = az staticwebapp secrets list `
    --name $FrontendAppName `
    --resource-group $ResourceGroupName `
    --query "properties.apiKey" -o tsv

# Output Summary
Write-Host "`n" -ForegroundColor Cyan
Write-Host "═══════════════════════════════════════════════════════════" -ForegroundColor Cyan
Write-Host "✅ DEPLOYMENT COMPLETE!" -ForegroundColor Green
Write-Host "═══════════════════════════════════════════════════════════" -ForegroundColor Cyan
Write-Host ""
Write-Host "📋 Resource Information:" -ForegroundColor Yellow
Write-Host "   Resource Group: $ResourceGroupName" -ForegroundColor White
Write-Host "   Location: $Location" -ForegroundColor White
Write-Host ""
Write-Host "🔧 Backend API:" -ForegroundColor Yellow
Write-Host "   Name: $BackendAppName" -ForegroundColor White
Write-Host "   URL: https://$BackendAppName.azurewebsites.net" -ForegroundColor White
Write-Host ""
Write-Host "🌐 Frontend Web App:" -ForegroundColor Yellow
Write-Host "   Name: $FrontendAppName" -ForegroundColor White
$FrontendUrl = az staticwebapp show --name $FrontendAppName --resource-group $ResourceGroupName --query "defaultHostname" -o tsv
Write-Host "   URL: https://$FrontendUrl" -ForegroundColor White
Write-Host ""
Write-Host "🗄️  Database:" -ForegroundColor Yellow
Write-Host "   Server: $DbServerName.postgres.database.azure.com" -ForegroundColor White
Write-Host "   Database: $DbName" -ForegroundColor White
Write-Host "   Admin User: $DbAdminUser" -ForegroundColor White
Write-Host "   ⚠️  Admin Password: $DbAdminPassword" -ForegroundColor Red
Write-Host "   ⚠️  SAVE THIS PASSWORD - It won't be shown again!" -ForegroundColor Red
Write-Host ""
Write-Host "🔑 GitHub Secrets (Add these to your repository):" -ForegroundColor Yellow
Write-Host "   AZURE_WEBAPP_PUBLISH_PROFILE_BACKEND: (Download from Azure Portal)" -ForegroundColor White
Write-Host "   AZURE_STATIC_WEB_APPS_API_TOKEN: $StaticWebAppToken" -ForegroundColor White
Write-Host "   NEXT_PUBLIC_API_URL: https://$BackendAppName.azurewebsites.net" -ForegroundColor White
Write-Host ""
Write-Host "📝 Next Steps:" -ForegroundColor Yellow
Write-Host "   1. Add GitHub Secrets to your repository" -ForegroundColor White
Write-Host "   2. Run database migrations on the backend" -ForegroundColor White
Write-Host "   3. Configure environment variables in Azure Portal" -ForegroundColor White
Write-Host "   4. Push to main branch to trigger deployment" -ForegroundColor White
Write-Host ""
Write-Host "═══════════════════════════════════════════════════════════" -ForegroundColor Cyan

# Save credentials to file
$CredsFile = "azure-deployment-credentials.txt"
@"
Betts CTIS - Azure Deployment Credentials
Generated: $(Get-Date -Format "yyyy-MM-dd HH:mm:ss")
═══════════════════════════════════════════════════════════

Resource Group: $ResourceGroupName
Location: $Location

Backend API:
  Name: $BackendAppName
  URL: https://$BackendAppName.azurewebsites.net

Frontend Web App:
  Name: $FrontendAppName
  URL: https://$FrontendUrl

Database:
  Server: $DbServerName.postgres.database.azure.com
  Database: $DbName
  Admin User: $DbAdminUser
  Admin Password: $DbAdminPassword

Static Web App Deployment Token:
  $StaticWebAppToken

Connection String:
  $ConnectionString

⚠️  IMPORTANT: Keep this file secure and do not commit to Git!
═══════════════════════════════════════════════════════════
"@ | Out-File -FilePath $CredsFile -Encoding UTF8

Write-Host "💾 Credentials saved to: $CredsFile" -ForegroundColor Green
Write-Host "⚠️  Keep this file secure!" -ForegroundColor Red
