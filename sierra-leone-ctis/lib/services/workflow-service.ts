import { api } from '@/lib/api-client'

interface ApiResponse<T> {
  success: boolean
  data?: T
  message?: string
}

const unwrap = <T>(response: ApiResponse<T>): T => {
  if (response?.success && response.data !== undefined) {
    return response.data
  }

  throw new Error(response?.message || 'Unexpected workflow API response')
}

export enum WorkflowInstanceStatus {
  NotStarted = 0,
  Running = 1,
  WaitingForApproval = 2,
  Paused = 3,
  Completed = 4,
  Failed = 5,
  Cancelled = 6,
}

export enum WorkflowTriggerType {
  Manual = 0,
  Event = 1,
  Schedule = 2,
  Webhook = 3,
  FileWatch = 4,
}

export enum WorkflowType {
  PaymentApproval = 1,
  DocumentReview = 2,
  ComplianceCheck = 3,
  Notification = 4,
  TaxFiling = 5,
  ClientOnboarding = 6,
  Custom = 99,
}

export enum WorkflowStepInstanceStatus {
  NotStarted = 0,
  Running = 1,
  WaitingForApproval = 2,
  Completed = 3,
  Failed = 4,
  Skipped = 5,
  Cancelled = 6,
}

export interface WorkflowDefinition {
  id: string
  name: string
  description: string
  type: WorkflowType | number
  triggerType: number | string
  isActive: boolean
  priority: number
}

export interface WorkflowMetrics {
  totalWorkflows: number
  activeWorkflows: number
  totalInstances: number
  runningInstances: number
  pendingApprovals: number
  overallSuccessRate: number
  lastUpdated: string
}

export interface WorkflowStepInstance {
  id: string
  workflowInstanceId: string
  workflowStepId: string
  status: WorkflowStepInstanceStatus | number | string
  input: Record<string, unknown>
  output: Record<string, unknown>
  assignedTo?: string
  startedAt?: string
  completedAt?: string
  completedBy?: string
  errorMessage?: string
  retryCount: number
}

export enum WorkflowApprovalStatus {
  Pending = 0,
  Approved = 1,
  Rejected = 2,
  Cancelled = 3,
}

export interface WorkflowApproval {
  id: string
  workflowInstanceId: string
  workflowStepInstanceId: string
  requiredApprover: string
  status: WorkflowApprovalStatus | number | string
  comments?: string
  requestedAt: string
  respondedAt?: string
  respondedBy?: string
}

export interface WorkflowInstance {
  id: string
  workflowId: string
  name: string
  description?: string
  status: WorkflowInstanceStatus
  variables: Record<string, unknown>
  context: Record<string, unknown>
  createdBy: string
  createdAt: string
  startedAt?: string
  completedAt?: string
  completedBy?: string
  errorMessage?: string
  stepInstances: WorkflowStepInstance[]
  approvals: WorkflowApproval[]
}

export interface WorkflowTrigger {
  id: string
  workflowId: string
  name: string
  type: WorkflowTriggerType | number | string
  configuration: Record<string, unknown>
  isActive: boolean
  createdBy: string
  createdAt: string
}

export interface CreateWorkflowTriggerInput {
  name: string
  type: WorkflowTriggerType
  configuration: Record<string, unknown>
}

export interface StartWorkflowInstanceRequest {
  workflowId: string
  variables: Record<string, unknown>
}

export interface CancelWorkflowInstanceRequest {
  reason: string
}

const getQueryString = (params: Record<string, string | undefined>) => {
  const search = new URLSearchParams();
  Object.entries(params).forEach(([key, value]) => {
    if (value) {
      search.append(key, value);
    }
  });

  const query = search.toString();
  return query ? `?${query}` : '';
};

