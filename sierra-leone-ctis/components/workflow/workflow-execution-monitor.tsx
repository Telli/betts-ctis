import React, { useCallback, useEffect, useMemo, useState } from 'react';
import { Card, CardContent, CardDescription, CardHeader, CardTitle } from '@/components/ui/card';
import { Button } from '@/components/ui/button';
import { Badge } from '@/components/ui/badge';
import { Select, SelectContent, SelectItem, SelectTrigger, SelectValue } from '@/components/ui/select';
import { Dialog, DialogContent, DialogDescription, DialogHeader, DialogTitle } from '@/components/ui/dialog';
import { ScrollArea } from '@/components/ui/scroll-area';
import { Separator } from '@/components/ui/separator';
import { RefreshCcw, Activity, CheckCircle, AlertTriangle, Hourglass, Info } from 'lucide-react';
import { toast } from 'sonner';
import {
  WorkflowApprovalStatus,
  WorkflowDefinition,
  WorkflowInstance,
  WorkflowInstanceStatus,
  WorkflowStepInstanceStatus,
  workflowService
} from '@/lib/services/workflow-service';

interface WorkflowExecutionMonitorProps {
  definitions: WorkflowDefinition[];
  onRefresh?: () => Promise<void> | void;
}

const instanceStatusOptions = [
  { value: 'all', label: 'All statuses' },
  { value: 'Running', label: 'Running' },
  { value: 'WaitingForApproval', label: 'Waiting for approval' },
  { value: 'Completed', label: 'Completed' },
  { value: 'Failed', label: 'Failed' },
  { value: 'Cancelled', label: 'Cancelled' }
] as const;

type InstanceStatusFilter = typeof instanceStatusOptions[number]['value'];

const formatInstanceStatus = (status: WorkflowInstanceStatus | number | string): string => {
  const numeric = typeof status === 'number' ? status : WorkflowInstanceStatus[status as keyof typeof WorkflowInstanceStatus];

  switch (numeric) {
    case WorkflowInstanceStatus.NotStarted:
      return 'Not started';
    case WorkflowInstanceStatus.Running:
      return 'Running';
    case WorkflowInstanceStatus.WaitingForApproval:
      return 'Waiting for approval';
    case WorkflowInstanceStatus.Paused:
      return 'Paused';
    case WorkflowInstanceStatus.Completed:
      return 'Completed';
    case WorkflowInstanceStatus.Failed:
      return 'Failed';
    case WorkflowInstanceStatus.Cancelled:
      return 'Cancelled';
    default:
      return 'Unknown';
  }
};

const badgeVariantForInstance = (status: WorkflowInstanceStatus | number | string): 'default' | 'secondary' | 'destructive' | 'outline' => {
  const numeric = typeof status === 'number' ? status : WorkflowInstanceStatus[status as keyof typeof WorkflowInstanceStatus];

  if (numeric === WorkflowInstanceStatus.Running) return 'secondary';
  if (numeric === WorkflowInstanceStatus.WaitingForApproval) return 'outline';
  if (numeric === WorkflowInstanceStatus.Completed) return 'default';
  if (numeric === WorkflowInstanceStatus.Failed || numeric === WorkflowInstanceStatus.Cancelled) return 'destructive';
  return 'outline';
};

const formatStepStatus = (status: WorkflowStepInstanceStatus | number | string): string => {
  const numeric = typeof status === 'number' ? status : WorkflowStepInstanceStatus[status as keyof typeof WorkflowStepInstanceStatus];

  switch (numeric) {
    case WorkflowStepInstanceStatus.NotStarted:
      return 'Not started';
    case WorkflowStepInstanceStatus.Running:
      return 'Running';
    case WorkflowStepInstanceStatus.WaitingForApproval:
      return 'Waiting for approval';
    case WorkflowStepInstanceStatus.Completed:
      return 'Completed';
    case WorkflowStepInstanceStatus.Failed:
      return 'Failed';
    case WorkflowStepInstanceStatus.Skipped:
      return 'Skipped';
    case WorkflowStepInstanceStatus.Cancelled:
      return 'Cancelled';
    default:
      return 'Unknown';
  }
};

