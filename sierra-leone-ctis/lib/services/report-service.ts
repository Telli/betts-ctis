// Report Service for BettsTax Sierra Leone
import { ApiResponse } from '@/lib/types/api'

export interface ReportRequest {
  id: string
  reportType: string
  title: string
  description: string
  status: 'Pending' | 'Processing' | 'Completed' | 'Failed' | 'Cancelled'
  progress: number
  createdAt: string
  completedAt?: string
  downloadUrl?: string
  parameters: Record<string, any>
  fileSize?: number
  estimatedDuration?: number
  errorMessage?: string
  userId: string
  clientId?: string
}

export interface GenerateReportRequest {
  reportType: string
  parameters: {
    clientId?: string
    dateFrom?: string
    dateTo?: string
    includeDetails?: boolean
    format?: string
    [key: string]: any
  }
}

export interface ReportTemplate {
  id: string
  name: string
  description: string
  reportType: string
  parameters: Record<string, any>
  isDefault: boolean
  createdAt: string
  updatedAt: string
}

export interface ReportStatistics {
  totalReports: number
  completedReports: number
  failedReports: number
  processingReports: number
  averageGenerationTime: number
  mostPopularTypes: { type: string; count: number }[]
  recentActivity: ReportRequest[]
}

class ReportService {
  private baseUrl = process.env.NEXT_PUBLIC_API_URL || 'http://localhost:5001'

  // Backend enums (numeric) to UI strings and vice versa
  private reportStatusToString(status: number): ReportRequest['status'] {
    switch (status) {
      case 0: return 'Pending'
      case 1: return 'Processing'
      case 2: return 'Completed'
      case 3: return 'Failed'
      case 4: return 'Cancelled'
      default: return 'Pending'
    }
  }

  private reportTypeToString(type: number): string {
    // See BettsTax.Data/Enums.cs ReportType
    switch (type) {
      case 1: return 'TaxFiling'
      case 2: return 'PaymentHistory'
      case 3: return 'Compliance'
      case 4: return 'ClientActivity'
      case 5: return 'FinancialSummary'
      case 6: return 'ComplianceAnalytics'
      case 7: return 'DocumentSubmission'
      case 8: return 'TaxCalendar'
      case 9: return 'ClientComplianceOverview'
      case 10: return 'Revenue'
      case 11: return 'CaseManagement'
      case 12: return 'EnhancedClientActivity'
      default: return 'Unknown'
    }
  }

  private reportTypeToEnum(reportType: string): number | null {
    const map: Record<string, number> = {
      // Core backend types
      TaxFiling: 1,
      PaymentHistory: 2,
      Compliance: 3,
      ClientActivity: 4,
      FinancialSummary: 5,
      ComplianceAnalytics: 6,
      DocumentSubmission: 7,
      TaxCalendar: 8,
      ClientComplianceOverview: 9,
      Revenue: 10,
      CaseManagement: 11,
      EnhancedClientActivity: 12,
      // UI convenience aliases
      TaxCompliance: 3, // map to Compliance
      KPISummary: 10, // map to Revenue (closest available)
      PenaltyAnalysis: 3, // penalties are part of compliance domain
      AuditTrail: 4, // closest fit is client activity/audit-like
      TaxSummary: 1, // map to TaxFiling summary
      ComplianceStatus: 3,
      ClientPortfolio: 9,
      MonthlyReconciliation: 10
    }
    return map[reportType] ?? null
  }

  private formatToEnum(format?: string): number {
    switch (format) {
      case 'PDF': return 1
      case 'Excel': return 2
      case 'CSV': return 3
      default: return 1
    }
  }

  private buildTitleFromDto(dto: any): string {
    const type = this.reportTypeToString(dto.type)
    const ts = dto.requestedAt ? new Date(dto.requestedAt).toLocaleString() : new Date().toLocaleString()
    return `${type} Report - ${ts}`
  }

