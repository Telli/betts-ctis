# Design Document: CTIS Production-Ready Implementation

## Overview

This design document outlines the technical architecture and implementation approach for completing the Client Tax Information System (CTIS) to production-ready status. The system builds upon the existing ASP.NET Core 9.0 backend and Next.js 15.2.4 frontend to deliver a comprehensive tax management platform for The Betts Firm in Sierra Leone.

## Architecture

### System Architecture Overview

The CTIS follows a clean architecture pattern with clear separation of concerns:

```
┌─────────────────────────────────────────────────────────────┐
│                    Frontend Layer                           │
│  Next.js 15.2.4 + React 19 + TypeScript + shadcn/ui      │
└─────────────────────────────────────────────────────────────┘
                              │
                              │ HTTPS/REST API
                              │
┌─────────────────────────────────────────────────────────────┐
│                    API Gateway Layer                       │
│         ASP.NET Core 9.0 Web API + JWT Auth               │
└─────────────────────────────────────────────────────────────┘
                              │
┌─────────────────────────────────────────────────────────────┐
│                   Business Logic Layer                     │
│    Services + DTOs + Validation + AutoMapper              │
└─────────────────────────────────────────────────────────────┘
                              │
┌─────────────────────────────────────────────────────────────┐
│                    Data Access Layer                       │
│         Entity Framework Core + PostgreSQL                │
└─────────────────────────────────────────────────────────────┘
                              │
┌─────────────────────────────────────────────────────────────┐
│                   External Integrations                    │
│  Payment Gateways + SMS/Email + File Storage + Audit      │
└─────────────────────────────────────────────────────────────┘
```

### Technology Stack Enhancement

**Backend Enhancements:**
- **Real-time Communication**: SignalR for live notifications and chat
- **Background Processing**: Hangfire for scheduled tasks and report generation
- **Caching**: Redis for performance optimization
- **Monitoring**: Application Insights for production monitoring
- **Security**: Enhanced JWT with refresh tokens and rate limiting

**Frontend Enhancements:**
- **State Management**: Zustand for global state management
- **Real-time Updates**: Socket.io client for live notifications
- **Offline Support**: Service workers for offline functionality
- **Performance**: React Query for efficient data fetching and caching
- **Testing**: Jest + React Testing Library for comprehensive testing

## Components and Interfaces

### 1. Enhanced KPI Dashboard System

#### Backend Components

**KPI Service Architecture:**
```csharp
public interface IKPIService
{
    Task<InternalKPIDto> GetInternalKPIsAsync(DateTime? fromDate = null, DateTime? toDate = null);
    Task<ClientKPIDto> GetClientKPIsAsync(int clientId, DateTime? fromDate = null, DateTime? toDate = null);
    Task<List<KPIAlertDto>> GetKPIAlertsAsync();
    Task UpdateKPIThresholdsAsync(KPIThresholdDto thresholds);
}

public class KPIService : IKPIService
{
    private readonly IClientService _clientService;
    private readonly ITaxFilingService _taxFilingService;
    private readonly IPaymentService _paymentService;
    private readonly IDocumentService _documentService;
    private readonly INotificationService _notificationService;
    private readonly ICacheService _cacheService;
}
```

**KPI Data Models:**
```csharp
public class InternalKPIDto
{
    public decimal ClientComplianceRate { get; set; }
    public double AverageFilingTimeliness { get; set; }
    public decimal PaymentCompletionRate { get; set; }
    public decimal DocumentSubmissionCompliance { get; set; }
    public decimal ClientEngagementRate { get; set; }
    public List<TrendDataPoint> ComplianceTrend { get; set; }
    public List<TaxTypeMetrics> TaxTypeBreakdown { get; set; }
}

public class ClientKPIDto
{
    public double MyFilingTimeliness { get; set; }
    public decimal OnTimePaymentPercentage { get; set; }
    public decimal DocumentReadinessScore { get; set; }
    public decimal ComplianceScore { get; set; }
    public ComplianceLevel ComplianceLevel { get; set; } // Green, Yellow, Red
    public List<DeadlineMetric> UpcomingDeadlines { get; set; }
}
```

