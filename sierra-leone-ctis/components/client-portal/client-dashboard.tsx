"use client"

import React, { useState, useEffect } from 'react';
import { Card, CardContent, CardDescription, CardHeader, CardTitle } from '@/components/ui/card';
import { Button } from '@/components/ui/button';
import { Badge } from '@/components/ui/badge';
import { Progress } from '@/components/ui/progress';
import { Skeleton } from '@/components/ui/skeleton';
import { useNotifications } from '@/hooks/useSignalR';
import { useToast } from '@/hooks/use-toast';
import { 
  Building2, 
  FileText, 
  DollarSign, 
  Calendar,
  Upload,
  TrendingUp,
  AlertCircle,
  CheckCircle,
  Clock,
  Shield
} from 'lucide-react';
import { apiClient } from '@/lib/api-client';
import { useRouter } from 'next/navigation';

interface ClientDashboardData {
  businessInfo: {
    clientId: number;
    businessName: string;
    contactPerson: string;
    email: string;
    phoneNumber: string;
    tin: string;
    taxpayerCategory: string;
    clientType: string;
    status: string;
  };
  complianceOverview: {
    totalFilings: number;
    completedFilings: number;
    pendingFilings: number;
    lateFilings: number;
    complianceScore: number;
    complianceStatus: string;
    taxTypeBreakdown: Record<string, number>;
    monthlyPayments: Record<string, number>;
  };
  recentActivity: Array<{
    id: number;
    type: string;
    action: string;
    description: string;
    entityName: string;
    timestamp: string;
  }>;
  upcomingDeadlines: Array<{
    id: number;
    title: string;
    description: string;
    dueDate: string;
    type: string;
    isUrgent: boolean;
    daysRemaining: number;
  }>;
  quickActions: {
    canUploadDocuments: boolean;
    canSubmitTaxFiling: boolean;
    canMakePayment: boolean;
    hasPendingFilings: boolean;
    hasOverduePayments: boolean;
    pendingDocumentCount: number;
    upcomingDeadlineCount: number;
  };
}

