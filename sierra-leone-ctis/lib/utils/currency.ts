/**
 * Currency formatting utilities for Sierra Leone operations
 */

/**
 * Format amount in Sierra Leone Leones
 * @param amount - The amount to format
 * @param showDecimals - Whether to show decimal places (default: false for whole numbers)
 * @returns Formatted currency string
 */
export const formatSierraLeones = (amount: number, showDecimals: boolean = false): string => {
  if (amount === 0) return 'Le 0';
  
  const options: Intl.NumberFormatOptions = {
    minimumFractionDigits: showDecimals ? 2 : 0,
    maximumFractionDigits: showDecimals ? 2 : 0,
  };
  
  return `Le ${amount.toLocaleString('en-SL', options)}`;
};

/**
 * Format amount as compact Sierra Leone Leones (e.g., Le 1.2M)
 * @param amount - The amount to format
 * @returns Compact formatted currency string
 */
export const formatCompactSierraLeones = (amount: number): string => {
  if (amount === 0) return 'Le 0';
  
  const formatCompact = new Intl.NumberFormat('en-SL', {
    notation: 'compact',
    compactDisplay: 'short',
    maximumFractionDigits: 1
  });
  
  return `Le ${formatCompact.format(amount)}`;
};

/**
 * Format percentage for Sierra Leone context
 * @param percentage - The percentage to format
 * @param decimals - Number of decimal places (default: 1)
 * @returns Formatted percentage string
 */
export const formatPercentage = (percentage: number, decimals: number = 1): string => {
  return `${percentage.toFixed(decimals)}%`;
};

/**
 * Parse Sierra Leone currency string back to number
 * @param currencyString - String in format "Le 1,000" or "1,000"
 * @returns Parsed number
 */
export const parseSierraLeones = (currencyString: string): number => {
  // Remove "Le", spaces, and commas, then parse
  const cleaned = currencyString.replace(/Le\s?|,/g, '').trim();
  return parseFloat(cleaned) || 0;
};

/**
 * Format amount with currency and optional comparison
 * @param current - Current amount
 * @param previous - Previous amount for comparison (optional)
 * @returns Object with formatted current amount and growth info
 */
export const formatCurrencyWithGrowth = (current: number, previous?: number) => {
  const formatted = formatSierraLeones(current);
  
  if (previous === undefined) {
    return { formatted, growth: null };
  }
  
  const growthAmount = current - previous;
  const growthPercentage = previous !== 0 ? (growthAmount / previous) * 100 : 0;
  
  return {
    formatted,
    growth: {
      amount: formatSierraLeones(Math.abs(growthAmount)),
      percentage: formatPercentage(Math.abs(growthPercentage)),
      isPositive: growthAmount >= 0,
      isNegative: growthAmount < 0
    }
  };
};

/**
 * Sierra Leone specific currency constants
 */
export const SIERRA_LEONE_CURRENCY = {
  code: 'SLE',
  symbol: 'Le',
  name: 'Sierra Leone Leone',
  minorUnit: 100, // 100 cents = 1 leone
} as const;

/**
 * Tax calculation formatting specific to Sierra Leone
 */
export const formatTaxAmount = (amount: number, context: 'liability' | 'payment' | 'refund' = 'liability'): string => {
  const formatted = formatSierraLeones(amount, true); // Show decimals for tax calculations
  
  switch (context) {
    case 'liability':
      return `${formatted} (Tax Liability)`;
    case 'payment':
      return `${formatted} (Payment)`;
    case 'refund':
      return `${formatted} (Refund)`;
    default:
      return formatted;
  }
};