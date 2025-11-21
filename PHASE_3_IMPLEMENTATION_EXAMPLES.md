# Phase 3 Implementation Examples

This document provides code examples for implementing the missing business-specific workflows.

---

## 1. Payment Approval Workflow Example

### Service Interface
```csharp
public interface IPaymentApprovalWorkflow
{
    Task<Result<PaymentApprovalRequest>> RequestPaymentApprovalAsync(
        Guid paymentId, 
        decimal amount, 
        string requestedBy);
    
    Task<Result> ApprovePaymentAsync(
        Guid approvalId, 
        string approverId, 
        string? comments = null);
    
    Task<Result> RejectPaymentAsync(
        Guid approvalId, 
        string approverId, 
        string reason);
    
    Task<Result<List<PaymentApprovalRequest>>> GetPendingApprovalsAsync(
        string approverId);
}
```

### Service Implementation
```csharp
public class PaymentApprovalWorkflow : IPaymentApprovalWorkflow
{
    private readonly ApplicationDbContext _context;
    private readonly IEnhancedWorkflowService _workflowService;
    private readonly INotificationService _notificationService;
    private readonly ILogger<PaymentApprovalWorkflow> _logger;

    public async Task<Result<PaymentApprovalRequest>> RequestPaymentApprovalAsync(
        Guid paymentId, decimal amount, string requestedBy)
    {
        try
        {
            // Determine approval chain based on amount
            var approvalChain = DetermineApprovalChain(amount);
            
            // Create approval request
            var approvalRequest = new PaymentApprovalRequest
            {
                Id = Guid.NewGuid(),
                PaymentId = paymentId,
                Amount = amount,
                RequestedBy = requestedBy,
                Status = ApprovalStatus.Pending,
                ApprovalChain = JsonSerializer.Serialize(approvalChain),
                CreatedAt = DateTime.UtcNow
            };
            
            _context.PaymentApprovalRequests.Add(approvalRequest);
            await _context.SaveChangesAsync();
            
            // Notify first approver
            await NotifyApprover(approvalRequest, approvalChain[0]);
            
            return Result.Success(approvalRequest);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error requesting payment approval");
            return Result.Failure<PaymentApprovalRequest>(ex.Message);
        }
    }

    private List<string> DetermineApprovalChain(decimal amount)
    {
        // Thresholds in Sierra Leone Leones
        if (amount < 1_000_000)
            return new List<string> { "Associate" };
        else if (amount < 10_000_000)
            return new List<string> { "Associate", "Manager" };
        else
            return new List<string> { "Associate", "Manager", "Director" };
    }
}
```

---

## 2. Compliance Monitoring Workflow Example

### Service Interface
```csharp
public interface IComplianceMonitoringWorkflow
{
    Task<Result> MonitorDeadlinesAsync();
    Task<Result> UpdateComplianceStatusAsync(Guid clientId);
    Task<Result<ComplianceAlert>> GenerateComplianceAlertAsync(
        Guid clientId, 
        string alertType);
    Task<Result<decimal>> CalculatePenaltyAsync(
        Guid filingId, 
        int daysOverdue);
}
```

