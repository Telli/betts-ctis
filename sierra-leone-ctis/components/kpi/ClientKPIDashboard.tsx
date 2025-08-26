'use client';

import { useQuery } from '@tanstack/react-query';
import { Card, CardContent, CardDescription, CardHeader, CardTitle } from '@/components/ui/card';
import { Badge } from '@/components/ui/badge';
import { Progress } from '@/components/ui/progress';
import { Alert, AlertDescription } from '@/components/ui/alert';
import { 
  Calendar,
  Clock, 
  DollarSign, 
  FileText, 
  TrendingUp, 
  AlertTriangle,
  CheckCircle,
  XCircle,
  MinusCircle
} from 'lucide-react';
import { format, differenceInDays } from 'date-fns';
import { useMyKPIs } from '@/lib/hooks/useKPIs';
import { ComplianceLevel, DeadlinePriority, FilingStatus } from '@/lib/types/kpi';
import ComplianceScoreCard from './ComplianceScoreCard';
import FilingTimelinessChart from './FilingTimelinessChart';
import PaymentTimelinessChart from './PaymentTimelinessChart';

interface ClientKPIDashboardProps {
  clientId?: number;
  showHeader?: boolean;
}

export default function ClientKPIDashboard({ 
  clientId, 
  showHeader = true 
}: ClientKPIDashboardProps) {
  const { data: clientKPIs, isLoading, error } = useMyKPIs();

  if (isLoading) {
    return <ClientKPILoadingSkeleton />;
  }

  if (error) {
    return (
      <Alert variant="destructive">
        <AlertTriangle className="h-4 w-4" />
        <AlertDescription>
          Failed to load your KPI data. Please try refreshing the page or contact support.
        </AlertDescription>
      </Alert>
    );
  }

  if (!clientKPIs) {
    return (
      <Alert>
        <AlertTriangle className="h-4 w-4" />
        <AlertDescription>
          No KPI data available. Your metrics will appear as you interact with the system.
        </AlertDescription>
      </Alert>
    );
  }

  const urgentDeadlines = clientKPIs.upcomingDeadlines.filter(
    deadline => deadline.daysRemaining <= 7 && deadline.status !== FilingStatus.Filed
  );

  const overdueItems = clientKPIs.upcomingDeadlines.filter(
    deadline => deadline.daysRemaining < 0
  );

  return (
    <div className="space-y-6">
      {showHeader && (
        <div className="flex items-center justify-between">
          <div>
            <h2 className="text-2xl font-bold text-sierra-blue-900">My Tax Dashboard</h2>
            <p className="text-gray-600">
              Track your compliance status and upcoming obligations
            </p>
          </div>
          <div className="text-sm text-gray-500">
            Last updated: {format(new Date(clientKPIs.calculatedAt), 'PPp')}
          </div>
        </div>
      )}

      {/* Alert Section */}
      {(urgentDeadlines.length > 0 || overdueItems.length > 0) && (
        <div className="space-y-3">
          {overdueItems.length > 0 && (
            <Alert variant="destructive">
              <XCircle className="h-4 w-4" />
              <AlertDescription>
                <strong>Overdue Items:</strong> You have {overdueItems.length} overdue tax obligation{overdueItems.length !== 1 ? 's' : ''}. 
                Please address immediately to avoid penalties.
              </AlertDescription>
            </Alert>
          )}
          
          {urgentDeadlines.length > 0 && (
            <Alert>
              <Clock className="h-4 w-4" />
              <AlertDescription>
                <strong>Upcoming Deadlines:</strong> You have {urgentDeadlines.length} tax filing{urgentDeadlines.length !== 1 ? 's' : ''} due within 7 days.
              </AlertDescription>
            </Alert>
          )}
        </div>
      )}

      {/* Compliance Score Card */}
      <ComplianceScoreCard 
        score={clientKPIs.complianceScore}
        level={clientKPIs.complianceLevel}
        trend={clientKPIs.filingHistory && clientKPIs.filingHistory.length >= 2 
          ? ((clientKPIs.filingHistory[clientKPIs.filingHistory.length - 1].value - 
              clientKPIs.filingHistory[clientKPIs.filingHistory.length - 2].value) / 
              clientKPIs.filingHistory[clientKPIs.filingHistory.length - 2].value) * 100
          : undefined}
      />

      {/* KPI Summary Cards */}
      <div className="grid grid-cols-1 md:grid-cols-3 gap-6">
        <Card>
          <CardContent className="p-6">
            <div className="flex items-center justify-between">
              <div>
                <p className="text-sm font-medium text-gray-600">Filing Timeliness</p>
                <p className="text-2xl font-bold text-sierra-blue-600">
                  {clientKPIs.myFilingTimeliness.toFixed(1)} days
                </p>
                <p className="text-xs text-gray-500">Average filing delay</p>
              </div>
              <div className={`p-3 rounded-full ${
                clientKPIs.myFilingTimeliness <= 3 ? 'bg-sierra-green-100' :
                clientKPIs.myFilingTimeliness <= 7 ? 'bg-sierra-gold-100' : 'bg-red-100'
              }`}>
                <Calendar className={`h-6 w-6 ${
                  clientKPIs.myFilingTimeliness <= 3 ? 'text-sierra-green-600' :
                  clientKPIs.myFilingTimeliness <= 7 ? 'text-sierra-gold-600' : 'text-red-600'
                }`} />
              </div>
            </div>
          </CardContent>
        </Card>

        <Card>
          <CardContent className="p-6">
            <div className="flex items-center justify-between">
              <div>
                <p className="text-sm font-medium text-gray-600">Payment Timeliness</p>
                <p className="text-2xl font-bold text-sierra-green-600">
                  {clientKPIs.onTimePaymentPercentage.toFixed(1)}%
                </p>
                <p className="text-xs text-gray-500">On-time payments</p>
              </div>
              <div className="p-3 rounded-full bg-sierra-green-100">
                <DollarSign className="h-6 w-6 text-sierra-green-600" />
              </div>
            </div>
          </CardContent>
        </Card>

        <Card>
          <CardContent className="p-6">
            <div className="flex items-center justify-between">
              <div>
                <p className="text-sm font-medium text-gray-600">Document Readiness</p>
                <p className="text-2xl font-bold text-sierra-blue-600">
                  {clientKPIs.documentReadinessScore.toFixed(1)}%
                </p>
                <p className="text-xs text-gray-500">Documents submitted</p>
              </div>
              <div className="p-3 rounded-full bg-sierra-blue-100">
                <FileText className="h-6 w-6 text-sierra-blue-600" />
              </div>
            </div>
          </CardContent>
        </Card>
      </div>

      {/* Charts Section */}
      <div className="grid grid-cols-1 lg:grid-cols-2 gap-6">
        <Card>
          <CardHeader>
            <CardTitle>Filing Performance Trend</CardTitle>
            <CardDescription>
              Your filing timeliness over the past months
            </CardDescription>
          </CardHeader>
          <CardContent>
            <FilingTimelinessChart data={clientKPIs.filingHistory} />
          </CardContent>
        </Card>

        <Card>
          <CardHeader>
            <CardTitle>Payment Performance Trend</CardTitle>
            <CardDescription>
              Your payment timeliness over the past months
            </CardDescription>
          </CardHeader>
          <CardContent>
            <PaymentTimelinessChart data={clientKPIs.paymentHistory} />
          </CardContent>
        </Card>
      </div>

      {/* Upcoming Deadlines */}
      <Card>
        <CardHeader>
          <CardTitle>Upcoming Tax Deadlines</CardTitle>
          <CardDescription>
            Your scheduled tax obligations and filing deadlines
          </CardDescription>
        </CardHeader>
        <CardContent>
          {clientKPIs.upcomingDeadlines.length === 0 ? (
            <div className="text-center py-8 text-gray-500">
              <CheckCircle className="h-12 w-12 mx-auto mb-4 text-sierra-green-500" />
              <p className="text-lg font-medium">All caught up!</p>
              <p className="text-sm">No upcoming deadlines at this time.</p>
            </div>
          ) : (
            <div className="space-y-4">
              {clientKPIs.upcomingDeadlines.map((deadline) => (
                <DeadlineCard key={deadline.id} deadline={deadline} />
              ))}
            </div>
          )}
        </CardContent>
      </Card>
    </div>
  );
}