  private mapDtoToUi(dto: any): ReportRequest {
    return {
      id: dto.requestId,
      reportType: this.reportTypeToString(dto.type),
      title: this.buildTitleFromDto(dto),
      description: '',
      status: this.reportStatusToString(dto.status),
      progress: dto.status === 1 ? (dto.progress ?? 0) : (dto.status === 2 ? 100 : 0),
      createdAt: dto.requestedAt,
      completedAt: dto.completedAt,
      downloadUrl: dto.downloadUrl,
      parameters: dto.parameters ?? {},
      fileSize: dto.fileSizeBytes,
      estimatedDuration: undefined,
      errorMessage: dto.errorMessage,
      userId: dto.requestedByUserId,
      clientId: dto.parameters?.clientId
    }
  }

  // Get all reports for the current user (uses backend history endpoint)
  async getReports(filters?: {
    status?: string
    type?: string
    clientId?: string
    dateFrom?: string
    dateTo?: string
    page?: number
    pageSize?: number
    search?: string
    sortBy?: string
    sortDir?: 'asc' | 'desc'
  }): Promise<import('../types/api').PaginatedResponse<ReportRequest>> {
    try {
      const page = filters?.page ?? 1
      const pageSize = filters?.pageSize ?? 100
      const params: string[] = [
        `pageSize=${encodeURIComponent(pageSize)}`,
        `pageNumber=${encodeURIComponent(page)}`
      ]

      if (filters?.status) params.push(`status=${encodeURIComponent(filters.status)}`)
      if (filters?.type) {
        // Prefer numeric enum for compatibility; fallback to string
        const typeEnum = this.reportTypeToEnum(filters.type)
        params.push(`type=${encodeURIComponent(typeEnum ?? filters.type)}`)
      }
      if (filters?.dateFrom) params.push(`fromDate=${encodeURIComponent(filters.dateFrom)}`)
      if (filters?.dateTo) params.push(`toDate=${encodeURIComponent(filters.dateTo)}`)
      if (filters?.search) params.push(`search=${encodeURIComponent(filters.search)}`)
      if (filters?.sortBy) params.push(`sortBy=${encodeURIComponent(filters.sortBy)}`)
      if (filters?.sortDir) params.push(`sortDir=${encodeURIComponent(filters.sortDir)}`)

      const url = `${this.baseUrl}/api/Reports/history?${params.join('&')}`

      const response = await fetch(url, {
        method: 'GET',
        headers: {
          'Content-Type': 'application/json',
          'Authorization': `Bearer ${localStorage.getItem('token')}`
        }
      })

      if (!response.ok) {
        throw new Error(`HTTP error! status: ${response.status}`)
      }

  const payload = await response.json()
      const items = Array.isArray(payload?.reports) ? payload.reports : (Array.isArray(payload?.Reports) ? payload.Reports : [])
      const mapped: ReportRequest[] = items.map((dto: any) => this.mapDtoToUi(dto))

      const totalCount = (payload?.totalCount ?? payload?.TotalCount ?? mapped.length) as number
      const pageNumber = (payload?.pageNumber ?? payload?.PageNumber ?? page) as number
      const pageSizeVal = (payload?.pageSize ?? payload?.PageSize ?? pageSize) as number
      const totalPages = Math.max(1, Math.ceil(totalCount / Math.max(1, pageSizeVal)))
      const hasNext = pageNumber < totalPages
      const hasPrevious = pageNumber > 1

      return {
        success: true,
        data: mapped,
        message: 'Reports retrieved successfully',
        pagination: {
          page: pageNumber,
          pageSize: pageSizeVal,
          totalPages,
          totalItems: totalCount,
          hasNext,
          hasPrevious
        }
      }
    } catch (error) {
      console.error('Error fetching reports:', error)
      return {
        success: false,
        error: error instanceof Error ? error.message : 'Unknown error occurred',
        data: [],
        pagination: {
          page: filters?.page ?? 1,
          pageSize: filters?.pageSize ?? 100,
          totalPages: 1,
          totalItems: 0,
          hasNext: false,
          hasPrevious: false
        }
      }
    }
  }

