/**
 * Payment Method Icon Component for Sierra Leone payment providers
 */

import { 
  CreditCard, 
  Smartphone, 
  Building, 
  DollarSign,
  Banknote
} from 'lucide-react';
import { cn } from '@/lib/utils';

export type PaymentMethod = 
  | 'orange-money' 
  | 'africell-money' 
  | 'bank-transfer' 
  | 'paypal' 
  | 'stripe' 
  | 'cash'
  | 'mobile-money'
  | 'qmoney';

interface PaymentMethodIconProps {
  method: PaymentMethod;
  className?: string;
  size?: 'sm' | 'md' | 'lg';
}

export function PaymentMethodIcon({ method, className, size = 'md' }: PaymentMethodIconProps) {
  const sizeClasses = {
    sm: 'h-4 w-4',
    md: 'h-5 w-5',
    lg: 'h-6 w-6'
  };

  const iconSize = sizeClasses[size];

  switch (method) {
    case 'orange-money':
      return (
        <Smartphone 
          className={cn(iconSize, 'text-orange-500', className)} 
          aria-label="Orange Money"
        />
      );
    
    case 'africell-money':
      return (
        <Smartphone 
          className={cn(iconSize, 'text-red-500', className)} 
          aria-label="Africell Money"
        />
      );
    
    case 'bank-transfer':
      return (
        <Building 
          className={cn(iconSize, 'text-blue-600', className)} 
          aria-label="Bank Transfer"
        />
      );
    
    case 'paypal':
      return (
        <CreditCard 
          className={cn(iconSize, 'text-blue-500', className)} 
          aria-label="PayPal"
        />
      );
    
    case 'stripe':
      return (
        <CreditCard 
          className={cn(iconSize, 'text-purple-500', className)} 
          aria-label="Credit Card (Stripe)"
        />
      );
    
    case 'cash':
      return (
        <Banknote 
          className={cn(iconSize, 'text-green-600', className)} 
          aria-label="Cash Payment"
        />
      );
    
    case 'mobile-money':
    case 'qmoney':
      return (
        <Smartphone 
          className={cn(iconSize, 'text-blue-500', className)} 
          aria-label="Mobile Money"
        />
      );
    
    default:
      return (
        <DollarSign 
          className={cn(iconSize, 'text-gray-500', className)} 
          aria-label="Payment"
        />
      );
  }
}

/**
 * Get the display name for a payment method
 */
export function getPaymentMethodName(method: PaymentMethod): string {
  switch (method) {
    case 'orange-money':
      return 'Orange Money';
    case 'africell-money':
      return 'Africell Money';
    case 'bank-transfer':
      return 'Bank Transfer';
    case 'paypal':
      return 'PayPal';
    case 'stripe':
      return 'Credit Card';
    case 'cash':
      return 'Cash Payment';
    case 'mobile-money':
      return 'Mobile Money';
    case 'qmoney':
      return 'QMoney';
    default:
      return 'Payment';
  }
}

/**
 * Payment method icon with label component
 */
interface PaymentMethodWithLabelProps {
  method: PaymentMethod;
  showLabel?: boolean;
  className?: string;
  iconSize?: 'sm' | 'md' | 'lg';
}

export function PaymentMethodWithLabel({ 
  method, 
  showLabel = true, 
  className,
  iconSize = 'md'
}: PaymentMethodWithLabelProps) {
  return (
    <div className={cn('flex items-center gap-2', className)}>
      <PaymentMethodIcon method={method} size={iconSize} />
      {showLabel && (
        <span className="text-sm font-medium">
          {getPaymentMethodName(method)}
        </span>
      )}
    </div>
  );
}

/**
 * Sierra Leone payment provider information
 */
export const SIERRA_LEONE_PAYMENT_PROVIDERS = {
  'orange-money': {
    name: 'Orange Money',
    provider: 'Orange Sierra Leone',
    description: 'Mobile money service by Orange',
    supportedCurrencies: ['SLE'],
    fees: 'Variable transaction fees apply',
    color: 'orange'
  },
  'africell-money': {
    name: 'Africell Money',
    provider: 'Africell Sierra Leone',
    description: 'Mobile money service by Africell',
    supportedCurrencies: ['SLE'],
    fees: 'Variable transaction fees apply',
    color: 'red'
  },
  'bank-transfer': {
    name: 'Bank Transfer',
    provider: 'Local Banks',
    description: 'Direct bank transfer to registered accounts',
    supportedCurrencies: ['SLE'],
    fees: 'Bank charges may apply',
    color: 'blue'
  },
  'paypal': {
    name: 'PayPal',
    provider: 'PayPal International',
    description: 'International payments for diaspora clients',
    supportedCurrencies: ['USD', 'EUR', 'GBP'],
    fees: 'PayPal fees + currency conversion',
    color: 'blue'
  },
  'stripe': {
    name: 'Credit Card',
    provider: 'Stripe',
    description: 'Credit/debit card payments',
    supportedCurrencies: ['USD', 'EUR', 'GBP'],
    fees: 'Processing fees + currency conversion',
    color: 'purple'
  }
} as const;