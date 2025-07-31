import { useState } from 'react'
import { Card, CardContent, CardDescription, CardHeader, CardTitle } from '@/components/ui/card'
import { Button } from '@/components/ui/button'
import { Select, SelectContent, SelectItem, SelectTrigger, SelectValue } from '@/components/ui/select'
import { Input } from '@/components/ui/input'
import { Label } from '@/components/ui/label'
import { Checkbox } from '@/components/ui/checkbox'
import { Separator } from '@/components/ui/separator'
import { Badge } from '@/components/ui/badge'
import { 
  FileText, 
  FileSpreadsheet, 
  AlertCircle, 
  Clock, 
  Plus, 
  RefreshCw,
  Info
} from 'lucide-react'
import { GenerateReportRequest } from '@/lib/services/report-service'

interface ReportType {
  value: string
  label: string
  description: string
  icon: any
  category: string
  estimatedDuration: number
  parameters: Array<{
    name: string
    type: string
    label: string
    required: boolean
    default?: any
    options?: string[]
  }>
}

interface ReportGenerationFormProps {
  onGenerate: (request: GenerateReportRequest) => Promise<void>
  loading?: boolean
}

const reportTypes: ReportType[] = [
  {
    value: 'TaxSummary',
    label: 'Tax Summary Report',
    description: 'Comprehensive tax overview for clients with Sierra Leone Finance Act 2025 compliance',
    icon: FileText,
    category: 'Tax',
    estimatedDuration: 120,
    parameters: [
      { name: 'taxYear', type: 'number', label: 'Tax Year', required: true, default: new Date().getFullYear() },
      { name: 'includePayments', type: 'boolean', label: 'Include Payment Details', required: false, default: true },
      { name: 'includePenalties', type: 'boolean', label: 'Include Penalty Calculations', required: false, default: true }
    ]
  },
  {
    value: 'ComplianceStatus',
    label: 'Compliance Status Report',
    description: 'Current compliance status and upcoming deadlines for NRA requirements',
    icon: AlertCircle,
    category: 'Compliance',
    estimatedDuration: 90,
    parameters: [
      { name: 'includeHistory', type: 'boolean', label: 'Include Historical Data', required: false, default: false },
      { name: 'riskAssessment', type: 'boolean', label: 'Include Risk Assessment', required: false, default: true }
    ]
  },
  {
    value: 'PaymentHistory',
    label: 'Payment History Report',
    description: 'Complete payment transaction history including mobile money transactions',
    icon: FileSpreadsheet,
    category: 'Financial',
    estimatedDuration: 180,
    parameters: [
      { 
        name: 'paymentMethod', 
        type: 'select', 
        label: 'Payment Method', 
        required: false, 
        options: ['All', 'Orange Money', 'Africell Money', 'Bank Transfer'],
        default: 'All'
      },
      { name: 'includeFailures', type: 'boolean', label: 'Include Failed Transactions', required: false, default: false }
    ]
  },
  {
    value: 'ClientPortfolio',
    label: 'Client Portfolio Report',
    description: 'Detailed client information and tax portfolio statistics',
    icon: FileText,
    category: 'Client Management',
    estimatedDuration: 240,
    parameters: [
      { name: 'includeDormant', type: 'boolean', label: 'Include Dormant Clients', required: false, default: false },
      { 
        name: 'riskLevel', 
        type: 'select', 
        label: 'Risk Level', 
        required: false, 
        options: ['All', 'Low', 'Medium', 'High', 'Critical'],
        default: 'All'
      },
      { 
        name: 'businessSize', 
        type: 'select', 
        label: 'Business Size', 
        required: false, 
        options: ['All', 'Large', 'Medium', 'Small', 'Micro'],
        default: 'All'
      }
    ]
  },
  {
    value: 'MonthlyReconciliation',
    label: 'Monthly Reconciliation',
    description: 'Monthly financial reconciliation report for accounting purposes',
    icon: FileSpreadsheet,
    category: 'Financial',
    estimatedDuration: 300,
    parameters: [
      { name: 'month', type: 'month', label: 'Month', required: true },
      { name: 'includeAdjustments', type: 'boolean', label: 'Include Adjustments', required: false, default: true },
      { 
        name: 'detailLevel', 
        type: 'select', 
        label: 'Detail Level', 
        required: false, 
        options: ['Summary', 'Detailed', 'Complete'],
        default: 'Detailed'
      }
    ]
  },
  {
    value: 'AuditTrail',
    label: 'Audit Trail Report',
    description: 'System audit trail for compliance and security monitoring',
    icon: FileText,
    category: 'Security',
    estimatedDuration: 150,
    parameters: [
      { 
        name: 'action', 
        type: 'select', 
        label: 'Action Type', 
        required: false, 
        options: ['All', 'Login', 'Data Modification', 'File Access', 'System Changes'],
        default: 'All'
      },
      { 
        name: 'riskLevel', 
        type: 'select', 
        label: 'Risk Level', 
        required: false, 
        options: ['All', 'Low', 'Medium', 'High', 'Critical'],
        default: 'All'
      }
    ]
  }
]

