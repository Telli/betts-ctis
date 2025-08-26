'use client';

import { useState } from 'react';
import { useForm } from 'react-hook-form';
import { zodResolver } from '@hookform/resolvers/zod';
import * as z from 'zod';
import { Card, CardContent, CardDescription, CardHeader, CardTitle } from '@/components/ui/card';
import { Button } from '@/components/ui/button';
import { Input } from '@/components/ui/input';
import { Label } from '@/components/ui/label';
import { Select, SelectContent, SelectItem, SelectTrigger, SelectValue } from '@/components/ui/select';
import { Tabs, TabsContent, TabsList, TabsTrigger } from '@/components/ui/tabs';
import { Badge } from '@/components/ui/badge';
import { Alert, AlertDescription } from '@/components/ui/alert';
import { Separator } from '@/components/ui/separator';
import { Calendar } from '@/components/ui/calendar';
import { Popover, PopoverContent, PopoverTrigger } from '@/components/ui/popover';
import { 
  Calculator, 
  TrendingUp, 
  AlertTriangle, 
  Info,
  CalendarIcon,
  FileText,
  DollarSign,
  ArrowUpDown,
  Import,
  Export
} from 'lucide-react';
import { format } from 'date-fns';
import { cn } from '@/lib/utils';
import { useToast } from '@/components/ui/use-toast';
import { TaxCalculationService, GstCalculationRequest, GstCalculation } from '@/lib/services/tax-calculation-service';

const gstSchema = z.object({
  taxYear: z.number().min(2020).max(2025),
  grossSales: z.number().min(0, 'Gross sales must be positive'),
  taxableSupplies: z.number().min(0, 'Taxable supplies must be positive'),
  exemptSupplies: z.number().min(0, 'Exempt supplies must be positive'),
  zeroRatedSupplies: z.number().min(0, 'Zero-rated supplies must be positive'),
  inputTax: z.number().min(0, 'Input tax must be positive'),
  isExport: z.boolean(),
  isImport: z.boolean(),
  importValue: z.number().min(0, 'Import value must be positive'),
  dueDate: z.date().optional(),
  filingDate: z.date().optional(),
});

type GstFormData = z.infer<typeof gstSchema>;

const gstExemptions = [
  { category: 'Basic Food Items', description: 'Rice, bread, milk, meat, fish' },
  { category: 'Medical Services', description: 'Hospital services, medical consultations' },
  { category: 'Educational Services', description: 'School fees, educational materials' },
  { category: 'Financial Services', description: 'Banking, insurance services' },
  { category: 'Public Transport', description: 'Bus, taxi services' },
  { category: 'Residential Rent', description: 'Rental of residential properties' },
];

const zeroRatedSupplies = [
  { category: 'Exports', description: 'Goods and services exported outside Sierra Leone' },
  { category: 'International Transport', description: 'Air and sea transport services' },
  { category: 'Diplomatic Supplies', description: 'Supplies to diplomatic missions' },
  { category: 'Charitable Organizations', description: 'Supplies to registered charities' },
];

interface GstCalculatorFormProps {
  onCalculationComplete?: (calculation: GstCalculation) => void;
  initialData?: Partial<GstFormData>;
}