const badgeVariantForStep = (status: WorkflowStepInstanceStatus | number | string): 'default' | 'secondary' | 'destructive' | 'outline' => {
  const numeric = typeof status === 'number' ? status : WorkflowStepInstanceStatus[status as keyof typeof WorkflowStepInstanceStatus];

  if (numeric === WorkflowStepInstanceStatus.Running || numeric === WorkflowStepInstanceStatus.WaitingForApproval) {
    return 'secondary';
  }
  if (numeric === WorkflowStepInstanceStatus.Completed) {
    return 'default';
  }
  if (numeric === WorkflowStepInstanceStatus.Failed || numeric === WorkflowStepInstanceStatus.Cancelled) {
    return 'destructive';
  }
  return 'outline';
};

const formatApprovalStatus = (status: WorkflowApprovalStatus | number | string): string => {
  const numeric = typeof status === 'number' ? status : WorkflowApprovalStatus[status as keyof typeof WorkflowApprovalStatus];

  switch (numeric) {
    case WorkflowApprovalStatus.Pending:
      return 'Pending';
    case WorkflowApprovalStatus.Approved:
      return 'Approved';
    case WorkflowApprovalStatus.Rejected:
      return 'Rejected';
    case WorkflowApprovalStatus.Cancelled:
      return 'Cancelled';
    default:
      return 'Unknown';
  }
};

const badgeVariantForApproval = (status: WorkflowApprovalStatus | number | string): 'default' | 'secondary' | 'destructive' | 'outline' => {
  const numeric = typeof status === 'number' ? status : WorkflowApprovalStatus[status as keyof typeof WorkflowApprovalStatus];

  if (numeric === WorkflowApprovalStatus.Pending) return 'outline';
  if (numeric === WorkflowApprovalStatus.Approved) return 'default';
  if (numeric === WorkflowApprovalStatus.Rejected) return 'destructive';
  return 'secondary';
};

const calculateProgress = (instance: WorkflowInstance) => {
  const steps = instance.stepInstances ?? [];
  if (steps.length === 0) {
    return instance.status === WorkflowInstanceStatus.Completed ? 100 : 0;
  }

  const completedSteps = steps.filter((step) => {
    const numeric = typeof step.status === 'number' ? step.status : WorkflowStepInstanceStatus[step.status as keyof typeof WorkflowStepInstanceStatus];
    return numeric === WorkflowStepInstanceStatus.Completed;
  }).length;

  return Math.round((completedSteps / steps.length) * 100);
};

const findCurrentStep = (instance: WorkflowInstance) => {
  const running = instance.stepInstances?.find((step) => {
    const numeric = typeof step.status === 'number' ? step.status : WorkflowStepInstanceStatus[step.status as keyof typeof WorkflowStepInstanceStatus];
    return numeric === WorkflowStepInstanceStatus.Running || numeric === WorkflowStepInstanceStatus.WaitingForApproval;
  });

  if (!running) {
    return instance.stepInstances?.[instance.stepInstances.length - 1]?.workflowStepId ?? null;
  }

  return running.workflowStepId;
};

