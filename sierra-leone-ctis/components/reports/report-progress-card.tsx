import { Card, CardContent, CardHeader, CardTitle } from '@/components/ui/card'
import { Progress } from '@/components/ui/progress'
import { Badge } from '@/components/ui/badge'
import { Button } from '@/components/ui/button'
import { 
  Clock, 
  CheckCircle, 
  XCircle, 
  AlertCircle, 
  RefreshCw, 
  Download, 
  Trash2,
  Eye,
  FileText
} from 'lucide-react'
import { format } from 'date-fns'
import { ReportRequest } from '@/lib/services/report-service'

interface ReportProgressCardProps {
  report: ReportRequest
  onDownload?: (reportId: string, title: string) => void
  onCancel?: (reportId: string) => void
  onDelete?: (reportId: string) => void
  onView?: (reportId: string) => void
}

const getStatusIcon = (status: ReportRequest['status']) => {
  switch (status) {
    case 'Completed': return CheckCircle
    case 'Processing': return RefreshCw
    case 'Failed': return XCircle
    case 'Cancelled': return XCircle
    default: return Clock
  }
}

const getStatusColor = (status: ReportRequest['status']) => {
  switch (status) {
    case 'Completed': return 'bg-green-100 text-green-800 border-green-200'
    case 'Processing': return 'bg-blue-100 text-blue-800 border-blue-200'
    case 'Failed': return 'bg-red-100 text-red-800 border-red-200'
    case 'Cancelled': return 'bg-gray-100 text-gray-800 border-gray-200'
    default: return 'bg-yellow-100 text-yellow-800 border-yellow-200'
  }
}

const formatFileSize = (bytes?: number) => {
  if (!bytes) return 'N/A'
  const sizes = ['Bytes', 'KB', 'MB', 'GB']
  const i = Math.floor(Math.log(bytes) / Math.log(1024))
  return Math.round(bytes / Math.pow(1024, i) * 100) / 100 + ' ' + sizes[i]
}

const estimateTimeRemaining = (report: ReportRequest): string => {
  if (report.status !== 'Processing' || !report.estimatedDuration) return ''
  
  const elapsed = Date.now() - new Date(report.createdAt).getTime()
  const totalEstimated = report.estimatedDuration * 1000
  const remaining = Math.max(0, totalEstimated - elapsed)
  
  const minutes = Math.floor(remaining / (1000 * 60))
  const seconds = Math.floor((remaining % (1000 * 60)) / 1000)
  
  if (minutes > 0) {
    return `~${minutes}m ${seconds}s remaining`
  } else if (seconds > 0) {
    return `~${seconds}s remaining`
  } else {
    return 'Almost done...'
  }
}

