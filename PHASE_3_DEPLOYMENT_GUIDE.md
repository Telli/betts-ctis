# Phase 3 Deployment Guide

## Quick Start - 5 Steps to Production

### Step 1: Database Migration (5 minutes)

```bash
# Navigate to the project directory
cd BettsTax/BettsTax.Web

# Create migration
dotnet ef migrations add Phase3WorkflowImplementation --project ../BettsTax.Data --startup-project .

# Apply migration
dotnet ef database update --project ../BettsTax.Data --startup-project .
```

**Expected Output:**
```
Build started...
Build succeeded.
Done. To undo this action, use 'ef migrations remove'
Applying migration '20251029_Phase3WorkflowImplementation'...
Done.
```

---

### Step 2: Update Program.cs (Service Registration)

Add the following to your `Program.cs` file:

```csharp
// Add after other service registrations
// Phase 3 Workflow Services
services.AddScoped<IPaymentApprovalWorkflow, PaymentApprovalWorkflow>();
services.AddScoped<IComplianceMonitoringWorkflow, ComplianceMonitoringWorkflow>();
services.AddScoped<IDocumentManagementWorkflow, DocumentManagementWorkflow>();
services.AddScoped<ICommunicationRoutingWorkflow, CommunicationRoutingWorkflow>();

// Hangfire Configuration
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");
services.AddHangfireServices(connectionString);
```

Add after `app.UseRouting()`:

```csharp
// Hangfire Dashboard
app.UseHangfireDashboard();

// Configure recurring jobs
var recurringJobManager = app.Services.GetRequiredService<IRecurringJobManager>();
HangfireConfiguration.ConfigureRecurringJobs(recurringJobManager, app.Services);
```

---

### Step 3: Install Hangfire NuGet Package

```bash
cd BettsTax/BettsTax.Web

# Install Hangfire
dotnet add package Hangfire.SqlServer

# Install Hangfire Core (if not already installed)
dotnet add package Hangfire.Core
```

---

### Step 4: Seed Default Data (Optional but Recommended)

Create a seeding method in your database initialization:

```csharp
// Seed Payment Approval Thresholds
var thresholds = new[]
{
    new PaymentApprovalThreshold
    {
        Id = Guid.NewGuid(),
        MinAmount = 0,
        MaxAmount = 1000000,
        RequiredApprovers = "Associate",
        CreatedAt = DateTime.UtcNow
    },
    new PaymentApprovalThreshold
    {
        Id = Guid.NewGuid(),
        MinAmount = 1000000,
        MaxAmount = 10000000,
        RequiredApprovers = "Associate,Manager",
        CreatedAt = DateTime.UtcNow
    },
    new PaymentApprovalThreshold
    {
        Id = Guid.NewGuid(),
        MinAmount = 10000000,
        MaxAmount = decimal.MaxValue,
        RequiredApprovers = "Associate,Manager,Director",
        CreatedAt = DateTime.UtcNow
    }
};

context.PaymentApprovalThresholds.AddRange(thresholds);
await context.SaveChangesAsync();
```

---

### Step 5: Run and Test

```bash
# Build the solution
dotnet build

# Run the application
dotnet run

# Test the API
# Navigate to: https://localhost:5001/swagger/index.html
```

---

## Verification Checklist

### API Endpoints
- [ ] `GET /api/workflow/payment-approval/pending` - Returns 200
- [ ] `GET /api/workflow/compliance-monitoring/pending` - Returns 200
- [ ] `GET /api/workflow/document-management/pending-verifications` - Returns 200
- [ ] `GET /api/workflow/communication-routing/pending` - Returns 200

### Hangfire Dashboard
- [ ] Access Hangfire at `https://localhost:5001/hangfire`
- [ ] Verify 4 recurring jobs are scheduled
- [ ] Check job execution history

