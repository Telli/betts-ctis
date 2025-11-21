"use client"

import { useEffect, useState } from 'react'
import { useToast } from '@/components/ui/use-toast'
import { Tabs, TabsContent, TabsList, TabsTrigger } from '@/components/ui/tabs'
import { Card, CardContent, CardDescription, CardHeader, CardTitle } from '@/components/ui/card'
import { Button } from '@/components/ui/button'
import { DashboardService, DashboardData } from '@/lib/services'
import Loading from '@/app/loading'
import ClientSummaryCard from '@/components/dashboard/client-summary-card'
import ComplianceOverview from '@/components/dashboard/compliance-overview'
import RecentActivityList from '@/components/dashboard/recent-activity-list'
import UpcomingDeadlines from '@/components/dashboard/upcoming-deadlines'
import PendingApprovals from '@/components/dashboard/pending-approvals'
import { useAuthGuard } from '@/hooks/use-auth-guard'
import { PageHeader } from '@/components/page-header'
import { MetricCard } from '@/components/metric-card'
import { CheckCircle, Clock, DollarSign, FileText, RefreshCw } from 'lucide-react'
import { getNumericDefault, getArrayDefault, getObjectDefault } from '@/lib/utils/data-defaults'

function DashboardPage() {
  const { isLoading: authLoading, isAuthorized } = useAuthGuard({ requireAuth: true })
  const { toast } = useToast()
  const [loading, setLoading] = useState<boolean>(true)
  const [dashboardData, setDashboardData] = useState<DashboardData | null>(null)
  const [error, setError] = useState<string | null>(null)

  // Fetch dashboard data
  useEffect(() => {
    // Don't fetch data if still checking auth
    if (authLoading || !isAuthorized) return;
    const fetchData = async () => {
      try {
        setLoading(true)
        const data = await DashboardService.getDashboard()
        setDashboardData(data)
        setError(null)
      } catch (err) {
        console.error('Error fetching dashboard data:', err)
        setError('Failed to load dashboard data. Please try again later.')
        toast({
          variant: 'destructive',
          title: 'Error',
          description: 'Failed to load dashboard data',
        })
      } finally {
        setLoading(false)
      }
    }

    fetchData()
  }, [toast, authLoading, isAuthorized])

  // Show loading during auth check or data fetch
  if (authLoading || loading) {
    return <Loading />
  }

  // This shouldn't render if auth guard is working, but just in case
  if (!isAuthorized) {
    return null
  }

  if (error) {
    return (
      <div className="flex items-center justify-center h-full">
        <Card className="w-full max-w-md">
          <CardHeader>
            <CardTitle>Error</CardTitle>
            <CardDescription>Failed to load dashboard data</CardDescription>
          </CardHeader>
          <CardContent>
            <p>{error}</p>
            <button 
              className="mt-4 px-4 py-2 bg-primary text-primary-foreground rounded-md"
              onClick={() => window.location.reload()}
            >
              Retry
            </button>
          </CardContent>
        </Card>
      </div>
    )
  }

  return (
    <div className="flex-1 flex flex-col">
      <PageHeader
        title="Dashboard"
        breadcrumbs={[{ label: 'Dashboard' }]}
        description="Overview of your tax compliance and filing status"
        actions={
          <Button variant="outline" onClick={() => window.location.reload()}>
            <RefreshCw className="w-4 h-4 mr-2" />
            Refresh Data
          </Button>
        }
      />
      
      {dashboardData && (
        <div className="flex-1 space-y-6 p-6">
          {/* Key Metrics */}
          <div className="grid gap-4 md:grid-cols-2 lg:grid-cols-4">
            <MetricCard
              title="Compliance Rate"
              value={`${getNumericDefault(dashboardData.metrics?.complianceRate)}%`}
              trend={dashboardData.metrics?.complianceRateTrendUp ? "up" : "down"}
              trendValue={dashboardData.metrics?.complianceRateTrend ?? "0%"}
              subtitle="vs last month"
              icon={<CheckCircle className="w-4 h-4" />}
              color="success"
            />
            <MetricCard
              title="Filing Timeliness"
              value={`${getNumericDefault(dashboardData.metrics?.filingTimelinessAvgDays)} days`}
              trend={dashboardData.metrics?.filingTimelinessTrendUp ? "up" : "down"}
              trendValue={dashboardData.metrics?.filingTimelinessTrend ?? "0 days"}
              subtitle="avg before deadline"
              icon={<Clock className="w-4 h-4" />}
              color="info"
            />
            <MetricCard
              title="Payment Status"
              value={`${getNumericDefault(dashboardData.metrics?.paymentOnTimeRate)}%`}
              trend={dashboardData.metrics?.paymentOnTimeRateTrendUp ? "up" : "down"}
              trendValue={dashboardData.metrics?.paymentOnTimeRateTrend ?? "0%"}
              subtitle="on-time payments"
              icon={<DollarSign className="w-4 h-4" />}
              color="warning"
            />
            <MetricCard
              title="Documents"
              value={`${getNumericDefault(dashboardData.metrics?.documentSubmissionRate)}%`}
              trend={dashboardData.metrics?.documentSubmissionRateTrendUp ? "up" : "down"}
              trendValue={dashboardData.metrics?.documentSubmissionRateTrend ?? "0%"}
              subtitle="submission rate"
              icon={<FileText className="w-4 h-4" />}
              color="primary"
            />
          </div>

          {/* Client Summary */}
          <ClientSummaryCard 
            clientSummary={dashboardData.clientSummary ?? {
              totalClients: 0,
              compliantClients: 0,
              pendingClients: 0,
              warningClients: 0,
              overdueClients: 0,
            }} 
          />
          
          <Tabs defaultValue="overview" className="space-y-4">
            <TabsList>
              <TabsTrigger value="overview">Overview</TabsTrigger>
              <TabsTrigger value="activity">Activity</TabsTrigger>
              <TabsTrigger value="approvals">Pending Approvals</TabsTrigger>
            </TabsList>
            
            <TabsContent value="overview" className="space-y-4">
              <div className="grid gap-4 md:grid-cols-2 lg:grid-cols-7">
                <ComplianceOverview 
                  className="col-span-4" 
                  complianceOverview={dashboardData.complianceOverview ?? {
                    totalFilings: 0,
                    completedFilings: 0,
                    pendingFilings: 0,
                    lateFilings: 0,
                    taxTypeBreakdown: {},
                    monthlyRevenue: {},
                  }} 
                />
                <UpcomingDeadlines
                  className="col-span-3"
                  deadlines={getArrayDefault(dashboardData.upcomingDeadlines)}
                />
              </div>
            </TabsContent>
            
            <TabsContent value="activity" className="space-y-4">
              <RecentActivityList activities={getArrayDefault(dashboardData.recentActivity)} />
            </TabsContent>
            
            <TabsContent value="approvals" className="space-y-4">
              <PendingApprovals approvals={getArrayDefault(dashboardData.pendingApprovals)} />
            </TabsContent>
          </Tabs>
        </div>
      )}
    </div>
  )
}

// Export the component with auth guard protection
export default DashboardPage
