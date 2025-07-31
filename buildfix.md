# Build Fix Plan

## Build Error Summary
- **Total Errors**: 77
- **Total Warnings**: 147

## Error Categories

### 1. Missing Interface Methods (9 errors)
- `INotificationService` is missing `SendSmsAsync` and `SendEmailAsync` methods
- `IEmailService` is missing `SendEmailAsync` method
- These methods are being called in `MfaService` and `PaymentNotificationService`

### 2. Model Property Mismatches (55 errors)
- **AuditLog**: Service using wrong property names (e.g., `AuditLogId` instead of `Id`)
- **PaymentRetryAttempt**: Missing properties: `Status`, `Duration`, `NextRetryAt`, `Transaction`, `AttemptedBy`
- **PaymentTransaction**: Missing `RetryAttempts` property
- **PaymentScheduledRetry**: Missing properties: `TransactionId`, `ScheduledBy`, `Status`, `Transaction`
- **PaymentFailureRecord**: Missing properties: `TransactionId`, `Reason`, `HandledBy`
- **PaymentDeadLetterQueue**: Missing properties: `TransactionId`, `OriginalTransactionReference`, `Reason`, `RetryAttempts`, `TransactionData`, `ProcessingNotes`
- **ExciseDutyRate**: Model doesn't exist but is referenced in `TaxCalculationEngineService`
- **TaxRate**: Missing `UpdatedAt` and `UpdatedBy` properties
- **TaxPenaltyRule**: Missing properties for penalty calculation
- **TaxAllowance**: Model doesn't exist

### 3. Method Call Issues (7 errors)
- `IAuditService.LogAsync`: Method expects 5 parameters but services are calling with 10
- `Convert.ToBase32String` and `Convert.FromBase32String`: These methods don't exist
- `IPaymentGatewayService.ProcessPaymentAsync`: Missing overload with `processedBy` parameter
- `SendPaymentFailedEmailAsync` and `SendPaymentCancelledEmailAsync`: Methods don't exist

### 4. Type Mismatch Issues (6 errors)
- Comparing enums with strings (e.g., `TaxpayerCategory == "Large"`)
- Comparing `TaxYear` with int
- Comparing `PaymentStatus` with string
- String to enum conversions

### 5. Async/Await Warnings (147 warnings)
- Many async methods lack await operators and run synchronously

### 6. Null Reference Warnings (numerous)
- Possible null reference assignments and dereferences throughout the codebase

## Detailed Fix Plan

### Step 1: Fix Critical Interface Issues

#### 1.1 Update INotificationService
```csharp
public interface INotificationService
{
    Task<IEnumerable<Notification>> GetUserNotificationsAsync(string userId);
    Task<Notification> CreateAsync(string userId, string message);
    Task<bool> MarkReadAsync(int notificationId, string userId);
    
    // Add these methods
    Task<bool> SendSmsAsync(string phoneNumber, string message);
    Task<bool> SendEmailAsync(string email, string subject, string body);
}
```

#### 1.2 Create/Update IEmailService
```csharp
public interface IEmailService
{
    Task<bool> SendEmailAsync(string to, string subject, string body);
    // Add other email methods as needed
}
```

### Step 2: Fix Model Properties

#### 2.1 Update PaymentRetryAttempt
```csharp
public class PaymentRetryAttempt
{
    // Existing properties...
    
    // Add these properties
    public string Status { get; set; } = string.Empty;
    public string AttemptedBy { get; set; } = string.Empty;
    public TimeSpan Duration { get; set; }
    public DateTime? NextRetryAt { get; set; }
    public PaymentTransaction? Transaction { get; set; }
}
```

#### 2.2 Update PaymentTransaction
```csharp
public class PaymentTransaction
{
    // Existing properties...
    
    // Add this property
    public int RetryAttempts { get; set; } = 0;
}
```

#### 2.3 Create ExciseDutyRate Model
```csharp
public class ExciseDutyRate
{
    public int Id { get; set; }
    public string ProductName { get; set; } = string.Empty;
    public string RateType { get; set; } = string.Empty;
    public string UnitOfMeasure { get; set; } = string.Empty;
    public decimal Rate { get; set; }
    public DateTime EffectiveDate { get; set; }
    public DateTime? ExpiryDate { get; set; }
}
```

#### 2.4 Update TaxRate Model
```csharp
public class TaxRate
{
    // Existing properties...
    
    // Add these properties
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
    public string? UpdatedBy { get; set; }
}
```

### Step 3: Fix Method Signatures

#### 3.1 Fix IAuditService Usage
Two options:
1. Update the interface to accept more parameters, OR
2. Use `LogClientPortalActivityAsync` which already accepts the needed parameters

Recommendation: Use option 2 as the interface already has the appropriate method.

#### 3.2 Fix Base32 Encoding
Install a Base32 NuGet package or implement custom Base32 encoding:
```bash
dotnet add package Base32
```

Then update MfaService to use the library instead of `Convert.ToBase32String`.

### Step 4: Fix Type Mismatches

#### 4.1 Fix Enum Comparisons
Change from:
```csharp
client.TaxpayerCategory == "Large"
```
To:
```csharp
client.TaxpayerCategory == TaxpayerCategory.Large
```

#### 4.2 Fix TaxYear Comparisons
Change from:
```csharp
taxYear == 2024
```
To:
```csharp
taxYear.Year == 2024
```

### Step 5: Add Missing Enum Values

Update Enums.cs to include:
- `PaymentRetryStatus.InProgress`
- `PaymentRetryStatus.Scheduled`
- `PaymentTransactionStatus.DeadLetter`
- `DeadLetterStatus.Discarded`
- `PaymentFailureType.Permanent`

### Step 6: Fix Async/Await Issues

For methods that don't need async operations, either:
1. Remove the async keyword and return `Task.CompletedTask`, OR
2. Add `await Task.CompletedTask;` at the end

### Implementation Order

1. **Phase 1 - Critical Fixes** (Enables build to succeed)
   - Fix INotificationService interface
   - Fix model properties
   - Fix method signatures
   - Fix type mismatches

2. **Phase 2 - Cleanup** (Reduces warnings)
   - Fix async/await warnings
   - Address null reference warnings

3. **Phase 3 - Testing**
   - Run build after each phase
   - Run tests to ensure no regressions

## Estimated Time
- Phase 1: 2-3 hours
- Phase 2: 1-2 hours
- Phase 3: 1 hour
- Total: 4-6 hours