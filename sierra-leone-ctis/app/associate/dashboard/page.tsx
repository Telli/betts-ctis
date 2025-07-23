'use client';

import { useState, useEffect } from 'react';
import { Card, CardContent, CardDescription, CardHeader, CardTitle } from '@/components/ui/card';
import { Button } from '@/components/ui/button';
import { Badge } from '@/components/ui/badge';
import { Tabs, TabsContent, TabsList, TabsTrigger } from '@/components/ui/tabs';
import { useToast } from '@/hooks/use-toast';
import { useAuth } from '@/context/auth-context';
import { 
  Loader2, 
  Users, 
  FileText, 
  Clock, 
  AlertTriangle,
  CheckCircle,
  Eye,
  Download,
  Calendar,
  Activity,
  TrendingUp
} from 'lucide-react';
import Link from 'next/link';
import { 
  AssociatePermissionService,
  ClientDelegationService, 
  OnBehalfActionService,
  type ClientDto,
  type OnBehalfActionDto,
  type DelegationStatisticsDto
} from '@/lib/services';

interface AssociateDashboardData {
  summary: {
    totalClients: number;
    totalPermissions: number;
    expiringPermissions: number;
    recentActions: number;
    upcomingDeadlines: number;
  };
  delegatedClients: Array<{
    clientId: number;
    businessName: string;
    contactPerson: string;
    taxpayerCategory: string;
    hasUpcomingDeadlines: boolean;
  }>;
  recentActions: Array<{
    id: number;
    action: string;
    entityType: string;
    entityId: number;
    clientName: string;
    actionDate: string;
    reason: string;
  }>;
  upcomingDeadlines: Array<{
    taxFilingId: number;
    clientName: string;
    taxType: string;
    dueDate: string;
    status: string;
    daysUntilDue: number;
  }>;
  permissionAlerts: {
    expiringPermissions: Array<{
      id: number;
      clientName: string;
      permissionArea: string;
      expiryDate: string;
      daysUntilExpiry: number;
    }>;
  };
  statistics: {
    permissionsByArea: Record<string, number>;
    actionsByType: Record<string, number>;
    actionsByEntityType: Record<string, number>;
    actionsPerDay: Record<string, number>;
  };
}