#### Frontend Components

**KPI Dashboard Components:**
```typescript
// components/kpi/InternalKPIDashboard.tsx
export function InternalKPIDashboard() {
  const { data: kpis, isLoading } = useKPIs();

  return (
    <div className="grid grid-cols-1 md:grid-cols-2 lg:grid-cols-4 gap-6">
      <KPICard
        title="Client Compliance Rate"
        value={kpis?.clientComplianceRate}
        format="percentage"
        trend={kpis?.complianceTrend}
      />
      <KPICard
        title="Filing Timeliness"
        value={kpis?.averageFilingTimeliness}
        format="days"
        threshold={7}
      />
      {/* Additional KPI cards */}
    </div>
  );
}

// components/kpi/ClientKPIDashboard.tsx
export function ClientKPIDashboard({ clientId }: { clientId: number }) {
  const { data: clientKPIs } = useClientKPIs(clientId);

  return (
    <div className="space-y-6">
      <ComplianceScoreCard score={clientKPIs?.complianceScore} />
      <FilingTimelinessChart data={clientKPIs?.filingHistory} />
      <PaymentTimelinessChart data={clientKPIs?.paymentHistory} />
      <DocumentReadinessProgress score={clientKPIs?.documentReadinessScore} />
    </div>
  );
}
```

### 2. Comprehensive Reporting System

#### Backend Components

**Report Generation Service:**
```csharp
public interface IReportService
{
    Task<byte[]> GenerateTaxFilingReportAsync(int clientId, int taxYear, ReportFormat format);
    Task<byte[]> GeneratePaymentHistoryReportAsync(int clientId, DateTime fromDate, DateTime toDate, ReportFormat format);
    Task<byte[]> GenerateComplianceReportAsync(int clientId, DateTime fromDate, DateTime toDate, ReportFormat format);
    Task<byte[]> GenerateClientActivityReportAsync(DateTime fromDate, DateTime toDate, ReportFormat format);
    Task<string> QueueReportGenerationAsync(ReportRequest request);
    Task<ReportStatus> GetReportStatusAsync(string reportId);
}

// New report types per updated tax information requirements
public interface IReportService // extension excerpt
{
    Task<byte[]> GenerateDocumentSubmissionReportAsync(int clientId, DateTime fromDate, DateTime toDate, ReportFormat format);
    Task<byte[]> GenerateTaxCalendarSummaryReportAsync(int clientId, DateTime fromDate, DateTime toDate, ReportFormat format);
    Task<byte[]> GenerateRevenueCollectedReportAsync(DateTime fromDate, DateTime toDate, ReportFormat format, int? clientId = null);
    Task<byte[]> GenerateCaseManagementReportAsync(DateTime fromDate, DateTime toDate, ReportFormat format, int? clientId = null);
}


public class ReportService : IReportService
{
    private readonly IBackgroundJobClient _backgroundJobClient;
    private readonly IPdfGenerator _pdfGenerator;
    private readonly IExcelGenerator _excelGenerator;
    private readonly IFileStorageService _fileStorageService;
    private readonly INotificationService _notificationService;
}
```

**Report Templates:**
```csharp
public class TaxFilingReportTemplate
{
    public async Task<byte[]> GeneratePdfAsync(TaxFilingReportData data)
    {
        // PDF generation using iTextSharp or similar
        // Include Sierra Leone branding and formatting

// New report templates per updated requirements
public class DocumentSubmissionReportTemplate
{
    public async Task<byte[]> GeneratePdfAsync(DocumentSubmissionReportData data)
    {
        // Summaries: completed %, pending %, rejected %, by tax type and period
    }
    public async Task<byte[]> GenerateExcelAsync(DocumentSubmissionReportData data)
    {
        // Excel with pivot by TaxType x Status and monthly breakdown
    }
}

public class TaxCalendarSummaryReportTemplate
{
    public async Task<byte[]> GeneratePdfAsync(TaxCalendarSummaryData data)
    {
        // Upcoming and past obligations; include due dates, statuses, and penalty flags
    }
    public async Task<byte[]> GenerateExcelAsync(TaxCalendarSummaryData data)
    {
        // Tabular calendar export with filters
    }
}

        // Support for multiple languages if needed
    }

    public async Task<byte[]> GenerateExcelAsync(TaxFilingReportData data)
    {
        // Excel generation using EPPlus or similar
        // Include charts and pivot tables
        // Sierra Leone currency formatting
    }
}
```