### Service Implementation
```csharp
public class ComplianceMonitoringWorkflow : IComplianceMonitoringWorkflow
{
    private readonly ApplicationDbContext _context;
    private readonly INotificationService _notificationService;
    private readonly ILogger<ComplianceMonitoringWorkflow> _logger;

    public async Task<Result> MonitorDeadlinesAsync()
    {
        try
        {
            var filings = await _context.TaxFilings
                .Where(f => f.Status == FilingStatus.Pending)
                .ToListAsync();

            foreach (var filing in filings)
            {
                var daysUntilDeadline = (filing.DueDate - DateTime.UtcNow).Days;
                
                // Send alerts at 30, 14, 7, 1 days before deadline
                if (daysUntilDeadline == 30 || daysUntilDeadline == 14 || 
                    daysUntilDeadline == 7 || daysUntilDeadline == 1)
                {
                    await GenerateComplianceAlertAsync(
                        filing.ClientId, 
                        $"Deadline approaching: {daysUntilDeadline} days");
                }
                
                // Check if overdue
                if (DateTime.UtcNow > filing.DueDate)
                {
                    filing.Status = FilingStatus.Overdue;
                    var penalty = await CalculatePenaltyAsync(
                        filing.Id, 
                        (int)(DateTime.UtcNow - filing.DueDate).TotalDays);
                    
                    await GenerateComplianceAlertAsync(
                        filing.ClientId, 
                        $"Filing overdue. Estimated penalty: {penalty:C}");
                }
            }
            
            await _context.SaveChangesAsync();
            return Result.Success();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error monitoring deadlines");
            return Result.Failure(ex.Message);
        }
    }

    public async Task<decimal> CalculatePenaltyAsync(Guid filingId, int daysOverdue)
    {
        // Finance Act 2025 penalty matrix
        // Example: 5% per month or part thereof
        var filing = await _context.TaxFilings.FindAsync(filingId);
        if (filing == null) return 0;
        
        var monthsOverdue = Math.Ceiling(daysOverdue / 30.0);
        var penaltyRate = 0.05m * (decimal)monthsOverdue;
        
        return filing.Amount * penaltyRate;
    }
}
```

---

## 3. Document Management Workflow Example

### Service Interface
```csharp
public interface IDocumentSubmissionWorkflow
{
    Task<Result<DocumentSubmission>> SubmitDocumentAsync(
        Guid clientId, 
        IFormFile file, 
        string documentType);
    
    Task<Result> VerifyDocumentAsync(
        Guid documentId, 
        string verificationStatus);
    
    Task<Result<List<DocumentRequirement>>> GetRequiredDocumentsAsync(
        Guid clientId, 
        int taxYear);
}
```

### Service Implementation
```csharp
public class DocumentSubmissionWorkflow : IDocumentSubmissionWorkflow
{
    private readonly ApplicationDbContext _context;
    private readonly IDocumentStorageService _storageService;
    private readonly INotificationService _notificationService;

    public async Task<Result<DocumentSubmission>> SubmitDocumentAsync(
        Guid clientId, IFormFile file, string documentType)
    {
        try
        {
            // Validate file
            if (file.Length > 10_000_000) // 10MB limit
                return Result.Failure<DocumentSubmission>("File too large");
            
            // Store file
            var fileUrl = await _storageService.UploadAsync(file);
            
            // Create document submission
            var submission = new DocumentSubmission
            {
                Id = Guid.NewGuid(),
                ClientId = clientId,
                DocumentType = documentType,
                FileUrl = fileUrl,
                Status = DocumentStatus.Submitted,
                SubmittedAt = DateTime.UtcNow,
                Version = 1
            };
            
            _context.DocumentSubmissions.Add(submission);
            await _context.SaveChangesAsync();
            
            // Notify admin for verification
            await _notificationService.NotifyAsync(
                "admin@thebettsfirmsl.com",
                "New Document Submitted",
                $"Client {clientId} submitted {documentType}");
            
            return Result.Success(submission);
        }
        catch (Exception ex)
        {
            return Result.Failure<DocumentSubmission>(ex.Message);
        }
    }
}
```

---

## 4. Communication Routing Workflow Example

### Service Interface
```csharp
public interface ICommunicationRoutingWorkflow
{
    Task<Result> RouteMessageAsync(
        Guid clientId, 
        string message, 
        string priority);
    
    Task<Result> AssignToTeamMemberAsync(
        Guid conversationId, 
        string teamMemberId);
    
    Task<Result> EscalateAsync(
        Guid conversationId, 
        string reason);
}
```

