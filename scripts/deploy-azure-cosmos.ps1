# Azure Deployment - Cosmos DB Alternative (FREE Tier)
# Uses Cosmos DB instead of PostgreSQL to avoid regional restrictions

param(
    [Parameter(Mandatory=$false)]
    [string]$ResourceGroupName = "rg-betts-ctis",
    
    [Parameter(Mandatory=$false)]
    [string]$Location = "westus2"
)

Write-Host "🚀 Deploying Betts CTIS to Azure (Cosmos DB FREE Tier)..." -ForegroundColor Cyan
Write-Host "Resource Group: $ResourceGroupName" -ForegroundColor Yellow
Write-Host "Location: $Location" -ForegroundColor Yellow

# Check Azure CLI login
$account = az account show 2>$null | ConvertFrom-Json
if (-not $account) {
    Write-Host "❌ Not logged in. Run 'az login' first." -ForegroundColor Red
    exit 1
}
Write-Host "✅ Logged in: $($account.user.name)" -ForegroundColor Green

# Ensure resource group
az group create --name $ResourceGroupName --location $Location --output none 2>&1 | Out-Null

# Create Cosmos DB Account (FREE tier - 25GB + 1000 RU/s)
$cosmosAccountName = "betts-ctis-cosmos-$(Get-Random -Minimum 1000 -Maximum 9999)"
Write-Host "`n🌌 Creating Cosmos DB Account (FREE tier)..." -ForegroundColor Cyan
Write-Host "   Account: $cosmosAccountName" -ForegroundColor Gray
Write-Host "   Free tier: 25GB storage + 1000 RU/s throughput" -ForegroundColor Gray
Write-Host "   ⏳ This takes 3-5 minutes..." -ForegroundColor Yellow

az cosmosdb create `
    --name $cosmosAccountName `
    --resource-group $ResourceGroupName `
    --location $Location `
    --enable-free-tier true `
    --default-consistency-level Session `
    --output none

if ($LASTEXITCODE -eq 0) {
    Write-Host "✅ Cosmos DB created (FREE tier)" -ForegroundColor Green
} else {
    Write-Host "❌ Cosmos DB creation failed" -ForegroundColor Red
    exit 1
}

# Create database and container
Write-Host "`n📊 Creating database and collections..." -ForegroundColor Cyan
az cosmosdb sql database create `
    --account-name $cosmosAccountName `
    --resource-group $ResourceGroupName `
    --name BettsCTIS `
    --output none

# Get connection details
$cosmosKeys = az cosmosdb keys list `
    --name $cosmosAccountName `
    --resource-group $ResourceGroupName | ConvertFrom-Json

$cosmosEndpoint = "https://${cosmosAccountName}.documents.azure.com:443/"
$cosmosPrimaryKey = $cosmosKeys.primaryMasterKey

Write-Host "✅ Database configured" -ForegroundColor Green

# Create VM for Backend
$vmName = "betts-ctis-vm"
$vmAdminUser = "azureuser"
$vmAdminPassword = -join ((65..90) + (97..122) + (48..57) + 33,35,36,37,38,42,64 | Get-Random -Count 20 | ForEach-Object {[char]$_})

Write-Host "`n💻 Creating VM (FREE B1s tier)..." -ForegroundColor Cyan
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
    --output none

if ($LASTEXITCODE -eq 0) {
    Write-Host "✅ VM created (FREE tier)" -ForegroundColor Green
} else {
    Write-Host "❌ VM creation failed" -ForegroundColor Red
    exit 1
}

# Open ports
az vm open-port --resource-group $ResourceGroupName --name $vmName --port 5001 --priority 1010 --output none
az vm open-port --resource-group $ResourceGroupName --name $vmName --port 80 --priority 1020 --output none

$vmPublicIP = az vm show `
    --resource-group $ResourceGroupName `
    --name $vmName `
    --show-details `
    --query publicIps -o tsv

