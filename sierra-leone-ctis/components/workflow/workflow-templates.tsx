import React, { useMemo, useState, useEffect } from 'react';
import { Card, CardContent, CardDescription, CardHeader, CardTitle } from '@/components/ui/card';
import { Button } from '@/components/ui/button';
import { Badge } from '@/components/ui/badge';
import { Dialog, DialogContent, DialogDescription, DialogHeader, DialogTitle } from '@/components/ui/dialog';
import { Input } from '@/components/ui/input';
import { Label } from '@/components/ui/label';
import { Textarea } from '@/components/ui/textarea';
import { Select, SelectContent, SelectItem, SelectTrigger, SelectValue } from '@/components/ui/select';
import { Settings, Rocket } from 'lucide-react';
import { toast } from 'sonner';
import {
  StartWorkflowInstanceRequest,
  WorkflowDefinition,
  WorkflowTriggerType,
  workflowService
} from '@/lib/services/workflow-service';

interface WorkflowTemplateManagerProps {
  definitions: WorkflowDefinition[];
  onInstanceStarted?: () => void;
}

interface StartDialogState {
  isOpen: boolean;
  workflow?: WorkflowDefinition;
  variables: string;
  isSubmitting: boolean;
}

const formatTriggerType = (trigger: WorkflowTriggerType | number | string) => {
  const value = typeof trigger === 'number' ? trigger : Number(trigger);
  switch (value) {
    case WorkflowTriggerType.Manual:
      return 'Manual';
    case WorkflowTriggerType.Event:
      return 'Event Driven';
    case WorkflowTriggerType.Schedule:
      return 'Scheduled';
    case WorkflowTriggerType.Webhook:
      return 'Webhook';
    case WorkflowTriggerType.FileWatch:
      return 'File Watch';
    default:
      return 'Unknown';
  }
};

