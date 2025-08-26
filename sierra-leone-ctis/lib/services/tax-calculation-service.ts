import { apiClient } from '@/lib/api-client';

// Tax Calculation DTOs
export interface IncomeTaxCalculationRequest {
  taxpayerCategory: 'Individual' | 'Large' | 'Medium' | 'Small' | 'Micro';
  taxYear: number;
  grossIncome: number;
  deductions: number;
  allowances: TaxAllowance[];
  dueDate?: Date;
  paymentDate?: Date;
}

export interface TaxAllowance {
  type: string;
  amount: number;
  description?: string;
}

export interface IncomeTaxCalculation {
  grossIncome: number;
  totalDeductions: number;
  totalAllowances: number;
  taxableIncome: number;
  taxBreakdown: TaxBracket[];
  totalTax: number;
  minimumTax: number;
  payableTax: number;
  effectiveRate: number;
  marginalRate: number;
  penalties?: PenaltyCalculation;
}

export interface TaxBracket {
  from: number;
  to: number;
  rate: number;
  taxableAmount: number;
  taxAmount: number;
}

export interface GstCalculationRequest {
  taxYear: number;
  grossSales: number;
  taxableSupplies: number;
  exemptSupplies: number;
  zeroRatedSupplies: number;
  inputTax: number;
  isExport: boolean;
  isImport: boolean;
  importValue: number;
  dueDate?: Date;
  filingDate?: Date;
}

export interface GstCalculation {
  grossSales: number;
  taxableSupplies: number;
  exemptSupplies: number;
  zeroRatedSupplies: number;
  outputGst: number;
  inputTax: number;
  reverseChargeGst?: number;
  netGstLiability: number;
  refundDue?: number;
  penalties?: PenaltyCalculation;
}

export interface PayrollTaxCalculationRequest {
  taxYear: number;
  employees: PayrollEmployee[];
  totalPayroll: number;
  dueDate?: Date;
  remittanceDate?: Date;
}

export interface PayrollEmployee {
  employeeId: string;
  employeeName: string;
  annualSalary: number;
  monthlyPayroll: number;
}

export interface PayrollTaxCalculation {
  totalPayroll: number;
  employeeCount: number;
  totalPaye: number;
  skillsDevelopmentLevy: number;
  totalPayrollTax: number;
  employeeBreakdown: EmployeePayrollTax[];
  penalties?: PenaltyCalculation;
}

export interface EmployeePayrollTax {
  employeeId: string;
  employeeName: string;
  annualSalary: number;
  taxFreeThreshold: number;
  taxableSalary: number;
  payeAmount: number;
  effectiveRate: number;
}

export interface ExciseDutyCalculationRequest {
  taxYear: number;
  productCategory: 'Tobacco' | 'Alcohol' | 'Fuel';
  items: ExciseDutyItem[];
  dueDate?: Date;
  paymentDate?: Date;
}

export interface ExciseDutyItem {
  productCode: string;
  productName: string;
  quantity: number;
  value: number;
}

export interface ExciseDutyCalculation {
  productCategory: string;
  items: ExciseDutyItemCalculation[];
  totalSpecificDuty: number;
  totalAdValoremDuty: number;
  totalExciseDuty: number;
  penalties?: PenaltyCalculation;
}

export interface ExciseDutyItemCalculation {
  productCode: string;
  productName: string;
  quantity: number;
  value: number;
  specificRate: number;
  adValoremRate: number;
  specificDuty: number;
  adValoremDuty: number;
  totalDuty: number;
}

export interface PenaltyCalculationRequest {
  taxAmount: number;
  dueDate: Date;
  actualDate: Date;
  taxType: 'Income Tax' | 'GST' | 'Payroll Tax' | 'Excise Duty';
}

export interface PenaltyCalculation {
  taxAmount: number;
  daysLate: number;
  lateFilingPenalty: number;
  latePaymentInterest: number;
  totalPenalty: number;
  penaltyBreakdown: PenaltyBreakdownItem[];
}

export interface PenaltyBreakdownItem {
  type: string;
  description: string;
  amount: number;
  calculation: string;
}

export interface ComprehensiveAssessmentRequest {
  clientId: number;
  taxYear: number;
  taxpayerCategory: string;
  
  // Income Tax
  grossIncome: number;
  deductions: number;
  allowances: TaxAllowance[];
  
