import React, { useState, useEffect } from 'react';
import { Card, CardContent, CardDescription, CardHeader, CardTitle } from '@/components/ui/card';
import { Button } from '@/components/ui/button';
import { Badge } from '@/components/ui/badge';
import { Tabs, TabsContent, TabsList, TabsTrigger } from '@/components/ui/tabs';
import { Dialog, DialogContent, DialogDescription, DialogHeader, DialogTitle, DialogTrigger } from '@/components/ui/dialog';
import { Select, SelectContent, SelectItem, SelectTrigger, SelectValue } from '@/components/ui/select';
import { Input } from '@/components/ui/input';
import { Label } from '@/components/ui/label';
import { Textarea } from '@/components/ui/textarea';
import { Switch } from '@/components/ui/switch';
import { Plus, Play, Edit, Trash2, Settings, Workflow } from 'lucide-react';
import { toast } from 'sonner';
import { RuleBuilder } from './rule-builder';
import { WorkflowTemplateManager } from './workflow-templates';
import { WorkflowExecutionMonitor } from './workflow-execution-monitor';
import { workflowService, type WorkflowDefinition } from '@/lib/services/workflow-service';

interface Workflow {
  id: string;
  name: string;
  description: string;
  type: string;
  trigger: string;
  isActive: boolean;
  priority: number;
  createdAt: string;
}

interface WorkflowTemplate {
  id: string;
  name: string;
  description: string;
  type: string;
}

interface WorkflowBuilderProps {
  onWorkflowCreated?: (workflow: Workflow) => void;
  onWorkflowUpdated?: (workflow: Workflow) => void;
}

