"use client"

import { useState } from 'react'
import { Card, CardContent, CardDescription, CardHeader, CardTitle } from '@/components/ui/card'
import { Button } from '@/components/ui/button'
import { ArrowLeft, Shield, Clock, CheckCircle, AlertTriangle } from 'lucide-react'
import PaymentMethodSelector, { PaymentMethod } from './PaymentMethodSelector'
import OrangeMoneyForm from './OrangeMoneyForm'
import AfricellMoneyForm from './AfricellMoneyForm'
import { useToast } from '@/components/ui/use-toast'

interface PaymentProvider {
  id: PaymentMethod
  name: string
  displayName: string
  description: string
  processingTime: string
  securityLevel: 'high' | 'medium' | 'basic'
}

interface PaymentGatewayFormProps {
  amount?: number
  taxType?: string
  taxYear?: number
  clientId?: number
  taxFilingId?: number
  onSuccess?: (paymentReference: string) => void
  onCancel?: () => void
}

interface PaymentFormData {
  phoneNumber?: string
  amount: number
  pin?: string
  description?: string
  // Bank transfer fields
  accountNumber?: string
  bankCode?: string
  accountName?: string
  // PayPal fields
  email?: string
  // Card fields
  cardNumber?: string
  expiryMonth?: string
  expiryYear?: string
  cvv?: string
  cardName?: string
}

export default function PaymentGatewayForm({
  amount = 50000,
  taxType,
  taxYear,
  clientId,
  taxFilingId,
  onSuccess,
  onCancel
}: PaymentGatewayFormProps) {
  const { toast } = useToast()
  const [selectedMethod, setSelectedMethod] = useState<PaymentMethod | null>(null)
  const [selectedProvider, setSelectedProvider] = useState<PaymentProvider | null>(null)
  const [paymentAmount, setPaymentAmount] = useState(amount)
  const [loading, setLoading] = useState(false)
  const [paymentStatus, setPaymentStatus] = useState<'idle' | 'processing' | 'success' | 'failed'>('idle')

  const handleMethodSelect = (method: PaymentMethod, provider: PaymentProvider) => {
    setSelectedMethod(method)
    setSelectedProvider(provider)
  }

  const handleBack = () => {
    setSelectedMethod(null)
    setSelectedProvider(null)
    setPaymentStatus('idle')
  }

  const handlePaymentSubmit = async (formData: PaymentFormData) => {
    setLoading(true)
    setPaymentStatus('processing')

    try {
      // Simulate payment processing
      await new Promise(resolve => setTimeout(resolve, 3000))

      // Generate payment reference
      const timestamp = Date.now()
      const reference = `PAY-${selectedMethod?.toUpperCase()}-${timestamp}`

      // Mock payment success/failure
      const isSuccess = Math.random() > 0.1 // 90% success rate

      if (isSuccess) {
        setPaymentStatus('success')
        toast({
          title: 'Payment Successful',
          description: `Payment processed successfully. Reference: ${reference}`,
        })
        
        // Notify parent component
        onSuccess?.(reference)
      } else {
        throw new Error('Payment failed')
      }
    } catch (error) {
      setPaymentStatus('failed')
      console.error('Payment error:', error)
      toast({
        variant: 'destructive',
        title: 'Payment Failed',
        description: 'Payment could not be processed. Please try again.',
      })
    } finally {
      setLoading(false)
    }
  }

  const renderPaymentForm = () => {
    if (!selectedMethod || !selectedProvider) return null

    const commonProps = {
      amount: paymentAmount,
      taxType,
      taxYear,
      onCancel: handleBack,
      loading
    }

    switch (selectedMethod) {
      case PaymentMethod.OrangeMoney:
        return (
          <OrangeMoneyForm
            {...commonProps}
            onSubmit={handlePaymentSubmit}
          />
        )
      
      case PaymentMethod.AfricellMoney:
        return (
          <AfricellMoneyForm
            {...commonProps}
            onSubmit={handlePaymentSubmit}
          />
        )
      
      case PaymentMethod.BankTransfer:
        return <BankTransferPlaceholder {...commonProps} onSubmit={handlePaymentSubmit} />
      
      case PaymentMethod.PayPal:
        return <PayPalPlaceholder {...commonProps} onSubmit={handlePaymentSubmit} />
      
      case PaymentMethod.Stripe:
        return <StripePlaceholder {...commonProps} onSubmit={handlePaymentSubmit} />
      
      default:
        return null
    }
  }

  const renderPaymentStatus = () => {
    if (paymentStatus === 'idle') return null

    return (
      <Card className="mb-4">
        <CardContent className="p-4">
          <div className="flex items-center gap-3">
            {paymentStatus === 'processing' && (
              <>
                <div className="animate-spin rounded-full h-6 w-6 border-b-2 border-sierra-blue" />
                <div>
                  <div className="font-medium">Processing Payment...</div>
                  <div className="text-sm text-muted-foreground">
                    Please wait while we process your {selectedProvider?.displayName} payment
                  </div>
                </div>
              </>
            )}
            
            {paymentStatus === 'success' && (
              <>
                <CheckCircle className="h-6 w-6 text-green-500" />
                <div>
                  <div className="font-medium text-green-700">Payment Successful!</div>
                  <div className="text-sm text-muted-foreground">
                    Your payment has been processed successfully
                  </div>
                </div>
              </>
            )}
            
            {paymentStatus === 'failed' && (
              <>
                <AlertTriangle className="h-6 w-6 text-red-500" />
                <div>
                  <div className="font-medium text-red-700">Payment Failed</div>
                  <div className="text-sm text-muted-foreground">
                    Please try again or select a different payment method
                  </div>
                </div>
              </>
            )}
          </div>
        </CardContent>
      </Card>
    )
  }

  if (paymentStatus === 'success') {
    return (
      <Card className="w-full max-w-md mx-auto">
        <CardHeader className="text-center">
          <CheckCircle className="h-12 w-12 text-green-500 mx-auto mb-2" />
          <CardTitle className="text-green-700">Payment Successful!</CardTitle>
          <CardDescription>
            Your payment has been processed successfully
          </CardDescription>
        </CardHeader>
        <CardContent className="text-center">
          <div className="space-y-2 mb-4">
            <div className="text-sm text-muted-foreground">Amount Paid</div>
            <div className="text-2xl font-bold">Le {paymentAmount.toLocaleString()}</div>
            <div className="text-sm text-muted-foreground">via {selectedProvider?.displayName}</div>
          </div>
          
          <div className="flex gap-2">
            <Button variant="outline" onClick={handleBack} className="flex-1">
              New Payment
            </Button>
            <Button onClick={onCancel} className="flex-1">
              Done
            </Button>
          </div>
        </CardContent>
      </Card>
    )
  }

  return (
    <div className="space-y-4">
      {renderPaymentStatus()}
      
      {!selectedMethod ? (
        <Card>
          <CardHeader>
            <CardTitle className="flex items-center gap-2">
              <Shield className="h-5 w-5 text-sierra-blue" />
              Secure Payment Gateway
            </CardTitle>
            <CardDescription>
              Choose your preferred payment method for Sierra Leone tax payments
            </CardDescription>
          </CardHeader>
          <CardContent>
            <PaymentMethodSelector
              selectedMethod={selectedMethod || undefined}
              onMethodSelect={handleMethodSelect}
              amount={paymentAmount}
              showFees={true}
              showFeatures={true}
            />
          </CardContent>
        </Card>
      ) : (
        <div className="space-y-4">
          <div className="flex items-center gap-2">
            <Button
              variant="ghost"
              size="sm"
              onClick={handleBack}
              disabled={loading}
              className="p-2"
            >
              <ArrowLeft className="h-4 w-4" />
            </Button>
            <div>
              <h3 className="font-medium">Payment with {selectedProvider?.displayName}</h3>
              <p className="text-sm text-muted-foreground">
                Amount: Le {paymentAmount.toLocaleString()} • {selectedProvider?.processingTime}
              </p>
            </div>
          </div>
          
          {renderPaymentForm()}
        </div>
      )}
    </div>
  )
}

