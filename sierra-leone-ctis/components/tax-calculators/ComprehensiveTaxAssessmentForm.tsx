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
import { Progress } from '@/components/ui/progress';
import { Alert, AlertDescription } from '@/components/ui/alert';
import { Separator } from '@/components/ui/separator';
import { DatePicker } from '@/components/ui/date-picker';
import { format } from 'date-fns';
import { 
  Calculator, 
  Plus,
  Minus,
  AlertTriangle, 
  Info,
  CheckCircle,
  XCircle,
  DollarSign,
  TrendingUp,
  PieChart,
  Users,
  FileText,
  Award,
  AlertCircle
} from 'lucide-react';
import { useToast } from '@/components/ui/use-toast';
import { TaxCalculationService, ComprehensiveAssessmentRequest, ComprehensiveAssessment, TaxAllowance, PayrollEmployee, ExciseDutyItem } from '@/lib/services/tax-calculation-service';

const taxAllowanceSchema = z.object({
  type: z.string().min(1, 'Allowance type is required'),
  amount: z.number().min(0, 'Amount must be positive'),
  description: z.string().optional(),
});

const payrollEmployeeSchema = z.object({
  employeeId: z.string().min(1, 'Employee ID is required'),
  employeeName: z.string().min(1, 'Employee name is required'),
  annualSalary: z.number().min(0, 'Annual salary must be positive'),
  monthlyPayroll: z.number().min(0, 'Monthly payroll must be positive'),
});

const exciseDutyItemSchema = z.object({
  productCode: z.string().min(1, 'Product code is required'),
  productName: z.string().min(1, 'Product name is required'),
  quantity: z.number().min(0, 'Quantity must be positive'),
  value: z.number().min(0, 'Value must be positive'),
});

const comprehensiveAssessmentSchema = z.object({
  clientId: z.number().min(1, 'Client ID is required'),
  taxYear: z.number().min(2020).max(2025),
  taxpayerCategory: z.enum(['Individual', 'Large', 'Medium', 'Small', 'Micro']),
  
  // Income Tax
  grossIncome: z.number().min(0, 'Gross income must be positive'),
  deductions: z.number().min(0, 'Deductions must be positive'),
  allowances: z.array(taxAllowanceSchema),
  
  // GST
  grossSales: z.number().min(0, 'Gross sales must be positive'),
  taxableSupplies: z.number().min(0, 'Taxable supplies must be positive'),
  inputTax: z.number().min(0, 'Input tax must be positive'),
  
  // Payroll
  totalPayroll: z.number().min(0, 'Total payroll must be positive'),
  employees: z.array(payrollEmployeeSchema),
  
  // Excise Duty
  exciseDutyItems: z.array(exciseDutyItemSchema),
  
  // Deadlines
  incomeTaxDueDate: z.date().optional(),
  gstDueDate: z.date().optional(),
  payrollTaxDueDate: z.date().optional(),
  exciseDutyDueDate: z.date().optional(),
});

type ComprehensiveAssessmentFormData = z.infer<typeof comprehensiveAssessmentSchema>;

const taxpayerCategories = [
  { value: 'Individual', label: 'Individual Taxpayer', threshold: 'N/A', description: 'Personal income tax' },
  { value: 'Large', label: 'Large Company', threshold: '> SLE 2B', description: 'Large taxpayer status' },
  { value: 'Medium', label: 'Medium Company', threshold: 'SLE 500M - 2B', description: 'Medium taxpayer status' },
  { value: 'Small', label: 'Small Company', threshold: 'SLE 100M - 500M', description: 'Small taxpayer status' },
  { value: 'Micro', label: 'Micro Company', threshold: '< SLE 100M', description: 'Micro taxpayer status' },
];

interface ComprehensiveTaxAssessmentFormProps {
  onAssessmentComplete?: (assessment: ComprehensiveAssessment) => void;
  initialData?: Partial<ComprehensiveAssessmentFormData>;
}

