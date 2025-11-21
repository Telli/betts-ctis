'use client';
import { DatePicker } from '@/components/ui/date-picker'

import React, { useState } from 'react';
import { useForm } from 'react-hook-form';
import { zodResolver } from '@hookform/resolvers/zod';
import * as z from 'zod';
import { Card, CardContent, CardDescription, CardHeader, CardTitle } from '@/components/ui/card';
import { Button } from '@/components/ui/button';
import { Input } from '@/components/ui/input';
import { Label } from '@/components/ui/label';
import { Select, SelectContent, SelectItem, SelectTrigger, SelectValue } from '@/components/ui/select';
import { Badge } from '@/components/ui/badge';
import { Alert, AlertDescription } from '@/components/ui/alert';
import { Separator } from '@/components/ui/separator';
// Calendar deprecated; using DatePicker
import { Popover, PopoverContent, PopoverTrigger } from '@/components/ui/popover';
import { 
  Calculator, 
  AlertTriangle, 
  Info,
  CalendarIcon,
  DollarSign,
  Clock,
  FileX,
  TrendingDown,
  Scale
} from 'lucide-react';
import { format, differenceInDays } from 'date-fns';
import { cn } from '@/lib/utils';
import { useToast } from '@/components/ui/use-toast';
import { TaxCalculationService, PenaltyCalculationRequest, PenaltyCalculation } from '@/lib/services/tax-calculation-service';

const penaltySchema = z.object({
  taxAmount: z.number().min(0, 'Tax amount must be positive'),
  dueDate: z.date({ required_error: 'Due date is required' }),
  actualDate: z.date({ required_error: 'Actual date is required' }),
  taxType: z.enum(['Income Tax', 'GST', 'Payroll Tax', 'Excise Duty']),
});

type PenaltyFormData = z.infer<typeof penaltySchema>;

const taxTypes = [
  { 
    value: 'Income Tax', 
    label: 'Income Tax',
    description: 'Individual and corporate income tax',
    baseRate: 5,
    dailyRate: 0.05
  },
  { 
    value: 'GST', 
    label: 'GST',
    description: 'Goods and Services Tax',
    baseRate: 10,
    dailyRate: 0.1
  },
  { 
    value: 'Payroll Tax', 
    label: 'Payroll Tax',
    description: 'PAYE and Skills Development Levy',
    baseRate: 15,
    dailyRate: 0.15
  },
  { 
    value: 'Excise Duty', 
    label: 'Excise Duty',
    description: 'Tobacco, alcohol, and fuel duties',
    baseRate: 20,
    dailyRate: 0.2
  },
];

const penaltyScenarios = [
  {
    name: 'Late Filing Only',
    description: 'Filed late but paid on time',
    taxAmount: 5000000,
    daysLate: 15,
    taxType: 'Income Tax' as const
  },
  {
    name: 'Late Payment Only',
    description: 'Filed on time but paid late',
    taxAmount: 3000000,
    daysLate: 30,
    taxType: 'GST' as const
  },
  {
    name: 'Both Late',
    description: 'Both filing and payment late',
    taxAmount: 8000000,
    daysLate: 60,
    taxType: 'Payroll Tax' as const
  },
];

interface PenaltyCalculatorFormProps {
  onCalculationComplete?: (calculation: PenaltyCalculation) => void;
  initialData?: Partial<PenaltyFormData>;
}

