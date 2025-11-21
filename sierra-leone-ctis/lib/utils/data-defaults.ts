/**
 * Utility functions for handling default values when API data is missing or null
 * Ensures consistent default handling across the application
 */

/**
 * Get numeric default value (0 for null/undefined)
 */
export const getNumericDefault = (value: number | null | undefined): number => {
  return value ?? 0;
};

/**
 * Get string default value ("N/A" for null/undefined)
 */
export const getStringDefault = (value: string | null | undefined): string => {
  return value ?? "N/A";
};

/**
 * Get array default value (empty array for null/undefined)
 */
export const getArrayDefault = <T>(value: T[] | null | undefined): T[] => {
  return value ?? [];
};

/**
 * Get date default value (null for null/undefined, otherwise converts to Date)
 */
export const getDateDefault = (value: Date | string | null | undefined): Date | null => {
  if (!value) return null;
  return value instanceof Date ? value : new Date(value);
};

/**
 * Get boolean default value (false for null/undefined)
 */
export const getBooleanDefault = (value: boolean | null | undefined): boolean => {
  return value ?? false;
};

/**
 * Get object default value (empty object for null/undefined)
 */
export const getObjectDefault = <T extends Record<string, any>>(value: T | null | undefined): T => {
  return value ?? ({} as T);
};

/**
 * Safely get a nested property with defaults
 */
export const getNestedDefault = <T>(
  obj: any,
  path: string,
  defaultValue: T
): T => {
  if (!obj) return defaultValue;
  const keys = path.split('.');
  let current = obj;
  for (const key of keys) {
    if (current == null || typeof current !== 'object') {
      return defaultValue;
    }
    current = current[key];
  }
  return current ?? defaultValue;
};

/**
 * Format currency with default handling
 */
export const formatCurrencyDefault = (
  amount: number | null | undefined,
  currency: string = 'Le'
): string => {
  const value = getNumericDefault(amount);
  return `${currency} ${value.toLocaleString()}`;
};

/**
 * Format percentage with default handling
 */
export const formatPercentageDefault = (
  value: number | null | undefined,
  decimals: number = 0
): string => {
  const num = getNumericDefault(value);
  return `${num.toFixed(decimals)}%`;
};

