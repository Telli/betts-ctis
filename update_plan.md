# Sierra Leone CTIS Dashboard Implementation Plan

## Overview
This document outlines the comprehensive implementation plan for enhancing the Sierra Leone Client Tax Information System (CTIS) dashboard interface to match the design system requirements and complete missing functionality.

## ✅ IMPLEMENTATION STATUS UPDATE (Current)
**Last Updated**: Current Session
**Major Milestone**: Database Integration & Currency Standardization Complete

### Recently Completed Work
- ✅ **Database Integration**: All pages now connect to backend APIs instead of using hardcoded mock data
- ✅ **Currency Standardization**: Complete conversion to Sierra Leone Leones (Le) across all pages
- ✅ **Empty State Handling**: Implemented proper empty states when APIs fail instead of fallback mock data
- ✅ **Service Layer**: Created comprehensive service layer (AnalyticsService, ComplianceService, NotificationService, DocumentService, DeadlineService)
- ✅ **Error Handling**: Fixed null pointer exceptions across all pages with proper optional chaining
- ✅ **Navigation Updates**: Replaced dollar sign icons with credit card icons in navigation menus
- ✅ **Payment Integration**: Updated payment icons to Sierra Leone providers (Orange Money, Africell Money, etc.)

### Key Files Modified/Created
- ✅ `lib/utils/currency.ts` - Sierra Leone currency formatting utilities
- ✅ `components/ui/payment-method-icon.tsx` - Sierra Leone payment provider icons
- ✅ `lib/services/` - Complete service layer with TypeScript interfaces
- ✅ `app/payments/page.tsx` - Full currency standardization
- ✅ `components/sidebar.tsx` - Navigation icon updates
- ✅ All page components - Null safety and empty state handling

### Technical Achievements
- ✅ **Type Safety**: Complete TypeScript interfaces for all service contracts
- ✅ **Null Safety**: Optional chaining and nullish coalescing throughout
- ✅ **User Experience**: New users see zeros/empty lists, demo users see populated data
- ✅ **Currency Localization**: Sierra Leone Leones formatting with proper Le symbol
- ✅ **Error Resilience**: Graceful degradation when APIs are unavailable

## Project Scope
- **Frontend**: Next.js 15.2.4 with React 19 and TypeScript
- **Backend**: ASP.NET Core 9.0 Web API (.NET 9)
- **Design System**: Sierra Leone custom theme with shadcn/ui components
- **Target**: Production-ready dashboard with complete navigation and API integration

## 1. Dashboard Implementation

### 1.1 Current State Analysis
- Two dashboard implementations exist: `sierra-leone-ctis/app/page.tsx` and `sierra-leone-ctis/app/dashboard/page.tsx`
- The dedicated dashboard page is more complete and follows proper structure
- Missing comprehensive widget integration and proper data flow

### 1.2 Dashboard Consolidation Strategy

#### Main Landing Page Redirect
**File**: `sierra-leone-ctis/app/page.tsx`
- Convert to authentication-based redirect component
- Route authenticated users to `/dashboard`
- Route unauthenticated users to `/login`

#### Enhanced Dashboard Page
**File**: `sierra-leone-ctis/app/dashboard/page.tsx`
- Implement tabbed interface for better organization
- Add refresh functionality with loading states
- Integrate all dashboard widgets:
  - Client Summary Cards
  - Compliance Overview
  - Recent Activity List
  - Upcoming Deadlines
  - Pending Approvals

### 1.3 Required Dashboard Components

#### Client Summary Card
**File**: `sierra-leone-ctis/components/dashboard/client-summary-card.tsx`
- Display total tax liability, active clients, pending filings, compliance score
- Use Sierra Leone color scheme for status indicators
- Include percentage change indicators

#### Compliance Overview
**File**: `sierra-leone-ctis/components/dashboard/compliance-overview.tsx`
- Show tax type compliance status (GST, Payroll, Income Tax, Excise Duty)
- Progress bars with Sierra Leone Finance Act 2025 compliance indicators
- Color-coded status system

#### Recent Activity List
**File**: `sierra-leone-ctis/components/dashboard/recent-activity-list.tsx`
- Timeline-based activity feed
- Activity types: filings, payments, document uploads, compliance checks
- Clickable items linking to relevant pages

#### Pending Approvals
**File**: `sierra-leone-ctis/components/dashboard/pending-approvals.tsx`
- Payment approval workflow interface
- Client-specific approval items
- Action buttons for approve/reject with proper authorization

## 2. Navigation Menu Implementation

### 2.1 Enhanced Sidebar Component
**File**: `sierra-leone-ctis/components/sidebar.tsx`

#### Navigation Structure
```
Dashboard
├── Clients (156)
├── Tax Filings (23)
├── Payments
├── Compliance
├── Tax Calculator
├── Documents
├── Deadlines (5)
├── Analytics
├── Notifications (3)
├── Settings
└── Help & Support
```

#### Features
- Collapsible sidebar with toggle button
- Badge indicators for item counts
- Active state highlighting with Sierra Leone blue
- Responsive design for mobile devices
- The Betts Firm branding integration

### 2.2 Layout Integration
**File**: `sierra-leone-ctis/app/layout.tsx`
- Integrate sidebar into main layout
- Implement flex-based layout system
- Maintain authentication context
- Add toast notification system

## 3. Design System Implementation

### 3.1 Sierra Leone Color Palette
**File**: `sierra-leone-ctis/tailwind.config.ts`

#### Color Scheme
- **Sierra Blue**: Primary navigation and action colors
  - `sierra-blue-600`: Primary buttons and active states
  - `sierra-blue-50`: Light backgrounds and hover states
- **Sierra Gold**: Accent colors for warnings and highlights
  - `sierra-gold-500`: Warning indicators
  - `sierra-gold-100`: Subtle accent backgrounds
- **Sierra Green**: Success states and positive indicators
  - `sierra-green-600`: Success buttons and positive metrics
  - `sierra-green-100`: Success background states

#### Typography & Spacing
- Consistent font sizing using Inter font family
- Standardized spacing scale (4px base unit)
- Responsive breakpoints aligned with container queries

### 3.2 Component Consistency
- All components follow shadcn/ui patterns
- Consistent border radius and shadow usage
- Standardized animation timing and easing
- Accessible color contrast ratios

