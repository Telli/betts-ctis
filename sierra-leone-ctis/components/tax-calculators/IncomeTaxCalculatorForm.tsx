'use client';

import { useState } from 'react';
import { useForm, useFieldArray } from 'react-hook-form';
import { zodResolver } from '@hookform/resolvers/zod';
import * as z from 'zod';
import { Card, CardContent, CardDescription, CardHeader, CardTitle } from '@/components/ui/card';
import { Button } from '@/components/ui/button';
import { Input } from '@/components/ui/input';
import { Label } from '@/components/ui/label';
import { Select, SelectContent, SelectItem, SelectTrigger, SelectValue } from '@/components/ui/select';
import { Tabs, TabsContent, TabsList, TabsTrigger } from '@/components/ui/tabs';
import { Badge } from '@/components/ui/badge';
import { Progress } from '@/components/ui/progress';
import { Alert, AlertDescription } from '@/components/ui/alert';
import { Separator } from '@/components/ui/separator';
// Calendar deprecated; using DatePicker
import { Popover, PopoverContent, PopoverTrigger } from '@/components/ui/popover';
import { DatePicker } from '@/components/ui/date-picker';
import { 
  Plus, 
  Minus, 
  Calculator, 
  TrendingUp, 
  AlertTriangle, 
  Info,
  CalendarIcon,
  FileText,
  DollarSign,
  PieChart
} from 'lucide-react';
import { format } from 'date-fns';
import { cn } from '@/lib/utils';
import { useToast } from '@/components/ui/use-toast';
import { TaxCalculationService, IncomeTaxCalculationRequest, IncomeTaxCalculation, TaxAllowance } from '@/lib/services/tax-calculation-service';

const incomeTaxSchema = z.object({
  taxpayerCategory: z.enum(['Individual', 'Large', 'Medium', 'Small', 'Micro']),
  taxYear: z.number().min(2020).max(2025),
  grossIncome: z.number().min(0, 'Gross income must be positive'),
  deductions: z.number().min(0, 'Deductions must be positive'),
  allowances: z.array(z.object({
    type: z.string().min(1, 'Allowance type is required'),
    amount: z.number().min(0, 'Allowance amount must be positive'),
    description: z.string().optional(),
  })),
  dueDate: z.date().optional(),
  paymentDate: z.date().optional(),
});

type IncomeTaxFormData = z.infer<typeof incomeTaxSchema>;

const taxpayerCategories = [
  { value: 'Individual', label: 'Individual Taxpayer', description: 'Personal income tax for individuals' },
  { value: 'Large', label: 'Large Company', description: 'Annual turnover > SLE 2 billion' },
  { value: 'Medium', label: 'Medium Company', description: 'Annual turnover SLE 500M - 2B' },
  { value: 'Small', label: 'Small Company', description: 'Annual turnover SLE 100M - 500M' },
  { value: 'Micro', label: 'Micro Company', description: 'Annual turnover < SLE 100M' },
];

const commonAllowances = [
  { type: 'Personal Allowance', description: 'Standard personal allowance' },
  { type: 'Dependent Allowance', description: 'Allowance for dependents' },
  { type: 'Medical Allowance', description: 'Medical expenses allowance' },
  { type: 'Education Allowance', description: 'Education expenses allowance' },
  { type: 'Housing Allowance', description: 'Housing/accommodation allowance' },
  { type: 'Pension Contribution', description: 'Pension scheme contributions' },
  { type: 'Life Insurance', description: 'Life insurance premiums' },
  { type: 'Charitable Donations', description: 'Donations to registered charities' },
];

interface IncomeTaxCalculatorFormProps {
  onCalculationComplete?: (calculation: IncomeTaxCalculation) => void;
  initialData?: Partial<IncomeTaxFormData>;
}

