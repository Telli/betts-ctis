import { apiClient } from '@/lib/api-client';

export interface TaxFilingDto {
  taxFilingId: number;
  clientId: number;
  clientName: string;
  clientNumber: string;
  taxType: TaxType;
  taxYear: number;
  filingDate: string;
  dueDate: string;
  status: FilingStatus;
  taxLiability: number;
  filingReference: string;
  submittedDate?: string;
  submittedBy?: string;
  submittedByName?: string;
  reviewedDate?: string;
  reviewedBy?: string;
  reviewedByName?: string;
  reviewComments?: string;
  createdDate: string;
  updatedDate: string;
  documentCount: number;
  paymentCount: number;
  totalPaid: number;
}

export interface CreateTaxFilingDto {
  clientId: number;
  taxType: TaxType;
  taxYear: number;
  dueDate: string;
  taxLiability: number;
  filingReference?: string;
  // Extended fields (optional)
  filingPeriod?: string;
  penaltyAmount?: number;
  interestAmount?: number;
  taxableAmount?: number;
  additionalData?: string;
  // Withholding-specific (optional)
  withholdingTaxSubtype?: string;
  isResident?: boolean;
}

export interface UpdateTaxFilingDto {
  taxType?: TaxType;
  taxYear?: number;
  dueDate?: string;
  taxLiability?: number;
  filingReference?: string;
  reviewComments?: string;
  // Withholding-specific (optional)
  withholdingTaxSubtype?: string;
  isResident?: boolean;
}

export interface ReviewTaxFilingDto {
  status: FilingStatus;
  reviewComments?: string;
}

export enum TaxType {
  IncomeTax = 'IncomeTax',
  GST = 'GST',
  PayrollTax = 'PayrollTax',
  ExciseDuty = 'ExciseDuty',
  PAYE = 'PAYE',
  WithholdingTax = 'WithholdingTax',
  PersonalIncomeTax = 'PersonalIncomeTax',
  CorporateIncomeTax = 'CorporateIncomeTax'
}

export enum FilingStatus {
  Draft = 'Draft',
  Submitted = 'Submitted',
  UnderReview = 'UnderReview',
  Approved = 'Approved',
  Rejected = 'Rejected',
  Filed = 'Filed'
}

export interface TaxFilingListResponse {
  success: boolean;
  data: TaxFilingDto[];
  pagination: {
    currentPage: number;
    pageSize: number;
    totalCount: number;
    totalPages: number;
  };
}

export interface TaxFilingResponse {
  success: boolean;
  data: TaxFilingDto;
}

export interface TaxLiabilityCalculationRequest {
  clientId: number;
  taxType: TaxType;
  taxYear: number;
  taxableAmount: number;
}

export interface TaxLiabilityCalculationResponse {
  success: boolean;
  data: {
    taxLiability: number;
    taxType: TaxType;
    taxableAmount: number;
    effectiveRate: number;
    breakdown: {
      baseTax: number;
      additionalTax: number;
      penalties: number;
      total: number;
    };
  };
}
export interface FilingScheduleDto {
  id?: number;
  description: string;
  amount: number;
  taxable: number;
}

export interface FilingAssessmentDto {
  totalSales: number;
  taxableSales: number;
  gstRate: number;
  outputTax: number;
  inputTaxCredit: number;
  penalties: number;
  totalPayable: number;
}

export interface FilingHistoryEntryDto {
  id: number;
  timestamp: string;
  user: string;
  action: string;
  changes: string;
}

export interface TaxFilingValidationResultDto {
  isValid: boolean;
  errors: string[];
  warnings: string[];
}

