export interface InternalKPIDto {
  clientComplianceRate: number;
  averageFilingTimeliness: number;
  paymentCompletionRate: number;
  documentSubmissionCompliance: number;
  clientEngagementRate: number;
  complianceTrend: TrendDataPoint[];
  taxTypeBreakdown: TaxTypeMetrics[];
  calculatedAt: string;
  period: string;
}

export interface KpiDashboardSummaryDto {
  internal: KpiDashboardInternalSummaryDto;
  client: KpiDashboardClientSummaryDto;
}

export interface KpiDashboardInternalSummaryDto {
  totalRevenue: number;
  revenueCurrency: string;
  revenueChangePercentage?: number | null;
  activeClients: number;
  totalClients: number;
  complianceRate: number;
  paymentCompletionRate: number;
  documentSubmissionRate: number;
  averageFilingTimelinessDays: number;
  averageProcessingTimeDays: number;
  clientEngagementRate: number;
  referencePeriodLabel: string;
}

export interface KpiDashboardClientSummaryDto {
  totalClients: number;
  activeClients: number;
  averageComplianceScore: number;
  averageFilingTimeDays: number;
  topPerformerName?: string;
  topPerformerComplianceScore: number;
  segments: KpiClientSegmentPerformanceDto[];
}

export interface KpiClientSegmentPerformanceDto {
  segment: string;
  complianceRate: number;
  clientCount: number;
}

export interface ClientKPIDto {
  myFilingTimeliness: number;
  onTimePaymentPercentage: number;
  documentReadinessScore: number;
  complianceScore: number;
  complianceLevel: ComplianceLevel;
  upcomingDeadlines: DeadlineMetric[];
  filingHistory: TrendDataPoint[];
  paymentHistory: TrendDataPoint[];
  calculatedAt: string;
}

export interface TrendDataPoint {
  date: string;
  value: number;
  label: string;
}

export interface TaxTypeMetrics {
  taxType: TaxType;
  totalFilings: number;
  onTimeFilings: number;
  complianceRate: number;
  totalAmount: number;
  clientCount: number;
}

export interface DeadlineMetric {
  id: number;
  taxType: TaxType;
  dueDate: string;
  daysRemaining: number;
  priority: DeadlinePriority;
  status: FilingStatus;
  estimatedAmount?: number;
  documentsReady: boolean;
}

export interface KPIAlertDto {
  id: number;
  alertType: KPIAlertType;
  title: string;
  message: string;
  severity: KPIAlertSeverity;
  clientId?: number;
  clientName?: string;
  createdAt: string;
  isRead: boolean;
  actionUrl?: string;
}

export interface KPIThresholdDto {
  minComplianceRate: number;
  maxFilingDelayDays: number;
  minPaymentCompletionRate: number;
  minDocumentCompletionRate: number;
  minEngagementRate: number;
}

// Enums
export enum ComplianceLevel {
  Red = 'Red',
  Yellow = 'Yellow',
  Green = 'Green'
}

export enum TaxType {
  GST = 'GST',
  IncomeTax = 'IncomeTax',
  PAYE = 'PAYE',
  CorporateTax = 'CorporateTax',
  WithholdingTax = 'WithholdingTax',
  ExciseDuty = 'ExciseDuty'
}

export enum FilingStatus {
  NotStarted = 'NotStarted',
  InProgress = 'InProgress',
  Pending = 'Pending',
  Filed = 'Filed',
  Overdue = 'Overdue',
  Rejected = 'Rejected'
}

export enum DeadlinePriority {
  Low = 'Low',
  Medium = 'Medium',
  High = 'High',
  Critical = 'Critical'
}

export enum KPIAlertType {
  ComplianceThreshold = 'ComplianceThreshold',
  FilingOverdue = 'FilingOverdue',
  PaymentDelayed = 'PaymentDelayed',
  DocumentMissing = 'DocumentMissing',
  ClientInactive = 'ClientInactive'
}

export enum KPIAlertSeverity {
  Info = 'Info',
  Warning = 'Warning',
  Error = 'Error',
  Critical = 'Critical'
}

// Chart data interfaces
export interface ChartDataPoint {
  name: string;
  value: number;
  label?: string;
  color?: string;
}

export interface ComplianceChartData {
  date: string;
  compliance: number;
  target: number;
  period: string;
}

export interface TaxTypeChartData {
  taxType: string;
  compliance: number;
  filings: number;
  amount: number;
}