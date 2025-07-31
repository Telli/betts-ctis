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
  private baseUrl = process.env.NEXT_PUBLIC_API_URL || 'http://localhost:5000'

  // Get all reports for the current user
  async getReports(filters?: {
    status?: string
    type?: string
    clientId?: string
    dateFrom?: string
    dateTo?: string
    page?: number
    pageSize?: number
  }): Promise<ApiResponse<ReportRequest[]>> {
    try {
      const params = new URLSearchParams()
      
      if (filters) {
        Object.entries(filters).forEach(([key, value]) => {
          if (value) params.append(key, value.toString())
        })
      }

      const url = `${this.baseUrl}/api/reports${params.toString() ? `?${params.toString()}` : ''}`
      
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

      const data = await response.json()
      return {
        success: true,
        data: data || [],
        message: 'Reports retrieved successfully'
      }
    } catch (error) {
      console.error('Error fetching reports:', error)
      return {
        success: false,
        error: error instanceof Error ? error.message : 'Unknown error occurred',
        data: []
      }
    }
  }

  // Get a specific report by ID
  async getReport(reportId: string): Promise<ApiResponse<ReportRequest>> {
    try {
      const response = await fetch(`${this.baseUrl}/api/reports/${reportId}`, {
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
        data: data,
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
      const response = await fetch(`${this.baseUrl}/api/reports/generate`, {
        method: 'POST',
        headers: {
          'Content-Type': 'application/json',
          'Authorization': `Bearer ${localStorage.getItem('token')}`
        },
        body: JSON.stringify(request)
      })

      if (!response.ok) {
        const errorData = await response.json()
        throw new Error(errorData.message || `HTTP error! status: ${response.status}`)
      }

      const data = await response.json()
      return {
        success: true,
        data: data,
        message: 'Report generation started successfully'
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
      const response = await fetch(`${this.baseUrl}/api/reports/${reportId}/download`, {
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
      const response = await fetch(`${this.baseUrl}/api/reports/${reportId}/cancel`, {
        method: 'POST',
        headers: {
          'Content-Type': 'application/json',
          'Authorization': `Bearer ${localStorage.getItem('token')}`
        }
      })

      if (!response.ok) {
        throw new Error(`HTTP error! status: ${response.status}`)
      }

      return {
        success: true,
        message: 'Report cancelled successfully'
      }
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
      const response = await fetch(`${this.baseUrl}/api/reports/${reportId}`, {
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
      const response = await fetch(`${this.baseUrl}/api/reports/templates`, {
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
        data: data,
        message: 'Templates retrieved successfully'
      }
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
      const response = await fetch(`${this.baseUrl}/api/reports/templates`, {
        method: 'POST',
        headers: {
          'Content-Type': 'application/json',
          'Authorization': `Bearer ${localStorage.getItem('token')}`
        },
        body: JSON.stringify(template)
      })

      if (!response.ok) {
        throw new Error(`HTTP error! status: ${response.status}`)
      }

      const data = await response.json()
      return {
        success: true,
        data: data,
        message: 'Template saved successfully'
      }
    } catch (error) {
      console.error('Error saving template:', error)
      return {
        success: false,
        error: error instanceof Error ? error.message : 'Unknown error occurred'
      }
    }
  }

  // Get report statistics
  async getStatistics(dateFrom?: string, dateTo?: string): Promise<ApiResponse<ReportStatistics>> {
    try {
      const params = new URLSearchParams()
      if (dateFrom) params.append('dateFrom', dateFrom)
      if (dateTo) params.append('dateTo', dateTo)

      const url = `${this.baseUrl}/api/reports/statistics${params.toString() ? `?${params.toString()}` : ''}`
      
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

      const data = await response.json()
      return {
        success: true,
        data: data || null,
        message: 'Statistics retrieved successfully'
      }
    } catch (error) {
      console.error('Error fetching statistics:', error)
      return {
        success: false,
        error: error instanceof Error ? error.message : 'Unknown error occurred'
      }
    }
  }

  // Preview a report template
  async previewTemplate(templateId: string, parameters: Record<string, any>): Promise<ApiResponse<string>> {
    try {
      const response = await fetch(`${this.baseUrl}/api/reports/templates/${templateId}/preview`, {
        method: 'POST',
        headers: {
          'Content-Type': 'application/json',
          'Authorization': `Bearer ${localStorage.getItem('token')}`
        },
        body: JSON.stringify({ parameters })
      })

      if (!response.ok) {
        throw new Error(`HTTP error! status: ${response.status}`)
      }

      const data = await response.json()
      return {
        success: true,
        data: data.previewUrl,
        message: 'Preview generated successfully'
      }
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