const clients = [
  { id: '', name: 'All Clients' },
  { id: 'client1', name: 'Sierra Leone Breweries Ltd' },
  { id: 'client2', name: 'Orange Sierra Leone' },
  { id: 'client3', name: 'Standard Chartered Bank SL' },
  { id: 'client4', name: 'Africell Sierra Leone' },
  { id: 'client5', name: 'Rokel Commercial Bank' }
]

export function ReportGenerationForm({ onGenerate, loading = false }: ReportGenerationFormProps) {
  const [selectedReportType, setSelectedReportType] = useState('')
  const [parameters, setParameters] = useState<Record<string, any>>({
    clientId: '',
    dateFrom: '',
    dateTo: '',
    includeDetails: true,
    format: 'PDF'
  })

  const selectedType = reportTypes.find(type => type.value === selectedReportType)

  const handleReportTypeChange = (value: string) => {
    setSelectedReportType(value)
    const reportType = reportTypes.find(type => type.value === value)
    
    if (reportType) {
      // Initialize parameters with defaults
      const newParams = { ...parameters }
      reportType.parameters.forEach(param => {
        if (param.default !== undefined) {
          newParams[param.name] = param.default
        }
      })
      setParameters(newParams)
    }
  }

  const handleParameterChange = (name: string, value: any) => {
    setParameters(prev => ({
      ...prev,
      [name]: value
    }))
  }

  const handleGenerate = async () => {
    if (!selectedReportType) return

    const request: GenerateReportRequest = {
      reportType: selectedReportType,
      parameters: {
        ...parameters,
        // Include type-specific parameters
        ...selectedType?.parameters.reduce((acc, param) => {
          if (parameters[param.name] !== undefined) {
            acc[param.name] = parameters[param.name]
          }
          return acc
        }, {} as Record<string, any>)
      }
    }

    await onGenerate(request)
    
    // Reset form
    setSelectedReportType('')
    setParameters({
      clientId: '',
      dateFrom: '',
      dateTo: '',
      includeDetails: true,
      format: 'PDF'
    })
  }

  const isFormValid = () => {
    if (!selectedReportType) return false
    
    if (selectedType) {
      return selectedType.parameters.every(param => {
        if (!param.required) return true
        return parameters[param.name] !== undefined && parameters[param.name] !== ''
      })
    }
    
    return false
  }

  const renderParameterInput = (param: any) => {
    switch (param.type) {
      case 'select':
        return (
          <Select
            value={parameters[param.name] || param.default || ''}
            onValueChange={(value) => handleParameterChange(param.name, value)}
          >
            <SelectTrigger>
              <SelectValue placeholder={`Select ${param.label.toLowerCase()}`} />
            </SelectTrigger>
            <SelectContent>
              {param.options?.map((option: string) => (
                <SelectItem key={option} value={option}>
                  {option}
                </SelectItem>
              ))}
            </SelectContent>
          </Select>
        )
      
      case 'boolean':
        return (
          <div className="flex items-center space-x-2">
            <Checkbox
              id={param.name}
              checked={parameters[param.name] ?? param.default ?? false}
              onCheckedChange={(checked) => handleParameterChange(param.name, checked)}
            />
            <Label htmlFor={param.name} className="text-sm">
              {param.label}
            </Label>
          </div>
        )
      
      case 'number':
        return (
          <Input
            type="number"
            value={parameters[param.name] || param.default || ''}
            onChange={(e) => handleParameterChange(param.name, parseInt(e.target.value) || '')}
            placeholder={`Enter ${param.label.toLowerCase()}`}
          />
        )
      
      case 'month':
        return (
          <Input
            type="month"
            value={parameters[param.name] || ''}
            onChange={(e) => handleParameterChange(param.name, e.target.value)}
          />
        )
      
      default:
        return (
          <Input
            value={parameters[param.name] || ''}
            onChange={(e) => handleParameterChange(param.name, e.target.value)}
            placeholder={`Enter ${param.label.toLowerCase()}`}
          />
        )
    }
  }

  return (
    <Card>
      <CardHeader>
        <CardTitle className="flex items-center space-x-2">
          <Plus className="h-5 w-5" />
          <span>Generate New Report</span>
        </CardTitle>
        <CardDescription>
          Create comprehensive reports for clients and Sierra Leone tax compliance requirements
        </CardDescription>
      </CardHeader>
      <CardContent className="space-y-6">
        <div className="grid grid-cols-1 lg:grid-cols-2 gap-6">
          {/* Report Type Selection */}
          <div className="space-y-4">
            <div>
              <Label htmlFor="reportType">Report Type *</Label>
              <Select value={selectedReportType} onValueChange={handleReportTypeChange}>
                <SelectTrigger>
                  <SelectValue placeholder="Select a report type" />
                </SelectTrigger>
                <SelectContent>
                  {reportTypes.map(type => {
                    const Icon = type.icon
                    return (
                      <SelectItem key={type.value} value={type.value}>
                        <div className="flex items-center space-x-2">
                          <Icon className="h-4 w-4" />
                          <span>{type.label}</span>
                          <Badge variant="outline" className="ml-2 text-xs">
                            {type.category}
                          </Badge>
                        </div>
                      </SelectItem>
                    )
                  })}
                </SelectContent>
              </Select>
            </div>

            {/* Report Type Information */}
            {selectedType && (
              <div className="p-4 bg-sierra-blue/5 rounded-lg border border-sierra-blue/20">
                <div className="flex items-start space-x-3">
                  <selectedType.icon className="h-5 w-5 text-sierra-blue mt-0.5 flex-shrink-0" />
                  <div>
                    <h4 className="font-medium text-sierra-blue">{selectedType.label}</h4>
                    <p className="text-sm text-muted-foreground mt-1">
                      {selectedType.description}
                    </p>
                    <div className="flex items-center space-x-4 mt-2">
                      <div className="flex items-center space-x-1 text-xs text-muted-foreground">
                        <Clock className="h-3 w-3" />
                        <span>~{Math.floor(selectedType.estimatedDuration / 60)}m {selectedType.estimatedDuration % 60}s</span>
                      </div>
                      <Badge variant="outline" className="text-xs">
                        {selectedType.category}
                      </Badge>
                    </div>
                  </div>
                </div>
              </div>
            )}

            {/* Common Parameters */}
            <div className="space-y-4">
              <div>
                <Label htmlFor="clientId">Client (Optional)</Label>
                <Select 
                  value={parameters.clientId} 
                  onValueChange={(value) => handleParameterChange('clientId', value)}
                >
                  <SelectTrigger>
                    <SelectValue placeholder="All clients" />
                  </SelectTrigger>
                  <SelectContent>
                    {clients.map(client => (
                      <SelectItem key={client.id} value={client.id}>
                        {client.name}
                      </SelectItem>
                    ))}
                  </SelectContent>
                </Select>
              </div>

              <div className="grid grid-cols-2 gap-4">
                <div>
                  <Label htmlFor="dateFrom">Date From</Label>
                  <Input
                    id="dateFrom"
                    type="date"
                    value={parameters.dateFrom}
                    onChange={(e) => handleParameterChange('dateFrom', e.target.value)}
                  />
                </div>
                <div>
                  <Label htmlFor="dateTo">Date To</Label>
                  <Input
                    id="dateTo"
                    type="date"
                    value={parameters.dateTo}
                    onChange={(e) => handleParameterChange('dateTo', e.target.value)}
                  />
                </div>
              </div>
            </div>
          </div>

          {/* Type-Specific Parameters */}
          <div className="space-y-4">
            <div>
              <Label htmlFor="format">Output Format</Label>
              <Select 
                value={parameters.format} 
                onValueChange={(value) => handleParameterChange('format', value)}
              >
                <SelectTrigger>
                  <SelectValue />
                </SelectTrigger>
                <SelectContent>
                  <SelectItem value="PDF">
                    <div className="flex items-center space-x-2">
                      <FileText className="h-4 w-4" />
                      <span>PDF Document</span>
                    </div>
                  </SelectItem>
                  <SelectItem value="Excel">
                    <div className="flex items-center space-x-2">
                      <FileSpreadsheet className="h-4 w-4" />
                      <span>Excel Spreadsheet</span>
                    </div>
                  </SelectItem>
                  <SelectItem value="CSV">
                    <div className="flex items-center space-x-2">
                      <FileSpreadsheet className="h-4 w-4" />
                      <span>CSV Data</span>
                    </div>
                  </SelectItem>
                </SelectContent>
              </Select>
            </div>

            <div className="flex items-center space-x-2">
              <Checkbox
                id="includeDetails"
                checked={parameters.includeDetails}
                onCheckedChange={(checked) => handleParameterChange('includeDetails', checked)}
              />
              <Label htmlFor="includeDetails" className="text-sm">
                Include detailed breakdowns
              </Label>
            </div>

            {/* Type-Specific Parameters */}
            {selectedType && selectedType.parameters.length > 0 && (
              <>
                <Separator />
                <div className="space-y-4">
                  <div className="flex items-center space-x-2">
                    <Info className="h-4 w-4 text-sierra-blue" />
                    <h4 className="font-medium text-sierra-blue">Report-Specific Options</h4>
                  </div>
                  
                  {selectedType.parameters.map(param => (
                    <div key={param.name}>
                      {param.type !== 'boolean' && (
                        <Label htmlFor={param.name}>
                          {param.label} {param.required && <span className="text-red-500">*</span>}
                        </Label>
                      )}
                      {renderParameterInput(param)}
                    </div>
                  ))}
                </div>
              </>
            )}
          </div>
        </div>

        <Separator />

        <div className="flex justify-end">
          <Button 
            onClick={handleGenerate} 
            disabled={!isFormValid() || loading}
            className="bg-sierra-blue hover:bg-sierra-blue/90"
          >
            {loading ? (
              <>
                <RefreshCw className="h-4 w-4 mr-2 animate-spin" />
                Generating...
              </>
            ) : (
              <>
                <FileText className="h-4 w-4 mr-2" />
                Generate Report
              </>
            )}
          </Button>
        </div>
      </CardContent>
    </Card>
  )
}