#### Frontend Components

**Report Generation Interface:**
```typescript
// components/reports/ReportGenerator.tsx
export function ReportGenerator() {
  const [reportType, setReportType] = useState<ReportType>('tax-filing');
  const [dateRange, setDateRange] = useState<DateRange>();
  const [format, setFormat] = useState<'pdf' | 'excel'>('pdf');

  const generateReport = useMutation({
    mutationFn: (request: ReportRequest) => reportService.generateReport(request),
    onSuccess: (reportId) => {
      // Poll for report completion
      pollReportStatus(reportId);
    }
  });

  return (
    <Card>
      <CardHeader>
        <CardTitle>Generate Report</CardTitle>
      </CardHeader>
      <CardContent>
        <ReportTypeSelector value={reportType} onChange={setReportType} />
        <DateRangePicker value={dateRange} onChange={setDateRange} />
        <FormatSelector value={format} onChange={setFormat} />
        <Button onClick={() => generateReport.mutate({ reportType, dateRange, format })}>
          Generate Report
        </Button>
      </CardContent>
    </Card>
  );
}
```

### 3. Advanced Compliance Monitoring

#### Backend Components

**Compliance Engine:**
```csharp
public interface IComplianceEngine
{
    Task<ComplianceStatusDto> CalculateComplianceStatusAsync(int clientId);
    Task<List<ComplianceAlertDto>> GetComplianceAlertsAsync(int clientId);
    Task<PenaltyCalculationDto> CalculatePenaltiesAsync(int clientId, int taxYear);
    Task<List<UpcomingDeadlineDto>> GetUpcomingDeadlinesAsync(int clientId);
    Task UpdateComplianceScoresAsync(); // Background job
}

public class ComplianceEngine : IComplianceEngine
{
    private readonly ISierraLeoneTaxCalculationService _taxCalculationService;
    private readonly IPenaltyCalculationService _penaltyCalculationService;
    private readonly ITaxFilingService _taxFilingService;
    private readonly IPaymentService _paymentService;
    private readonly IDocumentService _documentService;
}
```

**Compliance Scoring Algorithm:**
```csharp
public class ComplianceScoreCalculator
{
    public async Task<decimal> CalculateScoreAsync(int clientId, int taxYear)
    {
        var filingScore = await CalculateFilingScoreAsync(clientId, taxYear);

// Compliance Dashboard additions: metrics tiles per updated requirements
// components/compliance/MetricsTiles.tsx
export function MetricsTiles({ data }: { data: ComplianceMetrics }) {
  return (
    <div className="grid grid-cols-1 sm:grid-cols-2 lg:grid-cols-5 gap-4">
      <KPICard title="Compliance Score" value={data.complianceScore} format="percentage" />
      <KPICard title="Filing Timeliness" value={data.filingTimeliness} format="percentage" />
      <KPICard title="Payment Timeliness" value={data.paymentTimeliness} format="percentage" />
      <KPICard title="Documents Status" value={data.documentsCompletedPct} format="percentage" />
      <KPICard title="Deadline Adherence" value={data.deadlineAdherencePct} format="percentage" />
    </div>
  );
}

        var paymentScore = await CalculatePaymentScoreAsync(clientId, taxYear);
        var documentScore = await CalculateDocumentScoreAsync(clientId, taxYear);
        var timelinessScore = await CalculateTimelinessScoreAsync(clientId, taxYear);

        // Weighted scoring: Filing 30%, Payment 30%, Documents 20%, Timeliness 20%
        return (filingScore * 0.3m) + (paymentScore * 0.3m) +
               (documentScore * 0.2m) + (timelinessScore * 0.2m);
    }
}
```

