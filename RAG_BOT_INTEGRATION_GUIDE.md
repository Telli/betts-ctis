# RAG Bot Integration Guide

**Date:** November 17, 2025  
**Status:** Ready for Migration and Testing

---

## ✅ Completed Steps

### 1. Code Implementation
- ✅ Created 8 data models for bot system
- ✅ Implemented 3 LLM providers (OpenAI, Anthropic, Gemini)
- ✅ Built RAG orchestration service
- ✅ Created document processing pipeline
- ✅ Added user and admin API controllers
- ✅ Implemented encryption service for API keys
- ✅ Updated ApplicationDbContext with bot entities
- ✅ Configured pgvector for semantic search
- ✅ Registered services in Program.cs
- ✅ Added Pgvector.EntityFrameworkCore NuGet package

---

## 🚀 Next Steps for Integration

### Step 1: Restore NuGet Packages

```bash
cd c:\Users\telli\Desktop\Betts\Betts\BettsTax
dotnet restore
```

This will install the `Pgvector.EntityFrameworkCore` package.

---

### Step 2: Enable pgvector Extension in PostgreSQL

Connect to your PostgreSQL database and run:

```sql
-- Connect to your BettsTax database
\c betts_tax_db

-- Enable pgvector extension
CREATE EXTENSION IF NOT EXISTS vector;

-- Verify installation
SELECT * FROM pg_extension WHERE extname = 'vector';
```

**Alternative via psql command line:**
```bash
psql -U postgres -d betts_tax_db -c "CREATE EXTENSION IF NOT EXISTS vector;"
```

---

### Step 3: Create and Apply Migration

```bash
cd c:\Users\telli\Desktop\Betts\Betts\BettsTax

# Create migration
dotnet ef migrations add AddRAGBotSystem --project BettsTax.Data\BettsTax.Data.csproj --startup-project BettsTax.Web\BettsTax.Web.csproj

# Review the migration file (optional)
# Check BettsTax.Data\Migrations\<timestamp>_AddRAGBotSystem.cs

# Apply migration
dotnet ef database update --project BettsTax.Data\BettsTax.Data.csproj --startup-project BettsTax.Web\BettsTax.Web.csproj
```

**Expected Tables Created:**
- `BotConfigurations`
- `KnowledgeDocuments`
- `DocumentChunks` (with vector column)
- `BotConversations`
- `BotMessages`
- `BotFeedbacks`
- `EmbeddingJobs`

---

### Step 4: Create Initial Bot Configuration

After the application starts, create a bot configuration via API:

**Option A: Using OpenAI (Recommended)**

```bash
curl -X POST https://localhost:5001/api/admin/bot/configurations \
  -H "Content-Type: application/json" \
  -H "Authorization: Bearer YOUR_ADMIN_TOKEN" \
  -d '{
    "name": "Default OpenAI Configuration",
    "provider": 0,
    "modelName": "gpt-3.5-turbo",
    "apiKey": "sk-YOUR_OPENAI_API_KEY",
    "temperature": 0.7,
    "maxTokens": 1000,
    "topK": 5,
    "similarityThreshold": 0.7,
    "systemPrompt": "You are a helpful tax compliance assistant for Sierra Leone. Provide accurate, concise answers based on the context provided. If you don'\''t know something, say so. Always cite your sources from the knowledge base.",
    "isActive": true,
    "isDefault": true
  }'
```

**Option B: Using Google Gemini (Free Tier Available)**

```bash
curl -X POST https://localhost:5001/api/admin/bot/configurations \
  -H "Content-Type: application/json" \
  -H "Authorization: Bearer YOUR_ADMIN_TOKEN" \
  -d '{
    "name": "Gemini Pro Configuration",
    "provider": 2,
    "modelName": "gemini-pro",
    "apiKey": "YOUR_GEMINI_API_KEY",
    "temperature": 0.7,
    "maxTokens": 1000,
    "topK": 5,
    "similarityThreshold": 0.7,
    "systemPrompt": "You are a helpful tax compliance assistant for Sierra Leone.",
    "isActive": true,
    "isDefault": true
  }'
```

**Provider Enum Values:**
- `0` = OpenAI
- `1` = Anthropic
- `2` = Gemini

---

### Step 5: Upload Knowledge Base Documents

Create sample documents for testing:

**Example 1: GST Information**