export default function ComprehensiveTaxAssessmentForm({ 
  onAssessmentComplete, 
  initialData 
}: ComprehensiveTaxAssessmentFormProps) {
  const { toast } = useToast();
  const [assessment, setAssessment] = useState<ComprehensiveAssessment | null>(null);
  const [loading, setLoading] = useState(false);
  const [activeTab, setActiveTab] = useState('basic');

  const form = useForm<ComprehensiveAssessmentFormData>({
    resolver: zodResolver(comprehensiveAssessmentSchema),
    defaultValues: {
      clientId: 1,
      taxYear: 2024,
      taxpayerCategory: 'Individual',
      grossIncome: 0,
      deductions: 0,
      allowances: [],
      grossSales: 0,
      taxableSupplies: 0,
      inputTax: 0,
      totalPayroll: 0,
      employees: [],
      exciseDutyItems: [],
      ...initialData,
    },
  });

  const { fields: allowanceFields, append: appendAllowance, remove: removeAllowance } = useFieldArray({
    control: form.control,
    name: 'allowances',
  });

  const { fields: employeeFields, append: appendEmployee, remove: removeEmployee } = useFieldArray({
    control: form.control,
    name: 'employees',
  });

  const { fields: exciseFields, append: appendExciseItem, remove: removeExciseItem } = useFieldArray({
    control: form.control,
    name: 'exciseDutyItems',
  });

  const onSubmit = async (data: ComprehensiveAssessmentFormData) => {
    setLoading(true);
    try {
      const request: ComprehensiveAssessmentRequest = {
        ...data,
      };

      const result = await TaxCalculationService.performComprehensiveAssessment(request);
      setAssessment(result);
      onAssessmentComplete?.(result);

      toast({
        title: 'Comprehensive Assessment Complete',
        description: `Total tax liability: SLE ${result.totalTaxLiability.toLocaleString()}`,
      });
    } catch (error) {
      console.error('Comprehensive assessment error:', error);
      toast({
        variant: 'destructive',
        title: 'Assessment Error',
        description: error instanceof Error ? error.message : 'Failed to perform comprehensive assessment',
      });
    } finally {
      setLoading(false);
    }
  };

  const formatCurrency = (amount: number) => {
    return `SLE ${amount.toLocaleString('en-US', { minimumFractionDigits: 0, maximumFractionDigits: 0 })}`;
  };

  const selectedCategory = taxpayerCategories.find(cat => cat.value === form.watch('taxpayerCategory'));

  const getCompletionProgress = () => {
    const fields = [
      form.watch('grossIncome') > 0 ? 1 : 0,
      form.watch('grossSales') > 0 ? 1 : 0,
      form.watch('totalPayroll') > 0 ? 1 : 0,
      exciseFields.length > 0 ? 1 : 0,
    ];
    return (fields.reduce((sum, field) => sum + field, 0) / fields.length) * 100;
  };

  return (
    <div className="space-y-6">
      <Card>
        <CardHeader>
          <CardTitle className="flex items-center gap-2">
            <PieChart className="h-5 w-5 text-sierra-blue" />
            Comprehensive Tax Assessment
          </CardTitle>
          <CardDescription>
            Complete tax assessment covering income tax, GST, payroll tax, and excise duty
          </CardDescription>
          
          {/* Progress Indicator */}
          <div className="mt-4">
            <div className="flex justify-between items-center mb-2">
              <span className="text-sm font-medium">Assessment Progress</span>
              <span className="text-sm text-muted-foreground">{Math.round(getCompletionProgress())}%</span>
            </div>
            <Progress value={getCompletionProgress()} className="h-2" />
          </div>
        </CardHeader>
        <CardContent>
          <form onSubmit={form.handleSubmit(onSubmit)} className="space-y-6">
            <Tabs value={activeTab} onValueChange={setActiveTab} className="w-full">
              <TabsList className="grid w-full grid-cols-6">
                <TabsTrigger value="basic">Basic</TabsTrigger>
                <TabsTrigger value="income">Income Tax</TabsTrigger>
                <TabsTrigger value="gst">GST</TabsTrigger>
                <TabsTrigger value="payroll">Payroll</TabsTrigger>
                <TabsTrigger value="excise">Excise</TabsTrigger>
                <TabsTrigger value="results">Results</TabsTrigger>
              </TabsList>

              {/* Basic Details Tab */}
              <TabsContent value="basic" className="space-y-4">
                <div className="grid grid-cols-1 md:grid-cols-3 gap-4">
                  <div className="space-y-2">
                    <Label htmlFor="clientId">Client ID</Label>
                    <Input
                      id="clientId"
                      type="number"
                      {...form.register('clientId', { valueAsNumber: true })}
                    />
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
                    <Label htmlFor="taxpayerCategory">Taxpayer Category</Label>
                    <Select
                      value={form.watch('taxpayerCategory')}
                      onValueChange={(value) => form.setValue('taxpayerCategory', value as any)}
                    >
                      <SelectTrigger>
                        <SelectValue />
                      </SelectTrigger>
                      <SelectContent>
                        {taxpayerCategories.map((category) => (
                          <SelectItem key={category.value} value={category.value}>
                            <div>
                              <div className="font-medium">{category.label}</div>
                              <div className="text-xs text-muted-foreground">
                                {category.threshold} - {category.description}
                              </div>
                            </div>
                          </SelectItem>
                        ))}
                      </SelectContent>
                    </Select>
                  </div>
                </div>

                {selectedCategory && (
                  <Alert>
                    <Info className="h-4 w-4" />
                    <AlertDescription>
                      Selected: {selectedCategory.label} ({selectedCategory.threshold})
                      <br />
                      {selectedCategory.description}
                    </AlertDescription>
                  </Alert>
                )}
              </TabsContent>

              {/* Income Tax Tab */}
              <TabsContent value="income" className="space-y-4">
                <div className="grid grid-cols-1 md:grid-cols-2 gap-4">
                  <div className="space-y-2">
                    <Label htmlFor="grossIncome">Gross Income (SLE)</Label>
                    <Input
                      id="grossIncome"
                      type="number"
                      placeholder="Enter gross income"
                      {...form.register('grossIncome', { valueAsNumber: true })}
                      className="text-right"
                    />
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
                  </div>
                </div>

                {/* Allowances */}
                <div className="space-y-3">
                  <div className="flex justify-between items-center">
                    <Label>Tax Allowances</Label>
                    <Button
                      type="button"
                      variant="outline"
                      size="sm"
                      onClick={() => appendAllowance({ type: '', amount: 0, description: '' })}
                    >
                      <Plus className="h-4 w-4 mr-2" />
                      Add Allowance
                    </Button>
                  </div>

                  {allowanceFields.map((field, index) => (
                    <Card key={field.id} className="p-3">
                      <div className="grid grid-cols-1 md:grid-cols-3 gap-3 items-end">
                        <div className="space-y-2">
                          <Label>Type</Label>
                          <Input
                            placeholder="Allowance type"
                            {...form.register(`allowances.${index}.type`)}
                          />
                        </div>
                        <div className="space-y-2">
                          <Label>Amount (SLE)</Label>
                          <Input
                            type="number"
                            placeholder="Amount"
                            {...form.register(`allowances.${index}.amount`, { valueAsNumber: true })}
                            className="text-right"
                          />
                        </div>
                        <Button
                          type="button"
                          variant="outline"
                          size="sm"
                          onClick={() => removeAllowance(index)}
                        >
                          <Minus className="h-4 w-4" />
                        </Button>
                      </div>
                    </Card>
                  ))}
                </div>

                {/* Due Date */}
                <div className="space-y-2">
                  <Label>Income Tax Due Date</Label>
                  <DatePicker
                    value={form.watch('incomeTaxDueDate') ?? null}
                    onChange={(date) => form.setValue('incomeTaxDueDate', date || undefined)}
                    placeholder="Pick due date"
                  />
                </div>
              </TabsContent>

              {/* GST Tab */}
              <TabsContent value="gst" className="space-y-4">
                <div className="grid grid-cols-1 md:grid-cols-3 gap-4">
                  <div className="space-y-2">
                    <Label htmlFor="grossSales">Gross Sales (SLE)</Label>
                    <Input
                      id="grossSales"
                      type="number"
                      placeholder="Enter gross sales"
                      {...form.register('grossSales', { valueAsNumber: true })}
                      className="text-right"
                    />
                  </div>

                  <div className="space-y-2">
                    <Label htmlFor="taxableSupplies">Taxable Supplies (SLE)</Label>
                    <Input
                      id="taxableSupplies"
                      type="number"
                      placeholder="Enter taxable supplies"
                      {...form.register('taxableSupplies', { valueAsNumber: true })}
                      className="text-right"
                    />
                  </div>

                  <div className="space-y-2">
                    <Label htmlFor="inputTax">Input Tax (SLE)</Label>
                    <Input
                      id="inputTax"
                      type="number"
                      placeholder="Enter input tax"
                      {...form.register('inputTax', { valueAsNumber: true })}
                      className="text-right"
                    />
                  </div>
                </div>

                {/* GST Registration Status */}
                {form.watch('grossSales') > 500000000 && (
                  <Alert>
                    <AlertTriangle className="h-4 w-4" />
                    <AlertDescription>
                      GST registration required: Gross sales exceed SLE 500,000,000 threshold.
                    </AlertDescription>
                  </Alert>
                )}

                {/* Due Date */}
                <div className="space-y-2">
                  <Label>GST Due Date</Label>
                  <DatePicker
                    value={form.watch('gstDueDate') ?? null}
                    onChange={(date) => form.setValue('gstDueDate', date || undefined)}
                    placeholder="Pick due date"
                  />
                </div>
              </TabsContent>

              {/* Payroll Tab */}
              <TabsContent value="payroll" className="space-y-4">
                <div className="space-y-2">
                  <Label htmlFor="totalPayroll">Total Annual Payroll (SLE)</Label>
                  <Input
                    id="totalPayroll"
                    type="number"
                    placeholder="Enter total payroll"
                    {...form.register('totalPayroll', { valueAsNumber: true })}
                    className="text-right"
                  />
                </div>

                {/* Employees */}
                <div className="space-y-3">
                  <div className="flex justify-between items-center">
                    <Label>Employees</Label>
                    <Button
                      type="button"
                      variant="outline"
                      size="sm"
                      onClick={() => appendEmployee({
                        employeeId: `EMP${employeeFields.length + 1}`,
                        employeeName: '',
                        annualSalary: 0,
                        monthlyPayroll: 0,
                      })}
                    >
                      <Plus className="h-4 w-4 mr-2" />
                      Add Employee
                    </Button>
                  </div>

                  {employeeFields.map((field, index) => (
                    <Card key={field.id} className="p-3">
                      <div className="grid grid-cols-1 md:grid-cols-4 gap-3 items-end">
                        <div className="space-y-2">
                          <Label>Employee ID</Label>
                          <Input
                            placeholder="ID"
                            {...form.register(`employees.${index}.employeeId`)}
                          />
                        </div>
                        <div className="space-y-2">
                          <Label>Name</Label>
                          <Input
                            placeholder="Employee name"
                            {...form.register(`employees.${index}.employeeName`)}
                          />
                        </div>
                        <div className="space-y-2">
                          <Label>Annual Salary (SLE)</Label>
                          <Input
                            type="number"
                            placeholder="Annual salary"
                            {...form.register(`employees.${index}.annualSalary`, { 
                              valueAsNumber: true,
                              onChange: (e) => {
                                const annual = parseFloat(e.target.value) || 0;
                                form.setValue(`employees.${index}.monthlyPayroll`, annual / 12);
                              }
                            })}
                            className="text-right"
                          />
                        </div>
                        <Button
                          type="button"
                          variant="outline"
                          size="sm"
                          onClick={() => removeEmployee(index)}
                        >
                          <Minus className="h-4 w-4" />
                        </Button>
                      </div>
                    </Card>
                  ))}
                </div>

                {/* Due Date */}
                <div className="space-y-2">
                  <Label>Payroll Tax Due Date</Label>
                  <DatePicker
                    value={form.watch('payrollTaxDueDate') ?? null}
                    onChange={(date) => form.setValue('payrollTaxDueDate', date || undefined)}
                    placeholder="Pick due date"
                  />
                </div>
              </TabsContent>

              {/* Excise Duty Tab */}
              <TabsContent value="excise" className="space-y-4">
                <div className="space-y-3">
                  <div className="flex justify-between items-center">
                    <Label>Excise Duty Items</Label>
                    <Button
                      type="button"
                      variant="outline"
                      size="sm"
                      onClick={() => appendExciseItem({
                        productCode: `PROD${exciseFields.length + 1}`,
                        productName: '',
                        quantity: 0,
                        value: 0,
                      })}
                    >
                      <Plus className="h-4 w-4 mr-2" />
                      Add Item
                    </Button>
                  </div>

                  {exciseFields.map((field, index) => (
                    <Card key={field.id} className="p-3">
                      <div className="grid grid-cols-1 md:grid-cols-4 gap-3 items-end">
                        <div className="space-y-2">
                          <Label>Product Code</Label>
                          <Input
                            placeholder="Code"
                            {...form.register(`exciseDutyItems.${index}.productCode`)}
                          />
                        </div>
                        <div className="space-y-2">
                          <Label>Product Name</Label>
                          <Input
                            placeholder="Product name"
                            {...form.register(`exciseDutyItems.${index}.productName`)}
                          />
                        </div>
                        <div className="space-y-2">
                          <Label>Quantity</Label>
                          <Input
                            type="number"
                            placeholder="Quantity"
                            {...form.register(`exciseDutyItems.${index}.quantity`, { valueAsNumber: true })}
                            className="text-right"
                          />
                        </div>
                        <div className="grid grid-cols-2 gap-2">
                          <div className="space-y-2">
                            <Label>Value (SLE)</Label>
                            <Input
                              type="number"
                              placeholder="Value"
                              {...form.register(`exciseDutyItems.${index}.value`, { valueAsNumber: true })}
                              className="text-right"
                            />
                          </div>
                          <Button
                            type="button"
                            variant="outline"
                            size="sm"
                            onClick={() => removeExciseItem(index)}
                            className="self-end"
                          >
                            <Minus className="h-4 w-4" />
                          </Button>
                        </div>
                      </div>
                    </Card>
                  ))}
                </div>

                {/* Due Date */}
                <div className="space-y-2">
                  <Label>Excise Duty Due Date</Label>
                  <DatePicker
                    value={form.watch('exciseDutyDueDate') ?? null}
                    onChange={(date) => form.setValue('exciseDutyDueDate', date || undefined)}
                    placeholder="Pick due date"
                  />
                </div>
              </TabsContent>

              {/* Results Tab */}
              <TabsContent value="results" className="space-y-4">
                {assessment ? (
                  <ComprehensiveAssessmentResults assessment={assessment} />
                ) : (
                  <div className="text-center py-8 text-muted-foreground">
                    <Calculator className="h-8 w-8 mx-auto mb-2 opacity-50" />
                    <p>No assessment results yet</p>
                    <p className="text-sm">Complete the form and calculate to see comprehensive results</p>
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
                {loading ? 'Performing Assessment...' : 'Perform Comprehensive Assessment'}
              </Button>
            </div>
          </form>
        </CardContent>
      </Card>
    </div>
  );
}

// Comprehensive Assessment Results Component
function ComprehensiveAssessmentResults({ assessment }: { assessment: ComprehensiveAssessment }) {
  const formatCurrency = (amount: number) => {
    return `SLE ${amount.toLocaleString('en-US', { minimumFractionDigits: 0, maximumFractionDigits: 0 })}`;
  };

  const getComplianceColor = (grade: string) => {
    switch (grade) {
      case 'A': return 'text-green-600';
      case 'B': return 'text-blue-600';
      case 'C': return 'text-yellow-600';
      case 'D': return 'text-orange-600';
      case 'F': return 'text-red-600';
      default: return 'text-gray-600';
    }
  };

  const getComplianceIcon = (grade: string) => {
    switch (grade) {
      case 'A': return <Award className="h-6 w-6 text-green-600" />;
      case 'B': return <CheckCircle className="h-6 w-6 text-blue-600" />;
      case 'C': return <AlertCircle className="h-6 w-6 text-yellow-600" />;
      case 'D': return <AlertTriangle className="h-6 w-6 text-orange-600" />;
      case 'F': return <XCircle className="h-6 w-6 text-red-600" />;
      default: return <Info className="h-6 w-6 text-gray-600" />;
    }
  };

  return (
    <div className="space-y-6">
      {/* Summary Cards */}
      <div className="grid grid-cols-1 md:grid-cols-4 gap-4">
        <Card className="border-sierra-blue-200 bg-sierra-blue-50">
          <CardContent className="p-4">
            <div className="flex items-center justify-between">
              <div>
                <p className="text-sm text-sierra-blue-600 font-medium">Total Tax Liability</p>
                <p className="text-xl font-bold text-sierra-blue-800">
                  {formatCurrency(assessment.totalTaxLiability)}
                </p>
              </div>
              <DollarSign className="h-6 w-6 text-sierra-blue-600" />
            </div>
          </CardContent>
        </Card>

        <Card className="border-red-200 bg-red-50">
          <CardContent className="p-4">
            <div className="flex items-center justify-between">
              <div>
                <p className="text-sm text-red-600 font-medium">Total Penalties</p>
                <p className="text-xl font-bold text-red-800">
                  {formatCurrency(assessment.totalPenalties)}
                </p>
              </div>
              <AlertTriangle className="h-6 w-6 text-red-600" />
            </div>
          </CardContent>
        </Card>

        <Card className="border-sierra-green-200 bg-sierra-green-50">
          <CardContent className="p-4">
            <div className="flex items-center justify-between">
              <div>
                <p className="text-sm text-sierra-green-600 font-medium">Grand Total</p>
                <p className="text-xl font-bold text-sierra-green-800">
                  {formatCurrency(assessment.grandTotal)}
                </p>
              </div>
              <Calculator className="h-6 w-6 text-sierra-green-600" />
            </div>
          </CardContent>
        </Card>

        <Card className="border-gray-300">
          <CardContent className="p-4">
            <div className="flex items-center justify-between">
              <div>
                <p className="text-sm font-medium">Compliance Grade</p>
                <p className={`text-2xl font-bold ${getComplianceColor(assessment.complianceGrade)}`}>
                  {assessment.complianceGrade}
                </p>
                <p className="text-xs text-muted-foreground">
                  Score: {assessment.complianceScore}/100
                </p>
              </div>
              {getComplianceIcon(assessment.complianceGrade)}
            </div>
          </CardContent>
        </Card>
      </div>

      {/* Tax Breakdown */}
      <Card>
        <CardHeader>
          <CardTitle>Tax Breakdown by Category</CardTitle>
        </CardHeader>
        <CardContent>
          <div className="grid grid-cols-1 md:grid-cols-2 gap-6">
            <div className="space-y-3">
              <div className="flex justify-between items-center p-3 border rounded">
                <span>Income Tax:</span>
                <span className="font-medium">{formatCurrency(assessment.incomeTaxCalculation.payableTax)}</span>
              </div>
              <div className="flex justify-between items-center p-3 border rounded">
                <span>GST:</span>
                <span className="font-medium">{formatCurrency(assessment.gstCalculation.netGstLiability)}</span>
              </div>
            </div>
            <div className="space-y-3">
              <div className="flex justify-between items-center p-3 border rounded">
                <span>Payroll Tax:</span>
                <span className="font-medium">{formatCurrency(assessment.payrollTaxCalculation.totalPayrollTax)}</span>
              </div>
              <div className="flex justify-between items-center p-3 border rounded">
                <span>Excise Duty:</span>
                <span className="font-medium">{formatCurrency(assessment.exciseDutyCalculation.totalExciseDuty)}</span>
              </div>
            </div>
          </div>
        </CardContent>
      </Card>

      {/* Compliance Issues */}
      {assessment.complianceIssues && assessment.complianceIssues.length > 0 && (
        <Card>
          <CardHeader>
            <CardTitle>Compliance Issues</CardTitle>
          </CardHeader>
          <CardContent>
            <div className="space-y-3">
              {assessment.complianceIssues.map((issue, index) => (
                <div key={index} className="p-3 border rounded-lg">
                  <div className="flex justify-between items-start mb-2">
                    <div>
                      <div className="font-medium">{issue.issueType}</div>
                      <div className="text-sm text-muted-foreground">{issue.description}</div>
                    </div>
                    <Badge variant={issue.severity === 'Critical' ? 'destructive' : 
                                  issue.severity === 'High' ? 'destructive' :
                                  issue.severity === 'Medium' ? 'default' : 'secondary'}>
                      {issue.severity}
                    </Badge>
                  </div>
                  <div className="text-sm">
                    <strong>Recommended Action:</strong> {issue.recommendedAction}
                  </div>
                  {issue.deadline && (
                    <div className="text-xs text-muted-foreground mt-1">
                      Deadline: {format(new Date(issue.deadline), 'PPP')}
                    </div>
                  )}
                </div>
              ))}
            </div>
          </CardContent>
        </Card>
      )}

      {/* Compliance Score Breakdown */}
      <Card>
        <CardHeader>
          <CardTitle>Compliance Score Breakdown</CardTitle>
        </CardHeader>
        <CardContent>
          <div className="space-y-4">
            <div className="flex justify-between items-center">
              <span>Overall Compliance Score:</span>
              <div className="flex items-center gap-2">
                <Progress value={assessment.complianceScore} className="w-32" />
                <span className="font-medium">{assessment.complianceScore}/100</span>
              </div>
            </div>
            <div className="text-sm text-muted-foreground">
              Compliance grade is based on filing timeliness, payment history, documentation completeness, and adherence to tax regulations.
            </div>
          </div>
        </CardContent>
      </Card>

      {/* Summary Information */}
      <Alert>
        <Info className="h-4 w-4" />
        <AlertDescription>
          This comprehensive assessment covers all major tax obligations for {assessment.taxpayerCategory} taxpayers in Sierra Leone. 
          Ensure all payments are made by their respective due dates to avoid penalties.
        </AlertDescription>
      </Alert>
    </div>
  );
}