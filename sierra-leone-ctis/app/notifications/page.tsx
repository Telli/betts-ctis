'use client'

import { useState, useEffect } from 'react'
import { Card, CardContent, CardDescription, CardHeader, CardTitle } from '@/components/ui/card'
import { Badge } from '@/components/ui/badge'
import { Button } from '@/components/ui/button'
import { Tabs, TabsContent, TabsList, TabsTrigger } from '@/components/ui/tabs'
import { Switch } from '@/components/ui/switch'
import { Input } from '@/components/ui/input'
import { Label } from '@/components/ui/label'
import { 
  Bell, 
  Mail, 
  MessageSquare, 
  Phone, 
  Settings, 
  Check, 
  X, 
  Eye,
  Trash2,
  Filter,
  Circle
} from 'lucide-react'
import { format } from 'date-fns'
import { NotificationService, Notification, NotificationSettings, NotificationStats } from '@/lib/services/notification-service'
import { formatSierraLeones } from '@/lib/utils/currency'


export default function NotificationsPage() {
  const [notifications, setNotifications] = useState<Notification[]>([])
  const [settings, setSettings] = useState<NotificationSettings | null>(null)
  const [stats, setStats] = useState<NotificationStats | null>(null)
  const [loading, setLoading] = useState(true)
  const [error, setError] = useState<string | null>(null)
  const [activeTab, setActiveTab] = useState('inbox')
  const [filterStatus, setFilterStatus] = useState<string>('all')
  const [filterType, setFilterType] = useState<string>('all')

  useEffect(() => {
    const fetchNotificationData = async () => {
      try {
        setLoading(true)
        setError(null)
        
        const [notificationList, notificationStats, notificationSettings] = await Promise.all([
          NotificationService.getNotifications(),
          NotificationService.getNotificationStats(),
          NotificationService.getNotificationSettings()
        ])
        
        setNotifications(notificationList)
        setStats(notificationStats)
        setSettings(notificationSettings)
      } catch (err) {
        console.error('Error fetching notification data:', err)
        setError('Failed to load notification data. Please try again later.')
      } finally {
        setLoading(false)
      }
    }

    fetchNotificationData()
  }, [])

  const getTypeIcon = (type: string) => {
    switch (type) {
      case 'email':
        return <Mail className="h-4 w-4" />
      case 'sms':
        return <MessageSquare className="h-4 w-4" />
      case 'system':
        return <Bell className="h-4 w-4" />
      case 'reminder':
        return <Bell className="h-4 w-4" />
      default:
        return <Bell className="h-4 w-4" />
    }
  }

  const getStatusColor = (status: string) => {
    switch (status) {
      case 'sent':
        return 'bg-blue-100 text-blue-800'
      case 'delivered':
        return 'bg-green-100 text-green-800'
      case 'read':
        return 'bg-gray-100 text-gray-800'
      case 'failed':
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
      case 'deadline':
        return 'bg-orange-100 text-orange-800'
      case 'payment':
        return 'bg-green-100 text-green-800'
      case 'compliance':
        return 'bg-red-100 text-red-800'
      case 'system':
        return 'bg-blue-100 text-blue-800'
      case 'general':
        return 'bg-gray-100 text-gray-800'
      default:
        return 'bg-gray-100 text-gray-800'
    }
  }

  const markAsRead = async (id: string) => {
    try {
      await NotificationService.markAsRead(id)
      setNotifications(prev => 
        prev.map(notification => 
          notification.id === id 
            ? { ...notification, isRead: true, readAt: new Date().toISOString() }
            : notification
        )
      )
    } catch (err) {
      console.error('Error marking notification as read:', err)
    }
  }

  const markAsUnread = async (id: string) => {
    try {
      await NotificationService.markAsUnread(id)
      setNotifications(prev => 
        prev.map(notification => 
          notification.id === id 
            ? { ...notification, isRead: false, readAt: undefined }
            : notification
        )
      )
    } catch (err) {
      console.error('Error marking notification as unread:', err)
    }
  }

  const deleteNotification = async (id: string) => {
    try {
      await NotificationService.deleteNotification(id)
      setNotifications(prev => prev.filter(notification => notification.id !== id))
    } catch (err) {
      console.error('Error deleting notification:', err)
    }
  }

  const filteredNotifications = (notifications || []).filter(notification => {
    if (filterStatus !== 'all' && (filterStatus === 'read' ? !notification.isRead : notification.isRead)) {
      return false
    }
    if (filterType !== 'all' && notification.type !== filterType) {
      return false
    }
    return true
  })

  const displayStats = stats || {
    total: notifications?.length || 0,
    unread: notifications?.filter(n => !n.isRead).length || 0,
    high: notifications?.filter(n => n.priority === 'high').length || 0,
    sent: notifications?.filter(n => n.status === 'sent').length || 0,
    delivered: notifications?.filter(n => n.status === 'delivered').length || 0,
    failed: notifications?.filter(n => n.status === 'failed').length || 0
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
            <CardTitle>Error Loading Notifications</CardTitle>
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
          <h1 className="text-3xl font-bold tracking-tight">Notifications</h1>
          <p className="text-muted-foreground mt-2">
            Manage system notifications, alerts, and communication preferences
          </p>
        </div>
        <Button>
          <Settings className="mr-2 h-4 w-4" />
          Notification Settings
        </Button>
      </div>

      {/* Stats Cards */}
      <div className="grid gap-4 md:grid-cols-2 lg:grid-cols-6">
        <Card>
          <CardHeader className="flex flex-row items-center justify-between space-y-0 pb-2">
            <CardTitle className="text-sm font-medium">Total</CardTitle>
            <Bell className="h-4 w-4 text-muted-foreground" />
          </CardHeader>
          <CardContent>
            <div className="text-2xl font-bold">{displayStats.total}</div>
          </CardContent>
        </Card>

        <Card>
          <CardHeader className="flex flex-row items-center justify-between space-y-0 pb-2">
            <CardTitle className="text-sm font-medium">Unread</CardTitle>
            <Circle className="h-4 w-4 text-orange-500" />
          </CardHeader>
          <CardContent>
            <div className="text-2xl font-bold text-orange-600">{displayStats.unread}</div>
          </CardContent>
        </Card>

        <Card>
          <CardHeader className="flex flex-row items-center justify-between space-y-0 pb-2">
            <CardTitle className="text-sm font-medium">High Priority</CardTitle>
            <Bell className="h-4 w-4 text-red-500" />
          </CardHeader>
          <CardContent>
            <div className="text-2xl font-bold text-red-600">{displayStats.high}</div>
          </CardContent>
        </Card>

        <Card>
          <CardHeader className="flex flex-row items-center justify-between space-y-0 pb-2">
            <CardTitle className="text-sm font-medium">Delivered</CardTitle>
            <Check className="h-4 w-4 text-green-500" />
          </CardHeader>
          <CardContent>
            <div className="text-2xl font-bold text-green-600">{displayStats.delivered}</div>
          </CardContent>
        </Card>

        <Card>
          <CardHeader className="flex flex-row items-center justify-between space-y-0 pb-2">
            <CardTitle className="text-sm font-medium">Sent</CardTitle>
            <Mail className="h-4 w-4 text-blue-500" />
          </CardHeader>
          <CardContent>
            <div className="text-2xl font-bold text-blue-600">{displayStats.sent}</div>
          </CardContent>
        </Card>

        <Card>
          <CardHeader className="flex flex-row items-center justify-between space-y-0 pb-2">
            <CardTitle className="text-sm font-medium">Failed</CardTitle>
            <X className="h-4 w-4 text-red-500" />
          </CardHeader>
          <CardContent>
            <div className="text-2xl font-bold text-red-600">{displayStats.failed}</div>
          </CardContent>
        </Card>
      </div>

      {/* Notification Management */}
      <Tabs value={activeTab} onValueChange={setActiveTab}>
        <TabsList>
          <TabsTrigger value="inbox">Inbox</TabsTrigger>
          <TabsTrigger value="sent">Sent</TabsTrigger>
          <TabsTrigger value="templates">Templates</TabsTrigger>
          <TabsTrigger value="settings">Settings</TabsTrigger>
        </TabsList>

        <TabsContent value="inbox" className="space-y-4">
          {/* Filters */}
          <Card>
            <CardHeader>
              <div className="flex justify-between items-center">
                <CardTitle>Filter Notifications</CardTitle>
                <Button variant="outline" size="sm">
                  <Filter className="mr-2 h-4 w-4" />
                  Clear Filters
                </Button>
              </div>
            </CardHeader>
            <CardContent>
              <div className="flex gap-4">
                <div className="space-y-2">
                  <Label>Status</Label>
                  <select 
                    value={filterStatus} 
                    onChange={(e) => setFilterStatus(e.target.value)}
                    className="w-32 p-2 border rounded"
                  >
                    <option value="all">All</option>
                    <option value="unread">Unread</option>
                    <option value="read">Read</option>
                  </select>
                </div>
                <div className="space-y-2">
                  <Label>Type</Label>
                  <select 
                    value={filterType} 
                    onChange={(e) => setFilterType(e.target.value)}
                    className="w-32 p-2 border rounded"
                  >
                    <option value="all">All</option>
                    <option value="email">Email</option>
                    <option value="sms">SMS</option>
                    <option value="system">System</option>
                    <option value="reminder">Reminder</option>
                  </select>
                </div>
              </div>
            </CardContent>
          </Card>

          {/* Notifications List */}
          <Card>
            <CardHeader>
              <CardTitle>Notifications</CardTitle>
              <CardDescription>
                All system notifications and alerts
              </CardDescription>
            </CardHeader>
            <CardContent>
              <div className="space-y-4">
                {filteredNotifications.map((notification) => (
                  <div 
                    key={notification.id} 
                    className={`p-4 border rounded-lg ${
                      !notification.isRead ? 'bg-blue-50 border-blue-200' : 'bg-white'
                    }`}
                  >
                    <div className="flex items-start justify-between">
                      <div className="space-y-2 flex-1">
                        <div className="flex items-center gap-2">
                          {getTypeIcon(notification.type)}
                          <span className={`font-medium ${!notification.isRead ? 'font-bold' : ''}`}>
                            {notification.title}
                          </span>
                          {!notification.isRead && (
                            <Badge variant="secondary" className="text-xs">New</Badge>
                          )}
                        </div>
                        
                        <p className="text-sm text-muted-foreground">
                          {notification.message}
                        </p>
                        
                        <div className="flex items-center gap-2 text-xs text-muted-foreground">
                          <span>From: {notification.sender}</span>
                          <span>•</span>
                          <span>{format(new Date(notification.createdAt), 'MMM d, yyyy • h:mm a')}</span>
                          {notification.clientName && (
                            <>
                              <span>•</span>
                              <span>Client: {notification.clientName}</span>
                            </>
                          )}
                        </div>
                        
                        <div className="flex gap-2">
                          <Badge className={getStatusColor(notification.status)}>
                            {notification.status}
                          </Badge>
                          <Badge className={getPriorityColor(notification.priority)}>
                            {notification.priority} priority
                          </Badge>
                          <Badge className={getCategoryColor(notification.category)}>
                            {notification.category}
                          </Badge>
                        </div>
                      </div>
                      
                      <div className="flex gap-2 ml-4">
                        {notification.isRead ? (
                          <Button 
                            variant="outline" 
                            size="sm"
                            onClick={() => markAsUnread(notification.id)}
                          >
                            <Circle className="h-4 w-4" />
                          </Button>
                        ) : (
                          <Button 
                            variant="outline" 
                            size="sm"
                            onClick={() => markAsRead(notification.id)}
                          >
                            <Eye className="h-4 w-4" />
                          </Button>
                        )}
                        <Button 
                          variant="outline" 
                          size="sm"
                          onClick={() => deleteNotification(notification.id)}
                        >
                          <Trash2 className="h-4 w-4" />
                        </Button>
                      </div>
                    </div>
                  </div>
                ))}
                
                {filteredNotifications.length === 0 && (
                  <div className="text-center py-8">
                    <Bell className="mx-auto h-12 w-12 text-muted-foreground" />
                    <p className="text-muted-foreground mt-2">No notifications match your filters</p>
                  </div>
                )}
              </div>
            </CardContent>
          </Card>
        </TabsContent>

        <TabsContent value="sent" className="space-y-4">
          <Card>
            <CardHeader>
              <CardTitle>Sent Notifications</CardTitle>
              <CardDescription>History of notifications sent from the system</CardDescription>
            </CardHeader>
            <CardContent>
              <p className="text-muted-foreground text-center py-8">
                Sent notifications history will be displayed here
              </p>
            </CardContent>
          </Card>
        </TabsContent>

        <TabsContent value="templates" className="space-y-4">
          <Card>
            <CardHeader>
              <CardTitle>Notification Templates</CardTitle>
              <CardDescription>Manage email and SMS templates for automated notifications</CardDescription>
            </CardHeader>
            <CardContent>
              <p className="text-muted-foreground text-center py-8">
                Notification templates management will be available here
              </p>
            </CardContent>
          </Card>
        </TabsContent>

        <TabsContent value="settings" className="space-y-4">
          <Card>
            <CardHeader>
              <CardTitle>Notification Preferences</CardTitle>
              <CardDescription>Configure how and when you receive notifications</CardDescription>
            </CardHeader>
            <CardContent className="space-y-6">
              {/* Channel Settings */}
              <div className="space-y-4">
                <h4 className="font-medium">Notification Channels</h4>
                
                <div className="flex items-center justify-between">
                  <div className="space-y-0.5">
                    <Label>Email Notifications</Label>
                    <p className="text-sm text-muted-foreground">
                      Receive notifications via email
                    </p>
                  </div>
                  <Switch 
                    checked={settings?.emailEnabled || false}
                    onCheckedChange={(checked) => 
                      setSettings(prev => prev ? ({ ...prev, emailEnabled: checked }) : null)
                    }
                  />
                </div>
                
                <div className="flex items-center justify-between">
                  <div className="space-y-0.5">
                    <Label>SMS Notifications</Label>
                    <p className="text-sm text-muted-foreground">
                      Receive notifications via SMS
                    </p>
                  </div>
                  <Switch 
                    checked={settings?.smsEnabled || false}
                    onCheckedChange={(checked) => 
                      setSettings(prev => prev ? ({ ...prev, smsEnabled: checked }) : null)
                    }
                  />
                </div>
                
                <div className="flex items-center justify-between">
                  <div className="space-y-0.5">
                    <Label>System Notifications</Label>
                    <p className="text-sm text-muted-foreground">
                      Show notifications in the application
                    </p>
                  </div>
                  <Switch 
                    checked={settings?.systemEnabled || false}
                    onCheckedChange={(checked) => 
                      setSettings(prev => prev ? ({ ...prev, systemEnabled: checked }) : null)
                    }
                  />
                </div>
              </div>

              {/* Category Settings */}
              <div className="space-y-4">
                <h4 className="font-medium">Notification Categories</h4>
                
                <div className="flex items-center justify-between">
                  <div className="space-y-0.5">
                    <Label>Deadline Reminders</Label>
                    <p className="text-sm text-muted-foreground">
                      Reminders for upcoming tax deadlines
                    </p>
                  </div>
                  <Switch 
                    checked={settings?.deadlineReminders || false}
                    onCheckedChange={(checked) => 
                      setSettings(prev => prev ? ({ ...prev, deadlineReminders: checked }) : null)
                    }
                  />
                </div>
                
                <div className="flex items-center justify-between">
                  <div className="space-y-0.5">
                    <Label>Payment Alerts</Label>
                    <p className="text-sm text-muted-foreground">
                      Notifications for payment activities
                    </p>
                  </div>
                  <Switch 
                    checked={settings?.paymentAlerts || false}
                    onCheckedChange={(checked) => 
                      setSettings(prev => prev ? ({ ...prev, paymentAlerts: checked }) : null)
                    }
                  />
                </div>
                
                <div className="flex items-center justify-between">
                  <div className="space-y-0.5">
                    <Label>Compliance Alerts</Label>
                    <p className="text-sm text-muted-foreground">
                      Critical compliance and penalty notifications
                    </p>
                  </div>
                  <Switch 
                    checked={settings?.complianceAlerts || false}
                    onCheckedChange={(checked) => 
                      setSettings(prev => prev ? ({ ...prev, complianceAlerts: checked }) : null)
                    }
                  />
                </div>
                
                <div className="flex items-center justify-between">
                  <div className="space-y-0.5">
                    <Label>System Updates</Label>
                    <p className="text-sm text-muted-foreground">
                      System maintenance and update notifications
                    </p>
                  </div>
                  <Switch 
                    checked={settings?.systemUpdates || false}
                    onCheckedChange={(checked) => 
                      setSettings(prev => prev ? ({ ...prev, systemUpdates: checked }) : null)
                    }
                  />
                </div>
              </div>

              {/* Frequency Settings */}
              <div className="space-y-4">
                <h4 className="font-medium">Reminder Frequency</h4>
                <div className="space-y-2">
                  <Label>Default Reminder Schedule</Label>
                  <select 
                    value={settings?.reminderFrequency || '7days'}
                    onChange={(e) => setSettings(prev => prev ? ({ ...prev, reminderFrequency: e.target.value }) : null)}
                    className="w-full p-2 border rounded"
                  >
                    <option value="1day">1 Day Before</option>
                    <option value="3days">3 Days Before</option>
                    <option value="7days">7 Days Before</option>
                    <option value="14days">14 Days Before</option>
                    <option value="30days">30 Days Before</option>
                  </select>
                </div>
              </div>

              <Button className="w-full">
                Save Notification Settings
              </Button>
            </CardContent>
          </Card>
        </TabsContent>
      </Tabs>
    </div>
  )
}