  // GST
  grossSales: number;
  taxableSupplies: number;
  inputTax: number;
  
  // Payroll
  totalPayroll: number;
  employees: PayrollEmployee[];
  
  // Excise Duty
  exciseDutyItems: ExciseDutyItem[];
  
  // Deadlines
  incomeTaxDueDate?: Date;
  gstDueDate?: Date;
  payrollTaxDueDate?: Date;
  exciseDutyDueDate?: Date;
}

export interface ComprehensiveAssessment {
  clientId: number;
  taxYear: number;
  taxpayerCategory: string;
  
  incomeTaxCalculation: IncomeTaxCalculation;
  gstCalculation: GstCalculation;
  payrollTaxCalculation: PayrollTaxCalculation;
  exciseDutyCalculation: ExciseDutyCalculation;
  
  totalTaxLiability: number;
  totalPenalties: number;
  grandTotal: number;
  
  complianceScore: number;
  complianceGrade: 'A' | 'B' | 'C' | 'D' | 'F';
  complianceIssues: ComplianceIssue[];
}

export interface ComplianceIssue {
  issueType: string;
  description: string;
  severity: 'Low' | 'Medium' | 'High' | 'Critical';
  recommendedAction: string;
  deadline?: Date;
}

export interface TaxRate {
  id: number;
  taxType: string;
  taxpayerCategory?: string;
  taxYear: number;
  fromAmount: number;
  toAmount: number;
  rate: number;
  description: string;
  effectiveDate: Date;
}

export interface TaxTypeInfo {
  taxType: string;
  displayName: string;
  description: string;
  applicableCategories: string[];
}

export interface TaxpayerCategoryInfo {
  category: string;
  displayName: string;
  description: string;
  annualTurnoverThreshold?: number;
  minimumTaxRate?: number;
}

export interface FinanceAct2025Changes {
  effectiveDate: Date;
  keyChanges: FinanceActChange[];
  impactSummary: string;
  complianceDeadlines: Record<string, Date>;
}

export interface FinanceActChange {
  changeType: string;
  description: string;
  oldValue?: string;
  newValue: string;
  effectiveDate: Date;
  impact: 'Low' | 'Medium' | 'High';
}

