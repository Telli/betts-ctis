'use client'

import { useEffect, useState } from 'react'
import { Button } from '@/components/ui/button'
import { Card, CardContent, CardDescription, CardHeader, CardTitle } from '@/components/ui/card'
import { Tabs, TabsContent, TabsList, TabsTrigger } from '@/components/ui/tabs'
import { Input } from '@/components/ui/input'
import { Select, SelectContent, SelectItem, SelectTrigger, SelectValue } from '@/components/ui/select'
import { Calendar, Filter, Plus, RefreshCw, Search } from 'lucide-react'
import ReportGenerator from '@/components/reports/ReportGenerator'
import ReportTemplates from '@/components/reports/ReportTemplates'
import ReportHistory from '@/components/reports/ReportHistory'
import ReportPreview from '@/components/reports/ReportPreview'
import { ReportStatisticsCard } from '@/components/reports/report-statistics-card'
import { reportService, type ReportRequest, type GenerateReportRequest, type ReportStatistics } from '@/lib/services/report-service'

export default function ClientPortalReportsPage() {
  const [reports, setReports] = useState<ReportRequest[]>([])
  const [statistics, setStatistics] = useState<ReportStatistics | null>(null)
  const [loading, setLoading] = useState(true)
  const [statisticsLoading, setStatisticsLoading] = useState(true)
  const [generating, setGenerating] = useState(false)
  const [showNewGenerator, setShowNewGenerator] = useState(false)
  const [showPreview, setShowPreview] = useState(false)
  const [initialType, setInitialType] = useState<string | undefined>(undefined)
  const [initialParameters, setInitialParameters] = useState<Record<string, any> | undefined>(undefined)
  const [selectedReport, setSelectedReport] = useState<ReportRequest | null>(null)
  const [page, setPage] = useState(1)
  const [pageSize, setPageSize] = useState(20)
  const [hasMore, setHasMore] = useState(false)
  const [totalPages, setTotalPages] = useState(1)
  const [totalItems, setTotalItems] = useState(0)
  const [filters, setFilters] = useState({
    status: '',
    type: '',
    dateFrom: '',
    dateTo: '',
    search: ''
  })
  const [sortBy, setSortBy] = useState('requestedAt')
  const [sortDir, setSortDir] = useState<'asc' | 'desc'>('desc')

  useEffect(() => {
    fetchReports()
    fetchStatistics()
    const interval = setInterval(() => {
      fetchReports()
      fetchStatistics()
    }, 5000)
    return () => clearInterval(interval)
  }, [])

  const fetchReports = async () => {
    const result = await reportService.getReports({ ...filters, page, pageSize, sortBy, sortDir })
    if (result.success && result.data) {
      setReports(result.data)
      const p = result.pagination
      if (p) {
        setHasMore(p.hasNext)
        setTotalPages(p.totalPages)
        setTotalItems(p.totalItems)
      } else {
        setHasMore(result.data.length >= pageSize)
        setTotalPages(1)
        setTotalItems(result.data.length)
      }
    } else {
      setReports([])
      setHasMore(false)
      setTotalPages(1)
      setTotalItems(0)
    }
    setLoading(false)
  }

  const fetchStatistics = async () => {
    const result = await reportService.getStatistics()
    if (result.success && result.data) setStatistics(result.data)
    else setStatistics(null)
    setStatisticsLoading(false)
  }

  const handleGenerateReport = async (request: GenerateReportRequest) => {
    setGenerating(true)
    const result = await reportService.generateReport(request)
    if (result.success && result.data) {
      setReports(prev => [result.data!, ...prev])
      fetchStatistics()
    }
    setGenerating(false)
  }

  const downloadReport = async (reportId: string, title: string) => {
    const blob = await reportService.downloadReport(reportId)
    if (!blob) return
    const url = window.URL.createObjectURL(blob)
    const a = document.createElement('a')
    a.href = url
    a.download = `${title.replace(/\s/g, '_')}_${new Date().toISOString().split('T')[0]}.pdf`
    document.body.appendChild(a)
    a.click()
    window.URL.revokeObjectURL(url)
    document.body.removeChild(a)
  }

  const deleteReport = async (reportId: string) => {
    const result = await reportService.deleteReport(reportId)
    if (result.success) {
      setReports(prev => prev.filter(r => r.id !== reportId))
      fetchStatistics()
    }
  }

  const handleSortChange = (newSortBy: string, newSortDir: 'asc' | 'desc') => {
    setSortBy(newSortBy)
    setSortDir(newSortDir)
    setPage(1) // Reset to first page when sorting changes
    setLoading(true)
    fetchReports()
  }

  // Server-side filtering now; keep client filtering minimal (identity)
  const filteredReports = reports

  if (showNewGenerator) {
    return (
      <div className="container mx-auto p-6 space-y-6">
        <div className="flex items-center justify-between">
          <div>
            <h1 className="text-3xl font-bold text-sierra-blue">Generate New Report</h1>
            <p className="text-muted-foreground">Create reports for your account</p>
          </div>
          <Button variant="outline" onClick={() => setShowNewGenerator(false)}>← Back to Reports</Button>
        </div>
        <ReportGenerator onReportGenerated={(report) => {
          setReports([report, ...reports])
          setShowNewGenerator(false)
          fetchStatistics()
        }} initialType={initialType} initialParameters={initialParameters} />
      </div>
    )
  }

  if (showPreview && selectedReport) {
    return (
      <div className="container mx-auto p-6 space-y-6">
        <div className="flex items-center justify-between">
          <div>
            <h1 className="text-3xl font-bold text-sierra-blue">Report Preview</h1>
            <p className="text-muted-foreground">{selectedReport.title}</p>
          </div>
          <Button variant="outline" onClick={() => setShowPreview(false)}>← Back to Reports</Button>
        </div>
        <ReportPreview report={selectedReport} onClose={() => setShowPreview(false)} onDownload={downloadReport} />
      </div>
    )
  }

  return (
    <div className="container mx-auto p-6 space-y-6">
      <div className="flex items-center justify-between">
        <div>
          <h1 className="text-3xl font-bold text-sierra-blue">My Reports</h1>
          <p className="text-muted-foreground">Generate and manage your reports</p>
        </div>
        <div className="flex items-center space-x-2">
          <Button onClick={() => setShowNewGenerator(true)} size="sm">
            <Plus className="h-4 w-4 mr-2" /> New Report
          </Button>
          <Button onClick={fetchReports} variant="outline" size="sm">
            <RefreshCw className="h-4 w-4 mr-2" /> Refresh
          </Button>
        </div>
      </div>

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
                  <SelectItem value="TaxSummary">Tax Summary</SelectItem>
                  <SelectItem value="ComplianceStatus">Compliance Status</SelectItem>
                  <SelectItem value="PaymentHistory">Payment History</SelectItem>
                </SelectContent>
              </Select>
            </div>

            <div className="flex items-center space-x-2">
              <Calendar className="h-4 w-4" />
              <Input type="date" value={filters.dateFrom} onChange={(e) => setFilters(prev => ({ ...prev, dateFrom: e.target.value }))} className="w-[150px]" />
              <span className="text-muted-foreground">to</span>
              <Input type="date" value={filters.dateTo} onChange={(e) => setFilters(prev => ({ ...prev, dateTo: e.target.value }))} className="w-[150px]" />
            </div>

            <div className="flex items-center space-x-2">
              <Search className="h-4 w-4" />
              <Input placeholder="Search reports..." value={filters.search} onChange={(e) => setFilters(prev => ({ ...prev, search: e.target.value }))} className="w-[200px]" />
            </div>
          </div>
        </CardContent>
      </Card>

      <Tabs defaultValue="overview" className="space-y-6">
        <TabsList className="grid w-full grid-cols-3 md:grid-cols-4">
          <TabsTrigger value="overview">Overview</TabsTrigger>
          <TabsTrigger value="generate">Generate</TabsTrigger>
          <TabsTrigger value="history">History</TabsTrigger>
          <TabsTrigger value="templates">Templates</TabsTrigger>
        </TabsList>

        <TabsContent value="overview" className="space-y-6">
          {statistics && (
            <ReportStatisticsCard statistics={statistics} loading={statisticsLoading} />
          )}
        </TabsContent>

        <TabsContent value="generate" className="space-y-6">
          <ReportGenerator onReportGenerated={(report) => {
            setReports([report, ...reports])
            fetchStatistics()
          }} initialType={initialType} initialParameters={initialParameters} />
        </TabsContent>

        <TabsContent value="history" className="space-y-6">
          <ReportHistory
            reports={filteredReports}
            loading={loading}
            onDownload={downloadReport}
            onDelete={deleteReport}
            onPreview={(report) => { setSelectedReport(report); setShowPreview(true) }}
            onRefresh={fetchReports}
            page={page}
            pageSize={pageSize}
            hasMore={hasMore}
            totalPages={totalPages}
            totalItems={totalItems}
            onPageChange={(p) => { setPage(p); setLoading(true); fetchReports() }}
            sortBy={sortBy}
            sortDir={sortDir}
            onSortChange={handleSortChange}
          />
        </TabsContent>

        <TabsContent value="templates" className="space-y-6">
          <ReportTemplates onTemplateSelected={(tpl) => {
            // Switch to Generate tab and pre-select type
            setShowNewGenerator(true)
            setInitialType(tpl.reportType)
            setInitialParameters(tpl.parameters || {})
          }} />
        </TabsContent>
      </Tabs>
    </div>
  )
}