## 4. Missing API Implementations

### 4.1 Tax Filings Controller
**File**: `BettsTax/BettsTax.Web/Controllers/TaxFilingsController.cs`

#### Endpoints
- `GET /api/tax-filings` - Paginated list with filtering (Admin/Associate with permissions)
- `GET /api/tax-filings/{id}` - Individual filing details (Client/Associate with permissions)
- `GET /api/tax-filings/client/{clientId}` - Client-specific filings (Client/Associate with permissions)
- `GET /api/tax-filings/associate/delegated` - Filings the associate can manage on behalf of clients
- `POST /api/tax-filings` - Create new filing (Client/Associate with permissions)
- `POST /api/tax-filings/client/{clientId}` - Create filing on behalf of client (Associate with permissions)
- `PUT /api/tax-filings/{id}` - Update existing filing (Client/Associate with permissions)
- `PUT /api/tax-filings/{id}/on-behalf` - Update filing on behalf of client (Associate with permissions)
- `DELETE /api/tax-filings/{id}` - Delete filing (Admin/Associate with delete permissions)
- `POST /api/tax-filings/{id}/submit` - Submit filing for processing (Client/Associate with submit permissions)
- `POST /api/tax-filings/{id}/submit-on-behalf` - Submit filing on behalf of client (Associate with submit permissions)

#### Associate Permission Features
- **Client Delegation**: Associates can manage filings for specifically delegated clients
- **Permission Levels**: Read, Create, Update, Delete, Submit permissions per client
- **Admin Delegation**: Admins can grant/revoke associate permissions at granular levels
- **Audit Trail**: All on-behalf actions logged with associate and client information
- **Bulk Operations**: Associates can perform bulk operations on delegated clients' filings
- **Permission Inheritance**: System admins can set default permission templates for associates

#### Authorization Attributes
```csharp
[AssociatePermission("TaxFilings", "Read", ClientIdSource.Route)]
[AssociatePermission("TaxFilings", "Create", ClientIdSource.Body)]
[AssociatePermission("TaxFilings", "Update", ClientIdSource.Route)]
[AssociatePermission("TaxFilings", "Delete", ClientIdSource.Route)]
[AssociatePermission("TaxFilings", "Submit", ClientIdSource.Route)]
```

#### Features
- **Multi-level Authorization**: Client access, Associate delegation, Admin oversight
- **Granular Permissions**: Per-client, per-action permission management
- **Comprehensive Audit Logging**: Track all actions including on-behalf operations
- **Validation using FluentValidation**: Input validation with delegation context
- **Proper Error Handling**: Permission-aware error messages and status codes

### 4.2 Payments Controller
**File**: `BettsTax/BettsTax.Web/Controllers/PaymentsController.cs`

#### Endpoints
- `GET /api/payments` - Paginated payment list (Admin/Associate with permissions)
- `GET /api/payments/{id}` - Payment details (Client/Associate with permissions)
- `GET /api/payments/client/{clientId}` - Client-specific payments (Client/Associate with permissions)
- `GET /api/payments/associate/delegated` - Payments the associate can manage
- `GET /api/payments/pending-approvals` - Approval queue (Admin/Associate with approval permissions)
- `POST /api/payments` - Create payment record (Client/Associate with permissions)
- `POST /api/payments/client/{clientId}` - Create payment on behalf of client (Associate with permissions)
- `PUT /api/payments/{id}` - Update payment record (Client/Associate with permissions)
- `PUT /api/payments/{id}/on-behalf` - Update payment on behalf of client (Associate with permissions)
- `POST /api/payments/{id}/approve` - Approve payment (Admin/Associate with approval permissions)
- `POST /api/payments/{id}/reject` - Reject payment with reason (Admin/Associate with approval permissions)
- `POST /api/payments/{id}/approve-on-behalf` - Approve payment on behalf of client (Associate with approval permissions)

#### Associate Payment Management Features
- **Payment Delegation**: Associates can create and manage payments for delegated clients
- **Approval Permissions**: Associates can be granted approval rights for specific clients or payment types
- **Payment Method Management**: Associates can manage payment methods for delegated clients
- **Bulk Payment Processing**: Associates can process multiple payments for multiple clients
- **Payment Plan Management**: Associates can set up and manage payment plans on behalf of clients
- **Receipt Generation**: Associates can generate receipts and payment confirmations for clients

#### Approval Workflow with Delegation
- **Multi-level Approval System**: Client → Associate → Admin approval hierarchy
- **Delegation-aware Approvals**: Associates can approve within their permission limits
- **Escalation Rules**: Automatic escalation based on amount thresholds and client types
- **Audit Trail**: Comprehensive logging of all approval actions with delegation context
- **Email Notifications**: Automated notifications for approval requests and delegation changes

### 4.3 Documents Controller
**File**: `BettsTax/BettsTax.Web/Controllers/DocumentsController.cs`

#### Endpoints
- `GET /api/documents` - Document listing with metadata (Admin/Associate with permissions)
- `GET /api/documents/client/{clientId}` - Client documents (Client/Associate with permissions)
- `GET /api/documents/associate/delegated` - Documents for all delegated clients
- `GET /api/documents/{id}` - Individual document details (Client/Associate with permissions)
- `POST /api/documents/upload` - Secure file upload (Client/Associate with permissions)
- `POST /api/documents/upload/client/{clientId}` - Upload document on behalf of client (Associate with permissions)
- `POST /api/documents/bulk-upload` - Bulk document upload for multiple clients (Associate with permissions)
- `GET /api/documents/{id}/download` - Secure file download (Client/Associate with permissions)
- `PUT /api/documents/{id}` - Update document metadata (Client/Associate with permissions)
- `PUT /api/documents/{id}/on-behalf` - Update document on behalf of client (Associate with permissions)
- `DELETE /api/documents/{id}` - Document deletion (Client/Associate with delete permissions)
- `POST /api/documents/{id}/share` - Share document with associate or client (Client/Associate with share permissions)
- `POST /api/documents/organize/client/{clientId}` - Organize client documents (Associate with organize permissions)

#### Associate Document Management Features
- **Document Delegation**: Associates can upload and manage documents for delegated clients
- **Bulk Document Operations**: Upload, organize, and categorize documents across multiple clients
- **Document Templates**: Associates can create and apply document templates for clients
- **Document Workflows**: Set up document approval and review workflows
- **Version Control**: Manage document versions on behalf of clients
- **Document Sharing**: Controlled sharing between associates and clients
- **Document Organization**: Create folder structures and categorization for client documents

