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
import { PaymentService, ClientService, TaxFilingService, PaymentMethod, CreatePaymentDto, TaxFilingDto } from '@/lib/services'
import { CalendarIcon } from 'lucide-react'
import { Calendar } from '@/components/ui/calendar'
import ClientSearchSelect from '@/components/client-search-select'
import { Popover, PopoverContent, PopoverTrigger } from '@/components/ui/popover'
import { cn } from '@/lib/utils'
import { format } from 'date-fns'

const paymentSchema = z
  .object({
    clientId: z.string().min(1, 'Client is required'),
    applyTo: z.enum(['taxFiling', 'general']).default('taxFiling'),
    taxFilingId: z.string().optional(),
    reason: z.string().optional(),
    amount: z.number().min(0.01, 'Amount must be greater than 0'),
    method: z.nativeEnum(PaymentMethod, { required_error: 'Payment method is required' }),
    paymentReference: z.string().min(1, 'Payment reference is required'),
    paymentDate: z.date({ required_error: 'Payment date is required' }),
  })
  .superRefine((val, ctx) => {
    if (val.applyTo === 'general') {
      if (!val.reason || val.reason.trim().length === 0) {
        ctx.addIssue({ code: z.ZodIssueCode.custom, path: ['reason'], message: 'Description is required for General' })
      }
    }
  })

type PaymentFormData = z.infer<typeof paymentSchema>

interface PaymentFormProps {
  onSuccess?: () => void
  initialData?: Partial<CreatePaymentDto>
}

interface Client {
  clientId?: number
  businessName?: string
  clientNumber?: string
  firstName?: string
  lastName?: string
}