#### Frontend Components

**Compliance Dashboard:**
```typescript
// components/compliance/ComplianceDashboard.tsx
export function ComplianceDashboard({ clientId }: { clientId: number }) {
  const { data: compliance } = useComplianceStatus(clientId);

  return (
    <div className="space-y-6">
      <ComplianceScoreCard
        score={compliance?.overallScore}
        level={compliance?.level}
        trend={compliance?.trend}
      />

      <div className="grid grid-cols-1 md:grid-cols-2 gap-6">
        <FilingStatusGrid statuses={compliance?.filingStatuses} />
        <UpcomingDeadlines deadlines={compliance?.upcomingDeadlines} />
      </div>

      <PenaltyWarnings penalties={compliance?.potentialPenalties} />
      <DocumentTracker documents={compliance?.documentStatus} />
    </div>
  );
}

// components/compliance/ComplianceScoreCard.tsx
export function ComplianceScoreCard({ score, level, trend }: ComplianceScoreProps) {
  const getScoreColor = (score: number) => {
    if (score >= 85) return 'text-sierra-green-600';
    if (score >= 70) return 'text-sierra-gold-500';
    return 'text-red-600';
  };

  return (
    <Card>
      <CardContent className="p-6">
        <div className="flex items-center justify-between">
          <div>
            <h3 className="text-lg font-semibold">Compliance Score</h3>
            <p className={`text-3xl font-bold ${getScoreColor(score)}`}>
              {score.toFixed(1)}%
            </p>
          </div>
          <ComplianceLevelBadge level={level} />
        </div>
        <ComplianceTrendChart data={trend} />
      </CardContent>
    </Card>
  );
}
```

### 4. Integrated Communication System

#### Backend Components

**Real-time Chat Service:**
```csharp
public interface IChatService
{
    Task<MessageDto> SendMessageAsync(SendMessageDto message);
    Task<List<MessageDto>> GetConversationAsync(int clientId, string userId);
    Task<List<ConversationDto>> GetUserConversationsAsync(string userId);
    Task AssignConversationAsync(int conversationId, string assigneeId);
    Task AddInternalNoteAsync(int messageId, string note, string userId);
    Task SetMessagePriorityAsync(int messageId, MessagePriority priority);
}

// SignalR Hub for real-time communication
[Authorize]
public class ChatHub : Hub
{
    private readonly IChatService _chatService;
    private readonly IUserContextService _userContextService;

    public async Task JoinClientGroup(string clientId)
    {
        await Groups.AddToGroupAsync(Context.ConnectionId, $"Client_{clientId}");
    }

    public async Task SendMessage(SendMessageDto message)
    {
        var sentMessage = await _chatService.SendMessageAsync(message);
        await Clients.Group($"Client_{message.ClientId}")
            .SendAsync("ReceiveMessage", sentMessage);
    }

    public async Task TypingIndicator(string clientId, bool isTyping)
    {
        await Clients.Group($"Client_{clientId}")
            .SendAsync("UserTyping", Context.User.Identity.Name, isTyping);
    }
}
```

#### Frontend Components