#### Security Features with Delegation
- **Permission-based Access**: Fine-grained access control based on associate permissions
- **File Type Validation**: Virus scanning with delegation context logging
- **Secure File Storage**: Encryption with delegation audit trails
- **Access Control**: Multi-layered access based on client relationships and associate permissions
- **Comprehensive Audit Logging**: Track all document operations including on-behalf actions
- **Document Encryption**: Client-specific encryption keys with associate access controls

### 4.4 Admin Permission Delegation Controller
**File**: `BettsTax/BettsTax.Web/Controllers/AdminPermissionsController.cs`

#### Endpoints
- `GET /api/admin/permissions/associates` - List all associates with their permission summary
- `GET /api/admin/permissions/associate/{associateId}` - Get detailed permissions for specific associate
- `GET /api/admin/permissions/client/{clientId}/delegates` - Get all associates with permissions for a client
- `POST /api/admin/permissions/grant` - Grant specific permissions to associate for client(s)
- `POST /api/admin/permissions/revoke` - Revoke specific permissions from associate
- `POST /api/admin/permissions/bulk-grant` - Grant permissions to multiple associates for multiple clients
- `POST /api/admin/permissions/bulk-revoke` - Revoke permissions in bulk
- `GET /api/admin/permissions/templates` - Get permission templates
- `POST /api/admin/permissions/templates` - Create new permission template
- `PUT /api/admin/permissions/templates/{id}` - Update permission template
- `POST /api/admin/permissions/apply-template` - Apply template to associate(s)
- `GET /api/admin/permissions/audit` - Get permission change audit log
- `POST /api/admin/permissions/expire/{associateId}` - Set expiry dates for associate permissions

#### Permission Grant Request Model
```csharp
public class GrantPermissionRequest
{
    public string AssociateId { get; set; }
    public List<int> ClientIds { get; set; }
    public string PermissionArea { get; set; } // "TaxFilings", "Payments", "Documents"
    public AssociatePermissionLevel Level { get; set; }
    public DateTime? ExpiryDate { get; set; }
    public decimal? AmountThreshold { get; set; } // For payment permissions
    public bool RequiresApproval { get; set; }
    public string Notes { get; set; }
}

public class BulkPermissionRequest
{
    public List<string> AssociateIds { get; set; }
    public List<int> ClientIds { get; set; }
    public List<PermissionRule> Rules { get; set; }
    public DateTime? ExpiryDate { get; set; }
    public string Notes { get; set; }
}
```

#### Admin Dashboard Features
- **Permission Matrix View**: Visual grid showing associate permissions across all clients
- **Permission Templates**: Predefined permission sets (Junior Associate, Senior Associate, Manager)
- **Bulk Operations**: Grant/revoke permissions for multiple associates and clients simultaneously
- **Permission Analytics**: Reports on permission usage and access patterns
- **Automated Expiry**: Set automatic expiry dates for temporary permissions
- **Approval Workflows**: Require admin approval for certain permission changes
- **Client Risk Levels**: Different permission requirements based on client risk assessment

#### Security Features
- **Admin-only Access**: Only users with Admin or SystemAdmin roles can manage permissions
- **Two-factor Authentication**: Required for sensitive permission changes
- **Permission Change Approval**: Multi-admin approval for high-level permissions
- **Audit Logging**: Comprehensive logging of all permission changes with reasons
- **Emergency Revoke**: Immediate revocation of all permissions for an associate
- **Client Consent**: Optional client approval for associate access (configurable)

### 4.5 Service Layer Implementation

#### Required Services
- `ITaxFilingService` - Business logic for tax filings with associate delegation
- `IPaymentService` - Payment processing and approvals with delegation
- `IDocumentService` - Document management operations with delegation
- `IComplianceService` - Compliance monitoring and scoring
- `INotificationService` - System notifications and alerts
- `IAuditService` - Comprehensive audit logging including delegation actions
- `IFileStorageService` - Secure file storage management
- `IAssociatePermissionService` - Manage associate permissions and delegation
- `IPermissionTemplateService` - Manage permission templates and bulk operations
- `IClientDelegationService` - Handle client-associate delegation relationships
- `IOnBehalfActionService` - Track and log all on-behalf actions
- `IPermissionAuditService` - Specialized audit service for permission changes

#### Associate Permission Service Interface
```csharp
public interface IAssociatePermissionService
{
    Task<bool> HasPermissionAsync(string associateId, int clientId, string area, AssociatePermissionLevel level);
    Task<List<AssociateClientPermission>> GetAssociatePermissionsAsync(string associateId);
    Task<List<Client>> GetDelegatedClientsAsync(string associateId, string area);
    Task GrantPermissionAsync(GrantPermissionRequest request, string adminId);
    Task RevokePermissionAsync(string associateId, int clientId, string area, string adminId);
    Task BulkGrantPermissionsAsync(BulkPermissionRequest request, string adminId);
    Task BulkRevokePermissionsAsync(List<int> permissionIds, string adminId);
    Task SetPermissionExpiryAsync(int permissionId, DateTime? expiryDate, string adminId);
    Task<List<AssociatePermissionAuditLog>> GetPermissionAuditLogAsync(string associateId, DateTime? from, DateTime? to);
}

public interface IOnBehalfActionService
{
    Task LogActionAsync(string associateId, int clientId, string action, string entityType, int entityId, object oldValues, object newValues);
    Task<List<OnBehalfAction>> GetClientActionsAsync(int clientId, DateTime? from, DateTime? to);
    Task<List<OnBehalfAction>> GetAssociateActionsAsync(string associateId, DateTime? from, DateTime? to);
    Task NotifyClientOfActionAsync(int clientId, OnBehalfAction action);
}

public interface IClientDelegationService
{
    Task<List<Client>> GetAvailableClientsForDelegationAsync(string associateId);
    Task<bool> CanAccessClientAsync(string associateId, int clientId);
    Task<List<ApplicationUser>> GetClientAssociatesAsync(int clientId);
    Task RequestClientConsentAsync(int clientId, string associateId, List<string> permissionAreas);
    Task ProcessClientConsentAsync(int clientId, string associateId, bool approved, string reason);
}
```

