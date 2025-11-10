import { apiRequest } from '../api-client';

export interface EmailSettingsDto {
  smtpHost: string;
  smtpPort: number;
  username: string;
  password: string;
  fromEmail: string;
  fromName: string;
  useSSL: boolean;
  useTLS: boolean;
}

export interface TestEmailDto {
  toEmail: string;
  subject?: string;
  body?: string;
}

export interface TaxSettingsDto {
  gstRegistrationThreshold: number;
  gstRatePercent: number;
  annualInterestRatePercent: number;
  incomeMinimumTaxRatePercent: number;
  incomeMatRatePercent: number;
}

export const AdminSettingsService = {
  // Get current email settings
  getEmailSettings: async (): Promise<EmailSettingsDto> => {
    return await apiRequest<EmailSettingsDto>('/api/adminsettings/email');
  },

  // Update email settings
  updateEmailSettings: async (settings: EmailSettingsDto): Promise<void> => {
    await apiRequest('/api/adminsettings/email', {
      method: 'POST',
      body: settings,
    });
  },

  // Send test email
  sendTestEmail: async (testEmail: TestEmailDto): Promise<void> => {
    await apiRequest('/api/adminsettings/email/test', {
      method: 'POST',
      body: testEmail,
    });
  },

  // Get settings by category
  getSettingsByCategory: async (category: string): Promise<Record<string, string>> => {
    return await apiRequest<Record<string, string>>(`/api/adminsettings/category/${category}`);
  },

  // Get tax settings
  getTaxSettings: async (): Promise<TaxSettingsDto> => {
    return await apiRequest<TaxSettingsDto>('/api/adminsettings/tax');
  },

  // Update tax settings
  updateTaxSettings: async (settings: TaxSettingsDto): Promise<void> => {
    await apiRequest('/api/adminsettings/tax', {
      method: 'POST',
      body: settings,
    });
  },
};