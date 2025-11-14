#!/bin/bash

# Azure Deployment Script for Betts CTIS (Bash version)
# Prerequisites: Azure CLI installed and logged in

set -e

# Configuration
RESOURCE_GROUP_NAME="${1:-rg-betts-ctis}"
LOCATION="${2:-eastus}"
ENVIRONMENT="${3:-production}"

echo "🚀 Deploying Betts CTIS to Azure..."
echo "Resource Group: $RESOURCE_GROUP_NAME"
echo "Location: $LOCATION"
echo "Environment: $ENVIRONMENT"

# Check if logged in to Azure
echo ""
echo "📋 Checking Azure CLI login status..."
if ! az account show &>/dev/null; then
    echo "❌ Not logged in to Azure CLI. Please run 'az login' first."
    exit 1
fi

ACCOUNT_INFO=$(az account show)
ACCOUNT_NAME=$(echo $ACCOUNT_INFO | jq -r '.user.name')
SUBSCRIPTION_NAME=$(echo $ACCOUNT_INFO | jq -r '.name')
echo "✅ Logged in as: $ACCOUNT_NAME"
echo "   Subscription: $SUBSCRIPTION_NAME"

# Create Resource Group
echo ""
echo "📦 Creating Resource Group..."
az group create --name "$RESOURCE_GROUP_NAME" --location "$LOCATION"
echo "✅ Resource Group created"

# Create App Service Plan
APP_SERVICE_PLAN="asp-betts-ctis"
echo ""
echo "📱 Creating App Service Plan..."
az appservice plan create \
    --name "$APP_SERVICE_PLAN" \
    --resource-group "$RESOURCE_GROUP_NAME" \
    --location "$LOCATION" \
    --sku B1 \
    --is-linux
echo "✅ App Service Plan created"

# Create Web App for Backend (.NET)
BACKEND_APP_NAME="betts-ctis-api-$RANDOM"
echo ""
echo "🔧 Creating Backend Web App: $BACKEND_APP_NAME..."
az webapp create \
    --name "$BACKEND_APP_NAME" \
    --resource-group "$RESOURCE_GROUP_NAME" \
    --plan "$APP_SERVICE_PLAN" \
    --runtime "DOTNET|9.0"
echo "✅ Backend Web App created"

# Configure Backend App Settings
echo ""
echo "⚙️  Configuring Backend App Settings..."
az webapp config appsettings set \
    --name "$BACKEND_APP_NAME" \
    --resource-group "$RESOURCE_GROUP_NAME" \
    --settings \
        ASPNETCORE_ENVIRONMENT="$ENVIRONMENT" \
        WEBSITE_RUN_FROM_PACKAGE=1
echo "✅ Backend App Settings configured"

# Create Azure Database for PostgreSQL
DB_SERVER_NAME="betts-ctis-db-$RANDOM"
DB_ADMIN_USER="betts_admin"
DB_ADMIN_PASSWORD="$(openssl rand -base64 32)Aa1!"
echo ""
echo "🗄️  Creating PostgreSQL Database Server: $DB_SERVER_NAME..."
echo "   Admin User: $DB_ADMIN_USER"
echo "   ⚠️  Password will be displayed once - save it securely!"

az postgres flexible-server create \
    --name "$DB_SERVER_NAME" \
    --resource-group "$RESOURCE_GROUP_NAME" \
    --location "$LOCATION" \
    --admin-user "$DB_ADMIN_USER" \
    --admin-password "$DB_ADMIN_PASSWORD" \
    --sku-name Standard_B1ms \
    --tier Burstable \
    --storage-size 32 \
    --version 16 \
    --public-access 0.0.0.0-255.255.255.255
echo "✅ PostgreSQL Server created"

# Create Database
DB_NAME="betts_ctis"
echo ""
echo "📊 Creating Database: $DB_NAME..."
az postgres flexible-server db create \
    --resource-group "$RESOURCE_GROUP_NAME" \
    --server-name "$DB_SERVER_NAME" \
    --database-name "$DB_NAME"
echo "✅ Database created"

# Configure Connection String
CONNECTION_STRING="Host=${DB_SERVER_NAME}.postgres.database.azure.com;Database=${DB_NAME};Username=${DB_ADMIN_USER};Password=${DB_ADMIN_PASSWORD};SSL Mode=Require"
az webapp config connection-string set \
    --name "$BACKEND_APP_NAME" \
    --resource-group "$RESOURCE_GROUP_NAME" \
    --connection-string-type PostgreSQL \
    --settings DefaultConnection="$CONNECTION_STRING"
