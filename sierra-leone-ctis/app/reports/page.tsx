'use client'

import { useState, useEffect } from 'react'
import { Card, CardContent, CardDescription, CardHeader, CardTitle } from '@/components/ui/card'
import { Button } from '@/components/ui/button'
import { Badge } from '@/components/ui/badge'
import { Tabs, TabsContent, TabsList, TabsTrigger } from '@/components/ui/tabs'
import { Select, SelectContent, SelectItem, SelectTrigger, SelectValue } from '@/components/ui/select'
import { Input } from '@/components/ui/input'
import { Label } from '@/components/ui/label'
import { Progress } from '@/components/ui/progress'
import { Separator } from '@/components/ui/separator'
import { ScrollArea } from '@/components/ui/scroll-area'
import { 
  FileText, 
  Download, 
  Clock, 
  CheckCircle, 
  XCircle, 
  AlertCircle,
  FileSpreadsheet,
  Filter,
  Calendar,
  Search,
  RefreshCw,
  Eye,
  Trash2,
  Plus,
  BarChart3
} from 'lucide-react'
import { format } from 'date-fns'
import { ReportGenerationForm } from '@/components/reports/report-generation-form'
import { ReportProgressCard } from '@/components/reports/report-progress-card'
import { ReportStatisticsCard } from '@/components/reports/report-statistics-card'
import ReportGenerator from '@/components/reports/ReportGenerator'
import ReportHistory from '@/components/reports/ReportHistory'
import ReportPreview from '@/components/reports/ReportPreview'
import { reportService, ReportRequest, GenerateReportRequest, ReportStatistics } from '@/lib/services/report-service'

const reportTypes = [
  {
    value: 'TaxSummary',
    label: 'Tax Summary Report',
    description: 'Comprehensive tax overview for clients',
    icon: FileText,
    estimatedDuration: 120
  },
  {
    value: 'ComplianceStatus',
    label: 'Compliance Status Report',
    description: 'Current compliance status and deadlines',
    icon: AlertCircle,
    estimatedDuration: 90
  },
  {
    value: 'PaymentHistory',
    label: 'Payment History Report',
    description: 'Complete payment transaction history',
    icon: FileSpreadsheet,
    estimatedDuration: 180
  },
  {
    value: 'ClientPortfolio',
    label: 'Client Portfolio Report',
    description: 'Detailed client information and statistics',
    icon: FileText,
    estimatedDuration: 240
  },
  {
    value: 'MonthlyReconciliation',
    label: 'Monthly Reconciliation',
    description: 'Monthly financial reconciliation report',
    icon: FileSpreadsheet,
    estimatedDuration: 300
  },
  {
    value: 'AuditTrail',
    label: 'Audit Trail Report',
    description: 'System audit trail for compliance',
    icon: FileText,
    estimatedDuration: 150
  }
]

