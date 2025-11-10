'use client'

import { useState, useEffect } from 'react'
import { Card, CardContent, CardDescription, CardHeader, CardTitle } from '@/components/ui/card'
import { Badge } from '@/components/ui/badge'
import { Button } from '@/components/ui/button'
import { Calendar } from '@/components/ui/calendar'
import { Alert, AlertDescription } from '@/components/ui/alert'
import { useToast } from '@/components/ui/use-toast'
import { ClientPortalService } from '@/lib/services/client-portal-service'
import { 
  Calendar as CalendarIcon, 
  Clock, 
  AlertTriangle, 
  CheckCircle,
  Bell,
  FileText,
  DollarSign,
  Plus
} from 'lucide-react'
import { format, isSameDay, addDays, isBefore } from 'date-fns'

interface Deadline {
  id: string
  title: string
  type: 'tax-filing' | 'payment' | 'compliance' | 'document'
  description: string
  dueDate: Date
  status: 'upcoming' | 'due-soon' | 'overdue' | 'completed'
  priority: 'high' | 'medium' | 'low'
  category: string
  client?: string
  amount?: number
  reminderSet: boolean
}

export default function ClientDeadlinesPage() {
  const [deadlines, setDeadlines] = useState<Deadline[]>([])
  const [selectedDate, setSelectedDate] = useState<Date>(new Date())
  const [loading, setLoading] = useState(true)
  const [error, setError] = useState<string | null>(null)
  const { toast } = useToast()

  useEffect(() => {
    const load = async () => {
      try {
        setLoading(true)
        setError(null)
        const data = await ClientPortalService.getDeadlines(60)
        const mapped: Deadline[] = (data || []).map((d: any) => {
          const days = typeof d.daysRemaining === 'number' ? d.daysRemaining : 9999
          let status: Deadline['status'] = 'upcoming'
          if (days < 0) status = 'overdue'
          else if (days <= 7) status = 'due-soon'

          const t = (d.type || '').toString().toLowerCase()
          const type: Deadline['type'] = t.includes('payment')
            ? 'payment'
            : t.includes('document')
              ? 'document'
              : (t.includes('tax') || t.includes('filing'))
                ? 'tax-filing'
                : 'compliance'

          return {
            id: String(d.id ?? crypto.randomUUID?.() ?? Math.random().toString(36).slice(2)),
            title: d.title || 'Deadline',
            type,
            description: d.description || '',
            dueDate: d.dueDate ? new Date(d.dueDate) : new Date(),
            status,
            priority: d.isUrgent ? 'high' : 'medium',
            category: d.type || 'General',
            reminderSet: Boolean(d.isUrgent),
          } as Deadline
        })
        setDeadlines(mapped)
      } catch (e: any) {
        setDeadlines([])
        setError('Failed to load deadlines')
        toast({ variant: 'destructive', title: 'Error', description: 'Failed to load deadlines' })
      } finally {
        setLoading(false)
      }
    }
    load()
  }, [])

  const getStatusIcon = (status: string) => {
    switch (status) {
      case 'completed':
        return <CheckCircle className="h-4 w-4 text-green-500" />
      case 'due-soon':
        return <AlertTriangle className="h-4 w-4 text-yellow-500" />
      case 'overdue':
        return <AlertTriangle className="h-4 w-4 text-red-500" />
      case 'upcoming':
        return <Clock className="h-4 w-4 text-blue-500" />
      default:
        return null
    }
  }

  const getStatusColor = (status: string) => {
    switch (status) {
      case 'completed':
        return 'bg-green-100 text-green-800'
      case 'due-soon':
        return 'bg-yellow-100 text-yellow-800'
      case 'overdue':
        return 'bg-red-100 text-red-800'
      case 'upcoming':
        return 'bg-blue-100 text-blue-800'
      default:
        return 'bg-gray-100 text-gray-800'
    }
  }

  const getTypeIcon = (type: string) => {
    switch (type) {
      case 'tax-filing':
        return <FileText className="h-4 w-4" />
      case 'payment':
        return <DollarSign className="h-4 w-4" />
      case 'compliance':
        return <CheckCircle className="h-4 w-4" />
      case 'document':
        return <FileText className="h-4 w-4" />
      default:
        return <CalendarIcon className="h-4 w-4" />
    }
  }

  const hasDeadlineOnDate = (date: Date) => {
    return deadlines.some(deadline => isSameDay(deadline.dueDate, date))
  }

  const getDeadlinesForDate = (date: Date) => {
    return deadlines.filter(deadline => isSameDay(deadline.dueDate, date))
  }

  const upcomingDeadlines = deadlines
    .filter(deadline => deadline.status !== 'completed')
    .sort((a, b) => a.dueDate.getTime() - b.dueDate.getTime())
    .slice(0, 5)

  const stats = {
    total: deadlines.length,
    upcoming: deadlines.filter(d => d.status === 'upcoming').length,
    dueSoon: deadlines.filter(d => d.status === 'due-soon').length,
    overdue: deadlines.filter(d => d.status === 'overdue').length,
    completed: deadlines.filter(d => d.status === 'completed').length
  }

  if (loading) {
    return (
      <div className="flex items-center justify-center min-h-screen">
        <div className="animate-spin rounded-full h-32 w-32 border-b-2 border-sierra-blue"></div>
      </div>
    )
  }

  return (
    <div className="space-y-6">
      {error && (
        <Alert>
          <AlertDescription>{error}</AlertDescription>
        </Alert>
      )}
      <div className="flex justify-between items-center">
        <div>
          <h1 className="text-3xl font-bold tracking-tight">My Deadlines</h1>
          <p className="text-muted-foreground mt-2">
            Track important tax deadlines and compliance requirements
          </p>
        </div>
        <Button>
          <Plus className="mr-2 h-4 w-4" />
          Add Reminder
        </Button>
      </div>

      {/* Stats Cards */}
      <div className="grid gap-4 md:grid-cols-2 lg:grid-cols-4">
        <Card>
          <CardHeader className="flex flex-row items-center justify-between space-y-0 pb-2">
            <CardTitle className="text-sm font-medium">Total Deadlines</CardTitle>
            <CalendarIcon className="h-4 w-4 text-muted-foreground" />
          </CardHeader>
          <CardContent>
            <div className="text-2xl font-bold">{stats.total}</div>
          </CardContent>
        </Card>

        <Card>
          <CardHeader className="flex flex-row items-center justify-between space-y-0 pb-2">
            <CardTitle className="text-sm font-medium">Due Soon</CardTitle>
            <AlertTriangle className="h-4 w-4 text-yellow-500" />
          </CardHeader>
          <CardContent>
            <div className="text-2xl font-bold text-yellow-600">{stats.dueSoon}</div>
          </CardContent>
        </Card>

        <Card>
          <CardHeader className="flex flex-row items-center justify-between space-y-0 pb-2">
            <CardTitle className="text-sm font-medium">Upcoming</CardTitle>
            <Clock className="h-4 w-4 text-blue-500" />
          </CardHeader>
          <CardContent>
            <div className="text-2xl font-bold text-blue-600">{stats.upcoming}</div>
          </CardContent>
        </Card>

        <Card>
          <CardHeader className="flex flex-row items-center justify-between space-y-0 pb-2">
            <CardTitle className="text-sm font-medium">Completed</CardTitle>
            <CheckCircle className="h-4 w-4 text-green-500" />
          </CardHeader>
          <CardContent>
            <div className="text-2xl font-bold text-green-600">{stats.completed}</div>
          </CardContent>
        </Card>
      </div>

      <div className="grid gap-6 lg:grid-cols-2">
        {/* Calendar View */}
        <Card>
          <CardHeader>
            <CardTitle>Calendar View</CardTitle>
            <CardDescription>
              Select a date to view deadlines
            </CardDescription>
          </CardHeader>
          <CardContent>
            <Calendar
              mode="single"
              selected={selectedDate}
              onSelect={(date) => date && setSelectedDate(date)}
              modifiers={{
                hasDeadline: (date) => hasDeadlineOnDate(date)
              }}
              modifiersStyles={{
                hasDeadline: { backgroundColor: '#fef3c7' }
              }}
              className="rounded-md border"
            />
            
            {selectedDate && (
              <div className="mt-4 space-y-2">
                <h4 className="font-medium">
                  {format(selectedDate, 'MMMM d, yyyy')}
                </h4>
                {getDeadlinesForDate(selectedDate).length > 0 ? (
                  <div className="space-y-2">
                    {getDeadlinesForDate(selectedDate).map((deadline) => (
                      <div key={deadline.id} className="p-2 border rounded text-sm">
                        <div className="flex items-center gap-2">
                          {getTypeIcon(deadline.type)}
                          <span className="font-medium">{deadline.title}</span>
                        </div>
                        <p className="text-muted-foreground mt-1">{deadline.description}</p>
                      </div>
                    ))}
                  </div>
                ) : (
                  <p className="text-sm text-muted-foreground">No deadlines on this date</p>
                )}
              </div>
            )}
          </CardContent>
        </Card>

        {/* Upcoming Deadlines */}
        <Card>
          <CardHeader>
            <CardTitle>Upcoming Deadlines</CardTitle>
            <CardDescription>
              Your next important deadlines
            </CardDescription>
          </CardHeader>
          <CardContent>
            <div className="space-y-4">
              {upcomingDeadlines.map((deadline) => (
                <div key={deadline.id} className="p-4 border rounded-lg">
                  <div className="flex justify-between items-start">
                    <div className="space-y-2 flex-1">
                      <div className="flex items-center gap-2">
                        {getStatusIcon(deadline.status)}
                        <span className="font-medium">{deadline.title}</span>
                        <Badge className={getStatusColor(deadline.status)}>
                          {deadline.status.replace('-', ' ')}
                        </Badge>
                      </div>
                      
                      <p className="text-sm text-muted-foreground">
                        {deadline.description}
                      </p>
                      
                      <div className="flex items-center gap-4 text-sm">
                        <div className="flex items-center gap-1">
                          <CalendarIcon className="h-4 w-4" />
                          <span>{format(deadline.dueDate, 'MMM d, yyyy')}</span>
                        </div>
                        
                        {deadline.amount && (
                          <div className="flex items-center gap-1">
                            <DollarSign className="h-4 w-4" />
                            <span>Le {deadline.amount.toLocaleString()}</span>
                          </div>
                        )}
                        
                        <div className="flex items-center gap-1">
                          {getTypeIcon(deadline.type)}
                          <span className="capitalize">{deadline.type.replace('-', ' ')}</span>
                        </div>
                      </div>
                      
                      <div className="flex items-center gap-2">
                        <Badge variant="outline">{deadline.category}</Badge>
                        {deadline.reminderSet && (
                          <div className="flex items-center gap-1 text-xs text-muted-foreground">
                            <Bell className="h-3 w-3" />
                            <span>Reminder set</span>
                          </div>
                        )}
                      </div>
                    </div>
                    
                    <div className="flex gap-2 ml-4">
                      <Button variant="outline" size="sm">
                        <Bell className="mr-2 h-4 w-4" />
                        {deadline.reminderSet ? 'Edit' : 'Set'} Reminder
                      </Button>
                    </div>
                  </div>
                </div>
              ))}
              
              {upcomingDeadlines.length === 0 && (
                <div className="text-center py-8">
                  <CalendarIcon className="mx-auto h-12 w-12 text-muted-foreground" />
                  <p className="text-muted-foreground mt-2">No upcoming deadlines</p>
                </div>
              )}
            </div>
          </CardContent>
        </Card>
      </div>

      {/* Sierra Leone Specific Notice */}
      <Card className="border-sierra-blue-200 bg-sierra-blue-50">
        <CardHeader>
          <CardTitle className="flex items-center gap-2">
            <span className="text-lg">🇸🇱</span>
            Sierra Leone Tax Calendar 2025
          </CardTitle>
        </CardHeader>
        <CardContent>
          <div className="grid gap-4 md:grid-cols-3">
            <div>
              <h4 className="font-medium text-sierra-blue-800">Income Tax</h4>
              <p className="text-sm text-sierra-blue-700">Annual returns due March 31st</p>
            </div>
            <div>
              <h4 className="font-medium text-sierra-blue-800">GST Returns</h4>
              <p className="text-sm text-sierra-blue-700">Quarterly by month-end following quarter</p>
            </div>
            <div>
              <h4 className="font-medium text-sierra-blue-800">Payroll Tax</h4>
              <p className="text-sm text-sierra-blue-700">Monthly by 15th of following month</p>
            </div>
          </div>
        </CardContent>
      </Card>
    </div>
  )
}