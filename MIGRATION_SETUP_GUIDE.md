# RAG Bot Migration Setup Guide

**Status:** Migration Created ✅ | Database Setup Required ⏳

---

## 🎉 Migration Successfully Created!

The database migration `AddRAGBotSystem` has been generated and is ready to apply. However, PostgreSQL needs to be configured first.

---

## 📋 Prerequisites

### 1. PostgreSQL Installation

**Check if PostgreSQL is installed:**
```powershell
psql --version
```

**If not installed, download and install:**
- Download: https://www.postgresql.org/download/windows/
- Or use Docker: `docker run --name postgres -e POSTGRES_PASSWORD=postgres -p 5432:5432 -d postgres:16`

---

## 🔧 Database Setup Steps

### Step 1: Create Database

**Option A: Using psql command line**
```powershell
# Connect to PostgreSQL
psql -U postgres

# Create database
CREATE DATABASE betts_tax_db;

# Connect to the database
\c betts_tax_db

# Enable pgvector extension
CREATE EXTENSION IF NOT EXISTS vector;

# Verify extension
SELECT * FROM pg_extension WHERE extname = 'vector';

# Exit
\q
```

**Option B: Using pgAdmin**
1. Open pgAdmin
2. Right-click "Databases" → Create → Database
3. Name: `betts_tax_db`
4. Click "Save"
5. Open Query Tool for the database
6. Run: `CREATE EXTENSION IF NOT EXISTS vector;`

**Option C: Using Docker**
```powershell
# Start PostgreSQL with Docker
docker run --name betts-postgres `
  -e POSTGRES_PASSWORD=postgres `
  -e POSTGRES_DB=betts_tax_db `
  -p 5432:5432 `
  -d postgres:16

# Wait a few seconds, then enable pgvector
docker exec -it betts-postgres psql -U postgres -d betts_tax_db -c "CREATE EXTENSION IF NOT EXISTS vector;"
```

---

### Step 2: Update Connection String

The connection string has been updated in `appsettings.Development.json`:

```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Host=localhost;Port=5432;Database=betts_tax_db;Username=postgres;Password=postgres"
  }
}
```

**If your PostgreSQL uses different credentials, update:**
- **Username:** Change `postgres` to your username
- **Password:** Change `postgres` to your password
- **Port:** Change `5432` if using a different port
- **Host:** Change `localhost` if database is remote

---

### Step 3: Apply Migration

Once PostgreSQL is set up and the database is created:

```powershell
cd c:\Users\telli\Desktop\Betts\Betts\BettsTax

# Apply the migration
dotnet ef database update --project BettsTax.Data\BettsTax.Data.csproj --startup-project BettsTax.Web\BettsTax.Web.csproj
```

**Expected Output:**
```
Build succeeded.
Applying migration '20251118002541_AddRAGBotSystem'.
Done.
```

---

### Step 4: Verify Tables Created

**Connect to database and check tables:**
```sql
-- List all tables
\dt

-- Check bot tables specifically
SELECT table_name 
FROM information_schema.tables 
WHERE table_schema = 'public' 
AND table_name LIKE 'Bot%';
```

**Expected Bot Tables:**
- `BotConfigurations`
- `BotConversations`
- `BotFeedbacks`
- `BotMessages`
- `DocumentChunks` (with vector column)
- `EmbeddingJobs`
- `KnowledgeDocuments`

---

### Step 5: Verify pgvector Extension

```sql
-- Check extension
SELECT * FROM pg_extension WHERE extname = 'vector';

-- Check DocumentChunks table structure
\d "DocumentChunks"
```

**Expected:** You should see a column `Embedding` with type `vector(1536)`

---

## 🚀 Post-Migration Steps

### 1. Start the Application

```powershell
cd c:\Users\telli\Desktop\Betts\Betts\BettsTax\BettsTax.Web
dotnet run
```

### 2. Create Bot Configuration

**Using curl (PowerShell):**
```powershell
$headers = @{
    "Content-Type" = "application/json"
    "Authorization" = "Bearer YOUR_ADMIN_TOKEN"
}

$body = @{
    name = "Default OpenAI Configuration"
    provider = 0
    modelName = "gpt-3.5-turbo"
    apiKey = "sk-YOUR_OPENAI_API_KEY"
    temperature = 0.7
    maxTokens = 1000
    topK = 5
    similarityThreshold = 0.7
    systemPrompt = "You are a helpful tax compliance assistant for Sierra Leone."
    isActive = $true
    isDefault = $true
} | ConvertTo-Json

Invoke-RestMethod -Uri "https://localhost:5001/api/admin/bot/configurations" -Method Post -Headers $headers -Body $body
```

**Provider Values:**
- `0` = OpenAI
- `1` = Anthropic
- `2` = Gemini

### 3. Upload Knowledge Documents