export function WorkflowExecutionMonitor({ definitions, onRefresh }: WorkflowExecutionMonitorProps) {
  const [instances, setInstances] = useState<WorkflowInstance[]>([]);
  const [isLoading, setIsLoading] = useState(true);
  const [statusFilter, setStatusFilter] = useState<InstanceStatusFilter>('all');
  const [selectedInstance, setSelectedInstance] = useState<WorkflowInstance | null>(null);
  const [isDetailsOpen, setIsDetailsOpen] = useState(false);

  const definitionLookup = useMemo(() => {
    const map = new Map<string, WorkflowDefinition>();
    definitions.forEach((definition) => map.set(definition.id, definition));
    return map;
  }, [definitions]);

  const loadInstances = useCallback(async () => {
    try {
      setIsLoading(true);
      const data = await workflowService.getInstances(
        statusFilter === 'all' ? {} : { status: statusFilter }
      );
      setInstances(data);
    } catch (error) {
      console.error('Failed to load workflow instances', error);
      toast.error('Unable to load workflow executions');
    } finally {
      setIsLoading(false);
    }
  }, [statusFilter]);

  useEffect(() => {
    loadInstances();
    const interval = setInterval(loadInstances, 15000);
    return () => clearInterval(interval);
  }, [loadInstances]);

  const handleRefresh = async () => {
    await loadInstances();
    await onRefresh?.();
  };

  const handleOpenDetails = (instance: WorkflowInstance) => {
    setSelectedInstance(instance);
    setIsDetailsOpen(true);
  };

  const handleCancelInstance = async (instance: WorkflowInstance) => {
    const reason = window.prompt('Provide a reason for cancelling this workflow instance:', 'Manual cancellation from dashboard');
    if (!reason) {
      return;
    }

    try {
      await workflowService.cancelInstance(instance.id, reason);
      toast.success('Workflow instance cancelled');
      await handleRefresh();
    } catch (error) {
      console.error('Failed to cancel workflow instance', error);
      toast.error('Unable to cancel workflow instance');
    }
  };

  const summary = useMemo(() => {
    const total = instances.length;
    const running = instances.filter((instance) => instance.status === WorkflowInstanceStatus.Running).length;
    const waiting = instances.filter((instance) => instance.status === WorkflowInstanceStatus.WaitingForApproval).length;
    const failed = instances.filter((instance) => instance.status === WorkflowInstanceStatus.Failed).length;
    const completed = instances.filter((instance) => instance.status === WorkflowInstanceStatus.Completed).length;

    return { total, running, waiting, failed, completed };
  }, [instances]);

  return (
    <div className="space-y-6">
      <div className="flex flex-col gap-4 md:flex-row md:items-center md:justify-between">
        <div>
          <h3 className="text-lg font-semibold">Workflow Execution Monitor</h3>
          <p className="text-sm text-muted-foreground">
            Track recent executions, approvals, and automation outcomes in real time.
          </p>
        </div>
        <div className="flex flex-col gap-2 sm:flex-row">
          <Select value={statusFilter} onValueChange={(value) => setStatusFilter(value as InstanceStatusFilter)}>
            <SelectTrigger className="w-56">
              <SelectValue />
            </SelectTrigger>
            <SelectContent>
              {instanceStatusOptions.map((option) => (
                <SelectItem key={option.value} value={option.value}>
                  {option.label}
                </SelectItem>
              ))}
            </SelectContent>
          </Select>
          <Button onClick={handleRefresh} variant="outline">
            <RefreshCcw className="mr-2 h-4 w-4" /> Refresh
          </Button>
        </div>
      </div>

  <div className="grid gap-4 md:grid-cols-2 xl:grid-cols-5">
        <Card>
          <CardContent className="p-4">
            <div className="flex items-center justify-between">
              <div>
                <p className="text-sm text-muted-foreground">Total instances</p>
                <p className="text-2xl font-bold">{summary.total}</p>
              </div>
              <Info className="h-8 w-8 text-muted-foreground" />
            </div>
          </CardContent>
        </Card>
        <Card>
          <CardContent className="p-4">
            <div className="flex items-center justify-between">
              <div>
                <p className="text-sm text-muted-foreground">Running</p>
                <p className="text-2xl font-bold text-blue-600">{summary.running}</p>
              </div>
              <Activity className="h-8 w-8 text-blue-500" />
            </div>
          </CardContent>
        </Card>
        <Card>
          <CardContent className="p-4">
            <div className="flex items-center justify-between">
              <div>
                <p className="text-sm text-muted-foreground">Awaiting approval</p>
                <p className="text-2xl font-bold text-amber-600">{summary.waiting}</p>
              </div>
              <Hourglass className="h-8 w-8 text-amber-500" />
            </div>
          </CardContent>
        </Card>
        <Card>
          <CardContent className="p-4">
            <div className="flex items-center justify-between">
              <div>
                <p className="text-sm text-muted-foreground">Failed</p>
                <p className="text-2xl font-bold text-red-600">{summary.failed}</p>
              </div>
              <AlertTriangle className="h-8 w-8 text-red-500" />
            </div>
          </CardContent>
        </Card>
        <Card>
          <CardContent className="p-4">
            <div className="flex items-center justify-between">
              <div>
                <p className="text-sm text-muted-foreground">Completed</p>
                <p className="text-2xl font-bold text-emerald-600">{summary.completed}</p>
              </div>
              <CheckCircle className="h-8 w-8 text-emerald-500" />
            </div>
          </CardContent>
        </Card>
      </div>

      <Card>
        <CardHeader>
          <CardTitle>Recent workflow executions</CardTitle>
          <CardDescription>Latest instances ordered by creation time.</CardDescription>
        </CardHeader>
        <CardContent>
          {isLoading ? (
            <div className="flex h-32 items-center justify-center text-sm text-muted-foreground">
              Loading workflow executions...
            </div>
          ) : instances.length === 0 ? (
            <div className="flex h-32 flex-col items-center justify-center gap-2 text-sm text-muted-foreground">
              <p>No workflow executions match the selected filter.</p>
            </div>
          ) : (
            <div className="space-y-3">
              {instances.map((instance) => {
                const workflow = definitionLookup.get(instance.workflowId);
                const progress = calculateProgress(instance);
                const currentStepId = findCurrentStep(instance);

                return (
                  <Card key={instance.id} className="border border-muted/40">
                    <CardContent className="flex flex-col gap-3 p-4 md:flex-row md:items-center md:justify-between">
                      <div className="space-y-2">
                        <div className="flex items-center gap-2">
                          <Badge variant={badgeVariantForInstance(instance.status)}>
                            {formatInstanceStatus(instance.status)}
                          </Badge>
                          <span className="font-medium">
                            {workflow?.name ?? instance.name ?? 'Workflow Instance'}
                          </span>
                        </div>
                        <div className="text-xs text-muted-foreground space-y-1">
                          <p>Started {new Date(instance.createdAt).toLocaleString()}</p>
                          {instance.startedAt && (
                            <p>Execution began {new Date(instance.startedAt).toLocaleString()}</p>
                          )}
                          {currentStepId && <p>Current step: {currentStepId}</p>}
                          <div className="flex items-center gap-2">
                            <span>Progress:</span>
                            <div className="h-2 w-32 rounded-full bg-muted">
                              <div
                                className="h-2 rounded-full bg-blue-600 transition-all"
                                style={{ width: `${progress}%` }}
                              />
                            </div>
                            <span className="text-xs font-semibold">{progress}%</span>
                          </div>
                          {instance.errorMessage && (
                            <p className="text-red-500">Error: {instance.errorMessage}</p>
                          )}
                        </div>
                      </div>
                      <div className="flex items-center gap-2">
                        {instance.status === WorkflowInstanceStatus.Running && (
                          <Button variant="outline" onClick={() => handleCancelInstance(instance)}>
                            Cancel instance
                          </Button>
                        )}
                        <Button variant="ghost" onClick={() => handleOpenDetails(instance)}>
                          View details
                        </Button>
                      </div>
                    </CardContent>
                  </Card>
                );
              })}
            </div>
          )}
        </CardContent>
      </Card>

      <Dialog open={isDetailsOpen} onOpenChange={setIsDetailsOpen}>
        <DialogContent className="max-w-3xl">
          {selectedInstance && (
            <WorkflowInstanceDetails
              instance={selectedInstance}
              workflowName={definitionLookup.get(selectedInstance.workflowId)?.name}
            />
          )}
        </DialogContent>
      </Dialog>
    </div>
  );
}