  // Get a specific report by ID
  async getReport(reportId: string): Promise<ApiResponse<ReportRequest>> {
    try {
      const response = await fetch(`${this.baseUrl}/api/Reports/status/${encodeURIComponent(reportId)}`, {
        method: 'GET',
        headers: {
          'Content-Type': 'application/json',
          'Authorization': `Bearer ${localStorage.getItem('token')}`
        }
      })

      if (!response.ok) {
        throw new Error(`HTTP error! status: ${response.status}`)
      }

      const data = await response.json()
      return {
        success: true,
        data: this.mapDtoToUi(data),
        message: 'Report retrieved successfully'
      }
    } catch (error) {
      console.error('Error fetching report:', error)
      return {
        success: false,
        error: error instanceof Error ? error.message : 'Unknown error occurred'
      }
    }
  }

  // Generate a new report
  async generateReport(request: GenerateReportRequest): Promise<ApiResponse<ReportRequest>> {
    try {
      const typeEnum = this.reportTypeToEnum(request.reportType)
      if (typeEnum === null) {
        throw new Error(`Unsupported report type: ${request.reportType}`)
      }

      // Map parameters to backend expectations
      const params: Record<string, any> = { ...request.parameters }
      if (params.dateFrom && !params.fromDate) params.fromDate = params.dateFrom
      if (params.dateTo && !params.toDate) params.toDate = params.dateTo
      delete params.dateFrom
      delete params.dateTo

      const body = {
        type: typeEnum,
        format: this.formatToEnum(params.format),
        parameters: params
      }

      const response = await fetch(`${this.baseUrl}/api/Reports/queue`, {
        method: 'POST',
        headers: {
          'Content-Type': 'application/json',
          'Authorization': `Bearer ${localStorage.getItem('token')}`
        },
        body: JSON.stringify(body)
      })

      if (!response.ok) {
        let errorMessage = `HTTP error! status: ${response.status}`
        try {
          const errorData = await response.json()
          errorMessage = errorData?.message || errorMessage
        } catch {}
        throw new Error(errorMessage)
      }

      const data = await response.json()
      const now = new Date().toISOString()
      const uiReport: ReportRequest = {
        id: data.requestId || data.RequestId,
        reportType: request.reportType,
        title: request.parameters?.title || `${request.reportType} Report - ${new Date().toLocaleString()}`,
        description: request.parameters?.description || '',
        status: 'Pending',
        progress: 0,
        createdAt: now,
        completedAt: undefined,
        downloadUrl: undefined,
        parameters: params,
        fileSize: undefined,
        estimatedDuration: undefined,
        errorMessage: undefined,
        userId: ''
      }
      return {
        success: true,
        data: uiReport,
        message: 'Report generation queued successfully'
      }
    } catch (error) {
      console.error('Error generating report:', error)
      return {
        success: false,
        error: error instanceof Error ? error.message : 'Unknown error occurred'
      }
    }
  }

  // Download a completed report
  async downloadReport(reportId: string): Promise<Blob | null> {
    try {
      const response = await fetch(`${this.baseUrl}/api/Reports/download/${encodeURIComponent(reportId)}`, {
        method: 'GET',
        headers: {
          'Authorization': `Bearer ${localStorage.getItem('token')}`
        }
      })

      if (!response.ok) {
        throw new Error(`HTTP error! status: ${response.status}`)
      }

      return await response.blob()
    } catch (error) {
      console.error('Error downloading report:', error)
      return null
    }
  }

  // Cancel a processing report
  async cancelReport(reportId: string): Promise<ApiResponse<void>> {
    try {
      const response = await fetch(`${this.baseUrl}/api/Reports/cancel/${encodeURIComponent(reportId)}` , {
        method: 'POST',
        headers: {
          'Authorization': `Bearer ${localStorage.getItem('token')}`
        }
      })

      if (!response.ok) {
        let errorMessage = `HTTP error! status: ${response.status}`
        try {
          const errorData = await response.json()
          errorMessage = errorData?.message || errorMessage
        } catch {}
        throw new Error(errorMessage)
      }

      return { success: true, message: 'Report cancelled successfully' }
    } catch (error) {
      console.error('Error cancelling report:', error)
      return {
        success: false,
        error: error instanceof Error ? error.message : 'Unknown error occurred'
      }
    }
  }