# Create Static Web App
$frontendAppName = "betts-ctis-web"
Write-Host "`n🌐 Creating Static Web App..." -ForegroundColor Cyan
az staticwebapp create `
    --name $frontendAppName `
    --resource-group $ResourceGroupName `
    --location $Location `
    --sku Free `
    --output none

$swaDetails = az staticwebapp show `
    --name $frontendAppName `
    --resource-group $ResourceGroupName 2>$null | ConvertFrom-Json

$swaToken = az staticwebapp secrets list `
    --name $frontendAppName `
    --resource-group $ResourceGroupName `
    --query "properties.apiKey" -o tsv

# Summary
Write-Host "`n" -NoNewline
Write-Host "═══════════════════════════════════════════════════════════" -ForegroundColor Cyan
Write-Host "✅ DEPLOYMENT COMPLETE - ALL FREE TIER!" -ForegroundColor Green
Write-Host "═══════════════════════════════════════════════════════════" -ForegroundColor Cyan
Write-Host ""
Write-Host "🌌 Cosmos DB (FREE - 25GB + 1000 RU/s):" -ForegroundColor Yellow
Write-Host "   Account: $cosmosAccountName" -ForegroundColor White
Write-Host "   Endpoint: $cosmosEndpoint" -ForegroundColor White
Write-Host "   Primary Key: $cosmosPrimaryKey" -ForegroundColor White
Write-Host ""
Write-Host "💻 Backend VM (FREE - B1s, 750 hrs/month):" -ForegroundColor Yellow
Write-Host "   IP: $vmPublicIP" -ForegroundColor White
Write-Host "   User: $vmAdminUser" -ForegroundColor White
Write-Host "   Password: $vmAdminPassword" -ForegroundColor White
Write-Host "   API URL: http://${vmPublicIP}:5001" -ForegroundColor White
Write-Host ""
Write-Host "🌐 Frontend (FREE - Static Web App):" -ForegroundColor Yellow
Write-Host "   URL: https://$($swaDetails.defaultHostname)" -ForegroundColor White
Write-Host "   Token: $swaToken" -ForegroundColor White
Write-Host ""
Write-Host "📝 Next Steps:" -ForegroundColor Yellow
Write-Host "1. SSH to VM: ssh ${vmAdminUser}@${vmPublicIP}" -ForegroundColor Gray
Write-Host "2. Install .NET and deploy backend" -ForegroundColor Gray
Write-Host "3. Update backend to use Cosmos DB instead of PostgreSQL" -ForegroundColor Gray
Write-Host "4. Configure GitHub secrets and deploy frontend" -ForegroundColor Gray
Write-Host ""
Write-Host "💰 Monthly Cost: $0 (100% FREE tier)" -ForegroundColor Green
Write-Host "═══════════════════════════════════════════════════════════" -ForegroundColor Cyan

# Save credentials
@"
Betts CTIS - Azure FREE Tier Deployment
Generated: $(Get-Date -Format "yyyy-MM-dd HH:mm:ss")
═══════════════════════════════════════════════════════════

Cosmos DB (FREE - 25GB + 1000 RU/s):
  Account: $cosmosAccountName
  Endpoint: $cosmosEndpoint
  Primary Key: $cosmosPrimaryKey
  Database: BettsCTIS

VM (FREE - B1s):
  IP: $vmPublicIP
  SSH: ssh ${vmAdminUser}@${vmPublicIP}
  Password: $vmAdminPassword

Static Web App (FREE):
  URL: https://$($swaDetails.defaultHostname)
  Token: $swaToken

Monthly Cost: \$0

⚠️  NOTE: Your app currently uses PostgreSQL.
You'll need to update it to use Cosmos DB or deploy PostgreSQL elsewhere.
"@ | Out-File -FilePath "azure-free-tier-credentials.txt" -Encoding UTF8

Write-Host "💾 Credentials saved to: azure-free-tier-credentials.txt" -ForegroundColor Green
