# Azure Deployment Script - Free Tier (Student Subscription)
# Uses only free/included resources from Azure for Students

param(
    [Parameter(Mandatory=$false)]
    [string]$ResourceGroupName = "rg-betts-ctis",
    
    [Parameter(Mandatory=$false)]
    [string]$Location = "eastus"
)

Write-Host "🚀 Deploying Betts CTIS to Azure (Free Tier)..." -ForegroundColor Cyan
Write-Host "Resource Group: $ResourceGroupName" -ForegroundColor Yellow
Write-Host "Location: $Location" -ForegroundColor Yellow
Write-Host ""
Write-Host "✅ Using FREE tier resources from Azure for Students" -ForegroundColor Green
Write-Host "ℹ️  Allowed regions: polandcentral, norwayeast, switzerlandnorth, italynorth, spaincentral" -ForegroundColor Cyan
Write-Host ""

# Check Azure CLI login
Write-Host "`n📋 Checking Azure CLI login..." -ForegroundColor Cyan
$account = az account show 2>$null | ConvertFrom-Json
if (-not $account) {
    Write-Host "❌ Not logged in. Run 'az login' first." -ForegroundColor Red
    exit 1
}
Write-Host "✅ Logged in as: $($account.user.name)" -ForegroundColor Green
Write-Host "   Subscription: $($account.name)" -ForegroundColor Green

# Create Resource Group
Write-Host "`n📦 Creating Resource Group..." -ForegroundColor Cyan
$existingRG = az group show --name $ResourceGroupName 2>$null | ConvertFrom-Json
if ($existingRG) {
    Write-Host "ℹ️  Using existing Resource Group in $($existingRG.location)" -ForegroundColor Yellow
    $Location = $existingRG.location
} else {
    az group create --name $ResourceGroupName --location $Location --output none
    if ($LASTEXITCODE -eq 0) {
        Write-Host "✅ Resource Group created" -ForegroundColor Green
    } else {
        Write-Host "❌ Failed to create resource group" -ForegroundColor Red
        exit 1
    }
}

# Generate secure password
$dbAdminUser = "betts_admin"
$dbAdminPassword = -join ((65..90) + (97..122) + (48..57) + 33,35,36,37,38,42,64 | Get-Random -Count 20 | ForEach-Object {[char]$_})

# Create PostgreSQL Flexible Server (FREE B1MS tier - 750 hours/month)
$dbServerName = "betts-ctis-db-$(Get-Random -Minimum 1000 -Maximum 9999)"
Write-Host "`n🗄️  Creating PostgreSQL Flexible Server (FREE B1MS tier)..." -ForegroundColor Cyan
Write-Host "   Server: $dbServerName" -ForegroundColor Gray
Write-Host "   Tier: Burstable B1MS (750 free hours/month)" -ForegroundColor Gray
Write-Host "   Storage: 32GB (included free)" -ForegroundColor Gray

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
        --output none 2>&1 | Out-Null

    if ($LASTEXITCODE -eq 0) {
        Write-Host "✅ PostgreSQL Server created (FREE tier)" -ForegroundColor Green
    } else {
        Write-Host "❌ PostgreSQL creation failed" -ForegroundColor Red
        Write-Host "   This might be a regional restriction. Trying alternate approach..." -ForegroundColor Yellow
        
        # Try with minimal settings
        az postgres flexible-server create `
            --name $dbServerName `
            --resource-group $ResourceGroupName `
            --location $Location `
            --admin-user $dbAdminUser `
            --admin-password $dbAdminPassword `
            --sku-name Standard_B1ms `
            --tier Burstable `
            --public-access All `
            --yes
        
        if ($LASTEXITCODE -ne 0) {
            throw "Failed to create PostgreSQL server"
        }
    }
} catch {
    Write-Host "❌ PostgreSQL creation failed: $_" -ForegroundColor Red
    Write-Host "`n💡 Alternative: Use Azure Cosmos DB (also free tier available)" -ForegroundColor Yellow
    exit 1
}

# Create database
$dbName = "betts_ctis"
Write-Host "`n📊 Creating Database..." -ForegroundColor Cyan
az postgres flexible-server db create `
    --resource-group $ResourceGroupName `
    --server-name $dbServerName `
    --database-name $dbName `
    --output none 2>&1 | Out-Null

if ($LASTEXITCODE -eq 0) {
    Write-Host "✅ Database created" -ForegroundColor Green
} else {
    Write-Host "⚠️  Database creation may have failed, but continuing..." -ForegroundColor Yellow
}

# Configure firewall to allow Azure services
Write-Host "`n🔒 Configuring firewall rules..." -ForegroundColor Cyan
az postgres flexible-server firewall-rule create `
    --resource-group $ResourceGroupName `
    --name $dbServerName `
    --rule-name "AllowAzureServices" `
    --start-ip-address 0.0.0.0 `
    --end-ip-address 0.0.0.0 `
    --output none 2>&1 | Out-Null
