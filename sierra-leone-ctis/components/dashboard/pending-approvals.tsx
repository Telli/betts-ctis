import { Card, CardContent, CardDescription, CardHeader, CardTitle } from '@/components/ui/card'
import { PendingApproval } from '@/lib/services'
import { formatDate } from '@/lib/utils'
import { Badge } from '@/components/ui/badge'
import { Button } from '@/components/ui/button'
import Link from 'next/link'
import { ExternalLink, Clock, DollarSign } from 'lucide-react'

interface PendingApprovalsProps {
  approvals: PendingApproval[]
  className?: string
}

export default function PendingApprovals({ approvals = [], className = '' }: PendingApprovalsProps) {
  return (
    <Card className={className}>
      <CardHeader>
        <div className="flex items-center justify-between">
          <div>
            <CardTitle className="flex items-center gap-2">
              <Clock className="h-5 w-5 text-sierra-gold" />
              Pending Approvals
            </CardTitle>
            <CardDescription>
              Payment approvals awaiting review
            </CardDescription>
          </div>
          <Link href="/payments">
            <Button variant="outline" size="sm">
              <ExternalLink className="h-4 w-4 mr-1" />
              View All
            </Button>
          </Link>
        </div>
      </CardHeader>
      <CardContent>
        {approvals.length === 0 ? (
          <div className="text-center py-6">
            <Clock className="h-12 w-12 text-muted-foreground mx-auto mb-4" />
            <p className="text-sm text-muted-foreground">No pending approvals</p>
            <p className="text-xs text-muted-foreground mt-1">All payments are up to date</p>
          </div>
        ) : (
          <div className="space-y-4">
            {approvals.slice(0, 5).map((approval) => (
              <div key={`${approval.type}-${approval.id}`} className="border rounded-lg p-4 hover:bg-sierra-blue/5 transition-colors">
                <div className="flex justify-between items-start mb-3">
                  <div className="flex items-center gap-2">
                    <Badge className="bg-sierra-gold text-white">
                      {approval.type}
                    </Badge>
                    <div className="flex items-center gap-1 text-sm font-semibold">
                      <DollarSign className="h-4 w-4 text-muted-foreground" />
                      {approval.amount.toLocaleString('en-US', {
                        style: 'currency',
                        currency: 'SLE'
                      })}
                    </div>
                  </div>
                  <div className="text-xs text-muted-foreground">
                    {formatDate(new Date(approval.submittedDate))}
                  </div>
                </div>
                
                <Link 
                  href={`/clients/${approval.clientId}`}
                  className="text-sm font-medium text-sierra-blue hover:underline block mb-2"
                >
                  {approval.clientName}
                </Link>
                
                {approval.description && (
                  <p className="text-sm text-muted-foreground mb-3">{approval.description}</p>
                )}
                
                <div className="flex items-center justify-between">
                  <div className="text-xs text-muted-foreground">
                    Submitted by: {approval.submittedBy}
                  </div>
                  <Link href="/payments">
                    <Button size="sm" variant="outline" className="text-xs">
                      Review
                    </Button>
                  </Link>
                </div>
              </div>
            ))}
            
            {approvals.length > 5 && (
              <div className="text-center pt-4">
                <Link href="/payments">
                  <Button variant="outline" size="sm">
                    View {approvals.length - 5} more approvals
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
