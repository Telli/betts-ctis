"use client"

import { useState } from 'react'
import { Card, CardContent, CardDescription, CardHeader, CardTitle } from '@/components/ui/card'
import { Button } from '@/components/ui/button'
import { Badge } from '@/components/ui/badge'
import { Textarea } from '@/components/ui/textarea'
import { RadioGroup, RadioGroupItem } from '@/components/ui/radio-group'
import { Label } from '@/components/ui/label'
import { Separator } from '@/components/ui/separator'
import { Alert, AlertDescription } from '@/components/ui/alert'
import { 
  RefreshCw, 
  AlertTriangle, 
  Clock, 
  CheckCircle,
  XCircle,
  Info,
  ArrowRight,
  Settings,
  CreditCard,
  Smartphone,
  Building2
} from 'lucide-react'
import { format } from 'date-fns'
import { useToast } from '@/components/ui/use-toast'
import { PaymentStatus, PaymentMethod } from './PaymentStatusTracker'
import { cn } from '@/lib/utils'

interface FailedPayment {
  id: string
  reference: string
  amount: number
  fee: number
  method: PaymentMethod
  status: PaymentStatus
  createdAt: Date
  failedAt: Date
  errorCode?: string
  errorMessage: string
  retryCount: number
  maxRetries: number
  canRetry: boolean
  suggestedActions: string[]
  // Payment details
  taxType?: string
  taxYear?: number
  clientName: string
  // Failure analysis
  failureCategory: 'network' | 'provider' | 'validation' | 'insufficient_funds' | 'security' | 'system'
  isTemporary: boolean
  estimatedRetryTime?: Date
}

interface PaymentRetryInterfaceProps {
  payment: FailedPayment
  onRetry: (paymentId: string, options: RetryOptions) => Promise<void>
  onCancel: (paymentId: string, reason: string) => Promise<void>
  onMethodChange: (paymentId: string, newMethod: PaymentMethod) => void
  loading?: boolean
}

interface RetryOptions {
  retryMethod: 'same' | 'different'
  newMethod?: PaymentMethod
  delay?: number // minutes
  notes?: string
}

