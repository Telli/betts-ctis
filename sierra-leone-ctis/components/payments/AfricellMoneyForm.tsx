"use client"

import { useEffect, useState } from 'react'
import { useForm } from 'react-hook-form'
import { zodResolver } from '@hookform/resolvers/zod'
import * as z from 'zod'
import { Button } from '@/components/ui/button'
import { Input } from '@/components/ui/input'
import { Form, FormControl, FormField, FormItem, FormLabel, FormMessage } from '@/components/ui/form'
import { useToast } from '@/components/ui/use-toast'
import { Card, CardContent, CardDescription, CardHeader, CardTitle } from '@/components/ui/card'
import { Badge } from '@/components/ui/badge'
import { Eye, EyeOff, Smartphone, DollarSign, AlertTriangle, CheckCircle, Signal } from 'lucide-react'
import { cn } from '@/lib/utils'

// Sierra Leone phone number validation for Africell network
const africellPhoneRegex = /^(\+232|232|0)?(30|31|32|33|34|77|78|79)[0-9]{6}$/

const africellMoneySchema = z.object({
  phoneNumber: z.string()
    .min(1, 'Phone number is required')
    .regex(africellPhoneRegex, 'Please enter a valid Africell number (30, 31, 32, 33, 34, 77, 78, 79)'),
  amount: z.number().min(1, 'Amount must be at least Le 1'),
  pin: z.string()
    .min(4, 'PIN must be at least 4 digits')
    .max(6, 'PIN must be at most 6 digits')
    .regex(/^\d+$/, 'PIN must contain only numbers'),
  description: z.string().optional()
})

type AfricellMoneyFormData = z.infer<typeof africellMoneySchema>

interface AfricellMoneyFormProps {
  amount: number
  taxType?: string
  taxYear?: number
  onSubmit: (data: AfricellMoneyFormData) => Promise<void>
  onCancel?: () => void
  loading?: boolean
}