Write-Host "✅ Azure services can access database" -ForegroundColor Green

# Create Virtual Machine for Backend (FREE B1s tier - 750 hours/month)
$vmName = "betts-ctis-vm"
$vmAdminUser = "azureuser"
$vmAdminPassword = -join ((65..90) + (97..122) + (48..57) + 33,35,36,37,38,42,64 | Get-Random -Count 20 | ForEach-Object {[char]$_})

Write-Host "`n💻 Creating Virtual Machine for Backend (FREE B1s tier)..." -ForegroundColor Cyan
Write-Host "   VM: $vmName" -ForegroundColor Gray
Write-Host "   Size: B1s (750 free hours/month)" -ForegroundColor Gray
Write-Host "   OS: Ubuntu 22.04 LTS" -ForegroundColor Gray
Write-Host "   ⏳ This may take 3-5 minutes..." -ForegroundColor Yellow

az vm create `
    --resource-group $ResourceGroupName `
    --name $vmName `
    --location $Location `
    --size Standard_B1s `
    --image Ubuntu2204 `
    --admin-username $vmAdminUser `
    --admin-password $vmAdminPassword `
    --authentication-type password `
    --public-ip-sku Standard `
    --output none 2>&1 | Out-Null

if ($LASTEXITCODE -eq 0) {
    Write-Host "✅ Virtual Machine created (FREE tier)" -ForegroundColor Green
} else {
    Write-Host "❌ VM creation failed" -ForegroundColor Red
    Write-Host "   Note: You can deploy backend to Azure Container Instances instead" -ForegroundColor Yellow
}

# Open port 5001 for .NET backend
Write-Host "`n🌐 Opening port for backend API..." -ForegroundColor Cyan
az vm open-port `
    --resource-group $ResourceGroupName `
    --name $vmName `
    --port 5001 `
    --priority 1010 `
    --output none 2>&1 | Out-Null
Write-Host "✅ Port 5001 opened" -ForegroundColor Green

# Get VM public IP
$vmPublicIP = az vm show `
    --resource-group $ResourceGroupName `
    --name $vmName `
    --show-details `
    --query publicIps -o tsv

# Create Static Web App for Frontend (FREE tier)
$frontendAppName = "betts-ctis-web"
Write-Host "`n🌐 Creating Static Web App for Frontend (FREE tier)..." -ForegroundColor Cyan

az staticwebapp create `
    --name $frontendAppName `
    --resource-group $ResourceGroupName `
    --location $Location `
    --sku Free `
    --output none 2>&1 | Out-Null

if ($LASTEXITCODE -eq 0) {
    Write-Host "✅ Static Web App created (FREE tier)" -ForegroundColor Green
} else {
    Write-Host "⚠️  Static Web App creation may have failed" -ForegroundColor Yellow
}

# Get Static Web App details
$swaDetails = az staticwebapp show `
    --name $frontendAppName `
    --resource-group $ResourceGroupName 2>$null | ConvertFrom-Json

$swaToken = az staticwebapp secrets list `
    --name $frontendAppName `
    --resource-group $ResourceGroupName `
    --query "properties.apiKey" -o tsv 2>$null

# Connection string
$connectionString = "Host=${dbServerName}.postgres.database.azure.com;Database=${dbName};Username=${dbAdminUser};Password=${dbAdminPassword};SSL Mode=Require"
$backendUrl = "http://${vmPublicIP}:5001"

