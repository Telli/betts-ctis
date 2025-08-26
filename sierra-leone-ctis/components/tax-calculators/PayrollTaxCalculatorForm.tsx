'use client';

import React, { useState } from 'react';
import { useForm, useFieldArray } from 'react-hook-form';
import { zodResolver } from '@hookform/resolvers/zod';
import * as z from 'zod';
import { Card, CardContent, CardDescription, CardHeader, CardTitle } from '@/components/ui/card';
import { Button } from '@/components/ui/button';
import { Input } from '@/components/ui/input';
import { Label } from '@/components/ui/label';
import { Tabs, TabsContent, TabsList, TabsTrigger } from '@/components/ui/tabs';
import { Badge } from '@/components/ui/badge';
import { Alert, AlertDescription } from '@/components/ui/alert';
import { Separator } from '@/components/ui/separator';
import { Calendar } from '@/components/ui/calendar';
import { Popover, PopoverContent, PopoverTrigger } from '@/components/ui/popover';
import { 
  Calculator, 
  Users, 
  Plus,
  Minus,
  AlertTriangle, 
  Info,
  CalendarIcon,
  FileText,
  DollarSign,
  UserPlus,
  TrendingUp
} from 'lucide-react';
import { format } from 'date-fns';
import { cn } from '@/lib/utils';
import { useToast } from '@/components/ui/use-toast';
import { TaxCalculationService, PayrollTaxCalculationRequest, PayrollTaxCalculation, PayrollEmployee } from '@/lib/services/tax-calculation-service';

const payrollEmployeeSchema = z.object({
  employeeId: z.string().min(1, 'Employee ID is required'),
  employeeName: z.string().min(1, 'Employee name is required'),
  annualSalary: z.number().min(0, 'Annual salary must be positive'),
  monthlyPayroll: z.number().min(0, 'Monthly payroll must be positive'),
});

const payrollTaxSchema = z.object({
  taxYear: z.number().min(2020).max(2025),
  employees: z.array(payrollEmployeeSchema).min(1, 'At least one employee is required'),
  totalPayroll: z.number().min(0, 'Total payroll must be positive'),
  dueDate: z.date().optional(),
  remittanceDate: z.date().optional(),
});

type PayrollTaxFormData = z.infer<typeof payrollTaxSchema>;

const payeTaxBrackets = [
  { min: 0, max: 6000000, rate: 0, description: 'Tax-free threshold' },
  { min: 6000000, max: 20000000, rate: 15, description: 'First bracket' },
  { min: 20000000, max: 50000000, rate: 20, description: 'Second bracket' },
  { min: 50000000, max: Infinity, rate: 30, description: 'Top bracket' },
];

const sampleEmployees = [
  {
    employeeId: 'EMP001',
    employeeName: 'John Doe',
    annualSalary: 18000000, // 18M Leone
    monthlyPayroll: 1500000,
  },
  {
    employeeId: 'EMP002', 
    employeeName: 'Jane Smith',
    annualSalary: 24000000, // 24M Leone
    monthlyPayroll: 2000000,
  },
];

interface PayrollTaxCalculatorFormProps {
  onCalculationComplete?: (calculation: PayrollTaxCalculation) => void;
  initialData?: Partial<PayrollTaxFormData>;
}