export default function PaymentRetryInterface({
  payment,
  onRetry,
  onCancel,
  onMethodChange,
  loading = false
}: PaymentRetryInterfaceProps) {
  const { toast } = useToast()
  const [retryMethod, setRetryMethod] = useState<'same' | 'different'>('same')
  const [newPaymentMethod, setNewPaymentMethod] = useState<PaymentMethod>(payment.method)
  const [retryDelay, setRetryDelay] = useState<number>(0)
  const [notes, setNotes] = useState('')
  const [cancelReason, setCancelReason] = useState('')
  const [showCancelForm, setShowCancelForm] = useState(false)
  const [isRetrying, setIsRetrying] = useState(false)

  const getFailureCategoryInfo = (category: string) => {
    switch (category) {
      case 'network':
        return {
          icon: <AlertTriangle className="h-4 w-4 text-yellow-500" />,
          title: 'Network Issue',
          description: 'Connection problems with payment provider',
          severity: 'warning',
          retryRecommended: true
        }
      case 'provider':
        return {
          icon: <XCircle className="h-4 w-4 text-red-500" />,
          title: 'Provider Error',
          description: 'Payment provider service is unavailable',
          severity: 'error',
          retryRecommended: true
        }
      case 'insufficient_funds':
        return {
          icon: <AlertTriangle className="h-4 w-4 text-orange-500" />,
          title: 'Insufficient Funds',
          description: 'Not enough balance in payment account',
          severity: 'warning',
          retryRecommended: false
        }
      case 'validation':
        return {
          icon: <Info className="h-4 w-4 text-blue-500" />,
          title: 'Validation Error',
          description: 'Payment details need to be corrected',
          severity: 'info',
          retryRecommended: false
        }
      case 'security':
        return {
          icon: <XCircle className="h-4 w-4 text-red-500" />,
          title: 'Security Block',
          description: 'Payment blocked for security reasons',
          severity: 'error',
          retryRecommended: false
        }
      case 'system':
        return {
          icon: <Settings className="h-4 w-4 text-gray-500" />,
          title: 'System Error',
          description: 'Internal system processing error',
          severity: 'error',
          retryRecommended: true
        }
      default:
        return {
          icon: <AlertTriangle className="h-4 w-4 text-gray-500" />,
          title: 'Unknown Error',
          description: 'Unidentified payment failure',
          severity: 'warning',
          retryRecommended: true
        }
    }
  }

  const getMethodIcon = (method: PaymentMethod) => {
    switch (method) {
      case PaymentMethod.OrangeMoney:
      case PaymentMethod.AfricellMoney:
        return <Smartphone className="h-4 w-4" />
      case PaymentMethod.BankTransfer:
        return <Building2 className="h-4 w-4" />
      default:
        return <CreditCard className="h-4 w-4" />
    }
  }

  const getMethodName = (method: PaymentMethod) => {
    switch (method) {
      case PaymentMethod.OrangeMoney:
        return 'Orange Money'
      case PaymentMethod.AfricellMoney:
        return 'Africell Money'
      case PaymentMethod.BankTransfer:
        return 'Bank Transfer'
      case PaymentMethod.PayPal:
        return 'PayPal'
      case PaymentMethod.Stripe:
        return 'Credit/Debit Card'
      default:
        return String(method).replace('-', ' ').replace(/\b\w/g, l => l.toUpperCase())
    }
  }

  const handleRetry = async () => {
    setIsRetrying(true)
    try {
      const options: RetryOptions = {
        retryMethod,
        newMethod: retryMethod === 'different' ? newPaymentMethod : undefined,
        delay: retryDelay,
        notes: notes.trim() || undefined
      }

      await onRetry(payment.id, options)
      
      toast({
        title: 'Retry Initiated',
        description: `Payment retry has been queued ${retryDelay > 0 ? `with ${retryDelay} minute delay` : 'for immediate processing'}`,
      })
    } catch (error) {
      console.error('Retry failed:', error)
      toast({
        variant: 'destructive',
        title: 'Retry Failed',
        description: 'Could not initiate payment retry. Please try again.',
      })
    } finally {
      setIsRetrying(false)
    }
  }

  const handleCancel = async () => {
    if (!cancelReason.trim()) {
      toast({
        variant: 'destructive',
        title: 'Cancellation Reason Required',
        description: 'Please provide a reason for cancelling this payment.',
      })
      return
    }

    try {
      await onCancel(payment.id, cancelReason)
      toast({
        title: 'Payment Cancelled',
        description: 'The failed payment has been cancelled.',
      })
    } catch (error) {
      console.error('Cancellation failed:', error)
      toast({
        variant: 'destructive',
        title: 'Cancellation Failed',
        description: 'Could not cancel payment. Please try again.',
      })
    }
  }

  const failureInfo = getFailureCategoryInfo(payment.failureCategory)
  const remainingRetries = payment.maxRetries - payment.retryCount

  return (
    <div className="space-y-4">
      {/* Payment Summary */}
      <Card>
        <CardHeader>
          <div className="flex items-center justify-between">
            <div className="flex items-center gap-3">
              <XCircle className="h-6 w-6 text-red-500" />
              <div>
                <CardTitle className="text-red-700">Payment Failed</CardTitle>
                <CardDescription>
                  Reference: {payment.reference} • Failed on {format(payment.failedAt, 'MMM d, yyyy HH:mm')}
                </CardDescription>
              </div>
            </div>
            <Badge variant="destructive">
              {payment.status}
            </Badge>
          </div>
        </CardHeader>

        <CardContent className="space-y-4">
          {/* Payment Details */}
          <div className="grid grid-cols-2 gap-4 text-sm">
            <div>
              <span className="text-muted-foreground">Amount:</span>
              <span className="ml-2 font-medium">Le {payment.amount.toLocaleString()}</span>
            </div>
            <div>
              <span className="text-muted-foreground">Client:</span>
              <span className="ml-2 font-medium">{payment.clientName}</span>
            </div>
            <div>
              <span className="text-muted-foreground">Method:</span>
              <span className="ml-2 flex items-center gap-1 font-medium">
                {getMethodIcon(payment.method)}
                {getMethodName(payment.method)}
              </span>
            </div>
            {payment.taxType && (
              <div>
                <span className="text-muted-foreground">Tax Type:</span>
                <span className="ml-2 font-medium">{payment.taxType} {payment.taxYear}</span>
              </div>
            )}
          </div>

          {/* Failure Information */}
          <Alert className={cn(
            failureInfo.severity === 'error' && "border-red-200 bg-red-50",
            failureInfo.severity === 'warning' && "border-yellow-200 bg-yellow-50",
            failureInfo.severity === 'info' && "border-blue-200 bg-blue-50"
          )}>
            <div className="flex items-start gap-3">
              {failureInfo.icon}
              <div className="flex-1">
                <div className="font-medium">{failureInfo.title}</div>
                <AlertDescription className="mt-1">
                  {failureInfo.description}
                </AlertDescription>
                <div className="mt-2 text-sm">
                  <strong>Error:</strong> {payment.errorMessage}
                </div>
                {payment.errorCode && (
                  <div className="text-xs text-muted-foreground mt-1">
                    Error Code: {payment.errorCode}
                  </div>
                )}
              </div>
            </div>
          </Alert>

          {/* Retry Information */}
          <div className="flex items-center justify-between p-3 bg-gray-50 rounded-lg">
            <div className="flex items-center gap-2">
              <RefreshCw className="h-4 w-4 text-muted-foreground" />
              <span className="text-sm">
                Retry attempts: {payment.retryCount}/{payment.maxRetries}
              </span>
            </div>
            <div className="flex items-center gap-2">
              {payment.isTemporary && (
                <Badge variant="outline" className="text-xs">
                  <Clock className="h-3 w-3 mr-1" />
                  Temporary Issue
                </Badge>
              )}
              {failureInfo.retryRecommended && (
                <Badge variant="outline" className="text-xs text-green-700 border-green-200">
                  <CheckCircle className="h-3 w-3 mr-1" />
                  Retry Recommended
                </Badge>
              )}
            </div>
          </div>

          {/* Suggested Actions */}
          {payment.suggestedActions.length > 0 && (
            <div>
              <h4 className="font-medium text-sm mb-2">Suggested Actions:</h4>
              <ul className="text-sm text-muted-foreground space-y-1">
                {payment.suggestedActions.map((action, index) => (
                  <li key={index} className="flex items-start gap-2">
                    <ArrowRight className="h-3 w-3 mt-0.5 flex-shrink-0" />
                    {action}
                  </li>
                ))}
              </ul>
            </div>
          )}
        </CardContent>
      </Card>

      {/* Retry Options */}
      {payment.canRetry && remainingRetries > 0 && !showCancelForm && (
        <Card>
          <CardHeader>
            <CardTitle className="flex items-center gap-2">
              <RefreshCw className="h-5 w-5" />
              Retry Payment
            </CardTitle>
            <CardDescription>
              Choose how you want to retry this payment ({remainingRetries} attempts remaining)
            </CardDescription>
          </CardHeader>

          <CardContent className="space-y-4">
            <RadioGroup value={retryMethod} onValueChange={(value: 'same' | 'different') => setRetryMethod(value)}>
              <div className="flex items-center space-x-2">
                <RadioGroupItem value="same" id="same" />
                <Label htmlFor="same">Use the same payment method</Label>
              </div>
              <div className="flex items-center space-x-2">
                <RadioGroupItem value="different" id="different" />
                <Label htmlFor="different">Try a different payment method</Label>
              </div>
            </RadioGroup>

            {retryMethod === 'different' && (
              <div className="space-y-2">
                <Label>Select new payment method:</Label>
                <RadioGroup value={newPaymentMethod} onValueChange={(value: PaymentMethod) => setNewPaymentMethod(value)}>
                  {Object.values(PaymentMethod).filter(method => method !== payment.method).map((method) => (
                    <div key={method} className="flex items-center space-x-2">
                      <RadioGroupItem value={method} id={method} />
                      <Label htmlFor={method} className="flex items-center gap-2">
                        {getMethodIcon(method)}
                        {getMethodName(method)}
                      </Label>
                    </div>
                  ))}
                </RadioGroup>
              </div>
            )}

            <div className="space-y-2">
              <Label>Retry delay (optional):</Label>
              <RadioGroup value={retryDelay.toString()} onValueChange={(value) => setRetryDelay(parseInt(value))}>
                <div className="flex items-center space-x-2">
                  <RadioGroupItem value="0" id="immediate" />
                  <Label htmlFor="immediate">Retry immediately</Label>
                </div>
                <div className="flex items-center space-x-2">
                  <RadioGroupItem value="5" id="5min" />
                  <Label htmlFor="5min">Wait 5 minutes</Label>
                </div>
                <div className="flex items-center space-x-2">
                  <RadioGroupItem value="15" id="15min" />
                  <Label htmlFor="15min">Wait 15 minutes</Label>
                </div>
                <div className="flex items-center space-x-2">
                  <RadioGroupItem value="60" id="1hour" />
                  <Label htmlFor="1hour">Wait 1 hour</Label>
                </div>
              </RadioGroup>
            </div>

            <div className="space-y-2">
              <Label htmlFor="notes">Notes (optional):</Label>
              <Textarea
                id="notes"
                placeholder="Add any notes about this retry attempt..."
                value={notes}
                onChange={(e) => setNotes(e.target.value)}
                rows={3}
              />
            </div>

            <div className="flex gap-3 pt-2">
              <Button
                onClick={handleRetry}
                disabled={isRetrying || loading}
                className="flex-1"
              >
                {isRetrying ? (
                  <>
                    <div className="animate-spin rounded-full h-4 w-4 border-b-2 border-white mr-2" />
                    Retrying...
                  </>
                ) : (
                  <>
                    <RefreshCw className="h-4 w-4 mr-2" />
                    Retry Payment
                  </>
                )}
              </Button>
              
              <Button
                variant="outline"
                onClick={() => setShowCancelForm(true)}
                disabled={isRetrying || loading}
              >
                Cancel Payment
              </Button>
            </div>
          </CardContent>
        </Card>
      )}

      {/* Cancel Form */}
      {showCancelForm && (
        <Card>
          <CardHeader>
            <CardTitle className="text-red-700">Cancel Payment</CardTitle>
            <CardDescription>
              This will permanently cancel the failed payment. This action cannot be undone.
            </CardDescription>
          </CardHeader>

          <CardContent className="space-y-4">
            <div className="space-y-2">
              <Label htmlFor="cancelReason">Reason for cancellation:</Label>
              <Textarea
                id="cancelReason"
                placeholder="Please provide a reason for cancelling this payment..."
                value={cancelReason}
                onChange={(e) => setCancelReason(e.target.value)}
                rows={3}
                required
              />
            </div>

            <div className="flex gap-3">
              <Button
                variant="destructive"
                onClick={handleCancel}
                disabled={!cancelReason.trim() || loading}
                className="flex-1"
              >
                <XCircle className="h-4 w-4 mr-2" />
                Confirm Cancellation
              </Button>
              
              <Button
                variant="outline"
                onClick={() => setShowCancelForm(false)}
                disabled={loading}
                className="flex-1"
              >
                Keep Payment
              </Button>
            </div>
          </CardContent>
        </Card>
      )}

      {/* No Retry Available */}
      {(!payment.canRetry || remainingRetries <= 0) && !showCancelForm && (
        <Card>
          <CardContent className="p-6 text-center">
            <XCircle className="h-12 w-12 text-red-500 mx-auto mb-4" />
            <h3 className="font-semibold text-red-700 mb-2">No More Retries Available</h3>
            <p className="text-muted-foreground mb-4">
              This payment has reached the maximum number of retry attempts or cannot be retried due to the failure type.
            </p>
            <div className="flex gap-3 justify-center">
              <Button variant="outline" onClick={() => onMethodChange(payment.id, payment.method)}>
                Create New Payment
              </Button>
              <Button variant="outline" onClick={() => setShowCancelForm(true)}>
                Cancel Payment
              </Button>
            </div>
          </CardContent>
        </Card>
      )}
    </div>
  )
}