'use client';

import React, { useState } from 'react';
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
import { Alert, AlertDescription } from '@/components/ui/alert';
import { Separator } from '@/components/ui/separator';
import { Calendar } from '@/components/ui/calendar';
import { Popover, PopoverContent, PopoverTrigger } from '@/components/ui/popover';
import { 
  Calculator, 
  Plus,
  Minus,
  AlertTriangle, 
  Info,
  CalendarIcon,
  FileText,
  DollarSign,
  Package,
  Cigarette,
  Wine,
  Fuel
} from 'lucide-react';
import { format } from 'date-fns';
import { cn } from '@/lib/utils';
import { useToast } from '@/components/ui/use-toast';
import { TaxCalculationService, ExciseDutyCalculationRequest, ExciseDutyCalculation, ExciseDutyItem } from '@/lib/services/tax-calculation-service';

const exciseDutyItemSchema = z.object({
  productCode: z.string().min(1, 'Product code is required'),
  productName: z.string().min(1, 'Product name is required'),
  quantity: z.number().min(0, 'Quantity must be positive'),
  value: z.number().min(0, 'Value must be positive'),
});

const exciseDutySchema = z.object({
  taxYear: z.number().min(2020).max(2025),
  productCategory: z.enum(['Tobacco', 'Alcohol', 'Fuel']),
  items: z.array(exciseDutyItemSchema).min(1, 'At least one item is required'),
  dueDate: z.date().optional(),
  paymentDate: z.date().optional(),
});

type ExciseDutyFormData = z.infer<typeof exciseDutySchema>;

const productCategories = [
  { 
    value: 'Tobacco', 
    label: 'Tobacco Products',
    icon: Cigarette,
    description: 'Cigarettes, cigars, tobacco leaves, etc.',
    color: 'text-red-600'
  },
  { 
    value: 'Alcohol', 
    label: 'Alcoholic Beverages',
    icon: Wine,
    description: 'Beer, wine, spirits, liquor, etc.',
    color: 'text-purple-600'
  },
  { 
    value: 'Fuel', 
    label: 'Petroleum Products',
    icon: Fuel,
    description: 'Petrol, diesel, kerosene, etc.',
    color: 'text-blue-600'
  },
];

const sampleProducts = {
  Tobacco: [
    { productCode: 'TOB001', productName: 'Cigarettes (Local)', quantity: 1000, value: 5000000 },
    { productCode: 'TOB002', productName: 'Imported Cigarettes', quantity: 500, value: 3000000 },
    { productCode: 'TOB003', productName: 'Cigars', quantity: 100, value: 2000000 },
  ],
  Alcohol: [
    { productCode: 'ALC001', productName: 'Local Beer', quantity: 2000, value: 8000000 },
    { productCode: 'ALC002', productName: 'Imported Wine', quantity: 500, value: 6000000 },
    { productCode: 'ALC003', productName: 'Spirits', quantity: 300, value: 4500000 },
  ],
  Fuel: [
    { productCode: 'FUEL001', productName: 'Premium Petrol', quantity: 10000, value: 15000000 },
    { productCode: 'FUEL002', productName: 'Diesel', quantity: 8000, value: 12000000 },
    { productCode: 'FUEL003', productName: 'Kerosene', quantity: 5000, value: 7000000 },
  ],
};

// Sierra Leone Excise Duty Rates (example rates)
const exciseDutyRates = {
  Tobacco: {
    specific: 150, // SLE per unit
    adValorem: 25, // 25% of value
    description: 'SLE 150 per unit + 25% ad valorem'
  },
  Alcohol: {
    specific: 80, // SLE per unit
    adValorem: 20, // 20% of value
    description: 'SLE 80 per unit + 20% ad valorem'
  },
  Fuel: {
    specific: 500, // SLE per litre
    adValorem: 15, // 15% of value
    description: 'SLE 500 per litre + 15% ad valorem'
  },
};

interface ExciseDutyCalculatorFormProps {
  onCalculationComplete?: (calculation: ExciseDutyCalculation) => void;
  initialData?: Partial<ExciseDutyFormData>;
}