export default function PayrollTaxCalculatorForm({ 
  onCalculationComplete, 
  initialData 
}: PayrollTaxCalculatorFormProps) {
  const { toast } = useToast();
  const [calculation, setCalculation] = useState<PayrollTaxCalculation | null>(null);
  const [loading, setLoading] = useState(false);

  const form = useForm<PayrollTaxFormData>({
    resolver: zodResolver(payrollTaxSchema),
    defaultValues: {
      taxYear: 2024,
      employees: [],
      totalPayroll: 0,
      ...initialData,
    },
  });

  const { fields: employeeFields, append: appendEmployee, remove: removeEmployee } = useFieldArray({
    control: form.control,
    name: 'employees',
  });

  const onSubmit = async (data: PayrollTaxFormData) => {
    setLoading(true);
    try {
      const request: PayrollTaxCalculationRequest = {
        ...data,
        dueDate: data.dueDate,
        remittanceDate: data.remittanceDate,
      };

      const result = await TaxCalculationService.calculatePayrollTax(request);
      setCalculation(result);
      onCalculationComplete?.(result);

      toast({
        title: 'Payroll Tax Calculation Complete',
        description: `Total payroll tax: SLE ${result.totalPayrollTax.toLocaleString()}`,
      });
    } catch (error) {
      console.error('Payroll tax calculation error:', error);
      toast({
        variant: 'destructive',
        title: 'Calculation Error',
        description: error instanceof Error ? error.message : 'Failed to calculate payroll tax',
      });
    } finally {
      setLoading(false);
    }
  };

  const formatCurrency = (amount: number) => {
    return `SLE ${amount.toLocaleString('en-US', { minimumFractionDigits: 0, maximumFractionDigits: 0 })}`;
  };

  const addSampleEmployee = () => {
    const employee = sampleEmployees[employeeFields.length % sampleEmployees.length];
    appendEmployee({
      ...employee,
      employeeId: `EMP${String(employeeFields.length + 1).padStart(3, '0')}`,
    });
  };

  const updateTotalPayroll = () => {
    const total = employeeFields.reduce((sum, _, index) => {
      const monthlyPayroll = form.watch(`employees.${index}.monthlyPayroll`) || 0;
      return sum + (monthlyPayroll * 12);
    }, 0);
    form.setValue('totalPayroll', total);
  };

  // Watch for changes in employee payroll to update total
  const watchedEmployees = form.watch('employees');
  React.useEffect(() => {
    updateTotalPayroll();
  }, [watchedEmployees]);

  const calculateIndividualPAYE = (annualSalary: number) => {
    let paye = 0;
    let taxableAmount = annualSalary;

    for (const bracket of payeTaxBrackets) {
      if (taxableAmount > bracket.min) {
        const taxableAtThisBracket = Math.min(taxableAmount - bracket.min, bracket.max - bracket.min);
        paye += taxableAtThisBracket * (bracket.rate / 100);
      }
    }

    return paye;
  };

  return (
    <div className="space-y-6">
      <Card>
        <CardHeader>
          <CardTitle className="flex items-center gap-2">
            <Users className="h-5 w-5 text-sierra-blue" />
            Sierra Leone Payroll Tax Calculator
          </CardTitle>
          <CardDescription>
            Calculate PAYE and Skills Development Levy for your employees
          </CardDescription>
        </CardHeader>
        <CardContent>
          <form onSubmit={form.handleSubmit(onSubmit)} className="space-y-6">
            <Tabs defaultValue="basic" className="w-full">
              <TabsList className="grid w-full grid-cols-4">
                <TabsTrigger value="basic">Basic Details</TabsTrigger>
                <TabsTrigger value="employees">Employees</TabsTrigger>
                <TabsTrigger value="dates">Dates</TabsTrigger>
                <TabsTrigger value="results">Results</TabsTrigger>
              </TabsList>

              {/* Basic Details Tab */}
              <TabsContent value="basic" className="space-y-4">
                <div className="grid grid-cols-1 md:grid-cols-2 gap-4">
                  <div className="space-y-2">
                    <Label htmlFor="taxYear">Tax Year</Label>
                    <select
                      value={form.watch('taxYear')}
                      onChange={(e) => form.setValue('taxYear', parseInt(e.target.value))}
                      className="w-full p-2 border border-gray-300 rounded-md"
                    >
                      <option value={2024}>2024</option>
                      <option value={2025}>2025</option>
                    </select>
                  </div>

                  <div className="space-y-2">
                    <Label htmlFor="totalPayroll">Total Annual Payroll (SLE)</Label>
                    <Input
                      id="totalPayroll"
                      type="number"
                      placeholder="Calculated automatically from employees"
                      value={form.watch('totalPayroll')}
                      readOnly
                      className="text-right bg-gray-50"
                    />
                  </div>
                </div>

                {/* PAYE Tax Brackets Reference */}
                <Card className="bg-sierra-blue-50 border-sierra-blue-200">
                  <CardHeader className="pb-2">
                    <CardTitle className="text-sm text-sierra-blue-800">
                      PAYE Tax Brackets (Finance Act 2025)
                    </CardTitle>
                  </CardHeader>
                  <CardContent className="pt-0">
                    <div className="grid grid-cols-1 md:grid-cols-2 gap-2 text-xs">
                      {payeTaxBrackets.map((bracket, index) => (
                        <div key={index} className="flex justify-between items-center p-2 bg-white rounded">
                          <span>
                            {bracket.max === Infinity 
                              ? `Above ${formatCurrency(bracket.min)}`
                              : `${formatCurrency(bracket.min)} - ${formatCurrency(bracket.max)}`
                            }
                          </span>
                          <Badge variant={bracket.rate === 0 ? "secondary" : "default"}>
                            {bracket.rate}%
                          </Badge>
                        </div>
                      ))}
                    </div>
                  </CardContent>
                </Card>

                {/* Skills Development Levy Info */}
                <Alert>
                  <Info className="h-4 w-4" />
                  <AlertDescription>
                    Skills Development Levy: 2.5% of monthly payroll for companies with payroll exceeding SLE 500,000 per month.
                  </AlertDescription>
                </Alert>
              </TabsContent>

              {/* Employees Tab */}
              <TabsContent value="employees" className="space-y-4">
                <div className="flex items-center justify-between">
                  <h3 className="text-lg font-medium">Employee Details</h3>
                  <div className="flex gap-2">
                    <Button
                      type="button"
                      variant="outline"
                      size="sm"
                      onClick={addSampleEmployee}
                    >
                      <UserPlus className="h-4 w-4 mr-2" />
                      Add Sample
                    </Button>
                    <Button
                      type="button"
                      variant="outline"
                      size="sm"
                      onClick={() => appendEmployee({
                        employeeId: `EMP${String(employeeFields.length + 1).padStart(3, '0')}`,
                        employeeName: '',
                        annualSalary: 0,
                        monthlyPayroll: 0,
                      })}
                    >
                      <Plus className="h-4 w-4 mr-2" />
                      Add Employee
                    </Button>
                  </div>
                </div>

                <div className="space-y-4">
                  {employeeFields.map((field, index) => (
                    <Card key={field.id} className="p-4">
                      <div className="grid grid-cols-1 md:grid-cols-4 gap-3 items-end">
                        <div className="space-y-2">
                          <Label>Employee ID</Label>
                          <Input
                            placeholder="Enter employee ID"
                            {...form.register(`employees.${index}.employeeId`)}
                          />
                        </div>
                        <div className="space-y-2">
                          <Label>Employee Name</Label>
                          <Input
                            placeholder="Enter employee name"
                            {...form.register(`employees.${index}.employeeName`)}
                          />
                        </div>
                        <div className="space-y-2">
                          <Label>Annual Salary (SLE)</Label>
                          <Input
                            type="number"
                            placeholder="Enter annual salary"
                            {...form.register(`employees.${index}.annualSalary`, { 
                              valueAsNumber: true,
                              onChange: (e) => {
                                const annualSalary = parseFloat(e.target.value) || 0;
                                form.setValue(`employees.${index}.monthlyPayroll`, annualSalary / 12);
                              }
                            })}
                            className="text-right"
                          />
                        </div>
                        <div className="flex gap-2">
                          <Button
                            type="button"
                            variant="outline"
                            size="sm"
                            onClick={() => removeEmployee(index)}
                            className="flex-shrink-0"
                          >
                            <Minus className="h-4 w-4" />
                          </Button>
                        </div>
                      </div>
                      
                      {/* Employee PAYE Preview */}
                      {form.watch(`employees.${index}.annualSalary`) > 0 && (
                        <div className="mt-3 p-3 bg-gray-50 rounded border-t">
                          <div className="grid grid-cols-3 gap-4 text-sm">
                            <div>
                              <span className="text-muted-foreground">Monthly Salary:</span>
                              <div className="font-medium">
                                {formatCurrency(form.watch(`employees.${index}.annualSalary`) / 12)}
                              </div>
                            </div>
                            <div>
                              <span className="text-muted-foreground">Annual PAYE:</span>
                              <div className="font-medium text-sierra-blue-600">
                                {formatCurrency(calculateIndividualPAYE(form.watch(`employees.${index}.annualSalary`)))}
                              </div>
                            </div>
                            <div>
                              <span className="text-muted-foreground">Monthly PAYE:</span>
                              <div className="font-medium text-sierra-blue-600">
                                {formatCurrency(calculateIndividualPAYE(form.watch(`employees.${index}.annualSalary`)) / 12)}
                              </div>
                            </div>
                          </div>
                        </div>
                      )}
                    </Card>
                  ))}

                  {employeeFields.length === 0 && (
                    <div className="text-center py-8 text-muted-foreground">
                      <Users className="h-8 w-8 mx-auto mb-2 opacity-50" />
                      <p>No employees added yet</p>
                      <p className="text-sm">Click "Add Employee" to start building your payroll</p>
                    </div>
                  )}
                </div>

                {/* Payroll Summary */}
                {employeeFields.length > 0 && (
                  <Card className="bg-sierra-gold-50 border-sierra-gold-200">
                    <CardHeader className="pb-2">
                      <CardTitle className="text-sm text-sierra-gold-800">Payroll Summary</CardTitle>
                    </CardHeader>
                    <CardContent className="pt-0">
                      <div className="grid grid-cols-3 gap-4 text-sm">
                        <div>
                          <span className="text-muted-foreground">Total Employees:</span>
                          <div className="font-medium">{employeeFields.length}</div>
                        </div>
                        <div>
                          <span className="text-muted-foreground">Monthly Payroll:</span>
                          <div className="font-medium">{formatCurrency(form.watch('totalPayroll') / 12)}</div>
                        </div>
                        <div>
                          <span className="text-muted-foreground">Annual Payroll:</span>
                          <div className="font-medium">{formatCurrency(form.watch('totalPayroll'))}</div>
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
                    <Label>PAYE Due Date</Label>
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
                    <Label>Remittance Date</Label>
                    <Popover>
                      <PopoverTrigger asChild>
                        <Button
                          variant="outline"
                          className={cn(
                            "w-full justify-start text-left font-normal",
                            !form.watch('remittanceDate') && "text-muted-foreground"
                          )}
                        >
                          <CalendarIcon className="mr-2 h-4 w-4" />
                          {form.watch('remittanceDate') ? (
                            format(form.watch('remittanceDate')!, "PPP")
                          ) : (
                            <span>Pick remittance date</span>
                          )}
                        </Button>
                      </PopoverTrigger>
                      <PopoverContent className="w-auto p-0">
                        <Calendar
                          mode="single"
                          selected={form.watch('remittanceDate')}
                          onSelect={(date) => form.setValue('remittanceDate', date)}
                          initialFocus
                        />
                      </PopoverContent>
                    </Popover>
                  </div>
                </div>

                {form.watch('dueDate') && form.watch('remittanceDate') && (
                  <Alert>
                    <AlertTriangle className="h-4 w-4" />
                    <AlertDescription>
                      {form.watch('remittanceDate')! > form.watch('dueDate')! 
                        ? 'Late remittance penalties may apply'
                        : 'Remittance is on time'
                      }
                    </AlertDescription>
                  </Alert>
                )}

                <Alert>
                  <Info className="h-4 w-4" />
                  <AlertDescription>
                    PAYE must be remitted to NRA by the 15th of the following month. Skills Development Levy is due quarterly.
                  </AlertDescription>
                </Alert>
              </TabsContent>

              {/* Results Tab */}
              <TabsContent value="results" className="space-y-4">
                {calculation ? (
                  <PayrollTaxCalculationResults calculation={calculation} />
                ) : (
                  <div className="text-center py-8 text-muted-foreground">
                    <Calculator className="h-8 w-8 mx-auto mb-2 opacity-50" />
                    <p>No calculation results yet</p>
                    <p className="text-sm">Add employees and calculate to see results</p>
                  </div>
                )}
              </TabsContent>
            </Tabs>

            <Separator />

            <div className="flex justify-end gap-2">
              <Button type="button" variant="outline" onClick={() => form.reset()}>
                Reset
              </Button>
              <Button type="submit" disabled={loading || employeeFields.length === 0}>
                {loading ? 'Calculating...' : 'Calculate Payroll Tax'}
              </Button>
            </div>
          </form>
        </CardContent>
      </Card>
    </div>
  );
}

// Payroll Tax Calculation Results Component
function PayrollTaxCalculationResults({ calculation }: { calculation: PayrollTaxCalculation }) {
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
                <p className="text-sm text-sierra-blue-600 font-medium">Total PAYE</p>
                <p className="text-2xl font-bold text-sierra-blue-800">
                  {formatCurrency(calculation.totalPaye)}
                </p>
              </div>
              <Users className="h-8 w-8 text-sierra-blue-600" />
            </div>
          </CardContent>
        </Card>

        <Card className="border-sierra-gold-200 bg-sierra-gold-50">
          <CardContent className="p-4">
            <div className="flex items-center justify-between">
              <div>
                <p className="text-sm text-sierra-gold-600 font-medium">Skills Dev. Levy</p>
                <p className="text-2xl font-bold text-sierra-gold-800">
                  {formatCurrency(calculation.skillsDevelopmentLevy)}
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
                <p className="text-sm text-sierra-green-600 font-medium">Total Payroll Tax</p>
                <p className="text-2xl font-bold text-sierra-green-800">
                  {formatCurrency(calculation.totalPayrollTax)}
                </p>
              </div>
              <DollarSign className="h-8 w-8 text-sierra-green-600" />
            </div>
          </CardContent>
        </Card>
      </div>

      {/* Overall Summary */}
      <Card>
        <CardHeader>
          <CardTitle>Payroll Tax Summary</CardTitle>
        </CardHeader>
        <CardContent className="space-y-4">
          <div className="grid grid-cols-2 gap-2 text-sm">
            <div className="flex justify-between">
              <span>Number of Employees:</span>
              <span className="font-medium">{calculation.employeeCount}</span>
            </div>
            <div className="flex justify-between">
              <span>Total Annual Payroll:</span>
              <span className="font-medium">{formatCurrency(calculation.totalPayroll)}</span>
            </div>
            <div className="flex justify-between">
              <span>Total PAYE:</span>
              <span className="font-medium text-sierra-blue-600">{formatCurrency(calculation.totalPaye)}</span>
            </div>
            <div className="flex justify-between">
              <span>Skills Development Levy:</span>
              <span className="font-medium text-sierra-gold-600">{formatCurrency(calculation.skillsDevelopmentLevy)}</span>
            </div>
            <div className="flex justify-between border-t pt-2 font-medium">
              <span>Total Payroll Tax:</span>
              <span className="text-sierra-green-600">{formatCurrency(calculation.totalPayrollTax)}</span>
            </div>
          </div>
        </CardContent>
      </Card>

      {/* Employee Breakdown */}
      <Card>
        <CardHeader>
          <CardTitle>Employee PAYE Breakdown</CardTitle>
        </CardHeader>
        <CardContent>
          <div className="space-y-3">
            {calculation.employeeBreakdown.map((employee, index) => (
              <div key={index} className="p-3 border rounded-lg">
                <div className="flex justify-between items-start mb-2">
                  <div>
                    <div className="font-medium">{employee.employeeName}</div>
                    <div className="text-sm text-muted-foreground">ID: {employee.employeeId}</div>
                  </div>
                  <Badge variant="outline">{employee.effectiveRate.toFixed(2)}%</Badge>
                </div>
                <div className="grid grid-cols-2 md:grid-cols-4 gap-2 text-sm">
                  <div>
                    <span className="text-muted-foreground">Annual Salary:</span>
                    <div className="font-medium">{formatCurrency(employee.annualSalary)}</div>
                  </div>
                  <div>
                    <span className="text-muted-foreground">Tax-Free Threshold:</span>
                    <div className="font-medium">{formatCurrency(employee.taxFreeThreshold)}</div>
                  </div>
                  <div>
                    <span className="text-muted-foreground">Taxable Salary:</span>
                    <div className="font-medium">{formatCurrency(employee.taxableSalary)}</div>
                  </div>
                  <div>
                    <span className="text-muted-foreground">Annual PAYE:</span>
                    <div className="font-medium text-sierra-blue-600">{formatCurrency(employee.payeAmount)}</div>
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
          PAYE must be remitted monthly by the 15th. Skills Development Levy (2.5% of payroll) is due quarterly for companies with monthly payroll exceeding SLE 500,000.
        </AlertDescription>
      </Alert>
    </div>
  );
}