**Chat Interface:**
```typescript
// components/chat/ChatInterface.tsx
export function ChatInterface({ clientId }: { clientId: number }) {
  const [messages, setMessages] = useState<Message[]>([]);
  const [newMessage, setNewMessage] = useState('');
  const [isTyping, setIsTyping] = useState(false);

  const { connection } = useSignalR();

  useEffect(() => {
    if (connection) {
      connection.invoke('JoinClientGroup', clientId.toString());

      connection.on('ReceiveMessage', (message: Message) => {
        setMessages(prev => [...prev, message]);
      });

      connection.on('UserTyping', (userName: string, typing: boolean) => {
        setIsTyping(typing);
      });
    }
  }, [connection, clientId]);

  const sendMessage = async () => {
    if (newMessage.trim()) {
      await connection?.invoke('SendMessage', {
        clientId,
        content: newMessage,
        messageType: 'General'
      });
      setNewMessage('');
    }
  };

  return (
    <Card className="h-96 flex flex-col">
      <CardHeader>
        <CardTitle>Support Chat</CardTitle>
      </CardHeader>
      <CardContent className="flex-1 overflow-y-auto">
        <MessageList messages={messages} />
        {isTyping && <TypingIndicator />}
      </CardContent>
      <CardFooter>
        <MessageInput
          value={newMessage}
          onChange={setNewMessage}
          onSend={sendMessage}
          onTyping={(typing) => connection?.invoke('TypingIndicator', clientId.toString(), typing)}
        />
      </CardFooter>
    </Card>
  );
}
```

### 5. Multi-Gateway Payment Integration

#### Backend Components

**Payment Gateway Abstraction:**
```csharp
public interface IPaymentGateway
{
    string GatewayName { get; }
    Task<PaymentResult> ProcessPaymentAsync(PaymentRequest request);
    Task<PaymentStatus> GetPaymentStatusAsync(string transactionId);
    Task<RefundResult> RefundPaymentAsync(string transactionId, decimal amount);
}

// Sierra Leone specific payment providers
public class OrangeMoneyProvider : IPaymentGateway
{
    public string GatewayName => "Orange Money";

    public async Task<PaymentResult> ProcessPaymentAsync(PaymentRequest request)
    {
        // Integration with Orange Money API
        // Handle SLE currency conversion
        // Implement proper error handling and retry logic
    }
}

public class AfricellMoneyProvider : IPaymentGateway
{
    public string GatewayName => "Africell Money";

    public async Task<PaymentResult> ProcessPaymentAsync(PaymentRequest request)
    {
        // Integration with Africell Money API
        // Handle mobile money specific workflows
    }
}

public class PaymentGatewayFactory
{
    public IPaymentGateway CreateGateway(PaymentMethod method)
    {
        return method switch
        {
            PaymentMethod.OrangeMoney => new OrangeMoneyProvider(),
            PaymentMethod.AfricellMoney => new AfricellMoneyProvider(),
            PaymentMethod.BankTransfer => new BankTransferProvider(),
            _ => throw new NotSupportedException($"Payment method {method} not supported")
        };
    }
}
```

#### Frontend Components

**Payment Interface:**
```typescript
// components/payments/PaymentForm.tsx
export function PaymentForm({ taxFilingId, amount }: PaymentFormProps) {
  const [paymentMethod, setPaymentMethod] = useState<PaymentMethod>('bank-transfer');
  const [isProcessing, setIsProcessing] = useState(false);

  const processPayment = useMutation({
    mutationFn: (payment: PaymentRequest) => paymentService.processPayment(payment),
    onSuccess: (result) => {
      if (result.requiresRedirect) {
        window.location.href = result.redirectUrl;
      } else {
        showSuccessNotification('Payment processed successfully');
      }
    },
    onError: (error) => {
      showErrorNotification(error.message);
    }
  });

  return (
    <Card>
      <CardHeader>
        <CardTitle>Make Payment</CardTitle>
        <CardDescription>
          Amount: {formatCurrency(amount, 'SLE')}
        </CardDescription>
      </CardHeader>
      <CardContent>
        <PaymentMethodSelector
          value={paymentMethod}
          onChange={setPaymentMethod}
          availableMethods={['bank-transfer', 'orange-money', 'africell-money', 'cash']}
        />

        {paymentMethod === 'orange-money' && (
          <OrangeMoneyForm onSubmit={(data) => processPayment.mutate(data)} />
        )}

        {paymentMethod === 'africell-money' && (
          <AfricellMoneyForm onSubmit={(data) => processPayment.mutate(data)} />
        )}

        {paymentMethod === 'bank-transfer' && (
          <BankTransferForm onSubmit={(data) => processPayment.mutate(data)} />
        )}
      </CardContent>
    </Card>
  );
}
```

