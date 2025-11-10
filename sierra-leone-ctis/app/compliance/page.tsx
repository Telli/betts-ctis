'use client'

import { useState, useEffect } from 'react'
import { Card, CardContent, CardDescription, CardHeader, CardTitle } from '@/components/ui/card'
import { Badge } from '@/components/ui/badge'
import { Button } from '@/components/ui/button'
import { Progress } from '@/components/ui/progress'
import { Tabs, TabsContent, TabsList, TabsTrigger } from '@/components/ui/tabs'
import { AlertTriangle, CheckCircle, Clock, XCircle, TrendingUp, FileText, DollarSign, Calendar, Download } from 'lucide-react'
import { ComplianceService, ComplianceOverviewData, ComplianceItem } from '@/lib/services/compliance-service'
import { formatSierraLeones, formatPercentage } from '@/lib/utils/currency'
import { PageHeader } from '@/components/page-header'
import { FilingChecklistMatrix } from '@/components/filing-checklist-matrix'
import { PenaltyWarningsCard } from '@/components/penalty-warnings-card'
import { DocumentSubmissionTracker } from '@/components/document-submission-tracker'
import { ComplianceTimeline } from '@/components/compliance-timeline'


export default function CompliancePage() {
  const [complianceData, setComplianceData] = useState<ComplianceItem[]>([])
  const [overviewData, setOverviewData] = useState<ComplianceOverviewData | null>(null)
  const [loading, setLoading] = useState(true)
  const [error, setError] = useState<string | null>(null)
  const [activeTab, setActiveTab] = useState('overview')

  useEffect(() => {
    const fetchComplianceData = async () => {
      try {
        setLoading(true)
        setError(null)
        
        const [complianceItems, overview] = await Promise.all([
          ComplianceService.getComplianceData(),
          ComplianceService.getComplianceOverview()
        ])
        
        setComplianceData(complianceItems || [])
        setOverviewData(overview)
      } catch (err) {
        console.error('Error fetching compliance data:', err)
        setError('Failed to load compliance data. Please try again later.')
      } finally {
        setLoading(false)
      }
    }

    fetchComplianceData()
  }, [])

  const getStatusIcon = (status: string) => {
    switch (status) {
      case 'compliant':
        return <CheckCircle className="h-4 w-4 text-green-500" />
      case 'at-risk':
        return <Clock className="h-4 w-4 text-yellow-500" />
      case 'non-compliant':
        return <AlertTriangle className="h-4 w-4 text-orange-500" />
      case 'overdue':
        return <XCircle className="h-4 w-4 text-red-500" />
      default:
        return null
    }
  }

  const getStatusColor = (status: string) => {
    switch (status) {
      case 'compliant':
        return 'bg-green-100 text-green-800'
      case 'at-risk':
        return 'bg-yellow-100 text-yellow-800'
      case 'non-compliant':
        return 'bg-orange-100 text-orange-800'
      case 'overdue':
        return 'bg-red-100 text-red-800'
      default:
        return 'bg-gray-100 text-gray-800'
    }
  }

  const getScoreColor = (score: number) => {
    if (score >= 85) return 'text-green-600'
    if (score >= 70) return 'text-yellow-600'
    if (score >= 50) return 'text-orange-600'
    return 'text-red-600'
  }

  const overallStats = overviewData || {
    totalClients: 0,
    compliant: 0,
    atRisk: 0,
    overdue: 0,
    averageScore: 0,
    totalOutstanding: 0,
    totalAlerts: 0
  }

  if (loading) {
    return (
      <div className="flex items-center justify-center min-h-screen">
        <div className="animate-spin rounded-full h-32 w-32 border-b-2 border-sierra-blue"></div>
      </div>
    )
  }

  if (error) {
    return (
      <div className="flex items-center justify-center min-h-screen">
        <Card className="w-full max-w-md">
          <CardHeader>
            <CardTitle>Error Loading Compliance Data</CardTitle>
          </CardHeader>
          <CardContent>
            <p className="text-muted-foreground mb-4">{error}</p>
            <Button onClick={() => window.location.reload()}>
              Retry
            </Button>
          </CardContent>
        </Card>
      </div>
    )
  }

  if (!complianceData?.length && !loading) {
    return (
      <div className="flex items-center justify-center min-h-screen">
        <Card className="w-full max-w-md">
          <CardHeader>
            <CardTitle>No Compliance Data</CardTitle>
          </CardHeader>
          <CardContent>
            <p className="text-muted-foreground">No compliance data is available for your clients.</p>
          </CardContent>
        </Card>
      </div>
    )
  }

  return (
    <div className="flex-1 flex flex-col">
      <PageHeader
        title="Compliance Overview"
        breadcrumbs={[{ label: 'Compliance' }]}
        description="Monitor client compliance with Sierra Leone Finance Act 2025 requirements"
        actions={
          <Button variant="outline">
            <Download className="mr-2 h-4 w-4" />
            Export Report
          </Button>
        }
      />
      
      <div className="flex-1 p-6 space-y-6">

      {/* Overview Cards */}
      <div className="grid gap-4 md:grid-cols-2 lg:grid-cols-4">
        <Card>
          <CardHeader className="flex flex-row items-center justify-between space-y-0 pb-2">
            <CardTitle className="text-sm font-medium">Overall Score</CardTitle>
            <TrendingUp className="h-4 w-4 text-muted-foreground" />
          </CardHeader>
          <CardContent>
            <div className={`text-2xl font-bold ${getScoreColor(overallStats.averageScore)}`}>
              {formatPercentage(overallStats.averageScore)}
            </div>
            <Progress value={overallStats.averageScore} className="mt-2" />
          </CardContent>
        </Card>

        <Card>
          <CardHeader className="flex flex-row items-center justify-between space-y-0 pb-2">
            <CardTitle className="text-sm font-medium">Compliant Clients</CardTitle>
            <CheckCircle className="h-4 w-4 text-green-500" />
          </CardHeader>
          <CardContent>
            <div className="text-2xl font-bold text-green-600">
              {overallStats.compliant}
            </div>
            <p className="text-xs text-muted-foreground">
              of {overallStats.totalClients} total clients
            </p>
          </CardContent>
        </Card>

        <Card>
          <CardHeader className="flex flex-row items-center justify-between space-y-0 pb-2">
            <CardTitle className="text-sm font-medium">Outstanding Amount</CardTitle>
            <DollarSign className="h-4 w-4 text-muted-foreground" />
          </CardHeader>
          <CardContent>
            <div className="text-2xl font-bold">
              {formatSierraLeones(overallStats.totalOutstanding)}
            </div>
            <p className="text-xs text-muted-foreground">
              across all non-compliant clients
            </p>
          </CardContent>
        </Card>

        <Card>
          <CardHeader className="flex flex-row items-center justify-between space-y-0 pb-2">
            <CardTitle className="text-sm font-medium">Active Alerts</CardTitle>
            <AlertTriangle className="h-4 w-4 text-orange-500" />
          </CardHeader>
          <CardContent>
            <div className="text-2xl font-bold text-orange-600">
              {overallStats.totalAlerts}
            </div>
            <p className="text-xs text-muted-foreground">
              requiring immediate attention
            </p>
          </CardContent>
        </Card>
      </div>

      {/* Detailed Compliance View */}
      <Tabs value={activeTab} onValueChange={setActiveTab}>
        <TabsList>
          <TabsTrigger value="overview">Overview</TabsTrigger>
          <TabsTrigger value="by-status">By Status</TabsTrigger>
          <TabsTrigger value="by-tax-type">By Tax Type</TabsTrigger>
          <TabsTrigger value="alerts">Alerts</TabsTrigger>
        </TabsList>

        <TabsContent value="overview" className="space-y-4">
          <Card>
            <CardHeader>
              <CardTitle>Client Compliance Status</CardTitle>
              <CardDescription>
                Current compliance status for all clients under Finance Act 2025
              </CardDescription>
            </CardHeader>
            <CardContent>
              <div className="space-y-4">
                {(complianceData || []).map((item) => (
                  <div key={item.id} className="flex items-center justify-between p-4 border rounded-lg">
                    <div className="space-y-1">
                      <div className="flex items-center gap-2">
                        {getStatusIcon(item.status)}
                        <span className="font-medium">{item.clientName}</span>
                        <Badge variant="outline">{item.clientName}</Badge>
                      </div>
                      <div className="text-sm text-muted-foreground">
                        {item.type} • {item.taxYear ? `Tax Year ${item.taxYear}` : item.category}
                      </div>
                    </div>
                    
                    <div className="flex items-center gap-4">
                      <div className="text-right">
                        {item.complianceScore && (
                          <div className={`text-sm font-medium ${getScoreColor(item.complianceScore)}`}>
                            Score: {formatPercentage(item.complianceScore)}
                          </div>
                        )}
                        {item.daysOverdue && item.daysOverdue > 0 && (
                          <div className="text-xs text-red-600">
                            {item.daysOverdue} days overdue
                          </div>
                        )}
                      </div>
                      
                      <Badge className={getStatusColor(item.status)}>
                        {item.status.replace('-', ' ')}
                      </Badge>
                      
                      {item.alerts && item.alerts > 0 && (
                        <Badge variant="destructive">
                          {item.alerts} alerts
                        </Badge>
                      )}
                    </div>
                  </div>
                ))}
              </div>
            </CardContent>
          </Card>
        </TabsContent>

        <TabsContent value="by-status">
          <div className="grid gap-4 md:grid-cols-2 lg:grid-cols-4">
            {['compliant', 'at-risk', 'non-compliant', 'overdue'].map((status) => (
              <Card key={status}>
                <CardHeader>
                  <CardTitle className="flex items-center gap-2">
                    {getStatusIcon(status)}
                    {status.replace('-', ' ').toUpperCase()}
                  </CardTitle>
                </CardHeader>
                <CardContent>
                  <div className="space-y-2">
                    {(complianceData || [])
                      .filter(item => item.status === status)
                      .map(item => (
                        <div key={item.id} className="text-sm">
                          <div className="font-medium">{item.clientName}</div>
                          <div className="text-muted-foreground">{item.taxType || item.type}</div>
                        </div>
                      ))}
                  </div>
                </CardContent>
              </Card>
            ))}
          </div>
        </TabsContent>

        <TabsContent value="by-tax-type">
          <Card>
            <CardHeader>
              <CardTitle>Compliance by Tax Type</CardTitle>
            </CardHeader>
            <CardContent>
              <div className="space-y-4">
                {['Income Tax', 'GST', 'Payroll Tax', 'Excise Duty'].map((taxType) => {
                  const items = (complianceData || []).filter(item => (item.taxType || item.type) === taxType)
                  const avgScore = items.length > 0 
                    ? Math.round(items.reduce((sum, item) => sum + (item.complianceScore || 0), 0) / items.length)
                    : 0
                  
                  return (
                    <div key={taxType} className="flex items-center justify-between p-4 border rounded-lg">
                      <div>
                        <div className="font-medium">{taxType}</div>
                        <div className="text-sm text-muted-foreground">
                          {items.length} clients
                        </div>
                      </div>
                      <div className="text-right">
                        <div className={`text-lg font-bold ${getScoreColor(avgScore)}`}>
                          {formatPercentage(avgScore)}
                        </div>
                        <Progress value={avgScore} className="w-24 mt-1" />
                      </div>
                    </div>
                  )
                })}
              </div>
            </CardContent>
          </Card>
        </TabsContent>

        <TabsContent value="alerts">
          <Card>
            <CardHeader>
              <CardTitle>Active Compliance Alerts</CardTitle>
              <CardDescription>
                Issues requiring immediate attention to maintain compliance
              </CardDescription>
            </CardHeader>
            <CardContent>
              <div className="space-y-4">
                {(complianceData || [])
                  .filter(item => item.alerts && item.alerts > 0)
                  .map(item => (
                    <div key={item.id} className="flex items-center justify-between p-4 border border-orange-200 rounded-lg bg-orange-50">
                      <div className="space-y-1">
                        <div className="flex items-center gap-2">
                          <AlertTriangle className="h-4 w-4 text-orange-500" />
                          <span className="font-medium">{item.clientName}</span>
                        </div>
                        <div className="text-sm text-muted-foreground">
                          {item.taxType || item.type} • {item.alerts} active alerts
                        </div>
                      </div>
                      <Button variant="outline" size="sm">
                        View Details
                      </Button>
                    </div>
                  ))}
              </div>
            </CardContent>
          </Card>
        </TabsContent>
      </Tabs>

      {/* New Phase 3 Components */}
      <div className="grid gap-6 md:grid-cols-1 lg:grid-cols-2">
        <FilingChecklistMatrix />
        <PenaltyWarningsCard />
      </div>

      <div className="grid gap-6 md:grid-cols-1 lg:grid-cols-2">
        <DocumentSubmissionTracker />
        <ComplianceTimeline />
      </div>
    </div>
    </div>
  )
}