import { apiRequest } from '@/lib/api-client';

export enum PaymentGatewayType {
  OrangeMoney = 'OrangeMoney',
  AfricellMoney = 'AfricellMoney',
  BankTransfer = 'BankTransfer',
  CreditCard = 'CreditCard',
  PayPal = 'PayPal',
  Stripe = 'Stripe',
  Cash = 'Cash',
}

const gatewayTypeNumberMap: Record<number, PaymentGatewayType> = {
  1: PaymentGatewayType.OrangeMoney,
  2: PaymentGatewayType.AfricellMoney,
  3: PaymentGatewayType.BankTransfer,
  4: PaymentGatewayType.CreditCard,
  5: PaymentGatewayType.PayPal,
  6: PaymentGatewayType.Stripe,
  7: PaymentGatewayType.Cash,
};

export enum PaymentPurpose {
  TaxPayment = 'TaxPayment',
  PenaltyPayment = 'PenaltyPayment',
  InterestPayment = 'InterestPayment',
  FilingFee = 'FilingFee',
  ServiceFee = 'ServiceFee',
  AdvancePayment = 'AdvancePayment',
  Refund = 'Refund',
  Other = 'Other',
}

const paymentPurposeNumberMap: Record<number, PaymentPurpose> = {
  1: PaymentPurpose.TaxPayment,
  2: PaymentPurpose.PenaltyPayment,
  3: PaymentPurpose.InterestPayment,
  4: PaymentPurpose.FilingFee,
  5: PaymentPurpose.ServiceFee,
  6: PaymentPurpose.AdvancePayment,
  7: PaymentPurpose.Refund,
  8: PaymentPurpose.Other,
};

export enum PaymentTransactionStatus {
  Initiated = 'Initiated',
  Pending = 'Pending',
  Processing = 'Processing',
  Completed = 'Completed',
  Failed = 'Failed',
  Cancelled = 'Cancelled',
  Expired = 'Expired',
  Refunded = 'Refunded',
  PartialRefund = 'PartialRefund',
  Disputed = 'Disputed',
  Chargeback = 'Chargeback',
  Settled = 'Settled',
  DeadLetter = 'DeadLetter',
}

const transactionStatusNumberMap: Record<number, PaymentTransactionStatus> = {
  0: PaymentTransactionStatus.Initiated,
  1: PaymentTransactionStatus.Pending,
  2: PaymentTransactionStatus.Processing,
  3: PaymentTransactionStatus.Completed,
  4: PaymentTransactionStatus.Failed,
  5: PaymentTransactionStatus.Cancelled,
  6: PaymentTransactionStatus.Expired,
  7: PaymentTransactionStatus.Refunded,
  8: PaymentTransactionStatus.PartialRefund,
  9: PaymentTransactionStatus.Disputed,
  10: PaymentTransactionStatus.Chargeback,
  11: PaymentTransactionStatus.Settled,
  12: PaymentTransactionStatus.DeadLetter,
};

export enum SecurityRiskLevel {
  Low = 'Low',
  Medium = 'Medium',
  High = 'High',
  Critical = 'Critical',
}

const riskLevelNumberMap: Record<number, SecurityRiskLevel> = {
  1: SecurityRiskLevel.Low,
  2: SecurityRiskLevel.Medium,
  3: SecurityRiskLevel.High,
  4: SecurityRiskLevel.Critical,
};

const toGatewayType = (value: number | string): PaymentGatewayType => {
  if (typeof value === 'number') {
    return gatewayTypeNumberMap[value] ?? PaymentGatewayType.OrangeMoney;
  }
  return (value as PaymentGatewayType) ?? PaymentGatewayType.OrangeMoney;
};

const toPaymentPurpose = (value: number | string): PaymentPurpose => {
  if (typeof value === 'number') {
    return paymentPurposeNumberMap[value] ?? PaymentPurpose.TaxPayment;
  }
  return (value as PaymentPurpose) ?? PaymentPurpose.TaxPayment;
};

const toTransactionStatus = (value: number | string): PaymentTransactionStatus => {
  if (typeof value === 'number') {
    return transactionStatusNumberMap[value] ?? PaymentTransactionStatus.Initiated;
  }
  return (value as PaymentTransactionStatus) ?? PaymentTransactionStatus.Initiated;
};

const toRiskLevel = (value: number | string | undefined): SecurityRiskLevel => {
  if (typeof value === 'number') {
    return riskLevelNumberMap[value] ?? SecurityRiskLevel.Low;
  }
  if (!value) {
    return SecurityRiskLevel.Low;
  }
  return (value as SecurityRiskLevel) ?? SecurityRiskLevel.Low;
};