#### Database Models Required
**File**: `BettsTax/BettsTax.Data/Models/`
```csharp
// TaxFiling.cs
public class TaxFiling
{
    public int Id { get; set; }
    public int ClientId { get; set; }
    public Client Client { get; set; }
    public TaxType TaxType { get; set; }
    public int TaxYear { get; set; }
    public DateTime FilingDate { get; set; }
    public DateTime DueDate { get; set; }
    public FilingStatus Status { get; set; }
    public decimal TaxLiability { get; set; }
    public string FilingReference { get; set; }
    public List<Document> Documents { get; set; }
    
    // Associate delegation fields
    public string? CreatedByAssociateId { get; set; }
    public ApplicationUser? CreatedByAssociate { get; set; }
    public string? LastModifiedByAssociateId { get; set; }
    public ApplicationUser? LastModifiedByAssociate { get; set; }
    public bool IsCreatedOnBehalf { get; set; }
    public DateTime? OnBehalfActionDate { get; set; }
}

// Payment.cs
public class Payment
{
    public int Id { get; set; }
    public int ClientId { get; set; }
    public Client Client { get; set; }
    public decimal Amount { get; set; }
    public DateTime PaymentDate { get; set; }
    public PaymentMethod Method { get; set; }
    public PaymentStatus Status { get; set; }
    public string ApprovalWorkflow { get; set; }
    public int? TaxFilingId { get; set; }
    public TaxFiling TaxFiling { get; set; }
    
    // Associate delegation fields
    public string? ProcessedByAssociateId { get; set; }
    public ApplicationUser? ProcessedByAssociate { get; set; }
    public string? ApprovedByAssociateId { get; set; }
    public ApplicationUser? ApprovedByAssociate { get; set; }
    public bool IsProcessedOnBehalf { get; set; }
    public DateTime? OnBehalfProcessingDate { get; set; }
}

// Document.cs
public class Document
{
    public int Id { get; set; }
    public int ClientId { get; set; }
    public Client Client { get; set; }
    public string FileName { get; set; }
    public string ContentType { get; set; }
    public long FileSize { get; set; }
    public string StoragePath { get; set; }
    public DateTime UploadDate { get; set; }
    public DocumentCategory Category { get; set; }
    public int? TaxFilingId { get; set; }
    public TaxFiling TaxFiling { get; set; }
    
    // Associate delegation fields
    public string? UploadedByAssociateId { get; set; }
    public ApplicationUser? UploadedByAssociate { get; set; }
    public bool IsUploadedOnBehalf { get; set; }
    public DateTime? OnBehalfUploadDate { get; set; }
    public List<DocumentShare> SharedWith { get; set; } = new();
}

// NEW: Associate Permission Models
// AssociateClientPermission.cs
public class AssociateClientPermission
{
    public int Id { get; set; }
    public string AssociateId { get; set; }
    public ApplicationUser Associate { get; set; }
    public int ClientId { get; set; }
    public Client Client { get; set; }
    public string PermissionArea { get; set; } // "TaxFilings", "Payments", "Documents", etc.
    public AssociatePermissionLevel Level { get; set; }
    public DateTime GrantedDate { get; set; }
    public DateTime? ExpiryDate { get; set; }
    public string GrantedByAdminId { get; set; }
    public ApplicationUser GrantedByAdmin { get; set; }
    public bool IsActive { get; set; }
    public string? Notes { get; set; }
}

// AssociatePermissionTemplate.cs
public class AssociatePermissionTemplate
{
    public int Id { get; set; }
    public string Name { get; set; }
    public string Description { get; set; }
    public List<AssociatePermissionRule> Rules { get; set; } = new();
    public DateTime CreatedDate { get; set; }
    public string CreatedByAdminId { get; set; }
    public ApplicationUser CreatedByAdmin { get; set; }
    public bool IsDefault { get; set; }
}

// AssociatePermissionRule.cs
public class AssociatePermissionRule
{
    public int Id { get; set; }
    public int TemplateId { get; set; }
    public AssociatePermissionTemplate Template { get; set; }
    public string PermissionArea { get; set; }
    public AssociatePermissionLevel Level { get; set; }
    public decimal? AmountThreshold { get; set; } // For payment permissions
    public bool RequiresApproval { get; set; }
}

// DocumentShare.cs
public class DocumentShare
{
    public int Id { get; set; }
    public int DocumentId { get; set; }
    public Document Document { get; set; }
    public string SharedWithUserId { get; set; }
    public ApplicationUser SharedWithUser { get; set; }
    public string SharedByUserId { get; set; }
    public ApplicationUser SharedByUser { get; set; }
    public DateTime SharedDate { get; set; }
    public DateTime? ExpiryDate { get; set; }
    public DocumentSharePermission Permission { get; set; }
    public bool IsActive { get; set; }
}

// ENUMS
public enum AssociatePermissionLevel
{
    None = 0,
    Read = 1,
    Create = 2,
    Update = 4,
    Delete = 8,
    Submit = 16,
    Approve = 32,
    All = Read | Create | Update | Delete | Submit | Approve
}

public enum DocumentSharePermission
{
    Read = 1,
    Download = 2,
    Comment = 4,
    Edit = 8,
    All = Read | Download | Comment | Edit
}
```

