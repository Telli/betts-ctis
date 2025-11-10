"use client"

import { useEffect, useMemo, useRef, useState } from 'react'
import { Card, CardContent, CardDescription, CardHeader, CardTitle } from '@/components/ui/card'
import { Button } from '@/components/ui/button'
import { Input } from '@/components/ui/input'
import {
  ArrowLeft,
  Shield,
  Clock,
  CheckCircle,
  AlertTriangle,
  Loader2,
  Info,
  User,
  Mail,
  Hash,
  PiggyBank,
} from 'lucide-react'
import PaymentMethodSelector, { PaymentMethod } from './PaymentMethodSelector'
import OrangeMoneyForm from './OrangeMoneyForm'
import AfricellMoneyForm from './AfricellMoneyForm'
import { useToast } from '@/components/ui/use-toast'
import {
  PaymentGatewayService,
  PaymentGatewayType,
  PaymentPurpose,
  PaymentTransactionStatus,
  type PaymentTransaction,
  ClientService,
  type ClientDto,
} from '@/lib/services'

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
  onSuccess?: (paymentReference: string, transactionId?: number) => void
  onCancel?: () => void
}

interface PaymentFormData {
  phoneNumber?: string
  amount: number
  pin?: string
  description?: string
  accountNumber?: string
  bankCode?: string
  accountName?: string
  email?: string
  cardNumber?: string
  expiryMonth?: string
  expiryYear?: string
  cvv?: string
  cardName?: string
}

const mobileMoneyMethods = new Set<PaymentMethod>([
  PaymentMethod.OrangeMoney,
  PaymentMethod.AfricellMoney,
])

const mapMethodToGateway = (method: PaymentMethod): PaymentGatewayType => {
  switch (method) {
    case PaymentMethod.OrangeMoney:
      return PaymentGatewayType.OrangeMoney
    case PaymentMethod.AfricellMoney:
      return PaymentGatewayType.AfricellMoney
    case PaymentMethod.BankTransfer:
      return PaymentGatewayType.BankTransfer
    case PaymentMethod.PayPal:
      return PaymentGatewayType.PayPal
    case PaymentMethod.Stripe:
      return PaymentGatewayType.Stripe
    default:
      return PaymentGatewayType.OrangeMoney
  }
}

const requiresBackendSupport = (method: PaymentMethod): boolean => {
  if (mobileMoneyMethods.has(method)) {
    return true
  }

  // Bank transfer, PayPal and Stripe flows are not yet implemented on the frontend
  return false
}

const unsupportedMessage: Record<PaymentMethod, string> = {
  [PaymentMethod.BankTransfer]: 'Bank transfer checkout is coming soon. Please choose Orange or Africell Money for now.',
  [PaymentMethod.PayPal]: 'PayPal checkout is coming soon. Please choose another payment option.',
  [PaymentMethod.Stripe]: 'Card payments are coming soon. Please use a mobile money option instead.',
  [PaymentMethod.OrangeMoney]: '',
  [PaymentMethod.AfricellMoney]: '',
}

