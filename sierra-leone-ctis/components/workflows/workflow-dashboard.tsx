'use client'

import { useCallback, useEffect, useMemo, useState } from 'react'
import { format } from 'date-fns'
import {
  CreateWorkflowTriggerInput,
  WorkflowApproval,
  WorkflowApprovalStatus,
  workflowApprovalStatusLabels,
  WorkflowDefinition,
  WorkflowInstance,
  WorkflowInstanceStatus,
  workflowInstanceStatusLabels,
  WorkflowMetrics,
  WorkflowTrigger,
  WorkflowTriggerType,
  workflowTriggerTypeLabels,
  WorkflowType,
  workflowTypeLabels,
  workflowService,
} from '@/lib/services/workflow-service'
import { useToast } from '@/components/ui/use-toast'
import { Alert, AlertDescription, AlertTitle } from '@/components/ui/alert'
import { Badge } from '@/components/ui/badge'
import { Button } from '@/components/ui/button'
import { Card, CardContent, CardDescription, CardHeader, CardTitle } from '@/components/ui/card'
import {
  Dialog,
  DialogContent,
  DialogDescription,
  DialogFooter,
  DialogHeader,
  DialogTitle,
} from '@/components/ui/dialog'
import { Input } from '@/components/ui/input'
import { Label } from '@/components/ui/label'
import { Select, SelectContent, SelectItem, SelectTrigger, SelectValue } from '@/components/ui/select'
import { ScrollArea } from '@/components/ui/scroll-area'
import {
  Table,
  TableBody,
  TableCell,
  TableHead,
  TableHeader,
  TableRow,
} from '@/components/ui/table'
import { Tabs, TabsList, TabsTrigger } from '@/components/ui/tabs'
import { Textarea } from '@/components/ui/textarea'
import {
  AlertTriangle,
  Check,
  Loader2,
  PlayCircle,
  Plus,
  RefreshCcw,
  Trash2,
  XCircle,
} from 'lucide-react'

const formatDate = (value?: string | null) => {
  if (!value) return '—'
  try {
    return format(new Date(value), 'MMM d, yyyy HH:mm')
  } catch {
    return value
  }
}

const safeParseJson = (value: string) => {
  const trimmed = value.trim()
  if (!trimmed) {
    return {}
  }
  try {
    return JSON.parse(trimmed)
  } catch {
    throw new Error('Variables must be valid JSON')
  }
}

const coerceObject = (value: unknown): Record<string, unknown> => {
  if (!value) return {}
  if (typeof value === 'string') {
    try {
      return JSON.parse(value)
    } catch {
      return { raw: value }
    }
  }
  if (typeof value === 'object') {
    return value as Record<string, unknown>
  }
  return { value }
}

const jsonPreview = (value: Record<string, unknown>) => JSON.stringify(value, null, 2)

const DEFAULT_VARIABLES_TEMPLATE = `{
  "reference": "WF-${new Date().getFullYear()}",
  "initiatedBy": "user@example.com"
}`

const DEFAULT_TRIGGER_CONFIGURATION = `{
  "eventType": "payment.created",
  "threshold": 50000
}`

type StartDialogState = {
  open: boolean
  definition?: WorkflowDefinition
  variables: string
}

type CancelDialogState = {
  open: boolean
  instance?: WorkflowInstance
  reason: string
}

type ApprovalDialogState = {
  open: boolean
  approval?: WorkflowApproval
  mode: 'approve' | 'reject'
  comment: string
}

type TriggerDialogState = {
  open: boolean
  definition?: WorkflowDefinition
  name: string
  type: WorkflowTriggerType
  configuration: string
}

const deriveMetrics = (
  metrics: WorkflowMetrics | null,
  definitions: WorkflowDefinition[],
  instances: WorkflowInstance[],
  approvals: WorkflowApproval[],
): WorkflowMetrics => {
  if (metrics) return metrics

  const total = instances.length
  const completed = instances.filter((i) => Number(i.status) === WorkflowInstanceStatus.Completed).length
  const running = instances.filter((i) => Number(i.status) === WorkflowInstanceStatus.Running).length

  return {
    totalWorkflows: definitions.length,
    activeWorkflows: definitions.filter((d) => d.isActive).length,
    totalInstances: total,
    runningInstances: running,
  pendingApprovals: approvals.filter((a) => Number(a.status) === WorkflowApprovalStatus.Pending).length,
    overallSuccessRate: total === 0 ? 0 : Math.round((completed / total) * 100),
    lastUpdated: new Date().toISOString(),
  }
}

