# Simplified Azure Deployment for Student Subscription
# Uses alternative deployment strategy to avoid App Service Plan restrictions

param(
    [Parameter(Mandatory=$false)]
    [string]$ResourceGroupName = "rg-betts-ctis",
    
    [Parameter(Mandatory=$false)]
    [string]$Location = "westus2"
)

Write-Host "🚀 Deploying Betts CTIS to Azure (Student Subscription Mode)..." -ForegroundColor Cyan
Write-Host "Resource Group: $ResourceGroupName" -ForegroundColor Yellow
Write-Host "Location: $Location" -ForegroundColor Yellow
Write-Host ""
Write-Host "⚠️  This script uses a simplified deployment suitable for Azure Student subscriptions" -ForegroundColor Yellow

# Check Azure CLI login
Write-Host "`n📋 Checking Azure CLI login..." -ForegroundColor Cyan
$account = az account show 2>$null | ConvertFrom-Json
if (-not $account) {
    Write-Host "❌ Not logged in. Run 'az login' first." -ForegroundColor Red
    exit 1
}
Write-Host "✅ Logged in as: $($account.user.name)" -ForegroundColor Green
Write-Host "   Subscription: $($account.name)" -ForegroundColor Green

# Ensure resource group exists
Write-Host "`n📦 Creating Resource Group..." -ForegroundColor Cyan
az group create --name $ResourceGroupName --location $Location --output none 2>&1 | Out-Null
if ($LASTEXITCODE -eq 0) {
    Write-Host "✅ Resource Group ready" -ForegroundColor Green
} else {
    # Try to get existing
    $existingRG = az group show --name $ResourceGroupName 2>$null | ConvertFrom-Json
    if ($existingRG) {
        Write-Host "ℹ️  Using existing Resource Group in $($existingRG.location)" -ForegroundColor Yellow
        $Location = $existingRG.location
    } else {
        Write-Host "❌ Failed to create/access resource group" -ForegroundColor Red
        exit 1
    }
}

# Create PostgreSQL Database
$dbServerName = "betts-ctis-db-$(Get-Random -Minimum 1000 -Maximum 9999)"
$dbAdminUser = "betts_admin"
$dbAdminPassword = -join ((65..90) + (97..122) + (48..57) + 33,35,36,37,38,42 | Get-Random -Count 16 | ForEach-Object {[char]$_})

Write-Host "`n🗄️  Creating PostgreSQL Server..." -ForegroundColor Cyan
Write-Host "   Name: $dbServerName" -ForegroundColor Gray
Write-Host "   Admin: $dbAdminUser" -ForegroundColor Gray

try {
    az postgres flexible-server create `
        --name $dbServerName `
        --resource-group $ResourceGroupName `
        --location $Location `
        --admin-user $dbAdminUser `
        --admin-password $dbAdminPassword `
        --sku-name Standard_B1ms `
        --tier Burstable `
        --storage-size 32 `
        --version 16 `
        --public-access 0.0.0.0 `
        --yes `
        --output none

    if ($LASTEXITCODE -eq 0) {
        Write-Host "✅ PostgreSQL Server created" -ForegroundColor Green
    } else {
        throw "Failed to create PostgreSQL server"
    }
} catch {
    Write-Host "❌ PostgreSQL creation failed: $_" -ForegroundColor Red
    Write-Host "   Tip: Try a different region or check your subscription quotas" -ForegroundColor Yellow
    exit 1
}

# Create database
$dbName = "betts_ctis"
Write-Host "`n📊 Creating Database..." -ForegroundColor Cyan
az postgres flexible-server db create `
    --resource-group $ResourceGroupName `
    --server-name $dbServerName `
    --database-name $dbName `
    --output none

if ($LASTEXITCODE -eq 0) {
    Write-Host "✅ Database created" -ForegroundColor Green
} else {
    Write-Host "❌ Database creation failed" -ForegroundColor Red
    exit 1
}

# Create Static Web App for Frontend
$frontendAppName = "betts-ctis-web"
Write-Host "`n🌐 Creating Static Web App..." -ForegroundColor Cyan
az staticwebapp create `
    --name $frontendAppName `
    --resource-group $ResourceGroupName `
    --location $Location `
    --sku Free `
    --output none

if ($LASTEXITCODE -eq 0) {
    Write-Host "✅ Static Web App created" -ForegroundColor Green
} else {
    Write-Host "❌ Static Web App creation failed" -ForegroundColor Red
}

# Get Static Web App details
$swaDetails = az staticwebapp show `
    --name $frontendAppName `
    --resource-group $ResourceGroupName | ConvertFrom-Json

$swaToken = az staticwebapp secrets list `
    --name $frontendAppName `
    --resource-group $ResourceGroupName `
    --query "properties.apiKey" -o tsv

