'use client';

import React, { useCallback, useEffect, useMemo, useState } from 'react';
import { Card, CardContent, CardDescription, CardHeader, CardTitle } from '@/components/ui/card';
import { Button } from '@/components/ui/button';
import { Badge } from '@/components/ui/badge';
import { Dialog, DialogContent, DialogDescription, DialogHeader, DialogTitle, DialogTrigger } from '@/components/ui/dialog';
import { Input } from '@/components/ui/input';
import { Label } from '@/components/ui/label';
import { Select, SelectContent, SelectItem, SelectTrigger, SelectValue } from '@/components/ui/select';
import { Textarea } from '@/components/ui/textarea';
import { toast } from 'sonner';
import {
  CreateWorkflowTriggerInput,
  WorkflowDefinition,
  WorkflowTrigger,
  WorkflowTriggerType,
  workflowService
} from '@/lib/services/workflow-service';
import {
  Activity,
  CalendarClock,
  MousePointerClick,
  PlugZap,
  RefreshCcw,
  Trash2,
  UploadCloud
} from 'lucide-react';

interface WorkflowTriggerManagerProps {
  definitions: WorkflowDefinition[];
  onChange?: () => void;
}

interface TriggerFormState {
  name: string;
  type: WorkflowTriggerType;
  eventType: string;
  cronExpression: string;
  callbackUrl: string;
  filePath: string;
}

const defaultFormState: TriggerFormState = {
  name: '',
  type: WorkflowTriggerType.Manual,
  eventType: '',
  cronExpression: '0 0 * * *',
  callbackUrl: '',
  filePath: ''
};

const triggerIcons: Record<WorkflowTriggerType, React.ReactNode> = {
  [WorkflowTriggerType.Manual]: <MousePointerClick className="h-4 w-4" />,
  [WorkflowTriggerType.Event]: <PlugZap className="h-4 w-4" />,
  [WorkflowTriggerType.Schedule]: <CalendarClock className="h-4 w-4" />,
  [WorkflowTriggerType.Webhook]: <UploadCloud className="h-4 w-4" />,
  [WorkflowTriggerType.FileWatch]: <Activity className="h-4 w-4" />
};

const formatTriggerType = (type: WorkflowTriggerType | number | string): string => {
  const value = typeof type === 'number' ? type : Number(type);
  switch (value) {
    case WorkflowTriggerType.Manual:
      return 'Manual';
    case WorkflowTriggerType.Event:
      return 'Event';
    case WorkflowTriggerType.Schedule:
      return 'Schedule';
    case WorkflowTriggerType.Webhook:
      return 'Webhook';
    case WorkflowTriggerType.FileWatch:
      return 'File Watch';
    default:
      return 'Unknown';
  }
};

const getTriggerIcon = (type: WorkflowTriggerType | number | string) => {
  const numericType = typeof type === 'number' ? type : Number(type);
  return triggerIcons[numericType as WorkflowTriggerType] ?? <Activity className="h-4 w-4" />;
};

