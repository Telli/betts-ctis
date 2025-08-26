"use client"

import { useState, useEffect, useCallback } from 'react'
import { Card, CardContent, CardDescription, CardHeader, CardTitle } from '@/components/ui/card'
import { Badge } from '@/components/ui/badge'
import { Button } from '@/components/ui/button'
import { Progress } from '@/components/ui/progress'
import { 
  CheckCircle, 
  Clock, 
  AlertTriangle, 
  XCircle, 
  RefreshCw,
  Eye,
  Download,
  Copy,
  Bell,
  Smartphone,
  CreditCard,
  Building2
} from 'lucide-react'
import { cn } from '@/lib/utils'
import { useToast } from '@/components/ui/use-toast'
import { format } from 'date-fns'

export enum PaymentStatus {
  Pending = 'pending',
  Processing = 'processing',
  Confirmed = 'confirmed',
  Failed = 'failed',
  Cancelled = 'cancelled',
  Refunded = 'refunded'
}

export enum PaymentMethod {
  OrangeMoney = 'orange-money',
  AfricellMoney = 'africell-money',
  BankTransfer = 'bank-transfer',
  PayPal = 'paypal',
  Stripe = 'stripe'
}

interface PaymentTransaction {
  id: string
  reference: string
  amount: number
  fee: number
  method: PaymentMethod
  status: PaymentStatus
  createdAt: Date
  updatedAt: Date
  processedAt?: Date
  description?: string
  taxType?: string
  taxYear?: number
  transactionId?: string
  receiptUrl?: string
  errorMessage?: string
  retryCount?: number
  maxRetries?: number
  estimatedCompletion?: Date
  // Real-time updates
  lastStatusCheck?: Date
  isLive?: boolean
}

interface PaymentStatusTrackerProps {
  transaction: PaymentTransaction
  onStatusUpdate?: (transaction: PaymentTransaction) => void
  onRetry?: (transactionId: string) => void
  onCancel?: (transactionId: string) => void
  showActions?: boolean
  refreshInterval?: number // in milliseconds
}

