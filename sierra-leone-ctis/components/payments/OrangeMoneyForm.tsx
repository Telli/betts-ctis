"use client"

import { useState } from 'react'
import { useForm } from 'react-hook-form'
import { zodResolver } from '@hookform/resolvers/zod'
import * as z from 'zod'
import { Button } from '@/components/ui/button'
import { Input } from '@/components/ui/input'
import { Form, FormControl, FormField, FormItem, FormLabel, FormMessage } from '@/components/ui/form'
import { useToast } from '@/components/ui/use-toast'
import { Card, CardContent, CardDescription, CardHeader, CardTitle } from '@/components/ui/card'
import { Badge } from '@/components/ui/badge'
import { Eye, EyeOff, Smartphone, DollarSign, AlertTriangle, CheckCircle } from 'lucide-react'
import { cn } from '@/lib/utils'

// Sierra Leone phone number validation
const sierraLeonePhoneRegex = /^(\+232|232|0)?[2-9][0-9]{7}$/

const orangeMoneySchema = z.object({
  phoneNumber: z.string()
    .min(1, 'Phone number is required')
    .regex(sierraLeonePhoneRegex, 'Please enter a valid Sierra Leone phone number'),
  amount: z.number().min(1, 'Amount must be at least Le 1'),
  pin: z.string()
    .min(4, 'PIN must be at least 4 digits')
    .max(6, 'PIN must be at most 6 digits')
    .regex(/^\d+$/, 'PIN must contain only numbers'),
  description: z.string().optional()
})

type OrangeMoneyFormData = z.infer<typeof orangeMoneySchema>

interface OrangeMoneyFormProps {
  amount: number
  taxType?: string
  taxYear?: number
  onSubmit: (data: OrangeMoneyFormData) => Promise<void>
  onCancel?: () => void
  loading?: boolean
}

export default function OrangeMoneyForm({ 
  amount, 
  taxType, 
  taxYear, 
  onSubmit, 
  onCancel, 
  loading = false 
}: OrangeMoneyFormProps) {
  const { toast } = useToast()
  const [showPin, setShowPin] = useState(false)
  const [isValidating, setIsValidating] = useState(false)
  const [accountValid, setAccountValid] = useState<boolean | null>(null)

  const form = useForm<OrangeMoneyFormData>({
    resolver: zodResolver(orangeMoneySchema),
    defaultValues: {
      phoneNumber: '',
      amount: amount,
      pin: '',
      description: taxType && taxYear ? `${taxType} payment for ${taxYear}` : ''
    }
  })

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

  const validateAccount = async (phoneNumber: string) => {
    if (!sierraLeonePhoneRegex.test(phoneNumber)) {
      setAccountValid(null)
      return
    }

    setIsValidating(true)
    try {
      // Simulate account validation - replace with actual API call
      await new Promise(resolve => setTimeout(resolve, 1000))
      
      // Mock validation logic - in production, call Orange Money API
      const formatted = formatPhoneNumber(phoneNumber)
      const isValid = formatted.length === 12 && formatted.startsWith('+232')
      
      setAccountValid(isValid)
      
      if (!isValid) {
        toast({
          variant: 'destructive',
          title: 'Invalid Account',
          description: 'Orange Money account not found or inactive'
        })
      }
    } catch (error) {
      console.error('Account validation error:', error)
      setAccountValid(false)
    } finally {
      setIsValidating(false)
    }
  }

  const handlePhoneChange = (value: string) => {
    const formatted = formatPhoneNumber(value)
    form.setValue('phoneNumber', formatted)
    
    // Reset validation state
    setAccountValid(null)
    
    // Validate after a delay
    if (formatted.length >= 10) {
      setTimeout(() => validateAccount(formatted), 500)
    }
  }

  const handleSubmit = async (data: OrangeMoneyFormData) => {
    if (accountValid === false) {
      toast({
        variant: 'destructive',
        title: 'Invalid Account',
        description: 'Please verify your Orange Money account before proceeding'
      })
      return
    }

    try {
      await onSubmit(data)
    } catch (error) {
      console.error('Orange Money payment error:', error)
      toast({
        variant: 'destructive',
        title: 'Payment Failed',
        description: 'Failed to process Orange Money payment. Please try again.'
      })
    }
  }

  const calculateFee = (amount: number): number => {
    // Orange Money fee structure for Sierra Leone
    if (amount <= 50000) return 500      // Le 500 for amounts up to Le 50,000
    if (amount <= 200000) return 1000    // Le 1,000 for amounts up to Le 200,000
    if (amount <= 500000) return 2000    // Le 2,000 for amounts up to Le 500,000
    return Math.min(amount * 0.01, 5000) // 1% with max Le 5,000
  }

  const fee = calculateFee(amount)
  const totalAmount = amount + fee

  return (
    <Card className="w-full max-w-md mx-auto">
      <CardHeader className="text-center">
        <div className="flex items-center justify-center gap-2 mb-2">
          <div className="w-8 h-8 bg-orange-500 rounded-full flex items-center justify-center">
            <Smartphone className="w-4 h-4 text-white" />
          </div>
          <CardTitle className="text-orange-600">Orange Money Payment</CardTitle>
        </div>
        <CardDescription>
          Pay securely using your Orange Money wallet
        </CardDescription>
      </CardHeader>
      
      <CardContent className="space-y-4">
        {/* Payment Summary */}
        <div className="bg-orange-50 p-4 rounded-lg border border-orange-200">
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
                  <FormLabel>Orange Money Number</FormLabel>
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
                          accountValid === true && "border-green-500",
                          accountValid === false && "border-red-500"
                        )}
                      />
                    </FormControl>
                    <Smartphone className="absolute left-3 top-3 h-4 w-4 text-muted-foreground" />
                    
                    {/* Validation indicator */}
                    <div className="absolute right-3 top-3">
                      {isValidating && (
                        <div className="animate-spin rounded-full h-4 w-4 border-b-2 border-orange-500" />
                      )}
                      {accountValid === true && (
                        <CheckCircle className="h-4 w-4 text-green-500" />
                      )}
                      {accountValid === false && (
                        <AlertTriangle className="h-4 w-4 text-red-500" />
                      )}
                    </div>
                  </div>
                  <FormMessage />
                  
                  {accountValid === true && (
                    <div className="flex items-center gap-1 text-sm text-green-600">
                      <CheckCircle className="h-3 w-3" />
                      Account verified
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
                  <FormLabel>Orange Money PIN</FormLabel>
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
                  <p>Never share your PIN with anyone. Orange Money will never ask for your PIN via phone or SMS.</p>
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
                disabled={loading || accountValid === false || isValidating}
                className="flex-1 bg-orange-500 hover:bg-orange-600"
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