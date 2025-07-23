"use client"

import { useState, useEffect } from 'react'
import { useForm } from 'react-hook-form'
import { zodResolver } from '@hookform/resolvers/zod'
import * as z from 'zod'
import { Button } from '@/components/ui/button'
import { Input } from '@/components/ui/input'
import { Label } from '@/components/ui/label'
import { Select, SelectContent, SelectItem, SelectTrigger, SelectValue } from '@/components/ui/select'
import { Form, FormControl, FormField, FormItem, FormLabel, FormMessage } from '@/components/ui/form'
import { useToast } from '@/components/ui/use-toast'
import { TaxFilingService, ClientService, TaxType, CreateTaxFilingDto, ClientDto } from '@/lib/services'
import { CalendarIcon, Calculator } from 'lucide-react'
import { Calendar } from '@/components/ui/calendar'
import { Popover, PopoverContent, PopoverTrigger } from '@/components/ui/popover'
import { cn } from '@/lib/utils'
import { format } from 'date-fns'

const taxFilingSchema = z.object({
  clientId: z.string().min(1, 'Client is required'),
  taxType: z.nativeEnum(TaxType, { required_error: 'Tax type is required' }),
  taxYear: z.number().min(2000).max(2100),
  dueDate: z.date({ required_error: 'Due date is required' }),
  taxLiability: z.number().min(0, 'Tax liability must be non-negative'),
  filingReference: z.string().optional(),
})

type TaxFilingFormData = z.infer<typeof taxFilingSchema>

interface TaxFilingFormProps {
  onSuccess?: () => void
  initialData?: Partial<CreateTaxFilingDto>
}

interface Client {
  clientId: number
  businessName: string
  clientNumber: string
}