// Tax Calculation Service
export const TaxCalculationService = {
  // Income Tax
  calculateIncomeTax: async (request: IncomeTaxCalculationRequest): Promise<IncomeTaxCalculation> => {
    try {
      const response = await apiClient.post<IncomeTaxCalculation>('/api/TaxCalculationEngine/income-tax/calculate', request);
      return response.data;
    } catch (error: any) {
      console.error('Error calculating income tax:', error);
      throw new Error(error.response?.data?.message || 'Failed to calculate income tax');
    }
  },

  // GST
  calculateGst: async (request: GstCalculationRequest): Promise<GstCalculation> => {
    try {
      const response = await apiClient.post<GstCalculation>('/api/TaxCalculationEngine/gst/calculate', request);
      return response.data;
    } catch (error: any) {
      console.error('Error calculating GST:', error);
      throw new Error(error.response?.data?.message || 'Failed to calculate GST');
    }
  },

  // Payroll Tax
  calculatePayrollTax: async (request: PayrollTaxCalculationRequest): Promise<PayrollTaxCalculation> => {
    try {
      const response = await apiClient.post<PayrollTaxCalculation>('/api/TaxCalculationEngine/payroll-tax/calculate', request);
      return response.data;
    } catch (error: any) {
      console.error('Error calculating payroll tax:', error);
      throw new Error(error.response?.data?.message || 'Failed to calculate payroll tax');
    }
  },

  // Excise Duty
  calculateExciseDuty: async (request: ExciseDutyCalculationRequest): Promise<ExciseDutyCalculation> => {
    try {
      const response = await apiClient.post<ExciseDutyCalculation>('/api/TaxCalculationEngine/excise-duty/calculate', request);
      return response.data;
    } catch (error: any) {
      console.error('Error calculating excise duty:', error);
      throw new Error(error.response?.data?.message || 'Failed to calculate excise duty');
    }
  },

  // Penalties
  calculatePenalties: async (request: PenaltyCalculationRequest): Promise<PenaltyCalculation> => {
    try {
      const response = await apiClient.post<PenaltyCalculation>('/api/TaxCalculationEngine/penalties/calculate', request);
      return response.data;
    } catch (error: any) {
      console.error('Error calculating penalties:', error);
      throw new Error(error.response?.data?.message || 'Failed to calculate penalties');
    }
  },

  // Comprehensive Assessment
  performComprehensiveAssessment: async (request: ComprehensiveAssessmentRequest): Promise<ComprehensiveAssessment> => {
    try {
      const response = await apiClient.post<ComprehensiveAssessment>('/api/TaxCalculationEngine/assessment', request);
      return response.data;
    } catch (error: any) {
      console.error('Error performing comprehensive assessment:', error);
      throw new Error(error.response?.data?.message || 'Failed to perform comprehensive assessment');
    }
  },

  // Compliance
  getComplianceScore: async (clientId: number, taxYear: number): Promise<{ score: number; grade: string; description: string }> => {
    try {
      const response = await apiClient.get<{ score: number; grade: string; description: string }>(`/api/TaxCalculationEngine/compliance/score/${clientId}/${taxYear}`);
      return response.data;
    } catch (error: any) {
      console.error('Error fetching compliance score:', error);
      throw new Error(error.response?.data?.message || 'Failed to fetch compliance score');
    }
  },

  getComplianceIssues: async (clientId: number, taxYear: number): Promise<ComplianceIssue[]> => {
    try {
      const response = await apiClient.get<ComplianceIssue[]>(`/api/TaxCalculationEngine/compliance/issues/${clientId}/${taxYear}`);
      return response.data;
    } catch (error: any) {
      console.error('Error fetching compliance issues:', error);
      throw new Error(error.response?.data?.message || 'Failed to fetch compliance issues');
    }
  },

  // Information endpoints
  getTaxRates: async (taxYear: number, taxType?: string): Promise<TaxRate[]> => {
    try {
      const params = new URLSearchParams({ taxYear: taxYear.toString() });
      if (taxType) params.append('taxType', taxType);
      
      const response = await apiClient.get<TaxRate[]>(`/api/TaxCalculationEngine/rates/${taxYear}?${params}`);
      return response.data;
    } catch (error: any) {
      console.error('Error fetching tax rates:', error);
      throw new Error(error.response?.data?.message || 'Failed to fetch tax rates');
    }
  },

  getTaxTypes: async (): Promise<TaxTypeInfo[]> => {
    try {
      const response = await apiClient.get<TaxTypeInfo[]>('/api/TaxCalculationEngine/tax-types');
      return response.data;
    } catch (error: any) {
      console.error('Error fetching tax types:', error);
      throw new Error(error.response?.data?.message || 'Failed to fetch tax types');
    }
  },

  getTaxpayerCategories: async (): Promise<TaxpayerCategoryInfo[]> => {
    try {
      const response = await apiClient.get<TaxpayerCategoryInfo[]>('/api/TaxCalculationEngine/taxpayer-categories');
      return response.data;
    } catch (error: any) {
      console.error('Error fetching taxpayer categories:', error);
      throw new Error(error.response?.data?.message || 'Failed to fetch taxpayer categories');
    }
  },

  getFinanceAct2025Changes: async (): Promise<FinanceAct2025Changes> => {
    try {
      const response = await apiClient.get<FinanceAct2025Changes>('/api/TaxCalculationEngine/finance-act-2025/changes');
      return response.data;
    } catch (error: any) {
      console.error('Error fetching Finance Act 2025 changes:', error);
      throw new Error(error.response?.data?.message || 'Failed to fetch Finance Act 2025 changes');
    }
  },

  // Admin endpoints
  createTaxRate: async (taxRate: Omit<TaxRate, 'id'>): Promise<TaxRate> => {
    try {
      const response = await apiClient.post<TaxRate>('/api/TaxCalculationEngine/rates', taxRate);
      return response.data;
    } catch (error: any) {
      console.error('Error creating tax rate:', error);
      throw new Error(error.response?.data?.message || 'Failed to create tax rate');
    }
  },

  updateTaxRate: async (rateId: number, taxRate: Partial<TaxRate>): Promise<TaxRate> => {
    try {
      const response = await apiClient.put<TaxRate>(`/api/TaxCalculationEngine/rates/${rateId}`, taxRate);
      return response.data;
    } catch (error: any) {
      console.error('Error updating tax rate:', error);
      throw new Error(error.response?.data?.message || 'Failed to update tax rate');
    }
  },
};