## Data Models

### Enhanced Data Models for Production

**KPI and Analytics Models:**
```csharp
public class KPIMetric
{
    public int Id { get; set; }
    public string MetricName { get; set; }
    public decimal Value { get; set; }
    public DateTime CalculatedAt { get; set; }
    public string Period { get; set; } // Daily, Weekly, Monthly
    public int? ClientId { get; set; } // Null for system-wide metrics
    public Client? Client { get; set; }
}

public class ComplianceScore
{
    public int Id { get; set; }
    public int ClientId { get; set; }
    public int TaxYear { get; set; }
    public decimal OverallScore { get; set; }
    public decimal FilingScore { get; set; }
    public decimal PaymentScore { get; set; }
    public decimal DocumentScore { get; set; }
    public decimal TimelinessScore { get; set; }
    public ComplianceLevel Level { get; set; }
    public DateTime CalculatedAt { get; set; }
    public Client Client { get; set; }
}
```

**Enhanced Communication Models:**
```csharp
public class Conversation
{
    public int Id { get; set; }
    public int ClientId { get; set; }
    public string Subject { get; set; }
    public ConversationStatus Status { get; set; }
    public MessagePriority Priority { get; set; }
    public string? AssignedToUserId { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? ClosedAt { get; set; }

    public Client Client { get; set; }
    public ApplicationUser? AssignedTo { get; set; }
    public List<Message> Messages { get; set; } = new();
    public List<InternalNote> InternalNotes { get; set; } = new();
}

public class InternalNote
{
    public int Id { get; set; }
    public int ConversationId { get; set; }
    public string Content { get; set; }
    public string CreatedByUserId { get; set; }
    public DateTime CreatedAt { get; set; }

    public Conversation Conversation { get; set; }
    public ApplicationUser CreatedBy { get; set; }
}
```

**Report Generation Models:**
```csharp
public class ReportRequest
{
    public int Id { get; set; }
    public string RequestId { get; set; } = Guid.NewGuid().ToString();
    public ReportType Type { get; set; }
    public ReportFormat Format { get; set; }
    public string Parameters { get; set; } // JSON serialized parameters
    public string RequestedByUserId { get; set; }
    public ReportStatus Status { get; set; }
    public DateTime RequestedAt { get; set; }
    public DateTime? CompletedAt { get; set; }
    public string? FilePath { get; set; }
    public string? ErrorMessage { get; set; }

    public ApplicationUser RequestedBy { get; set; }
}
```

## Error Handling

### Comprehensive Error Handling Strategy

**Global Exception Handling:**
```csharp
public class GlobalExceptionMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<GlobalExceptionMiddleware> _logger;

    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await _next(context);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "An unhandled exception occurred");
            await HandleExceptionAsync(context, ex);
        }
    }

    private static async Task HandleExceptionAsync(HttpContext context, Exception exception)
    {
        var response = exception switch
        {
            ValidationException => new ErrorResponse("VALIDATION_ERROR", exception.Message, 400),
            UnauthorizedAccessException => new ErrorResponse("UNAUTHORIZED", "Access denied", 401),
            NotFoundException => new ErrorResponse("NOT_FOUND", exception.Message, 404),
            PaymentProcessingException => new ErrorResponse("PAYMENT_ERROR", exception.Message, 422),
            _ => new ErrorResponse("INTERNAL_ERROR", "An internal error occurred", 500)
        };

        context.Response.StatusCode = response.StatusCode;
        context.Response.ContentType = "application/json";

        await context.Response.WriteAsync(JsonSerializer.Serialize(response));
    }
}
```