export function ClientDashboard() {
  const [data, setData] = useState<ClientDashboardData | null>(null);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);
  const router = useRouter();
  const { toast } = useToast();
  
  // Real-time notifications via SignalR
  const { isConnected: notifConnected, notifications, unreadCount } = useNotifications();

  const fetchDashboardData = async () => {
    try {
      setLoading(true);
      setError(null);
      const response = await apiClient.get<{ success: boolean; data: ClientDashboardData }>('/api/client-portal/dashboard');
      setData(response.data.data);
    } catch (err) {
      console.error('Failed to fetch dashboard data:', err);
      setError('Failed to load dashboard data. Please try again later.');
    } finally {
      setLoading(false);
    }
  };

  useEffect(() => {
    fetchDashboardData();
  }, []);
  
  // Show real-time notification connection status
  useEffect(() => {
    if (notifConnected) {
      console.log('✅ Real-time notifications connected');
    }
  }, [notifConnected]);
  
  // Display notification count badge
  useEffect(() => {
    if (unreadCount > 0) {
      console.log(`📬 ${unreadCount} unread notifications`);
    }
  }, [unreadCount]);

  if (loading) {
    return <DashboardSkeleton />;
  }

  if (error) {
    return (
      <div className="p-8">
        <div className="text-center">
          <AlertCircle className="h-12 w-12 text-red-500 mx-auto mb-4" />
          <h2 className="text-xl font-semibold text-gray-900 mb-2">Unable to Load Dashboard</h2>
          <p className="text-gray-600 mb-4">{error}</p>
          <Button onClick={fetchDashboardData}>Try Again</Button>
        </div>
      </div>
    );
  }

  if (!data) {
    return null;
  }

  const getComplianceColor = (status: string) => {
    switch (status.toLowerCase()) {
      case 'compliant': return 'text-green-600 bg-green-50';
      case 'warning': return 'text-yellow-600 bg-yellow-50';
      case 'overdue': return 'text-red-600 bg-red-50';
      default: return 'text-gray-600 bg-gray-50';
    }
  };

  return (
    <div className="p-6 space-y-6">
      {/* Header */}
      <div className="flex items-center justify-between">
        <div>
          <h1 className="text-3xl font-bold text-gray-900">Welcome back, {data.businessInfo.contactPerson}</h1>
          <p className="text-gray-600">{data.businessInfo.businessName}</p>
        </div>
        <div className="flex items-center space-x-2">
          <Badge className={getComplianceColor(data.complianceOverview.complianceStatus)}>
            <Shield className="h-3 w-3 mr-1" />
            {data.complianceOverview.complianceStatus}
          </Badge>
          {unreadCount > 0 && (
            <Badge variant="destructive" className="relative">
              <span className="absolute -top-1 -right-1 flex h-3 w-3">
                <span className="animate-ping absolute inline-flex h-full w-full rounded-full bg-red-400 opacity-75"></span>
                <span className="relative inline-flex rounded-full h-3 w-3 bg-red-500"></span>
              </span>
              {unreadCount} New
            </Badge>
          )}
          {notifConnected && (
            <Badge variant="outline" className="text-green-600 border-green-600">
              <span className="flex h-2 w-2 mr-1">
                <span className="animate-ping absolute inline-flex h-2 w-2 rounded-full bg-green-400 opacity-75"></span>
                <span className="relative inline-flex rounded-full h-2 w-2 bg-green-500"></span>
              </span>
              Live
            </Badge>
          )}
        </div>
      </div>

      {/* Quick Actions */}
      <div className="grid grid-cols-1 md:grid-cols-4 gap-4">
        <Card className="hover:shadow-md transition-shadow cursor-pointer" onClick={() => router.push('/client-portal/documents')}>
          <CardContent className="p-4">
            <div className="flex items-center space-x-2">
              <Upload className="h-5 w-5 text-sierra-blue-600" />
              <span className="font-medium">Upload Documents</span>
            </div>
          </CardContent>
        </Card>
        <Card className="hover:shadow-md transition-shadow cursor-pointer" onClick={() => router.push('/client-portal/tax-filings')}>
          <CardContent className="p-4">
            <div className="flex items-center space-x-2">
              <FileText className="h-5 w-5 text-sierra-blue-600" />
              <span className="font-medium">View Tax Filings</span>
            </div>
          </CardContent>
        </Card>
        <Card className="hover:shadow-md transition-shadow cursor-pointer" onClick={() => router.push('/client-portal/payments')}>
          <CardContent className="p-4">
            <div className="flex items-center space-x-2">
              <DollarSign className="h-5 w-5 text-sierra-blue-600" />
              <span className="font-medium">Payment History</span>
            </div>
          </CardContent>
        </Card>
        <Card className="hover:shadow-md transition-shadow cursor-pointer" onClick={() => router.push('/client-portal/deadlines')}>
          <CardContent className="p-4">
            <div className="flex items-center space-x-2">
              <Calendar className="h-5 w-5 text-sierra-blue-600" />
              <span className="font-medium">View Deadlines</span>
            </div>
          </CardContent>
        </Card>
      </div>

      {/* Main Content Grid */}
      <div className="grid grid-cols-1 lg:grid-cols-3 gap-6">
        {/* Compliance Overview */}
        <Card className="lg:col-span-2">
          <CardHeader>
            <CardTitle className="flex items-center space-x-2">
              <TrendingUp className="h-5 w-5" />
              <span>Tax Compliance Overview</span>
            </CardTitle>
            <CardDescription>Your organization's tax compliance status and performance</CardDescription>
          </CardHeader>
          <CardContent className="space-y-4">
            <div className="flex items-center justify-between">
              <span className="text-sm font-medium">Compliance Score</span>
              <span className="text-2xl font-bold text-sierra-blue-600">{data.complianceOverview.complianceScore}%</span>
            </div>
            <Progress value={data.complianceOverview.complianceScore} className="h-2" />
            
            <div className="grid grid-cols-2 md:grid-cols-4 gap-4 mt-6">
              <div className="text-center">
                <div className="text-2xl font-bold text-gray-900">{data.complianceOverview.totalFilings}</div>
                <div className="text-sm text-gray-600">Total Filings</div>
              </div>
              <div className="text-center">
                <div className="text-2xl font-bold text-green-600">{data.complianceOverview.completedFilings}</div>
                <div className="text-sm text-gray-600">Completed</div>
              </div>
              <div className="text-center">
                <div className="text-2xl font-bold text-yellow-600">{data.complianceOverview.pendingFilings}</div>
                <div className="text-sm text-gray-600">Pending</div>
              </div>
              <div className="text-center">
                <div className="text-2xl font-bold text-red-600">{data.complianceOverview.lateFilings}</div>
                <div className="text-sm text-gray-600">Overdue</div>
              </div>
            </div>
          </CardContent>
        </Card>

        {/* Upcoming Deadlines */}
        <Card>
          <CardHeader>
            <CardTitle className="flex items-center space-x-2">
              <Calendar className="h-5 w-5" />
              <span>Upcoming Deadlines</span>
            </CardTitle>
            <CardDescription>Important dates to remember</CardDescription>
          </CardHeader>
          <CardContent>
            <div className="space-y-3">
              {data.upcomingDeadlines.length === 0 ? (
                <div className="text-center py-4">
                  <CheckCircle className="h-8 w-8 text-green-500 mx-auto mb-2" />
                  <p className="text-sm text-gray-600">No upcoming deadlines</p>
                </div>
              ) : (
                data.upcomingDeadlines.slice(0, 5).map((deadline) => (
                  <div key={deadline.id} className="flex items-center space-x-3 p-2 border rounded-lg">
                    <div className={`p-1 rounded ${deadline.isUrgent ? 'bg-red-100' : 'bg-blue-100'}`}>
                      <Clock className={`h-4 w-4 ${deadline.isUrgent ? 'text-red-600' : 'text-blue-600'}`} />
                    </div>
                    <div className="flex-1 min-w-0">
                      <p className="text-sm font-medium text-gray-900 truncate">{deadline.title}</p>
                      <p className="text-xs text-gray-600">{deadline.daysRemaining} days remaining</p>
                    </div>
                  </div>
                ))
              )}
            </div>
            {data.upcomingDeadlines.length > 5 && (
              <Button variant="outline" size="sm" className="w-full mt-3" onClick={() => router.push('/client-portal/deadlines')}>
                View All Deadlines
              </Button>
            )}
          </CardContent>
        </Card>
      </div>

      {/* Recent Activity */}
      <Card>
        <CardHeader>
          <CardTitle>Recent Activity</CardTitle>
          <CardDescription>Your latest tax-related activities</CardDescription>
        </CardHeader>
        <CardContent>
          <div className="space-y-3">
            {data.recentActivity.length === 0 ? (
              <p className="text-center text-gray-600 py-4">No recent activity</p>
            ) : (
              data.recentActivity.slice(0, 10).map((activity) => (
                <div key={activity.id} className="flex items-center space-x-3 p-2 hover:bg-gray-50 rounded-lg">
                  <div className="p-2 bg-sierra-blue-100 rounded-full">
                    {activity.type === 'document' && <FileText className="h-4 w-4 text-sierra-blue-600" />}
                    {activity.type === 'payment' && <DollarSign className="h-4 w-4 text-sierra-blue-600" />}
                    {activity.type === 'filing' && <Building2 className="h-4 w-4 text-sierra-blue-600" />}
                  </div>
                  <div className="flex-1">
                    <p className="text-sm font-medium text-gray-900">{activity.description}</p>
                    <p className="text-xs text-gray-600">{new Date(activity.timestamp).toLocaleDateString()}</p>
                  </div>
                </div>
              ))
            )}
          </div>
        </CardContent>
      </Card>
    </div>
  );
}

