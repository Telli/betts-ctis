"use client"

import { useState, useEffect, useMemo } from 'react'
import { useForm } from 'react-hook-form'
import { zodResolver } from '@hookform/resolvers/zod'
import * as z from 'zod'
import { Button } from '@/components/ui/button'
import { Input } from '@/components/ui/input'
import { Label } from '@/components/ui/label'
import { Select, SelectContent, SelectItem, SelectTrigger, SelectValue } from '@/components/ui/select'
import { Form, FormControl, FormField, FormItem, FormLabel, FormMessage } from '@/components/ui/form'
import { useToast } from '@/components/ui/use-toast'
import { TaxFilingService, ClientService, TaxType, CreateTaxFilingDto, ClientDto, TaxCalculationService } from '@/lib/services'
import { DocumentService, type DocumentUploadCategory } from '@/lib/services'
import type { WithholdingTaxType } from '@/lib/services/tax-calculation-service'
import { FileUpload, type FileUploadFile } from '@/components/ui/file-upload'
import { Textarea } from '@/components/ui/textarea'
import { CalendarIcon, Calculator, CheckCircle2 } from 'lucide-react'
import { Calendar } from '@/components/ui/calendar'
import { Popover, PopoverContent, PopoverTrigger } from '@/components/ui/popover'
import { cn } from '@/lib/utils'
import { format } from 'date-fns'
import { Checkbox } from '@/components/ui/checkbox'
import ClientSearchSelect from '@/components/client-search-select'

const taxFilingSchema = z.object({
  clientId: z.string().min(1, 'Client is required'),
  taxType: z.nativeEnum(TaxType, { required_error: 'Tax type is required' }),
  taxYear: z.number().min(2000).max(2100),
  dueDate: z.date({ required_error: 'Due date is required' }),
  taxLiability: z.number().min(0, 'Tax liability must be non-negative'),
  filingReference: z.string().optional(),
  filingPeriod: z.string().optional(),
  penaltyAmount: z.number().min(0).optional(),
  interestAmount: z.number().min(0).optional(),
  additionalData: z.string().optional(),
})

type TaxFilingFormData = z.infer<typeof taxFilingSchema>

interface TaxFilingFormProps {
  onSuccess?: () => void
  initialData?: Partial<CreateTaxFilingDto>
}