export default function GstCalculatorForm({ 
  onCalculationComplete, 
  initialData 
}: GstCalculatorFormProps) {
  const { toast } = useToast();
  const [calculation, setCalculation] = useState<GstCalculation | null>(null);
  const [loading, setLoading] = useState(false);

  const form = useForm<GstFormData>({
    resolver: zodResolver(gstSchema),
    defaultValues: {
      taxYear: 2024,
      grossSales: 0,
      taxableSupplies: 0,
      exemptSupplies: 0,
      zeroRatedSupplies: 0,
      inputTax: 0,
      isExport: false,
      isImport: false,
      importValue: 0,
      ...initialData,
    },
  });

  const onSubmit = async (data: GstFormData) => {
    setLoading(true);
    try {
      const request: GstCalculationRequest = {
        ...data,
        dueDate: data.dueDate,
        filingDate: data.filingDate,
      };

      const result = await TaxCalculationService.calculateGst(request);
      setCalculation(result);
      onCalculationComplete?.(result);

      toast({
        title: 'GST Calculation Complete',
        description: `Net GST liability: SLE ${result.netGstLiability.toLocaleString()}`,
      });
    } catch (error) {
      console.error('GST calculation error:', error);
      toast({
        variant: 'destructive',
        title: 'Calculation Error',
        description: error instanceof Error ? error.message : 'Failed to calculate GST',
      });
    } finally {
      setLoading(false);
    }
  };

  const formatCurrency = (amount: number) => {
    return `SLE ${amount.toLocaleString('en-US', { minimumFractionDigits: 0, maximumFractionDigits: 0 })}`;
  };

  const getGstRegistrationThreshold = () => {
    return 'SLE 500,000,000'; // 500 million Leone
  };

  const isGstRegistrationRequired = () => {
    return form.watch('grossSales') > 500000000;
  };

  return (
    <div className="space-y-6">
      <Card>
        <CardHeader>
          <CardTitle className="flex items-center gap-2">
            <Calculator className="h-5 w-5 text-sierra-blue" />
            Sierra Leone GST Calculator
          </CardTitle>
          <CardDescription>
            Calculate Goods and Services Tax (15% standard rate) based on Finance Act 2025
          </CardDescription>
        </CardHeader>
        <CardContent>
          <form onSubmit={form.handleSubmit(onSubmit)} className="space-y-6">
            <Tabs defaultValue="basic" className="w-full">
              <TabsList className="grid w-full grid-cols-4">
                <TabsTrigger value="basic">Basic Details</TabsTrigger>
                <TabsTrigger value="supplies">Supplies Breakdown</TabsTrigger>
                <TabsTrigger value="trade">Import/Export</TabsTrigger>
                <TabsTrigger value="results">Results</TabsTrigger>
              </TabsList>

              {/* Basic Details Tab */}
              <TabsContent value="basic" className="space-y-4">
                <div className="grid grid-cols-1 md:grid-cols-2 gap-4">
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
                    <Label htmlFor="grossSales">Gross Sales (SLE)</Label>
                    <Input
                      id="grossSales"
                      type="number"
                      placeholder="Enter total gross sales"
                      {...form.register('grossSales', { valueAsNumber: true })}
                      className="text-right"
                    />
                    {form.formState.errors.grossSales && (
                      <p className="text-sm text-red-600">{form.formState.errors.grossSales.message}</p>
                    )}
                  </div>

                  <div className="space-y-2">
                    <Label htmlFor="inputTax">Input Tax Claimed (SLE)</Label>
                    <Input
                      id="inputTax"
                      type="number"
                      placeholder="Enter input tax amount"
                      {...form.register('inputTax', { valueAsNumber: true })}
                      className="text-right"
                    />
                    {form.formState.errors.inputTax && (
                      <p className="text-sm text-red-600">{form.formState.errors.inputTax.message}</p>
                    )}
                  </div>
                </div>

                {/* GST Registration Status */}
                <Card className={`${isGstRegistrationRequired() ? 'border-sierra-blue-200 bg-sierra-blue-50' : 'border-gray-200 bg-gray-50'}`}>
                  <CardHeader className="pb-2">
                    <CardTitle className="text-sm flex items-center gap-2">
                      <FileText className="h-4 w-4" />
                      GST Registration Status
                    </CardTitle>
                  </CardHeader>
                  <CardContent className="pt-0">
                    <div className="space-y-2">
                      <div className="flex justify-between items-center">
                        <span className="text-sm">Registration Threshold:</span>
                        <Badge variant="outline">{getGstRegistrationThreshold()}</Badge>
                      </div>
                      <div className="flex justify-between items-center">
                        <span className="text-sm">Your Gross Sales:</span>
                        <span className="font-medium">{formatCurrency(form.watch('grossSales'))}</span>
                      </div>
                      <div className="flex justify-between items-center">
                        <span className="text-sm">Registration Required:</span>
                        <Badge variant={isGstRegistrationRequired() ? "destructive" : "secondary"}>
                          {isGstRegistrationRequired() ? "Yes" : "No"}
                        </Badge>
                      </div>
                    </div>
                    {isGstRegistrationRequired() && (
                      <Alert className="mt-3">
                        <AlertTriangle className="h-4 w-4" />
                        <AlertDescription>
                          Your gross sales exceed the GST registration threshold. You must register for GST.
                        </AlertDescription>
                      </Alert>
                    )}
                  </CardContent>
                </Card>
              </TabsContent>

              {/* Supplies Breakdown Tab */}
              <TabsContent value="supplies" className="space-y-4">
                <div className="grid grid-cols-1 md:grid-cols-2 gap-6">
                  <div className="space-y-4">
                    <h3 className="text-lg font-medium">Supply Categories</h3>
                    
                    <div className="space-y-2">
                      <Label htmlFor="taxableSupplies">Taxable Supplies (15% GST)</Label>
                      <Input
                        id="taxableSupplies"
                        type="number"
                        placeholder="Enter taxable supplies value"
                        {...form.register('taxableSupplies', { valueAsNumber: true })}
                        className="text-right"
                      />
                    </div>

                    <div className="space-y-2">
                      <Label htmlFor="exemptSupplies">Exempt Supplies (0% GST)</Label>
                      <Input
                        id="exemptSupplies"
                        type="number"
                        placeholder="Enter exempt supplies value"
                        {...form.register('exemptSupplies', { valueAsNumber: true })}
                        className="text-right"
                      />
                    </div>

                    <div className="space-y-2">
                      <Label htmlFor="zeroRatedSupplies">Zero-Rated Supplies</Label>
                      <Input
                        id="zeroRatedSupplies"
                        type="number"
                        placeholder="Enter zero-rated supplies value"
                        {...form.register('zeroRatedSupplies', { valueAsNumber: true })}
                        className="text-right"
                      />
                    </div>

                    {/* Calculation Summary */}
                    <Card className="bg-sierra-gold-50 border-sierra-gold-200">
                      <CardContent className="p-4">
                        <div className="space-y-2 text-sm">
                          <div className="flex justify-between">
                            <span>Taxable Supplies:</span>
                            <span className="font-medium">{formatCurrency(form.watch('taxableSupplies'))}</span>
                          </div>
                          <div className="flex justify-between">
                            <span>Exempt Supplies:</span>
                            <span className="font-medium">{formatCurrency(form.watch('exemptSupplies'))}</span>
                          </div>
                          <div className="flex justify-between">
                            <span>Zero-Rated Supplies:</span>
                            <span className="font-medium">{formatCurrency(form.watch('zeroRatedSupplies'))}</span>
                          </div>
                          <Separator />
                          <div className="flex justify-between font-medium">
                            <span>Total Supplies:</span>
                            <span>{formatCurrency(form.watch('taxableSupplies') + form.watch('exemptSupplies') + form.watch('zeroRatedSupplies'))}</span>
                          </div>
                          <div className="flex justify-between font-medium text-sierra-blue-600">
                            <span>Output GST (15%):</span>
                            <span>{formatCurrency(form.watch('taxableSupplies') * 0.15)}</span>
                          </div>
                        </div>
                      </CardContent>
                    </Card>
                  </div>

                  <div className="space-y-4">
                    <div>
                      <h4 className="font-medium mb-3">GST Exemptions</h4>
                      <div className="space-y-2">
                        {gstExemptions.map((exemption, index) => (
                          <div key={index} className="p-3 border rounded-lg bg-gray-50">
                            <div className="font-medium text-sm">{exemption.category}</div>
                            <div className="text-xs text-muted-foreground">{exemption.description}</div>
                          </div>
                        ))}
                      </div>
                    </div>

                    <div>
                      <h4 className="font-medium mb-3">Zero-Rated Supplies</h4>
                      <div className="space-y-2">
                        {zeroRatedSupplies.map((supply, index) => (
                          <div key={index} className="p-3 border rounded-lg bg-gray-50">
                            <div className="font-medium text-sm">{supply.category}</div>
                            <div className="text-xs text-muted-foreground">{supply.description}</div>
                          </div>
                        ))}
                      </div>
                    </div>
                  </div>
                </div>
              </TabsContent>

              {/* Import/Export Tab */}
              <TabsContent value="trade" className="space-y-4">
                <div className="grid grid-cols-1 md:grid-cols-2 gap-6">
                  <Card>
                    <CardHeader>
                      <CardTitle className="text-base flex items-center gap-2">
                        <Export className="h-4 w-4 text-green-600" />
                        Export Activities
                      </CardTitle>
                    </CardHeader>
                    <CardContent className="space-y-4">
                      <div className="flex items-center space-x-2">
                        <input
                          type="checkbox"
                          id="isExport"
                          {...form.register('isExport')}
                          className="rounded border-gray-300"
                        />
                        <Label htmlFor="isExport">Business exports goods/services</Label>
                      </div>
                      
                      {form.watch('isExport') && (
                        <Alert>
                          <Info className="h-4 w-4" />
                          <AlertDescription>
                            Exports are zero-rated for GST. You can claim input tax credits but don't charge GST on exports.
                          </AlertDescription>
                        </Alert>
                      )}
                    </CardContent>
                  </Card>

                  <Card>
                    <CardHeader>
                      <CardTitle className="text-base flex items-center gap-2">
                        <Import className="h-4 w-4 text-blue-600" />
                        Import Activities
                      </CardTitle>
                    </CardHeader>
                    <CardContent className="space-y-4">
                      <div className="flex items-center space-x-2">
                        <input
                          type="checkbox"
                          id="isImport"
                          {...form.register('isImport')}
                          className="rounded border-gray-300"
                        />
                        <Label htmlFor="isImport">Business imports goods/services</Label>
                      </div>

                      {form.watch('isImport') && (
                        <>
                          <div className="space-y-2">
                            <Label htmlFor="importValue">Import Value (SLE)</Label>
                            <Input
                              id="importValue"
                              type="number"
                              placeholder="Enter total import value"
                              {...form.register('importValue', { valueAsNumber: true })}
                              className="text-right"
                            />
                          </div>
                          
                          <Alert>
                            <Info className="h-4 w-4" />
                            <AlertDescription>
                              Reverse charge GST may apply on imports. GST is payable at 15% of import value.
                            </AlertDescription>
                          </Alert>

                          {form.watch('importValue') > 0 && (
                            <div className="p-3 bg-blue-50 border border-blue-200 rounded">
                              <div className="text-sm font-medium text-blue-800">Reverse Charge GST</div>
                              <div className="text-sm text-blue-600">
                                {formatCurrency(form.watch('importValue') * 0.15)}
                              </div>
                            </div>
                          )}
                        </>
                      )}
                    </CardContent>
                  </Card>
                </div>

                {/* Dates */}
                <div className="grid grid-cols-1 md:grid-cols-2 gap-4">
                  <div className="space-y-2">
                    <Label>GST Due Date</Label>
                    <Popover>
                      <PopoverTrigger asChild>
                        <Button
                          variant="outline"
                          className={cn(
                            "w-full justify-start text-left font-normal",
                            !form.watch('dueDate') && "text-muted-foreground"
                          )}
                        >
                          <CalendarIcon className="mr-2 h-4 w-4" />
                          {form.watch('dueDate') ? (
                            format(form.watch('dueDate')!, "PPP")
                          ) : (
                            <span>Pick due date</span>
                          )}
                        </Button>
                      </PopoverTrigger>
                      <PopoverContent className="w-auto p-0">
                        <Calendar
                          mode="single"
                          selected={form.watch('dueDate')}
                          onSelect={(date) => form.setValue('dueDate', date)}
                          initialFocus
                        />
                      </PopoverContent>
                    </Popover>
                  </div>

                  <div className="space-y-2">
                    <Label>Filing Date</Label>
                    <Popover>
                      <PopoverTrigger asChild>
                        <Button
                          variant="outline"
                          className={cn(
                            "w-full justify-start text-left font-normal",
                            !form.watch('filingDate') && "text-muted-foreground"
                          )}
                        >
                          <CalendarIcon className="mr-2 h-4 w-4" />
                          {form.watch('filingDate') ? (
                            format(form.watch('filingDate')!, "PPP")
                          ) : (
                            <span>Pick filing date</span>
                          )}
                        </Button>
                      </PopoverTrigger>
                      <PopoverContent className="w-auto p-0">
                        <Calendar
                          mode="single"
                          selected={form.watch('filingDate')}
                          onSelect={(date) => form.setValue('filingDate', date)}
                          initialFocus
                        />
                      </PopoverContent>
                    </Popover>
                  </div>
                </div>
              </TabsContent>

              {/* Results Tab */}
              <TabsContent value="results" className="space-y-4">
                {calculation ? (
                  <GstCalculationResults calculation={calculation} />
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
                {loading ? 'Calculating...' : 'Calculate GST'}
              </Button>
            </div>
          </form>
        </CardContent>
      </Card>
    </div>
  );
}

// GST Calculation Results Component
function GstCalculationResults({ calculation }: { calculation: GstCalculation }) {
  const formatCurrency = (amount: number) => {
    return `SLE ${amount.toLocaleString('en-US', { minimumFractionDigits: 0, maximumFractionDigits: 0 })}`;
  };

  const getNetLiabilityColor = () => {
    if (calculation.netGstLiability > 0) return 'text-red-600';
    if (calculation.refundDue && calculation.refundDue > 0) return 'text-green-600';
    return 'text-gray-600';
  };

  const getNetLiabilityLabel = () => {
    if (calculation.netGstLiability > 0) return 'GST Payable';
    if (calculation.refundDue && calculation.refundDue > 0) return 'Refund Due';
    return 'Net Position';
  };

  return (
    <div className="space-y-6">
      {/* Summary Cards */}
      <div className="grid grid-cols-1 md:grid-cols-3 gap-4">
        <Card className="border-sierra-blue-200 bg-sierra-blue-50">
          <CardContent className="p-4">
            <div className="flex items-center justify-between">
              <div>
                <p className="text-sm text-sierra-blue-600 font-medium">Output GST</p>
                <p className="text-2xl font-bold text-sierra-blue-800">
                  {formatCurrency(calculation.outputGst)}
                </p>
              </div>
              <TrendingUp className="h-8 w-8 text-sierra-blue-600" />
            </div>
          </CardContent>
        </Card>

        <Card className="border-sierra-gold-200 bg-sierra-gold-50">
          <CardContent className="p-4">
            <div className="flex items-center justify-between">
              <div>
                <p className="text-sm text-sierra-gold-600 font-medium">Input Tax</p>
                <p className="text-2xl font-bold text-sierra-gold-800">
                  {formatCurrency(calculation.inputTax)}
                </p>
              </div>
              <ArrowUpDown className="h-8 w-8 text-sierra-gold-600" />
            </div>
          </CardContent>
        </Card>

        <Card className={`border-2 ${calculation.netGstLiability > 0 ? 'border-red-200 bg-red-50' : calculation.refundDue ? 'border-green-200 bg-green-50' : 'border-gray-200 bg-gray-50'}`}>
          <CardContent className="p-4">
            <div className="flex items-center justify-between">
              <div>
                <p className="text-sm font-medium">{getNetLiabilityLabel()}</p>
                <p className={`text-2xl font-bold ${getNetLiabilityColor()}`}>
                  {calculation.refundDue && calculation.refundDue > 0 
                    ? formatCurrency(calculation.refundDue)
                    : formatCurrency(Math.abs(calculation.netGstLiability))
                  }
                </p>
              </div>
              <DollarSign className={`h-8 w-8 ${getNetLiabilityColor().replace('text-', 'text-')}`} />
            </div>
          </CardContent>
        </Card>
      </div>

      {/* Calculation Breakdown */}
      <Card>
        <CardHeader>
          <CardTitle>GST Calculation Breakdown</CardTitle>
        </CardHeader>
        <CardContent className="space-y-4">
          <div className="grid grid-cols-2 gap-2 text-sm">
            <div className="flex justify-between">
              <span>Gross Sales:</span>
              <span className="font-medium">{formatCurrency(calculation.grossSales)}</span>
            </div>
            <div className="flex justify-between">
              <span>Taxable Supplies:</span>
              <span className="font-medium">{formatCurrency(calculation.taxableSupplies)}</span>
            </div>
            <div className="flex justify-between">
              <span>Exempt Supplies:</span>
              <span className="font-medium">{formatCurrency(calculation.exemptSupplies)}</span>
            </div>
            <div className="flex justify-between">
              <span>Zero-Rated Supplies:</span>
              <span className="font-medium">{formatCurrency(calculation.zeroRatedSupplies)}</span>
            </div>
            <div className="flex justify-between border-t pt-2 font-medium">
              <span>Output GST (15%):</span>
              <span className="text-sierra-blue-600">{formatCurrency(calculation.outputGst)}</span>
            </div>
            <div className="flex justify-between border-t pt-2 font-medium">
              <span>Input Tax Credits:</span>
              <span className="text-sierra-gold-600">-{formatCurrency(calculation.inputTax)}</span>
            </div>
          </div>
        </CardContent>
      </Card>

      {/* Reverse Charge GST */}
      {calculation.reverseChargeGst && calculation.reverseChargeGst > 0 && (
        <Card className="border-blue-200 bg-blue-50">
          <CardHeader>
            <CardTitle className="text-blue-800">Reverse Charge GST</CardTitle>
          </CardHeader>
          <CardContent>
            <div className="flex justify-between">
              <span>Reverse Charge GST:</span>
              <span className="font-medium text-blue-600">
                {formatCurrency(calculation.reverseChargeGst)}
              </span>
            </div>
            <Alert className="mt-3">
              <Info className="h-4 w-4" />
              <AlertDescription>
                Reverse charge GST applies to imports. This amount is both payable as output tax and claimable as input tax.
              </AlertDescription>
            </Alert>
          </CardContent>
        </Card>
      )}

      {/* Penalties */}
      {calculation.penalties && (
        <Card className="border-red-200 bg-red-50">
          <CardHeader>
            <CardTitle className="text-red-800">Penalties Applied</CardTitle>
          </CardHeader>
          <CardContent>
            <div className="space-y-2">
              <div className="flex justify-between">
                <span>Late Filing Penalty:</span>
                <span className="font-medium text-red-600">
                  {formatCurrency(calculation.penalties.lateFilingPenalty)}
                </span>
              </div>
              <div className="flex justify-between">
                <span>Late Payment Interest:</span>
                <span className="font-medium text-red-600">
                  {formatCurrency(calculation.penalties.latePaymentInterest)}
                </span>
              </div>
              <Separator />
              <div className="flex justify-between font-medium">
                <span>Total Penalties:</span>
                <span className="text-red-600">
                  {formatCurrency(calculation.penalties.totalPenalty)}
                </span>
              </div>
            </div>
          </CardContent>
        </Card>
      )}

      {/* GST Compliance Information */}
      <Alert>
        <Info className="h-4 w-4" />
        <AlertDescription>
          GST returns must be filed monthly by the 15th of the following month. 
          Payment is due on the same date. Late filing and payment attract penalties.
        </AlertDescription>
      </Alert>
    </div>
  );
}