export default function PenaltyCalculatorForm({ 
  onCalculationComplete, 
  initialData 
}: PenaltyCalculatorFormProps) {
  const { toast } = useToast();
  const [calculation, setCalculation] = useState<PenaltyCalculation | null>(null);
  const [loading, setLoading] = useState(false);

  const form = useForm<PenaltyFormData>({
    resolver: zodResolver(penaltySchema),
    defaultValues: {
      taxAmount: 0,
      taxType: 'Income Tax',
      ...initialData,
    },
  });

  const onSubmit = async (data: PenaltyFormData) => {
    setLoading(true);
    try {
      const request: PenaltyCalculationRequest = {
        ...data,
      };

      const result = await TaxCalculationService.calculatePenalties(request);
      setCalculation(result);
      onCalculationComplete?.(result);

      toast({
        title: 'Penalty Calculation Complete',
        description: `Total penalty: SLE ${result.totalPenalty.toLocaleString()}`,
      });
    } catch (error) {
      console.error('Penalty calculation error:', error);
      toast({
        variant: 'destructive',
        title: 'Calculation Error',
        description: error instanceof Error ? error.message : 'Failed to calculate penalty',
      });
    } finally {
      setLoading(false);
    }
  };

  const formatCurrency = (amount: number) => {
    return `SLE ${amount.toLocaleString('en-US', { minimumFractionDigits: 0, maximumFractionDigits: 0 })}`;
  };

  const loadScenario = (scenario: typeof penaltyScenarios[0]) => {
    const dueDate = new Date();
    dueDate.setDate(dueDate.getDate() - scenario.daysLate);
    
    form.setValue('taxAmount', scenario.taxAmount);
    form.setValue('taxType', scenario.taxType);
    form.setValue('dueDate', dueDate);
    form.setValue('actualDate', new Date());
  };

  const selectedTaxType = taxTypes.find(type => type.value === form.watch('taxType'));
  const dueDate = form.watch('dueDate');
  const actualDate = form.watch('actualDate');
  const daysLate = dueDate && actualDate ? Math.max(0, differenceInDays(actualDate, dueDate)) : 0;

  const calculatePreviewPenalty = () => {
    if (!selectedTaxType || !form.watch('taxAmount') || daysLate <= 0) return null;

    const taxAmount = form.watch('taxAmount');
    const lateFilingPenalty = taxAmount * (selectedTaxType.baseRate / 100);
    const latePaymentInterest = (taxAmount * (selectedTaxType.dailyRate / 100) * daysLate) / 365;
    const totalPenalty = lateFilingPenalty + latePaymentInterest;

    return {
      lateFilingPenalty,
      latePaymentInterest,
      totalPenalty,
      daysLate
    };
  };

  const previewPenalty = calculatePreviewPenalty();

  return (
    <div className="space-y-6">
      <Card>
        <CardHeader>
          <CardTitle className="flex items-center gap-2">
            <Scale className="h-5 w-5 text-sierra-blue" />
            Sierra Leone Tax Penalty Calculator
          </CardTitle>
          <CardDescription>
            Calculate penalties for late filing and payment based on Finance Act 2025
          </CardDescription>
        </CardHeader>
        <CardContent>
          <form onSubmit={form.handleSubmit(onSubmit)} className="space-y-6">
            {/* Scenario Quick Load */}
            <Card className="bg-gray-50 border-gray-200">
              <CardHeader className="pb-2">
                <CardTitle className="text-sm">Quick Load Scenarios</CardTitle>
              </CardHeader>
              <CardContent className="pt-0">
                <div className="grid grid-cols-1 md:grid-cols-3 gap-2">
                  {penaltyScenarios.map((scenario, index) => (
                    <Button
                      key={index}
                      type="button"
                      variant="outline"
                      size="sm"
                      onClick={() => loadScenario(scenario)}
                      className="h-auto p-3 text-left"
                    >
                      <div>
                        <div className="font-medium text-sm">{scenario.name}</div>
                        <div className="text-xs text-muted-foreground">{scenario.description}</div>
                        <div className="text-xs mt-1">
                          {formatCurrency(scenario.taxAmount)} | {scenario.daysLate} days late
                        </div>
                      </div>
                    </Button>
                  ))}
                </div>
              </CardContent>
            </Card>

            {/* Input Form */}
            <div className="grid grid-cols-1 md:grid-cols-2 gap-6">
              <div className="space-y-4">
                <div className="space-y-2">
                  <Label htmlFor="taxType">Tax Type</Label>
                  <Select
                    value={form.watch('taxType')}
                    onValueChange={(value) => form.setValue('taxType', value as any)}
                  >
                    <SelectTrigger>
                      <SelectValue placeholder="Select tax type" />
                    </SelectTrigger>
                    <SelectContent>
                      {taxTypes.map((type) => (
                        <SelectItem key={type.value} value={type.value}>
                          <div>
                            <div className="font-medium">{type.label}</div>
                            <div className="text-xs text-muted-foreground">{type.description}</div>
                          </div>
                        </SelectItem>
                      ))}
                    </SelectContent>
                  </Select>
                </div>

                <div className="space-y-2">
                  <Label htmlFor="taxAmount">Tax Amount (SLE)</Label>
                  <Input
                    id="taxAmount"
                    type="number"
                    placeholder="Enter original tax amount"
                    {...form.register('taxAmount', { valueAsNumber: true })}
                    className="text-right"
                  />
                  {form.formState.errors.taxAmount && (
                    <p className="text-sm text-red-600">{form.formState.errors.taxAmount.message}</p>
                  )}
                </div>

                {/* Tax Type Information */}
                {selectedTaxType && (
                  <Card className="bg-sierra-blue-50 border-sierra-blue-200">
                    <CardContent className="p-3">
                      <div className="space-y-2 text-sm">
                        <div className="flex justify-between">
                          <span>Late Filing Penalty:</span>
                          <Badge variant="outline">{selectedTaxType.baseRate}%</Badge>
                        </div>
                        <div className="flex justify-between">
                          <span>Daily Interest Rate:</span>
                          <Badge variant="outline">{selectedTaxType.dailyRate}% per day</Badge>
                        </div>
                      </div>
                    </CardContent>
                  </Card>
                )}
              </div>

              <div className="space-y-4">
                <div className="space-y-2">
                  <Label>Due Date</Label>
                    <DatePicker
                      value={form.watch('dueDate') ?? null}
                      onChange={(d) => form.setValue('dueDate', d!)}
                    />
                  {form.formState.errors.dueDate && (
                    <p className="text-sm text-red-600">{form.formState.errors.dueDate.message}</p>
                  )}
                </div>

                <div className="space-y-2">
                  <Label>Actual Filing/Payment Date</Label>
                    <DatePicker
                      value={form.watch('actualDate') ?? null}
                      onChange={(d) => form.setValue('actualDate', d!)}
                    />
                  {form.formState.errors.actualDate && (
                    <p className="text-sm text-red-600">{form.formState.errors.actualDate.message}</p>
                  )}
                </div>

                {/* Days Late Display */}
                {dueDate && actualDate && (
                  <Card className={`${daysLate > 0 ? 'border-red-200 bg-red-50' : 'border-green-200 bg-green-50'}`}>
                    <CardContent className="p-3">
                      <div className="flex items-center justify-between">
                        <div className="flex items-center gap-2">
                          <Clock className={`h-4 w-4 ${daysLate > 0 ? 'text-red-600' : 'text-green-600'}`} />
                          <span className="font-medium">
                            {daysLate > 0 ? `${daysLate} days late` : 'On time'}
                          </span>
                        </div>
                        <Badge variant={daysLate > 0 ? "destructive" : "default"}>
                          {daysLate > 0 ? 'Late' : 'On Time'}
                        </Badge>
                      </div>
                    </CardContent>
                  </Card>
                )}
              </div>
            </div>

            {/* Penalty Preview */}
            {previewPenalty && (
              <Card className="border-sierra-blue-200 bg-sierra-blue-50">
                <CardHeader>
                  <CardTitle className="text-base text-sierra-blue-900">Penalty Preview</CardTitle>
                </CardHeader>
                <CardContent>
                  <div className="grid grid-cols-1 md:grid-cols-3 gap-4">
                    <div className="text-center">
                      <div className="text-2xl font-bold text-sierra-blue-600">
                        {formatCurrency(previewPenalty.lateFilingPenalty)}
                      </div>
                      <div className="text-sm text-muted-foreground">Late Filing Penalty</div>
                      <div className="text-xs text-muted-foreground">
                        {selectedTaxType?.baseRate}% of tax amount
                      </div>
                    </div>
                    <div className="text-center">
                      <div className="text-2xl font-bold text-sierra-gold-600">
                        {formatCurrency(previewPenalty.latePaymentInterest)}
                      </div>
                      <div className="text-sm text-muted-foreground">Late Payment Interest</div>
                      <div className="text-xs text-muted-foreground">
                        {selectedTaxType?.dailyRate}% daily × {daysLate} days
                      </div>
                    </div>
                    <div className="text-center">
                      <div className="text-2xl font-bold text-sierra-green-600">
                        {formatCurrency(previewPenalty.totalPenalty)}
                      </div>
                      <div className="text-sm text-muted-foreground">Total Penalty</div>
                      <div className="text-xs text-muted-foreground">
                        Combined penalties
                      </div>
                    </div>
                  </div>
                </CardContent>
              </Card>
            )}

            <Separator />

            <div className="flex justify-end gap-2">
              <Button type="button" variant="outline" onClick={() => form.reset()}>
                Reset
              </Button>
              <Button type="submit" disabled={loading}>
                {loading ? 'Calculating...' : 'Calculate Detailed Penalty'}
              </Button>
            </div>
          </form>
        </CardContent>
      </Card>

      {/* Results */}
      {calculation && (
        <PenaltyCalculationResults calculation={calculation} />
      )}

      {/* Penalty Guidance */}
      <Card className="border-sierra-blue-200 bg-sierra-blue-50">
        <CardHeader>
          <CardTitle className="text-sierra-blue-800">Sierra Leone Tax Penalty Guidelines</CardTitle>
        </CardHeader>
        <CardContent>
          <div className="grid grid-cols-1 md:grid-cols-2 gap-4">
            <div>
              <h4 className="font-medium mb-2">Late Filing Penalties</h4>
              <ul className="text-sm space-y-1">
                <li>• Income Tax: 5% of tax due</li>
                <li>• GST: 10% of tax due</li>
                <li>• Payroll Tax: 15% of tax due</li>
                <li>• Excise Duty: 20% of tax due</li>
              </ul>
            </div>
            <div>
              <h4 className="font-medium mb-2">Late Payment Interest</h4>
              <ul className="text-sm space-y-1">
                <li>• Calculated daily from due date</li>
                <li>• Compounds monthly</li>
                <li>• Minimum penalty may apply</li>
                <li>• Interest stops when paid in full</li>
              </ul>
            </div>
          </div>
        </CardContent>
      </Card>
    </div>
  );
}

