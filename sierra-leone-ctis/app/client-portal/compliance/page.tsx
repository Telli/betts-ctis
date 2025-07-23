'use client'

import { useState, useEffect } from 'react'
import { Card, CardContent, CardDescription, CardHeader, CardTitle } from '@/components/ui/card'
import { Badge } from '@/components/ui/badge'
import { Button } from '@/components/ui/button'
import { Progress } from '@/components/ui/progress'
import { 
  Shield, 
  CheckCircle, 
  Clock, 
  AlertTriangle, 
  XCircle,
  Calendar,
  DollarSign,
  FileText,
  TrendingUp,
  Eye
} from 'lucide-react'
import { format } from 'date-fns'

interface ComplianceItem {
  id: string
  type: string
  description: string
  status: 'compliant' | 'at-risk' | 'overdue' | 'pending'
  dueDate: Date
  lastUpdated: Date
  priority: 'high' | 'medium' | 'low'
  penalty?: number
  actions: string[]
}

export default function ClientCompliancePage() {
  const [complianceItems, setComplianceItems] = useState<ComplianceItem[]>([])
  const [loading, setLoading] = useState(true)

  useEffect(() => {
    // Mock data - replace with actual API call
    const mockComplianceItems: ComplianceItem[] = [
      {
        id: '1',
        type: 'Income Tax Filing',
        description: '2024 Annual Income Tax Return',
        status: 'compliant',
        dueDate: new Date(2025, 2, 31),
        lastUpdated: new Date(2025, 0, 15),
        priority: 'high',
        actions: ['Filed on time', 'Payment completed']
      },
      {
        id: '2',
        type: 'GST Return',
        description: 'Q4 2024 GST Return Filing',
        status: 'at-risk',
        dueDate: new Date(2025, 0, 31),
        lastUpdated: new Date(2025, 0, 20),
        priority: 'high',
        actions: ['Documents pending review', 'Submit supporting documents']
      },
      {
        id: '3',
        type: 'Payroll Tax',
        description: 'December 2024 Payroll Tax',
        status: 'pending',
        dueDate: new Date(2025, 1, 15),
        lastUpdated: new Date(2024, 11, 30),
        priority: 'medium',
        actions: ['Prepare payroll register', 'Calculate employee deductions']
      }
    ]
    
    setComplianceItems(mockComplianceItems)
    setLoading(false)
  }, [])

  const getStatusIcon = (status: string) => {
    switch (status) {
      case 'compliant':
        return <CheckCircle className="h-4 w-4 text-green-500" />
      case 'at-risk':
        return <Clock className="h-4 w-4 text-yellow-500" />
      case 'overdue':
        return <XCircle className="h-4 w-4 text-red-500" />
      case 'pending':
        return <Clock className="h-4 w-4 text-blue-500" />
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
      case 'overdue':
        return 'bg-red-100 text-red-800'
      case 'pending':
        return 'bg-blue-100 text-blue-800'
      default:
        return 'bg-gray-100 text-gray-800'
    }
  }

  const getPriorityColor = (priority: string) => {
    switch (priority) {
      case 'high':
        return 'bg-red-100 text-red-800'
      case 'medium':
        return 'bg-yellow-100 text-yellow-800'
      case 'low':
        return 'bg-green-100 text-green-800'
      default:
        return 'bg-gray-100 text-gray-800'
    }
  }

  const stats = {
    total: complianceItems.length,
    compliant: complianceItems.filter(item => item.status === 'compliant').length,
    atRisk: complianceItems.filter(item => item.status === 'at-risk').length,
    overdue: complianceItems.filter(item => item.status === 'overdue').length,
    pending: complianceItems.filter(item => item.status === 'pending').length
  }

  const complianceScore = stats.total > 0 ? Math.round((stats.compliant / stats.total) * 100) : 100

  if (loading) {
    return (
      <div className="flex items-center justify-center min-h-screen">
        <div className="animate-spin rounded-full h-32 w-32 border-b-2 border-sierra-blue"></div>
      </div>
    )
  }

  return (
    <div className="space-y-6">
      <div className="flex justify-between items-center">
        <div>
          <h1 className="text-3xl font-bold tracking-tight">My Compliance Status</h1>
          <p className="text-muted-foreground mt-2">
            Monitor your tax compliance status and upcoming requirements
          </p>
        </div>
        <Button>
          <Eye className="mr-2 h-4 w-4" />
          View Full Report
        </Button>
      </div>

      {/* Compliance Score */}
      <Card className="border-sierra-blue-200">
        <CardHeader>
          <div className="flex items-center justify-between">
            <div>
              <CardTitle className="flex items-center gap-2">
                <Shield className="h-6 w-6 text-sierra-blue-600" />
                Compliance Score
              </CardTitle>
              <CardDescription>
                Your overall tax compliance status
              </CardDescription>
            </div>
            <div className="text-right">
              <div className="text-4xl font-bold text-sierra-blue-600">{complianceScore}%</div>
              <div className="text-sm text-muted-foreground">Current Score</div>
            </div>
          </div>
        </CardHeader>
        <CardContent>
          <Progress value={complianceScore} className="mb-4" />
          <div className="grid grid-cols-4 gap-4 text-center">
            <div>
              <div className="text-2xl font-bold text-green-600">{stats.compliant}</div>
              <div className="text-sm text-muted-foreground">Compliant</div>
            </div>
            <div>
              <div className="text-2xl font-bold text-yellow-600">{stats.atRisk}</div>
              <div className="text-sm text-muted-foreground">At Risk</div>
            </div>
            <div>
              <div className="text-2xl font-bold text-red-600">{stats.overdue}</div>
              <div className="text-sm text-muted-foreground">Overdue</div>
            </div>
            <div>
              <div className="text-2xl font-bold text-blue-600">{stats.pending}</div>
              <div className="text-sm text-muted-foreground">Pending</div>
            </div>
          </div>
        </CardContent>
      </Card>

      {/* Compliance Items */}
      <div className="grid gap-4">
        <h2 className="text-xl font-semibold">Compliance Requirements</h2>
        
        {complianceItems.map((item) => (
          <Card key={item.id} className="p-4">
            <div className="flex justify-between items-start">
              <div className="space-y-3 flex-1">
                <div className="flex items-center gap-2">
                  {getStatusIcon(item.status)}
                  <span className="font-medium">{item.type}</span>
                  <Badge className={getStatusColor(item.status)}>
                    {item.status.replace('-', ' ')}
                  </Badge>
                  <Badge className={getPriorityColor(item.priority)}>
                    {item.priority} priority
                  </Badge>
                </div>
                
                <p className="text-sm text-muted-foreground">
                  {item.description}
                </p>
                
                <div className="flex items-center gap-4 text-sm">
                  <div className="flex items-center gap-1">
                    <Calendar className="h-4 w-4" />
                    <span>Due: {format(item.dueDate, 'MMMM d, yyyy')}</span>
                  </div>
                  <div className="flex items-center gap-1">
                    <Clock className="h-4 w-4" />
                    <span>Updated: {format(item.lastUpdated, 'MMM d, yyyy')}</span>
                  </div>
                  {item.penalty && (
                    <div className="flex items-center gap-1 text-red-600">
                      <DollarSign className="h-4 w-4" />
                      <span>Penalty: Le {item.penalty.toLocaleString()}</span>
                    </div>
                  )}
                </div>
                
                <div className="space-y-2">
                  <h4 className="text-sm font-medium">Required Actions:</h4>
                  <ul className="space-y-1">
                    {item.actions.map((action, index) => (
                      <li key={index} className="text-sm text-muted-foreground flex items-center gap-2">
                        <div className="w-1 h-1 bg-sierra-blue-400 rounded-full" />
                        {action}
                      </li>
                    ))}
                  </ul>
                </div>
              </div>
              
              <div className="flex gap-2 ml-4">
                <Button variant="outline" size="sm">
                  <FileText className="mr-2 h-4 w-4" />
                  Details
                </Button>
              </div>
            </div>
          </Card>
        ))}
      </div>

      {/* Compliance Insights */}
      <Card>
        <CardHeader>
          <CardTitle className="flex items-center gap-2">
            <TrendingUp className="h-5 w-5" />
            Compliance Insights
          </CardTitle>
          <CardDescription>
            Recommendations to improve your compliance status
          </CardDescription>
        </CardHeader>
        <CardContent>
          <div className="space-y-4">
            <div className="p-4 bg-sierra-blue-50 border border-sierra-blue-200 rounded-lg">
              <h4 className="font-medium text-sierra-blue-800 mb-2">🇸🇱 Sierra Leone Finance Act 2025</h4>
              <p className="text-sm text-sierra-blue-700">
                Stay compliant with the latest Sierra Leone tax regulations. Your current compliance score of {complianceScore}% 
                {complianceScore >= 90 ? ' exceeds the recommended threshold.' : ' can be improved by addressing pending items.'}
              </p>
            </div>
            
            <div className="grid gap-4 md:grid-cols-2">
              <div className="space-y-2">
                <h4 className="font-medium">Next Due Date</h4>
                <p className="text-sm text-muted-foreground">
                  {complianceItems.length > 0 
                    ? format(Math.min(...complianceItems.map(item => item.dueDate.getTime())), 'MMMM d, yyyy')
                    : 'No upcoming deadlines'
                  }
                </p>
              </div>
              
              <div className="space-y-2">
                <h4 className="font-medium">Priority Actions</h4>
                <p className="text-sm text-muted-foreground">
                  {stats.atRisk + stats.overdue > 0 
                    ? `${stats.atRisk + stats.overdue} items need immediate attention`
                    : 'All items are on track'
                  }
                </p>
              </div>
            </div>
          </div>
        </CardContent>
      </Card>
    </div>
  )
}