```bash
curl -X POST https://localhost:5001/api/admin/bot/documents \
  -H "Content-Type: application/json" \
  -H "Authorization: Bearer YOUR_ADMIN_TOKEN" \
  -d '{
    "title": "GST Filing Requirements in Sierra Leone",
    "content": "Goods and Services Tax (GST) in Sierra Leone:\n\n1. Filing Deadline: GST returns must be filed within 21 days of the end of each tax period.\n\n2. Tax Rate: The standard GST rate is 15% on most goods and services.\n\n3. Registration Threshold: Businesses with annual turnover exceeding Le 200 million must register for GST.\n\n4. Filing Frequency: Monthly for businesses with turnover over Le 500 million, quarterly for others.\n\n5. Penalties: Late filing incurs a penalty of 5% of the tax due, plus 1% interest per month.\n\n6. Zero-Rated Items: Exports, basic food items, and medical supplies are zero-rated.\n\n7. Exempt Items: Financial services, education, and residential rent are exempt from GST.",
    "category": "Tax Law",
    "tags": ["GST", "Filing", "Deadlines", "Rates"]
  }'
```

**Example 2: Corporate Income Tax**

```bash
curl -X POST https://localhost:5001/api/admin/bot/documents \
  -H "Content-Type: application/json" \
  -H "Authorization: Bearer YOUR_ADMIN_TOKEN" \
  -d '{
    "title": "Corporate Income Tax in Sierra Leone",
    "content": "Corporate Income Tax (CIT) in Sierra Leone:\n\n1. Tax Rate: The standard corporate tax rate is 30% of taxable income.\n\n2. Filing Deadline: Corporate tax returns must be filed within 4 months (120 days) of the end of the accounting period.\n\n3. Payment: Tax is payable in quarterly installments based on estimated income.\n\n4. Deductions: Allowable deductions include operating expenses, depreciation, and employee costs.\n\n5. Capital Allowances: Initial allowance of 40% and annual allowance of 20% on qualifying capital expenditure.\n\n6. Loss Carryforward: Tax losses can be carried forward for up to 5 years.\n\n7. Withholding Tax: 10% withholding tax on dividends, 15% on interest, and 25% on royalties paid to non-residents.",
    "category": "Tax Law",
    "tags": ["Corporate Tax", "Income Tax", "CIT", "Deadlines"]
  }'
```

**Example 3: PAYE Information**

```bash
curl -X POST https://localhost:5001/api/admin/bot/documents \
  -H "Content-Type: application/json" \
  -H "Authorization: Bearer YOUR_ADMIN_TOKEN" \
  -d '{
    "title": "PAYE (Pay As You Earn) in Sierra Leone",
    "content": "PAYE System in Sierra Leone:\n\n1. Tax Rates: Progressive rates from 0% to 30% based on income brackets.\n\n2. Filing Deadline: PAYE returns must be filed within 21 days of the end of each month.\n\n3. Income Brackets (2024):\n   - Le 0 - 600,000: 0%\n   - Le 600,001 - 1,200,000: 15%\n   - Le 1,200,001 - 2,400,000: 20%\n   - Le 2,400,001 - 4,800,000: 25%\n   - Above Le 4,800,000: 30%\n\n4. Employer Obligations: Employers must deduct PAYE from employee salaries and remit to NRA.\n\n5. Annual Returns: Employers must submit annual PAYE returns by January 31 of the following year.\n\n6. Penalties: Late payment incurs 5% penalty plus 1.5% monthly interest.",
    "category": "Tax Law",
    "tags": ["PAYE", "Income Tax", "Employment", "Payroll"]
  }'
```

**Monitor Processing:**

```bash
# Check embedding job status
curl https://localhost:5001/api/admin/bot/jobs/{jobId} \
  -H "Authorization: Bearer YOUR_ADMIN_TOKEN"
```

---

### Step 6: Test Bot Chat

**Start a new conversation:**

```bash
curl -X POST https://localhost:5001/api/bot/chat \
  -H "Content-Type: application/json" \
  -H "Authorization: Bearer YOUR_USER_TOKEN" \
  -d '{
    "conversationId": null,
    "message": "What is the GST filing deadline in Sierra Leone?"
  }'
```

**Expected Response:**
```json
"Based on the GST filing requirements in Sierra Leone, GST returns must be filed within 21 days of the end of each tax period. The filing frequency depends on your business turnover - monthly for businesses with turnover over Le 500 million, and quarterly for others."
```

**Continue conversation:**

```bash
curl -X POST https://localhost:5001/api/bot/chat \
  -H "Content-Type: application/json" \
  -H "Authorization: Bearer YOUR_USER_TOKEN" \
  -d '{
    "conversationId": 1,
    "message": "What happens if I file late?"
  }'
```