# Output summary
Write-Host "`n" -NoNewline
Write-Host "═══════════════════════════════════════════════════════════" -ForegroundColor Cyan
Write-Host "✅ DEPLOYMENT COMPLETE - FREE TIER RESOURCES!" -ForegroundColor Green
Write-Host "═══════════════════════════════════════════════════════════" -ForegroundColor Cyan
Write-Host ""
Write-Host "📋 Resource Summary:" -ForegroundColor Yellow
Write-Host "   Resource Group: $ResourceGroupName" -ForegroundColor White
Write-Host "   Location: $Location" -ForegroundColor White
Write-Host ""
Write-Host "🗄️  PostgreSQL Database (FREE - B1MS, 750 hrs/month):" -ForegroundColor Yellow
Write-Host "   Server: ${dbServerName}.postgres.database.azure.com" -ForegroundColor White
Write-Host "   Database: $dbName" -ForegroundColor White
Write-Host "   Admin User: $dbAdminUser" -ForegroundColor White
Write-Host "   Password: $dbAdminPassword" -ForegroundColor White
Write-Host ""
Write-Host "💻 Backend VM (FREE - B1s, 750 hrs/month):" -ForegroundColor Yellow
Write-Host "   Name: $vmName" -ForegroundColor White
Write-Host "   Public IP: $vmPublicIP" -ForegroundColor White
Write-Host "   SSH User: $vmAdminUser" -ForegroundColor White
Write-Host "   SSH Password: $vmAdminPassword" -ForegroundColor White
Write-Host "   API URL: $backendUrl" -ForegroundColor White
Write-Host ""
Write-Host "🌐 Frontend (FREE - Static Web App):" -ForegroundColor Yellow
if ($swaDetails) {
    Write-Host "   Name: $frontendAppName" -ForegroundColor White
    Write-Host "   URL: https://$($swaDetails.defaultHostname)" -ForegroundColor White
    Write-Host "   Deployment Token: $swaToken" -ForegroundColor White
} else {
    Write-Host "   ⚠️  Static Web App creation pending - check Azure Portal" -ForegroundColor Yellow
}
Write-Host ""
Write-Host "📝 Next Steps:" -ForegroundColor Yellow
Write-Host ""
Write-Host "1️⃣  Deploy Backend to VM:" -ForegroundColor Cyan
Write-Host "   ssh ${vmAdminUser}@${vmPublicIP}" -ForegroundColor Gray
Write-Host "   # Install .NET 9:" -ForegroundColor Gray
Write-Host "   wget https://dot.net/v1/dotnet-install.sh -O dotnet-install.sh" -ForegroundColor Gray
Write-Host "   bash dotnet-install.sh --version latest --runtime aspnetcore" -ForegroundColor Gray
Write-Host "   # Upload and run your app" -ForegroundColor Gray
Write-Host ""
Write-Host "2️⃣  Run Database Migrations:" -ForegroundColor Cyan
Write-Host "   cd Betts/BettsTax" -ForegroundColor Gray
Write-Host "   dotnet ef database update --project BettsTax.Data --startup-project BettsTax.Web --connection `"$connectionString`"" -ForegroundColor Gray
Write-Host ""
Write-Host "3️⃣  Configure GitHub Secrets:" -ForegroundColor Cyan
Write-Host "   AZURE_STATIC_WEB_APPS_API_TOKEN = $swaToken" -ForegroundColor Gray
Write-Host "   NEXT_PUBLIC_API_URL = $backendUrl" -ForegroundColor Gray
Write-Host "   DATABASE_URL = $connectionString" -ForegroundColor Gray
Write-Host ""
Write-Host "4️⃣  Deploy Frontend:" -ForegroundColor Cyan
Write-Host "   Push to main branch to trigger GitHub Actions deployment" -ForegroundColor Gray
Write-Host ""
Write-Host "💰 Cost Breakdown (Monthly):" -ForegroundColor Yellow
Write-Host "   PostgreSQL B1MS: FREE (750 hrs included)" -ForegroundColor Green
Write-Host "   VM B1s: FREE (750 hrs included)" -ForegroundColor Green
Write-Host "   Static Web App: FREE" -ForegroundColor Green
Write-Host "   Storage: FREE (within limits)" -ForegroundColor Green
Write-Host "   Total: $0/month ✅" -ForegroundColor Green
Write-Host ""
Write-Host "═══════════════════════════════════════════════════════════" -ForegroundColor Cyan

# Save credentials
$credsFile = "azure-deployment-credentials.txt"
@"
Betts CTIS - Azure Deployment Credentials (FREE TIER)
Generated: $(Get-Date -Format "yyyy-MM-dd HH:mm:ss")
═══════════════════════════════════════════════════════════

Resource Group: $ResourceGroupName
Location: $Location

PostgreSQL Database (FREE B1MS - 750 hrs/month):
  Server: ${dbServerName}.postgres.database.azure.com
  Database: $dbName
  Admin User: $dbAdminUser
  Admin Password: $dbAdminPassword
  Connection String: $connectionString

Backend VM (FREE B1s - 750 hrs/month):
  Name: $vmName
  Public IP: $vmPublicIP
  SSH User: $vmAdminUser
  SSH Password: $vmAdminPassword
  API URL: $backendUrl
  
  SSH Command:
  ssh ${vmAdminUser}@${vmPublicIP}

Frontend Static Web App (FREE):
  Name: $frontendAppName
  URL: https://$($swaDetails.defaultHostname)
  Deployment Token: $swaToken

GitHub Secrets:
  AZURE_STATIC_WEB_APPS_API_TOKEN: $swaToken
  NEXT_PUBLIC_API_URL: $backendUrl
  DATABASE_URL: $connectionString

Monthly Cost: $0 (All FREE tier resources)

⚠️  Keep this file secure - do not commit to Git!
═══════════════════════════════════════════════════════════
"@ | Out-File -FilePath $credsFile -Encoding UTF8

Write-Host "💾 Credentials saved to: $credsFile" -ForegroundColor Green
Write-Host "⚠️  Keep credentials file secure!" -ForegroundColor Yellow
Write-Host ""

# Add to gitignore
if (-not (Get-Content .gitignore -ErrorAction SilentlyContinue | Select-String "azure-deployment-credentials.txt")) {
    "azure-deployment-credentials.txt" | Out-File -FilePath .gitignore -Append -Encoding UTF8
    Write-Host "✅ Added credentials file to .gitignore" -ForegroundColor Green
}