export default function PaymentStatusTracker({
  transaction,
  onStatusUpdate,
  onRetry,
  onCancel,
  showActions = true,
  refreshInterval = 5000 // 5 seconds
}: PaymentStatusTrackerProps) {
  const { toast } = useToast()
  const [isRefreshing, setIsRefreshing] = useState(false)
  const [lastUpdate, setLastUpdate] = useState<Date>(new Date())
  const [autoRefresh, setAutoRefresh] = useState(false)

  // Auto-refresh for pending/processing payments
  useEffect(() => {
    if (transaction.status === PaymentStatus.Pending || transaction.status === PaymentStatus.Processing) {
      setAutoRefresh(true)
    } else {
      setAutoRefresh(false)
    }
  }, [transaction.status])

  // Auto-refresh timer
  useEffect(() => {
    if (!autoRefresh) return

    const interval = setInterval(() => {
      handleRefresh()
    }, refreshInterval)

    return () => clearInterval(interval)
  }, [autoRefresh, refreshInterval])

  const handleRefresh = useCallback(async () => {
    if (isRefreshing) return

    setIsRefreshing(true)
    try {
      // Simulate API call to check payment status
      await new Promise(resolve => setTimeout(resolve, 1000))
      
      // Mock status progression
      let newStatus = transaction.status
      if (transaction.status === PaymentStatus.Pending) {
        // 30% chance to move to processing
        if (Math.random() < 0.3) {
          newStatus = PaymentStatus.Processing
        }
      } else if (transaction.status === PaymentStatus.Processing) {
        // 40% chance to complete, 10% chance to fail
        const rand = Math.random()
        if (rand < 0.4) {
          newStatus = PaymentStatus.Confirmed
        } else if (rand < 0.5) {
          newStatus = PaymentStatus.Failed
        }
      }

      if (newStatus !== transaction.status) {
        const updatedTransaction = {
          ...transaction,
          status: newStatus,
          updatedAt: new Date(),
          processedAt: newStatus === PaymentStatus.Confirmed ? new Date() : transaction.processedAt,
          errorMessage: newStatus === PaymentStatus.Failed ? 'Payment processing failed' : undefined
        }

        onStatusUpdate?.(updatedTransaction)
        
        if (newStatus === PaymentStatus.Confirmed) {
          toast({
            title: 'Payment Confirmed',
            description: `Payment ${transaction.reference} has been successfully processed`,
          })
        } else if (newStatus === PaymentStatus.Failed) {
          toast({
            variant: 'destructive',
            title: 'Payment Failed',
            description: `Payment ${transaction.reference} could not be processed`,
          })
        }
      }

      setLastUpdate(new Date())
    } catch (error) {
      console.error('Failed to refresh payment status:', error)
      toast({
        variant: 'destructive',
        title: 'Refresh Failed',
        description: 'Could not update payment status. Please try again.',
      })
    } finally {
      setIsRefreshing(false)
    }
  }, [transaction, onStatusUpdate, isRefreshing, toast])

  const getStatusIcon = (status: PaymentStatus) => {
    switch (status) {
      case PaymentStatus.Pending:
        return <Clock className="h-5 w-5 text-yellow-500" />
      case PaymentStatus.Processing:
        return <div className="animate-spin rounded-full h-5 w-5 border-b-2 border-blue-500" />
      case PaymentStatus.Confirmed:
        return <CheckCircle className="h-5 w-5 text-green-500" />
      case PaymentStatus.Failed:
        return <XCircle className="h-5 w-5 text-red-500" />
      case PaymentStatus.Cancelled:
        return <XCircle className="h-5 w-5 text-gray-500" />
      case PaymentStatus.Refunded:
        return <RefreshCw className="h-5 w-5 text-orange-500" />
      default:
        return <Clock className="h-5 w-5 text-gray-500" />
    }
  }

  const getStatusColor = (status: PaymentStatus) => {
    switch (status) {
      case PaymentStatus.Pending:
        return 'bg-yellow-100 text-yellow-800 border-yellow-200'
      case PaymentStatus.Processing:
        return 'bg-blue-100 text-blue-800 border-blue-200'
      case PaymentStatus.Confirmed:
        return 'bg-green-100 text-green-800 border-green-200'
      case PaymentStatus.Failed:
        return 'bg-red-100 text-red-800 border-red-200'
      case PaymentStatus.Cancelled:
        return 'bg-gray-100 text-gray-800 border-gray-200'
      case PaymentStatus.Refunded:
        return 'bg-orange-100 text-orange-800 border-orange-200'
      default:
        return 'bg-gray-100 text-gray-800 border-gray-200'
    }
  }

  const getMethodIcon = (method: PaymentMethod) => {
    switch (method) {
      case PaymentMethod.OrangeMoney:
      case PaymentMethod.AfricellMoney:
        return <Smartphone className="h-4 w-4" />
      case PaymentMethod.BankTransfer:
        return <Building2 className="h-4 w-4" />
      case PaymentMethod.PayPal:
      case PaymentMethod.Stripe:
        return <CreditCard className="h-4 w-4" />
      default:
        return <CreditCard className="h-4 w-4" />
    }
  }

  const getProgressValue = (status: PaymentStatus) => {
    switch (status) {
      case PaymentStatus.Pending:
        return 25
      case PaymentStatus.Processing:
        return 75
      case PaymentStatus.Confirmed:
        return 100
      case PaymentStatus.Failed:
      case PaymentStatus.Cancelled:
        return 0
      case PaymentStatus.Refunded:
        return 50
      default:
        return 0
    }
  }

  const copyToClipboard = (text: string) => {
    navigator.clipboard.writeText(text)
    toast({
      title: 'Copied',
      description: 'Transaction reference copied to clipboard',
    })
  }

  const canRetry = transaction.status === PaymentStatus.Failed && 
                  (transaction.retryCount || 0) < (transaction.maxRetries || 3)

  const canCancel = transaction.status === PaymentStatus.Pending || 
                   transaction.status === PaymentStatus.Processing

  return (
    <Card className="w-full">
      <CardHeader>
        <div className="flex items-center justify-between">
          <div className="flex items-center gap-3">
            {getStatusIcon(transaction.status)}
            <div>
              <CardTitle className="text-lg">Payment Status</CardTitle>
              <CardDescription className="flex items-center gap-2">
                <span>Reference: {transaction.reference}</span>
                <Button
                  variant="ghost"
                  size="sm"
                  onClick={() => copyToClipboard(transaction.reference)}
                  className="h-6 w-6 p-0"
                >
                  <Copy className="h-3 w-3" />
                </Button>
              </CardDescription>
            </div>
          </div>
          
          <div className="flex items-center gap-2">
            <Badge className={cn("border", getStatusColor(transaction.status))}>
              {transaction.status.charAt(0).toUpperCase() + transaction.status.slice(1)}
            </Badge>
            
            {autoRefresh && (
              <Badge variant="outline" className="text-xs">
                <Bell className="h-3 w-3 mr-1" />
                Live
              </Badge>
            )}
          </div>
        </div>
      </CardHeader>

      <CardContent className="space-y-4">
        {/* Progress Bar */}
        <div className="space-y-2">
          <div className="flex justify-between text-sm text-muted-foreground">
            <span>Progress</span>
            <span>{getProgressValue(transaction.status)}%</span>
          </div>
          <Progress 
            value={getProgressValue(transaction.status)} 
            className="h-2"
          />
        </div>

        {/* Transaction Details */}
        <div className="grid grid-cols-2 gap-4 text-sm">
          <div>
            <div className="text-muted-foreground">Amount</div>
            <div className="font-medium">Le {transaction.amount.toLocaleString()}</div>
          </div>
          
          <div>
            <div className="text-muted-foreground">Fee</div>
            <div className="font-medium">Le {transaction.fee.toLocaleString()}</div>
          </div>
          
          <div>
            <div className="text-muted-foreground">Method</div>
            <div className="flex items-center gap-1 font-medium">
              {getMethodIcon(transaction.method)}
              {transaction.method.replace('-', ' ').replace(/\b\w/g, l => l.toUpperCase())}
            </div>
          </div>
          
          <div>
            <div className="text-muted-foreground">Created</div>
            <div className="font-medium">{format(transaction.createdAt, 'MMM d, yyyy HH:mm')}</div>
          </div>
          
          {transaction.processedAt && (
            <>
              <div>
                <div className="text-muted-foreground">Processed</div>
                <div className="font-medium">{format(transaction.processedAt, 'MMM d, yyyy HH:mm')}</div>
              </div>
            </>
          )}
          
          {transaction.taxType && (
            <div>
              <div className="text-muted-foreground">Tax Type</div>
              <div className="font-medium">{transaction.taxType} {transaction.taxYear}</div>
            </div>
          )}
        </div>

        {/* Error Message */}
        {transaction.status === PaymentStatus.Failed && transaction.errorMessage && (
          <div className="bg-red-50 border border-red-200 rounded-lg p-3">
            <div className="flex items-start gap-2">
              <AlertTriangle className="h-4 w-4 text-red-500 mt-0.5 flex-shrink-0" />
              <div>
                <div className="font-medium text-red-800 text-sm">Payment Failed</div>
                <div className="text-red-700 text-sm">{transaction.errorMessage}</div>
                {transaction.retryCount && (
                  <div className="text-red-600 text-xs mt-1">
                    Retry attempts: {transaction.retryCount}/{transaction.maxRetries || 3}
                  </div>
                )}
              </div>
            </div>
          </div>
        )}

        {/* Estimated Completion */}
        {transaction.estimatedCompletion && transaction.status === PaymentStatus.Processing && (
          <div className="bg-blue-50 border border-blue-200 rounded-lg p-3">
            <div className="flex items-center gap-2">
              <Clock className="h-4 w-4 text-blue-500" />
              <div className="text-blue-800 text-sm">
                <div className="font-medium">Estimated Completion</div>
                <div>{format(transaction.estimatedCompletion, 'MMM d, yyyy HH:mm')}</div>
              </div>
            </div>
          </div>
        )}

        {/* Actions */}
        {showActions && (
          <div className="flex flex-wrap gap-2 pt-2">
            <Button
              variant="outline"
              size="sm"
              onClick={handleRefresh}
              disabled={isRefreshing}
            >
              <RefreshCw className={cn("h-4 w-4 mr-2", isRefreshing && "animate-spin")} />
              Refresh
            </Button>

            {transaction.receiptUrl && (
              <Button variant="outline" size="sm">
                <Download className="h-4 w-4 mr-2" />
                Receipt
              </Button>
            )}

            <Button variant="outline" size="sm">
              <Eye className="h-4 w-4 mr-2" />
              Details
            </Button>

            {canRetry && onRetry && (
              <Button
                variant="outline"
                size="sm"
                onClick={() => onRetry(transaction.id)}
                className="text-orange-600 border-orange-200 hover:bg-orange-50"
              >
                <RefreshCw className="h-4 w-4 mr-2" />
                Retry Payment
              </Button>
            )}

            {canCancel && onCancel && (
              <Button
                variant="outline"
                size="sm"
                onClick={() => onCancel(transaction.id)}
                className="text-red-600 border-red-200 hover:bg-red-50"
              >
                <XCircle className="h-4 w-4 mr-2" />
                Cancel
              </Button>
            )}
          </div>
        )}

        {/* Last Update */}
        <div className="text-xs text-muted-foreground border-t pt-2">
          Last updated: {format(lastUpdate, 'MMM d, yyyy HH:mm:ss')}
          {autoRefresh && (
            <span className="ml-2">• Auto-refreshing every {refreshInterval / 1000}s</span>
          )}
        </div>
      </CardContent>
    </Card>
  )
}