#### Service Registration with Associate Permissions
**File**: `BettsTax/BettsTax.Web/Program.cs`
```csharp
// Core Services
builder.Services.AddScoped<ITaxFilingService, TaxFilingService>();
builder.Services.AddScoped<IPaymentService, PaymentService>();
builder.Services.AddScoped<IDocumentService, DocumentService>();
builder.Services.AddScoped<IComplianceService, ComplianceService>();
builder.Services.AddScoped<INotificationService, NotificationService>();
builder.Services.AddScoped<IAuditService, AuditService>();
builder.Services.AddScoped<IFileStorageService, FileStorageService>();

// Associate Permission Services
builder.Services.AddScoped<IAssociatePermissionService, AssociatePermissionService>();
builder.Services.AddScoped<IPermissionTemplateService, PermissionTemplateService>();
builder.Services.AddScoped<IClientDelegationService, ClientDelegationService>();
builder.Services.AddScoped<IOnBehalfActionService, OnBehalfActionService>();

// Authorization Handlers
builder.Services.AddScoped<IAuthorizationHandler, AssociatePermissionHandler>();
builder.Services.AddScoped<IAuthorizationHandler, ClientDelegationHandler>();

// Authorization Policies
builder.Services.AddAuthorization(options =>
{
    options.AddPolicy("TaxFilingRead", policy =>
        policy.Requirements.Add(new AssociatePermissionRequirement("TaxFilings", AssociatePermissionLevel.Read)));
    options.AddPolicy("TaxFilingCreate", policy =>
        policy.Requirements.Add(new AssociatePermissionRequirement("TaxFilings", AssociatePermissionLevel.Create)));
    options.AddPolicy("TaxFilingUpdate", policy =>
        policy.Requirements.Add(new AssociatePermissionRequirement("TaxFilings", AssociatePermissionLevel.Update)));
    options.AddPolicy("TaxFilingDelete", policy =>
        policy.Requirements.Add(new AssociatePermissionRequirement("TaxFilings", AssociatePermissionLevel.Delete)));
    options.AddPolicy("TaxFilingSubmit", policy =>
        policy.Requirements.Add(new AssociatePermissionRequirement("TaxFilings", AssociatePermissionLevel.Submit)));
    
    options.AddPolicy("PaymentRead", policy =>
        policy.Requirements.Add(new AssociatePermissionRequirement("Payments", AssociatePermissionLevel.Read)));
    options.AddPolicy("PaymentCreate", policy =>
        policy.Requirements.Add(new AssociatePermissionRequirement("Payments", AssociatePermissionLevel.Create)));
    options.AddPolicy("PaymentApprove", policy =>
        policy.Requirements.Add(new AssociatePermissionRequirement("Payments", AssociatePermissionLevel.Approve)));
    
    options.AddPolicy("DocumentRead", policy =>
        policy.Requirements.Add(new AssociatePermissionRequirement("Documents", AssociatePermissionLevel.Read)));
    options.AddPolicy("DocumentCreate", policy =>
        policy.Requirements.Add(new AssociatePermissionRequirement("Documents", AssociatePermissionLevel.Create)));
    options.AddPolicy("DocumentDelete", policy =>
        policy.Requirements.Add(new AssociatePermissionRequirement("Documents", AssociatePermissionLevel.Delete)));
});

// Security and Rate Limiting with Role-based limits
builder.Services.AddRateLimiter(options =>
{
    // Higher limits for associates managing multiple clients
    options.AddFixedWindowLimiter("associate-api", limiterOptions =>
    {
        limiterOptions.Window = TimeSpan.FromMinutes(1);
        limiterOptions.PermitLimit = 200; // Higher limit for associates
    });
    
    options.AddFixedWindowLimiter("client-api", limiterOptions =>
    {
        limiterOptions.Window = TimeSpan.FromMinutes(1);
        limiterOptions.PermitLimit = 60; // Standard limit for clients
    });
    
    options.AddFixedWindowLimiter("admin-api", limiterOptions =>
    {
        limiterOptions.Window = TimeSpan.FromMinutes(1);
        limiterOptions.PermitLimit = 300; // Highest limit for admins
    });
});
```

## 5. Frontend Service Layer Extensions

### 5.0 Associate Permission Management Components
**Files**: `sierra-leone-ctis/components/admin/`

#### Permission Management Dashboard
- `PermissionMatrix` - Visual grid of associate permissions across clients
- `PermissionTemplateManager` - Create and manage permission templates
- `BulkPermissionEditor` - Bulk grant/revoke permissions interface
- `AssociatePermissionCard` - Individual associate permission summary
- `ClientDelegationPanel` - Manage client-associate relationships
- `PermissionAuditLog` - View permission change history
- `OnBehalfActionLog` - Track all on-behalf actions by associates

#### Associate Dashboard Components
- `DelegatedClientsGrid` - Show all clients the associate can manage
- `PermissionSummaryCard` - Associate's current permission summary
- `OnBehalfActionHistory` - Associate's action history
- `ClientAccessRequestForm` - Request access to additional clients
- `PermissionExpiryNotifications` - Alerts for expiring permissions

#### Client Permission Components
- `AssociateAccessPanel` - View associates with access to client data
- `ConsentManagementForm` - Approve/deny associate access requests
- `AssociateActionReview` - Review actions taken on behalf of client
- `PermissionHistoryTimeline` - Timeline of permission changes

### 5.1 API Client Services
**Files**: `sierra-leone-ctis/lib/services/`

#### Tax Filings Service
- `tax-filings-service.ts` - Complete CRUD operations
- TypeScript interfaces for type safety
- Error handling and loading states
- Pagination and filtering support

#### Payments Service
- `payments-service.ts` - Payment management
- Approval workflow integration
- Status tracking and updates
- Integration with notification system

#### Documents Service
- `documents-service.ts` - File upload/download
- Progress tracking for uploads
- File type validation
- Secure download links

### 5.2 State Management and Error Handling
- React Context for global state
- Custom hooks for data fetching
- Optimistic updates for better UX
- Error boundary implementation
- Loading skeleton components
- Toast notification system
- Retry mechanisms with exponential backoff

#### Required Frontend Components
**Files**: `sierra-leone-ctis/components/`
```typescript
// ui/error-boundary.tsx
export class ErrorBoundary extends React.Component {
  // Catch and display React errors gracefully
}

// ui/loading-skeleton.tsx
export function LoadingSkeleton({ lines, className }: SkeletonProps) {
  // Animated loading placeholders
}

// ui/search-input.tsx
export function SearchInput({ onSearch, placeholder }: SearchProps) {
  // Debounced search with filtering
}

// ui/pagination.tsx
export function Pagination({ currentPage, totalPages, onPageChange }: PaginationProps) {
  // Consistent pagination across all tables
}

// ui/toast.tsx
export function Toast({ message, type, onDismiss }: ToastProps) {
  // Sierra Leone themed notifications
}
```

## 6. Page Implementations

### 6.1 Tax Filings Management with Associate Features
**File**: `sierra-leone-ctis/app/tax-filings/page.tsx`

#### Features
- **Multi-client View**: Associates can view filings across all delegated clients
- **On-behalf Actions**: Clear indicators when actions are taken on behalf of clients
- **Permission-based UI**: Show/hide actions based on associate permissions
- **Bulk Operations**: Create, update, submit filings for multiple clients
- **Client Context Switching**: Easy switching between different client contexts
- **Delegation Alerts**: Notifications about permission changes or expirations