  // Delete a report
  async deleteReport(reportId: string): Promise<ApiResponse<void>> {
    try {
      const response = await fetch(`${this.baseUrl}/api/Reports/${encodeURIComponent(reportId)}`, {
        method: 'DELETE',
        headers: {
          'Authorization': `Bearer ${localStorage.getItem('token')}`
        }
      })

      if (!response.ok) {
        throw new Error(`HTTP error! status: ${response.status}`)
      }

      return {
        success: true,
        message: 'Report deleted successfully'
      }
    } catch (error) {
      console.error('Error deleting report:', error)
      return {
        success: false,
        error: error instanceof Error ? error.message : 'Unknown error occurred'
      }
    }
  }

  // Get report templates
  async getTemplates(): Promise<ApiResponse<ReportTemplate[]>> {
    try {
      const response = await fetch(`${this.baseUrl}/api/Reports/templates`, {
        method: 'GET',
        headers: {
          'Authorization': `Bearer ${localStorage.getItem('token')}`
        }
      })

      if (!response.ok) {
        throw new Error(`HTTP error! status: ${response.status}`)
      }

      const data = await response.json()
      return { success: true, data, message: 'Templates retrieved' }
    } catch (error) {
      console.error('Error fetching templates:', error)
      return {
        success: false,
        error: error instanceof Error ? error.message : 'Unknown error occurred',
        data: []
      }
    }
  }

  // Save a report template
  async saveTemplate(template: Omit<ReportTemplate, 'id' | 'createdAt' | 'updatedAt'>): Promise<ApiResponse<ReportTemplate>> {
    try {
      const payload = {
        name: template.name,
        description: template.description,
        reportType: template.reportType,
        parameters: template.parameters,
        isDefault: template.isDefault ?? false
      }

      const response = await fetch(`${this.baseUrl}/api/Reports/templates`, {
        method: 'POST',
        headers: {
          'Content-Type': 'application/json',
          'Authorization': `Bearer ${localStorage.getItem('token')}`
        },
        body: JSON.stringify(payload)
      })

      if (!response.ok) {
        throw new Error(`HTTP error! status: ${response.status}`)
      }

      const data = await response.json()
      return { success: true, data, message: 'Template saved' }
    } catch (error) {
      console.error('Error saving template:', error)
      return {
        success: false,
        error: error instanceof Error ? error.message : 'Unknown error occurred'
      }
    }
  }

  // Update a report template
  async updateTemplate(templateId: string, template: Partial<Omit<ReportTemplate, 'id' | 'createdAt' | 'updatedAt'>>): Promise<ApiResponse<ReportTemplate>> {
    try {
      const payload: any = {}
      if (template.name !== undefined) payload.name = template.name
      if (template.description !== undefined) payload.description = template.description
      if (template.reportType !== undefined) payload.reportType = template.reportType
      if (template.parameters !== undefined) payload.parameters = template.parameters
      if (template.isDefault !== undefined) payload.isDefault = template.isDefault

      const response = await fetch(`${this.baseUrl}/api/Reports/templates/${encodeURIComponent(templateId)}`, {
        method: 'PUT',
        headers: {
          'Content-Type': 'application/json',
          'Authorization': `Bearer ${localStorage.getItem('token')}`
        },
        body: JSON.stringify(payload)
      })

      if (!response.ok) {
        throw new Error(`HTTP error! status: ${response.status}`)
      }

      const data = await response.json()
      return { success: true, data, message: 'Template updated' }
    } catch (error) {
      console.error('Error updating template:', error)
      return {
        success: false,
        error: error instanceof Error ? error.message : 'Unknown error occurred'
      }
    }
  }

  // Delete a report template
  async deleteTemplate(templateId: string): Promise<ApiResponse<void>> {
    try {
      const response = await fetch(`${this.baseUrl}/api/Reports/templates/${encodeURIComponent(templateId)}`, {
        method: 'DELETE',
        headers: {
          'Authorization': `Bearer ${localStorage.getItem('token')}`
        }
      })

      if (!response.ok) {
        let errorMessage = `HTTP error! status: ${response.status}`
        try {
          const err = await response.json()
          errorMessage = err?.message || errorMessage
        } catch {}
        throw new Error(errorMessage)
      }

      return { success: true, message: 'Template deleted' }
    } catch (error) {
      console.error('Error deleting template:', error)
      return {
        success: false,
        error: error instanceof Error ? error.message : 'Unknown error occurred'
      }
    }
  }

