import { Card, CardContent, CardDescription, CardHeader, CardTitle } from '@/components/ui/card'
import { Progress } from '@/components/ui/progress'
import { Badge } from '@/components/ui/badge'
import { 
  TrendingUp, 
  TrendingDown, 
  FileText, 
  CheckCircle, 
  XCircle, 
  Clock, 
  BarChart3,
  Activity,
  Calendar
} from 'lucide-react'
import { ReportStatistics } from '@/lib/services/report-service'

interface ReportStatisticsCardProps {
  statistics: ReportStatistics
  loading?: boolean
}

export function ReportStatisticsCard({ statistics, loading = false }: ReportStatisticsCardProps) {
  if (loading) {
    return (
      <div className="grid grid-cols-1 md:grid-cols-2 lg:grid-cols-4 gap-4">
        {[...Array(4)].map((_, i) => (
          <Card key={i}>
            <CardHeader className="pb-2">
              <div className="animate-pulse">
                <div className="h-4 bg-gray-200 rounded w-3/4 mb-2" />
                <div className="h-8 bg-gray-200 rounded w-1/2" />
              </div>
            </CardHeader>
          </Card>
        ))}
      </div>
    )
  }

  const completionRate = statistics.totalReports > 0 
    ? Math.round((statistics.completedReports / statistics.totalReports) * 100)
    : 0

  const failureRate = statistics.totalReports > 0 
    ? Math.round((statistics.failedReports / statistics.totalReports) * 100)
    : 0

  const formatDuration = (seconds: number) => {
    const minutes = Math.floor(seconds / 60)
    const remainingSeconds = seconds % 60
    if (minutes > 0) {
      return `${minutes}m ${remainingSeconds}s`
    }
    return `${remainingSeconds}s`
  }

  return (
    <div className="space-y-6">
      {/* Overview Cards */}
      <div className="grid grid-cols-1 md:grid-cols-2 lg:grid-cols-4 gap-4">
        <Card>
          <CardHeader className="flex flex-row items-center justify-between space-y-0 pb-2">
            <CardTitle className="text-sm font-medium">Total Reports</CardTitle>
            <FileText className="h-4 w-4 text-muted-foreground" />
          </CardHeader>
          <CardContent>
            <div className="text-2xl font-bold">{statistics.totalReports}</div>
            <p className="text-xs text-muted-foreground">
              All time generated reports
            </p>
          </CardContent>
        </Card>

        <Card>
          <CardHeader className="flex flex-row items-center justify-between space-y-0 pb-2">
            <CardTitle className="text-sm font-medium">Completed</CardTitle>
            <CheckCircle className="h-4 w-4 text-green-600" />
          </CardHeader>
          <CardContent>
            <div className="text-2xl font-bold text-green-600">{statistics.completedReports}</div>
            <div className="flex items-center space-x-2">
              <Progress value={completionRate} className="flex-1 h-2" />
              <span className="text-xs text-muted-foreground">{completionRate}%</span>
            </div>
          </CardContent>
        </Card>

        <Card>
          <CardHeader className="flex flex-row items-center justify-between space-y-0 pb-2">
            <CardTitle className="text-sm font-medium">Processing</CardTitle>
            <Clock className="h-4 w-4 text-blue-600" />
          </CardHeader>
          <CardContent>
            <div className="text-2xl font-bold text-blue-600">{statistics.processingReports}</div>
            <p className="text-xs text-muted-foreground">
              Currently being generated
            </p>
          </CardContent>
        </Card>

        <Card>
          <CardHeader className="flex flex-row items-center justify-between space-y-0 pb-2">
            <CardTitle className="text-sm font-medium">Failed</CardTitle>
            <XCircle className="h-4 w-4 text-red-600" />
          </CardHeader>
          <CardContent>
            <div className="text-2xl font-bold text-red-600">{statistics.failedReports}</div>
            <div className="flex items-center space-x-2">
              <Progress value={failureRate} className="flex-1 h-2" />
              <span className="text-xs text-muted-foreground">{failureRate}%</span>
            </div>
          </CardContent>
        </Card>
      </div>

      {/* Detailed Statistics */}
      <div className="grid grid-cols-1 lg:grid-cols-2 gap-6">
        {/* Performance Card */}
        <Card>
          <CardHeader>
            <CardTitle className="flex items-center space-x-2">
              <Activity className="h-5 w-5" />
              <span>Performance Metrics</span>
            </CardTitle>
            <CardDescription>
              Average report generation performance
            </CardDescription>
          </CardHeader>
          <CardContent className="space-y-4">
            <div className="flex items-center justify-between">
              <span className="text-sm text-muted-foreground">Average Generation Time</span>
              <div className="flex items-center space-x-2">
                <span className="font-medium">{formatDuration(statistics.averageGenerationTime)}</span>
                <TrendingDown className="h-4 w-4 text-green-600" />
              </div>
            </div>
            
            <div className="flex items-center justify-between">
              <span className="text-sm text-muted-foreground">Success Rate</span>
              <div className="flex items-center space-x-2">
                <span className="font-medium">{completionRate}%</span>
                {completionRate >= 90 ? (
                  <TrendingUp className="h-4 w-4 text-green-600" />
                ) : (
                  <TrendingDown className="h-4 w-4 text-red-600" />
                )}
              </div>
            </div>

            <div className="pt-2">
              <div className="flex justify-between text-sm mb-2">
                <span>System Performance</span>
                <span>{completionRate >= 95 ? 'Excellent' : completionRate >= 85 ? 'Good' : 'Needs Improvement'}</span>
              </div>
              <Progress 
                value={completionRate} 
                className="h-2"
              />
            </div>
          </CardContent>
        </Card>

        {/* Popular Report Types */}
        <Card>
          <CardHeader>
            <CardTitle className="flex items-center space-x-2">
              <BarChart3 className="h-5 w-5" />
              <span>Popular Report Types</span>
            </CardTitle>
            <CardDescription>
              Most frequently generated reports
            </CardDescription>
          </CardHeader>
          <CardContent>
            {statistics.mostPopularTypes.length > 0 ? (
              <div className="space-y-4">
                {statistics.mostPopularTypes.slice(0, 5).map((type, index) => {
                  const percentage = statistics.totalReports > 0 
                    ? Math.round((type.count / statistics.totalReports) * 100)
                    : 0
                  
                  return (
                    <div key={type.type} className="space-y-2">
                      <div className="flex items-center justify-between">
                        <div className="flex items-center space-x-2">
                          <Badge variant="outline" className="text-xs">
                            #{index + 1}
                          </Badge>
                          <span className="text-sm font-medium">
                            {type.type.replace(/([A-Z])/g, ' $1').trim()}
                          </span>
                        </div>
                        <div className="flex items-center space-x-2">
                          <span className="text-sm text-muted-foreground">{type.count}</span>
                          <span className="text-xs text-muted-foreground">({percentage}%)</span>
                        </div>
                      </div>
                      <Progress value={percentage} className="h-2" />
                    </div>
                  )
                })}
              </div>
            ) : (
              <div className="text-center py-4 text-muted-foreground">
                <BarChart3 className="h-8 w-8 mx-auto mb-2 opacity-50" />
                <p className="text-sm">No report data available</p>
              </div>
            )}
          </CardContent>
        </Card>
      </div>

      {/* Recent Activity */}
      {statistics.recentActivity.length > 0 && (
        <Card>
          <CardHeader>
            <CardTitle className="flex items-center space-x-2">
              <Calendar className="h-5 w-5" />
              <span>Recent Activity</span>
            </CardTitle>
            <CardDescription>
              Latest report generation activity
            </CardDescription>
          </CardHeader>
          <CardContent>
            <div className="space-y-3">
              {statistics.recentActivity.slice(0, 5).map((report) => {
                const getStatusColor = (status: string) => {
                  switch (status) {
                    case 'Completed': return 'text-green-600 bg-green-50'
                    case 'Processing': return 'text-blue-600 bg-blue-50'
                    case 'Failed': return 'text-red-600 bg-red-50'
                    default: return 'text-yellow-600 bg-yellow-50'
                  }
                }

                return (
                  <div key={report.id} className="flex items-center justify-between p-3 rounded-lg border">
                    <div className="flex items-center space-x-3">
                      <Badge variant="outline" className={getStatusColor(report.status)}>
                        {report.status}
                      </Badge>
                      <div>
                        <p className="text-sm font-medium">{report.title}</p>
                        <p className="text-xs text-muted-foreground">
                          {new Date(report.createdAt).toLocaleString()}
                        </p>
                      </div>
                    </div>
                    <div className="text-right">
                      <p className="text-xs text-muted-foreground">
                        {report.reportType.replace(/([A-Z])/g, ' $1').trim()}
                      </p>
                      {report.progress !== undefined && report.status === 'Processing' && (
                        <p className="text-xs text-blue-600">{report.progress}% complete</p>
                      )}
                    </div>
                  </div>
                )
              })}
            </div>
          </CardContent>
        </Card>
      )}
    </div>
  )
}