**Example document upload:**
```powershell
$body = @{
    title = "GST Filing Requirements"
    content = "Goods and Services Tax (GST) in Sierra Leone: Filing deadline is 21 days after the end of each tax period..."
    category = "Tax Law"
    tags = @("GST", "Filing", "Deadlines")
} | ConvertTo-Json

Invoke-RestMethod -Uri "https://localhost:5001/api/admin/bot/documents" -Method Post -Headers $headers -Body $body
```

### 4. Test Chat

```powershell
$body = @{
    conversationId = $null
    message = "What is the GST filing deadline?"
} | ConvertTo-Json

Invoke-RestMethod -Uri "https://localhost:5001/api/bot/chat" -Method Post -Headers $headers -Body $body
```

---

## 🐛 Troubleshooting

### Error: "password authentication failed"

**Solution:**
```powershell
# Reset PostgreSQL password
psql -U postgres
ALTER USER postgres PASSWORD 'postgres';
\q
```

### Error: "database does not exist"

**Solution:**
```powershell
psql -U postgres
CREATE DATABASE betts_tax_db;
\q
```

### Error: "extension vector does not exist"

**Solution:**
```powershell
# Install pgvector extension
# On Windows: Download from https://github.com/pgvector/pgvector/releases
# On Docker: Use postgres:16 image (includes pgvector)

# Then enable it
psql -U postgres -d betts_tax_db
CREATE EXTENSION vector;
\q
```

### Error: "Could not find a part of the path"

**Solution:**
```powershell
# Ensure you're in the correct directory
cd c:\Users\telli\Desktop\Betts\Betts\BettsTax
```

---

## 📊 Migration Details

### What Was Changed

**Database Provider:**
- ❌ SQLite (removed - no vector support)
- ✅ PostgreSQL with pgvector (added)

**New Tables Created:**
1. **BotConfigurations** - LLM provider settings
2. **KnowledgeDocuments** - Document storage
3. **DocumentChunks** - Text chunks with vector embeddings
4. **BotConversations** - User conversations
5. **BotMessages** - Individual messages
6. **BotFeedbacks** - User ratings
7. **EmbeddingJobs** - Processing status

**Extensions Enabled:**
- `vector` - pgvector for similarity search

**Indexes Created:**
- Vector similarity index on `DocumentChunks.Embedding`
- User/conversation indexes
- Category/status indexes

---

## 🔍 Verification Checklist

After migration, verify:

- [ ] PostgreSQL is running
- [ ] Database `betts_tax_db` exists
- [ ] pgvector extension is enabled
- [ ] Migration applied successfully
- [ ] All bot tables exist
- [ ] `DocumentChunks.Embedding` column has type `vector(1536)`
- [ ] Application starts without errors
- [ ] Can create bot configuration
- [ ] Can upload documents
- [ ] Can chat with bot

---

## 📚 Next Steps

Once migration is complete:

1. **Configure LLM Provider**
   - Get API key from OpenAI/Anthropic/Gemini
   - Create bot configuration via API

2. **Build Knowledge Base**
   - Upload tax law documents
   - Upload FAQ documents
   - Upload procedure documents

3. **Test System**
   - Test document upload
   - Test embedding generation
   - Test chat functionality
   - Test feedback system

4. **Optimize Performance**
   - Create vector index
   - Monitor query performance
   - Tune similarity threshold

5. **Monitor Usage**
   - Track conversation count
   - Monitor API costs
   - Review user feedback
   - Analyze popular topics

---

## 💡 Tips

### Getting API Keys

**OpenAI:**
1. Visit https://platform.openai.com/api-keys
2. Create new secret key
3. Copy and save securely

**Google Gemini:**
1. Visit https://makersuite.google.com/app/apikey
2. Create API key
3. Free tier available

**Anthropic:**
1. Visit https://console.anthropic.com/
2. Create API key
3. Requires payment method

### Cost Management

**Start with:**
- GPT-3.5-turbo for chat (~$18/month for 1000 conversations)
- OpenAI ada-002 for embeddings (~$0.50/month for 1000 docs)

**Monitor:**
- Check usage at https://platform.openai.com/usage
- Set spending limits
- Review monthly costs

---

## 🎯 Summary

**Current Status:**
- ✅ Migration created successfully
- ✅ Code updated to use PostgreSQL
- ✅ Connection string configured
- ⏳ PostgreSQL setup required
- ⏳ Migration application pending

**Required Actions:**
1. Install/start PostgreSQL
2. Create database `betts_tax_db`
3. Enable pgvector extension
4. Run `dotnet ef database update`
5. Verify tables created
6. Start application
7. Create bot configuration
8. Upload documents
9. Test chat

**Estimated Time:** 15-30 minutes

---

## 📞 Support

If you encounter issues:

1. Check PostgreSQL is running: `pg_isready`
2. Check connection: `psql -U postgres -d betts_tax_db`
3. Check logs: `logs/application.log`
4. Review error messages carefully
5. Ensure all credentials are correct

**Common Issues:**
- Wrong password → Update connection string
- Database doesn't exist → Create it first
- Extension not found → Install pgvector
- Port conflict → Change port in connection string

---

**Ready to proceed!** Follow the steps above to complete the migration. 🚀