export default function PaymentForm({ onSuccess, initialData }: PaymentFormProps) {
  const { toast } = useToast()
  const [loading, setLoading] = useState(false)
  const [clients, setClients] = useState<Client[]>([])
  const [taxFilings, setTaxFilings] = useState<TaxFilingDto[]>([])
  const [selectedClientId, setSelectedClientId] = useState<number | null>(null)
  const [receiptFile, setReceiptFile] = useState<File | null>(null)

  const form = useForm<PaymentFormData>({
    resolver: zodResolver(paymentSchema),
    defaultValues: {
      clientId: initialData?.clientId?.toString() || '',
      applyTo: 'taxFiling',
      taxFilingId: initialData?.taxFilingId?.toString() || '',
      reason: '',
      amount: initialData?.amount || 0,
      method: initialData?.method || PaymentMethod.BankTransfer,
      paymentReference: initialData?.paymentReference || '',
      paymentDate: initialData?.paymentDate ? new Date(initialData.paymentDate) : new Date(),
    },
  })

  const applyTo = form.watch('applyTo')

  // Load clients
  useEffect(() => {
    const loadClients = async () => {
      try {
        const clientsData = await ClientService.getAll()
        setClients(clientsData || [])
      } catch (error) {
        console.error('Error loading clients:', error)
        toast({
          variant: 'destructive',
          title: 'Error',
          description: 'Failed to load clients',
        })
      }
    }
    loadClients()
  }, [toast])

  // Load tax filings for selected client
  useEffect(() => {
    const loadTaxFilings = async () => {
      if (!selectedClientId) {
        setTaxFilings([])
        return
      }

      try {
        const filingsData = await TaxFilingService.getTaxFilingsByClientId(selectedClientId)
        // Extract the data from the response array
        const filings = filingsData.map(response => response.data)
        setTaxFilings(filings)
      } catch (error) {
        console.error('Error loading tax filings:', error)
        setTaxFilings([])
      }
    }
    loadTaxFilings()
  }, [selectedClientId])

  // Handle client selection
  const handleClientChange = (clientId: string) => {
    const id = parseInt(clientId)
    setSelectedClientId(id)
    form.setValue('clientId', clientId)
    form.setValue('taxFilingId', 'none') // Reset tax filing selection
  }

  // Generate payment reference
  const generatePaymentReference = () => {
    const client = clients.find(c => c.clientId === selectedClientId)
    if (client) {
      const timestamp = new Date().toISOString().slice(0, 16).replace(/[-:T]/g, '')
      const reference = `PAY-${client.clientNumber}-${timestamp}`
      form.setValue('paymentReference', reference)
    }
  }

  const onSubmit = async (data: PaymentFormData) => {
    try {
      setLoading(true)
      
      const createData: CreatePaymentDto = {
        clientId: parseInt(data.clientId),
        taxFilingId:
          data.applyTo === 'taxFiling' && data.taxFilingId && data.taxFilingId !== 'none'
            ? parseInt(data.taxFilingId)
            : undefined,
        amount: data.amount,
        method: data.method,
        paymentReference: data.paymentReference,
        paymentDate: data.paymentDate.toISOString(),
      }

      const result = await PaymentService.createPayment(createData)
      
      if (result.success) {
        // Optionally upload receipt/evidence
        if (receiptFile) {
          try {
            await PaymentService.uploadPaymentEvidence(
              result.data.paymentId,
              {
                clientId: parseInt(data.clientId),
                taxFilingId:
                  data.applyTo === 'taxFiling' && data.taxFilingId && data.taxFilingId !== 'none'
                    ? parseInt(data.taxFilingId)
                    : undefined,
                description: data.applyTo === 'general' && data.reason ? data.reason : 'Payment receipt',
                category: 'Receipt',
              },
              receiptFile
            )
            toast({ title: 'Receipt uploaded', description: 'Your payment receipt was uploaded.' })
          } catch (e: any) {
            console.error('Error uploading receipt:', e)
            toast({ variant: 'destructive', title: 'Receipt upload failed', description: e?.message || 'Could not upload receipt' })
          }
        }
        toast({
          title: 'Success',
          description: `Payment ${result.data.paymentReference} recorded successfully`,
        })
        onSuccess?.()
      }
    } catch (error) {
      console.error('Error creating payment:', error)
      toast({
        variant: 'destructive',
        title: 'Error',
        description: 'Failed to record payment',
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
            name="clientId"
            render={({ field }) => (
              <FormItem>
                <FormLabel>Client</FormLabel>
                <ClientSearchSelect
                  value={field.value}
                  onChange={(val) => handleClientChange(val)}
                  placeholder="Select client"
                />
                <FormMessage />
              </FormItem>
            )}
          />

          <FormField
            control={form.control}
            name="taxFilingId"
            render={({ field }) => (
              <FormItem>
                <FormLabel>Tax Filing (Optional)</FormLabel>
                <Select onValueChange={field.onChange} defaultValue={field.value} disabled={!selectedClientId}>
                  <FormControl>
                    <SelectTrigger>
                      <SelectValue placeholder="Select tax filing" />
                    </SelectTrigger>
                  </FormControl>
                  <SelectContent>
                    <SelectItem value="none">No specific filing</SelectItem>
                    {taxFilings.map((filing) => (
                      <SelectItem key={filing.taxFilingId} value={filing.taxFilingId.toString()}>
                        {filing.filingReference} - {filing.taxType} {filing.taxYear}
                      </SelectItem>
                    ))}
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
                <div className="flex gap-2">
                  <FormControl>
                    <Input placeholder="Payment reference" {...field} />
                  </FormControl>
                  <Button
                    type="button"
                    variant="outline"
                    onClick={generatePaymentReference}
                    disabled={!selectedClientId}
                    className="shrink-0"
                  >
                    Generate
                  </Button>
                </div>
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

        {/* Receipt upload */}
        <div className="grid grid-cols-2 gap-4">
          <div>
            <FormLabel>Receipt (Optional)</FormLabel>
            <Input type="file" accept=".pdf,.jpg,.jpeg,.png,.webp" onChange={(e) => setReceiptFile(e.target.files?.[0] ?? null)} />
            <p className="text-xs text-muted-foreground mt-1">Accepted: PDF, JPG, PNG, WEBP. Max ~10MB recommended.</p>
          </div>
        </div>

        <div className="bg-sierra-blue/5 p-4 rounded-lg border mt-4">
          <h4 className="font-semibold text-sierra-blue mb-2">Payment Information</h4>
          <p className="text-sm text-muted-foreground">
            This payment will be recorded with "Pending" status and will require approval from an administrator 
            before being processed. The client will be notified once the payment is approved or rejected.
          </p>
        </div>

        <div className="flex justify-end gap-4">
          <Button
            type="button"
            variant="outline"
            onClick={() => {
              form.reset()
              setReceiptFile(null)
            }}
          >
            Reset
          </Button>
          <Button type="submit" disabled={loading} variant="default">
            {loading ? 'Recording...' : 'Record Payment'}
          </Button>
        </div>
      </form>
    </Form>
  )
}