---

### Step 7: Create Vector Index (Performance Optimization)

After uploading several documents, create an index for faster similarity search:

```sql
-- Connect to database
\c betts_tax_db

-- Create IVFFlat index for cosine similarity
CREATE INDEX ON "DocumentChunks" 
USING ivfflat ("Embedding" vector_cosine_ops)
WITH (lists = 100);

-- For larger datasets (>100k chunks), use HNSW index
-- CREATE INDEX ON "DocumentChunks" 
-- USING hnsw ("Embedding" vector_cosine_ops);
```

**Index Types:**
- **IVFFlat**: Good for up to 1M vectors, faster build time
- **HNSW**: Better for >1M vectors, slower build but faster queries

---

## 🧪 Testing Checklist

### Functional Tests

- [ ] **Configuration Management**
  - [ ] Create bot configuration
  - [ ] Update configuration
  - [ ] Switch between providers (OpenAI, Gemini, Anthropic)
  - [ ] Set default configuration

- [ ] **Document Upload**
  - [ ] Upload text document
  - [ ] Check embedding job status
  - [ ] Verify chunks created
  - [ ] Verify embeddings generated

- [ ] **Chat Functionality**
  - [ ] Start new conversation
  - [ ] Continue existing conversation
  - [ ] Receive contextual answers
  - [ ] Test without knowledge base (fallback)

- [ ] **Feedback System**
  - [ ] Submit positive feedback
  - [ ] Submit negative feedback
  - [ ] View feedback analytics

- [ ] **Admin Features**
  - [ ] View all documents
  - [ ] Delete document
  - [ ] Reprocess embeddings
  - [ ] View usage statistics
  - [ ] View popular topics

### Performance Tests

- [ ] **Response Time**
  - [ ] Chat response < 3 seconds
  - [ ] Document upload < 1 second
  - [ ] Embedding generation < 30 seconds per document

- [ ] **Accuracy**
  - [ ] Relevant context retrieved (similarity > 0.7)
  - [ ] Answers cite knowledge base
  - [ ] No hallucinations on unknown topics

### Security Tests

- [ ] **Authentication**
  - [ ] Chat requires authentication
  - [ ] Admin endpoints require admin role
  - [ ] Users can only access own conversations

- [ ] **Data Protection**
  - [ ] API keys encrypted at rest
  - [ ] Conversations isolated by user
  - [ ] No PII leakage in logs

---

## 📊 Monitoring and Analytics

### Key Metrics to Track

1. **Usage Metrics**
   ```sql
   -- Total conversations
   SELECT COUNT(*) FROM "BotConversations";
   
   -- Messages per day
   SELECT DATE("Timestamp"), COUNT(*) 
   FROM "BotMessages" 
   GROUP BY DATE("Timestamp") 
   ORDER BY DATE("Timestamp") DESC;
   
   -- Average messages per conversation
   SELECT AVG(message_count) 
   FROM (
     SELECT "BotConversationId", COUNT(*) as message_count 
     FROM "BotMessages" 
     GROUP BY "BotConversationId"
   ) subquery;
   ```

2. **Quality Metrics**
   ```sql
   -- Average rating
   SELECT AVG("Rating") FROM "BotFeedbacks";
   
   -- Helpful percentage
   SELECT 
     COUNT(CASE WHEN "WasHelpful" = true THEN 1 END) * 100.0 / COUNT(*) 
   FROM "BotFeedbacks";
   ```

3. **Performance Metrics**
   ```sql
   -- Embedding job success rate
   SELECT 
     "Status", 
     COUNT(*) 
   FROM "EmbeddingJobs" 
   GROUP BY "Status";
   
   -- Average processing time
   SELECT 
     AVG(EXTRACT(EPOCH FROM ("CompletedDate" - "StartedDate"))) as avg_seconds
   FROM "EmbeddingJobs" 
   WHERE "Status" = 'Completed';
   ```

---

## 🔧 Troubleshooting

### Issue: Migration Fails with "vector type does not exist"

**Solution:**
```sql
-- Ensure pgvector extension is installed
CREATE EXTENSION IF NOT EXISTS vector;

-- Verify installation
SELECT * FROM pg_extension WHERE extname = 'vector';
```

### Issue: "No active bot configuration found"

**Solution:**
Create a bot configuration via admin API (see Step 4).

### Issue: Embeddings not generating