export default function PaymentGatewayForm({
  amount = 50000,
  taxType,
  taxYear,
  clientId,
  taxFilingId,
  onSuccess,
  onCancel,
}: PaymentGatewayFormProps) {
  const { toast } = useToast()
  const [selectedMethod, setSelectedMethod] = useState<PaymentMethod | null>(null)
  const [selectedProvider, setSelectedProvider] = useState<PaymentProvider | null>(null)
  const [paymentAmount, setPaymentAmount] = useState(amount)
  const [loading, setLoading] = useState(false)
  const [paymentStatus, setPaymentStatus] = useState<'idle' | 'processing' | 'success' | 'failed'>('idle')
  const [activeTransaction, setActiveTransaction] = useState<PaymentTransaction | null>(null)
  const [gatewayError, setGatewayError] = useState<string | null>(null)

  const [clientLookupError, setClientLookupError] = useState<string | null>(null)
  const [clientLookupLoading, setClientLookupLoading] = useState(false)
  const [client, setClient] = useState<ClientDto | null>(null)
  const [clientIdInput, setClientIdInput] = useState(clientId ? String(clientId) : '')
  const [payerName, setPayerName] = useState('')
  const [payerEmail, setPayerEmail] = useState('')

  const abortRef = useRef(false)

  useEffect(() => {
    setPaymentAmount(amount)
  }, [amount])

  useEffect(() => {
    if (clientId) {
      setClientIdInput(String(clientId))
    }
  }, [clientId])

  useEffect(() => {
    return () => {
      abortRef.current = true
    }
  }, [])

  const defaultDescription = useMemo(() => {
    if (taxType && taxYear) {
      return `${taxType} payment for ${taxYear}`
    }
    if (taxType) {
      return `${taxType} payment`
    }
    return 'Tax compliance payment'
  }, [taxType, taxYear])

  useEffect(() => {
    const autoLoadClient = async () => {
      if (!clientId || client) return
      try {
        setClientLookupLoading(true)
        const details = await ClientService.getById(clientId)
        if (!abortRef.current) {
          setClient(details)
          setPayerName(details.businessName ?? details.contactPerson ?? '')
          setPayerEmail(details.email ?? '')
          setClientLookupError(null)
        }
      } catch (error: any) {
        if (!abortRef.current) {
          setClientLookupError(error?.message ?? 'Unable to load client details')
        }
      } finally {
        if (!abortRef.current) {
          setClientLookupLoading(false)
        }
      }
    }

    void autoLoadClient()
  }, [clientId, client])

  const handleMethodSelect = (method: PaymentMethod, provider: PaymentProvider) => {
    setSelectedMethod(method)
    setSelectedProvider(provider)
    setGatewayError(null)
    setPaymentStatus('idle')
    setActiveTransaction(null)
  }

  const handleBack = () => {
    setSelectedMethod(null)
    setSelectedProvider(null)
    setPaymentStatus('idle')
    setGatewayError(null)
    setActiveTransaction(null)
  }

  const handleClientLookup = async () => {
    setClientLookupError(null)
    const parsedId = Number(clientIdInput)

    if (!clientIdInput.trim() || Number.isNaN(parsedId) || parsedId <= 0) {
      setClientLookupError('Please enter a valid numeric client ID')
      setClient(null)
      return
    }

    try {
      setClientLookupLoading(true)
      const details = await ClientService.getById(parsedId)
      if (abortRef.current) return

      setClient(details)
      setPayerName(details.businessName ?? details.contactPerson ?? '')
      setPayerEmail(details.email ?? '')
      setClientLookupError(null)
    } catch (error: any) {
      if (!abortRef.current) {
        setClient(null)
        setClientLookupError(error?.message ?? 'Unable to find the specified client')
      }
    } finally {
      if (!abortRef.current) {
        setClientLookupLoading(false)
      }
    }
  }

  const pollTransaction = async (transactionId: number, maxAttempts = 8): Promise<PaymentTransaction> => {
    let attempts = 0

    while (attempts < maxAttempts) {
      if (abortRef.current) {
        throw new Error('Payment cancelled')
      }

      const current = await PaymentGatewayService.getTransaction(transactionId)
      if (abortRef.current) {
        throw new Error('Payment cancelled')
      }

      setActiveTransaction(current)

      if (current.status === PaymentTransactionStatus.Completed) {
        return current
      }

      if ([
        PaymentTransactionStatus.Failed,
        PaymentTransactionStatus.Cancelled,
        PaymentTransactionStatus.Expired,
        PaymentTransactionStatus.DeadLetter,
      ].includes(current.status)) {
        throw new Error(current.statusMessage || 'Payment was not completed')
      }

      attempts += 1
      await new Promise((resolve) => setTimeout(resolve, Math.min(2000 * attempts, 7000)))
    }

    throw new Error('Payment confirmation timed out')
  }

  const handlePaymentSubmit = async (formData: PaymentFormData) => {
    if (!selectedMethod) {
      toast({
        variant: 'destructive',
        title: 'No payment method selected',
        description: 'Choose a payment method to continue.',
      })
      return
    }

    if (!requiresBackendSupport(selectedMethod)) {
      toast({
        variant: 'destructive',
        title: 'Payment method not available yet',
        description: unsupportedMessage[selectedMethod],
      })
      return
    }

    if (!client) {
      toast({
        variant: 'destructive',
        title: 'Client required',
        description: 'Please validate a client before processing the payment.',
      })
      return
    }

    if (!formData.phoneNumber) {
      toast({
        variant: 'destructive',
        title: 'Phone number required',
        description: 'Please provide the payer phone number for this payment.',
      })
      return
    }

    if (paymentAmount <= 0) {
      toast({
        variant: 'destructive',
        title: 'Invalid amount',
        description: 'Payment amount must be greater than zero.',
      })
      return
    }

    const resolvedClientId = client.clientId ?? clientId ?? (clientIdInput ? Number(clientIdInput) : undefined)

    if (!resolvedClientId || Number.isNaN(resolvedClientId)) {
      toast({
        variant: 'destructive',
        title: 'Client information missing',
        description: 'We couldn’t resolve the client ID for this payment. Please verify the client again.',
      })
      return
    }

  setLoading(true)
  setPaymentStatus('processing')
  setGatewayError(null)
  setActiveTransaction(null)

    try {
      const gatewayType = mapMethodToGateway(selectedMethod)
      const description = formData.description?.trim() || defaultDescription
      const payload = {
        clientId: resolvedClientId,
        gatewayType,
        purpose: PaymentPurpose.TaxPayment,
        amount: paymentAmount,
        currency: 'SLE',
        payerPhone: formData.phoneNumber,
        payerName: payerName || client.businessName || client.contactPerson,
        payerEmail: payerEmail || client.email,
        description,
        taxFilingId,
        taxYearId: taxYear,
        taxType,
      }

      const transaction = await PaymentGatewayService.initiateTransaction(payload)
      if (abortRef.current) return

      setActiveTransaction(transaction)

      let latest = transaction

      if (mobileMoneyMethods.has(selectedMethod)) {
        if (!formData.pin) {
          throw new Error('Mobile money PIN is required to process this payment.')
        }
        latest = await PaymentGatewayService.processMobileMoney(transaction.id, gatewayType, formData.pin)
        if (abortRef.current) return
        setActiveTransaction(latest)
      } else {
        latest = await PaymentGatewayService.processPayment(transaction.id)
        if (abortRef.current) return
        setActiveTransaction(latest)
      }

      const completed = await pollTransaction(latest.id)
      if (abortRef.current) return

      setPaymentStatus('success')
      toast({
        title: 'Payment successful',
        description: `Transaction ${completed.transactionReference} completed successfully.`,
      })

      onSuccess?.(completed.transactionReference, completed.id)
    } catch (error: any) {
      if (abortRef.current) return

      const message = error?.message ?? 'Payment could not be processed. Please try again.'
      setGatewayError(message)
      setPaymentStatus('failed')
      toast({
        variant: 'destructive',
        title: 'Payment failed',
        description: message,
      })
    } finally {
      if (!abortRef.current) {
        setLoading(false)
      }
    }
  }

  const renderPaymentForm = () => {
    if (!selectedMethod || !selectedProvider) return null

    const commonProps = {
      amount: paymentAmount,
      taxType,
      taxYear,
      onCancel: handleBack,
      loading,
    }

    switch (selectedMethod) {
      case PaymentMethod.OrangeMoney:
        return (
          <OrangeMoneyForm
            key={`orange-${paymentAmount}`}
            {...commonProps}
            onSubmit={handlePaymentSubmit}
          />
        )

      case PaymentMethod.AfricellMoney:
        return (
          <AfricellMoneyForm
            key={`africell-${paymentAmount}`}
            {...commonProps}
            onSubmit={handlePaymentSubmit}
          />
        )

      case PaymentMethod.BankTransfer:
      case PaymentMethod.PayPal:
      case PaymentMethod.Stripe:
        return (
          <UnsupportedMethodNotice
            provider={selectedProvider}
            description={unsupportedMessage[selectedMethod]}
            onBack={handleBack}
          />
        )

      default:
        return null
    }
  }

  const renderPaymentStatus = () => {
    if (paymentStatus === 'idle') return null

    const statusTitle =
      paymentStatus === 'processing'
        ? 'Processing payment'
        : paymentStatus === 'success'
        ? 'Payment successful'
        : 'Payment failed'

    const statusDescription =
      paymentStatus === 'processing'
        ? selectedProvider
          ? `Please wait while we process your ${selectedProvider.displayName} payment.`
          : 'Processing payment.'
        : paymentStatus === 'success'
        ? 'Your payment has been processed successfully.'
        : gatewayError ?? 'The payment did not complete successfully.'

    return (
      <Card className="border-dashed">
        <CardContent className="flex items-start gap-3 p-4">
          {paymentStatus === 'processing' && (
            <Loader2 className="h-6 w-6 animate-spin text-sierra-blue" />
          )}
          {paymentStatus === 'success' && (
            <CheckCircle className="h-6 w-6 text-green-500" />
          )}
          {paymentStatus === 'failed' && (
            <AlertTriangle className="h-6 w-6 text-red-500" />
          )}
          <div className="space-y-1">
            <div className="font-medium capitalize">{statusTitle}</div>
            <p className="text-sm text-muted-foreground">{statusDescription}</p>
            {activeTransaction && (
              <div className="text-xs text-muted-foreground space-y-1">
                <div className="flex items-center gap-2">
                  <Hash className="h-3 w-3" />
                  <span>Reference: {activeTransaction.transactionReference}</span>
                </div>
                <div className="flex items-center gap-2">
                  <Clock className="h-3 w-3" />
                  <span>Status: {activeTransaction.statusName ?? activeTransaction.status}</span>
                </div>
              </div>
            )}
          </div>
        </CardContent>
      </Card>
    )
  }

  const renderSuccessState = () => {
    if (paymentStatus !== 'success') return null

    return (
      <Card className="w-full max-w-md mx-auto">
        <CardHeader className="text-center">
          <CheckCircle className="h-12 w-12 text-green-500 mx-auto mb-2" />
          <CardTitle className="text-green-700">Payment Successful!</CardTitle>
          <CardDescription>
            {activeTransaction?.transactionReference
              ? `Transaction ${activeTransaction.transactionReference} completed successfully.`
              : 'Your payment has been processed successfully.'}
          </CardDescription>
        </CardHeader>
        <CardContent className="text-center space-y-4">
          <div className="space-y-2">
            <div className="text-sm text-muted-foreground">Amount Paid</div>
            <div className="text-2xl font-bold">Le {paymentAmount.toLocaleString()}</div>
            {selectedProvider && (
              <div className="text-sm text-muted-foreground">
                via {selectedProvider.displayName}
              </div>
            )}
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

  if (paymentStatus === 'success') {
    return renderSuccessState()
  }

  return (
    <div className="space-y-4">
      {renderPaymentStatus()}

      <Card>
        <CardHeader>
          <CardTitle className="flex items-center gap-2">
            <PiggyBank className="h-5 w-5 text-sierra-blue" />
            Payment Details
          </CardTitle>
          <CardDescription>
            Provide the client and payer information for this transaction.
          </CardDescription>
        </CardHeader>
        <CardContent className="space-y-4">
          <div className="grid gap-4 md:grid-cols-2">
            <div className="space-y-2">
              <label className="text-sm font-medium flex items-center gap-2">
                <User className="h-4 w-4 text-muted-foreground" />
                Client ID
              </label>
              <div className="flex gap-2">
                <Input
                  value={clientIdInput}
                  onChange={(event) => setClientIdInput(event.target.value)}
                  placeholder="Enter client ID"
                  disabled={clientLookupLoading}
                />
                <Button type="button" variant="outline" onClick={handleClientLookup} disabled={clientLookupLoading}>
                  {clientLookupLoading ? (
                    <Loader2 className="h-4 w-4 animate-spin" />
                  ) : (
                    'Verify'
                  )}
                </Button>
              </div>
              {clientLookupError && (
                <p className="text-xs text-red-500">{clientLookupError}</p>
              )}
              {client && !clientLookupError && (
                <p className="text-xs text-muted-foreground">
                  {client.businessName || client.contactPerson} • {client.clientNumber}
                </p>
              )}
            </div>

            <div className="space-y-2">
              <label className="text-sm font-medium flex items-center gap-2">
                <PiggyBank className="h-4 w-4 text-muted-foreground" />
                Amount (SLE)
              </label>
              <Input
                type="number"
                min={1}
                value={paymentAmount}
                onChange={(event) => setPaymentAmount(Math.max(0, Number(event.target.value)))}
              />
              <p className="text-xs text-muted-foreground">
                Enter the payment amount in Sierra Leone Leones.
              </p>
            </div>
          </div>

          <div className="grid gap-4 md:grid-cols-2">
            <div className="space-y-2">
              <label className="text-sm font-medium flex items-center gap-2">
                <User className="h-4 w-4 text-muted-foreground" />
                Payer Name
              </label>
              <Input
                value={payerName}
                onChange={(event) => setPayerName(event.target.value)}
                placeholder="Name of the payer"
              />
            </div>

            <div className="space-y-2">
              <label className="text-sm font-medium flex items-center gap-2">
                <Mail className="h-4 w-4 text-muted-foreground" />
                Payer Email (optional)
              </label>
              <Input
                type="email"
                value={payerEmail}
                onChange={(event) => setPayerEmail(event.target.value)}
                placeholder="payer@example.com"
              />
            </div>
          </div>

          <div className="grid gap-4 md:grid-cols-2">
            <div className="space-y-2">
              <label className="text-sm font-medium flex items-center gap-2">
                <Info className="h-4 w-4 text-muted-foreground" />
                Tax Type (optional)
              </label>
              <Input
                value={taxType ?? ''}
                onChange={() => {}}
                disabled
                placeholder="Tax type"
              />
            </div>
            <div className="space-y-2">
              <label className="text-sm font-medium flex items-center gap-2">
                <Info className="h-4 w-4 text-muted-foreground" />
                Tax Year (optional)
              </label>
              <Input value={taxYear ?? ''} onChange={() => {}} disabled placeholder="Tax year" />
            </div>
          </div>

          <div className="rounded-md border border-dashed border-sierra-blue/40 bg-sierra-blue/5 px-4 py-3 text-sm text-muted-foreground">
            <div className="flex items-start gap-2">
              <Info className="mt-0.5 h-4 w-4 text-sierra-blue" />
              <p>
                After you select a payment method, you will be asked for the provider specific details
                (for example, mobile number and PIN). Processing runs against the live gateway APIs and may
                take up to a few seconds.
              </p>
            </div>
          </div>
        </CardContent>
      </Card>

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
              showFees
              showFeatures
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

function UnsupportedMethodNotice({
  provider,
  description,
  onBack,
}: {
  provider: PaymentProvider
  description: string
  onBack: () => void
}) {
  return (
    <Card className="w-full max-w-md mx-auto">
      <CardHeader className="text-center space-y-2">
        <AlertTriangle className="mx-auto h-10 w-10 text-yellow-500" />
        <CardTitle className="text-lg">{provider.displayName} Checkout</CardTitle>
        <CardDescription>{description}</CardDescription>
      </CardHeader>
      <CardContent className="flex justify-center">
        <Button onClick={onBack} variant="outline">
          Choose another method
        </Button>
      </CardContent>
    </Card>
  )
}