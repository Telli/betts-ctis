import { Card, CardContent, CardDescription, CardHeader, CardTitle } from '@/components/ui/card'
import { UpcomingDeadline } from '@/lib/services'
import { formatDate } from '@/lib/utils'
import { Badge } from '@/components/ui/badge'
import { Button } from '@/components/ui/button'
import Link from 'next/link'
import { Calendar, ExternalLink, AlertTriangle, Clock } from 'lucide-react'

interface UpcomingDeadlinesProps {
  deadlines: UpcomingDeadline[]
  className?: string
}

export default function UpcomingDeadlines({ deadlines, className = '' }: UpcomingDeadlinesProps) {
  // Helper function to determine deadline badge styling
  const getDeadlineBadge = (daysRemaining: number) => {
    if (daysRemaining <= 3) {
      return <Badge variant="destructive" className="flex items-center gap-1">
        <AlertTriangle className="h-3 w-3" />
        Urgent
      </Badge>
    } else if (daysRemaining <= 7) {
      return <Badge className="bg-sierra-gold text-white flex items-center gap-1">
        <Clock className="h-3 w-3" />
        Soon
      </Badge>
    } else {
      return <Badge variant="outline" className="flex items-center gap-1">
        <Calendar className="h-3 w-3" />
        {daysRemaining} days
      </Badge>
    }
  }

  return (
    <Card className={className}>
      <CardHeader>
        <div className="flex items-center justify-between">
          <div>
            <CardTitle className="flex items-center gap-2">
              <Calendar className="h-5 w-5 text-sierra-blue" />
              Upcoming Deadlines
            </CardTitle>
            <CardDescription>
              Tax filing deadlines in the next 30 days
            </CardDescription>
          </div>
          <Link href="/tax-filings">
            <Button variant="outline" size="sm">
              <ExternalLink className="h-4 w-4 mr-1" />
              View All
            </Button>
          </Link>
        </div>
      </CardHeader>
      <CardContent>
        {deadlines.length === 0 ? (
          <div className="text-center py-6">
            <Calendar className="h-12 w-12 text-muted-foreground mx-auto mb-4" />
            <p className="text-sm text-muted-foreground">No upcoming deadlines</p>
            <p className="text-xs text-muted-foreground mt-1">All filings are up to date</p>
          </div>
        ) : (
          <div className="space-y-4">
            {deadlines.slice(0, 5).map((deadline) => (
              <div key={`${deadline.type}-${deadline.id}`} className="border rounded-lg p-3 hover:bg-sierra-blue/5 transition-colors">
                <div className="flex items-start gap-3">
                  <div className={`mt-1 w-3 h-3 rounded-full flex-shrink-0 ${
                    deadline.isUrgent ? 'bg-red-500' : 
                    deadline.daysRemaining <= 7 ? 'bg-sierra-gold' : 'bg-sierra-blue'
                  }`} />
                  <div className="space-y-2 flex-1 min-w-0">
                    <div className="flex items-center justify-between gap-2">
                      <Link 
                        href={deadline.clientId ? `/clients/${deadline.clientId}` : '/tax-filings'}
                        className="text-sm font-medium text-sierra-blue hover:underline truncate"
                      >
                        {deadline.title}
                      </Link>
                      {getDeadlineBadge(deadline.daysRemaining)}
                    </div>
                    
                    <div className="flex justify-between items-center text-xs text-muted-foreground">
                      <div className="flex items-center gap-1">
                        <Calendar className="h-3 w-3" />
                        <span>Due: {formatDate(new Date(deadline.dueDate))}</span>
                      </div>
                      {deadline.clientName && (
                        <Link 
                          href={deadline.clientId ? `/clients/${deadline.clientId}` : '/clients'}
                          className="hover:underline text-sierra-blue truncate max-w-[120px]"
                          title={deadline.clientName}
                        >
                          {deadline.clientName}
                        </Link>
                      )}
                    </div>
                    
                    {deadline.description && (
                      <p className="text-xs text-muted-foreground line-clamp-2">{deadline.description}</p>
                    )}
                    
                    <div className="flex items-center justify-between">
                      <Badge variant="outline" className="text-xs">
                        {deadline.type}
                      </Badge>
                      <Link href="/tax-filings">
                        <Button size="sm" variant="ghost" className="text-xs h-6 px-2">
                          View Filing
                        </Button>
                      </Link>
                    </div>
                  </div>
                </div>
              </div>
            ))}
            
            {deadlines.length > 5 && (
              <div className="text-center pt-4">
                <Link href="/tax-filings">
                  <Button variant="outline" size="sm">
                    View {deadlines.length - 5} more deadlines
                  </Button>
                </Link>
              </div>
            )}
          </div>
        )}
      </CardContent>
    </Card>
  )
}