  // Get report statistics
  async getStatistics(dateFrom?: string, dateTo?: string): Promise<ApiResponse<ReportStatistics>> {
    try {
      // No dedicated endpoint; derive from history
      const history = await this.getReports({})
      if (!history.success || !history.data) throw new Error(history.error || 'Failed to load reports')
      const reports = history.data

      const totalReports = reports.length
      const completedReports = reports.filter(r => r.status === 'Completed').length
      const failedReports = reports.filter(r => r.status === 'Failed').length
      const processingReports = reports.filter(r => r.status === 'Processing').length
      const completed = reports.filter(r => r.status === 'Completed' && r.completedAt)
      const averageGenerationTime = completed.length
        ? Math.round(completed.reduce((acc, r) => acc + (new Date(r.completedAt!).getTime() - new Date(r.createdAt).getTime()), 0) / completed.length / 1000)
        : 0
      const typeCounts: Record<string, number> = {}
      for (const r of reports) typeCounts[r.reportType] = (typeCounts[r.reportType] || 0) + 1
      const mostPopularTypes = Object.entries(typeCounts).map(([type, count]) => ({ type, count })).sort((a,b) => b.count - a.count).slice(0, 5)
      const recentActivity = reports.slice(0, 10)

      const stats: ReportStatistics = {
        totalReports,
        completedReports,
        failedReports,
        processingReports,
        averageGenerationTime,
        mostPopularTypes,
        recentActivity
      }

      return { success: true, data: stats, message: 'Statistics derived successfully' }
    } catch (error) {
      console.error('Error fetching statistics:', error)
      return {
        success: false,
        error: error instanceof Error ? error.message : 'Unknown error occurred'
      }
    }
  }

  // Preview a report template
  async previewTemplate(templateId: string, parameters: Record<string, any>, options?: { format?: 'PDF' | 'Excel' | 'CSV' }): Promise<ApiResponse<{ url: string; contentType: string }>> {
    try {
      const query = options?.format ? `?format=${encodeURIComponent(options.format)}` : ''
      const response = await fetch(`${this.baseUrl}/api/Reports/templates/${encodeURIComponent(templateId)}/preview${query}` , {
        method: 'POST',
        headers: {
          'Content-Type': 'application/json',
          'Authorization': `Bearer ${localStorage.getItem('token')}`
        },
        body: JSON.stringify(parameters ?? {})
      })

      if (!response.ok) {
        let errorMessage = `HTTP error! status: ${response.status}`
        try {
          const err = await response.json()
          errorMessage = err?.message || errorMessage
        } catch {}
        throw new Error(errorMessage)
      }

      // The API returns a file stream; to preview easily, convert to a Blob URL
      const blob = await response.blob()
      const url = URL.createObjectURL(blob)
      const contentType = blob.type || response.headers.get('Content-Type') || 'application/octet-stream'
      return { success: true, data: { url, contentType }, message: 'Preview generated' }
    } catch (error) {
      console.error('Error generating preview:', error)
      return {
        success: false,
        error: error instanceof Error ? error.message : 'Unknown error occurred'
      }
    }
  }