export default function ExciseDutyCalculatorForm({ 
  onCalculationComplete, 
  initialData 
}: ExciseDutyCalculatorFormProps) {
  const { toast } = useToast();
  const [calculation, setCalculation] = useState<ExciseDutyCalculation | null>(null);
  const [loading, setLoading] = useState(false);

  const form = useForm<ExciseDutyFormData>({
    resolver: zodResolver(exciseDutySchema),
    defaultValues: {
      taxYear: 2024,
      productCategory: 'Tobacco',
      items: [],
      ...initialData,
    },
  });

  const { fields: itemFields, append: appendItem, remove: removeItem } = useFieldArray({
    control: form.control,
    name: 'items',
  });

  const onSubmit = async (data: ExciseDutyFormData) => {
    setLoading(true);
    try {
      const request: ExciseDutyCalculationRequest = {
        ...data,
        dueDate: data.dueDate,
        paymentDate: data.paymentDate,
      };

      const result = await TaxCalculationService.calculateExciseDuty(request);
      setCalculation(result);
      onCalculationComplete?.(result);

      toast({
        title: 'Excise Duty Calculation Complete',
        description: `Total excise duty: SLE ${result.totalExciseDuty.toLocaleString()}`,
      });
    } catch (error) {
      console.error('Excise duty calculation error:', error);
      toast({
        variant: 'destructive',
        title: 'Calculation Error',
        description: error instanceof Error ? error.message : 'Failed to calculate excise duty',
      });
    } finally {
      setLoading(false);
    }
  };

  const formatCurrency = (amount: number) => {
    return `SLE ${amount.toLocaleString('en-US', { minimumFractionDigits: 0, maximumFractionDigits: 0 })}`;
  };

  const addSampleProduct = () => {
    const category = form.watch('productCategory');
    const samples = sampleProducts[category];
    const product = samples[itemFields.length % samples.length];
    appendItem(product);
  };

  const selectedCategory = productCategories.find(cat => cat.value === form.watch('productCategory'));
  const categoryRates = exciseDutyRates[form.watch('productCategory')];

  const calculatePreviewDuty = (item: ExciseDutyItem) => {
    const rates = exciseDutyRates[form.watch('productCategory')];
    const specificDuty = item.quantity * rates.specific;
    const adValoremDuty = item.value * (rates.adValorem / 100);
    return specificDuty + adValoremDuty;
  };

  return (
    <div className="space-y-6">
      <Card>
        <CardHeader>
          <CardTitle className="flex items-center gap-2">
            <Package className="h-5 w-5 text-sierra-blue" />
            Sierra Leone Excise Duty Calculator
          </CardTitle>
          <CardDescription>
            Calculate excise duty on tobacco, alcohol, and fuel products
          </CardDescription>
        </CardHeader>
        <CardContent>
          <form onSubmit={form.handleSubmit(onSubmit)} className="space-y-6">
            <Tabs defaultValue="basic" className="w-full">
              <TabsList className="grid w-full grid-cols-4">
                <TabsTrigger value="basic">Basic Details</TabsTrigger>
                <TabsTrigger value="products">Products</TabsTrigger>
                <TabsTrigger value="dates">Dates</TabsTrigger>
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
                    <Label htmlFor="productCategory">Product Category</Label>
                    <Select
                      value={form.watch('productCategory')}
                      onValueChange={(value) => {
                        form.setValue('productCategory', value as any);
                        // Clear items when category changes
                        form.setValue('items', []);
                      }}
                    >
                      <SelectTrigger>
                        <SelectValue placeholder="Select product category" />
                      </SelectTrigger>
                      <SelectContent>
                        {productCategories.map((category) => {
                          const Icon = category.icon;
                          return (
                            <SelectItem key={category.value} value={category.value}>
                              <div className="flex items-center gap-2">
                                <Icon className={`h-4 w-4 ${category.color}`} />
                                <div>
                                  <div className="font-medium">{category.label}</div>
                                  <div className="text-xs text-muted-foreground">{category.description}</div>
                                </div>
                              </div>
                            </SelectItem>
                          );
                        })}
                      </SelectContent>
                    </Select>
                  </div>
                </div>

                {/* Category Information */}
                {selectedCategory && (
                  <Card className="bg-gray-50 border-gray-200">
                    <CardHeader className="pb-2">
                      <CardTitle className="text-sm flex items-center gap-2">
                        <selectedCategory.icon className={`h-4 w-4 ${selectedCategory.color}`} />
                        {selectedCategory.label} - Excise Duty Rates
                      </CardTitle>
                    </CardHeader>
                    <CardContent className="pt-0">
                      <div className="space-y-2">
                        <div className="flex justify-between items-center">
                          <span className="text-sm">Specific Duty:</span>
                          <Badge variant="outline">SLE {categoryRates.specific.toLocaleString()}</Badge>
                        </div>
                        <div className="flex justify-between items-center">
                          <span className="text-sm">Ad Valorem Duty:</span>
                          <Badge variant="outline">{categoryRates.adValorem}%</Badge>
                        </div>
                        <div className="text-xs text-muted-foreground">
                          {categoryRates.description}
                        </div>
                      </div>
                    </CardContent>
                  </Card>
                )}

                <Alert>
                  <Info className="h-4 w-4" />
                  <AlertDescription>
                    Excise duty applies to specific goods at import or production. 
                    It consists of both specific rates (per unit) and ad valorem rates (percentage of value).
                  </AlertDescription>
                </Alert>
              </TabsContent>

              {/* Products Tab */}
              <TabsContent value="products" className="space-y-4">
                <div className="flex items-center justify-between">
                  <h3 className="text-lg font-medium">Product Details</h3>
                  <div className="flex gap-2">
                    <Button
                      type="button"
                      variant="outline"
                      size="sm"
                      onClick={addSampleProduct}
                    >
                      <Package className="h-4 w-4 mr-2" />
                      Add Sample
                    </Button>
                    <Button
                      type="button"
                      variant="outline"
                      size="sm"
                      onClick={() => appendItem({
                        productCode: `${form.watch('productCategory').substring(0, 3).toUpperCase()}${String(itemFields.length + 1).padStart(3, '0')}`,
                        productName: '',
                        quantity: 0,
                        value: 0,
                      })}
                    >
                      <Plus className="h-4 w-4 mr-2" />
                      Add Product
                    </Button>
                  </div>
                </div>

                <div className="space-y-4">
                  {itemFields.map((field, index) => (
                    <Card key={field.id} className="p-4">
                      <div className="grid grid-cols-1 md:grid-cols-4 gap-3 items-end">
                        <div className="space-y-2">
                          <Label>Product Code</Label>
                          <Input
                            placeholder="Enter product code"
                            {...form.register(`items.${index}.productCode`)}
                          />
                        </div>
                        <div className="space-y-2">
                          <Label>Product Name</Label>
                          <Input
                            placeholder="Enter product name"
                            {...form.register(`items.${index}.productName`)}
                          />
                        </div>
                        <div className="space-y-2">
                          <Label>Quantity</Label>
                          <Input
                            type="number"
                            placeholder="Enter quantity"
                            {...form.register(`items.${index}.quantity`, { valueAsNumber: true })}
                            className="text-right"
                          />
                        </div>
                        <div className="space-y-2">
                          <Label>Value (SLE)</Label>
                          <Input
                            type="number"
                            placeholder="Enter value"
                            {...form.register(`items.${index}.value`, { valueAsNumber: true })}
                            className="text-right"
                          />
                        </div>
                      </div>
                      
                      <div className="flex justify-between items-center mt-3">
                        <Button
                          type="button"
                          variant="outline"
                          size="sm"
                          onClick={() => removeItem(index)}
                          className="flex-shrink-0"
                        >
                          <Minus className="h-4 w-4 mr-2" />
                          Remove
                        </Button>
                        
                        {/* Duty Preview */}
                        {form.watch(`items.${index}.quantity`) > 0 && form.watch(`items.${index}.value`) > 0 && (
                          <div className="text-right">
                            <div className="text-sm text-muted-foreground">Estimated Duty:</div>
                            <div className="font-medium text-sierra-blue-600">
                              {formatCurrency(calculatePreviewDuty({
                                productCode: form.watch(`items.${index}.productCode`),
                                productName: form.watch(`items.${index}.productName`),
                                quantity: form.watch(`items.${index}.quantity`),
                                value: form.watch(`items.${index}.value`),
                              }))}
                            </div>
                          </div>
                        )}
                      </div>
                    </Card>
                  ))}

                  {itemFields.length === 0 && (
                    <div className="text-center py-8 text-muted-foreground">
                      <Package className="h-8 w-8 mx-auto mb-2 opacity-50" />
                      <p>No products added yet</p>
                      <p className="text-sm">Click "Add Product" to start calculating excise duty</p>
                    </div>
                  )}
                </div>

                {/* Products Summary */}
                {itemFields.length > 0 && (
                  <Card className="bg-sierra-gold-50 border-sierra-gold-200">
                    <CardHeader className="pb-2">
                      <CardTitle className="text-sm text-sierra-gold-800">Products Summary</CardTitle>
                    </CardHeader>
                    <CardContent className="pt-0">
                      <div className="grid grid-cols-3 gap-4 text-sm">
                        <div>
                          <span className="text-muted-foreground">Total Products:</span>
                          <div className="font-medium">{itemFields.length}</div>
                        </div>
                        <div>
                          <span className="text-muted-foreground">Total Quantity:</span>
                          <div className="font-medium">
                            {itemFields.reduce((sum, _, index) => 
                              sum + (form.watch(`items.${index}.quantity`) || 0), 0
                            ).toLocaleString()}
                          </div>
                        </div>
                        <div>
                          <span className="text-muted-foreground">Total Value:</span>
                          <div className="font-medium">
                            {formatCurrency(itemFields.reduce((sum, _, index) => 
                              sum + (form.watch(`items.${index}.value`) || 0), 0
                            ))}
                          </div>
                        </div>
                      </div>
                    </CardContent>
                  </Card>
                )}
              </TabsContent>

              {/* Dates Tab */}
              <TabsContent value="dates" className="space-y-4">
                <div className="grid grid-cols-1 md:grid-cols-2 gap-4">
                  <div className="space-y-2">
                    <Label>Due Date</Label>
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
                    <Label>Payment Date</Label>
                    <Popover>
                      <PopoverTrigger asChild>
                        <Button
                          variant="outline"
                          className={cn(
                            "w-full justify-start text-left font-normal",
                            !form.watch('paymentDate') && "text-muted-foreground"
                          )}
                        >
                          <CalendarIcon className="mr-2 h-4 w-4" />
                          {form.watch('paymentDate') ? (
                            format(form.watch('paymentDate')!, "PPP")
                          ) : (
                            <span>Pick payment date</span>
                          )}
                        </Button>
                      </PopoverTrigger>
                      <PopoverContent className="w-auto p-0">
                        <Calendar
                          mode="single"
                          selected={form.watch('paymentDate')}
                          onSelect={(date) => form.setValue('paymentDate', date)}
                          initialFocus
                        />
                      </PopoverContent>
                    </Popover>
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

                <Alert>
                  <Info className="h-4 w-4" />
                  <AlertDescription>
                    Excise duty is typically payable upon import or manufacture. 
                    Consult with customs for specific payment schedules and requirements.
                  </AlertDescription>
                </Alert>
              </TabsContent>

              {/* Results Tab */}
              <TabsContent value="results" className="space-y-4">
                {calculation ? (
                  <ExciseDutyCalculationResults calculation={calculation} />
                ) : (
                  <div className="text-center py-8 text-muted-foreground">
                    <Calculator className="h-8 w-8 mx-auto mb-2 opacity-50" />
                    <p>No calculation results yet</p>
                    <p className="text-sm">Add products and calculate to see results</p>
                  </div>
                )}
              </TabsContent>
            </Tabs>

            <Separator />

            <div className="flex justify-end gap-2">
              <Button type="button" variant="outline" onClick={() => form.reset()}>
                Reset
              </Button>
              <Button type="submit" disabled={loading || itemFields.length === 0}>
                {loading ? 'Calculating...' : 'Calculate Excise Duty'}
              </Button>
            </div>
          </form>
        </CardContent>
      </Card>
    </div>
  );
}