export default function TaxFilingForm({ onSuccess, initialData }: TaxFilingFormProps) {
  const { toast } = useToast()
  const [loading, setLoading] = useState(false)
  const [clients, setClients] = useState<ClientDto[]>([])
  const [calculatingLiability, setCalculatingLiability] = useState(false)
  const [taxableAmount, setTaxableAmount] = useState<number>(0)

  const form = useForm<TaxFilingFormData>({
    resolver: zodResolver(taxFilingSchema),
    defaultValues: {
      clientId: initialData?.clientId?.toString() || '',
      taxType: initialData?.taxType || TaxType.IncomeTax,
      taxYear: initialData?.taxYear || new Date().getFullYear(),
      dueDate: initialData?.dueDate ? new Date(initialData.dueDate) : undefined,
      taxLiability: initialData?.taxLiability || 0,
      filingReference: initialData?.filingReference || '',
    },
  })

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

  // Calculate tax liability
  const calculateTaxLiability = async () => {
    const clientId = parseInt(form.getValues('clientId'))
    const taxType = form.getValues('taxType')
    const taxYear = form.getValues('taxYear')

    if (!clientId || !taxType || !taxYear || !taxableAmount) {
      toast({
        variant: 'destructive',
        title: 'Missing Information',
        description: 'Please provide client, tax type, tax year, and taxable amount',
      })
      return
    }

    try {
      setCalculatingLiability(true)
      const result = await TaxFilingService.calculateTaxLiability({
        clientId,
        taxType,
        taxYear,
        taxableAmount,
      })

      if (result.success) {
        form.setValue('taxLiability', result.data.taxLiability)
        toast({
          title: 'Tax Liability Calculated',
          description: `Tax liability: ${result.data.taxLiability.toLocaleString('en-US', {
            style: 'currency',
            currency: 'SLE'
          })} (${(result.data.effectiveRate * 100).toFixed(2)}% effective rate)`,
        })
      }
    } catch (error) {
      console.error('Error calculating tax liability:', error)
      toast({
        variant: 'destructive',
        title: 'Calculation Error',
        description: 'Failed to calculate tax liability',
      })
    } finally {
      setCalculatingLiability(false)
    }
  }

  const onSubmit = async (data: TaxFilingFormData) => {
    try {
      setLoading(true)
      
      const createData: CreateTaxFilingDto = {
        clientId: parseInt(data.clientId),
        taxType: data.taxType,
        taxYear: data.taxYear,
        dueDate: data.dueDate.toISOString(),
        taxLiability: data.taxLiability,
        filingReference: data.filingReference || undefined,
      }

      const result = await TaxFilingService.createTaxFiling(createData)
      
      if (result.success) {
        toast({
          title: 'Success',
          description: `Tax filing ${result.data.filingReference} created successfully`,
        })
        onSuccess?.()
      }
    } catch (error) {
      console.error('Error creating tax filing:', error)
      toast({
        variant: 'destructive',
        title: 'Error',
        description: 'Failed to create tax filing',
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
                <Select onValueChange={field.onChange} defaultValue={field.value}>
                  <FormControl>
                    <SelectTrigger>
                      <SelectValue placeholder="Select client" />
                    </SelectTrigger>
                  </FormControl>
                  <SelectContent>
                    {clients.filter(client => client.clientId).map((client) => (
                      <SelectItem key={client.clientId} value={client.clientId!.toString()}>
                        {client.name || 'Unnamed Client'}
                      </SelectItem>
                    ))}
                  </SelectContent>
                </Select>
                <FormMessage />
              </FormItem>
            )}
          />

          <FormField
            control={form.control}
            name="taxType"
            render={({ field }) => (
              <FormItem>
                <FormLabel>Tax Type</FormLabel>
                <Select onValueChange={field.onChange} defaultValue={field.value}>
                  <FormControl>
                    <SelectTrigger>
                      <SelectValue placeholder="Select tax type" />
                    </SelectTrigger>
                  </FormControl>
                  <SelectContent>
                    <SelectItem value={TaxType.IncomeTax}>Income Tax</SelectItem>
                    <SelectItem value={TaxType.GST}>GST</SelectItem>
                    <SelectItem value={TaxType.PayrollTax}>Payroll Tax</SelectItem>
                    <SelectItem value={TaxType.ExciseDuty}>Excise Duty</SelectItem>
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
            name="taxYear"
            render={({ field }) => (
              <FormItem>
                <FormLabel>Tax Year</FormLabel>
                <FormControl>
                  <Input
                    type="number"
                    placeholder="2024"
                    {...field}
                    onChange={(e) => field.onChange(parseInt(e.target.value))}
                  />
                </FormControl>
                <FormMessage />
              </FormItem>
            )}
          />

          <FormField
            control={form.control}
            name="dueDate"
            render={({ field }) => (
              <FormItem>
                <FormLabel>Due Date</FormLabel>
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
                        date < new Date()
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

        {/* Tax Liability Calculation */}
        <div className="space-y-4 p-4 border rounded-lg bg-sierra-blue/5">
          <div className="flex items-center gap-2">
            <Calculator className="h-5 w-5 text-sierra-blue" />
            <Label className="text-lg font-semibold text-sierra-blue">Tax Liability Calculation</Label>
          </div>
          
          <div className="grid grid-cols-2 gap-4">
            <div>
              <Label htmlFor="taxableAmount">Taxable Amount (SLE)</Label>
              <Input
                id="taxableAmount"
                type="number"
                placeholder="0.00"
                step="0.01"
                value={taxableAmount}
                onChange={(e) => setTaxableAmount(parseFloat(e.target.value) || 0)}
              />
            </div>
            <div className="flex items-end">
              <Button
                type="button"
                onClick={calculateTaxLiability}
                disabled={calculatingLiability}
                className="w-full"
              >
                {calculatingLiability ? 'Calculating...' : 'Calculate Tax'}
              </Button>
            </div>
          </div>
        </div>

        <div className="grid grid-cols-2 gap-4">
          <FormField
            control={form.control}
            name="taxLiability"
            render={({ field }) => (
              <FormItem>
                <FormLabel>Tax Liability (SLE)</FormLabel>
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
            name="filingReference"
            render={({ field }) => (
              <FormItem>
                <FormLabel>Filing Reference (Optional)</FormLabel>
                <FormControl>
                  <Input placeholder="Auto-generated if empty" {...field} />
                </FormControl>
                <FormMessage />
              </FormItem>
            )}
          />
        </div>

        <div className="flex justify-end gap-4">
          <Button type="button" variant="outline" onClick={() => form.reset()}>
            Reset
          </Button>
          <Button type="submit" disabled={loading} className="bg-sierra-blue hover:bg-sierra-blue/90">
            {loading ? 'Creating...' : 'Create Tax Filing'}
          </Button>
        </div>
      </form>
    </Form>
  )
}