export function ReportProgressCard({
  report,
  onDownload,
  onCancel,
  onDelete,
  onView
}: ReportProgressCardProps) {
  const StatusIcon = getStatusIcon(report.status)
  const isProcessing = report.status === 'Processing'
  const isCompleted = report.status === 'Completed'
  const isFailed = report.status === 'Failed'

  return (
    <Card className="hover:shadow-md transition-shadow">
      <CardHeader className="pb-3">
        <div className="flex items-start justify-between">
          <div className="flex items-start space-x-3">
            <div className="flex items-center space-x-2">
              <StatusIcon 
                className={`h-5 w-5 ${isProcessing ? 'animate-spin text-blue-600' : 
                  isCompleted ? 'text-green-600' : 
                  isFailed ? 'text-red-600' : 'text-yellow-600'}`}
              />
              <Badge 
                variant="outline" 
                className={getStatusColor(report.status)}
              >
                {report.status}
              </Badge>
            </div>
          </div>
          
          <div className="flex items-center space-x-2">
            {isCompleted && report.downloadUrl && onDownload && (
              <Button
                size="sm"
                variant="outline"
                onClick={() => onDownload(report.id, report.title)}
                className="h-8"
              >
                <Download className="h-4 w-4 mr-1" />
                Download
              </Button>
            )}
            
            {isProcessing && onCancel && (
              <Button
                size="sm"
                variant="outline"
                onClick={() => onCancel(report.id)}
                className="h-8"
              >
                <XCircle className="h-4 w-4 mr-1" />
                Cancel
              </Button>
            )}
            
            {onView && (
              <Button
                size="sm"
                variant="ghost"
                onClick={() => onView(report.id)}
                className="h-8"
              >
                <Eye className="h-4 w-4" />
              </Button>
            )}
            
            {onDelete && (
              <Button
                size="sm"
                variant="ghost"
                onClick={() => onDelete(report.id)}
                className="h-8 text-red-600 hover:text-red-700 hover:bg-red-50"
              >
                <Trash2 className="h-4 w-4" />
              </Button>
            )}
          </div>
        </div>
        
        <div>
          <CardTitle className="text-lg flex items-center space-x-2">
            <FileText className="h-5 w-5 text-sierra-blue" />
            <span>{report.title}</span>
          </CardTitle>
          {report.description && (
            <p className="text-sm text-muted-foreground mt-1">
              {report.description}
            </p>
          )}
        </div>
      </CardHeader>
      
      <CardContent className="space-y-4">
        {/* Progress Bar for Processing Reports */}
        {isProcessing && (
          <div className="space-y-2">
            <div className="flex justify-between items-center text-sm">
              <span className="text-muted-foreground">Progress</span>
              <div className="flex items-center space-x-2">
                <span className="font-medium">{report.progress}%</span>
                <span className="text-muted-foreground">
                  {estimateTimeRemaining(report)}
                </span>
              </div>
            </div>
            <Progress 
              value={report.progress} 
              className="w-full h-2"
            />
          </div>
        )}
        
        {/* Error Message for Failed Reports */}
        {isFailed && report.errorMessage && (
          <div className="p-3 bg-red-50 border border-red-200 rounded-md">
            <div className="flex items-start space-x-2">
              <AlertCircle className="h-4 w-4 text-red-600 mt-0.5 flex-shrink-0" />
              <div>
                <h4 className="text-sm font-medium text-red-800">Error Details</h4>
                <p className="text-sm text-red-700 mt-1">{report.errorMessage}</p>
              </div>
            </div>
          </div>
        )}
        
        {/* Report Metadata */}
        <div className="grid grid-cols-2 gap-4 text-sm">
          <div>
            <span className="text-muted-foreground">Created:</span>
            <p className="font-medium">
              {format(new Date(report.createdAt), 'MMM dd, yyyy')}
            </p>
            <p className="text-xs text-muted-foreground">
              {format(new Date(report.createdAt), 'HH:mm:ss')}
            </p>
          </div>
          
          {report.completedAt && (
            <div>
              <span className="text-muted-foreground">Completed:</span>
              <p className="font-medium">
                {format(new Date(report.completedAt), 'MMM dd, yyyy')}
              </p>
              <p className="text-xs text-muted-foreground">
                {format(new Date(report.completedAt), 'HH:mm:ss')}
              </p>
            </div>
          )}
          
          {report.fileSize && (
            <div>
              <span className="text-muted-foreground">File Size:</span>
              <p className="font-medium">{formatFileSize(report.fileSize)}</p>
            </div>
          )}
          
          <div>
            <span className="text-muted-foreground">Type:</span>
            <p className="font-medium">{report.reportType}</p>
          </div>
        </div>
        
        {/* Report Parameters */}
        {report.parameters && Object.keys(report.parameters).length > 0 && (
          <div>
            <h4 className="text-sm font-medium text-muted-foreground mb-2">Parameters</h4>
            <div className="space-y-1">
              {Object.entries(report.parameters).map(([key, value]) => (
                <div key={key} className="flex justify-between text-sm">
                  <span className="text-muted-foreground capitalize">
                    {key.replace(/([A-Z])/g, ' $1').trim()}:
                  </span>
                  <span className="font-medium">
                    {typeof value === 'boolean' ? (value ? 'Yes' : 'No') : String(value) || 'N/A'}
                  </span>
                </div>
              ))}
            </div>
          </div>
        )}
        
        {/* Processing Time Indicator */}
        {isCompleted && report.createdAt && report.completedAt && (
          <div className="text-xs text-muted-foreground">
            Generated in {Math.round((new Date(report.completedAt).getTime() - new Date(report.createdAt).getTime()) / 1000)}s
          </div>
        )}
      </CardContent>
    </Card>
  )
}