### Service Implementation
```csharp
public class CommunicationRoutingWorkflow : ICommunicationRoutingWorkflow
{
    private readonly ApplicationDbContext _context;
    private readonly INotificationService _notificationService;

    public async Task<Result> RouteMessageAsync(
        Guid clientId, string message, string priority)
    {
        try
        {
            // Determine routing based on priority
            var assignee = DetermineAssignee(priority);
            
            // Create conversation
            var conversation = new Conversation
            {
                Id = Guid.NewGuid(),
                ClientId = clientId,
                Priority = priority,
                Status = ConversationStatus.Open,
                AssignedTo = assignee,
                CreatedAt = DateTime.UtcNow
            };
            
            _context.Conversations.Add(conversation);
            
            // Add message
            var msg = new Message
            {
                Id = Guid.NewGuid(),
                ConversationId = conversation.Id,
                SenderId = clientId.ToString(),
                Content = message,
                CreatedAt = DateTime.UtcNow
            };
            
            _context.Messages.Add(msg);
            await _context.SaveChangesAsync();
            
            // Notify assignee
            await _notificationService.NotifyAsync(
                assignee,
                $"New {priority} Message",
                message);
            
            return Result.Success();
        }
        catch (Exception ex)
        {
            return Result.Failure(ex.Message);
        }
    }

    private string DetermineAssignee(string priority)
    {
        return priority switch
        {
            "Urgent" => GetDirectorEmail(),
            "High" => GetManagerEmail(),
            _ => GetAssociateEmail()
        };
    }
}
```

---

## 5. API Controller Example

```csharp
[ApiController]
[Route("api/[controller]")]
[Authorize]
public class WorkflowController : ControllerBase
{
    private readonly IEnhancedWorkflowService _workflowService;
    private readonly IPaymentApprovalWorkflow _paymentWorkflow;
    private readonly IComplianceMonitoringWorkflow _complianceWorkflow;

    [HttpPost("payments/approve")]
    [Authorize(Roles = "Admin,Manager")]
    public async Task<ActionResult> ApprovePayment(
        [FromBody] ApprovePaymentRequest request)
    {
        var result = await _paymentWorkflow.ApprovePaymentAsync(
            request.ApprovalId,
            User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "",
            request.Comments);
        
        return result.IsSuccess ? Ok(result) : BadRequest(result);
    }

    [HttpGet("compliance/alerts")]
    public async Task<ActionResult> GetComplianceAlerts()
    {
        var alerts = await _complianceWorkflow.GetComplianceAlertsAsync();
        return Ok(alerts);
    }

    [HttpPost("monitor-deadlines")]
    [Authorize(Roles = "Admin")]
    public async Task<ActionResult> MonitorDeadlines()
    {
        var result = await _complianceWorkflow.MonitorDeadlinesAsync();
        return result.IsSuccess ? Ok(result) : BadRequest(result);
    }
}
```

---

## 6. Background Job Example

```csharp
public class DeadlineMonitoringJob : IRecurringJob
{
    private readonly IComplianceMonitoringWorkflow _complianceWorkflow;
    private readonly ILogger<DeadlineMonitoringJob> _logger;

    public async Task ExecuteAsync()
    {
        try
        {
            _logger.LogInformation("Starting deadline monitoring job");
            var result = await _complianceWorkflow.MonitorDeadlinesAsync();
            
            if (result.IsSuccess)
                _logger.LogInformation("Deadline monitoring completed successfully");
            else
                _logger.LogError("Deadline monitoring failed: {Error}", result.Error);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error in deadline monitoring job");
        }
    }

    public string CronExpression => "0 9 * * *"; // Daily at 9 AM
}
```

---

## Integration with Existing Framework

All these workflows should be registered in the DI container:

```csharp
services.AddScoped<IPaymentApprovalWorkflow, PaymentApprovalWorkflow>();
services.AddScoped<IComplianceMonitoringWorkflow, ComplianceMonitoringWorkflow>();
services.AddScoped<IDocumentSubmissionWorkflow, DocumentSubmissionWorkflow>();
services.AddScoped<ICommunicationRoutingWorkflow, CommunicationRoutingWorkflow>();

// Register background jobs
services.AddHangfireJob<DeadlineMonitoringJob>();
services.AddHangfireJob<WorkflowTriggerEvaluationJob>();
services.AddHangfireJob<WorkflowCleanupJob>();
```

---

**Note:** These are simplified examples. Production implementations should include:
- Comprehensive error handling
- Detailed logging
- Performance optimization
- Security validation
- Audit trail tracking
- Unit and integration tests