function DeadlineCard({ deadline }: { deadline: any }) {
  const getPriorityColor = (priority: DeadlinePriority) => {
    switch (priority) {
      case DeadlinePriority.Critical:
        return 'bg-red-100 text-red-800';
      case DeadlinePriority.High:
        return 'bg-orange-100 text-orange-800';
      case DeadlinePriority.Medium:
        return 'bg-sierra-gold-100 text-sierra-gold-800';
      default:
        return 'bg-sierra-blue-100 text-sierra-blue-800';
    }
  };

  const getStatusIcon = (status: FilingStatus) => {
    switch (status) {
      case FilingStatus.Filed:
        return <CheckCircle className="h-5 w-5 text-sierra-green-600" />;
      case FilingStatus.Overdue:
        return <XCircle className="h-5 w-5 text-red-600" />;
      case FilingStatus.InProgress:
        return <Clock className="h-5 w-5 text-sierra-gold-600" />;
      default:
        return <MinusCircle className="h-5 w-5 text-gray-400" />;
    }
  };

  const isOverdue = deadline.daysRemaining < 0;
  const isUrgent = deadline.daysRemaining <= 7 && deadline.daysRemaining >= 0;

  return (
    <div className={`p-4 border rounded-lg ${
      isOverdue ? 'border-red-200 bg-red-50' :
      isUrgent ? 'border-sierra-gold-200 bg-sierra-gold-50' :
      'border-gray-200'
    }`}>
      <div className="flex items-center justify-between">
        <div className="flex items-center space-x-3">
          {getStatusIcon(deadline.status)}
          <div>
            <h4 className="font-medium text-gray-900">{deadline.taxType}</h4>
            <p className="text-sm text-gray-600">
              Due: {format(new Date(deadline.dueDate), 'PPP')}
            </p>
            {deadline.estimatedAmount && (
              <p className="text-sm text-gray-600">
                Estimated: SLE {deadline.estimatedAmount.toLocaleString()}
              </p>
            )}
          </div>
        </div>
        
        <div className="text-right space-y-2">
          <Badge className={getPriorityColor(deadline.priority)}>
            {deadline.priority}
          </Badge>
          <p className={`text-sm font-medium ${
            isOverdue ? 'text-red-600' :
            isUrgent ? 'text-sierra-gold-600' :
            'text-gray-600'
          }`}>
            {isOverdue 
              ? `${Math.abs(deadline.daysRemaining)} days overdue`
              : `${deadline.daysRemaining} days remaining`}
          </p>
          <div className="flex items-center space-x-2">
            <div className={`w-2 h-2 rounded-full ${
              deadline.documentsReady ? 'bg-sierra-green-500' : 'bg-red-500'
            }`} />
            <span className="text-xs text-gray-500">
              Documents {deadline.documentsReady ? 'Ready' : 'Pending'}
            </span>
          </div>
        </div>
      </div>
    </div>
  );
}

function ClientKPILoadingSkeleton() {
  return (
    <div className="space-y-6">
      <div className="h-8 w-64 bg-gray-200 rounded animate-pulse" />
      
      <div className="h-32 bg-gray-200 rounded-lg animate-pulse" />
      
      <div className="grid grid-cols-1 md:grid-cols-3 gap-6">
        {Array.from({ length: 3 }).map((_, i) => (
          <div key={i} className="h-24 bg-gray-200 rounded-lg animate-pulse" />
        ))}
      </div>
      
      <div className="grid grid-cols-1 lg:grid-cols-2 gap-6">
        {Array.from({ length: 2 }).map((_, i) => (
          <div key={i} className="h-64 bg-gray-200 rounded-lg animate-pulse" />
        ))}
      </div>
      
      <div className="h-48 bg-gray-200 rounded-lg animate-pulse" />
    </div>
  );
}