export function WorkflowTemplateManager({ definitions, onInstanceStarted }: WorkflowTemplateManagerProps) {
  const [statusFilter, setStatusFilter] = useState<'all' | 'active' | 'inactive'>('all');
  const [dialogState, setDialogState] = useState<StartDialogState>({
    isOpen: false,
    variables: '{\n  "sample": "value"\n}' ,
    isSubmitting: false
  });
  const [fetchedDefinitions, setFetchedDefinitions] = useState<WorkflowDefinition[]>([]);

  useEffect(() => {
    if ((definitions?.length || 0) === 0) {
      (async () => {
        try {
          const defs = await workflowService.getDefinitions();
          setFetchedDefinitions(defs);
        } catch (e) {
          toast.error('Failed to load workflow definitions');
        }
      })();
    }
  }, [definitions]);

  const effectiveDefinitions = useMemo(() => {
    return (definitions && definitions.length > 0) ? definitions : fetchedDefinitions;
  }, [definitions, fetchedDefinitions]);

  const filteredDefinitions = useMemo(() => {
    if (statusFilter === 'active') {
      return effectiveDefinitions.filter((definition) => definition.isActive);
    }

    if (statusFilter === 'inactive') {
      return effectiveDefinitions.filter((definition) => !definition.isActive);
    }

    return effectiveDefinitions;
  }, [effectiveDefinitions, statusFilter]);

  const openStartDialog = (workflow: WorkflowDefinition) => {
    setDialogState({
      isOpen: true,
      workflow,
      variables: '{\n  "context": "example"\n}',
      isSubmitting: false
    });
  };

  const closeStartDialog = () => {
    setDialogState((state) => ({ ...state, isOpen: false }));
  };

  const handleStartInstance = async () => {
    if (!dialogState.workflow) return;

    let parsedVariables: Record<string, unknown> = {};
    const payload = dialogState.variables.trim();

    if (payload.length > 0) {
      try {
        parsedVariables = JSON.parse(payload);
      } catch {
        toast.error('Variables must be valid JSON');
        return;
      }
    }

    const request: StartWorkflowInstanceRequest = {
      workflowId: dialogState.workflow.id,
      variables: parsedVariables
    };

    try {
      setDialogState((state) => ({ ...state, isSubmitting: true }));
      await workflowService.startInstance(request);
      toast.success('Workflow instance started');
      setDialogState({ isOpen: false, workflow: undefined, variables: '{\n  "context": "example"\n}', isSubmitting: false });
      onInstanceStarted?.();
    } catch (error) {
      console.error('Failed to start workflow instance', error);
      toast.error('Failed to start workflow instance');
      setDialogState((state) => ({ ...state, isSubmitting: false }));
    }
  };

  if (effectiveDefinitions.length === 0) {
    return (
      <Card>
        <CardHeader>
          <CardTitle>No workflow definitions found</CardTitle>
          <CardDescription>
            Create a workflow definition in the back office to begin orchestrating automation templates.
          </CardDescription>
        </CardHeader>
      </Card>
    );
  }

  return (
    <div className="space-y-6">
      <div className="flex flex-col justify-between gap-4 md:flex-row md:items-center">
        <div>
          <h3 className="text-lg font-semibold">Workflow Definitions</h3>
          <p className="text-sm text-muted-foreground">
            Review published workflows, inspect trigger types, and launch manual instances for testing.
          </p>
        </div>
        <Select value={statusFilter} onValueChange={(value) => setStatusFilter(value as typeof statusFilter)}>
          <SelectTrigger className="w-52">
            <SelectValue placeholder="Filter by status" />
          </SelectTrigger>
          <SelectContent>
            <SelectItem value="all">All Workflows</SelectItem>
            <SelectItem value="active">Active Only</SelectItem>
            <SelectItem value="inactive">Inactive Only</SelectItem>
          </SelectContent>
        </Select>
      </div>

      <div className="grid gap-4 md:grid-cols-2 xl:grid-cols-3">
        {filteredDefinitions.map((definition) => (
          <Card key={definition.id} className="border border-muted/40">
            <CardHeader className="space-y-3">
              <div className="flex items-center justify-between">
                <CardTitle className="text-base font-semibold flex items-center gap-2">
                  <Settings className="h-4 w-4 text-muted-foreground" />
                  {definition.name}
                </CardTitle>
                <Badge variant={definition.isActive ? 'default' : 'secondary'}>
                  {definition.isActive ? 'Active' : 'Inactive'}
                </Badge>
              </div>
              <CardDescription>{definition.description || 'No description provided.'}</CardDescription>
            </CardHeader>
            <CardContent className="flex flex-col gap-4">
              <div className="flex items-center justify-between text-sm">
                <span className="text-muted-foreground">Type</span>
                <span className="font-medium">{definition.type ?? 'N/A'}</span>
              </div>
              <div className="flex items-center justify-between text-sm">
                <span className="text-muted-foreground">Trigger</span>
                <Badge variant="outline">{formatTriggerType(definition.triggerType)}</Badge>
              </div>
              <Button
                variant={definition.isActive ? 'default' : 'outline'}
                onClick={() => openStartDialog(definition)}
                disabled={!definition.isActive}
                className="flex items-center justify-center gap-2"
              >
                <Rocket className="h-4 w-4" />
                Start Instance
              </Button>
            </CardContent>
          </Card>
        ))}
      </div>

      {filteredDefinitions.length === 0 && (
        <Card>
          <CardContent className="py-12 text-center text-sm text-muted-foreground">
            No workflows match the selected filter.
          </CardContent>
        </Card>
      )}

      <Dialog open={dialogState.isOpen} onOpenChange={closeStartDialog}>
        <DialogContent className="max-w-2xl">
          <DialogHeader>
            <DialogTitle>
              Start Workflow Instance
            </DialogTitle>
            <DialogDescription>
              Provide optional variables to inject into the workflow context before execution begins.
            </DialogDescription>
          </DialogHeader>

          {dialogState.workflow && (
            <div className="space-y-4 py-4">
              <div className="grid gap-4 sm:grid-cols-2">
                <div>
                  <Label>Workflow</Label>
                  <p className="text-sm font-medium">{dialogState.workflow.name}</p>
                </div>
                <div>
                  <Label>Status</Label>
                  <Badge variant={dialogState.workflow.isActive ? 'default' : 'secondary'}>
                    {dialogState.workflow.isActive ? 'Active' : 'Inactive'}
                  </Badge>
                </div>
              </div>

              <div className="space-y-2">
                <Label htmlFor="instance-variables">Instance Variables (JSON)</Label>
                <Textarea
                  id="instance-variables"
                  rows={6}
                  value={dialogState.variables}
                  onChange={(event) =>
                    setDialogState((state) => ({ ...state, variables: event.target.value }))
                  }
                />
              </div>
            </div>
          )}

          <div className="flex justify-end gap-2">
            <Button variant="outline" onClick={closeStartDialog}>
              Cancel
            </Button>
            <Button onClick={handleStartInstance} disabled={dialogState.isSubmitting}>
              {dialogState.isSubmitting ? 'Starting…' : 'Start Workflow'}
            </Button>
          </div>
        </DialogContent>
      </Dialog>
    </div>
  );
}