// Excise Duty Calculation Results Component
function ExciseDutyCalculationResults({ calculation }: { calculation: ExciseDutyCalculation }) {
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
                <p className="text-sm text-sierra-blue-600 font-medium">Specific Duty</p>
                <p className="text-2xl font-bold text-sierra-blue-800">
                  {formatCurrency(calculation.totalSpecificDuty)}
                </p>
              </div>
              <Package className="h-8 w-8 text-sierra-blue-600" />
            </div>
          </CardContent>
        </Card>

        <Card className="border-sierra-gold-200 bg-sierra-gold-50">
          <CardContent className="p-4">
            <div className="flex items-center justify-between">
              <div>
                <p className="text-sm text-sierra-gold-600 font-medium">Ad Valorem Duty</p>
                <p className="text-2xl font-bold text-sierra-gold-800">
                  {formatCurrency(calculation.totalAdValoremDuty)}
                </p>
              </div>
              <DollarSign className="h-8 w-8 text-sierra-gold-600" />
            </div>
          </CardContent>
        </Card>

        <Card className="border-sierra-green-200 bg-sierra-green-50">
          <CardContent className="p-4">
            <div className="flex items-center justify-between">
              <div>
                <p className="text-sm text-sierra-green-600 font-medium">Total Excise Duty</p>
                <p className="text-2xl font-bold text-sierra-green-800">
                  {formatCurrency(calculation.totalExciseDuty)}
                </p>
              </div>
              <Calculator className="h-8 w-8 text-sierra-green-600" />
            </div>
          </CardContent>
        </Card>
      </div>

      {/* Calculation Summary */}
      <Card>
        <CardHeader>
          <CardTitle>Excise Duty Summary - {calculation.productCategory}</CardTitle>
        </CardHeader>
        <CardContent className="space-y-4">
          <div className="grid grid-cols-2 gap-2 text-sm">
            <div className="flex justify-between">
              <span>Product Category:</span>
              <Badge variant="outline">{calculation.productCategory}</Badge>
            </div>
            <div className="flex justify-between">
              <span>Number of Items:</span>
              <span className="font-medium">{calculation.items.length}</span>
            </div>
            <div className="flex justify-between">
              <span>Total Specific Duty:</span>
              <span className="font-medium text-sierra-blue-600">{formatCurrency(calculation.totalSpecificDuty)}</span>
            </div>
            <div className="flex justify-between">
              <span>Total Ad Valorem Duty:</span>
              <span className="font-medium text-sierra-gold-600">{formatCurrency(calculation.totalAdValoremDuty)}</span>
            </div>
            <div className="flex justify-between border-t pt-2 font-medium">
              <span>Total Excise Duty:</span>
              <span className="text-sierra-green-600">{formatCurrency(calculation.totalExciseDuty)}</span>
            </div>
          </div>
        </CardContent>
      </Card>

      {/* Item Breakdown */}
      <Card>
        <CardHeader>
          <CardTitle>Product Breakdown</CardTitle>
        </CardHeader>
        <CardContent>
          <div className="space-y-3">
            {calculation.items.map((item, index) => (
              <div key={index} className="p-3 border rounded-lg">
                <div className="flex justify-between items-start mb-2">
                  <div>
                    <div className="font-medium">{item.productName}</div>
                    <div className="text-sm text-muted-foreground">Code: {item.productCode}</div>
                  </div>
                  <div className="text-right">
                    <div className="font-medium text-sierra-green-600">{formatCurrency(item.totalDuty)}</div>
                    <div className="text-xs text-muted-foreground">Total Duty</div>
                  </div>
                </div>
                <div className="grid grid-cols-2 md:grid-cols-5 gap-2 text-sm">
                  <div>
                    <span className="text-muted-foreground">Quantity:</span>
                    <div className="font-medium">{item.quantity.toLocaleString()}</div>
                  </div>
                  <div>
                    <span className="text-muted-foreground">Value:</span>
                    <div className="font-medium">{formatCurrency(item.value)}</div>
                  </div>
                  <div>
                    <span className="text-muted-foreground">Specific Rate:</span>
                    <div className="font-medium">SLE {item.specificRate.toLocaleString()}</div>
                  </div>
                  <div>
                    <span className="text-muted-foreground">Ad Valorem Rate:</span>
                    <div className="font-medium">{item.adValoremRate}%</div>
                  </div>
                  <div>
                    <span className="text-muted-foreground">Breakdown:</span>
                    <div className="text-xs">
                      <div>Specific: {formatCurrency(item.specificDuty)}</div>
                      <div>Ad Valorem: {formatCurrency(item.adValoremDuty)}</div>
                    </div>
                  </div>
                </div>
              </div>
            ))}
          </div>
        </CardContent>
      </Card>

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

      {/* Compliance Information */}
      <Alert>
        <Info className="h-4 w-4" />
        <AlertDescription>
          Excise duty rates and schedules may vary. Always verify current rates with Sierra Leone Customs. 
          Some products may qualify for reduced rates or exemptions.
        </AlertDescription>
      </Alert>
    </div>
  );
}