#### Components
- `TaxFilingsTable` - Main data table with delegation context
- `TaxFilingsHeader` - Page header with client selector for associates
- `TaxFilingForm` - Create/edit form with on-behalf indicators
- `TaxFilingDetails` - Detailed view modal with delegation audit trail
- `ClientSelectorDropdown` - Multi-client navigation for associates
- `OnBehalfIndicator` - Visual indicator for actions taken on behalf of clients
- `PermissionGuard` - Component wrapper that shows/hides content based on permissions
- `BulkTaxFilingActions` - Bulk operations across multiple clients

### 6.2 Payments Management with Associate Features
**File**: `sierra-leone-ctis/app/payments/page.tsx`

#### Features
- **Multi-client Payment Processing**: Associates can process payments for multiple clients
- **Delegation-aware Approval Workflow**: Associates can approve within their permission limits
- **Bulk Payment Operations**: Process multiple payments across multiple clients
- **On-behalf Payment Creation**: Create payments on behalf of delegated clients
- **Permission-based Amount Limits**: Enforce payment amount thresholds per associate
- **Client Payment Method Management**: Associates can manage payment methods for clients
- **Automated Penalty Calculations**: With delegation context for associate actions

#### Components
- `PaymentsTable` - Multi-client payments view for associates
- `PaymentApprovalWorkflow` - Delegation-aware approval interface
- `BulkPaymentProcessor` - Process multiple payments across clients
- `OnBehalfPaymentForm` - Create payments on behalf of clients
- `PaymentAmountGuard` - Enforce amount limits based on associate permissions
- `ClientPaymentMethodManager` - Manage payment methods for delegated clients
- `PaymentDelegationAudit` - Audit trail for payment actions

### 6.3 Documents Management with Associate Features
**File**: `sierra-leone-ctis/app/documents/page.tsx`

#### Features
- **Multi-client Document Management**: Associates can manage documents across all delegated clients
- **Bulk Document Upload**: Upload documents for multiple clients simultaneously
- **Document Organization**: Organize and categorize documents on behalf of clients
- **Delegation-aware Sharing**: Share documents between associates and clients with proper permissions
- **Document Templates**: Create and apply document templates across multiple clients
- **Version Control**: Manage document versions on behalf of clients
- **Secure Access Control**: Fine-grained access control based on associate permissions

#### Components
- `MultiClientDocumentGrid` - Document view across all delegated clients
- `BulkDocumentUploader` - Upload documents for multiple clients
- `DocumentOrganizer` - Organize documents on behalf of clients
- `DelegationAwareDocumentShare` - Share documents with proper permission checks
- `DocumentTemplateManager` - Create and apply templates across clients
- `DocumentVersionControl` - Manage versions with delegation context
- `DocumentAccessControl` - Fine-grained permission-based access

### 6.4 Compliance Dashboard
**File**: `sierra-leone-ctis/app/compliance/page.tsx`

#### Features
- Real-time compliance scoring
- Deadline monitoring
- Automated alerts and notifications
- Compliance history tracking
- Sierra Leone Finance Act 2025 integration

### 6.4 Admin Permission Management
**File**: `sierra-leone-ctis/app/admin/permissions/page.tsx`

#### Features
- **Permission Matrix Dashboard**: Visual overview of all associate permissions
- **Bulk Permission Management**: Grant/revoke permissions for multiple associates and clients
- **Permission Templates**: Create and apply standardized permission sets
- **Real-time Permission Analytics**: Monitor permission usage and patterns
- **Automated Permission Workflows**: Set up automatic permission grants based on criteria
- **Permission Audit Dashboard**: Complete audit trail of all permission changes
- **Emergency Controls**: Quick revoke all permissions for an associate

#### Components
- `PermissionMatrixDashboard` - Main permission overview grid
- `BulkPermissionManager` - Bulk operations interface
- `PermissionTemplateEditor` - Create and edit permission templates
- `PermissionAnalyticsDashboard` - Usage analytics and reporting
- `PermissionWorkflowBuilder` - Automated permission rule builder
- `PermissionAuditViewer` - Comprehensive audit log viewer
- `EmergencyPermissionControls` - Emergency revoke and security controls

## 7. Implementation Timeline (Revised 8-Week Plan with Associate Features)

### Phase 1: Foundation and Database with Permission Models (Week 1)
**Days 1-2: Database Schema and Migrations with Permission Models**
- [ ] Create Entity Framework models for TaxFiling, Payment, Document entities with delegation fields
- [ ] Create Associate Permission models (AssociateClientPermission, AssociatePermissionTemplate, etc.)
- [ ] Generate and review database migrations for all new permission tables
- [ ] Update database relationships and foreign keys including permission relationships
- [ ] Create indexes for permission queries and delegation lookups
- [ ] Test migrations on development database
- [ ] **Security Checkpoint**: Review data model security including permission model security

**Days 3-4: Core Infrastructure**
- ✅ Update `app/layout.tsx` with sidebar integration
- ✅ Implement enhanced `components/sidebar.tsx`
- ✅ Update `tailwind.config.ts` with Sierra Leone color palette
- ✅ Create responsive layout system
- ✅ Add error boundary components

**Days 5-7: Authentication and Security**
- [ ] Configure CORS and security middleware
- [ ] Implement rate limiting
- [ ] Add JWT token refresh mechanism
- [ ] Create authorization attribute classes
- [ ] **Security Checkpoint**: Authentication flow review

### Phase 2: Backend API Foundation with Permission Services (Week 2)
**Days 1-3: Service Layer with Permission Services**
- ✅ Create service interfaces including permission services (`IAssociatePermissionService`, `IOnBehalfActionService`, etc.)
- ✅ Implement business logic services with empty state handling and permission checks
- [ ] Add FluentValidation rules including permission validation
- [ ] Configure dependency injection for permission services
- [ ] Implement permission authorization handlers
- ✅ Add comprehensive error handling including permission-related errors