export interface PaymentTransactionDto {
  id: number;
  transactionReference: string;
  externalReference?: string;
  clientId: number;
  gatewayType: PaymentGatewayType;
  gatewayName?: string;
  purpose: PaymentPurpose;
  purposeName?: string;
  amount: number;
  fee: number;
  netAmount: number;
  currency: string;
  payerPhone: string;
  payerName?: string;
  payerEmail?: string;
  status: PaymentTransactionStatus;
  statusName?: string;
  statusMessage?: string;
  description?: string;
  riskLevel: SecurityRiskLevel;
  requiresManualReview: boolean;
  initiatedAt: string;
  processedAt?: string;
  completedAt?: string;
  failedAt?: string;
  expiresAt: string;
  retryCount: number;
  nextRetryAt?: string;
}

export interface CreatePaymentTransactionRequest {
  clientId: number;
  gatewayType: PaymentGatewayType;
  purpose: PaymentPurpose;
  amount: number;
  currency: string;
  payerPhone: string;
  payerName?: string;
  payerEmail?: string;
  description?: string;
  taxFilingId?: number;
  taxYearId?: number;
  taxType?: string;
  ipAddress?: string;
  userAgent?: string;
  successUrl?: string;
  failureUrl?: string;
  cancelUrl?: string;
}

interface RawPaymentTransactionDto {
  id: number;
  transactionReference: string;
  externalReference?: string;
  clientId: number;
  gatewayType: number | string;
  gatewayName?: string;
  purpose: number | string;
  purposeName?: string;
  amount: number;
  fee: number;
  netAmount: number;
  currency: string;
  payerPhone: string;
  payerName?: string;
  payerEmail?: string;
  status: number | string;
  statusName?: string;
  statusMessage?: string;
  description?: string;
  riskLevel?: number | string;
  requiresManualReview: boolean;
  initiatedAt: string;
  processedAt?: string;
  completedAt?: string;
  failedAt?: string;
  expiresAt: string;
  retryCount: number;
  nextRetryAt?: string;
}

const normalizeTransaction = (dto: RawPaymentTransactionDto): PaymentTransactionDto => ({
  id: dto.id,
  transactionReference: dto.transactionReference,
  externalReference: dto.externalReference,
  clientId: dto.clientId,
  gatewayType: toGatewayType(dto.gatewayType),
  gatewayName: dto.gatewayName,
  purpose: toPaymentPurpose(dto.purpose),
  purposeName: dto.purposeName,
  amount: dto.amount,
  fee: dto.fee,
  netAmount: dto.netAmount,
  currency: dto.currency,
  payerPhone: dto.payerPhone,
  payerName: dto.payerName,
  payerEmail: dto.payerEmail,
  status: toTransactionStatus(dto.status),
  statusName: dto.statusName,
  statusMessage: dto.statusMessage,
  description: dto.description,
  riskLevel: toRiskLevel(dto.riskLevel),
  requiresManualReview: dto.requiresManualReview,
  initiatedAt: dto.initiatedAt,
  processedAt: dto.processedAt,
  completedAt: dto.completedAt,
  failedAt: dto.failedAt,
  expiresAt: dto.expiresAt,
  retryCount: dto.retryCount,
  nextRetryAt: dto.nextRetryAt,
});

export interface MobileMoneyProcessRequest {
  pin: string;
}

const getMobileMoneyPath = (gatewayType: PaymentGatewayType): string => {
  switch (gatewayType) {
    case PaymentGatewayType.AfricellMoney:
      return 'africell';
    case PaymentGatewayType.OrangeMoney:
    default:
      return 'orange';
  }
};

export const PaymentGatewayService = {
  async initiateTransaction(payload: CreatePaymentTransactionRequest): Promise<PaymentTransactionDto> {
    const data = await apiRequest<RawPaymentTransactionDto>('/api/paymentgateway/transactions', {
      method: 'POST',
      body: payload,
    });
    return normalizeTransaction(data);
  },

  async getTransaction(transactionId: number): Promise<PaymentTransactionDto> {
    const data = await apiRequest<RawPaymentTransactionDto>(`/api/paymentgateway/transactions/${transactionId}`);
    return normalizeTransaction(data);
  },

  async processMobileMoney(transactionId: number, gatewayType: PaymentGatewayType, pin: string): Promise<PaymentTransactionDto> {
    const segment = getMobileMoneyPath(gatewayType);
    const data = await apiRequest<RawPaymentTransactionDto>(`/api/paymentgateway/mobile-money/${segment}/${transactionId}`, {
      method: 'POST',
      body: { pin } satisfies MobileMoneyProcessRequest,
    });
    return normalizeTransaction(data);
  },

  async processPayment(transactionId: number): Promise<PaymentTransactionDto> {
    const data = await apiRequest<RawPaymentTransactionDto>(`/api/paymentgateway/transactions/${transactionId}/process`, {
      method: 'POST',
      body: { transactionId },
    });
    return normalizeTransaction(data);
  },
};

export type PaymentTransaction = PaymentTransactionDto;