export default function WorkflowDashboard() {
  const { toast } = useToast()

  const [loading, setLoading] = useState(true)
  const [error, setError] = useState<string | null>(null)
  const [definitions, setDefinitions] = useState<WorkflowDefinition[]>([])
  const [selectedDefinitionId, setSelectedDefinitionId] = useState<string | null>(null)
  const [instances, setInstances] = useState<WorkflowInstance[]>([])
  const [metrics, setMetrics] = useState<WorkflowMetrics | null>(null)
  const [approvals, setApprovals] = useState<WorkflowApproval[]>([])
  const [triggers, setTriggers] = useState<WorkflowTrigger[]>([])
  const [instancesFilter, setInstancesFilter] = useState<'all' | string>('all')
  const [isRefreshing, setIsRefreshing] = useState(false)
  const [actionLoading, setActionLoading] = useState(false)

  const [startDialog, setStartDialog] = useState<StartDialogState>({
    open: false,
    variables: DEFAULT_VARIABLES_TEMPLATE,
  })
  const [cancelDialog, setCancelDialog] = useState<CancelDialogState>({ open: false, reason: '' })
  const [approvalDialog, setApprovalDialog] = useState<ApprovalDialogState>({
    open: false,
    mode: 'approve',
    comment: '',
  })
  const [triggerDialog, setTriggerDialog] = useState<TriggerDialogState>({
    open: false,
    name: '',
    type: WorkflowTriggerType.Event,
    configuration: DEFAULT_TRIGGER_CONFIGURATION,
  })

  const loadCoreData = useCallback(async () => {
    try {
      setError(null)
      const [defs, insts, pendingApprovals, metricSet] = await Promise.all([
        workflowService.getDefinitions(),
        workflowService.getInstances(),
        workflowService.getPendingApprovals(),
        workflowService.getMetrics(),
      ])

      setDefinitions(defs)
      setInstances(insts)
      setApprovals(pendingApprovals)
      setMetrics(metricSet)

      if (defs.length > 0) {
        const currentId = selectedDefinitionId && defs.some((d) => d.id === selectedDefinitionId)
          ? selectedDefinitionId
          : defs[0].id
        setSelectedDefinitionId(currentId)
      } else {
        setSelectedDefinitionId(null)
        setTriggers([])
      }
    } catch (err) {
      setError(err instanceof Error ? err.message : 'Failed to load workflow data')
    }
  }, [selectedDefinitionId])

  const loadTriggers = useCallback(async (workflowId: string) => {
    try {
      const triggerList = await workflowService.getTriggers(workflowId)
      setTriggers(triggerList)
    } catch (err) {
      toast({
        title: 'Trigger load failed',
        description: err instanceof Error ? err.message : 'Unable to fetch workflow triggers',
        variant: 'destructive',
      })
    }
  }, [toast])

  const refreshAll = useCallback(async () => {
    setIsRefreshing(true)
    await loadCoreData()
    if (selectedDefinitionId) {
      await loadTriggers(selectedDefinitionId)
    }
    setIsRefreshing(false)
  }, [loadCoreData, loadTriggers, selectedDefinitionId])

  useEffect(() => {
    ;(async () => {
      setLoading(true)
      await loadCoreData()
      setLoading(false)
    })()
  }, [loadCoreData])

  useEffect(() => {
    if (selectedDefinitionId) {
      loadTriggers(selectedDefinitionId)
    }
  }, [selectedDefinitionId, loadTriggers])

  const computedMetrics = useMemo(
    () => deriveMetrics(metrics, definitions, instances, approvals),
    [metrics, definitions, instances, approvals],
  )

  const filteredInstances = useMemo(() => {
    if (instancesFilter === 'all') return instances
    return instances.filter((instance) => String(instance.status) === String(instancesFilter))
  }, [instances, instancesFilter])

  const activeDefinition = definitions.find((def) => def.id === selectedDefinitionId) ?? null

  const handleOpenStartDialog = (definition: WorkflowDefinition) => {
    setStartDialog({ open: true, definition, variables: DEFAULT_VARIABLES_TEMPLATE })
  }

  const handleStartInstance = async () => {
    if (!startDialog.definition) return

    try {
      setActionLoading(true)
      const variables = safeParseJson(startDialog.variables)
      await workflowService.startInstance({
        workflowId: startDialog.definition.id,
        variables,
      })
      toast({ title: 'Workflow started', description: `${startDialog.definition.name} launched successfully` })
      setStartDialog({ open: false, definition: undefined, variables: DEFAULT_VARIABLES_TEMPLATE })
      await refreshAll()
    } catch (err) {
      toast({
        title: 'Failed to start workflow',
        description: err instanceof Error ? err.message : 'Unable to start workflow instance',
        variant: 'destructive',
      })
    } finally {
      setActionLoading(false)
    }
  }

  const handleOpenCancelDialog = (instance: WorkflowInstance) => {
    setCancelDialog({ open: true, instance, reason: '' })
  }

  const handleCancelInstance = async () => {
    if (!cancelDialog.instance) return
    if (!cancelDialog.reason.trim()) {
      toast({ title: 'Reason required', description: 'Please provide a cancellation reason', variant: 'destructive' })
      return
    }

    try {
      setActionLoading(true)
      await workflowService.cancelInstance(cancelDialog.instance.id, cancelDialog.reason.trim())
      toast({ title: 'Workflow cancelled', description: `${cancelDialog.instance.name} has been cancelled` })
      setCancelDialog({ open: false, instance: undefined, reason: '' })
      await refreshAll()
    } catch (err) {
      toast({
        title: 'Failed to cancel workflow',
        description: err instanceof Error ? err.message : 'Unable to cancel workflow instance',
        variant: 'destructive',
      })
    } finally {
      setActionLoading(false)
    }
  }

  const handleOpenApprovalDialog = (approval: WorkflowApproval, mode: 'approve' | 'reject') => {
    setApprovalDialog({ open: true, approval, mode, comment: '' })
  }

  const handleApprovalAction = async () => {
    if (!approvalDialog.approval) return

    if (approvalDialog.mode === 'reject' && !approvalDialog.comment.trim()) {
      toast({ title: 'Comment required', description: 'Please provide a reason for rejection', variant: 'destructive' })
      return
    }

    try {
      setActionLoading(true)
      if (approvalDialog.mode === 'approve') {
        await workflowService.approveStep(approvalDialog.approval.id, approvalDialog.comment.trim() || undefined)
        toast({ title: 'Step approved', description: 'Approval recorded successfully' })
      } else {
        await workflowService.rejectStep(approvalDialog.approval.id, approvalDialog.comment.trim())
        toast({ title: 'Step rejected', description: 'Rejection recorded successfully' })
      }

      setApprovalDialog({ open: false, approval: undefined, mode: 'approve', comment: '' })
      await refreshAll()
    } catch (err) {
      toast({
        title: 'Approval action failed',
        description: err instanceof Error ? err.message : 'Unable to process approval action',
        variant: 'destructive',
      })
    } finally {
      setActionLoading(false)
    }
  }

  const handleOpenTriggerDialog = (definition: WorkflowDefinition) => {
    setTriggerDialog({
      open: true,
      definition,
      name: '',
      type: WorkflowTriggerType.Event,
      configuration: DEFAULT_TRIGGER_CONFIGURATION,
    })
  }

  const handleCreateTrigger = async () => {
    if (!triggerDialog.definition) return

    if (!triggerDialog.name.trim()) {
      toast({ title: 'Name required', description: 'Please provide a trigger name', variant: 'destructive' })
      return
    }

    try {
      setActionLoading(true)
      const configuration = safeParseJson(triggerDialog.configuration) as CreateWorkflowTriggerInput['configuration']

      await workflowService.createTrigger(triggerDialog.definition.id, {
        name: triggerDialog.name.trim(),
        type: triggerDialog.type,
        configuration,
      })

      toast({ title: 'Trigger created', description: `${triggerDialog.name.trim()} added successfully` })
      setTriggerDialog({
        open: false,
        definition: undefined,
        name: '',
        type: WorkflowTriggerType.Event,
        configuration: DEFAULT_TRIGGER_CONFIGURATION,
      })
      if (selectedDefinitionId) {
        await loadTriggers(selectedDefinitionId)
      }
    } catch (err) {
      toast({
        title: 'Failed to create trigger',
        description: err instanceof Error ? err.message : 'Unable to save workflow trigger',
        variant: 'destructive',
      })
    } finally {
      setActionLoading(false)
    }
  }

  const handleDeleteTrigger = async (trigger: WorkflowTrigger) => {
    try {
      setActionLoading(true)
      await workflowService.deleteTrigger(trigger.id)
      toast({ title: 'Trigger deleted', description: `${trigger.name} removed successfully` })
      if (selectedDefinitionId) {
        await loadTriggers(selectedDefinitionId)
      }
    } catch (err) {
      toast({
        title: 'Failed to delete trigger',
        description: err instanceof Error ? err.message : 'Unable to delete workflow trigger',
        variant: 'destructive',
      })
    } finally {
      setActionLoading(false)
    }
  }

  if (loading) {
    return (
      <div className="flex h-full items-center justify-center p-16">
        <div className="flex flex-col items-center gap-4 text-muted-foreground">
          <Loader2 className="h-8 w-8 animate-spin" />
          <p className="text-sm">Loading workflow dashboard…</p>
        </div>
      </div>
    )
  }

  return (
    <div className="mx-auto flex w-full max-w-7xl flex-col gap-8 p-6">
      <div className="flex flex-col gap-4 lg:flex-row lg:items-center lg:justify-between">
        <div>
          <h1 className="text-3xl font-semibold tracking-tight">Workflow Automation Console</h1>
          <p className="text-muted-foreground">
            Monitor workflow health, manage triggers, and action pending approvals in one place.
          </p>
        </div>
        <div className="flex gap-2">
          <Button variant="outline" onClick={refreshAll} disabled={isRefreshing}>
            <RefreshCcw className={`mr-2 h-4 w-4 ${isRefreshing ? 'animate-spin' : ''}`} />
            Refresh
          </Button>
          {activeDefinition && (
            <Button onClick={() => handleOpenStartDialog(activeDefinition)}>
              <PlayCircle className="mr-2 h-4 w-4" />
              Start Instance
            </Button>
          )}
        </div>
      </div>

      {error && (
        <Alert variant="destructive">
          <AlertTriangle className="h-4 w-4" />
          <AlertTitle>Unable to load workflows</AlertTitle>
          <AlertDescription>{error}</AlertDescription>
        </Alert>
      )}

      <div className="grid gap-4 md:grid-cols-2 xl:grid-cols-4">
        <Card>
          <CardHeader className="pb-2">
            <CardDescription>Total Workflows</CardDescription>
            <CardTitle className="text-3xl">{computedMetrics.totalWorkflows}</CardTitle>
          </CardHeader>
          <CardContent className="text-sm text-muted-foreground">
            {computedMetrics.activeWorkflows} active definitions
          </CardContent>
        </Card>
        <Card>
          <CardHeader className="pb-2">
            <CardDescription>Active Instances</CardDescription>
            <CardTitle className="text-3xl">{computedMetrics.runningInstances}</CardTitle>
          </CardHeader>
          <CardContent className="text-sm text-muted-foreground">
            {computedMetrics.totalInstances} total instances tracked
          </CardContent>
        </Card>
        <Card>
          <CardHeader className="pb-2">
            <CardDescription>Pending Approvals</CardDescription>
            <CardTitle className="text-3xl">{computedMetrics.pendingApprovals}</CardTitle>
          </CardHeader>
          <CardContent className="text-sm text-muted-foreground">
            Awaiting your review or delegation
          </CardContent>
        </Card>
        <Card>
          <CardHeader className="pb-2">
            <CardDescription>Overall Success</CardDescription>
            <CardTitle className="text-3xl">{computedMetrics.overallSuccessRate}%</CardTitle>
          </CardHeader>
          <CardContent className="text-sm text-muted-foreground">
            Last updated {formatDate(computedMetrics.lastUpdated)}
          </CardContent>
        </Card>
      </div>

      <div className="grid gap-6 lg:grid-cols-3">
        <Card className="lg:col-span-2">
          <CardHeader>
            <CardTitle>Workflow Definitions</CardTitle>
            <CardDescription>Select a workflow to review triggers and start new instances.</CardDescription>
          </CardHeader>
          <CardContent>
            <ScrollArea className="h-[320px] pr-4">
              <div className="space-y-4">
                {definitions.map((definition) => {
                  const isSelected = definition.id === selectedDefinitionId
                  const typeLabel = workflowTypeLabels[Number(definition.type)] ?? definition.type
                  return (
                    <button
                      key={definition.id}
                      className={`w-full rounded-lg border p-4 text-left transition hover:border-primary hover:shadow ${
                        isSelected ? 'border-primary shadow-sm' : 'border-border'
                      }`}
                      onClick={() => setSelectedDefinitionId(definition.id)}
                    >
                      <div className="flex items-start justify-between gap-3">
                        <div>
                          <div className="flex items-center gap-2">
                            <h3 className="font-semibold">{definition.name}</h3>
                            <Badge variant={definition.isActive ? 'default' : 'secondary'}>
                              {definition.isActive ? 'Active' : 'Inactive'}
                            </Badge>
                          </div>
                          <p className="mt-1 text-sm text-muted-foreground">{definition.description}</p>
                        </div>
                        <div className="text-right text-xs text-muted-foreground">
                          <div>{typeLabel}</div>
                          <div>Priority {definition.priority}</div>
                        </div>
                      </div>
                      <div className="mt-3 flex gap-2">
                        <Button
                          size="sm"
                          onClick={(event) => {
                            event.stopPropagation()
                            handleOpenStartDialog(definition)
                          }}
                        >
                          <PlayCircle className="mr-2 h-4 w-4" /> Start Instance
                        </Button>
                        <Button
                          size="sm"
                          variant="outline"
                          onClick={(event) => {
                            event.stopPropagation()
                            setSelectedDefinitionId(definition.id)
                          }}
                        >
                          View Triggers
                        </Button>
                      </div>
                    </button>
                  )
                })}
                {definitions.length === 0 && (
                  <div className="rounded border border-dashed p-8 text-center text-sm text-muted-foreground">
                    No workflow definitions available yet.
                  </div>
                )}
              </div>
            </ScrollArea>
          </CardContent>
        </Card>

        <Card>
          <CardHeader>
            <CardTitle>Pending Approvals</CardTitle>
            <CardDescription>Review workflow steps awaiting your decision.</CardDescription>
          </CardHeader>
          <CardContent>
            <ScrollArea className="h-[320px] pr-4">
              <div className="space-y-4">
                {approvals.length === 0 && (
                  <div className="rounded border border-dashed p-6 text-center text-sm text-muted-foreground">
                    No approvals require your attention right now.
                  </div>
                )}
                {approvals.map((approval) => {
                  const statusLabel = workflowApprovalStatusLabels[Number(approval.status)] ?? approval.status
                  return (
                    <div key={approval.id} className="rounded-lg border p-4">
                      <div className="flex items-center justify-between">
                        <div>
                          <h3 className="font-medium">Approval Request</h3>
                          <p className="text-xs text-muted-foreground">
                            Requested {formatDate(approval.requestedAt)}
                          </p>
                        </div>
                        <Badge>{statusLabel}</Badge>
                      </div>
                      {approval.comments && (
                        <p className="mt-2 text-sm text-muted-foreground">{approval.comments}</p>
                      )}
                      <div className="mt-3 flex gap-2">
                        <Button size="sm" onClick={() => handleOpenApprovalDialog(approval, 'approve')}>
                          <Check className="mr-2 h-4 w-4" /> Approve
                        </Button>
                        <Button
                          size="sm"
                          variant="outline"
                          onClick={() => handleOpenApprovalDialog(approval, 'reject')}
                        >
                          <XCircle className="mr-2 h-4 w-4" /> Reject
                        </Button>
                      </div>
                    </div>
                  )
                })}
              </div>
            </ScrollArea>
          </CardContent>
        </Card>
      </div>

      <Card>
        <CardHeader className="flex flex-col gap-2 lg:flex-row lg:items-center lg:justify-between">
          <div>
            <CardTitle>Workflow Instances</CardTitle>
            <CardDescription>Track active and historical workflow executions.</CardDescription>
          </div>
          <Tabs value={instancesFilter} onValueChange={(value) => setInstancesFilter(value)}>
            <TabsList>
              <TabsTrigger value="all">All</TabsTrigger>
              <TabsTrigger value={String(WorkflowInstanceStatus.Running)}>Running</TabsTrigger>
              <TabsTrigger value={String(WorkflowInstanceStatus.WaitingForApproval)}>Waiting Approval</TabsTrigger>
              <TabsTrigger value={String(WorkflowInstanceStatus.Completed)}>Completed</TabsTrigger>
            </TabsList>
          </Tabs>
        </CardHeader>
        <CardContent className="pt-0">
          <div className="overflow-x-auto">
            <Table>
              <TableHeader>
                <TableRow>
                  <TableHead>Name</TableHead>
                  <TableHead>Status</TableHead>
                  <TableHead>Started</TableHead>
                  <TableHead>Last Update</TableHead>
                  <TableHead>Actions</TableHead>
                </TableRow>
              </TableHeader>
              <TableBody>
                {filteredInstances.map((instance) => {
                  const statusLabel = workflowInstanceStatusLabels[Number(instance.status)] ?? instance.status
                  const variables = coerceObject(instance.variables)
                  const variablesPreview = jsonPreview(variables)
                  return (
                    <TableRow key={instance.id}>
                      <TableCell>
                        <div className="font-medium">{instance.name}</div>
                        <p className="text-xs text-muted-foreground">
                          {Object.keys(variables).length > 0
                            ? `${variablesPreview.slice(0, 80)}${variablesPreview.length > 80 ? '…' : ''}`
                            : 'No variables'}
                        </p>
                      </TableCell>
                      <TableCell>
                        <Badge variant="outline">{statusLabel}</Badge>
                      </TableCell>
                      <TableCell className="text-sm text-muted-foreground">{formatDate(instance.startedAt)}</TableCell>
                      <TableCell className="text-sm text-muted-foreground">
                        {formatDate(instance.completedAt ?? instance.createdAt)}
                      </TableCell>
                      <TableCell>
                        <div className="flex gap-2">
                          <Button size="sm" variant="ghost" onClick={() => handleOpenCancelDialog(instance)}>
                            Cancel
                          </Button>
                        </div>
                      </TableCell>
                    </TableRow>
                  )
                })}
                {filteredInstances.length === 0 && (
                  <TableRow>
                    <TableCell colSpan={5} className="py-12 text-center text-sm text-muted-foreground">
                      No workflow instances match this filter.
                    </TableCell>
                  </TableRow>
                )}
              </TableBody>
            </Table>
          </div>
        </CardContent>
      </Card>

      <Card>
        <CardHeader className="flex flex-col gap-2 lg:flex-row lg:items-center lg:justify-between">
          <div>
            <CardTitle>Workflow Triggers</CardTitle>
            <CardDescription>Automation rules linked to the selected workflow definition.</CardDescription>
          </div>
          {activeDefinition && (
            <Button variant="outline" onClick={() => handleOpenTriggerDialog(activeDefinition)}>
              <Plus className="mr-2 h-4 w-4" /> Add Trigger
            </Button>
          )}
        </CardHeader>
        <CardContent className="pt-0">
          <div className="overflow-x-auto">
            <Table>
              <TableHeader>
                <TableRow>
                  <TableHead>Name</TableHead>
                  <TableHead>Type</TableHead>
                  <TableHead>Configuration</TableHead>
                  <TableHead>Created</TableHead>
                  <TableHead></TableHead>
                </TableRow>
              </TableHeader>
              <TableBody>
                {triggers.map((trigger) => {
                  const typeLabel = workflowTriggerTypeLabels[Number(trigger.type)] ?? trigger.type
                  const config = coerceObject(trigger.configuration)
                  return (
                    <TableRow key={trigger.id}>
                      <TableCell>
                        <div className="font-medium">{trigger.name}</div>
                        <p className="text-xs text-muted-foreground">By {trigger.createdBy}</p>
                      </TableCell>
                      <TableCell>
                        <Badge variant="secondary">{typeLabel}</Badge>
                      </TableCell>
                      <TableCell className="text-xs text-muted-foreground">
                        <pre className="max-w-xs whitespace-pre-wrap break-words text-left">
                          {jsonPreview(config)}
                        </pre>
                      </TableCell>
                      <TableCell className="text-xs text-muted-foreground">{formatDate(trigger.createdAt)}</TableCell>
                      <TableCell className="text-right">
                        <Button
                          size="sm"
                          variant="ghost"
                          className="text-destructive hover:text-destructive"
                          onClick={() => handleDeleteTrigger(trigger)}
                        >
                          <Trash2 className="h-4 w-4" />
                        </Button>
                      </TableCell>
                    </TableRow>
                  )
                })}
                {triggers.length === 0 && (
                  <TableRow>
                    <TableCell colSpan={5} className="py-12 text-center text-sm text-muted-foreground">
                      No triggers configured for this workflow yet.
                    </TableCell>
                  </TableRow>
                )}
              </TableBody>
            </Table>
          </div>
        </CardContent>
      </Card>

      <Dialog open={startDialog.open} onOpenChange={(open) => setStartDialog((state) => ({ ...state, open }))}>
        <DialogContent className="sm:max-w-lg">
          <DialogHeader>
            <DialogTitle>Start Workflow Instance</DialogTitle>
            <DialogDescription>
              Provide runtime variables to launch {startDialog.definition?.name ?? 'the selected workflow'}.
            </DialogDescription>
          </DialogHeader>
          <div className="space-y-4">
            <div className="space-y-2">
              <Label htmlFor="variables">Variables (JSON)</Label>
              <Textarea
                id="variables"
                rows={8}
                value={startDialog.variables}
                onChange={(event) => setStartDialog((state) => ({ ...state, variables: event.target.value }))}
              />
            </div>
          </div>
          <DialogFooter>
            <Button variant="outline" onClick={() => setStartDialog({ open: false, variables: DEFAULT_VARIABLES_TEMPLATE })}>
              Cancel
            </Button>
            <Button onClick={handleStartInstance} disabled={actionLoading}>
              {actionLoading && <Loader2 className="mr-2 h-4 w-4 animate-spin" />}
              Launch
            </Button>
          </DialogFooter>
        </DialogContent>
      </Dialog>

      <Dialog open={cancelDialog.open} onOpenChange={(open) => setCancelDialog((state) => ({ ...state, open }))}>
        <DialogContent className="sm:max-w-lg">
          <DialogHeader>
            <DialogTitle>Cancel Workflow Instance</DialogTitle>
            <DialogDescription>
              Provide a reason to cancel {cancelDialog.instance?.name ?? 'this instance'}.
            </DialogDescription>
          </DialogHeader>
          <div className="space-y-4">
            <div className="space-y-2">
              <Label htmlFor="cancel-reason">Reason</Label>
              <Textarea
                id="cancel-reason"
                rows={4}
                value={cancelDialog.reason}
                onChange={(event) => setCancelDialog((state) => ({ ...state, reason: event.target.value }))}
              />
            </div>
          </div>
          <DialogFooter>
            <Button variant="outline" onClick={() => setCancelDialog({ open: false, reason: '' })}>
              Keep Running
            </Button>
            <Button variant="destructive" onClick={handleCancelInstance} disabled={actionLoading}>
              {actionLoading && <Loader2 className="mr-2 h-4 w-4 animate-spin" />}
              Cancel Instance
            </Button>
          </DialogFooter>
        </DialogContent>
      </Dialog>

      <Dialog open={approvalDialog.open} onOpenChange={(open) => setApprovalDialog((state) => ({ ...state, open }))}>
        <DialogContent className="sm:max-w-lg">
          <DialogHeader>
            <DialogTitle>
              {approvalDialog.mode === 'approve' ? 'Approve Workflow Step' : 'Reject Workflow Step'}
            </DialogTitle>
            <DialogDescription>
              {approvalDialog.mode === 'approve'
                ? 'Optionally provide a comment before approving this workflow step.'
                : 'Provide a reason for rejecting this workflow step.'}
            </DialogDescription>
          </DialogHeader>
          <div className="space-y-4">
            <div className="space-y-2">
              <Label htmlFor="approval-comment">Comment</Label>
              <Textarea
                id="approval-comment"
                rows={4}
                placeholder={approvalDialog.mode === 'approve' ? 'Optional comment' : 'Required comment'}
                value={approvalDialog.comment}
                onChange={(event) => setApprovalDialog((state) => ({ ...state, comment: event.target.value }))}
              />
            </div>
          </div>
          <DialogFooter>
            <Button variant="outline" onClick={() => setApprovalDialog({ open: false, mode: 'approve', comment: '' })}>
              Cancel
            </Button>
            <Button
              onClick={handleApprovalAction}
              variant={approvalDialog.mode === 'approve' ? 'default' : 'destructive'}
              disabled={actionLoading}
            >
              {actionLoading && <Loader2 className="mr-2 h-4 w-4 animate-spin" />}
              {approvalDialog.mode === 'approve' ? 'Approve Step' : 'Reject Step'}
            </Button>
          </DialogFooter>
        </DialogContent>
      </Dialog>

      <Dialog open={triggerDialog.open} onOpenChange={(open) => setTriggerDialog((state) => ({ ...state, open }))}>
        <DialogContent className="sm:max-w-xl">
          <DialogHeader>
            <DialogTitle>Create Workflow Trigger</DialogTitle>
            <DialogDescription>
              Configure automation for {triggerDialog.definition?.name ?? 'the selected workflow'}.
            </DialogDescription>
          </DialogHeader>
          <div className="space-y-4">
            <div className="space-y-2">
              <Label htmlFor="trigger-name">Trigger Name</Label>
              <Input
                id="trigger-name"
                value={triggerDialog.name}
                onChange={(event) => setTriggerDialog((state) => ({ ...state, name: event.target.value }))}
              />
            </div>
            <div className="space-y-2">
              <Label>Trigger Type</Label>
              <Select
                value={String(triggerDialog.type)}
                onValueChange={(value) =>
                  setTriggerDialog((state) => ({ ...state, type: Number(value) as WorkflowTriggerType }))
                }
              >
                <SelectTrigger>
                  <SelectValue placeholder="Choose trigger type" />
                </SelectTrigger>
                <SelectContent>
                  {Object.entries(workflowTriggerTypeLabels).map(([key, label]) => (
                    <SelectItem key={key} value={key}>
                      {label}
                    </SelectItem>
                  ))}
                </SelectContent>
              </Select>
            </div>
            <div className="space-y-2">
              <Label htmlFor="trigger-config">Configuration (JSON)</Label>
              <Textarea
                id="trigger-config"
                rows={6}
                value={triggerDialog.configuration}
                onChange={(event) => setTriggerDialog((state) => ({ ...state, configuration: event.target.value }))}
              />
            </div>
          </div>
          <DialogFooter>
            <Button variant="outline" onClick={() => setTriggerDialog({
              open: false,
              definition: undefined,
              name: '',
              type: WorkflowTriggerType.Event,
              configuration: DEFAULT_TRIGGER_CONFIGURATION,
            })}>
              Cancel
            </Button>
            <Button onClick={handleCreateTrigger} disabled={actionLoading}>
              {actionLoading && <Loader2 className="mr-2 h-4 w-4 animate-spin" />}
              Save Trigger
            </Button>
          </DialogFooter>
        </DialogContent>
      </Dialog>
    </div>
  )
}