function DashboardSkeleton() {
  return (
    <div className="p-6 space-y-6">
      <div className="flex items-center justify-between">
        <div>
          <Skeleton className="h-8 w-64 mb-2" />
          <Skeleton className="h-4 w-48" />
        </div>
        <Skeleton className="h-6 w-20" />
      </div>
      
      <div className="grid grid-cols-1 md:grid-cols-4 gap-4">
        {[...Array(4)].map((_, i) => (
          <Card key={i}>
            <CardContent className="p-4">
              <Skeleton className="h-5 w-full" />
            </CardContent>
          </Card>
        ))}
      </div>
      
      <div className="grid grid-cols-1 lg:grid-cols-3 gap-6">
        <Card className="lg:col-span-2">
          <CardHeader>
            <Skeleton className="h-6 w-48" />
            <Skeleton className="h-4 w-64" />
          </CardHeader>
          <CardContent>
            <Skeleton className="h-32 w-full" />
          </CardContent>
        </Card>
        
        <Card>
          <CardHeader>
            <Skeleton className="h-6 w-32" />
            <Skeleton className="h-4 w-48" />
          </CardHeader>
          <CardContent>
            <div className="space-y-3">
              {[...Array(3)].map((_, i) => (
                <Skeleton key={i} className="h-12 w-full" />
              ))}
            </div>
          </CardContent>
        </Card>
      </div>
    </div>
  );
}