**Frontend Error Boundaries:**
```typescript
// components/ErrorBoundary.tsx
export class ErrorBoundary extends React.Component<ErrorBoundaryProps, ErrorBoundaryState> {
  constructor(props: ErrorBoundaryProps) {
    super(props);
    this.state = { hasError: false, error: null };
  }

  static getDerivedStateFromError(error: Error): ErrorBoundaryState {
    return { hasError: true, error };
  }

  componentDidCatch(error: Error, errorInfo: React.ErrorInfo) {
    // Log error to monitoring service
    errorReportingService.reportError(error, errorInfo);
  }

  render() {
    if (this.state.hasError) {
      return (
        <div className="min-h-screen flex items-center justify-center">
          <Card className="w-96">
            <CardHeader>
              <CardTitle className="text-red-600">Something went wrong</CardTitle>
            </CardHeader>
            <CardContent>
              <p className="text-gray-600 mb-4">
                We're sorry, but something unexpected happened. Please try refreshing the page.
              </p>
              <Button onClick={() => window.location.reload()}>
                Refresh Page
              </Button>
            </CardContent>
          </Card>
        </div>
      );
    }

    return this.props.children;
  }
}
```

## Testing Strategy

### Backend Testing

**Unit Testing Strategy:**
```csharp
[TestClass]
public class ComplianceEngineTests
{
    private readonly Mock<ITaxFilingService> _mockTaxFilingService;
    private readonly Mock<IPaymentService> _mockPaymentService;
    private readonly ComplianceEngine _complianceEngine;

    [TestMethod]
    public async Task CalculateComplianceScore_AllFilingsOnTime_ReturnsHighScore()
    {
        // Arrange
        var clientId = 1;
        var taxYear = 2024;
        _mockTaxFilingService.Setup(x => x.GetClientFilingsAsync(clientId, taxYear))
            .ReturnsAsync(GetMockOnTimeFilings());

        // Act
        var result = await _complianceEngine.CalculateComplianceStatusAsync(clientId);

        // Assert
        Assert.IsTrue(result.OverallScore >= 85);
        Assert.AreEqual(ComplianceLevel.Green, result.Level);
    }
}
```

**Integration Testing:**
```csharp
[TestClass]
public class PaymentControllerIntegrationTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly WebApplicationFactory<Program> _factory;
    private readonly HttpClient _client;

    [TestMethod]
    public async Task ProcessPayment_ValidRequest_ReturnsSuccess()
    {
        // Arrange
        var paymentRequest = new PaymentRequest
        {
            ClientId = 1,
            Amount = 1000,
            Method = PaymentMethod.BankTransfer,
            TaxFilingId = 1
        };

        // Act
        var response = await _client.PostAsJsonAsync("/api/payments", paymentRequest);

        // Assert
        response.EnsureSuccessStatusCode();
        var result = await response.Content.ReadFromJsonAsync<PaymentResult>();
        Assert.IsNotNull(result);
        Assert.AreEqual(PaymentStatus.Pending, result.Status);
    }
}
```

### Frontend Testing

**Component Testing:**
```typescript
// __tests__/components/ComplianceDashboard.test.tsx
import { render, screen, waitFor } from '@testing-library/react';
import { ComplianceDashboard } from '@/components/compliance/ComplianceDashboard';
import { QueryClient, QueryClientProvider } from '@tanstack/react-query';

const mockComplianceData = {
  overallScore: 85.5,
  level: 'Green' as ComplianceLevel,
  filingStatuses: [
    { taxType: 'GST', status: 'Filed', dueDate: '2024-01-31' },
    { taxType: 'Income Tax', status: 'Pending', dueDate: '2024-04-30' }
  ]
};

jest.mock('@/lib/services/compliance-service', () => ({
  useComplianceStatus: () => ({
    data: mockComplianceData,
    isLoading: false,
    error: null
  })
}));

describe('ComplianceDashboard', () => {
  it('displays compliance score correctly', async () => {
    const queryClient = new QueryClient();

    render(
      <QueryClientProvider client={queryClient}>
        <ComplianceDashboard clientId={1} />
      </QueryClientProvider>
    );

    await waitFor(() => {
      expect(screen.getByText('85.5%')).toBeInTheDocument();
      expect(screen.getByText('Green')).toBeInTheDocument();
    });
  });
});
```

This design provides a comprehensive foundation for implementing the remaining CTIS features while ensuring production readiness, scalability, and maintainability.