**Days 4-5: Core Controllers with Permission Features**
- [ ] Implement `TaxFilingsController` with full CRUD and delegation endpoints
- [ ] Implement `PaymentsController` with approval workflow and delegation features
- [ ] Implement `DocumentsController` with secure upload and delegation capabilities
- [ ] Implement `AdminPermissionsController` for permission management
- [ ] Add authorization attributes and permission checks to all endpoints
- [ ] Add Swagger documentation including permission requirements
- [ ] **Security Checkpoint**: API endpoint security review including permission model security

**Days 6-7: File Management and Storage**
- [ ] Configure secure file storage (local/cloud)
- [ ] Implement file upload validation and virus scanning
- [ ] Add file download with access control
- [ ] Create audit logging for all operations
- [ ] Test file upload/download functionality

### Phase 3: Permission System Implementation (Week 3)
**Days 1-3: Permission Authorization System**
- [ ] Implement custom authorization attributes for associate permissions
- [ ] Create permission authorization handlers and policies
- [ ] Add permission middleware for request context
- [ ] Implement permission caching for performance
- [ ] Add permission validation and business rules
- [ ] **Security Checkpoint**: Permission system security review

**Days 4-5: Admin Permission Management Backend**
- [ ] Complete `AdminPermissionsController` implementation
- [ ] Implement permission template system
- [ ] Add bulk permission operations
- [ ] Create permission audit logging system
- [ ] Add permission analytics and reporting endpoints

**Days 6-7: Associate Permission Services**
- [ ] Complete `AssociatePermissionService` implementation
- [ ] Implement `ClientDelegationService` with consent management
- [ ] Add `OnBehalfActionService` for comprehensive audit trails
- [ ] Create permission notification system
- [ ] Test all permission workflows end-to-end

### Phase 4: Dashboard and Core Components (Week 4)
**Days 1-2: Dashboard Foundation**
- ✅ Consolidate dashboard pages
- ✅ Create loading skeleton components
- ✅ Implement error handling and retry logic
- ✅ Add dashboard refresh functionality
- ✅ Create pagination components

**Days 3-4: Dashboard Widgets**
- [ ] Create `ClientSummaryCard` component
- [ ] Create `ComplianceOverview` component
- [ ] Create `RecentActivityList` component
- [ ] Create `PendingApprovals` component
- ✅ Add real-time data integration (API-based)

**Days 5-7: Design System Enhancement**
- ✅ Standardize component styling
- ✅ Implement consistent spacing and typography
- ✅ Create reusable UI patterns
- [ ] Add animation and transition library
- ✅ Test responsive design across devices

### Phase 5: Admin Permission Management Frontend (Week 5)
**Days 1-3: Admin Permission Dashboard**
- [ ] Implement `PermissionMatrixDashboard` with real-time permission grid
- [ ] Create `BulkPermissionManager` for bulk operations
- [ ] Build `PermissionTemplateEditor` for template management
- [ ] Add `PermissionAnalyticsDashboard` for usage analytics
- [ ] Implement `EmergencyPermissionControls` for security

**Days 4-5: Permission Management Components**
- [ ] Create `AssociatePermissionCard` for individual associate management
- [ ] Build `ClientDelegationPanel` for client-associate relationships
- [ ] Implement `PermissionAuditViewer` for audit trail visualization
- [ ] Add `PermissionWorkflowBuilder` for automated rules
- [ ] Create permission change notification system

**Days 6-7: Integration and Testing**
- [ ] Integrate permission management with existing admin dashboard
- [ ] Add permission-based navigation and component rendering
- [ ] Implement real-time permission updates using SignalR
- [ ] Test all permission management workflows
- [ ] **Security Checkpoint**: Frontend permission security review

### Phase 6: Frontend Pages Implementation with Associate Features (Week 6)
**Days 1-2: Tax Filings Interface with Associate Features**
- [ ] Implement tax filings page with multi-client support for associates
- [ ] Add client selector dropdown for associates
- [ ] Create on-behalf tax filing forms with delegation indicators
- [ ] Implement bulk operations across multiple clients
- [ ] Add permission-based feature toggles
- [ ] Create delegation audit trail components

**Days 3-4: Payments Management with Associate Features**
- ✅ Implement payments page with multi-client support
- [ ] Create delegation-aware payment approval workflow
- [ ] Add bulk payment processing for associates
- [ ] Implement on-behalf payment creation
- [ ] Add payment amount threshold enforcement
- ✅ Currency standardization to Sierra Leone Leones (Le)
- [ ] Add automated penalty calculations with delegation context

**Days 5-7: Documents and Compliance with Associate Features**
- ✅ Create documents management interface with multi-client support
- [ ] Implement bulk document upload for multiple clients
- [ ] Add document organization features for associates
- [ ] Create delegation-aware document sharing
- ✅ Add compliance dashboard with delegation context
- ✅ Create deadline monitoring system with associate notifications
- ✅ **Currency & UI Updates**: All pages standardized to Sierra Leone theme

### Phase 7: Associate Dashboard and Client Features (Week 7)
**Days 1-3: Associate Dashboard**
- [ ] Create comprehensive associate dashboard with delegated client overview
- [ ] Implement `DelegatedClientsGrid` for client management
- [ ] Add `PermissionSummaryCard` for associate's current permissions
- [ ] Create `OnBehalfActionHistory` for tracking associate actions
- [ ] Implement `PermissionExpiryNotifications` for expiring permissions

**Days 4-5: Client Permission Features**
- [ ] Build `AssociateAccessPanel` for clients to view who has access
- [ ] Create `ConsentManagementForm` for client approval of associate access
- [ ] Implement `AssociateActionReview` for clients to review on-behalf actions
- [ ] Add `PermissionHistoryTimeline` for permission change visualization
- [ ] Create client notification system for associate actions

**Days 6-7: Advanced Associate Features**
- [ ] Implement context switching between client accounts
- [ ] Add bulk operation workflows across multiple clients
- [ ] Create associate-specific reporting and analytics
- [ ] Implement permission request workflows
- [ ] Add automated permission renewal system

### Phase 8: Advanced Features and Integration (Week 8)
**Days 1-2: Frontend Services**
- ✅ Complete API client services with TypeScript interfaces
- [ ] Add optimistic updates and caching
- [ ] Implement real-time notifications
- [ ] Add search functionality across pages
- ✅ Create custom hooks for data fetching

**Days 3-4: Notification System**
- [ ] Implement toast notification system
- [ ] Add email notification service
- [ ] Create notification preferences
- [ ] Add push notification support
- [ ] Implement notification history