### Database
- [ ] Verify new tables created:
  - `PaymentApprovalRequests`
  - `PaymentApprovalSteps`
  - `PaymentApprovalThresholds`
  - `ComplianceMonitoringWorkflows`
  - `ComplianceMonitoringAlerts`
  - `CompliancePenaltyCalculations`
  - `DocumentSubmissionWorkflows`
  - `DocumentSubmissionSteps`
  - `DocumentVerificationResults`
  - `DocumentVersionControls`
  - `CommunicationRoutingWorkflows`
  - `CommunicationRoutingSteps`
  - `CommunicationRoutingRules`
  - `CommunicationEscalationRules`

---

## Testing the Workflows

### Test Payment Approval
```bash
curl -X POST https://localhost:5001/api/workflow/payment-approval/request \
  -H "Content-Type: application/json" \
  -H "Authorization: Bearer YOUR_TOKEN" \
  -d '{"paymentId": 1, "amount": 500000}'
```

### Test Compliance Monitoring
```bash
curl -X GET https://localhost:5001/api/workflow/compliance-monitoring/pending \
  -H "Authorization: Bearer YOUR_TOKEN"
```

### Test Document Management
```bash
curl -X POST https://localhost:5001/api/workflow/document-management/submit \
  -H "Content-Type: application/json" \
  -H "Authorization: Bearer YOUR_TOKEN" \
  -d '{"documentId": 1, "clientId": 1, "documentType": "TaxReturn"}'
```

### Test Communication Routing
```bash
curl -X POST https://localhost:5001/api/workflow/communication-routing/receive \
  -H "Content-Type: application/json" \
  -H "Authorization: Bearer YOUR_TOKEN" \
  -d '{
    "clientId": 1,
    "messageType": "Inquiry",
    "subject": "Tax Question",
    "content": "I have a question",
    "priority": "Normal",
    "channel": "Email"
  }'
```

---

## Troubleshooting

### Issue: Migration fails
**Solution**: 
- Ensure database connection string is correct
- Check database permissions
- Verify SQL Server is running

### Issue: Hangfire jobs not executing
**Solution**:
- Verify Hangfire storage is configured
- Check Hangfire dashboard for errors
- Review application logs

### Issue: API endpoints return 404
**Solution**:
- Verify services are registered in DI container
- Check controller routing
- Verify authorization headers

### Issue: Database tables not created
**Solution**:
- Run migration again: `dotnet ef database update`
- Check migration status: `dotnet ef migrations list`
- Review migration file for errors

---

## Production Deployment

### Before Going Live
1. Run all unit tests: `dotnet test`
2. Review all code changes
3. Backup production database
4. Test in staging environment
5. Verify all endpoints work
6. Check Hangfire job execution
7. Monitor application logs

### Deployment Steps
1. Build release: `dotnet build -c Release`
2. Run migrations on production database
3. Deploy application
4. Verify endpoints are accessible
5. Monitor Hangfire dashboard
6. Check application logs for errors

### Post-Deployment Monitoring
- Monitor background job execution
- Track API response times
- Monitor error logs
- Verify email notifications
- Check database performance

---

## Rollback Plan

If issues occur:

1. **Stop the application**
2. **Rollback database migration**:
   ```bash
   dotnet ef database update <previous-migration-name>
   ```
3. **Redeploy previous version**
4. **Verify system is operational**

---

## Support & Documentation

- **API Documentation**: Swagger UI at `/swagger`
- **Hangfire Dashboard**: `/hangfire`
- **Implementation Guide**: `PHASE_3_COMPLETE_IMPLEMENTATION_SUMMARY.md`
- **Unit Tests**: `BettsTax.Tests/Services/`

---

## Success Criteria

✅ All 4 workflows are operational  
✅ API endpoints return correct responses  
✅ Background jobs execute on schedule  
✅ Database tables are created  
✅ Unit tests pass  
✅ No errors in application logs  
✅ Hangfire dashboard shows job execution  

---

**Estimated Deployment Time**: 30-45 minutes  
**Difficulty Level**: Intermediate  
**Risk Level**: Low (with proper testing)

---

**Status**: Ready for deployment