export function WorkflowTriggerManager({ definitions, onChange }: WorkflowTriggerManagerProps) {
  const [selectedWorkflowId, setSelectedWorkflowId] = useState<string>(() => definitions[0]?.id ?? '');
  const [triggers, setTriggers] = useState<WorkflowTrigger[]>([]);
  const [isLoading, setIsLoading] = useState(false);
  const [isDialogOpen, setIsDialogOpen] = useState(false);
  const [formState, setFormState] = useState<TriggerFormState>(defaultFormState);
  const [isSubmitting, setIsSubmitting] = useState(false);
  const [evaluationEventType, setEvaluationEventType] = useState('');
  const [evaluationPayload, setEvaluationPayload] = useState('{}');
  const [isEvaluating, setIsEvaluating] = useState(false);

  const selectedWorkflow = useMemo(
    () => definitions.find((definition) => definition.id === selectedWorkflowId) ?? null,
    [definitions, selectedWorkflowId]
  );

  const refreshTriggers = useCallback(async () => {
    if (!selectedWorkflowId) {
      setTriggers([]);
      return;
    }

    try {
      setIsLoading(true);
      const response = await workflowService.getTriggers(selectedWorkflowId);
      setTriggers(response);
    } catch (error) {
      console.error('Failed to load triggers', error);
      toast.error('Unable to load triggers for this workflow.');
    } finally {
      setIsLoading(false);
    }
  }, [selectedWorkflowId]);

  useEffect(() => {
  setSelectedWorkflowId((current) => current || (definitions[0]?.id ?? ''));
  }, [definitions]);

  useEffect(() => {
    refreshTriggers();
  }, [refreshTriggers]);

  const handleOpenDialog = () => {
    setFormState({
      ...defaultFormState,
      name: `${selectedWorkflow?.name ?? 'Workflow'} Trigger`
    });
    setIsDialogOpen(true);
  };

  const handleSubmit = async () => {
    if (!selectedWorkflowId) {
      toast.error('Select a workflow before creating a trigger');
      return;
    }

    if (!formState.name.trim()) {
      toast.error('Trigger name is required');
      return;
    }

    const configuration: Record<string, unknown> = {};
    switch (formState.type) {
      case WorkflowTriggerType.Event:
        if (!formState.eventType.trim()) {
          toast.error('Event type is required for event triggers');
          return;
        }
        configuration.eventType = formState.eventType.trim();
        break;
      case WorkflowTriggerType.Schedule:
        if (!formState.cronExpression.trim()) {
          toast.error('Cron expression is required for schedule triggers');
          return;
        }
        configuration.cronExpression = formState.cronExpression.trim();
        break;
      case WorkflowTriggerType.Webhook:
        if (!formState.callbackUrl.trim()) {
          toast.error('Callback URL is required for webhook triggers');
          return;
        }
        configuration.callbackUrl = formState.callbackUrl.trim();
        break;
      case WorkflowTriggerType.FileWatch:
        if (!formState.filePath.trim()) {
          toast.error('File path is required for file watch triggers');
          return;
        }
        configuration.path = formState.filePath.trim();
        break;
      default:
        break;
    }

    const payload: CreateWorkflowTriggerInput = {
      name: formState.name.trim(),
      type: formState.type,
      configuration,
    };

    try {
      setIsSubmitting(true);
      await workflowService.createTrigger(selectedWorkflowId, payload);
      toast.success('Trigger created successfully');
      setIsDialogOpen(false);
      await refreshTriggers();
      onChange?.();
    } catch (error) {
      console.error('Failed to create trigger', error);
      toast.error('Failed to create trigger');
    } finally {
      setIsSubmitting(false);
    }
  };

  const handleDelete = async (triggerId: string) => {
    if (!confirm('Delete this trigger? This action cannot be undone.')) {
      return;
    }

    try {
      await workflowService.deleteTrigger(triggerId);
      toast.success('Trigger deleted');
      await refreshTriggers();
      onChange?.();
    } catch (error) {
      console.error('Failed to delete trigger', error);
      toast.error('Unable to delete trigger');
    }
  };

  const handleEvaluate = async () => {
    const eventType = evaluationEventType.trim();

    if (!eventType) {
      toast.error('Provide an event type to evaluate triggers');
      return;
    }

    let payload: Record<string, unknown> = {};
    try {
      payload = evaluationPayload ? JSON.parse(evaluationPayload) : {};
    } catch {
      toast.error('Event payload must be valid JSON');
      return;
    }

    try {
      setIsEvaluating(true);
      const triggeredWorkflows = await workflowService.evaluateTriggers(eventType, payload);
      if (triggeredWorkflows.length === 0) {
        toast.info('No workflows were triggered for this event.');
      } else {
        toast.success(`Event matched ${triggeredWorkflows.length} trigger(s).`);
      }
    } catch (error) {
      console.error('Failed to evaluate triggers', error);
      toast.error('Unable to evaluate triggers');
    } finally {
      setIsEvaluating(false);
    }
  };

  if (definitions.length === 0) {
    return (
      <Card>
        <CardHeader>
          <CardTitle>No workflows available</CardTitle>
          <CardDescription>Create a workflow definition before configuring triggers.</CardDescription>
        </CardHeader>
      </Card>
    );
  }

  return (
    <div className="space-y-6">
      <div className="flex flex-col gap-4 md:flex-row md:items-center md:justify-between">
        <div>
          <h2 className="text-lg font-semibold">Workflow Triggers</h2>
          <p className="text-sm text-muted-foreground">
            Configure how workflows are initiated and test trigger logic in real time.
          </p>
        </div>
        <div className="flex flex-col gap-2 sm:flex-row">
          <Select value={selectedWorkflowId} onValueChange={setSelectedWorkflowId}>
            <SelectTrigger className="w-[260px]">
              <SelectValue placeholder="Select workflow" />
            </SelectTrigger>
            <SelectContent>
              {definitions.map((definition) => (
                <SelectItem key={definition.id} value={definition.id}>
                  {definition.name}
                </SelectItem>
              ))}
            </SelectContent>
          </Select>
          <Button onClick={handleOpenDialog} disabled={!selectedWorkflowId}>
            Create Trigger
          </Button>
        </div>
      </div>

      <Card>
        <CardHeader className="flex items-center justify-between">
          <div>
            <CardTitle>{selectedWorkflow?.name}</CardTitle>
            <CardDescription>
              {selectedWorkflow?.description || 'No description provided for this workflow.'}
            </CardDescription>
          </div>
          <Button variant="outline" size="sm" onClick={refreshTriggers} disabled={isLoading}>
            <RefreshCcw className="mr-2 h-4 w-4" /> Refresh
          </Button>
        </CardHeader>
        <CardContent>
          {isLoading ? (
            <div className="flex h-32 items-center justify-center text-sm text-muted-foreground">
              Loading triggers...
            </div>
          ) : triggers.length === 0 ? (
            <div className="flex h-32 flex-col items-center justify-center gap-2 text-sm text-muted-foreground">
              <p>No triggers have been configured for this workflow yet.</p>
              <p>Create one to start automating execution.</p>
            </div>
          ) : (
            <div className="space-y-3">
              {triggers.map((trigger) => (
                <Card key={trigger.id} className="border border-muted-foreground/10">
                  <CardContent className="flex flex-col gap-3 p-4 md:flex-row md:items-center md:justify-between">
                    <div className="flex flex-col gap-2">
                      <div className="flex items-center gap-2">
                        <Badge variant="outline" className="flex items-center gap-1 text-xs">
                          {getTriggerIcon(trigger.type)}
                          {formatTriggerType(trigger.type)}
                        </Badge>
                        <span className="text-sm font-semibold">{trigger.name}</span>
                      </div>
                      <div className="text-xs text-muted-foreground">
                        Created {new Date(trigger.createdAt).toLocaleString()} by {trigger.createdBy}
                      </div>
                      <pre className="max-h-40 w-full overflow-auto rounded bg-muted/40 p-2 text-xs text-muted-foreground">
                        {JSON.stringify(trigger.configuration, null, 2)}
                      </pre>
                    </div>
                    <Button
                      variant="ghost"
                      size="icon"
                      className="self-start text-muted-foreground hover:text-destructive"
                      onClick={() => handleDelete(trigger.id)}
                    >
                      <Trash2 className="h-4 w-4" />
                    </Button>
                  </CardContent>
                </Card>
              ))}
            </div>
          )}
        </CardContent>
      </Card>

      <Card>
        <CardHeader>
          <CardTitle className="text-base">Evaluate Event Triggers</CardTitle>
          <CardDescription>
            Send a sample event payload to test event-based triggers without waiting for production events.
          </CardDescription>
        </CardHeader>
        <CardContent className="space-y-4">
          <div className="grid gap-4 md:grid-cols-[240px_1fr]">
            <div className="space-y-2">
              <Label htmlFor="event-type">Event Type</Label>
              <Input
                id="event-type"
                placeholder="e.g. customer.payment.created"
                value={evaluationEventType}
                onChange={(event) => setEvaluationEventType(event.target.value)}
              />
            </div>
            <div className="space-y-2">
              <Label htmlFor="event-payload">Event Payload (JSON)</Label>
              <Textarea
                id="event-payload"
                rows={4}
                value={evaluationPayload}
                onChange={(event) => setEvaluationPayload(event.target.value)}
              />
            </div>
          </div>
          <Button onClick={handleEvaluate} disabled={isEvaluating} className="w-full md:w-auto">
            {isEvaluating ? 'Evaluating…' : 'Evaluate Event Triggers'}
          </Button>
        </CardContent>
      </Card>

      <Dialog open={isDialogOpen} onOpenChange={setIsDialogOpen}>
        <DialogContent className="max-w-2xl">
          <DialogHeader>
            <DialogTitle>New Trigger</DialogTitle>
            <DialogDescription>
              Choose how this workflow should be started. Different trigger types expose different configuration options.
            </DialogDescription>
          </DialogHeader>

          <div className="space-y-4 py-4">
            <div className="space-y-2">
              <Label htmlFor="trigger-name">Trigger Name</Label>
              <Input
                id="trigger-name"
                value={formState.name}
                onChange={(event) => setFormState((state) => ({ ...state, name: event.target.value }))
                }
              />
            </div>

            <div className="space-y-2">
              <Label htmlFor="trigger-type">Trigger Type</Label>
              <Select
                value={formState.type.toString()}
                onValueChange={(value) =>
                  setFormState((state) => ({ ...state, type: Number(value) as WorkflowTriggerType }))
                }
              >
                <SelectTrigger id="trigger-type">
                  <SelectValue />
                </SelectTrigger>
                <SelectContent>
                  <SelectItem value={WorkflowTriggerType.Manual.toString()}>Manual</SelectItem>
                  <SelectItem value={WorkflowTriggerType.Event.toString()}>Event</SelectItem>
                  <SelectItem value={WorkflowTriggerType.Schedule.toString()}>Schedule</SelectItem>
                  <SelectItem value={WorkflowTriggerType.Webhook.toString()}>Webhook</SelectItem>
                  <SelectItem value={WorkflowTriggerType.FileWatch.toString()}>File Watch</SelectItem>
                </SelectContent>
              </Select>
            </div>

            <TriggerConfigurationFields formState={formState} setFormState={setFormState} />
          </div>

          <div className="flex justify-end gap-2">
            <Button variant="outline" onClick={() => setIsDialogOpen(false)}>
              Cancel
            </Button>
            <Button onClick={handleSubmit} disabled={isSubmitting}>
              {isSubmitting ? 'Saving…' : 'Save Trigger'}
            </Button>
          </div>
        </DialogContent>
      </Dialog>
    </div>
  );
}

