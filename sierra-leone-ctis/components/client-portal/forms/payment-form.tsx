"use client"

import { useState, useEffect } from 'react'
import { useForm } from 'react-hook-form'
import { zodResolver } from '@hookform/resolvers/zod'
import * as z from 'zod'
import { Button } from '@/components/ui/button'
import { Input } from '@/components/ui/input'
import { Select, SelectContent, SelectItem, SelectTrigger, SelectValue } from '@/components/ui/select'
import { Form, FormControl, FormField, FormItem, FormLabel, FormMessage } from '@/components/ui/form'
import { useToast } from '@/components/ui/use-toast'
import { DatePicker } from '@/components/ui/date-picker'
import { ClientPortalService, CreateClientPaymentDto } from '@/lib/services/client-portal-service'

enum PaymentMethod {
  BankTransfer = 'BankTransfer',
  Cash = 'Cash',
  Check = 'Check',
  OnlinePayment = 'OnlinePayment',
  MobileMoney = 'MobileMoney'
}

const paymentSchema = z.object({
  taxFilingId: z.string().optional(),
  amount: z.number().min(0.01, 'Amount must be greater than 0'),
  method: z.nativeEnum(PaymentMethod, { required_error: 'Payment method is required' }),
  paymentReference: z.string().min(1, 'Payment reference is required'),
  paymentDate: z.date({ required_error: 'Payment date is required' }),
})

type PaymentFormData = z.infer<typeof paymentSchema>

interface ClientPaymentFormProps {
  onSuccess?: () => void
  initialData?: Partial<PaymentFormData>
}

export default function ClientPaymentForm({ onSuccess, initialData }: ClientPaymentFormProps) {
  const { toast } = useToast()
  const [loading, setLoading] = useState(false)
  const [filingOptions, setFilingOptions] = useState<Array<{ id: string; label: string }>>([])

  const form = useForm<PaymentFormData>({
    resolver: zodResolver(paymentSchema),
    defaultValues: {
      taxFilingId: initialData?.taxFilingId || '',
      amount: initialData?.amount || 0,
      method: initialData?.method || PaymentMethod.BankTransfer,
      paymentReference: initialData?.paymentReference || '',
      paymentDate: initialData?.paymentDate ? new Date(initialData.paymentDate) : new Date(),
    },
  })

  useEffect(() => {
    const loadFilings = async () => {
      try {
        const res = await ClientPortalService.getTaxFilings(1, 50)
        const options = (res.items || []).map(f => ({
          id: String(f.taxFilingId),
          label: `${f.taxType} ${f.taxYear}`
        }))
        setFilingOptions(options)
      } catch (e) {
        toast({ variant: 'destructive', title: 'Error', description: 'Failed to load tax filings' })
      }
    }
    loadFilings()
  }, [toast])

  const onSubmit = async (data: PaymentFormData) => {
    try {
      setLoading(true)
      
      const createData: CreateClientPaymentDto = {
        taxFilingId: data.taxFilingId ? parseInt(data.taxFilingId) : undefined,
        amount: data.amount,
        paymentMethod: data.method,
        paymentReference: data.paymentReference,
        paymentDate: data.paymentDate.toISOString(),
      }

      const result = await ClientPortalService.createPayment(createData)
      
      toast({
        title: 'Success',
        description: `Payment ${result.reference} submitted successfully`,
      })
      onSuccess?.()
    } catch (error) {
      console.error('Error creating payment:', error)
      toast({
        variant: 'destructive',
        title: 'Error',
        description: 'Failed to submit payment',
      })
    } finally {
      setLoading(false)
    }
  }

  return (
    <Form {...form}>
      <form onSubmit={form.handleSubmit(onSubmit)} className="space-y-6">
        <div className="grid grid-cols-2 gap-4">
          <FormField
            control={form.control}
            name="amount"
            render={({ field }) => (
              <FormItem>
                <FormLabel>Amount (SLE)</FormLabel>
                <FormControl>
                  <Input
                    type="number"
                    placeholder="0.00"
                    step="0.01"
                    {...field}
                    onChange={(e) => field.onChange(parseFloat(e.target.value))}
                  />
                </FormControl>
                <FormMessage />
              </FormItem>
            )}
          />

          <FormField
            control={form.control}
            name="method"
            render={({ field }) => (
              <FormItem>
                <FormLabel>Payment Method</FormLabel>
                <Select onValueChange={field.onChange} defaultValue={field.value}>
                  <FormControl>
                    <SelectTrigger>
                      <SelectValue placeholder="Select payment method" />
                    </SelectTrigger>
                  </FormControl>
                  <SelectContent>
                    <SelectItem value={PaymentMethod.BankTransfer}>Bank Transfer</SelectItem>
                    <SelectItem value={PaymentMethod.Cash}>Cash</SelectItem>
                    <SelectItem value={PaymentMethod.Check}>Check</SelectItem>
                    <SelectItem value={PaymentMethod.OnlinePayment}>Online Payment</SelectItem>
                    <SelectItem value={PaymentMethod.MobileMoney}>Mobile Money</SelectItem>
                  </SelectContent>
                </Select>
                <FormMessage />
              </FormItem>
            )}
          />
        </div>

        <div className="grid grid-cols-2 gap-4">
          <FormField
            control={form.control}
            name="paymentReference"
            render={({ field }) => (
              <FormItem>
                <FormLabel>Payment Reference</FormLabel>
                <FormControl>
                  <Input placeholder="Enter payment reference" {...field} />
                </FormControl>
                <FormMessage />
              </FormItem>
            )}
          />

          <FormField
            control={form.control}
            name="paymentDate"
            render={({ field }) => (
              <FormItem>
                <FormLabel>Payment Date</FormLabel>
                <FormControl>
                  <DatePicker
                    value={field.value ?? null}
                    onChange={(d) => field.onChange(d || undefined)}
                    maxDate={new Date()}
                    minDate={new Date("1900-01-01")}
                    placeholder="Pick a date"
                  />
                </FormControl>
                <FormMessage />
              </FormItem>
            )}
          />
        </div>

        <FormField
          control={form.control}
          name="taxFilingId"
          render={({ field }) => (
            <FormItem>
              <FormLabel>Tax Filing (Optional)</FormLabel>
              <Select onValueChange={field.onChange} defaultValue={field.value}>
                <FormControl>
                  <SelectTrigger>
                    <SelectValue placeholder="Select tax filing (optional)" />
                  </SelectTrigger>
                </FormControl>
                <SelectContent>
                  <SelectItem value="">No specific filing</SelectItem>
                  {filingOptions.map(opt => (
                    <SelectItem key={opt.id} value={opt.id}>{opt.label}</SelectItem>
                  ))}
                </SelectContent>
              </Select>
              <FormMessage />
            </FormItem>
          )}
        />

        <div className="bg-sierra-blue/5 p-4 rounded-lg border">
          <h4 className="font-semibold text-sierra-blue mb-2">Payment Information</h4>
          <p className="text-sm text-muted-foreground">
            Your payment will be submitted for review and approval. You will receive a confirmation 
            once the payment has been processed by our team.
          </p>
        </div>

        <div className="flex justify-end gap-4">
          <Button type="button" variant="outline" onClick={() => form.reset()}>
            Reset
          </Button>
          <Button type="submit" disabled={loading} className="bg-sierra-blue hover:bg-sierra-blue/90">
            {loading ? 'Submitting...' : 'Submit Payment'}
          </Button>
        </div>
      </form>
    </Form>
  )
}