// Penalty Calculation Results Component
function PenaltyCalculationResults({ calculation }: { calculation: PenaltyCalculation }) {
  const formatCurrency = (amount: number) => {
    return `SLE ${amount.toLocaleString('en-US', { minimumFractionDigits: 0, maximumFractionDigits: 0 })}`;
  };

  const getSeverityColor = (daysLate: number) => {
    if (daysLate <= 7) return 'text-yellow-600';
    if (daysLate <= 30) return 'text-orange-600';
    return 'text-red-600';
  };

  const getSeverityBadge = (daysLate: number) => {
    if (daysLate <= 7) return { variant: 'secondary' as const, label: 'Minor' };
    if (daysLate <= 30) return { variant: 'destructive' as const, label: 'Moderate' };
    return { variant: 'destructive' as const, label: 'Severe' };
  };

  const severity = getSeverityBadge(calculation.daysLate);

  return (
    <div className="space-y-6">
      {/* Summary Cards */}
      <div className="grid grid-cols-1 md:grid-cols-4 gap-4">
        <Card className="border-gray-200 bg-gray-50">
          <CardContent className="p-4">
            <div className="flex items-center justify-between">
              <div>
                <p className="text-sm text-muted-foreground font-medium">Original Tax</p>
                <p className="text-xl font-bold">
                  {formatCurrency(calculation.taxAmount)}
                </p>
              </div>
              <DollarSign className="h-6 w-6 text-muted-foreground" />
            </div>
          </CardContent>
        </Card>

        <Card className="border-yellow-200 bg-yellow-50">
          <CardContent className="p-4">
            <div className="flex items-center justify-between">
              <div>
                <p className="text-sm text-yellow-600 font-medium">Days Late</p>
                <p className={`text-xl font-bold ${getSeverityColor(calculation.daysLate)}`}>
                  {calculation.daysLate}
                </p>
              </div>
              <Clock className={`h-6 w-6 ${getSeverityColor(calculation.daysLate)}`} />
            </div>
          </CardContent>
        </Card>

        <Card className="border-sierra-blue-200 bg-sierra-blue-50">
          <CardContent className="p-4">
            <div className="flex items-center justify-between">
              <div>
                <p className="text-sm text-sierra-blue-600 font-medium">Filing Penalty</p>
                <p className="text-xl font-bold text-sierra-blue-800">
                  {formatCurrency(calculation.lateFilingPenalty)}
                </p>
              </div>
              <FileX className="h-6 w-6 text-sierra-blue-600" />
            </div>
          </CardContent>
        </Card>

        <Card className="border-red-200 bg-red-50">
          <CardContent className="p-4">
            <div className="flex items-center justify-between">
              <div>
                <p className="text-sm text-red-600 font-medium">Total Penalty</p>
                <p className="text-xl font-bold text-red-800">
                  {formatCurrency(calculation.totalPenalty)}
                </p>
              </div>
              <AlertTriangle className="h-6 w-6 text-red-600" />
            </div>
          </CardContent>
        </Card>
      </div>

      {/* Penalty Breakdown */}
      <Card>
        <CardHeader>
          <CardTitle className="flex items-center justify-between">
            Penalty Breakdown
            <Badge variant={severity.variant}>{severity.label} Violation</Badge>
          </CardTitle>
        </CardHeader>
        <CardContent className="space-y-4">
          <div className="grid grid-cols-2 gap-2 text-sm">
            <div className="flex justify-between">
              <span>Original Tax Amount:</span>
              <span className="font-medium">{formatCurrency(calculation.taxAmount)}</span>
            </div>
            <div className="flex justify-between">
              <span>Days Late:</span>
              <span className={`font-medium ${getSeverityColor(calculation.daysLate)}`}>
                {calculation.daysLate} days
              </span>
            </div>
            <div className="flex justify-between">
              <span>Late Filing Penalty:</span>
              <span className="font-medium text-sierra-blue-600">
                {formatCurrency(calculation.lateFilingPenalty)}
              </span>
            </div>
            <div className="flex justify-between">
              <span>Late Payment Interest:</span>
              <span className="font-medium text-sierra-gold-600">
                {formatCurrency(calculation.latePaymentInterest)}
              </span>
            </div>
            <div className="flex justify-between border-t pt-2 font-medium">
              <span>Total Penalty:</span>
              <span className="text-red-600">{formatCurrency(calculation.totalPenalty)}</span>
            </div>
            <div className="flex justify-between border-t pt-2 font-bold">
              <span>Total Amount Due:</span>
              <span className="text-red-800">
                {formatCurrency(calculation.taxAmount + calculation.totalPenalty)}
              </span>
            </div>
          </div>
        </CardContent>
      </Card>

      {/* Detailed Penalty Breakdown */}
      {calculation.penaltyBreakdown && calculation.penaltyBreakdown.length > 0 && (
        <Card>
          <CardHeader>
            <CardTitle>Detailed Penalty Calculation</CardTitle>
          </CardHeader>
          <CardContent>
            <div className="space-y-3">
              {calculation.penaltyBreakdown.map((item, index) => (
                <div key={index} className="p-3 border rounded-lg">
                  <div className="flex justify-between items-start mb-2">
                    <div>
                      <div className="font-medium">{item.type}</div>
                      <div className="text-sm text-muted-foreground">{item.description}</div>
                    </div>
                    <div className="text-right">
                      <div className="font-medium text-red-600">{formatCurrency(item.amount)}</div>
                    </div>
                  </div>
                  <div className="text-xs text-muted-foreground bg-gray-50 p-2 rounded">
                    Calculation: {item.calculation}
                  </div>
                </div>
              ))}
            </div>
          </CardContent>
        </Card>
      )}

      {/* Penalty Mitigation Advice */}
      <Alert>
        <TrendingDown className="h-4 w-4" />
        <AlertDescription>
          <strong>Penalty Mitigation:</strong> Contact NRA immediately to discuss payment plans or penalty waivers. 
          First-time offenders may qualify for reduced penalties. Voluntary disclosure before audit can reduce penalties by up to 50%.
        </AlertDescription>
      </Alert>
    </div>
  );
}