// Placeholder components for other payment methods
function BankTransferPlaceholder({ amount, onSubmit, onCancel, loading }: any) {
  return (
    <Card className="w-full max-w-md mx-auto">
      <CardHeader className="text-center">
        <CardTitle className="text-blue-600">Bank Transfer Payment</CardTitle>
        <CardDescription>Direct bank transfer payment form</CardDescription>
      </CardHeader>
      <CardContent>
        <div className="text-center py-8">
          <div className="text-muted-foreground mb-4">
            Bank transfer form implementation coming soon
          </div>
          <div className="text-2xl font-bold mb-4">Le {amount.toLocaleString()}</div>
          <div className="flex gap-3">
            <Button variant="outline" onClick={onCancel} className="flex-1">
              Cancel
            </Button>
            <Button
              onClick={() => onSubmit({ amount })}
              disabled={loading}
              className="flex-1"
            >
              {loading ? 'Processing...' : 'Pay Now'}
            </Button>
          </div>
        </div>
      </CardContent>
    </Card>
  )
}

function PayPalPlaceholder({ amount, onSubmit, onCancel, loading }: any) {
  return (
    <Card className="w-full max-w-md mx-auto">
      <CardHeader className="text-center">
        <CardTitle className="text-blue-600">PayPal Payment</CardTitle>
        <CardDescription>International PayPal payment form</CardDescription>
      </CardHeader>
      <CardContent>
        <div className="text-center py-8">
          <div className="text-muted-foreground mb-4">
            PayPal integration form implementation coming soon
          </div>
          <div className="text-2xl font-bold mb-4">Le {amount.toLocaleString()}</div>
          <div className="flex gap-3">
            <Button variant="outline" onClick={onCancel} className="flex-1">
              Cancel
            </Button>
            <Button
              onClick={() => onSubmit({ amount })}
              disabled={loading}
              className="flex-1"
            >
              {loading ? 'Processing...' : 'Pay with PayPal'}
            </Button>
          </div>
        </div>
      </CardContent>
    </Card>
  )
}

function StripePlaceholder({ amount, onSubmit, onCancel, loading }: any) {
  return (
    <Card className="w-full max-w-md mx-auto">
      <CardHeader className="text-center">
        <CardTitle className="text-purple-600">Card Payment</CardTitle>
        <CardDescription>Secure credit/debit card payment</CardDescription>
      </CardHeader>
      <CardContent>
        <div className="text-center py-8">
          <div className="text-muted-foreground mb-4">
            Stripe card payment form implementation coming soon
          </div>
          <div className="text-2xl font-bold mb-4">Le {amount.toLocaleString()}</div>
          <div className="flex gap-3">
            <Button variant="outline" onClick={onCancel} className="flex-1">
              Cancel
            </Button>
            <Button
              onClick={() => onSubmit({ amount })}
              disabled={loading}
              className="flex-1"
            >
              {loading ? 'Processing...' : 'Pay with Card'}
            </Button>
          </div>
        </div>
      </CardContent>
    </Card>
  )
}