**Possible Causes:**
1. Invalid API key
2. Rate limit exceeded
3. Network connectivity

**Debug:**
```bash
# Check embedding job status
curl https://localhost:5001/api/admin/bot/jobs/{jobId}

# Check logs
tail -f logs/application.log | grep "Embedding"
```

### Issue: Bot responses not using context

**Possible Causes:**
1. Similarity threshold too high
2. No relevant documents in knowledge base
3. Embedding dimension mismatch

**Solution:**
```bash
# Lower similarity threshold
curl -X PUT https://localhost:5001/api/admin/bot/configurations/{id} \
  -d '{"similarityThreshold": 0.5}'

# Verify chunks exist
SELECT COUNT(*) FROM "DocumentChunks" WHERE "Embedding" IS NOT NULL;
```

### Issue: Slow query performance

**Solution:**
```sql
-- Create vector index
CREATE INDEX ON "DocumentChunks" 
USING ivfflat ("Embedding" vector_cosine_ops)
WITH (lists = 100);

-- Analyze query plan
EXPLAIN ANALYZE 
SELECT * FROM "DocumentChunks" 
ORDER BY "Embedding" <=> '[...]' 
LIMIT 5;
```

---

## 💰 Cost Management

### OpenAI Pricing (as of Nov 2024)

**GPT-3.5-turbo** (Recommended for production):
- Input: $0.0015 per 1K tokens
- Output: $0.002 per 1K tokens
- **Estimated cost:** ~$18/month for 1000 conversations

**GPT-4**:
- Input: $0.03 per 1K tokens
- Output: $0.06 per 1K tokens
- **Estimated cost:** ~$450/month for 1000 conversations

**Embeddings (text-embedding-ada-002)**:
- $0.0001 per 1K tokens
- **Estimated cost:** ~$0.50/month for 1000 documents

### Cost Optimization Tips

1. **Use GPT-3.5-turbo** for most queries, GPT-4 for complex cases
2. **Cache embeddings** (already implemented)
3. **Limit conversation history** to last 10 messages (already implemented)
4. **Set max tokens** appropriately (default 1000)
5. **Monitor usage** via analytics dashboard

---

## 🎓 Best Practices

### Knowledge Base Management

1. **Document Structure**
   - Keep documents focused on single topics
   - Use clear, concise language
   - Include examples and specific numbers
   - Update regularly with latest regulations

2. **Categorization**
   - Use consistent categories (Tax Law, Procedures, FAQ)
   - Tag documents thoroughly
   - Link related documents

3. **Quality Control**
   - Review bot responses regularly
   - Update documents based on feedback
   - Remove outdated information

### System Prompt Engineering

**Good System Prompt:**
```
You are a helpful tax compliance assistant for Sierra Leone. 

Guidelines:
1. Always cite sources from the knowledge base
2. If you don't know something, say so clearly
3. Provide specific numbers, dates, and references
4. Suggest next steps or related topics
5. Use simple, professional language
6. Never make up information
```

**Bad System Prompt:**
```
You are a tax expert. Answer questions.
```

### Conversation Management

1. **Archive old conversations** after 90 days
2. **Analyze popular topics** to improve knowledge base
3. **Monitor feedback** to identify issues
4. **Track user satisfaction** metrics

---

## 📝 Summary

### What's Been Done ✅

1. ✅ Complete RAG bot implementation
2. ✅ 3 LLM providers (OpenAI, Anthropic, Gemini)
3. ✅ pgvector integration for semantic search
4. ✅ Document processing with automatic chunking
5. ✅ Conversation management with history
6. ✅ Admin configuration and analytics
7. ✅ API key encryption
8. ✅ Database schema and migrations ready
9. ✅ Service registration complete

### What's Next 🚀

1. Run `dotnet restore` to install packages
2. Enable pgvector extension in PostgreSQL
3. Create and apply migration
4. Create initial bot configuration
5. Upload knowledge base documents
6. Test chat functionality
7. Create vector index for performance
8. Monitor usage and iterate

### Estimated Time to Production

- **Setup:** 30 minutes
- **Knowledge base creation:** 2-4 hours
- **Testing:** 1-2 hours
- **Optimization:** 1 hour
- **Total:** ~4-8 hours

---

## 🆘 Support

For issues or questions:
1. Check troubleshooting section above
2. Review logs in `logs/application.log`
3. Check database with SQL queries provided
4. Verify API keys and configuration
5. Test with simple queries first

**Ready to proceed with Step 1!** 🎉
