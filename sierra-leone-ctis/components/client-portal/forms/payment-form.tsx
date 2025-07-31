"use client"

import { useState } from 'react'
import { useForm } from 'react-hook-form'
import { zodResolver } from '@hookform/resolvers/zod'
import * as z from 'zod'
import { Button } from '@/components/ui/button'
import { Input } from '@/components/ui/input'
import { Select, SelectContent, SelectItem, SelectTrigger, SelectValue } from '@/components/ui/select'
import { Form, FormControl, FormField, FormItem, FormLabel, FormMessage } from '@/components/ui/form'
import { useToast } from '@/components/ui/use-toast'
import { CalendarIcon } from 'lucide-react'
import { Calendar } from '@/components/ui/calendar'
import { Popover, PopoverContent, PopoverTrigger } from '@/components/ui/popover'
import { cn } from '@/lib/utils'
import { format } from 'date-fns'
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

  const onSubmit = async (data: PaymentFormData) => {
    try {
      setLoading(true)
      
      const createData: CreateClientPaymentDto = {
        taxFilingId: data.taxFilingId ? parseInt(data.taxFilingId) : undefined,
        amount: data.amount,
        method: data.method,
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
                <Popover>
                  <PopoverTrigger asChild>
                    <FormControl>
                      <Button
                        variant="outline"
                        className={cn(
                          "w-full pl-3 text-left font-normal",
                          !field.value && "text-muted-foreground"
                        )}
                      >
                        {field.value ? (
                          format(field.value, "PPP")
                        ) : (
                          <span>Pick a date</span>
                        )}
                        <CalendarIcon className="ml-auto h-4 w-4 opacity-50" />
                      </Button>
                    </FormControl>
                  </PopoverTrigger>
                  <PopoverContent className="w-auto p-0" align="start">
                    <Calendar
                      mode="single"
                      selected={field.value}
                      onSelect={field.onChange}
                      disabled={(date) =>
                        date > new Date() || date < new Date("1900-01-01")
                      }
                      initialFocus
                    />
                  </PopoverContent>
                </Popover>
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
                  {/* TODO: Load user's tax filings when ClientPortalService is ready */}
                  <SelectItem value="1">Income Tax 2024</SelectItem>
                  <SelectItem value="2">GST Q1 2024</SelectItem>
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