interface TriggerConfigurationFieldsProps {
  formState: TriggerFormState;
  setFormState: React.Dispatch<React.SetStateAction<TriggerFormState>>;
}

function TriggerConfigurationFields({ formState, setFormState }: TriggerConfigurationFieldsProps) {
  switch (formState.type) {
    case WorkflowTriggerType.Event:
      return (
        <div className="space-y-2">
          <Label htmlFor="event-type-input">Event Type</Label>
          <Input
            id="event-type-input"
            placeholder="e.g. workflow.submitted"
            value={formState.eventType}
            onChange={(event) => setFormState((state) => ({ ...state, eventType: event.target.value }))}
          />
        </div>
      );
    case WorkflowTriggerType.Schedule:
      return (
        <div className="space-y-2">
          <Label htmlFor="cron-expression">Cron Expression</Label>
          <Input
            id="cron-expression"
            placeholder="0 9 * * 1-5"
            value={formState.cronExpression}
            onChange={(event) => setFormState((state) => ({ ...state, cronExpression: event.target.value }))}
          />
          <p className="text-xs text-muted-foreground">
            Use standard cron syntax. Example: <code>0 9 * * 1-5</code> runs on weekdays at 09:00.
          </p>
        </div>
      );
    case WorkflowTriggerType.Webhook:
      return (
        <div className="space-y-2">
          <Label htmlFor="callback-url">Callback URL</Label>
          <Input
            id="callback-url"
            placeholder="https://example.com/webhook"
            value={formState.callbackUrl}
            onChange={(event) => setFormState((state) => ({ ...state, callbackUrl: event.target.value }))}
          />
        </div>
      );
    case WorkflowTriggerType.FileWatch:
      return (
        <div className="space-y-2">
          <Label htmlFor="file-path">File Path</Label>
          <Input
            id="file-path"
            placeholder="/shared/reports/inbox"
            value={formState.filePath}
            onChange={(event) => setFormState((state) => ({ ...state, filePath: event.target.value }))}
          />
        </div>
      );
    default:
      return (
        <div className="space-y-2 text-sm text-muted-foreground">
          Manual triggers can be run on demand from the workflow detail view.
        </div>
      );
  }
}