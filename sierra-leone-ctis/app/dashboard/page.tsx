"use client"

import { useEffect, useState } from 'react'
import { useToast } from '@/components/ui/use-toast'
import { Tabs, TabsContent, TabsList, TabsTrigger } from '@/components/ui/tabs'
import { Card, CardContent, CardDescription, CardHeader, CardTitle } from '@/components/ui/card'
import { DashboardService, DashboardData } from '@/lib/services'
import Loading from '@/app/loading'
import ClientSummaryCard from '@/components/dashboard/client-summary-card'
import ComplianceOverview from '@/components/dashboard/compliance-overview'
import RecentActivityList from '@/components/dashboard/recent-activity-list'
import UpcomingDeadlines from '@/components/dashboard/upcoming-deadlines'
import PendingApprovals from '@/components/dashboard/pending-approvals'
import { useAuthGuard } from '@/hooks/use-auth-guard'

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
    <div className="flex-1 space-y-4 p-4 md:p-8 pt-6">
      <div className="flex items-center justify-between">
        <h2 className="text-3xl font-bold tracking-tight">Dashboard</h2>
        <div className="flex items-center gap-2">
          <button 
            className="px-4 py-2 bg-primary text-primary-foreground rounded-md" 
            onClick={() => window.location.reload()}
          >
            Refresh Data
          </button>
        </div>
      </div>
      
      {dashboardData && (
        <>
          {/* Client Summary */}
          <ClientSummaryCard clientSummary={dashboardData.clientSummary} />
          
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
                  complianceOverview={dashboardData.complianceOverview} 
                />
                <UpcomingDeadlines
                  className="col-span-3"
                  deadlines={dashboardData.upcomingDeadlines}
                />
              </div>
            </TabsContent>
            
            <TabsContent value="activity" className="space-y-4">
              <RecentActivityList activities={dashboardData.recentActivity} />
            </TabsContent>
            
            <TabsContent value="approvals" className="space-y-4">
              <PendingApprovals approvals={dashboardData.pendingApprovals} />
            </TabsContent>
          </Tabs>
        </>
      )}
    </div>
  )
}

// Export the component with auth guard protection
export default DashboardPage
