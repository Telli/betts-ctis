/**
 * Export all services
 */

export * from './auth-service';
export * from './client-service';
export * from './document-service';
export * from './dashboard-service';
export * from './tax-filing-service';
export * from './payment-service';
export * from './payment-gateway-service';
export * from './enrollment-service';
export * from './admin-settings-service';
export * from './analytics-service';
export * from './compliance-service';
export * from './notification-service';
export * from './deadline-service';
export * from './client-portal-service';
export { TaxCalculationService } from './tax-calculation-service';
export type { 
  IncomeTaxCalculationRequest,
  IncomeTaxCalculation,
  GstCalculationRequest,
  GstCalculation,
  PayrollTaxCalculationRequest,
  PayrollTaxCalculation,
  ExciseDutyCalculationRequest,
  ExciseDutyCalculation,
  WithholdingTaxType,
  WithholdingTaxCalculationRequest,
  WithholdingTaxCalculationResponse,
  TaxRate as CalcTaxRate,
  TaxTypeInfo,
  TaxpayerCategoryInfo,
  FinanceAct2025Changes,
  FinanceActChange
} from './tax-calculation-service';

// Associate permission services
export * from './associate-permission-service';
export * from './on-behalf-action-service';
export * from './associate-dashboard-service';
export { 
  ClientDelegationService, 
  type DelegatedClientListResponse,
  type AssociateDto,
  type DelegationStatisticsDto
} from './client-delegation-service';