# Connection string
$connectionString = "Host=${dbServerName}.postgres.database.azure.com;Database=${dbName};Username=${dbAdminUser};Password=${dbAdminPassword};SSL Mode=Require"

# Output summary
Write-Host "`n" -NoNewline
Write-Host "═══════════════════════════════════════════════════════════" -ForegroundColor Cyan
Write-Host "✅ DEPLOYMENT COMPLETE (Partial - Student Mode)" -ForegroundColor Green
Write-Host "═══════════════════════════════════════════════════════════" -ForegroundColor Cyan
Write-Host ""
Write-Host "📋 Deployed Resources:" -ForegroundColor Yellow
Write-Host "   Resource Group: $ResourceGroupName" -ForegroundColor White
Write-Host "   Location: $Location" -ForegroundColor White
Write-Host ""
Write-Host "🗄️  Database:" -ForegroundColor Yellow
Write-Host "   Server: ${dbServerName}.postgres.database.azure.com" -ForegroundColor White
Write-Host "   Database: $dbName" -ForegroundColor White
Write-Host "   Admin User: $dbAdminUser" -ForegroundColor White
Write-Host "   Password: $dbAdminPassword" -ForegroundColor White
Write-Host ""
Write-Host "🌐 Frontend (Static Web App):" -ForegroundColor Yellow
Write-Host "   Name: $frontendAppName" -ForegroundColor White
Write-Host "   URL: https://$($swaDetails.defaultHostname)" -ForegroundColor White
Write-Host "   Deployment Token: $swaToken" -ForegroundColor White
Write-Host ""
Write-Host "⚠️  BACKEND DEPLOYMENT NOTE:" -ForegroundColor Yellow
Write-Host "   Azure Student subscription restricts App Service Plans." -ForegroundColor White
Write-Host "   Backend deployment options:" -ForegroundColor White
Write-Host "   1. Deploy backend to Azure Container Instances (manual)" -ForegroundColor Gray
Write-Host "   2. Deploy to Azure Container Apps (requires extension)" -ForegroundColor Gray
Write-Host "   3. Use local development + ngrok for testing" -ForegroundColor Gray
Write-Host "   4. Deploy to different cloud provider (Railway, Render, Heroku)" -ForegroundColor Gray
Write-Host ""
Write-Host "📝 Next Steps:" -ForegroundColor Yellow
Write-Host "   1. Save these credentials securely" -ForegroundColor White
Write-Host "   2. Deploy frontend to Static Web App using GitHub Actions" -ForegroundColor White
Write-Host "   3. Run database migrations: dotnet ef database update" -ForegroundColor White
Write-Host "   4. Choose backend deployment strategy" -ForegroundColor White
Write-Host ""
Write-Host "═══════════════════════════════════════════════════════════" -ForegroundColor Cyan

# Save credentials
$credsFile = "azure-deployment-credentials.txt"
@"
Betts CTIS - Azure Deployment Credentials (Student Mode)
Generated: $(Get-Date -Format "yyyy-MM-dd HH:mm:ss")
═══════════════════════════════════════════════════════════

Resource Group: $ResourceGroupName
Location: $Location

Database (PostgreSQL):
  Server: ${dbServerName}.postgres.database.azure.com
  Database: $dbName
  Admin User: $dbAdminUser
  Admin Password: $dbAdminPassword
  
Connection String:
  $connectionString

Frontend (Static Web App):
  Name: $frontendAppName
  URL: https://$($swaDetails.defaultHostname)
  Deployment Token: $swaToken

GitHub Secrets Required:
  AZURE_STATIC_WEB_APPS_API_TOKEN: $swaToken
  DATABASE_URL: $connectionString

⚠️  BACKEND NOT DEPLOYED - See deployment notes above
⚠️  Keep this file secure - do not commit to Git!
═══════════════════════════════════════════════════════════
"@ | Out-File -FilePath $credsFile -Encoding UTF8

Write-Host "💾 Credentials saved to: $credsFile" -ForegroundColor Green
Write-Host ""