export default function AssociateDashboardPage() {
  const [isLoading, setIsLoading] = useState(true);
  const [dashboardData, setDashboardData] = useState<AssociateDashboardData | null>(null);
  const { user } = useAuth();
  const { toast } = useToast();

  useEffect(() => {
    if (user?.id) {
      loadDashboardData();
    }
  }, [user]);

  const loadDashboardData = async () => {
    if (!user?.id) return;

    try {
      setIsLoading(true);
      
      // Make API call to associate dashboard endpoint
      const response = await fetch(`/api/associate-dashboard/${user.id}`, {
        headers: {
          'Authorization': `Bearer ${localStorage.getItem('auth_token')}`,
          'Content-Type': 'application/json',
        },
      });

      if (!response.ok) {
        throw new Error('Failed to load dashboard data');
      }

      const result = await response.json();
      setDashboardData(result.data);
    } catch (error: any) {
      toast({
        title: 'Error',
        description: 'Failed to load dashboard data',
        variant: 'destructive',
      });
    } finally {
      setIsLoading(false);
    }
  };

  const getTaxpayerCategoryColor = (category: string) => {
    switch (category.toLowerCase()) {
      case 'large':
        return 'bg-red-100 text-red-800';
      case 'medium':
        return 'bg-orange-100 text-orange-800';
      case 'small':
        return 'bg-yellow-100 text-yellow-800';
      case 'micro':
        return 'bg-green-100 text-green-800';
      default:
        return 'bg-gray-100 text-gray-800';
    }
  };

  const getStatusColor = (status: string) => {
    switch (status.toLowerCase()) {
      case 'draft':
        return 'bg-gray-100 text-gray-800';
      case 'submitted':
        return 'bg-blue-100 text-blue-800';
      case 'approved':
        return 'bg-green-100 text-green-800';
      case 'rejected':
        return 'bg-red-100 text-red-800';
      default:
        return 'bg-gray-100 text-gray-800';
    }
  };

  const getDaysUntilDueColor = (days: number) => {
    if (days < 0) return 'text-red-600'; // Overdue
    if (days <= 7) return 'text-orange-600'; // Due soon
    if (days <= 30) return 'text-yellow-600'; // Due this month
    return 'text-green-600'; // Future
  };

  if (isLoading) {
    return (
      <div className="flex items-center justify-center min-h-[400px]">
        <Loader2 className="h-8 w-8 animate-spin" />
      </div>
    );
  }

  if (!dashboardData) {
    return (
      <div className="container mx-auto p-6">
        <div className="text-center py-8">
          <h1 className="text-2xl font-bold text-gray-900 mb-2">No Dashboard Data</h1>
          <p className="text-gray-600">Unable to load dashboard data at this time.</p>
        </div>
      </div>
    );
  }

  return (
    <div className="container mx-auto p-6 space-y-6">
      <div className="flex items-center justify-between">
        <div>
          <h1 className="text-3xl font-bold">Associate Dashboard</h1>
          <p className="text-muted-foreground">
            Welcome back, {user?.name}. Here's your delegation overview.
          </p>
        </div>
      </div>

      {/* Summary Cards */}
      <div className="grid gap-4 md:grid-cols-2 lg:grid-cols-5">
        <Card>
          <CardHeader className="flex flex-row items-center justify-between space-y-0 pb-2">
            <CardTitle className="text-sm font-medium">Delegated Clients</CardTitle>
            <Users className="h-4 w-4 text-muted-foreground" />
          </CardHeader>
          <CardContent>
            <div className="text-2xl font-bold">{dashboardData.summary.totalClients}</div>
          </CardContent>
        </Card>
        
        <Card>
          <CardHeader className="flex flex-row items-center justify-between space-y-0 pb-2">
            <CardTitle className="text-sm font-medium">Active Permissions</CardTitle>
            <CheckCircle className="h-4 w-4 text-muted-foreground" />
          </CardHeader>
          <CardContent>
            <div className="text-2xl font-bold">{dashboardData.summary.totalPermissions}</div>
          </CardContent>
        </Card>
        
        <Card>
          <CardHeader className="flex flex-row items-center justify-between space-y-0 pb-2">
            <CardTitle className="text-sm font-medium">Expiring Soon</CardTitle>
            <AlertTriangle className="h-4 w-4 text-muted-foreground" />
          </CardHeader>
          <CardContent>
            <div className="text-2xl font-bold text-orange-600">
              {dashboardData.summary.expiringPermissions}
            </div>
          </CardContent>
        </Card>
        
        <Card>
          <CardHeader className="flex flex-row items-center justify-between space-y-0 pb-2">
            <CardTitle className="text-sm font-medium">Recent Actions</CardTitle>
            <Activity className="h-4 w-4 text-muted-foreground" />
          </CardHeader>
          <CardContent>
            <div className="text-2xl font-bold">{dashboardData.summary.recentActions}</div>
          </CardContent>
        </Card>
        
        <Card>
          <CardHeader className="flex flex-row items-center justify-between space-y-0 pb-2">
            <CardTitle className="text-sm font-medium">Upcoming Deadlines</CardTitle>
            <Calendar className="h-4 w-4 text-muted-foreground" />
          </CardHeader>
          <CardContent>
            <div className="text-2xl font-bold text-blue-600">
              {dashboardData.summary.upcomingDeadlines}
            </div>
          </CardContent>
        </Card>
      </div>

      <Tabs defaultValue="overview" className="space-y-4">
        <TabsList>
          <TabsTrigger value="overview">Overview</TabsTrigger>
          <TabsTrigger value="clients">My Clients</TabsTrigger>
          <TabsTrigger value="actions">Recent Actions</TabsTrigger>
          <TabsTrigger value="deadlines">Deadlines</TabsTrigger>
          <TabsTrigger value="permissions">Permissions</TabsTrigger>
        </TabsList>

        <TabsContent value="overview" className="space-y-4">
          <div className="grid gap-4 md:grid-cols-2">
            {/* Quick Actions */}
            <Card>
              <CardHeader>
                <CardTitle>Quick Actions</CardTitle>
                <CardDescription>Common tasks for managing client tax matters</CardDescription>
              </CardHeader>
              <CardContent className="space-y-3">
                <Link href="/tax-filings">
                  <Button variant="outline" className="w-full justify-start">
                    <FileText className="mr-2 h-4 w-4" />
                    View Tax Filings
                  </Button>
                </Link>
                <Link href="/documents">
                  <Button variant="outline" className="w-full justify-start">
                    <Download className="mr-2 h-4 w-4" />
                    Manage Documents
                  </Button>
                </Link>
                <Link href="/payments">
                  <Button variant="outline" className="w-full justify-start">
                    <TrendingUp className="mr-2 h-4 w-4" />
                    Process Payments
                  </Button>
                </Link>
              </CardContent>
            </Card>

            {/* Statistics */}
            <Card>
              <CardHeader>
                <CardTitle>Activity Statistics</CardTitle>
                <CardDescription>Your recent activity breakdown</CardDescription>
              </CardHeader>
              <CardContent>
                <div className="space-y-3">
                  <div className="flex justify-between items-center">
                    <span className="text-sm">Actions by Type:</span>
                  </div>
                  {Object.entries(dashboardData.statistics.actionsByType).map(([type, count]) => (
                    <div key={type} className="flex justify-between items-center">
                      <span className="text-sm text-muted-foreground">{type}</span>
                      <Badge variant="secondary">{count}</Badge>
                    </div>
                  ))}
                </div>
              </CardContent>
            </Card>
          </div>
        </TabsContent>

        <TabsContent value="clients" className="space-y-4">
          <Card>
            <CardHeader>
              <CardTitle>Delegated Clients</CardTitle>
              <CardDescription>Clients you have permissions to manage</CardDescription>
            </CardHeader>
            <CardContent>
              <div className="space-y-4">
                {dashboardData.delegatedClients.map((client) => (
                  <div key={client.clientId} className="flex items-center justify-between p-4 border rounded-lg">
                    <div className="flex items-center space-x-4">
                      <div>
                        <div className="flex items-center space-x-2">
                          <h3 className="font-medium">{client.businessName}</h3>
                          <Badge 
                            variant="outline" 
                            className={getTaxpayerCategoryColor(client.taxpayerCategory)}
                          >
                            {client.taxpayerCategory}
                          </Badge>
                          {client.hasUpcomingDeadlines && (
                            <Badge variant="destructive" className="text-xs">
                              <Clock className="mr-1 h-3 w-3" />
                              Deadlines
                            </Badge>
                          )}
                        </div>
                        <p className="text-sm text-muted-foreground">{client.contactPerson}</p>
                      </div>
                    </div>
                    <div className="flex space-x-2">
                      <Link href={`/clients/${client.clientId}`}>
                        <Button size="sm" variant="outline">
                          <Eye className="mr-1 h-3 w-3" />
                          View
                        </Button>
                      </Link>
                    </div>
                  </div>
                ))}
                
                {dashboardData.delegatedClients.length === 0 && (
                  <div className="text-center py-8 text-muted-foreground">
                    No delegated clients found
                  </div>
                )}
              </div>
            </CardContent>
          </Card>
        </TabsContent>

        <TabsContent value="actions" className="space-y-4">
          <Card>
            <CardHeader>
              <CardTitle>Recent Actions</CardTitle>
              <CardDescription>Your recent activities on behalf of clients</CardDescription>
            </CardHeader>
            <CardContent>
              <div className="space-y-3">
                {dashboardData.recentActions.map((action) => (
                  <div key={action.id} className="flex items-center justify-between p-3 border rounded">
                    <div className="flex items-center space-x-3">
                      <Activity className="h-4 w-4 text-muted-foreground" />
                      <div>
                        <div className="flex items-center space-x-2">
                          <span className="font-medium">{action.action}</span>
                          <Badge variant="outline">{action.entityType}</Badge>
                        </div>
                        <div className="text-sm text-muted-foreground">
                          {action.clientName} • {action.reason}
                        </div>
                      </div>
                    </div>
                    <div className="text-sm text-muted-foreground">
                      {new Date(action.actionDate).toLocaleDateString()}
                    </div>
                  </div>
                ))}
                
                {dashboardData.recentActions.length === 0 && (
                  <div className="text-center py-8 text-muted-foreground">
                    No recent actions found
                  </div>
                )}
              </div>
            </CardContent>
          </Card>
        </TabsContent>

        <TabsContent value="deadlines" className="space-y-4">
          <Card>
            <CardHeader>
              <CardTitle>Upcoming Deadlines</CardTitle>
              <CardDescription>Tax filing deadlines for your delegated clients</CardDescription>
            </CardHeader>
            <CardContent>
              <div className="space-y-3">
                {dashboardData.upcomingDeadlines.map((deadline) => (
                  <div key={deadline.taxFilingId} className="flex items-center justify-between p-3 border rounded">
                    <div className="flex items-center space-x-3">
                      <Calendar className="h-4 w-4 text-muted-foreground" />
                      <div>
                        <div className="flex items-center space-x-2">
                          <span className="font-medium">{deadline.clientName}</span>
                          <Badge variant="outline">{deadline.taxType}</Badge>
                          <Badge className={getStatusColor(deadline.status)}>
                            {deadline.status}
                          </Badge>
                        </div>
                        <div className="text-sm text-muted-foreground">
                          Due: {new Date(deadline.dueDate).toLocaleDateString()}
                        </div>
                      </div>
                    </div>
                    <div className={`text-sm font-medium ${getDaysUntilDueColor(deadline.daysUntilDue)}`}>
                      {deadline.daysUntilDue < 0 
                        ? `${Math.abs(deadline.daysUntilDue)} days overdue`
                        : deadline.daysUntilDue === 0
                        ? 'Due today'
                        : `${deadline.daysUntilDue} days left`
                      }
                    </div>
                  </div>
                ))}
                
                {dashboardData.upcomingDeadlines.length === 0 && (
                  <div className="text-center py-8 text-muted-foreground">
                    No upcoming deadlines
                  </div>
                )}
              </div>
            </CardContent>
          </Card>
        </TabsContent>

        <TabsContent value="permissions" className="space-y-4">
          <Card>
            <CardHeader>
              <CardTitle>Permission Alerts</CardTitle>
              <CardDescription>Permissions that require your attention</CardDescription>
            </CardHeader>
            <CardContent>
              <div className="space-y-3">
                {dashboardData.permissionAlerts.expiringPermissions.map((permission) => (
                  <div key={permission.id} className="flex items-center justify-between p-3 border rounded border-orange-200 bg-orange-50">
                    <div className="flex items-center space-x-3">
                      <AlertTriangle className="h-4 w-4 text-orange-600" />
                      <div>
                        <div className="flex items-center space-x-2">
                          <span className="font-medium">{permission.clientName}</span>
                          <Badge variant="outline">{permission.permissionArea}</Badge>
                        </div>
                        <div className="text-sm text-muted-foreground">
                          Expires: {new Date(permission.expiryDate).toLocaleDateString()}
                        </div>
                      </div>
                    </div>
                    <div className="text-sm font-medium text-orange-600">
                      {permission.daysUntilExpiry} days left
                    </div>
                  </div>
                ))}
                
                {dashboardData.permissionAlerts.expiringPermissions.length === 0 && (
                  <div className="text-center py-8 text-muted-foreground">
                    No permission alerts
                  </div>
                )}
              </div>
            </CardContent>
          </Card>
        </TabsContent>
      </Tabs>
    </div>
  );
}