interface WorkflowInstanceDetailsProps {
  instance: WorkflowInstance;
  workflowName?: string;
}

function WorkflowInstanceDetails({ instance, workflowName }: WorkflowInstanceDetailsProps) {
  return (
    <>
      <DialogHeader>
        <DialogTitle>Workflow instance details</DialogTitle>
        <DialogDescription>
          Instance {instance.id} for {workflowName ?? 'workflow'}
        </DialogDescription>
      </DialogHeader>

      <ScrollArea className="max-h-[60vh] pr-4">
        <div className="space-y-6 py-4">
          <section className="space-y-2">
            <h4 className="text-sm font-semibold">Overview</h4>
            <div className="grid gap-2 text-sm text-muted-foreground sm:grid-cols-2">
              <div>
                <span className="font-medium text-foreground">Status:</span> {formatInstanceStatus(instance.status)}
              </div>
              <div>
                <span className="font-medium text-foreground">Started:</span> {new Date(instance.createdAt).toLocaleString()}
              </div>
              {instance.startedAt && (
                <div>
                  <span className="font-medium text-foreground">Execution began:</span> {new Date(instance.startedAt).toLocaleString()}
                </div>
              )}
              {instance.completedAt && (
                <div>
                  <span className="font-medium text-foreground">Completed:</span> {new Date(instance.completedAt).toLocaleString()}
                </div>
              )}
            </div>
          </section>

          <section className="space-y-2">
            <h4 className="text-sm font-semibold">Variables</h4>
            <pre className="rounded bg-muted/40 p-3 text-xs">
              {JSON.stringify(instance.variables ?? {}, null, 2)}
            </pre>
          </section>

          <section className="space-y-2">
            <h4 className="text-sm font-semibold">Step executions</h4>
            {instance.stepInstances?.length ? (
              <div className="space-y-2">
                {instance.stepInstances.map((step) => (
                  <div key={step.id} className="rounded border border-muted/40 p-3 text-xs">
                    <div className="flex flex-wrap items-center justify-between gap-2">
                      <span className="font-medium text-foreground">Step {step.workflowStepId}</span>
                      <Badge variant={badgeVariantForStep(step.status)}>
                        {formatStepStatus(step.status)}
                      </Badge>
                    </div>
                    <Separator className="my-2" />
                    <div className="space-y-2">
                      <div>
                        <p className="font-medium text-foreground">Input</p>
                        <pre className="rounded bg-muted/40 p-2">{JSON.stringify(step.input ?? {}, null, 2)}</pre>
                      </div>
                      <div>
                        <p className="font-medium text-foreground">Output</p>
                        <pre className="rounded bg-muted/40 p-2">{JSON.stringify(step.output ?? {}, null, 2)}</pre>
                      </div>
                      {step.errorMessage && (
                        <div className="text-destructive">Error: {step.errorMessage}</div>
                      )}
                    </div>
                  </div>
                ))}
              </div>
            ) : (
              <p className="text-xs text-muted-foreground">No step executions recorded yet.</p>
            )}
          </section>

          <section className="space-y-2">
            <h4 className="text-sm font-semibold">Approvals</h4>
            {instance.approvals?.length ? (
              <div className="space-y-2">
                {instance.approvals.map((approval) => (
                  <div key={approval.id} className="rounded border border-muted/40 p-3 text-xs">
                    <div className="flex flex-wrap items-center justify-between gap-2">
                      <span className="font-medium text-foreground">Approver: {approval.requiredApprover}</span>
                      <Badge variant={badgeVariantForApproval(approval.status)}>
                        {formatApprovalStatus(approval.status)}
                      </Badge>
                    </div>
                    <Separator className="my-2" />
                    <div className="space-y-1">
                      <p>Requested: {new Date(approval.requestedAt).toLocaleString()}</p>
                      {approval.respondedAt && <p>Responded: {new Date(approval.respondedAt).toLocaleString()}</p>}
                      {approval.comments && <p>Comments: {approval.comments}</p>}
                    </div>
                  </div>
                ))}
              </div>
            ) : (
              <p className="text-xs text-muted-foreground">No approvals requested.</p>
            )}
          </section>
        </div>
      </ScrollArea>
    </>
  );
}