export const workflowService = {
  async getDefinitions(includeInactive = false): Promise<WorkflowDefinition[]> {
    const response = await api.get<ApiResponse<WorkflowDefinition[]>>(
      `/api/workflow/definitions${includeInactive ? '?includeInactive=true' : ''}`
    )
    return unwrap(response.data)
  },

  async createDefinition(definition: Omit<WorkflowDefinition, 'id'> & Partial<Pick<WorkflowDefinition, 'id'>>): Promise<WorkflowDefinition> {
    const payload = {
      name: definition.name,
      description: definition.description,
      type: definition.type,
      triggerType: definition.triggerType,
      isActive: definition.isActive ?? true,
      priority: definition.priority ?? 1,
    }
    const response = await api.post<ApiResponse<WorkflowDefinition>>('/api/workflow/definitions', payload)
    return unwrap(response.data)
  },

  async updateDefinition(id: string, updates: Partial<WorkflowDefinition>): Promise<WorkflowDefinition> {
    const payload = {
      name: updates.name,
      description: updates.description,
      type: updates.type,
      triggerType: updates.triggerType,
      isActive: updates.isActive,
      priority: updates.priority,
    }
    const response = await api.put<ApiResponse<WorkflowDefinition>>(`/api/workflow/definitions/${id}`, payload)
    return unwrap(response.data)
  },

  async deleteDefinition(id: string): Promise<void> {
    const response = await api.delete<ApiResponse<unknown>>(`/api/workflow/definitions/${id}`)
    unwrap(response.data)
  },

  async getMetrics(): Promise<WorkflowMetrics | null> {
    const response = await api.get<ApiResponse<WorkflowMetrics[]>>('/api/workflow/metrics')
    const metrics = unwrap(response.data)
    return metrics.length > 0 ? metrics[0] : null
  },

  async getInstances(options: { workflowId?: string; status?: string } = {}): Promise<WorkflowInstance[]> {
    const query = getQueryString({
      workflowId: options.workflowId,
      status: options.status,
    });

    const response = await api.get<ApiResponse<WorkflowInstance[]>>(`/api/workflow/instances${query}`)
    return unwrap(response.data)
  },

  async getInstance(instanceId: string): Promise<WorkflowInstance> {
    const response = await api.get<ApiResponse<WorkflowInstance>>(`/api/workflow/instances/${instanceId}`)
    return unwrap(response.data)
  },

  async startInstance(request: StartWorkflowInstanceRequest): Promise<WorkflowInstance> {
    const response = await api.post<ApiResponse<WorkflowInstance>>('/api/workflow/instances', request)
    return unwrap(response.data)
  },

  async cancelInstance(instanceId: string, reason: string): Promise<void> {
    const payload: CancelWorkflowInstanceRequest = { reason }
    const response = await api.post<ApiResponse<unknown>>(
      `/api/workflow/instances/${instanceId}/cancel`,
      payload,
    )
    unwrap(response.data)
  },

  async approveStep(approvalId: string, comments?: string): Promise<void> {
    const response = await api.post<ApiResponse<unknown>>(
      `/api/workflow/approvals/${approvalId}/approve`,
      { comments },
    )
    unwrap(response.data)
  },

  async rejectStep(approvalId: string, comments: string): Promise<void> {
    const response = await api.post<ApiResponse<unknown>>(
      `/api/workflow/approvals/${approvalId}/reject`,
      { comments },
    )
    unwrap(response.data)
  },

  async getTriggers(workflowId: string): Promise<WorkflowTrigger[]> {
    const response = await api.get<ApiResponse<WorkflowTrigger[]>>(`/api/workflow/${workflowId}/triggers`)
    return unwrap(response.data)
  },

  async createTrigger(workflowId: string, trigger: CreateWorkflowTriggerInput): Promise<WorkflowTrigger> {
    const response = await api.post<ApiResponse<WorkflowTrigger>>(
      `/api/workflow/${workflowId}/triggers`,
      trigger,
    )
    return unwrap(response.data)
  },

  async deleteTrigger(triggerId: string): Promise<void> {
    const response = await api.delete<ApiResponse<unknown>>(`/api/workflow/triggers/${triggerId}`)
    unwrap(response.data)
  },

  async evaluateTriggers(eventType: string, eventData: Record<string, unknown>) {
    const response = await api.post<ApiResponse<string[]>>('/api/workflow/triggers/evaluate', {
      eventType,
      eventData,
    })
    return unwrap(response.data)
  },

  async getPendingApprovals(approverId?: string) {
    const query = approverId ? `?approverId=${encodeURIComponent(approverId)}` : ''
    const response = await api.get<ApiResponse<WorkflowApproval[]>>(
      `/api/workflow/approvals/pending${query}`,
    )
    return unwrap(response.data)
  },

  async getAnalytics(workflowId: string, range?: { from?: string; to?: string }) {
    const query = getQueryString({ from: range?.from, to: range?.to })
    const response = await api.get<ApiResponse<unknown>>(`/api/workflow/${workflowId}/analytics${query}`)
    return unwrap(response.data)
  },
}

export const workflowInstanceStatusLabels: Record<number, string> = {
  [WorkflowInstanceStatus.NotStarted]: 'Not Started',
  [WorkflowInstanceStatus.Running]: 'Running',
  [WorkflowInstanceStatus.WaitingForApproval]: 'Waiting for Approval',
  [WorkflowInstanceStatus.Paused]: 'Paused',
  [WorkflowInstanceStatus.Completed]: 'Completed',
  [WorkflowInstanceStatus.Failed]: 'Failed',
  [WorkflowInstanceStatus.Cancelled]: 'Cancelled',
}

export const workflowApprovalStatusLabels: Record<number, string> = {
  [WorkflowApprovalStatus.Pending]: 'Pending',
  [WorkflowApprovalStatus.Approved]: 'Approved',
  [WorkflowApprovalStatus.Rejected]: 'Rejected',
  [WorkflowApprovalStatus.Cancelled]: 'Cancelled',
}

export const workflowTriggerTypeLabels: Record<number, string> = {
  [WorkflowTriggerType.Manual]: 'Manual',
  [WorkflowTriggerType.Event]: 'Event',
  [WorkflowTriggerType.Schedule]: 'Schedule',
  [WorkflowTriggerType.Webhook]: 'Webhook',
  [WorkflowTriggerType.FileWatch]: 'File Watch',
}

export const workflowTypeLabels: Record<number, string> = {
  [WorkflowType.PaymentApproval]: 'Payment Approval',
  [WorkflowType.DocumentReview]: 'Document Review',
  [WorkflowType.ComplianceCheck]: 'Compliance Check',
  [WorkflowType.Notification]: 'Notification',
  [WorkflowType.TaxFiling]: 'Tax Filing',
  [WorkflowType.ClientOnboarding]: 'Client Onboarding',
  [WorkflowType.Custom]: 'Custom',
}

export type WorkflowService = typeof workflowService