export const TaxFilingService = {
  /**
   * Get paginated list of tax filings with optional filtering
   */
  getTaxFilings: async (
    page: number = 1,
    pageSize: number = 20,
    searchTerm?: string,
    taxType?: TaxType,
    status?: FilingStatus,
    clientId?: number
  ): Promise<TaxFilingListResponse> => {
    const params = new URLSearchParams({
      page: page.toString(),
      pageSize: pageSize.toString(),
    });

    if (searchTerm) params.append('search', searchTerm);
    if (taxType) params.append('taxType', taxType);
    if (status) params.append('status', status);
    if (clientId) params.append('clientId', clientId.toString());

    const response = await apiClient.get<TaxFilingListResponse>(`/api/tax-filings?${params}`);
    return response.data;
  },

  /**
   * Get specific tax filing by ID
   */
  getTaxFilingById: async (id: number): Promise<TaxFilingResponse> => {
    const response = await apiClient.get<TaxFilingResponse>(`/api/tax-filings/${id}`);
    return response.data;
  },

  /**
   * Get tax filings for a specific client
   */
  getTaxFilingsByClientId: async (clientId: number): Promise<TaxFilingResponse[]> => {
    const response = await apiClient.get<{ success: boolean; data: TaxFilingDto[] }>(`/api/tax-filings/client/${clientId}`);
    return response.data.data.map(filing => ({ success: true, data: filing }));
  },

  /**
   * Create a new tax filing
   */
  createTaxFiling: async (filing: CreateTaxFilingDto): Promise<TaxFilingResponse> => {
    const response = await apiClient.post<TaxFilingResponse>('/api/tax-filings', filing);
    return response.data;
  },

  /**
   * Update an existing tax filing
   */
  updateTaxFiling: async (id: number, filing: UpdateTaxFilingDto): Promise<TaxFilingResponse> => {
    const response = await apiClient.put<TaxFilingResponse>(`/api/tax-filings/${id}`, filing);
    return response.data;
  },

  /**
   * Delete a tax filing (only draft filings)
   */
  deleteTaxFiling: async (id: number): Promise<{ success: boolean; message: string }> => {
    const response = await apiClient.delete<{ success: boolean; message: string }>(`/api/tax-filings/${id}`);
    return response.data;
  },

  /**
   * Submit a tax filing for review
   */
  submitTaxFiling: async (id: number): Promise<TaxFilingResponse> => {
    const response = await apiClient.post<TaxFilingResponse>(`/api/tax-filings/${id}/submit`);
    return response.data;
  },

  /**
   * Review a tax filing (approve/reject)
   */
  reviewTaxFiling: async (id: number, review: ReviewTaxFilingDto): Promise<TaxFilingResponse> => {
    const response = await apiClient.post<TaxFilingResponse>(`/api/tax-filings/${id}/review`, review);
    return response.data;
  },

  /**
   * Get upcoming deadlines
   */
  getUpcomingDeadlines: async (days: number = 30): Promise<{ success: boolean; data: TaxFilingDto[] }> => {
    const response = await apiClient.get<{ success: boolean; data: TaxFilingDto[] }>(`/api/tax-filings/deadlines?days=${days}`);
    return response.data;
  },

  /**
   * Calculate tax liability
   */
  calculateTaxLiability: async (
    request: TaxLiabilityCalculationRequest
  ): Promise<TaxLiabilityCalculationResponse> => {
    const response = await apiClient.post<TaxLiabilityCalculationResponse>(
      '/api/tax-filings/calculate-liability',
      request
    );
    return response.data;
  },

  /**
   * Get schedules for a tax filing
   */
  getSchedules: async (taxFilingId: number): Promise<FilingScheduleDto[]> => {
    try {
      const response = await apiClient.get<{ success: boolean; data: any[] }>(
        `/api/tax-filings/${taxFilingId}/schedules`
      );
      const raw = response.data?.data ?? [];

      return raw.map((item, index) => ({
        id: item.id ?? item.Id ?? index + 1,
        description: item.description ?? item.Description ?? '',
        amount: Number(item.amount ?? item.Amount ?? 0),
        taxable: Number(item.taxable ?? item.Taxable ?? 0),
      }));
    } catch (error) {
      console.warn('Failed to fetch filing schedules, returning empty array:', error);
      return [];
    }
  },

  /**
   * Save schedules for a tax filing
   */
  saveSchedules: async (
    taxFilingId: number,
    schedules: FilingScheduleDto[]
  ): Promise<{ success: boolean; message?: string }> => {
    const payload = schedules.map((s) => ({
      description: s.description?.trim() ?? '',
      amount: s.amount,
      taxable: s.taxable,
    }));

    const response = await apiClient.post<{ success: boolean; message?: string }>(
      `/api/tax-filings/${taxFilingId}/schedules`,
      payload
    );

    return response.data;
  },

  /**
   * Get assessment summary for a tax filing
   */
  getAssessmentSummary: async (taxFilingId: number): Promise<FilingAssessmentDto> => {
    const response = await apiClient.get<{ success: boolean; data: any }>(
      `/api/tax-filings/${taxFilingId}/assessment`
    );
    const d = response.data?.data ?? {};

    return {
      totalSales: Number(d.totalSales ?? d.TotalSales ?? 0),
      taxableSales: Number(d.taxableSales ?? d.TaxableSales ?? 0),
      gstRate: Number(d.gstRate ?? d.GstRate ?? 0),
      outputTax: Number(d.outputTax ?? d.OutputTax ?? 0),
      inputTaxCredit: Number(d.inputTaxCredit ?? d.InputTaxCredit ?? 0),
      penalties: Number(d.penalties ?? d.Penalties ?? 0),
      totalPayable: Number(d.totalPayable ?? d.TotalPayable ?? 0),
    };
  },

  /**
   * Get filing history (audit trail) for a tax filing
   */
  getFilingHistory: async (taxFilingId: number): Promise<FilingHistoryEntryDto[]> => {
    try {
      const response = await apiClient.get<{ success: boolean; data: any[] }>(
        `/api/tax-filings/${taxFilingId}/history`
      );
      const raw = response.data?.data ?? [];

      return raw.map((item) => ({
        id: item.id ?? item.Id ?? 0,
        timestamp: item.timestamp ?? item.Timestamp ?? '',
        user: item.user ?? item.User ?? 'System',
        action: item.action ?? item.Action ?? '',
        changes: item.changes ?? item.Changes ?? '',
      }));
    } catch (error) {
      console.warn('Failed to fetch filing history, returning empty array:', error);
      return [];
    }
  },

  // Associate-specific endpoints

  /**
   * Get tax filings for clients delegated to current associate
   */
  getDelegatedTaxFilings: async (
    page: number = 1,
    pageSize: number = 20,
    searchTerm?: string,
    taxType?: TaxType,
    status?: FilingStatus
  ): Promise<TaxFilingListResponse> => {
    const params = new URLSearchParams({
      page: page.toString(),
      pageSize: pageSize.toString(),
    });

    if (searchTerm) params.append('search', searchTerm);
    if (taxType) params.append('taxType', taxType);
    if (status) params.append('status', status);

    const response = await apiClient.get<TaxFilingListResponse>(`/api/tax-filings/associate/delegated?${params}`);
    return response.data;
  },

  /**
   * Create tax filing on behalf of a client
   */
  createTaxFilingOnBehalf: async (clientId: number, filing: Omit<CreateTaxFilingDto, 'clientId'>): Promise<TaxFilingResponse> => {
    const response = await apiClient.post<TaxFilingResponse>(`/api/tax-filings/client/${clientId}`, filing);
    return response.data;
  },

  /**
   * Update tax filing on behalf of a client
   */
  updateTaxFilingOnBehalf: async (id: number, filing: UpdateTaxFilingDto): Promise<TaxFilingResponse> => {
    const response = await apiClient.put<TaxFilingResponse>(`/api/tax-filings/${id}/on-behalf`, filing);
    return response.data;
  },

  /**
   * Submit tax filing on behalf of a client
   */
  submitTaxFilingOnBehalf: async (id: number): Promise<TaxFilingResponse> => {
    const response = await apiClient.post<TaxFilingResponse>(`/api/tax-filings/${id}/submit-on-behalf`);
    return response.data;
  },

  // Filing Workspace endpoints

  /**
   * Get complete filing workspace data
   */
  getFilingWorkspace: async (id: number): Promise<any> => {
    const response = await apiClient.get<{ success: boolean; data: any }>(`/api/tax-filings/${id}/workspace`);
    return response.data.data;
  },



  /**
   * Import schedules from CSV/Excel
   */
  importSchedules: async (id: number, file: File): Promise<void> => {
    const formData = new FormData();
    formData.append('file', file);
    await apiClient.post(`/api/tax-filings/${id}/schedules/import`, formData, { isFormData: true });
  },

  /**
   * Validate a tax filing before submission
   */
  validateTaxFilingForSubmission: async (
    id: number
  ): Promise<TaxFilingValidationResultDto> => {
    const response = await apiClient.get<{
      success: boolean;
      data: TaxFilingValidationResultDto;
    }>(`/api/tax-filings/${id}/validate`);
    return response.data.data;
  },

  /**
   * Get calculated assessment summary
   */
  getAssessment: async (id: number): Promise<any> => {
    const response = await apiClient.get<{ success: boolean; data: any }>(`/api/tax-filings/${id}/assessment`);
    return response.data.data;
  },

  /**
   * Get documents for a filing
   */
  getFilingDocuments: async (id: number): Promise<any[]> => {
    const response = await apiClient.get<{ success: boolean; data: any[] }>(`/api/tax-filings/${id}/documents`);
    return response.data.data;
  },

  /**
   * Upload document for a filing
   */
  uploadFilingDocument: async (id: number, file: File): Promise<void> => {
    const formData = new FormData();
    formData.append('file', file);
    await apiClient.post(`/api/tax-filings/${id}/documents`, formData, { isFormData: true });
  },

  /**
   * Download filing document
   */
  downloadFilingDocument: async (id: number, documentId: number): Promise<void> => {
    const response = await apiClient.get<Blob>(`/api/tax-filings/${id}/documents/${documentId}`, {
      responseType: 'blob'
    });
    const blob = response.data as Blob;
    const url = window.URL.createObjectURL(blob);
    const link = document.createElement('a');
    link.href = url;
    link.setAttribute('download', `document-${documentId}.pdf`);
    document.body.appendChild(link);
    link.click();
    link.remove();
  },


  /**
   * Save filing draft
   */
  saveDraft: async (id: number, filing: any): Promise<void> => {
    await apiClient.post(`/api/tax-filings/${id}/save-draft`, filing);
  },

  /**
   * Submit filing for review
   */
  submitFiling: async (id: number): Promise<void> => {
    await apiClient.post(`/api/tax-filings/${id}/submit`);
  }
};

export default TaxFilingService;