export default function TaxFilingForm({ onSuccess, initialData }: TaxFilingFormProps) {
  const { toast } = useToast()
  const [loading, setLoading] = useState(false)
  const [clients, setClients] = useState<ClientDto[]>([])
  const [calculatingLiability, setCalculatingLiability] = useState(false)
  const [taxableAmount, setTaxableAmount] = useState<number>(0)
  const [statusMessage, setStatusMessage] = useState<string>('')
  const [uploads, setUploads] = useState<FileUploadFile[]>([])
  const [categoryById, setCategoryById] = useState<Record<string, DocumentUploadCategory>>({})
  const [requirements, setRequirements] = useState<Array<{ category: string; required: boolean; description: string; acceptedFormats: string[]; maxSizeMb: number }>>([])
  const [withholdingType, setWithholdingType] = useState<WithholdingTaxType>('ProfessionalFees')
  const [isResident, setIsResident] = useState<boolean>(true)

  const form = useForm<TaxFilingFormData>({
    resolver: zodResolver(taxFilingSchema),
    defaultValues: {
      clientId: initialData?.clientId?.toString() || '',
      taxType: initialData?.taxType || TaxType.IncomeTax,
      taxYear: initialData?.taxYear || new Date().getFullYear(),
      dueDate: initialData?.dueDate ? new Date(initialData.dueDate) : undefined,
      taxLiability: initialData?.taxLiability || 0,
      filingReference: initialData?.filingReference || '',
      filingPeriod: '',
      penaltyAmount: undefined,
      interestAmount: undefined,
      additionalData: '',
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

  // Helpers for categories
  const serverCategoryToFrontend = (cat: string): DocumentUploadCategory => {
    switch (cat) {
      case 'TaxReturn': return 'tax-return'
      case 'FinancialStatement': return 'financial-statement'
      case 'Receipt': return 'receipt'
      case 'Invoice': return 'invoice'
      case 'PaymentEvidence': return 'payment-evidence'
      case 'BankStatement': return 'bank-statement'
      default: return 'supporting-document'
    }
  }

  const frontendCategoryOptions: Array<{ value: DocumentUploadCategory; label: string }> = [
    { value: 'tax-return', label: 'Tax Return' },
    { value: 'financial-statement', label: 'Financial Statement' },
    { value: 'invoice', label: 'Invoice' },
    { value: 'receipt', label: 'Receipt' },
    { value: 'payment-evidence', label: 'Payment Evidence' },
    { value: 'bank-statement', label: 'Bank Statement' },
    { value: 'supporting-document', label: 'Supporting Document' },
    { value: 'correspondence', label: 'Correspondence' },
  ]

  // Withholding subtype options
  const withholdingOptions: Array<{ value: WithholdingTaxType; label: string }> = [
    { value: 'ProfessionalFees', label: 'Professional Fees (15%)' },
    { value: 'ManagementFees', label: 'Management Fees (15%)' },
    { value: 'Dividends', label: 'Dividends (15%)' },
    { value: 'Royalties', label: 'Royalties (15%)' },
    { value: 'Interest', label: 'Interest (15%)' },
    { value: 'Rent', label: 'Rent (10%)' },
    { value: 'Commissions', label: 'Commissions (5%)' },
    { value: 'LotteryWinnings', label: 'Lottery Winnings (15%)' },
  ]

  const defaultCategoryForTaxType = (t: TaxType): DocumentUploadCategory => {
    switch (t) {
      case TaxType.GST: return 'invoice'
      case TaxType.PayrollTax: return 'financial-statement'
      case TaxType.PAYE: return 'financial-statement'
      case TaxType.WithholdingTax: return 'invoice'
      case TaxType.ExciseDuty: return 'receipt'
      case TaxType.IncomeTax:
      case TaxType.PersonalIncomeTax:
      case TaxType.CorporateIncomeTax:
        return 'tax-return'
      default: return 'supporting-document'
    }
  }

  // Fetch document requirements when client or tax type changes
  const watchedTaxType = form.watch('taxType')
  const watchedClientId = form.watch('clientId')
  useEffect(() => {
    const fetchReqs = async () => {
      const clientId = parseInt(watchedClientId || '0')
      if (!clientId || !watchedTaxType) { setRequirements([]); return }
      const selectedClient = clients.find(c => c.clientId === clientId)
      if (!selectedClient) { setRequirements([]); return }
      try {
        const reqs = await DocumentService.getDocumentRequirements(String(watchedTaxType), String(selectedClient.taxpayerCategory))
        setRequirements(reqs)
      } catch (e) {
        setRequirements([])
      }
    }
    fetchReqs()
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, [watchedTaxType, watchedClientId, clients])

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
      if (taxType === TaxType.WithholdingTax) {
        // Use dedicated withholding tax endpoint with subtype and residency
        const resp = await TaxCalculationService.calculateWithholdingTax({
          amount: taxableAmount,
          withholdingTaxType: withholdingType,
          isResident,
        })
        const wtAmount = (resp as any).withholdingTaxAmount ?? (resp as any).WithholdingTaxAmount ?? 0
        form.setValue('taxLiability', wtAmount)
        const eff = taxableAmount ? (wtAmount / taxableAmount) : 0
        toast({
          title: 'Withholding Tax Calculated',
          description: `Tax: ${wtAmount.toLocaleString('en-US', { style: 'currency', currency: 'SLE' })} (${(eff * 100).toFixed(2)}%)`,
        })
        setStatusMessage(`Withholding tax calculated: ${wtAmount.toLocaleString('en-US', { style: 'currency', currency: 'SLE' })}`)
      } else {
        const result = await TaxFilingService.calculateTaxLiability({
          clientId,
          taxType,
          taxYear,
          taxableAmount,
        })

        if (result.success) {
          form.setValue('taxLiability', result.data.taxLiability)
          const eff = (result as any).data?.effectiveRate ?? (taxableAmount ? (result.data.taxLiability / taxableAmount) : 0)
          toast({
            title: 'Tax Liability Calculated',
            description: `Tax liability: ${result.data.taxLiability.toLocaleString('en-US', {
              style: 'currency',
              currency: 'SLE'
            })} (${(eff * 100).toFixed(2)}% effective rate)`,
          })
          setStatusMessage(`Tax liability calculated: ${result.data.taxLiability.toLocaleString('en-US', {
            style: 'currency',
            currency: 'SLE'
          })}`)
        }
      }
    } catch (error) {
      console.error('Error calculating tax liability:', error)
      toast({
        variant: 'destructive',
        title: 'Calculation Error',
        description: 'Failed to calculate tax liability',
      })
      setStatusMessage('Error calculating tax liability')
    } finally {
      setCalculatingLiability(false)
    }
  }

  // File upload handlers
  const onFilesSelected = (files: File[]) => {
    const newItems: FileUploadFile[] = files.map((f, idx) => ({
      id: `${Date.now()}-${idx}-${f.name}`,
      file: f,
      status: 'pending',
      progress: 0,
    }))
    setUploads(prev => [...prev, ...newItems])
    // Assign default category per new file
    const defCat = defaultCategoryForTaxType(form.getValues('taxType'))
    setCategoryById(prev => {
      const next = { ...prev }
      newItems.forEach(n => { next[n.id] = next[n.id] || defCat })
      return next
    })
  }

  const onFileRemove = (fileId: string) => {
    setUploads(prev => prev.filter(f => f.id !== fileId))
  }

  // Compute file constraints from requirements
  const defaultAccepted = useMemo(() => [".pdf", ".doc", ".docx", ".xlsx", ".xls", ".csv", ".jpg", ".jpeg", ".png"], [])
  const acceptedFromRequirements = useMemo(() => {
    const all = new Set<string>()
    for (const r of requirements) {
      (r.acceptedFormats || []).forEach(f => all.add(f))
    }
    return Array.from(all)
  }, [requirements])
  const maxSizeFromRequirements = useMemo(() => {
    const sizes = requirements.map(r => r.maxSizeMb).filter(s => typeof s === 'number' && s > 0)
    if (sizes.length === 0) return 15
    return Math.min(...sizes)
  }, [requirements])

  const onSubmit = async (data: TaxFilingFormData) => {
    try {
      setLoading(true)
      setStatusMessage('Creating tax filing...')
      
      const createData: CreateTaxFilingDto = {
        clientId: parseInt(data.clientId),
        taxType: data.taxType,
        taxYear: data.taxYear,
        dueDate: data.dueDate.toISOString(),
        taxLiability: data.taxLiability,
        filingReference: data.filingReference || undefined,
        // Extended
        filingPeriod: data.filingPeriod || undefined,
        taxableAmount: taxableAmount || undefined,
        penaltyAmount: data.penaltyAmount,
        interestAmount: data.interestAmount,
        additionalData: data.additionalData && data.additionalData.trim().length > 0 ? data.additionalData : undefined,
      }

      // Withholding-specific
      if (data.taxType === TaxType.WithholdingTax) {
        (createData as any).withholdingTaxSubtype = withholdingType ?? null;
        (createData as any).isResident = isResident ?? null;
      }

      // Validate required document categories before creating (optional policy)
      if (requirements && requirements.length > 0) {
        const requiredCats = requirements
          .filter(r => r.required)
          .map(r => serverCategoryToFrontend(r.category) as DocumentUploadCategory)
        const selectedCats = uploads.map(u => categoryById[u.id] || defaultCategoryForTaxType(data.taxType))
        const missing = requiredCats.filter(rc => !selectedCats.includes(rc))
        if (missing.length > 0) {
          toast({
            variant: 'destructive',
            title: 'Missing required documents',
            description: `Please add documents for: ${missing.join(', ')}`
          })
          setLoading(false)
          return
        }
      }

      const result = await TaxFilingService.createTaxFiling(createData)
      
      if (result.success) {
        toast({ title: 'Success', description: `Tax filing ${result.data.filingReference} created successfully` })
        setStatusMessage(`Tax filing ${result.data.filingReference} created successfully`)

        // Upload supporting documents if any
        if (uploads.length > 0) {
          setStatusMessage(`Uploading ${uploads.length} supporting document(s)...`)
          const clientId = parseInt(data.clientId)
          for (const uf of uploads) {
            setUploads(prev => prev.map(p => p.id === uf.id ? { ...p, status: 'uploading', progress: 10 } : p))
            try {
              await DocumentService.upload(clientId, {
                file: uf.file,
                category: categoryById[uf.id] || defaultCategoryForTaxType(data.taxType),
                taxFilingId: result.data.taxFilingId,
                description: 'Tax filing supporting document'
              })
              setUploads(prev => prev.map(p => p.id === uf.id ? { ...p, status: 'success', progress: 100 } : p))
            } catch (e: any) {
              setUploads(prev => prev.map(p => p.id === uf.id ? { ...p, status: 'error', error: e?.message || 'Upload failed' } : p))
            }
          }
          toast({ title: 'Documents uploaded', description: 'Supporting documents have been uploaded.' })
        }
        onSuccess?.()
      }
    } catch (error) {
      console.error('Error creating tax filing:', error)
      toast({
        variant: 'destructive',
        title: 'Error',
        description: 'Failed to create tax filing',
      })
      setStatusMessage('Error creating tax filing')
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
                  onChange={(val) => field.onChange(val)}
                  placeholder="Select client"
                />
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
                    <SelectItem value={TaxType.PAYE}>PAYE</SelectItem>
                    <SelectItem value={TaxType.WithholdingTax}>Withholding Tax</SelectItem>
                    <SelectItem value={TaxType.PersonalIncomeTax}>Personal Income Tax</SelectItem>
                    <SelectItem value={TaxType.CorporateIncomeTax}>Corporate Income Tax</SelectItem>
                  </SelectContent>
                </Select>
                <FormMessage />
              </FormItem>
            )}
          />
        </div>

        {/* Withholding Tax Details */}
        {form.watch('taxType') === TaxType.WithholdingTax && (
          <div className="grid grid-cols-2 gap-4">
            <div>
              <Label>Withholding Subtype</Label>
              <Select value={withholdingType} onValueChange={(v) => setWithholdingType(v as WithholdingTaxType)}>
                <SelectTrigger>
                  <SelectValue placeholder="Select subtype" />
                </SelectTrigger>
                <SelectContent>
                  {withholdingOptions.map(opt => (
                    <SelectItem key={opt.value} value={opt.value}>{opt.label}</SelectItem>
                  ))}
                </SelectContent>
              </Select>
            </div>
            <div className="flex items-center gap-2 pt-6">
              <Checkbox id="isResident" checked={isResident} onCheckedChange={(val:any) => setIsResident(!!val)} />
              <Label htmlFor="isResident">Resident (apply resident rates)</Label>
            </div>
          </div>
        )}

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

        {/* Filing Period */}
        <div className="grid grid-cols-2 gap-4">
          <FormField
            control={form.control}
            name="filingPeriod"
            render={({ field }) => (
              <FormItem>
                <FormLabel>Filing Period (e.g., 2025-09 or Q1-2025)</FormLabel>
                <FormControl>
                  <Input placeholder="YYYY-MM or Q#-YYYY" {...field} />
                </FormControl>
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

        {/* Penalty & Interest */}
        <div className="grid grid-cols-2 gap-4">
          <FormField
            control={form.control}
            name="penaltyAmount"
            render={({ field }) => (
              <FormItem>
                <FormLabel>Penalty Amount (SLE, optional)</FormLabel>
                <FormControl>
                  <Input
                    type="number"
                    placeholder="0.00"
                    step="0.01"
                    value={field.value ?? ''}
                    onChange={(e) => {
                      const v = e.target.value
                      field.onChange(v === '' ? undefined : parseFloat(v))
                    }}
                  />
                </FormControl>
                <FormMessage />
              </FormItem>
            )}
          />
          <FormField
            control={form.control}
            name="interestAmount"
            render={({ field }) => (
              <FormItem>
                <FormLabel>Interest Amount (SLE, optional)</FormLabel>
                <FormControl>
                  <Input
                    type="number"
                    placeholder="0.00"
                    step="0.01"
                    value={field.value ?? ''}
                    onChange={(e) => {
                      const v = e.target.value
                      field.onChange(v === '' ? undefined : parseFloat(v))
                    }}
                  />
                </FormControl>
                <FormMessage />
              </FormItem>
            )}
          />
        </div>

        {/* Additional Data */}
        <FormField
          control={form.control}
          name="additionalData"
          render={({ field }) => (
            <FormItem>
              <FormLabel>Additional Data (JSON, optional)</FormLabel>
              <FormControl>
                <Textarea rows={4} placeholder='{"note":"..."}' {...field} />
              </FormControl>
              <FormMessage />
            </FormItem>
          )}
        />

        {/* Supporting Documents */}
        <div className="space-y-2">
          <Label>Supporting Documents</Label>
          <FileUpload
            files={uploads}
            onFilesSelected={onFilesSelected}
            onFileRemove={onFileRemove}
            acceptedFileTypes={acceptedFromRequirements.length > 0 ? acceptedFromRequirements : defaultAccepted}
            maxFileSize={maxSizeFromRequirements}
            maxFiles={10}
          />

          {/* Per-file Category Selectors */}
          {uploads.length > 0 && (
            <div className="space-y-3 mt-3">
              <div className="text-sm font-medium">Assign a category to each uploaded file</div>
              {uploads.map((u) => (
                <div key={u.id} className="grid grid-cols-2 gap-3 items-center">
                  <div className="text-sm text-muted-foreground truncate" title={u.file.name}>{u.file.name}</div>
                  <div>
                    <Select
                      value={categoryById[u.id] ?? defaultCategoryForTaxType(form.getValues('taxType'))}
                      onValueChange={(v) => setCategoryById(prev => ({ ...prev, [u.id]: v as DocumentUploadCategory }))}
                    >
                      <SelectTrigger>
                        <SelectValue placeholder="Select category" />
                      </SelectTrigger>
                      <SelectContent>
                        {frontendCategoryOptions.map(opt => (
                          <SelectItem key={opt.value} value={opt.value}>{opt.label}</SelectItem>
                        ))}
                      </SelectContent>
                    </Select>
                  </div>
                </div>
              ))}
            </div>
          )}
          {/* Document Requirements Checklist */}
          {requirements.length > 0 && (
            <div className="mt-4 border rounded-md p-3">
              <div className="font-medium mb-2">Document Requirements</div>
              <ul className="list-none pl-0 space-y-2">
                {requirements.map((r, idx) => {
                  const cat = serverCategoryToFrontend(r.category)
                  const selectedCats = new Set(uploads.map(u => categoryById[u.id] || defaultCategoryForTaxType(form.getValues('taxType'))))
                  const satisfied = selectedCats.has(cat)
                  const hasFormatsOrSize = Boolean((r.acceptedFormats?.length || r.maxSizeMb))
                  const formatsText = r.acceptedFormats?.length ? `formats: ${r.acceptedFormats.join(', ')}` : ''
                  const sizeText = r.maxSizeMb ? `max ${r.maxSizeMb}MB` : ''
                  const sep = (r.acceptedFormats?.length && r.maxSizeMb) ? ' · ' : ''
                  return (
                    <li key={`${r.category}-${idx}`} className="text-sm flex items-center gap-2">
                      <CheckCircle2 className={cn('h-4 w-4', satisfied ? 'text-green-600' : 'text-gray-300')} />
                      <span className="font-medium">{cat}</span>
                      {r.required && <span className="ml-2 px-2 py-0.5 text-xs rounded bg-amber-100 text-amber-800">required</span>}
                      {r.description && <span className="ml-2 text-muted-foreground">- {r.description}</span>}
                      {hasFormatsOrSize ? (
                        <span className="ml-2 text-muted-foreground">
                          {formatsText}{sep}{sizeText}
                        </span>
                      ) : null}
                    </li>
                  )
                })}
              </ul>
            </div>
          )}
        </div>

        <div className="flex justify-end gap-4">
          <Button type="button" variant="outline" onClick={() => form.reset()}>
            Reset
          </Button>
          <Button type="submit" disabled={loading} className="bg-sierra-blue hover:bg-sierra-blue/90">
            {loading ? 'Creating...' : 'Create Tax Filing'}
          </Button>
        </div>

        {/* Screen reader status announcements */}
        <div
          role="status"
          aria-live="polite"
          aria-atomic="true"
          className="sr-only"
        >
          {statusMessage}
        </div>
      </form>
    </Form>
  )
}