  // Get available report types with their configurations
  getReportTypes() {
    return [
      {
        value: 'TaxSummary',
        label: 'Tax Summary Report',
        description: 'Comprehensive tax overview for clients with Sierra Leone Finance Act 2025 compliance',
        category: 'Tax',
        parameters: [
          { name: 'clientId', type: 'select', label: 'Client', required: false },
          { name: 'taxYear', type: 'number', label: 'Tax Year', required: true },
          { name: 'includePayments', type: 'boolean', label: 'Include Payment Details', default: true },
          { name: 'includePenalties', type: 'boolean', label: 'Include Penalty Calculations', default: true }
        ],
        estimatedDuration: 120
      },
      {
        value: 'ComplianceStatus',
        label: 'Compliance Status Report',
        description: 'Current compliance status and upcoming deadlines for NRA requirements',
        category: 'Compliance',
        parameters: [
          { name: 'clientId', type: 'select', label: 'Client', required: false },
          { name: 'includeHistory', type: 'boolean', label: 'Include Historical Data', default: false },
          { name: 'riskAssessment', type: 'boolean', label: 'Include Risk Assessment', default: true }
        ],
        estimatedDuration: 90
      },
      {
        value: 'PaymentHistory',
        label: 'Payment History Report',
        description: 'Complete payment transaction history including mobile money transactions',
        category: 'Financial',
        parameters: [
          { name: 'clientId', type: 'select', label: 'Client', required: false },
          { name: 'paymentMethod', type: 'select', label: 'Payment Method', options: ['All', 'Orange Money', 'Africell Money', 'Bank Transfer'] },
          { name: 'includeFailures', type: 'boolean', label: 'Include Failed Transactions', default: false }
        ],
        estimatedDuration: 180
      },
      {
        value: 'ClientPortfolio',
        label: 'Client Portfolio Report',
        description: 'Detailed client information and tax portfolio statistics',
        category: 'Client Management',
        parameters: [
          { name: 'includeDormant', type: 'boolean', label: 'Include Dormant Clients', default: false },
          { name: 'riskLevel', type: 'select', label: 'Risk Level', options: ['All', 'Low', 'Medium', 'High', 'Critical'] },
          { name: 'businessSize', type: 'select', label: 'Business Size', options: ['All', 'Large', 'Medium', 'Small', 'Micro'] }
        ],
        estimatedDuration: 240
      },
      {
        value: 'MonthlyReconciliation',
        label: 'Monthly Reconciliation',
        description: 'Monthly financial reconciliation report for accounting purposes',
        category: 'Financial',
        parameters: [
          { name: 'month', type: 'month', label: 'Month', required: true },
          { name: 'includeAdjustments', type: 'boolean', label: 'Include Adjustments', default: true },
          { name: 'detailLevel', type: 'select', label: 'Detail Level', options: ['Summary', 'Detailed', 'Complete'] }
        ],
        estimatedDuration: 300
      },
      {
        value: 'AuditTrail',
        label: 'Audit Trail Report',
        description: 'System audit trail for compliance and security monitoring',
        category: 'Security',
        parameters: [
          { name: 'userId', type: 'select', label: 'User', required: false },
          { name: 'action', type: 'select', label: 'Action Type', options: ['All', 'Login', 'Data Modification', 'File Access', 'System Changes'] },
          { name: 'riskLevel', type: 'select', label: 'Risk Level', options: ['All', 'Low', 'Medium', 'High', 'Critical'] }
        ],
        estimatedDuration: 150
      }
    ]
  }

  // Utility function to format file size
  static formatFileSize(bytes: number): string {
    const sizes = ['Bytes', 'KB', 'MB', 'GB']
    if (bytes === 0) return '0 Bytes'
    const i = Math.floor(Math.log(bytes) / Math.log(1024))
    return Math.round(bytes / Math.pow(1024, i) * 100) / 100 + ' ' + sizes[i]
  }

  // Utility function to get status color
  static getStatusColor(status: ReportRequest['status']): string {
    switch (status) {
      case 'Completed': return 'text-green-600 bg-green-100'
      case 'Processing': return 'text-blue-600 bg-blue-100'
      case 'Failed': return 'text-red-600 bg-red-100'
      case 'Cancelled': return 'text-gray-600 bg-gray-100'
      default: return 'text-yellow-600 bg-yellow-100'
    }
  }

  // Utility function to estimate completion time
  static estimateCompletionTime(report: ReportRequest): string {
    if (report.status !== 'Processing') return 'N/A'
    
    const elapsed = Date.now() - new Date(report.createdAt).getTime()
    const totalEstimated = (report.estimatedDuration || 120) * 1000 // Convert to ms
    const remaining = Math.max(0, totalEstimated - elapsed)
    
    const minutes = Math.floor(remaining / (1000 * 60))
    const seconds = Math.floor((remaining % (1000 * 60)) / 1000)
    
    if (minutes > 0) {
      return `~${minutes}m ${seconds}s remaining`
    } else {
      return `~${seconds}s remaining`
    }
  }
}

export const reportService = new ReportService()
export default reportService