export function WorkflowBuilder({ onWorkflowCreated, onWorkflowUpdated }: WorkflowBuilderProps) {
  const [workflows, setWorkflows] = useState<Workflow[]>([]);
  const [definitions, setDefinitions] = useState<WorkflowDefinition[]>([]);
  const [templates, setTemplates] = useState<WorkflowTemplate[]>([]);
  const [isLoading, setIsLoading] = useState(true);
  const [selectedWorkflow, setSelectedWorkflow] = useState<Workflow | null>(null);
  const [isCreateDialogOpen, setIsCreateDialogOpen] = useState(false);
  const [isEditDialogOpen, setIsEditDialogOpen] = useState(false);
  const [refreshKey, setRefreshKey] = useState(0);

  useEffect(() => {
    loadWorkflows();
    loadTemplates();
  }, []);

  const loadWorkflows = async () => {
  try {
    const defs = await workflowService.getDefinitions();
    setDefinitions(defs);
    const mapped: Workflow[] = defs.map((d) => ({
      id: d.id,
      name: d.name,
      description: d.description,
      type: String(d.type),
      trigger: String((d as any).triggerType ?? ''),
      isActive: d.isActive,
      priority: d.priority,
      createdAt: ''
    }));
    setWorkflows(mapped);
  } catch (error) {
    toast.error('Failed to load workflows');
  } finally {
    setIsLoading(false);
  }
};

  const loadTemplates = async () => {
  try {
    const defs = await workflowService.getDefinitions();
    setTemplates(defs.map(d => ({ id: d.id, name: d.name, description: d.description, type: String(d.type) })));
  } catch (error) {
    toast.error('Failed to load workflow templates');
  }
};

  const handleCreateWorkflow = async (workflowData: Partial<Workflow>) => {
  try {
    const t = (workflowData.trigger || 'Manual').toString().toLowerCase();
    const triggerType = t.includes('event') ? 'Event' : t.includes('schedule') ? 'Schedule' : t.includes('webhook') ? 'Webhook' : t.includes('file') ? 'FileWatch' : 'Manual';
    const created = await workflowService.createDefinition({
      name: String(workflowData.name || 'Untitled Workflow'),
      description: String(workflowData.description || ''),
      type: (workflowData.type as any) ?? 'Custom',
      triggerType,
      isActive: workflowData.isActive ?? true,
      priority: workflowData.priority ?? 1,
    });
    setDefinitions(prev => [...prev, created]);
    const mapped: Workflow = {
      id: created.id,
      name: created.name,
      description: created.description,
      type: String(created.type),
      trigger: String((created as any).triggerType ?? ''),
      isActive: created.isActive,
      priority: created.priority,
      createdAt: ''
    };
    setWorkflows(prev => [...prev, mapped]);
    setIsCreateDialogOpen(false);
    toast.success('Workflow created successfully');
    onWorkflowCreated?.(mapped);
  } catch (error) {
    toast.error('Failed to create workflow');
  }
};

  const handleUpdateWorkflow = async (workflowId: string, workflowData: Partial<Workflow>) => {
  try {
    const t = workflowData.trigger?.toString().toLowerCase();
    const triggerType = t?.includes('event') ? 'Event' : t?.includes('schedule') ? 'Schedule' : t?.includes('webhook') ? 'Webhook' : t?.includes('file') ? 'FileWatch' : undefined;
    const updated = await workflowService.updateDefinition(workflowId, {
      name: workflowData.name,
      description: workflowData.description,
      type: (workflowData.type as any),
      triggerType,
      isActive: workflowData.isActive,
      priority: workflowData.priority,
    });
    setDefinitions(prev => prev.map(d => d.id === workflowId ? updated : d));
    const mapped: Workflow = {
      id: updated.id,
      name: updated.name,
      description: updated.description,
      type: String(updated.type),
      trigger: String((updated as any).triggerType ?? ''),
      isActive: updated.isActive,
      priority: updated.priority,
      createdAt: ''
    };
    setWorkflows(prev => prev.map(w => w.id === workflowId ? mapped : w));
    setIsEditDialogOpen(false);
    toast.success('Workflow updated successfully');
    onWorkflowUpdated?.(mapped);
  } catch (error) {
    toast.error('Failed to update workflow');
  }
};

  const handleDeleteWorkflow = async (workflowId: string) => {
  if (!confirm('Are you sure you want to delete this workflow?')) return;

  try {
    await workflowService.deleteDefinition(workflowId);
    setDefinitions(prev => prev.filter(d => d.id !== workflowId));
    setWorkflows(prev => prev.filter(w => w.id !== workflowId));
    toast.success('Workflow deleted successfully');
  } catch (error) {
    toast.error('Failed to delete workflow');
  }
};

  const handleTriggerWorkflow = async (workflowId: string) => {
  try {
    await workflowService.startInstance({ workflowId, variables: {} });
    toast.success('Workflow triggered successfully');
  } catch (error) {
    toast.error('Failed to trigger workflow');
  }
};

  if (isLoading) {
    return <div className="flex justify-center p-8">Loading workflows...</div>;
  }

  return (
    <div className="space-y-6">
      <div className="flex justify-between items-center">
        <div>
          <h2 className="text-2xl font-bold">Workflow Automation</h2>
          <p className="text-muted-foreground">Create and manage automated business processes</p>
        </div>
        <Dialog open={isCreateDialogOpen} onOpenChange={setIsCreateDialogOpen}>
          <DialogTrigger asChild>
            <Button>
              <Plus className="w-4 h-4 mr-2" />
              Create Workflow
            </Button>
          </DialogTrigger>
          <DialogContent className="max-w-2xl">
            <WorkflowForm
              templates={templates}
              onSubmit={handleCreateWorkflow}
              onCancel={() => setIsCreateDialogOpen(false)}
            />
          </DialogContent>
        </Dialog>
      </div>

      <Tabs defaultValue="workflows" className="space-y-4">
        <TabsList>
          <TabsTrigger value="workflows">My Workflows</TabsTrigger>
          <TabsTrigger value="templates">Templates</TabsTrigger>
          <TabsTrigger value="rules">Business Rules</TabsTrigger>
          <TabsTrigger value="executions">Recent Executions</TabsTrigger>
        </TabsList>

        <TabsContent value="workflows" className="space-y-4">
          <div className="grid gap-4 md:grid-cols-2 lg:grid-cols-3">
            {workflows.map((workflow) => (
              <Card key={workflow.id} className="relative">
                <CardHeader>
                  <div className="flex justify-between items-start">
                    <div>
                      <CardTitle className="text-lg">{workflow.name}</CardTitle>
                      <CardDescription>{workflow.description}</CardDescription>
                    </div>
                    <Badge variant={workflow.isActive ? "default" : "secondary"}>
                      {workflow.isActive ? "Active" : "Inactive"}
                    </Badge>
                  </div>
                </CardHeader>
                <CardContent>
                  <div className="space-y-2">
                    <div className="flex justify-between text-sm">
                      <span>Type:</span>
                      <span className="font-medium">{workflow.type}</span>
                    </div>
                    <div className="flex justify-between text-sm">
                      <span>Trigger:</span>
                      <span className="font-medium">{workflow.trigger}</span>
                    </div>
                    <div className="flex justify-between text-sm">
                      <span>Priority:</span>
                      <span className="font-medium">{workflow.priority}</span>
                    </div>
                  </div>
                  <div className="flex gap-2 mt-4">
                    <Button
                      size="sm"
                      variant="outline"
                      onClick={() => handleTriggerWorkflow(workflow.id)}
                    >
                      <Play className="w-4 h-4" />
                    </Button>
                    <Button
                      size="sm"
                      variant="outline"
                      onClick={() => {
                        setSelectedWorkflow(workflow);
                        setIsEditDialogOpen(true);
                      }}
                    >
                      <Edit className="w-4 h-4" />
                    </Button>
                    <Button
                      size="sm"
                      variant="outline"
                      onClick={() => handleDeleteWorkflow(workflow.id)}
                    >
                      <Trash2 className="w-4 h-4" />
                    </Button>
                  </div>
                </CardContent>
              </Card>
            ))}
          </div>
        </TabsContent>

        <TabsContent value="templates" className="space-y-4">
          <WorkflowTemplateManager
            definitions={definitions}
            onInstanceStarted={() => {
              // Handle instance started
              setRefreshKey(prev => prev + 1);
            }}
          />
          {/* TODO: Add template selection functionality here */}
          <div className="text-center py-8 text-muted-foreground">
            Template selection will be implemented here
          </div>
        </TabsContent>

        <TabsContent value="rules" className="space-y-4">
          <RuleBuilder
            entityType="Payment"
            onRuleCreated={(rule) => toast.success(`Rule "${rule.name}" created successfully`)}
            onRuleUpdated={(rule) => toast.success(`Rule "${rule.name}" updated successfully`)}
          />
        </TabsContent>

        <TabsContent value="executions" className="space-y-4">
          <WorkflowExecutionMonitor definitions={definitions} />
        </TabsContent>
      </Tabs>

      {/* Edit Workflow Dialog */}
      <Dialog open={isEditDialogOpen} onOpenChange={setIsEditDialogOpen}>
        <DialogContent className="max-w-2xl">
          {selectedWorkflow && (
            <WorkflowForm
              workflow={selectedWorkflow}
              templates={templates}
              onSubmit={(data) => handleUpdateWorkflow(selectedWorkflow.id, data)}
              onCancel={() => setIsEditDialogOpen(false)}
            />
          )}
        </DialogContent>
      </Dialog>
    </div>
  );
}

