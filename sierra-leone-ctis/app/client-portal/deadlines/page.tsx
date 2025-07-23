'use client'

import { useState, useEffect } from 'react'
import { Card, CardContent, CardDescription, CardHeader, CardTitle } from '@/components/ui/card'
import { Badge } from '@/components/ui/badge'
import { Button } from '@/components/ui/button'
import { Calendar } from '@/components/ui/calendar'
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

  useEffect(() => {
    // Mock data - replace with actual API call
    const mockDeadlines: Deadline[] = [
      {
        id: '1',
        title: 'GST Return Filing',
        type: 'tax-filing',
        description: 'Q4 2024 GST Return submission',
        dueDate: new Date(2025, 0, 31),
        status: 'due-soon',
        priority: 'high',
        category: 'GST',
        reminderSet: true
      },
      {
        id: '2',
        title: 'Income Tax Payment',
        type: 'payment',
        description: '2024 Annual Income Tax payment',
        dueDate: new Date(2025, 2, 31),
        status: 'upcoming',
        priority: 'high',
        category: 'Income Tax',
        amount: 150000,
        reminderSet: true
      },
      {
        id: '3',
        title: 'Payroll Tax Filing',
        type: 'tax-filing',
        description: 'January 2025 Payroll Tax Return',
        dueDate: new Date(2025, 1, 15),
        status: 'upcoming',
        priority: 'medium',
        category: 'Payroll Tax',
        reminderSet: false
      },
      {
        id: '4',
        title: 'Annual Accounts Submission',
        type: 'document',
        description: 'Submit audited annual accounts',
        dueDate: new Date(2025, 3, 30),
        status: 'upcoming',
        priority: 'medium',
        category: 'Compliance',
        reminderSet: true
      },
      {
        id: '5',
        title: 'Excise Duty Payment',
        type: 'payment',
        description: 'Monthly excise duty payment',
        dueDate: new Date(2025, 1, 5),
        status: 'upcoming',
        priority: 'medium',
        category: 'Excise Duty',
        amount: 25000,
        reminderSet: true
      }
    ]
    
    setDeadlines(mockDeadlines)
    setLoading(false)
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