import { Card, CardContent, CardDescription, CardHeader, CardTitle } from '@/components/ui/card'
import { RecentActivity } from '@/lib/services'
import { formatDate } from '@/lib/utils'
import { Badge } from '@/components/ui/badge'
import Link from 'next/link'

interface RecentActivityListProps {
  activities: RecentActivity[]
  className?: string
}

export default function RecentActivityList({ activities, className = '' }: RecentActivityListProps) {
  // Map activity types to colors and icons
  const getActivityMeta = (type: string) => {
    switch (type) {
      case 'document':
        return { 
          color: 'bg-blue-100 text-blue-800 dark:bg-blue-900 dark:text-blue-300',
          icon: (
            <svg className="h-4 w-4" xmlns="http://www.w3.org/2000/svg" viewBox="0 0 20 20" fill="currentColor">
              <path fillRule="evenodd" d="M4 4a2 2 0 012-2h4.586A2 2 0 0112 2.586L15.414 6A2 2 0 0116 7.414V16a2 2 0 01-2 2H6a2 2 0 01-2-2V4z" clipRule="evenodd" />
            </svg>
          )
        }
      case 'payment':
        return { 
          color: 'bg-green-100 text-green-800 dark:bg-green-900 dark:text-green-300',
          icon: (
            <svg className="h-4 w-4" xmlns="http://www.w3.org/2000/svg" viewBox="0 0 20 20" fill="currentColor">
              <path d="M4 4a2 2 0 00-2 2v1h16V6a2 2 0 00-2-2H4z" />
              <path fillRule="evenodd" d="M18 9H2v5a2 2 0 002 2h12a2 2 0 002-2V9zM4 13a1 1 0 011-1h1a1 1 0 110 2H5a1 1 0 01-1-1zm5-1a1 1 0 100 2h1a1 1 0 100-2H9z" clipRule="evenodd" />
            </svg>
          )
        }
      case 'client':
        return { 
          color: 'bg-purple-100 text-purple-800 dark:bg-purple-900 dark:text-purple-300',
          icon: (
            <svg className="h-4 w-4" xmlns="http://www.w3.org/2000/svg" viewBox="0 0 20 20" fill="currentColor">
              <path d="M13 6a3 3 0 11-6 0 3 3 0 016 0zM18 8a2 2 0 11-4 0 2 2 0 014 0zM14 15a4 4 0 00-8 0v3h8v-3zM6 8a2 2 0 11-4 0 2 2 0 014 0zM16 18v-3a5.972 5.972 0 00-.75-2.906A3.005 3.005 0 0119 15v3h-3zM4.75 12.094A5.973 5.973 0 004 15v3H1v-3a3 3 0 013.75-2.906z" />
            </svg>
          )
        }
      case 'filing':
        return { 
          color: 'bg-amber-100 text-amber-800 dark:bg-amber-900 dark:text-amber-300',
          icon: (
            <svg className="h-4 w-4" xmlns="http://www.w3.org/2000/svg" viewBox="0 0 20 20" fill="currentColor">
              <path fillRule="evenodd" d="M2 5a2 2 0 012-2h12a2 2 0 012 2v10a2 2 0 01-2 2H4a2 2 0 01-2-2V5zm3.293 1.293a1 1 0 011.414 0l3 3a1 1 0 010 1.414l-3 3a1 1 0 01-1.414-1.414L7.586 10 5.293 7.707a1 1 0 010-1.414zM11 12a1 1 0 100 2h3a1 1 0 100-2h-3z" clipRule="evenodd" />
            </svg>
          )
        }
      default:
        return { 
          color: 'bg-gray-100 text-gray-800 dark:bg-gray-800 dark:text-gray-300',
          icon: (
            <svg className="h-4 w-4" xmlns="http://www.w3.org/2000/svg" viewBox="0 0 20 20" fill="currentColor">
              <path fillRule="evenodd" d="M18 10a8 8 0 11-16 0 8 8 0 0116 0zm-7-4a1 1 0 11-2 0 1 1 0 012 0zM9 9a1 1 0 000 2v3a1 1 0 001 1h1a1 1 0 100-2v-3a1 1 0 00-1-1H9z" clipRule="evenodd" />
            </svg>
          )
        }
    }
  }
  
  // Helper to get link for different activity types
  const getActivityLink = (activity: RecentActivity) => {
    switch (activity.type) {
      case 'document':
        return activity.clientId ? `/clients/${activity.clientId}?tab=documents` : '/documents'
      case 'payment':
        return activity.clientId ? `/clients/${activity.clientId}` : '/clients'
      case 'client':
        return `/clients/${activity.clientId}`
      case 'filing':
        return activity.clientId ? `/clients/${activity.clientId}` : '/clients'
      default:
        return '#'
    }
  }

  return (
    <Card className={className}>
      <CardHeader>
        <CardTitle>Recent Activity</CardTitle>
        <CardDescription>
          Latest actions across the platform
        </CardDescription>
      </CardHeader>
      <CardContent>
        {activities.length === 0 ? (
          <p className="text-sm text-muted-foreground text-center py-4">No recent activities found</p>
        ) : (
          <div className="space-y-5">
            {activities.map((activity) => {
              const { color, icon } = getActivityMeta(activity.type)
              return (
                <div key={`${activity.type}-${activity.id}`} className="flex items-start space-x-4">
                  <div className={`${color} p-2 rounded-full`}>
                    {icon}
                  </div>
                  <div className="space-y-1 flex-1">
                    <div className="flex items-center justify-between">
                      <Link 
                        href={getActivityLink(activity)} 
                        className="text-sm font-medium hover:underline"
                      >
                        {activity.description}
                      </Link>
                      <span className="text-xs text-muted-foreground">
                        {formatDate(new Date(activity.timestamp))}
                      </span>
                    </div>
                    <div className="flex items-center text-xs text-muted-foreground">
                      {activity.clientName && (
                        <Link 
                          href={activity.clientId ? `/clients/${activity.clientId}` : '/clients'} 
                          className="hover:underline mr-2"
                        >
                          {activity.clientName}
                        </Link>
                      )}
                      {activity.action && (
                        <Badge variant="outline" className="ml-auto">
                          {activity.action}
                        </Badge>
                      )}
                    </div>
                  </div>
                </div>
              )
            })}
          </div>
        )}
      </CardContent>
    </Card>
  )
}