export default function AfricellMoneyForm({ 
  amount, 
  taxType, 
  taxYear, 
  onSubmit, 
  onCancel, 
  loading = false 
}: AfricellMoneyFormProps) {
  const { toast } = useToast()
  const [showPin, setShowPin] = useState(false)
  const [isValidating, setIsValidating] = useState(false)
  const [accountValid, setAccountValid] = useState<boolean | null>(null)
  const [networkStatus, setNetworkStatus] = useState<'checking' | 'available' | 'unavailable' | null>(null)

  const form = useForm<AfricellMoneyFormData>({
    resolver: zodResolver(africellMoneySchema),
    defaultValues: {
      phoneNumber: '',
      amount: amount,
      pin: '',
      description: taxType && taxYear ? `${taxType} payment for ${taxYear}` : ''
    }
  })

  useEffect(() => {
    form.setValue('amount', amount)
  }, [amount, form])

  useEffect(() => {
    const currentDescription = form.getValues('description')
    if (!currentDescription && taxType && taxYear) {
      form.setValue('description', `${taxType} payment for ${taxYear}`)
    }
  }, [taxType, taxYear, form])

  const formatPhoneNumber = (phone: string): string => {
    // Remove all non-digit characters
    const cleaned = phone.replace(/\D/g, '')
    
    // Handle different input formats
    if (cleaned.startsWith('232')) {
      return `+${cleaned}`
    } else if (cleaned.startsWith('0')) {
      return `+232${cleaned.slice(1)}`
    } else if (cleaned.length === 8) {
      return `+232${cleaned}`
    }
    
    return phone
  }

  const getNetworkPrefix = (phone: string): string | null => {
    const cleaned = phone.replace(/\D/g, '')
    const prefix = cleaned.slice(-8, -6) // Get the network prefix
    
    if (['30', '31', '32', '33', '34'].includes(prefix)) {
      return prefix
    } else if (['77', '78', '79'].includes(prefix)) {
      return prefix
    }
    
    return null
  }

  const validateAccount = async (phoneNumber: string) => {
    if (!africellPhoneRegex.test(phoneNumber)) {
      setAccountValid(null)
      setNetworkStatus(null)
      return
    }

    setIsValidating(true)
    setNetworkStatus('checking')
    
    try {
      // Simulate account validation and network check
      await new Promise(resolve => setTimeout(resolve, 1200))
      
      const formatted = formatPhoneNumber(phoneNumber)
      const prefix = getNetworkPrefix(formatted)
      
      // Mock validation logic - in production, call Africell Money API
      const isValid = formatted.length === 12 && formatted.startsWith('+232') && prefix !== null
      const networkAvailable = Math.random() > 0.1 // 90% uptime simulation
      
      setAccountValid(isValid)
      setNetworkStatus(networkAvailable ? 'available' : 'unavailable')
      
      if (!isValid) {
        toast({
          variant: 'destructive',
          title: 'Invalid Account',
          description: 'Africell Money account not found or inactive'
        })
      } else if (!networkAvailable) {
        toast({
          variant: 'destructive',
          title: 'Service Unavailable',
          description: 'Africell Money service is temporarily unavailable. Please try again later.'
        })
      }
    } catch (error) {
      console.error('Account validation error:', error)
      setAccountValid(false)
      setNetworkStatus('unavailable')
    } finally {
      setIsValidating(false)
    }
  }

  const handlePhoneChange = (value: string) => {
    const formatted = formatPhoneNumber(value)
    form.setValue('phoneNumber', formatted)
    
    // Reset validation state
    setAccountValid(null)
    setNetworkStatus(null)
    
    // Validate after a delay
    if (formatted.length >= 10) {
      setTimeout(() => validateAccount(formatted), 500)
    }
  }

  const handleSubmit = async (data: AfricellMoneyFormData) => {
    if (accountValid === false) {
      toast({
        variant: 'destructive',
        title: 'Invalid Account',
        description: 'Please verify your Africell Money account before proceeding'
      })
      return
    }

    if (networkStatus === 'unavailable') {
      toast({
        variant: 'destructive',
        title: 'Service Unavailable',
        description: 'Africell Money service is currently unavailable. Please try again later.'
      })
      return
    }

    try {
      await onSubmit(data)
    } catch (error) {
      console.error('Africell Money payment error:', error)
      toast({
        variant: 'destructive',
        title: 'Payment Failed',
        description: 'Failed to process Africell Money payment. Please try again.'
      })
    }
  }

  const calculateFee = (amount: number): number => {
    // Africell Money fee structure for Sierra Leone
    if (amount <= 25000) return 250       // Le 250 for amounts up to Le 25,000
    if (amount <= 100000) return 750      // Le 750 for amounts up to Le 100,000
    if (amount <= 300000) return 1500     // Le 1,500 for amounts up to Le 300,000
    return Math.min(amount * 0.008, 4000) // 0.8% with max Le 4,000
  }

  const fee = calculateFee(amount)
  const totalAmount = amount + fee
  const networkPrefix = getNetworkPrefix(form.watch('phoneNumber'))

  return (
    <Card className="w-full max-w-md mx-auto">
      <CardHeader className="text-center">
        <div className="flex items-center justify-center gap-2 mb-2">
          <div className="w-8 h-8 bg-red-500 rounded-full flex items-center justify-center">
            <Smartphone className="w-4 h-4 text-white" />
          </div>
          <CardTitle className="text-red-600">Africell Money Payment</CardTitle>
        </div>
        <CardDescription>
          Pay securely using your Africell Money wallet
        </CardDescription>
      </CardHeader>
      
      <CardContent className="space-y-4">
        {/* Network Status Banner */}
        {networkStatus && (
          <div className={cn(
            "p-3 rounded-lg border flex items-center gap-2 text-sm",
            networkStatus === 'available' && "bg-green-50 border-green-200 text-green-800",
            networkStatus === 'unavailable' && "bg-red-50 border-red-200 text-red-800",
            networkStatus === 'checking' && "bg-blue-50 border-blue-200 text-blue-800"
          )}>
            <Signal className="h-4 w-4" />
            {networkStatus === 'available' && 'Africell Money service is available'}
            {networkStatus === 'unavailable' && 'Africell Money service is temporarily unavailable'}
            {networkStatus === 'checking' && 'Checking service availability...'}
          </div>
        )}

        {/* Payment Summary */}
        <div className="bg-red-50 p-4 rounded-lg border border-red-200">
          <div className="space-y-2 text-sm">
            <div className="flex justify-between">
              <span>Amount:</span>
              <span className="font-medium">Le {amount.toLocaleString()}</span>
            </div>
            <div className="flex justify-between">
              <span>Transaction Fee:</span>
              <span className="font-medium">Le {fee.toLocaleString()}</span>
            </div>
            <div className="flex justify-between border-t pt-2 font-bold">
              <span>Total:</span>
              <span>Le {totalAmount.toLocaleString()}</span>
            </div>
          </div>
        </div>

        <Form {...form}>
          <form onSubmit={form.handleSubmit(handleSubmit)} className="space-y-4">
            <FormField
              control={form.control}
              name="phoneNumber"
              render={({ field }) => (
                <FormItem>
                  <FormLabel>Africell Money Number</FormLabel>
                  <div className="relative">
                    <FormControl>
                      <Input
                        placeholder="+232 XX XXX XXXX"
                        {...field}
                        onChange={(e) => {
                          field.onChange(e.target.value)
                          handlePhoneChange(e.target.value)
                        }}
                        className={cn(
                          "pl-10",
                          accountValid === true && networkStatus === 'available' && "border-green-500",
                          (accountValid === false || networkStatus === 'unavailable') && "border-red-500"
                        )}
                      />
                    </FormControl>
                    <Smartphone className="absolute left-3 top-3 h-4 w-4 text-muted-foreground" />
                    
                    {/* Validation indicator */}
                    <div className="absolute right-3 top-3">
                      {isValidating && (
                        <div className="animate-spin rounded-full h-4 w-4 border-b-2 border-red-500" />
                      )}
                      {accountValid === true && networkStatus === 'available' && (
                        <CheckCircle className="h-4 w-4 text-green-500" />
                      )}
                      {(accountValid === false || networkStatus === 'unavailable') && (
                        <AlertTriangle className="h-4 w-4 text-red-500" />
                      )}
                    </div>
                  </div>
                  <FormMessage />
                  
                  {/* Network prefix indicator */}
                  {networkPrefix && (
                    <div className="flex items-center gap-2 text-xs">
                      <Badge variant="outline" className="text-red-600 border-red-200">
                        Network: {networkPrefix}
                      </Badge>
                      {['30', '31', '32', '33', '34'].includes(networkPrefix) && (
                        <span className="text-muted-foreground">4G Network</span>
                      )}
                      {['77', '78', '79'].includes(networkPrefix) && (
                        <span className="text-muted-foreground">Legacy Network</span>
                      )}
                    </div>
                  )}
                  
                  {accountValid === true && networkStatus === 'available' && (
                    <div className="flex items-center gap-1 text-sm text-green-600">
                      <CheckCircle className="h-3 w-3" />
                      Account verified and service available
                    </div>
                  )}
                </FormItem>
              )}
            />

            <FormField
              control={form.control}
              name="amount"
              render={({ field }) => (
                <FormItem>
                  <FormLabel>Amount (SLE)</FormLabel>
                  <FormControl>
                    <Input
                      type="number"
                      {...field}
                      onChange={(e) => field.onChange(parseFloat(e.target.value))}
                      disabled
                      className="bg-gray-50"
                    />
                  </FormControl>
                  <FormMessage />
                </FormItem>
              )}
            />

            <FormField
              control={form.control}
              name="pin"
              render={({ field }) => (
                <FormItem>
                  <FormLabel>Africell Money PIN</FormLabel>
                  <div className="relative">
                    <FormControl>
                      <Input
                        type={showPin ? "text" : "password"}
                        placeholder="Enter your 4-6 digit PIN"
                        {...field}
                        className="pr-10"
                        maxLength={6}
                      />
                    </FormControl>
                    <Button
                      type="button"
                      variant="ghost"
                      size="sm"
                      className="absolute right-0 top-0 h-full px-3 py-2 hover:bg-transparent"
                      onClick={() => setShowPin(!showPin)}
                    >
                      {showPin ? (
                        <EyeOff className="h-4 w-4" />
                      ) : (
                        <Eye className="h-4 w-4" />
                      )}
                    </Button>
                  </div>
                  <FormMessage />
                </FormItem>
              )}
            />

            <FormField
              control={form.control}
              name="description"
              render={({ field }) => (
                <FormItem>
                  <FormLabel>Description (Optional)</FormLabel>
                  <FormControl>
                    <Input
                      placeholder="Payment description"
                      {...field}
                    />
                  </FormControl>
                  <FormMessage />
                </FormItem>
              )}
            />

            {/* Security Notice */}
            <div className="bg-blue-50 p-3 rounded-lg border border-blue-200">
              <div className="flex items-start gap-2">
                <AlertTriangle className="h-4 w-4 text-blue-600 mt-0.5 flex-shrink-0" />
                <div className="text-xs text-blue-800">
                  <p className="font-medium mb-1">Security Notice:</p>
                  <p>Keep your PIN secure. Africell Money will never request your PIN via call or message.</p>
                </div>
              </div>
            </div>

            <div className="flex gap-3 pt-4">
              {onCancel && (
                <Button
                  type="button"
                  variant="outline"
                  onClick={onCancel}
                  className="flex-1"
                >
                  Cancel
                </Button>
              )}
              <Button
                type="submit"
                disabled={
                  loading || 
                  accountValid === false || 
                  networkStatus === 'unavailable' || 
                  isValidating
                }
                className="flex-1 bg-red-500 hover:bg-red-600"
              >
                {loading ? (
                  <div className="flex items-center gap-2">
                    <div className="animate-spin rounded-full h-4 w-4 border-b-2 border-white" />
                    Processing...
                  </div>
                ) : (
                  <div className="flex items-center gap-2">
                    <DollarSign className="h-4 w-4" />
                    Pay Le {totalAmount.toLocaleString()}
                  </div>
                )}
              </Button>
            </div>
          </form>
        </Form>
      </CardContent>
    </Card>
  )
}