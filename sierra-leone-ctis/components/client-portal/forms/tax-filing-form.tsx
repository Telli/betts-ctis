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
import { CalendarIcon, Calculator } from 'lucide-react'
import { Calendar } from '@/components/ui/calendar'
import { Popover, PopoverContent, PopoverTrigger } from '@/components/ui/popover'
import { cn } from '@/lib/utils'
import { format } from 'date-fns'
import { ClientPortalService, CreateClientTaxFilingDto } from '@/lib/services/client-portal-service'
import { TaxFilingService, TaxType as BackendTaxType } from '@/lib/services/tax-filing-service'

enum TaxType {
  IncomeTax = 'IncomeTax',
  GST = 'GST',
  PayrollTax = 'PayrollTax',
  ExciseDuty = 'ExciseDuty'
}

const taxFilingSchema = z.object({
  taxType: z.nativeEnum(TaxType, { required_error: 'Tax type is required' }),
  taxYear: z.number().min(2000).max(2100),
  dueDate: z.date({ required_error: 'Due date is required' }),
  taxLiability: z.number().min(0, 'Tax liability must be non-negative'),
  filingReference: z.string().optional(),
})

type TaxFilingFormData = z.infer<typeof taxFilingSchema>

interface ClientTaxFilingFormProps {
  onSuccess?: () => void
  initialData?: Partial<TaxFilingFormData>
}

export default function ClientTaxFilingForm({ onSuccess, initialData }: ClientTaxFilingFormProps) {
  const { toast } = useToast()
  const [loading, setLoading] = useState(false)
  const [calculatingLiability, setCalculatingLiability] = useState(false)
  const [taxableAmount, setTaxableAmount] = useState<number>(0)
  const [clientId, setClientId] = useState<number | null>(null)

  const form = useForm<TaxFilingFormData>({
    resolver: zodResolver(taxFilingSchema),
    defaultValues: {
      taxType: initialData?.taxType || TaxType.IncomeTax,
      taxYear: initialData?.taxYear || new Date().getFullYear(),
      dueDate: initialData?.dueDate ? new Date(initialData.dueDate) : undefined,
      taxLiability: initialData?.taxLiability || 0,
      filingReference: initialData?.filingReference || '',
    },
  })

  useEffect(() => {
    const loadProfile = async () => {
      try {
        const profile = await ClientPortalService.getProfile()
        setClientId(profile.clientId)
      } catch (e) {
        toast({ variant: 'destructive', title: 'Error', description: 'Failed to load profile for calculation' })
      }
    }
    loadProfile()
  }, [toast])

  // Calculate tax liability (will need to integrate with ClientPortalService)
  const calculateTaxLiability = async () => {
    const taxType = form.getValues('taxType')
    const taxYear = form.getValues('taxYear')

    if (!taxType || !taxYear || !taxableAmount) {
      toast({
        variant: 'destructive',
        title: 'Missing Information',
        description: 'Please provide tax type, tax year, and taxable amount',
      })
      return
    }

    try {
      setCalculatingLiability(true)
      if (!clientId) {
        throw new Error('Missing client context')
      }
      const result = await TaxFilingService.calculateTaxLiability({
        clientId,
        taxType: taxType as unknown as BackendTaxType,
        taxYear,
        taxableAmount,
      })
      const liability = result?.data?.taxLiability ?? result?.data?.breakdown?.total ?? 0
      form.setValue('taxLiability', liability)
      toast({ title: 'Tax Liability Calculated', description: `Tax liability: Le ${liability.toLocaleString()}` })
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
      
      const createData: CreateClientTaxFilingDto = {
        taxType: data.taxType,
        taxYear: data.taxYear,
        dueDate: data.dueDate.toISOString(),
        taxLiability: data.taxLiability,
        filingReference: data.filingReference || undefined,
      }

      const result = await ClientPortalService.createTaxFiling(createData)
      
      toast({
        title: 'Success',
        description: `Tax filing ${result.filingDate ? 'created' : 'submitted'} successfully`,
      })
      onSuccess?.()
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
        </div>

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