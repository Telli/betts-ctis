'use client'

import { useState, useEffect } from 'react'
import { Card, CardContent, CardDescription, CardHeader, CardTitle } from '@/components/ui/card'
import { Badge } from '@/components/ui/badge'
import { Button } from '@/components/ui/button'
import { Calendar } from '@/components/ui/calendar'
import { Tabs, TabsContent, TabsList, TabsTrigger } from '@/components/ui/tabs'
import { AlertTriangle, Calendar as CalendarIcon, Clock, CheckCircle, Bell, Filter } from 'lucide-react'
import { format, isAfter, isBefore, addDays, startOfToday } from 'date-fns'
import { DeadlineService, Deadline, DeadlineStats } from '@/lib/services/deadline-service'
import { formatSierraLeones } from '@/lib/utils/currency'


export default function DeadlinesPage() {
  const [deadlines, setDeadlines] = useState<Deadline[]>([])
  const [stats, setStats] = useState<DeadlineStats | null>(null)
  const [selectedDate, setSelectedDate] = useState<Date | undefined>(new Date())
  const [loading, setLoading] = useState(true)
  const [error, setError] = useState<string | null>(null)
  const [activeTab, setActiveTab] = useState('calendar')

  useEffect(() => {
    const fetchDeadlineData = async () => {
      try {
        setLoading(true)
        setError(null)
        
        const [deadlineList, deadlineStats] = await Promise.all([
          DeadlineService.getDeadlines(),
          DeadlineService.getDeadlineStats()
        ])
        
        setDeadlines(deadlineList)
        setStats(deadlineStats)
      } catch (err) {
        console.error('Error fetching deadline data:', err)
        setError('Failed to load deadline data. Please try again later.')
      } finally {
        setLoading(false)
      }
    }

    fetchDeadlineData()
  }, [])

  const getStatusIcon = (status: string) => {
    switch (status) {
      case 'completed':
        return <CheckCircle className="h-4 w-4 text-green-500" />
      case 'upcoming':
        return <CalendarIcon className="h-4 w-4 text-blue-500" />
      case 'due-soon':
        return <Clock className="h-4 w-4 text-yellow-500" />
      case 'overdue':
        return <AlertTriangle className="h-4 w-4 text-red-500" />
      default:
        return null
    }
  }

  const getStatusColor = (status: string) => {
    switch (status) {
      case 'completed':
        return 'bg-green-100 text-green-800'
      case 'upcoming':
        return 'bg-blue-100 text-blue-800'
      case 'due-soon':
        return 'bg-yellow-100 text-yellow-800'
      case 'overdue':
        return 'bg-red-100 text-red-800'
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

  const getCategoryColor = (category: string) => {
    switch (category) {
      case 'filing':
        return 'bg-blue-100 text-blue-800'
      case 'payment':
        return 'bg-green-100 text-green-800'
      case 'document-submission':
        return 'bg-purple-100 text-purple-800'
      case 'compliance-check':
        return 'bg-orange-100 text-orange-800'
      default:
        return 'bg-gray-100 text-gray-800'
    }
  }

  const today = startOfToday()
  const displayStats = stats || {
    total: deadlines?.length || 0,
    overdue: deadlines?.filter(d => d.status === 'overdue').length || 0,
    dueSoon: deadlines?.filter(d => d.status === 'due-soon').length || 0,
    upcoming: deadlines?.filter(d => d.status === 'upcoming').length || 0,
    thisWeek: deadlines?.filter(d => d.status === 'due-soon').length || 0,
    thisMonth: deadlines?.filter(d => d.status === 'upcoming').length || 0,
    byType: {},
    byPriority: {}
  }

  const getDeadlinesForDate = (date: Date) => {
    return (deadlines || []).filter(deadline => 
      format(new Date(deadline.dueDate), 'yyyy-MM-dd') === format(date, 'yyyy-MM-dd')
    )
  }

  const hasDeadlineOnDate = (date: Date) => {
    return getDeadlinesForDate(date).length > 0
  }

  if (loading) {
    return (
      <div className="flex items-center justify-center min-h-screen">
        <div className="animate-spin rounded-full h-32 w-32 border-b-2 border-sierra-blue"></div>
      </div>
    )
  }

  if (error) {
    return (
      <div className="flex items-center justify-center min-h-screen">
        <Card className="w-full max-w-md">
          <CardHeader>
            <CardTitle>Error Loading Deadlines</CardTitle>
          </CardHeader>
          <CardContent>
            <p className="text-muted-foreground mb-4">{error}</p>
            <Button onClick={() => window.location.reload()}>
              Retry
            </Button>
          </CardContent>
        </Card>
      </div>
    )
  }

  return (
    <div className="space-y-6">
      <div className="flex justify-between items-center">
        <div>
          <h1 className="text-3xl font-bold tracking-tight">Tax Deadlines</h1>
          <p className="text-muted-foreground mt-2">
            Track and manage upcoming tax filing and payment deadlines
          </p>
        </div>
        <div className="flex gap-2">
          <Button variant="outline">
            <Filter className="mr-2 h-4 w-4" />
            Filter
          </Button>
          <Button>
            <Bell className="mr-2 h-4 w-4" />
            Set Reminder
          </Button>
        </div>
      </div>

      {/* Overview Cards */}
      <div className="grid gap-4 md:grid-cols-2 lg:grid-cols-4">
        <Card>
          <CardHeader className="flex flex-row items-center justify-between space-y-0 pb-2">
            <CardTitle className="text-sm font-medium">Overdue</CardTitle>
            <AlertTriangle className="h-4 w-4 text-red-500" />
          </CardHeader>
          <CardContent>
            <div className="text-2xl font-bold text-red-600">{displayStats.overdue}</div>
            <p className="text-xs text-muted-foreground">
              require immediate attention
            </p>
          </CardContent>
        </Card>

        <Card>
          <CardHeader className="flex flex-row items-center justify-between space-y-0 pb-2">
            <CardTitle className="text-sm font-medium">Due Soon</CardTitle>
            <Clock className="h-4 w-4 text-yellow-500" />
          </CardHeader>
          <CardContent>
            <div className="text-2xl font-bold text-yellow-600">{displayStats.dueSoon}</div>
            <p className="text-xs text-muted-foreground">
              due within 7 days
            </p>
          </CardContent>
        </Card>

        <Card>
          <CardHeader className="flex flex-row items-center justify-between space-y-0 pb-2">
            <CardTitle className="text-sm font-medium">Upcoming</CardTitle>
            <CalendarIcon className="h-4 w-4 text-blue-500" />
          </CardHeader>
          <CardContent>
            <div className="text-2xl font-bold text-blue-600">{displayStats.upcoming}</div>
            <p className="text-xs text-muted-foreground">
              scheduled for later
            </p>
          </CardContent>
        </Card>

        <Card>
          <CardHeader className="flex flex-row items-center justify-between space-y-0 pb-2">
            <CardTitle className="text-sm font-medium">Reminders Sent</CardTitle>
            <Bell className="h-4 w-4 text-muted-foreground" />
          </CardHeader>
          <CardContent>
            <div className="text-2xl font-bold">{displayStats.thisWeek + displayStats.thisMonth}</div>
            <p className="text-xs text-muted-foreground">
              across all deadlines
            </p>
          </CardContent>
        </Card>
      </div>

      {/* Deadline Management */}
      <Tabs value={activeTab} onValueChange={setActiveTab}>
        <TabsList>
          <TabsTrigger value="calendar">Calendar View</TabsTrigger>
          <TabsTrigger value="list">List View</TabsTrigger>
          <TabsTrigger value="overdue">Overdue</TabsTrigger>
          <TabsTrigger value="alerts">Alerts</TabsTrigger>
        </TabsList>

        <TabsContent value="calendar" className="space-y-4">
          <div className="grid gap-4 md:grid-cols-3">
            <Card className="md:col-span-1">
              <CardHeader>
                <CardTitle>Calendar</CardTitle>
                <CardDescription>Click on a date to view deadlines</CardDescription>
              </CardHeader>
              <CardContent>
                <Calendar
                  mode="single"
                  selected={selectedDate}
                  onSelect={setSelectedDate}
                  className="rounded-md border"
                  modifiers={{
                    hasDeadline: (date) => hasDeadlineOnDate(date)
                  }}
                  modifiersStyles={{
                    hasDeadline: { 
                      backgroundColor: '#fef3c7', 
                      color: '#d97706',
                      fontWeight: 'bold'
                    }
                  }}
                />
              </CardContent>
            </Card>

            <Card className="md:col-span-2">
              <CardHeader>
                <CardTitle>
                  Deadlines for {selectedDate ? format(selectedDate, 'MMMM d, yyyy') : 'Selected Date'}
                </CardTitle>
              </CardHeader>
              <CardContent>
                {selectedDate && (
                  <div className="space-y-4">
                    {getDeadlinesForDate(selectedDate).length === 0 ? (
                      <p className="text-muted-foreground text-center py-8">
                        No deadlines for this date
                      </p>
                    ) : (
                      getDeadlinesForDate(selectedDate).map((deadline) => (
                        <div key={deadline.id} className="flex items-center justify-between p-4 border rounded-lg">
                          <div className="space-y-1">
                            <div className="flex items-center gap-2">
                              {getStatusIcon(deadline.status)}
                              <span className="font-medium">{deadline.clientName}</span>
                              <Badge variant="outline">{deadline.clientId}</Badge>
                            </div>
                            <div className="text-sm text-muted-foreground">
                              {deadline.description}
                            </div>
                            <div className="flex gap-2">
                              <Badge className={getCategoryColor(deadline.category)}>
                                {deadline.category.replace('-', ' ')}
                              </Badge>
                              <Badge className={getPriorityColor(deadline.priority)}>
                                {deadline.priority} priority
                              </Badge>
                            </div>
                          </div>
                          <div className="text-right">
                            {deadline.amount && (
                              <div className="text-sm font-medium">
                                {formatSierraLeones(deadline.amount)}
                              </div>
                            )}
                            <Button variant="outline" size="sm">
                              View Details
                            </Button>
                          </div>
                        </div>
                      ))
                    )}
                  </div>
                )}
              </CardContent>
            </Card>
          </div>
        </TabsContent>

        <TabsContent value="list" className="space-y-4">
          <Card>
            <CardHeader>
              <CardTitle>All Deadlines</CardTitle>
              <CardDescription>Complete list of upcoming tax deadlines</CardDescription>
            </CardHeader>
            <CardContent>
              <div className="space-y-4">
                {(deadlines || [])
                  .sort((a, b) => new Date(a.dueDate).getTime() - new Date(b.dueDate).getTime())
                  .map((deadline) => (
                    <div key={deadline.id} className="flex items-center justify-between p-4 border rounded-lg">
                      <div className="space-y-1">
                        <div className="flex items-center gap-2">
                          {getStatusIcon(deadline.status)}
                          <span className="font-medium">{deadline.clientName}</span>
                          <Badge variant="outline">{deadline.clientId}</Badge>
                        </div>
                        <div className="text-sm text-muted-foreground">
                          {deadline.description}
                        </div>
                        <div className="flex gap-2">
                          <Badge className={getCategoryColor(deadline.category)}>
                            {deadline.category.replace('-', ' ')}
                          </Badge>
                          <Badge className={getPriorityColor(deadline.priority)}>
                            {deadline.priority} priority
                          </Badge>
                          <Badge className={getStatusColor(deadline.status)}>
                            {deadline.status.replace('-', ' ')}
                          </Badge>
                        </div>
                      </div>
                      
                      <div className="text-right space-y-1">
                        <div className="text-sm font-medium">
                          Due: {format(new Date(deadline.dueDate), 'MMM d, yyyy')}
                        </div>
                        {deadline.amount && (
                          <div className="text-sm text-muted-foreground">
                            {formatSierraLeones(deadline.amount)}
                          </div>
                        )}
                        {deadline.reminderSet && (
                          <div className="text-xs text-muted-foreground">
                            Reminders configured
                          </div>
                        )}
                      </div>
                    </div>
                  ))}
              </div>
            </CardContent>
          </Card>
        </TabsContent>

        <TabsContent value="overdue" className="space-y-4">
          <Card>
            <CardHeader>
              <CardTitle className="text-red-600">Overdue Deadlines</CardTitle>
              <CardDescription>These deadlines require immediate attention</CardDescription>
            </CardHeader>
            <CardContent>
              <div className="space-y-4">
                {(deadlines || [])
                  .filter(deadline => deadline.status === 'overdue')
                  .map((deadline) => (
                    <div key={deadline.id} className="flex items-center justify-between p-4 border border-red-200 rounded-lg bg-red-50">
                      <div className="space-y-1">
                        <div className="flex items-center gap-2">
                          <AlertTriangle className="h-4 w-4 text-red-500" />
                          <span className="font-medium">{deadline.clientName}</span>
                          <Badge variant="outline">{deadline.clientId}</Badge>
                        </div>
                        <div className="text-sm text-muted-foreground">
                          {deadline.description}
                        </div>
                        <div className="text-sm text-red-600 font-medium">
                          {Math.abs((today.getTime() - new Date(deadline.dueDate).getTime()) / (1000 * 60 * 60 * 24))} days overdue
                        </div>
                      </div>
                      
                      <div className="space-x-2">
                        <Button variant="outline" size="sm">
                          Send Reminder
                        </Button>
                        <Button size="sm">
                          Take Action
                        </Button>
                      </div>
                    </div>
                  ))}
                
                {(deadlines || []).filter(d => d.status === 'overdue').length === 0 && (
                  <p className="text-center text-muted-foreground py-8">
                    No overdue deadlines. Great job staying on top of compliance!
                  </p>
                )}
              </div>
            </CardContent>
          </Card>
        </TabsContent>

        <TabsContent value="alerts" className="space-y-4">
          <Card>
            <CardHeader>
              <CardTitle>Deadline Alerts & Reminders</CardTitle>
              <CardDescription>Automated notification system for upcoming deadlines</CardDescription>
            </CardHeader>
            <CardContent>
              <div className="space-y-4">
                <div className="grid gap-4 md:grid-cols-2">
                  <Card>
                    <CardHeader>
                      <CardTitle className="text-base">Alert Settings</CardTitle>
                    </CardHeader>
                    <CardContent className="space-y-2">
                      <div className="flex justify-between">
                        <span>30 days before</span>
                        <Badge variant="outline">Email + SMS</Badge>
                      </div>
                      <div className="flex justify-between">
                        <span>14 days before</span>
                        <Badge variant="outline">Email + SMS</Badge>
                      </div>
                      <div className="flex justify-between">
                        <span>7 days before</span>
                        <Badge variant="outline">Email + SMS</Badge>
                      </div>
                      <div className="flex justify-between">
                        <span>1 day before</span>
                        <Badge variant="destructive">Email + SMS + Call</Badge>
                      </div>
                    </CardContent>
                  </Card>

                  <Card>
                    <CardHeader>
                      <CardTitle className="text-base">Recent Notifications</CardTitle>
                    </CardHeader>
                    <CardContent className="space-y-2">
                      <div className="text-sm">
                        <div className="font-medium">GST Filing Reminder</div>
                        <div className="text-muted-foreground">Sent to Sierra Mining Corp</div>
                        <div className="text-xs text-muted-foreground">2 hours ago</div>
                      </div>
                      <div className="text-sm">
                        <div className="font-medium">Overdue Payment Alert</div>
                        <div className="text-muted-foreground">Sent to Bo Trading Company</div>
                        <div className="text-xs text-muted-foreground">1 day ago</div>
                      </div>
                      <div className="text-sm">
                        <div className="font-medium">Document Submission Due</div>
                        <div className="text-muted-foreground">Sent to Makeni Services Co</div>
                        <div className="text-xs text-muted-foreground">3 days ago</div>
                      </div>
                    </CardContent>
                  </Card>
                </div>
              </div>
            </CardContent>
          </Card>
        </TabsContent>
      </Tabs>
    </div>
  )
}