export default function IncomeTaxCalculatorForm({ 
  onCalculationComplete, 
  initialData 
}: IncomeTaxCalculatorFormProps) {
  const { toast } = useToast();
  const [calculation, setCalculation] = useState<IncomeTaxCalculation | null>(null);
  const [loading, setLoading] = useState(false);

  const form = useForm<IncomeTaxFormData>({
    resolver: zodResolver(incomeTaxSchema),
    defaultValues: {
      taxpayerCategory: 'Individual',
      taxYear: 2024,
      grossIncome: 0,
      deductions: 0,
      allowances: [],
      ...initialData,
    },
  });

  const { fields: allowanceFields, append: appendAllowance, remove: removeAllowance } = useFieldArray({
    control: form.control,
    name: 'allowances',
  });

  const onSubmit = async (data: IncomeTaxFormData) => {
    setLoading(true);
    try {
      const request: IncomeTaxCalculationRequest = {
        ...data,
        dueDate: data.dueDate,
        paymentDate: data.paymentDate,
      };

      const result = await TaxCalculationService.calculateIncomeTax(request);
      setCalculation(result);
      onCalculationComplete?.(result);

      toast({
        title: 'Calculation Complete',
        description: `Income tax calculated: SLE ${result.payableTax.toLocaleString()}`,
      });
    } catch (error) {
      console.error('Income tax calculation error:', error);
      toast({
        variant: 'destructive',
        title: 'Calculation Error',
        description: error instanceof Error ? error.message : 'Failed to calculate income tax',
      });
    } finally {
      setLoading(false);
    }
  };

  const selectedCategory = taxpayerCategories.find(cat => cat.value === form.watch('taxpayerCategory'));

  const addAllowance = (allowanceType: string, description: string) => {
    appendAllowance({
      type: allowanceType,
      amount: 0,
      description: description,
    });
  };

  const formatCurrency = (amount: number) => {
    return `SLE ${amount.toLocaleString('en-US', { minimumFractionDigits: 0, maximumFractionDigits: 0 })}`;
  };

  return (
    <div className="space-y-6">
      <Card>
        <CardHeader>
          <CardTitle className="flex items-center gap-2">
            <Calculator className="h-5 w-5 text-sierra-blue" />
            Sierra Leone Income Tax Calculator
          </CardTitle>
          <CardDescription>
            Calculate income tax liability based on Finance Act 2025 rules for individuals and companies
          </CardDescription>
        </CardHeader>
        <CardContent>
          <form onSubmit={form.handleSubmit(onSubmit)} className="space-y-6">
            <Tabs defaultValue="basic" className="w-full">
              <TabsList className="grid w-full grid-cols-4">
                <TabsTrigger value="basic">Basic Details</TabsTrigger>
                <TabsTrigger value="allowances">Allowances</TabsTrigger>
                <TabsTrigger value="dates">Dates</TabsTrigger>
                <TabsTrigger value="results">Results</TabsTrigger>
              </TabsList>

              {/* Basic Details Tab */}
              <TabsContent value="basic" className="space-y-4">
                <div className="grid grid-cols-1 md:grid-cols-2 gap-4">
                  <div className="space-y-2">
                    <Label htmlFor="taxpayerCategory">Taxpayer Category</Label>
                    <Select
                      value={form.watch('taxpayerCategory')}
                      onValueChange={(value) => form.setValue('taxpayerCategory', value as any)}
                    >
                      <SelectTrigger>
                        <SelectValue placeholder="Select taxpayer category" />
                      </SelectTrigger>
                      <SelectContent>
                        {taxpayerCategories.map((category) => (
                          <SelectItem key={category.value} value={category.value}>
                            <div>
                              <div className="font-medium">{category.label}</div>
                              <div className="text-xs text-muted-foreground">{category.description}</div>
                            </div>
                          </SelectItem>
                        ))}
                      </SelectContent>
                    </Select>
                    {selectedCategory && (
                      <Alert>
                        <Info className="h-4 w-4" />
                        <AlertDescription>{selectedCategory.description}</AlertDescription>
                      </Alert>
                    )}
                  </div>

                  <div className="space-y-2">
                    <Label htmlFor="taxYear">Tax Year</Label>
                    <Select
                      value={form.watch('taxYear').toString()}
                      onValueChange={(value) => form.setValue('taxYear', parseInt(value))}
                    >
                      <SelectTrigger>
                        <SelectValue />
                      </SelectTrigger>
                      <SelectContent>
                        <SelectItem value="2024">2024</SelectItem>
                        <SelectItem value="2025">2025</SelectItem>
                      </SelectContent>
                    </Select>
                  </div>

                  <div className="space-y-2">
                    <Label htmlFor="grossIncome">Gross Income (SLE)</Label>
                    <Input
                      id="grossIncome"
                      type="number"
                      placeholder="Enter gross income"
                      {...form.register('grossIncome', { valueAsNumber: true })}
                      className="text-right"
                    />
                    {form.formState.errors.grossIncome && (
                      <p className="text-sm text-red-600">{form.formState.errors.grossIncome.message}</p>
                    )}
                  </div>

                  <div className="space-y-2">
                    <Label htmlFor="deductions">Total Deductions (SLE)</Label>
                    <Input
                      id="deductions"
                      type="number"
                      placeholder="Enter total deductions"
                      {...form.register('deductions', { valueAsNumber: true })}
                      className="text-right"
                    />
                    {form.formState.errors.deductions && (
                      <p className="text-sm text-red-600">{form.formState.errors.deductions.message}</p>
                    )}
                  </div>
                </div>

                {/* Tax Bracket Preview */}
                {form.watch('taxpayerCategory') && (
                  <Card className="bg-sierra-blue-50 border-sierra-blue-200">
                    <CardHeader className="pb-2">
                      <CardTitle>Tax Rates for {selectedCategory?.label} (Finance Act 2025)</CardTitle>
                    </CardHeader>
                    <CardContent className="pt-0">
                      <div className="grid grid-cols-1 md:grid-cols-3 gap-2 text-xs">
                        {form.watch('taxpayerCategory') === 'Individual' ? (
                          <>
                            <div className="flex justify-between">
                              <span>SLE 0 - 6,000,000:</span>
                              <span className="font-medium">0%</span>
                            </div>
                            <div className="flex justify-between">
                              <span>SLE 6,000,001 - 20,000,000:</span>
                              <span className="font-medium">15%</span>
                            </div>
                            <div className="flex justify-between">
                              <span>SLE 20,000,001 - 50,000,000:</span>
                              <span className="font-medium">20%</span>
                            </div>
                            <div className="flex justify-between md:col-span-3">
                              <span>Above SLE 50,000,000:</span>
                              <span className="font-medium">30%</span>
                            </div>
                          </>
                        ) : (
                          <>
                            <div className="flex justify-between">
                              <span>Standard Rate:</span>
                              <span className="font-medium">30%</span>
                            </div>
                            <div className="flex justify-between">
                              <span>Minimum Tax ({form.watch('taxpayerCategory') === 'Large' ? '0.5%' : '0.25%'}):</span>
                              <span className="font-medium">Yes</span>
                            </div>
                          </>
                        )}
                      </div>
                    </CardContent>
                  </Card>
                )}
              </TabsContent>

              {/* Allowances Tab */}
              <TabsContent value="allowances" className="space-y-4">
                <div className="flex items-center justify-between">
                  <h3 className="text-lg font-medium">Tax Allowances</h3>
                  <Popover>
                    <PopoverTrigger asChild>
                      <Button variant="outline" size="sm">
                        <Plus className="h-4 w-4 mr-2" />
                        Add Allowance
                      </Button>
                    </PopoverTrigger>
                    <PopoverContent className="w-80">
                      <div className="space-y-2">
                        <h4 className="font-medium">Common Allowances</h4>
                        {commonAllowances.map((allowance) => (
                          <Button
                            key={allowance.type}
                            variant="ghost"
                            className="w-full justify-start h-auto p-2"
                            onClick={() => addAllowance(allowance.type, allowance.description)}
                          >
                            <div className="text-left">
                              <div className="font-medium text-sm">{allowance.type}</div>
                              <div className="text-xs text-muted-foreground">{allowance.description}</div>
                            </div>
                          </Button>
                        ))}
                      </div>
                    </PopoverContent>
                  </Popover>
                </div>

                <div className="space-y-3">
                  {allowanceFields.map((field, index) => (
                    <Card key={field.id} className="p-4">
                      <div className="grid grid-cols-1 md:grid-cols-3 gap-3 items-end">
                        <div className="space-y-2">
                          <Label>Allowance Type</Label>
                          <Input
                            placeholder="Enter allowance type"
                            {...form.register(`allowances.${index}.type`)}
                          />
                        </div>
                        <div className="space-y-2">
                          <Label>Amount (SLE)</Label>
                          <Input
                            type="number"
                            placeholder="Enter amount"
                            {...form.register(`allowances.${index}.amount`, { valueAsNumber: true })}
                            className="text-right"
                          />
                        </div>
                        <div className="flex gap-2">
                          <Button
                            type="button"
                            variant="outline"
                            size="sm"
                            onClick={() => removeAllowance(index)}
                            className="flex-shrink-0"
                          >
                            <Minus className="h-4 w-4" />
                          </Button>
                        </div>
                      </div>
                      {field.description && (
                        <p className="text-xs text-muted-foreground mt-2">{field.description}</p>
                      )}
                    </Card>
                  ))}

                  {allowanceFields.length === 0 && (
                    <div className="text-center py-8 text-muted-foreground">
                      <FileText className="h-8 w-8 mx-auto mb-2 opacity-50" />
                      <p>No allowances added yet</p>
                      <p className="text-sm">Click "Add Allowance" to include tax allowances</p>
                    </div>
                  )}
                </div>
              </TabsContent>

              {/* Dates Tab */}
              <TabsContent value="dates" className="space-y-4">
                <div className="grid grid-cols-1 md:grid-cols-2 gap-4">
                  <div className="space-y-2">
                    <Label>Tax Due Date</Label>
                    <DatePicker
                      value={form.watch('dueDate') ?? null}
                      onChange={(d) => form.setValue('dueDate', d || undefined)}
                    />
                  </div>

                  <div className="space-y-2">
                    <Label>Payment Date</Label>
                    <DatePicker
                      value={form.watch('paymentDate') ?? null}
                      onChange={(d) => form.setValue('paymentDate', d || undefined)}
                    />
                  </div>
                </div>

                {form.watch('dueDate') && form.watch('paymentDate') && (
                  <Alert>
                    <AlertTriangle className="h-4 w-4" />
                    <AlertDescription>
                      {form.watch('paymentDate')! > form.watch('dueDate')! 
                        ? 'Late payment penalties may apply'
                        : 'Payment is on time'
                      }
                    </AlertDescription>
                  </Alert>
                )}
              </TabsContent>

              {/* Results Tab */}
              <TabsContent value="results" className="space-y-4">
                {calculation ? (
                  <TaxCalculationResults calculation={calculation} />
                ) : (
                  <div className="text-center py-8 text-muted-foreground">
                    <Calculator className="h-8 w-8 mx-auto mb-2 opacity-50" />
                    <p>No calculation results yet</p>
                    <p className="text-sm">Complete the form and calculate to see results</p>
                  </div>
                )}
              </TabsContent>
            </Tabs>

            <Separator />

            <div className="flex justify-end gap-2">
              <Button type="button" variant="outline" onClick={() => form.reset()}>
                Reset
              </Button>
              <Button type="submit" disabled={loading}>
                {loading ? 'Calculating...' : 'Calculate Income Tax'}
              </Button>
            </div>
          </form>
        </CardContent>
      </Card>
    </div>
  );
}