interface WorkflowFormProps {
  workflow?: Workflow;
  templates: WorkflowTemplate[];
  onSubmit: (data: Partial<Workflow>) => void;
  onCancel: () => void;
}

function WorkflowForm({ workflow, templates, onSubmit, onCancel }: WorkflowFormProps) {
  const [formData, setFormData] = useState<Partial<Workflow>>({
    name: workflow?.name || '',
    description: workflow?.description || '',
    type: workflow?.type || 'Custom',
    trigger: workflow?.trigger || 'Manual',
    isActive: workflow?.isActive ?? true,
    priority: workflow?.priority || 1,
  });

  const [selectedTemplate, setSelectedTemplate] = useState<string>('');

  const handleTemplateSelect = (templateId: string) => {
    const template = templates.find(t => t.id === templateId);
    if (template) {
      setFormData(prev => ({
        ...prev,
        name: `${template.name} - Custom`,
        description: template.description,
        type: template.type,
      }));
      setSelectedTemplate(templateId);
    }
  };

  const handleSubmit = (e: React.FormEvent) => {
    e.preventDefault();
    onSubmit(formData);
  };

  return (
    <form onSubmit={handleSubmit}>
      <DialogHeader>
        <DialogTitle>{workflow ? 'Edit Workflow' : 'Create New Workflow'}</DialogTitle>
        <DialogDescription>
          {workflow ? 'Update the workflow configuration' : 'Create a new automated workflow'}
        </DialogDescription>
      </DialogHeader>

      <div className="space-y-4 py-4">
        {!workflow && (
          <div className="space-y-2">
            <Label htmlFor="template">Start from Template (Optional)</Label>
            <Select value={selectedTemplate} onValueChange={handleTemplateSelect}>
              <SelectTrigger>
                <SelectValue placeholder="Choose a template or start blank" />
              </SelectTrigger>
              <SelectContent>
                <SelectItem value="">Start Blank</SelectItem>
                {templates.map((template) => (
                  <SelectItem key={template.id} value={template.id}>
                    {template.name}
                  </SelectItem>
                ))}
              </SelectContent>
            </Select>
          </div>
        )}

        <div className="grid grid-cols-2 gap-4">
          <div className="space-y-2">
            <Label htmlFor="name">Workflow Name</Label>
            <Input
              id="name"
              value={formData.name}
              onChange={(e) => setFormData(prev => ({ ...prev, name: e.target.value }))}
              required
            />
          </div>
          <div className="space-y-2">
            <Label htmlFor="type">Type</Label>
            <Select
              value={formData.type}
              onValueChange={(value) => setFormData(prev => ({ ...prev, type: value }))}
            >
              <SelectTrigger>
                <SelectValue />
              </SelectTrigger>
              <SelectContent>
                <SelectItem value="PaymentApproval">Payment Approval</SelectItem>
                <SelectItem value="DocumentReview">Document Review</SelectItem>
                <SelectItem value="ComplianceCheck">Compliance Check</SelectItem>
                <SelectItem value="Notification">Notification</SelectItem>
                <SelectItem value="TaxFiling">Tax Filing</SelectItem>
                <SelectItem value="ClientOnboarding">Client Onboarding</SelectItem>
                <SelectItem value="Custom">Custom</SelectItem>
              </SelectContent>
            </Select>
          </div>
        </div>

        <div className="space-y-2">
          <Label htmlFor="description">Description</Label>
          <Textarea
            id="description"
            value={formData.description}
            onChange={(e) => setFormData(prev => ({ ...prev, description: e.target.value }))}
            rows={3}
          />
        </div>

        <div className="grid grid-cols-2 gap-4">
          <div className="space-y-2">
            <Label htmlFor="trigger">Trigger</Label>
            <Select
              value={formData.trigger}
              onValueChange={(value) => setFormData(prev => ({ ...prev, trigger: value }))}
            >
              <SelectTrigger>
                <SelectValue />
              </SelectTrigger>
              <SelectContent>
                <SelectItem value="Manual">Manual</SelectItem>
                <SelectItem value="Scheduled">Scheduled</SelectItem>
                <SelectItem value="EventBased">Event Based</SelectItem>
                <SelectItem value="ConditionBased">Condition Based</SelectItem>
              </SelectContent>
            </Select>
          </div>
          <div className="space-y-2">
            <Label htmlFor="priority">Priority</Label>
            <Select
              value={formData.priority?.toString()}
              onValueChange={(value) => setFormData(prev => ({ ...prev, priority: parseInt(value) }))}
            >
              <SelectTrigger>
                <SelectValue />
              </SelectTrigger>
              <SelectContent>
                <SelectItem value="1">Low</SelectItem>
                <SelectItem value="2">Medium</SelectItem>
                <SelectItem value="3">High</SelectItem>
                <SelectItem value="4">Critical</SelectItem>
              </SelectContent>
            </Select>
          </div>
        </div>

        <div className="flex items-center space-x-2">
          <Switch
            id="isActive"
            checked={formData.isActive}
            onCheckedChange={(checked) => setFormData(prev => ({ ...prev, isActive: checked }))}
          />
          <Label htmlFor="isActive">Active</Label>
        </div>
      </div>

      <div className="flex justify-end gap-2">
        <Button type="button" variant="outline" onClick={onCancel}>
          Cancel
        </Button>
        <Button type="submit">
          {workflow ? 'Update Workflow' : 'Create Workflow'}
        </Button>
      </div>
    </form>
  );
}