echo "✅ Connection String configured"

# Create Static Web App for Frontend
FRONTEND_APP_NAME="betts-ctis-web"
echo ""
echo "🌐 Creating Static Web App: $FRONTEND_APP_NAME..."
az staticwebapp create \
    --name "$FRONTEND_APP_NAME" \
    --resource-group "$RESOURCE_GROUP_NAME" \
    --location "$LOCATION" \
    --sku Free
echo "✅ Static Web App created"

# Get deployment token for Static Web App
STATIC_WEBAPP_TOKEN=$(az staticwebapp secrets list \
    --name "$FRONTEND_APP_NAME" \
    --resource-group "$RESOURCE_GROUP_NAME" \
    --query "properties.apiKey" -o tsv)

FRONTEND_URL=$(az staticwebapp show \
    --name "$FRONTEND_APP_NAME" \
    --resource-group "$RESOURCE_GROUP_NAME" \
    --query "defaultHostname" -o tsv)

# Output Summary
echo ""
echo "═══════════════════════════════════════════════════════════"
echo "✅ DEPLOYMENT COMPLETE!"
echo "═══════════════════════════════════════════════════════════"
echo ""
echo "📋 Resource Information:"
echo "   Resource Group: $RESOURCE_GROUP_NAME"
echo "   Location: $LOCATION"
echo ""
echo "🔧 Backend API:"
echo "   Name: $BACKEND_APP_NAME"
echo "   URL: https://${BACKEND_APP_NAME}.azurewebsites.net"
echo ""
echo "🌐 Frontend Web App:"
echo "   Name: $FRONTEND_APP_NAME"
echo "   URL: https://$FRONTEND_URL"
echo ""
echo "🗄️  Database:"
echo "   Server: ${DB_SERVER_NAME}.postgres.database.azure.com"
echo "   Database: $DB_NAME"
echo "   Admin User: $DB_ADMIN_USER"
echo "   ⚠️  Admin Password: $DB_ADMIN_PASSWORD"
echo "   ⚠️  SAVE THIS PASSWORD - It won't be shown again!"
echo ""
echo "🔑 GitHub Secrets (Add these to your repository):"
echo "   AZURE_WEBAPP_PUBLISH_PROFILE_BACKEND: (Download from Azure Portal)"
echo "   AZURE_STATIC_WEB_APPS_API_TOKEN: $STATIC_WEBAPP_TOKEN"
echo "   NEXT_PUBLIC_API_URL: https://${BACKEND_APP_NAME}.azurewebsites.net"
echo ""
echo "📝 Next Steps:"
echo "   1. Add GitHub Secrets to your repository"
echo "   2. Run database migrations on the backend"
echo "   3. Configure environment variables in Azure Portal"
echo "   4. Push to main branch to trigger deployment"
echo ""
echo "═══════════════════════════════════════════════════════════"

# Save credentials to file
CREDS_FILE="azure-deployment-credentials.txt"
cat > "$CREDS_FILE" <<EOF
Betts CTIS - Azure Deployment Credentials
Generated: $(date +"%Y-%m-%d %H:%M:%S")
═══════════════════════════════════════════════════════════

Resource Group: $RESOURCE_GROUP_NAME
Location: $LOCATION

Backend API:
  Name: $BACKEND_APP_NAME
  URL: https://${BACKEND_APP_NAME}.azurewebsites.net

Frontend Web App:
  Name: $FRONTEND_APP_NAME
  URL: https://$FRONTEND_URL

Database:
  Server: ${DB_SERVER_NAME}.postgres.database.azure.com
  Database: $DB_NAME
  Admin User: $DB_ADMIN_USER
  Admin Password: $DB_ADMIN_PASSWORD

Static Web App Deployment Token:
  $STATIC_WEBAPP_TOKEN

Connection String:
  $CONNECTION_STRING

⚠️  IMPORTANT: Keep this file secure and do not commit to Git!
═══════════════════════════════════════════════════════════
EOF

echo ""
echo "💾 Credentials saved to: $CREDS_FILE"
echo "⚠️  Keep this file secure!"

# Make sure the credentials file is in .gitignore
if ! grep -q "azure-deployment-credentials.txt" .gitignore 2>/dev/null; then
    echo "azure-deployment-credentials.txt" >> .gitignore
    echo "✅ Added credentials file to .gitignore"
fi