export default function ReportsPage() {
  const [reports, setReports] = useState<ReportRequest[]>([])
  const [statistics, setStatistics] = useState<ReportStatistics | null>(null)
  const [loading, setLoading] = useState(true)
  const [statisticsLoading, setStatisticsLoading] = useState(true)
  const [generating, setGenerating] = useState(false)
  const [showNewGenerator, setShowNewGenerator] = useState(false)
  const [showPreview, setShowPreview] = useState(false)
  const [selectedReport, setSelectedReport] = useState<ReportRequest | null>(null)
  const [filters, setFilters] = useState({
    status: '',
    type: '',
    dateFrom: '',
    dateTo: '',
    search: ''
  })

  useEffect(() => {
    fetchReports()
    fetchStatistics()
    const interval = setInterval(() => {
      fetchReports()
      fetchStatistics()
    }, 5000) // Poll every 5 seconds for updates
    return () => clearInterval(interval)
  }, [])

  const fetchReports = async () => {
    const result = await reportService.getReports(filters)
    if (result.success && result.data) {
      setReports(result.data)
    } else {
      setReports([])
    }
    setLoading(false)
  }

  const fetchStatistics = async () => {
    const result = await reportService.getStatistics()
    if (result.success && result.data) {
      setStatistics(result.data)
    } else {
      setStatistics(null)
    }
    setStatisticsLoading(false)
  }

  const handleGenerateReport = async (request: GenerateReportRequest) => {
    setGenerating(true)
    const result = await reportService.generateReport(request)
    if (result.success && result.data) {
      setReports(prev => [result.data!, ...prev])
      // Refresh statistics
      fetchStatistics()
    }
    setGenerating(false)
  }

  const downloadReport = async (reportId: string, title: string) => {
    const blob = await reportService.downloadReport(reportId)
    if (blob) {
      const url = window.URL.createObjectURL(blob)
      const a = document.createElement('a')
      a.href = url
      a.download = `${title.replace(/\s/g, '_')}_${new Date().toISOString().split('T')[0]}.pdf`
      document.body.appendChild(a)
      a.click()
      window.URL.revokeObjectURL(url)
      document.body.removeChild(a)
    }
  }

  const cancelReport = async (reportId: string) => {
    const result = await reportService.cancelReport(reportId)
    if (result.success) {
      fetchReports()
    }
  }

  const deleteReport = async (reportId: string) => {
    const result = await reportService.deleteReport(reportId)
    if (result.success) {
      setReports(prev => prev.filter(r => r.id !== reportId))
      fetchStatistics()
    }
  }

  const filteredReports = reports.filter(report => {
    return (
      (!filters.status || report.status === filters.status) &&
      (!filters.type || report.reportType === filters.type) &&
      (!filters.search || report.title.toLowerCase().includes(filters.search.toLowerCase())) &&
      (!filters.dateFrom || new Date(report.createdAt) >= new Date(filters.dateFrom)) &&
      (!filters.dateTo || new Date(report.createdAt) <= new Date(filters.dateTo))
    )
  })

  // Handle new report generator
  if (showNewGenerator) {
    return (
      <div className="container mx-auto p-6 space-y-6">
        <div className="flex items-center justify-between">
          <div>
            <h1 className="text-3xl font-bold text-sierra-blue">Generate New Report</h1>
            <p className="text-muted-foreground">
              Create comprehensive reports with advanced Sierra Leone tax compliance features
            </p>
          </div>
          <Button variant="outline" onClick={() => setShowNewGenerator(false)}>
            ← Back to Reports
          </Button>
        </div>
        <ReportGenerator 
          onReportGenerated={(report) => {
            setReports([report, ...reports]);
            setShowNewGenerator(false);
            fetchStatistics();
          }}
        />
      </div>
    );
  }

  // Handle report preview
  if (showPreview && selectedReport) {
    return (
      <div className="container mx-auto p-6 space-y-6">
        <div className="flex items-center justify-between">
          <div>
            <h1 className="text-3xl font-bold text-sierra-blue">Report Preview</h1>
            <p className="text-muted-foreground">
              {selectedReport.title}
            </p>
          </div>
          <Button variant="outline" onClick={() => setShowPreview(false)}>
            ← Back to Reports
          </Button>
        </div>
        <ReportPreview 
          report={selectedReport}
          onClose={() => setShowPreview(false)}
          onDownload={downloadReport}
        />
      </div>
    );
  }

  return (
    <div className="container mx-auto p-6 space-y-6">
      <div className="flex items-center justify-between">
        <div>
          <h1 className="text-3xl font-bold text-sierra-blue">Reports & Analytics</h1>
          <p className="text-muted-foreground">
            Generate and manage comprehensive reports for Sierra Leone tax compliance
          </p>
        </div>
        <div className="flex items-center space-x-2">
          <Button onClick={() => setShowNewGenerator(true)} size="sm">
            <Plus className="h-4 w-4 mr-2" />
            New Report
          </Button>
          <Button onClick={fetchReports} variant="outline" size="sm">
            <RefreshCw className="h-4 w-4 mr-2" />
            Refresh
          </Button>
        </div>
      </div>

      {/* Filters */}
      <Card>
        <CardContent className="p-6">
          <div className="flex flex-wrap gap-4 items-center">
            <div className="flex items-center space-x-2">
              <Filter className="h-4 w-4" />
              <Select value={filters.status} onValueChange={(value) => setFilters(prev => ({ ...prev, status: value }))}>
                <SelectTrigger className="w-[150px]">
                  <SelectValue placeholder="All Status" />
                </SelectTrigger>
                <SelectContent>
                  <SelectItem value="">All Status</SelectItem>
                  <SelectItem value="Pending">Pending</SelectItem>
                  <SelectItem value="Processing">Processing</SelectItem>
                  <SelectItem value="Completed">Completed</SelectItem>
                  <SelectItem value="Failed">Failed</SelectItem>
                </SelectContent>
              </Select>
            </div>

            <div className="flex items-center space-x-2">
              <Select value={filters.type} onValueChange={(value) => setFilters(prev => ({ ...prev, type: value }))}>
                <SelectTrigger className="w-[200px]">
                  <SelectValue placeholder="All Report Types" />
                </SelectTrigger>
                <SelectContent>
                  <SelectItem value="">All Types</SelectItem>
                  {reportTypes.map((type) => (
                    <SelectItem key={type.value} value={type.value}>
                      {type.label}
                    </SelectItem>
                  ))}
                </SelectContent>
              </Select>
            </div>

            <div className="flex items-center space-x-2">
              <Calendar className="h-4 w-4" />
              <Input
                type="date"
                placeholder="From date"
                value={filters.dateFrom}
                onChange={(e) => setFilters(prev => ({ ...prev, dateFrom: e.target.value }))}
                className="w-[150px]"
              />
              <span className="text-muted-foreground">to</span>
              <Input
                type="date"
                placeholder="To date"
                value={filters.dateTo}
                onChange={(e) => setFilters(prev => ({ ...prev, dateTo: e.target.value }))}
                className="w-[150px]"
              />
            </div>

            <div className="flex items-center space-x-2">
              <Search className="h-4 w-4" />
              <Input
                placeholder="Search reports..."
                value={filters.search}
                onChange={(e) => setFilters(prev => ({ ...prev, search: e.target.value }))}
                className="w-[200px]"
              />
            </div>
          </div>
        </CardContent>
      </Card>

      <Tabs defaultValue="overview" className="space-y-6">
        <TabsList className="grid w-full grid-cols-4">
          <TabsTrigger value="overview">Overview</TabsTrigger>
          <TabsTrigger value="generate">Generate</TabsTrigger>
          <TabsTrigger value="history">History</TabsTrigger>
          <TabsTrigger value="templates">Templates</TabsTrigger>
        </TabsList>

        <TabsContent value="overview" className="space-y-6">
          {statistics && (
            <ReportStatisticsCard 
              statistics={statistics} 
              loading={statisticsLoading}
            />
          )}
        </TabsContent>

        <TabsContent value="generate" className="space-y-6">
          <ReportGenerationForm 
            onGenerate={handleGenerateReport}
            loading={generating}
          />
        </TabsContent>

        <TabsContent value="history" className="space-y-6">
          <ReportHistory 
            reports={filteredReports}
            loading={loading}
            onDownload={downloadReport}
            onDelete={deleteReport}
            onPreview={(report) => {
              setSelectedReport(report);
              setShowPreview(true);
            }}
            onRefresh={fetchReports}
          />
        </TabsContent>

        <TabsContent value="templates" className="space-y-6">
          <Card>
            <CardHeader>
              <CardTitle>Report Templates</CardTitle>
              <CardDescription>
                Manage and customize report templates for Sierra Leone compliance
              </CardDescription>
            </CardHeader>
            <CardContent>
              <div className="grid grid-cols-1 md:grid-cols-2 lg:grid-cols-3 gap-4">
                {reportTypes.map((type) => {
                  const Icon = type.icon
                  return (
                    <Card key={type.value} className="p-4 hover:shadow-md transition-shadow">
                      <div className="flex items-start space-x-3">
                        <Icon className="h-8 w-8 text-sierra-blue" />
                        <div className="flex-1">
                          <h4 className="font-medium">{type.label}</h4>
                          <p className="text-sm text-muted-foreground mb-3">
                            {type.description}
                          </p>
                          <div className="flex items-center justify-between">
                            <span className="text-xs text-muted-foreground">
                              ~{Math.floor(type.estimatedDuration / 60)}m
                            </span>
                            <Button size="sm" variant="outline">
                              <Eye className="h-4 w-4 mr-1" />
                              Preview
                            </Button>
                          </div>
                        </div>
                      </div>
                    </Card>
                  )
                })}
              </div>
            </CardContent>
          </Card>
        </TabsContent>
      </Tabs>
    </div>
  )
}