// Tax Calculation Results Component
function TaxCalculationResults({ calculation }: { calculation: IncomeTaxCalculation }) {
  const formatCurrency = (amount: number) => {
    return `SLE ${amount.toLocaleString('en-US', { minimumFractionDigits: 0, maximumFractionDigits: 0 })}`;
  };

  return (
    <div className="space-y-6">
      {/* Summary Cards */}
      <div className="grid grid-cols-1 md:grid-cols-3 gap-4">
        <Card className="border-sierra-blue-200 bg-sierra-blue-50">
          <CardContent className="p-4">
            <div className="flex items-center justify-between">
              <div>
                <p className="text-sm text-sierra-blue-600 font-medium">Tax Payable</p>
                <p className="text-2xl font-bold text-sierra-blue-800">
                  {formatCurrency(calculation.payableTax)}
                </p>
              </div>
              <DollarSign className="h-8 w-8 text-sierra-blue-600" />
            </div>
          </CardContent>
        </Card>

        <Card className="border-sierra-gold-200 bg-sierra-gold-50">
          <CardContent className="p-4">
            <div className="flex items-center justify-between">
              <div>
                <p className="text-sm text-sierra-gold-600 font-medium">Effective Rate</p>
                <p className="text-2xl font-bold text-sierra-gold-800">
                  {calculation.effectiveRate.toFixed(2)}%
                </p>
              </div>
              <TrendingUp className="h-8 w-8 text-sierra-gold-600" />
            </div>
          </CardContent>
        </Card>

        <Card className="border-sierra-green-200 bg-sierra-green-50">
          <CardContent className="p-4">
            <div className="flex items-center justify-between">
              <div>
                <p className="text-sm text-sierra-green-600 font-medium">Marginal Rate</p>
                <p className="text-2xl font-bold text-sierra-green-800">
                  {calculation.marginalRate.toFixed(2)}%
                </p>
              </div>
              <PieChart className="h-8 w-8 text-sierra-green-600" />
            </div>
          </CardContent>
        </Card>
      </div>

      {/* Calculation Breakdown */}
      <Card>
        <CardHeader>
          <CardTitle>Tax Calculation Breakdown</CardTitle>
        </CardHeader>
        <CardContent className="space-y-4">
          <div className="grid grid-cols-2 gap-2 text-sm">
            <div className="flex justify-between">
              <span>Gross Income:</span>
              <span className="font-medium">{formatCurrency(calculation.grossIncome)}</span>
            </div>
            <div className="flex justify-between">
              <span>Total Deductions:</span>
              <span className="font-medium text-red-600">-{formatCurrency(calculation.totalDeductions)}</span>
            </div>
            <div className="flex justify-between">
              <span>Total Allowances:</span>
              <span className="font-medium text-red-600">-{formatCurrency(calculation.totalAllowances)}</span>
            </div>
            <div className="flex justify-between font-medium border-t pt-2">
              <span>Taxable Income:</span>
              <span>{formatCurrency(calculation.taxableIncome)}</span>
            </div>
          </div>
        </CardContent>
      </Card>

      {/* Tax Brackets */}
      {calculation.taxBreakdown && calculation.taxBreakdown.length > 0 && (
        <Card>
          <CardHeader>
            <CardTitle>Tax Bracket Breakdown</CardTitle>
          </CardHeader>
          <CardContent>
            <div className="space-y-3">
              {calculation.taxBreakdown.map((bracket, index) => (
                <div key={index} className="flex items-center justify-between p-3 border rounded-lg">
                  <div className="flex-1">
                    <div className="text-sm font-medium">
                      {formatCurrency(bracket.from)} - {formatCurrency(bracket.to)}
                    </div>
                    <div className="text-xs text-muted-foreground">
                      Taxable: {formatCurrency(bracket.taxableAmount)} @ {bracket.rate}%
                    </div>
                  </div>
                </div>
              ))}
            </div>
            <Separator className="my-3" />
            {calculation.penalties && (
              <div className="flex justify-between font-medium">
                <span>Total Penalties:</span>
                <span className="text-red-600">
                  {formatCurrency(calculation.penalties.totalPenalty)}
                </span>
              </div>
            )}
          </CardContent>
        </Card>
      )}

      {/* Minimum Tax */}
      {calculation.minimumTax > 0 && (
        <Alert>
          <Info className="h-4 w-4" />
          <AlertDescription>
            Minimum tax applies: {formatCurrency(calculation.minimumTax)}. 
            Final tax payable is the higher of calculated tax or minimum tax.
          </AlertDescription>
        </Alert>
      )}
    </div>
  );
}