import { API_BASE_URL, authenticatedFetch } from "../auth";

export interface SelectionOption {
  value: string;
  label: string;
}

export interface FilingScheduleEntry {
  id: number;
  description: string;
  amount: number;
  taxableAmount: number;
}

export interface FilingDocument {
  id: number;
  name: string;
  version: number;
  uploadedBy: string;
  uploadedAt: string;
}

export interface FilingHistoryEntry {
  timestamp: string;
  user: string;
  action: string;
  changes: string;
}

export interface FilingWorkspace {
  filingId: string;
  clientId?: number;
  clientName: string;
  title: string;
  taxType: string;
  taxPeriodOptions: SelectionOption[];
  selectedTaxPeriod: string;
  filingStatusOptions: SelectionOption[];
  selectedFilingStatus: string;
  totalSales: number;
  taxableSales: number;
  gstRate: number;
  outputTax: number;
  inputTaxCredit: number;
  netGstPayable: number;
  notes: string;
  schedule: FilingScheduleEntry[];
  supportingDocuments: FilingDocument[];
  history: FilingHistoryEntry[];
}

interface FilingResponse {
  success: boolean;
  data?: FilingWorkspace;
  message?: string;
}

export async function fetchActiveFiling(): Promise<FilingWorkspace> {
  const response = await authenticatedFetch(`${API_BASE_URL}/filings/active`);
  const payload = (await response.json()) as FilingResponse;

  if (!response.ok || !payload?.success || !payload.data) {
    const message = payload?.message || `Failed to load active filing (status ${response.status})`;
    throw new Error(message);
  }

  const data = payload.data;
  return {
    filingId: data.filingId ?? "",
    clientId: data.clientId,
    clientName: data.clientName ?? "Not available",
    title: data.title ?? "Not available",
    taxType: data.taxType ?? "Not available",
    taxPeriodOptions: data.taxPeriodOptions ?? [],
    selectedTaxPeriod: data.selectedTaxPeriod ?? "",
    filingStatusOptions: data.filingStatusOptions ?? [],
    selectedFilingStatus: data.selectedFilingStatus ?? "",
    totalSales: data.totalSales ?? 0,
    taxableSales: data.taxableSales ?? 0,
    gstRate: data.gstRate ?? 0,
    outputTax: data.outputTax ?? 0,
    inputTaxCredit: data.inputTaxCredit ?? 0,
    netGstPayable: data.netGstPayable ?? 0,
    notes: data.notes ?? "",
    schedule: data.schedule ?? [],
    supportingDocuments: data.supportingDocuments ?? [],
    history: data.history ?? [],
  };
}