**Days 5-7: Analytics and Reporting**
- [ ] Create analytics dashboard
- [ ] Implement compliance reporting
- [ ] Add data export functionality
- [ ] Create performance monitoring
- [ ] Add user activity tracking

### Phase 9: Testing, Polish, and Deployment (Week 9)
**Days 1-2: Comprehensive Testing with Permission Features**
- [ ] Unit tests for all services and controllers including permission services
- [ ] Integration tests for API endpoints with permission scenarios
- [ ] Component tests for React components including permission-based components
- [ ] End-to-end testing for critical workflows including delegation workflows
- [ ] Permission system security testing and penetration testing
- [ ] Performance testing and optimization including permission query optimization

**Days 3-4: User Experience Polish**
- [ ] Add keyboard navigation support
- [ ] Implement accessibility features (WCAG compliance)
- [ ] Optimize loading states and animations
- [ ] Add progressive web app features
- [ ] Cross-browser compatibility testing

**Days 5-7: Deployment Preparation with Permission System**
- [ ] Production environment setup including permission system configuration
- [ ] Database migration strategy for production including permission tables
- [ ] Security penetration testing focusing on permission bypass attempts
- [ ] Performance benchmarking including permission query performance
- [ ] Documentation updates and user guides including permission management guides
- [ ] Associate training materials and permission workflows documentation
- [ ] **Final Security Checkpoint**: Complete security audit including permission system audit

## 8. Quality Assurance

### 8.1 Testing Strategy
- **Unit Tests**: Service layer and business logic
- **Integration Tests**: API endpoints and database operations
- **Component Tests**: React component functionality
- **E2E Tests**: Complete user workflows

### 8.2 Performance Requirements
- Page load times under 2 seconds
- API response times under 500ms
- Smooth animations at 60fps
- Mobile responsiveness across devices

### 8.3 Security Considerations
- JWT token validation on all endpoints
- Role-based authorization checks
- Input validation and sanitization
- Secure file upload handling
- HTTPS enforcement
- XSS protection with Content Security Policy
- SQL injection prevention with parameterized queries
- File upload virus scanning
- Rate limiting on all API endpoints
- CORS configuration for production domains
- Audit logging for all sensitive operations

#### Security Implementation Checklist
**Backend Security**:
- [ ] Configure HTTPS redirects
- [ ] Implement JWT token refresh mechanism
- [ ] Add rate limiting middleware
- [ ] Configure CORS for production domains
- [ ] Add input validation attributes
- [ ] Implement file upload scanning
- [ ] Add comprehensive audit logging
- [ ] Configure Content Security Policy headers

**Frontend Security**:
- [ ] Sanitize all user inputs
- [ ] Implement XSS protection
- [ ] Secure localStorage token handling
- [ ] Add CSRF protection
- [ ] Validate file uploads on client side
- [ ] Implement proper error handling (no sensitive data leaks)

## 9. Deployment Considerations

### 9.1 Environment Configuration
- Development, staging, and production environments
- Environment-specific configuration files
- Database migration strategies
- CI/CD pipeline integration

### 9.2 Monitoring and Maintenance
- Application performance monitoring (APM)
- Error tracking and alerting
- Database performance monitoring
- User activity analytics
- Health check endpoints
- Logging aggregation and analysis
- Backup and disaster recovery procedures

#### Health Check Implementation
**File**: `BettsTax/BettsTax.Web/Controllers/HealthController.cs`
```csharp
[ApiController]
[Route("api/[controller]")]
public class HealthController : ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> Get()
    {
        var health = new
        {
            Status = "Healthy",
            Timestamp = DateTime.UtcNow,
            Database = await CheckDatabaseConnection(),
            FileStorage = CheckFileStorage(),
            Version = Assembly.GetExecutingAssembly().GetName().Version?.ToString()
        };
        return Ok(health);
    }
}
```

#### Monitoring Configuration
**File**: `BettsTax/BettsTax.Web/Program.cs`
```csharp
// Add health checks
builder.Services.AddHealthChecks()
    .AddDbContext<ApplicationDbContext>()
    .AddCheck<FileStorageHealthCheck>("file-storage");

// Add logging
builder.Host.UseSerilog((context, configuration) =>
    configuration.ReadFrom.Configuration(context.Configuration));

// Configure monitoring endpoints
app.MapHealthChecks("/health");
app.MapHealthChecks("/health/ready", new HealthCheckOptions
{
    Predicate = check => check.Tags.Contains("ready")
});
```

## 10. Success Criteria

### 10.1 Functional Requirements
- [ ] All navigation items have working pages
- [ ] Dashboard displays real-time data
- [ ] CRUD operations work for all entities
- [ ] Approval workflows function correctly
- [ ] File upload/download works securely

### 10.2 Non-Functional Requirements
- [ ] Responsive design works on all devices
- [ ] Application loads within performance targets
- [ ] Security requirements are met
- [ ] Accessibility standards are followed
- [ ] Code quality standards are maintained

## 11. Risk Mitigation

### 11.1 Technical Risks
- **API Integration Issues**: Implement comprehensive error handling
- **Performance Problems**: Regular performance testing and optimization
- **Security Vulnerabilities**: Security audits and penetration testing
- **Browser Compatibility**: Cross-browser testing strategy

### 11.2 Project Risks
- **Scope Creep**: Regular stakeholder reviews and change control
- **Timeline Delays**: Buffer time in schedule and parallel development
- **Resource Constraints**: Cross-training and knowledge sharing
- **Quality Issues**: Continuous integration and automated testing

## Conclusion

This implementation plan provides a comprehensive roadmap for building a production-ready Sierra Leone CTIS dashboard that meets all design requirements while maintaining consistency with the existing codebase architecture. The phased approach ensures systematic development with regular checkpoints for quality assurance and stakeholder feedback.

The plan emphasizes:
- **User Experience**: Intuitive navigation and responsive design
- **Performance**: Fast loading times and smooth interactions
- **Security**: Proper authorization and data protection
- **Maintainability**: Clean code architecture and comprehensive testing
- **Scalability**: Modular design for future enhancements

Regular progress reviews and stakeholder feedback sessions should be scheduled throughout the implementation to ensure alignment with business requirements and user expectations.