import React, { useState, useEffect, useCallback } from 'react';
import { Card, CardContent, CardDescription, CardHeader, CardTitle } from '@/components/ui/card';
import { Button } from '@/components/ui/button';
import { Badge } from '@/components/ui/badge';
import { Select, SelectContent, SelectItem, SelectTrigger, SelectValue } from '@/components/ui/select';
import { Input } from '@/components/ui/input';
import { Label } from '@/components/ui/label';
import { Textarea } from '@/components/ui/textarea';
import { Dialog, DialogContent, DialogDescription, DialogHeader, DialogTitle, DialogTrigger } from '@/components/ui/dialog';
import { Tabs, TabsContent, TabsList, TabsTrigger } from '@/components/ui/tabs';
import { Plus, Trash2, Settings, CheckCircle, XCircle, GripVertical, Play, Save, Eye, Code } from 'lucide-react';
import { toast } from 'sonner';
import { DragDropContext, Droppable, Draggable, DropResult } from '@hello-pangea/dnd';

interface RuleCondition {
  id: string;
  field: string;
  operator: string;
  value: string;
  entityType: string;
}

interface RuleAction {
  id: string;
  actionType: string;
  parameters: Record<string, any>;
}

interface WorkflowRule {
  id: string;
  name: string;
  description: string;
  conditions: RuleCondition[];
  actions: RuleAction[];
  isActive: boolean;
}

interface RuleBuilderProps {
  entityType: string;
  onRuleCreated?: (rule: WorkflowRule) => void;
  onRuleUpdated?: (rule: WorkflowRule) => void;
}

export function RuleBuilder({ entityType, onRuleCreated, onRuleUpdated }: RuleBuilderProps) {
  const [rules, setRules] = useState<WorkflowRule[]>([]);
  const [availableFields, setAvailableFields] = useState<string[]>([]);
  const [conditionOperators, setConditionOperators] = useState<string[]>([]);
  const [actionTypes, setActionTypes] = useState<string[]>([]);
  const [isLoading, setIsLoading] = useState(true);
  const [isRuleDialogOpen, setIsRuleDialogOpen] = useState(false);
  const [editingRule, setEditingRule] = useState<WorkflowRule | null>(null);
  const [viewMode, setViewMode] = useState<'visual' | 'code'>('visual');

  useEffect(() => {
    loadRuleBuilderData();
  }, [entityType]);

  const loadRuleBuilderData = async () => {
    try {
      const [fieldsRes, operatorsRes, actionsRes] = await Promise.all([
        fetch(`/api/workflows/rule-builder/fields/${entityType}`),
        fetch('/api/workflows/rule-builder/operators'),
        fetch('/api/workflows/rule-builder/actions')
      ]);

      if (fieldsRes.ok) {
        const fields = await fieldsRes.json();
        setAvailableFields(fields);
      }

      if (operatorsRes.ok) {
        const operators = await operatorsRes.json();
        setConditionOperators(operators);
      }

      if (actionsRes.ok) {
        const actions = await actionsRes.json();
        setActionTypes(actions);
      }
    } catch (error) {
      toast.error('Failed to load rule builder data');
    } finally {
      setIsLoading(false);
    }
  };

  const handleCreateRule = async (ruleData: Omit<WorkflowRule, 'id'>) => {
    try {
      // For now, we'll store rules locally. In a full implementation,
      // this would be saved to the backend
      const newRule: WorkflowRule = {
        ...ruleData,
        id: Date.now().toString(),
      };

      setRules(prev => [...prev, newRule]);
      setIsRuleDialogOpen(false);
      toast.success('Rule created successfully');
      onRuleCreated?.(newRule);
    } catch (error) {
      toast.error('Failed to create rule');
    }
  };

  const handleUpdateRule = async (ruleId: string, ruleData: Omit<WorkflowRule, 'id'>) => {
    try {
      const updatedRule: WorkflowRule = {
        ...ruleData,
        id: ruleId,
      };

      setRules(prev => prev.map(r => r.id === ruleId ? updatedRule : r));
      setIsRuleDialogOpen(false);
      toast.success('Rule updated successfully');
      onRuleUpdated?.(updatedRule);
    } catch (error) {
      toast.error('Failed to update rule');
    }
  };

  const handleDeleteRule = (ruleId: string) => {
    setRules(prev => prev.filter(r => r.id !== ruleId));
    toast.success('Rule deleted successfully');
  };

  const validateRule = async (rule: Omit<WorkflowRule, 'id'>) => {
    const errors: string[] = [];

    if (!rule.name.trim()) {
      errors.push('Rule name is required');
    }

    if (rule.conditions.length === 0) {
      errors.push('At least one condition is required');
    }

    if (rule.actions.length === 0) {
      errors.push('At least one action is required');
    }

    // Validate each condition
    for (const condition of rule.conditions) {
      if (!condition.field) {
        errors.push('All conditions must have a field selected');
      }
      if (!condition.operator) {
        errors.push('All conditions must have an operator selected');
      }
    }

    // Validate each action
    for (const action of rule.actions) {
      if (!action.actionType) {
        errors.push('All actions must have a type selected');
      }
    }

    return errors;
  };

  if (isLoading) {
    return <div className="flex justify-center p-8">Loading rule builder...</div>;
  }

  return (
    <div className="space-y-6">
      <div className="flex justify-between items-center">
        <div>
          <h3 className="text-lg font-semibold">No-Code Rule Builder</h3>
          <p className="text-sm text-muted-foreground">
            Create business rules with drag-and-drop simplicity for {entityType} workflows
          </p>
        </div>
        <div className="flex gap-2">
          <Button
            size="sm"
            variant={viewMode === 'visual' ? 'default' : 'outline'}
            onClick={() => setViewMode('visual')}
          >
            <Eye className="w-4 h-4 mr-2" />
            Visual
          </Button>
          <Button
            size="sm"
            variant={viewMode === 'code' ? 'default' : 'outline'}
            onClick={() => setViewMode('code')}
          >
            <Code className="w-4 h-4 mr-2" />
            Code
          </Button>
          <Dialog open={isRuleDialogOpen} onOpenChange={setIsRuleDialogOpen}>
            <DialogTrigger asChild>
              <Button size="sm">
                <Plus className="w-4 h-4 mr-2" />
                Create Rule
              </Button>
            </DialogTrigger>
            <DialogContent className="max-w-6xl max-h-[90vh] overflow-y-auto">
              <RuleForm
                entityType={entityType}
                availableFields={availableFields}
                conditionOperators={conditionOperators}
                actionTypes={actionTypes}
                rule={editingRule}
                viewMode={viewMode}
                onSubmit={editingRule ? (data) => handleUpdateRule(editingRule.id, data) : handleCreateRule}
                onCancel={() => {
                  setIsRuleDialogOpen(false);
                  setEditingRule(null);
                }}
                validateRule={validateRule}
              />
            </DialogContent>
          </Dialog>
        </div>
      </div>

      <div className="space-y-4">
        {rules.length === 0 ? (
          <Card>
            <CardContent className="flex flex-col items-center justify-center py-12">
              <Settings className="w-12 h-12 text-muted-foreground mb-4" />
              <h3 className="text-lg font-medium mb-2">No rules created yet</h3>
              <p className="text-muted-foreground text-center mb-4">
                Create your first no-code business rule to automate {entityType} workflows
              </p>
              <Button onClick={() => setIsRuleDialogOpen(true)}>
                <Plus className="w-4 h-4 mr-2" />
                Create Your First Rule
              </Button>
            </CardContent>
          </Card>
        ) : (
          <div className="grid gap-4">
            {rules.map((rule) => (
              <Card key={rule.id}>
                <CardHeader>
                  <div className="flex justify-between items-start">
                    <div>
                      <CardTitle className="text-base">{rule.name}</CardTitle>
                      <CardDescription>{rule.description}</CardDescription>
                    </div>
                    <div className="flex items-center gap-2">
                      <Badge variant={rule.isActive ? "default" : "secondary"}>
                        {rule.isActive ? "Active" : "Inactive"}
                      </Badge>
                      <Button
                        size="sm"
                        variant="outline"
                        onClick={() => {
                          setEditingRule(rule);
                          setIsRuleDialogOpen(true);
                        }}
                      >
                        <Settings className="w-4 h-4" />
                      </Button>
                      <Button
                        size="sm"
                        variant="outline"
                        onClick={() => handleDeleteRule(rule.id)}
                      >
                        <Trash2 className="w-4 h-4" />
                      </Button>
                    </div>
                  </div>
                </CardHeader>
                <CardContent>
                  <div className="space-y-3">
                    <div>
                      <h4 className="text-sm font-medium mb-2">Conditions ({rule.conditions.length})</h4>
                      <div className="space-y-1">
                        {rule.conditions.slice(0, 2).map((condition, index) => (
                          <div key={index} className="text-sm text-muted-foreground bg-muted p-2 rounded">
                            {condition.field} {condition.operator} {condition.value || '[value]'}
                          </div>
                        ))}
                        {rule.conditions.length > 2 && (
                          <div className="text-sm text-muted-foreground">
                            +{rule.conditions.length - 2} more conditions
                          </div>
                        )}
                      </div>
                    </div>
                    <div>
                      <h4 className="text-sm font-medium mb-2">Actions ({rule.actions.length})</h4>
                      <div className="space-y-1">
                        {rule.actions.slice(0, 2).map((action, index) => (
                          <div key={index} className="text-sm text-muted-foreground bg-muted p-2 rounded">
                            {action.actionType}
                          </div>
                        ))}
                        {rule.actions.length > 2 && (
                          <div className="text-sm text-muted-foreground">
                            +{rule.actions.length - 2} more actions
                          </div>
                        )}
                      </div>
                    </div>
                  </div>
                </CardContent>
              </Card>
            ))}
          </div>
        )}
      </div>
    </div>
  );
}

interface RuleFormProps {
  entityType: string;
  availableFields: string[];
  conditionOperators: string[];
  actionTypes: string[];
  rule?: WorkflowRule | null;
  viewMode: 'visual' | 'code';
  onSubmit: (rule: Omit<WorkflowRule, 'id'>) => void;
  onCancel: () => void;
  validateRule: (rule: Omit<WorkflowRule, 'id'>) => Promise<string[]>;
}

function RuleForm({
  entityType,
  availableFields,
  conditionOperators,
  actionTypes,
  rule,
  viewMode,
  onSubmit,
  onCancel,
  validateRule
}: RuleFormProps) {
  const [formData, setFormData] = useState<Omit<WorkflowRule, 'id'>>({
    name: rule?.name || '',
    description: rule?.description || '',
    conditions: rule?.conditions || [{ id: '1', field: '', operator: '', value: '', entityType }],
    actions: rule?.actions || [{ id: '1', actionType: '', parameters: {} }],
    isActive: rule?.isActive ?? true,
  });

  const [validationErrors, setValidationErrors] = useState<string[]>([]);
  const [isValidating, setIsValidating] = useState(false);

  const addCondition = () => {
    const newCondition: RuleCondition = {
      id: Date.now().toString(),
      field: '',
      operator: '',
      value: '',
      entityType,
    };
    setFormData(prev => ({
      ...prev,
      conditions: [...prev.conditions, newCondition],
    }));
  };

  const removeCondition = (conditionId: string) => {
    setFormData(prev => ({
      ...prev,
      conditions: prev.conditions.filter(c => c.id !== conditionId),
    }));
  };

  const updateCondition = (conditionId: string, updates: Partial<RuleCondition>) => {
    setFormData(prev => ({
      ...prev,
      conditions: prev.conditions.map(c =>
        c.id === conditionId ? { ...c, ...updates } : c
      ),
    }));
  };

  const addAction = () => {
    const newAction: RuleAction = {
      id: Date.now().toString(),
      actionType: '',
      parameters: {},
    };
    setFormData(prev => ({
      ...prev,
      actions: [...prev.actions, newAction],
    }));
  };

  const removeAction = (actionId: string) => {
    setFormData(prev => ({
      ...prev,
      actions: prev.actions.filter(a => a.id !== actionId),
    }));
  };

  const updateAction = (actionId: string, updates: Partial<RuleAction>) => {
    setFormData(prev => ({
      ...prev,
      actions: prev.actions.map(a =>
        a.id === actionId ? { ...a, ...updates } : a
      ),
    }));
  };

  const onDragEnd = (result: DropResult) => {
    if (!result.destination) return;

    const { source, destination, type } = result;

    if (type === 'condition') {
      const newConditions = Array.from(formData.conditions);
      const [reorderedItem] = newConditions.splice(source.index, 1);
      newConditions.splice(destination.index, 0, reorderedItem);

      setFormData(prev => ({ ...prev, conditions: newConditions }));
    } else if (type === 'action') {
      const newActions = Array.from(formData.actions);
      const [reorderedItem] = newActions.splice(source.index, 1);
      newActions.splice(destination.index, 0, reorderedItem);

      setFormData(prev => ({ ...prev, actions: newActions }));
    }
  };

  const handleSubmit = async (e: React.FormEvent) => {
    e.preventDefault();
    setIsValidating(true);

    try {
      const errors = await validateRule(formData);
      setValidationErrors(errors);

      if (errors.length === 0) {
        onSubmit(formData);
      }
    } catch (error) {
      toast.error('Validation failed');
    } finally {
      setIsValidating(false);
    }
  };

  return (
    <form onSubmit={handleSubmit}>
      <DialogHeader>
        <DialogTitle>{rule ? 'Edit Rule' : 'Create New Rule'}</DialogTitle>
        <DialogDescription>
          Build a no-code business rule for {entityType} automation
        </DialogDescription>
      </DialogHeader>

      <div className="space-y-6 py-4">
        {/* Basic Information */}
        <div className="grid grid-cols-2 gap-4">
          <div className="space-y-2">
            <Label htmlFor="ruleName">Rule Name</Label>
            <Input
              id="ruleName"
              value={formData.name}
              onChange={(e) => setFormData(prev => ({ ...prev, name: e.target.value }))}
              placeholder="e.g., High Value Payment Approval"
            />
          </div>
          <div className="space-y-2">
            <Label htmlFor="entityType">Entity Type</Label>
            <Input id="entityType" value={entityType} disabled />
          </div>
        </div>

        <div className="space-y-2">
          <Label htmlFor="ruleDescription">Description</Label>
          <Textarea
            id="ruleDescription"
            value={formData.description}
            onChange={(e) => setFormData(prev => ({ ...prev, description: e.target.value }))}
            placeholder="Describe what this rule does..."
            rows={2}
          />
        </div>

        <Tabs value={viewMode} onValueChange={(value) => {}}>
          <TabsList className="grid w-full grid-cols-2">
            <TabsTrigger value="visual">Visual Builder</TabsTrigger>
            <TabsTrigger value="code">Code View</TabsTrigger>
          </TabsList>

          <TabsContent value="visual" className="space-y-6">
            <DragDropContext onDragEnd={onDragEnd}>
              {/* Conditions */}
              <div className="space-y-4">
                <div className="flex justify-between items-center">
                  <h3 className="text-lg font-medium">When these conditions are met...</h3>
                  <Button type="button" size="sm" variant="outline" onClick={addCondition}>
                    <Plus className="w-4 h-4 mr-2" />
                    Add Condition
                  </Button>
                </div>

                <Droppable droppableId="conditions" type="condition">
                  {(provided) => (
                    <div {...provided.droppableProps} ref={provided.innerRef} className="space-y-3">
                      {formData.conditions.map((condition, index) => (
                        <Draggable key={condition.id} draggableId={condition.id} index={index}>
                          {(provided) => (
                            <Card
                              ref={provided.innerRef}
                              {...provided.draggableProps}
                              className="border-2 border-dashed border-muted"
                            >
                              <CardContent className="pt-4">
                                <div className="flex items-center gap-3">
                                  <div {...provided.dragHandleProps}>
                                    <GripVertical className="w-5 h-5 text-muted-foreground cursor-grab" />
                                  </div>
                                  <div className="flex-1 grid grid-cols-4 gap-2">
                                    <Select
                                      value={condition.field}
                                      onValueChange={(value: string) => updateCondition(condition.id, { field: value })}
                                    >
                                      <SelectTrigger>
                                        <SelectValue placeholder="Field" />
                                      </SelectTrigger>
                                      <SelectContent>
                                        {availableFields.map((field) => (
                                          <SelectItem key={field} value={field}>
                                            {field}
                                          </SelectItem>
                                        ))}
                                      </SelectContent>
                                    </Select>

                                    <Select
                                      value={condition.operator}
                                      onValueChange={(value: string) => updateCondition(condition.id, { operator: value })}
                                    >
                                      <SelectTrigger>
                                        <SelectValue placeholder="Operator" />
                                      </SelectTrigger>
                                      <SelectContent>
                                        {conditionOperators.map((op) => (
                                          <SelectItem key={op} value={op}>
                                            {op}
                                          </SelectItem>
                                        ))}
                                      </SelectContent>
                                    </Select>

                                    <Input
                                      value={condition.value}
                                      onChange={(e) => updateCondition(condition.id, { value: e.target.value })}
                                      placeholder="Value"
                                    />

                                    <Button
                                      type="button"
                                      size="sm"
                                      variant="outline"
                                      onClick={() => removeCondition(condition.id)}
                                      disabled={formData.conditions.length === 1}
                                    >
                                      <Trash2 className="w-4 h-4" />
                                    </Button>
                                  </div>
                                </div>
                              </CardContent>
                            </Card>
                          )}
                        </Draggable>
                      ))}
                      {provided.placeholder}
                    </div>
                  )}
                </Droppable>
              </div>

              {/* Actions */}
              <div className="space-y-4">
                <div className="flex justify-between items-center">
                  <h3 className="text-lg font-medium">Then perform these actions...</h3>
                  <Button type="button" size="sm" variant="outline" onClick={addAction}>
                    <Plus className="w-4 h-4 mr-2" />
                    Add Action
                  </Button>
                </div>

                <Droppable droppableId="actions" type="action">
                  {(provided) => (
                    <div {...provided.droppableProps} ref={provided.innerRef} className="space-y-3">
                      {formData.actions.map((action, index) => (
                        <Draggable key={action.id} draggableId={action.id} index={index}>
                          {(provided) => (
                            <Card
                              ref={provided.innerRef}
                              {...provided.draggableProps}
                              className="border-2 border-dashed border-muted"
                            >
                              <CardContent className="pt-4">
                                <div className="flex items-center gap-3">
                                  <div {...provided.dragHandleProps}>
                                    <GripVertical className="w-5 h-5 text-muted-foreground cursor-grab" />
                                  </div>
                                  <div className="flex-1 space-y-3">
                                    <Select
                                      value={action.actionType}
                                      onValueChange={(value: string) => updateAction(action.id, { actionType: value })}
                                    >
                                      <SelectTrigger>
                                        <SelectValue placeholder="Select action" />
                                      </SelectTrigger>
                                      <SelectContent>
                                        {actionTypes.map((type) => (
                                          <SelectItem key={type} value={type}>
                                            {type}
                                          </SelectItem>
                                        ))}
                                      </SelectContent>
                                    </Select>

                                    {action.actionType && (
                                      <Textarea
                                        value={JSON.stringify(action.parameters, null, 2)}
                                        onChange={(e) => {
                                          try {
                                            const params = JSON.parse(e.target.value);
                                            updateAction(action.id, { parameters: params });
                                          } catch {
                                            // Invalid JSON, ignore for now
                                          }
                                        }}
                                        placeholder='{"key": "value"}'
                                        rows={3}
                                      />
                                    )}
                                  </div>
                                  <Button
                                    type="button"
                                    size="sm"
                                    variant="outline"
                                    onClick={() => removeAction(action.id)}
                                    disabled={formData.actions.length === 1}
                                  >
                                    <Trash2 className="w-4 h-4" />
                                  </Button>
                                </div>
                              </CardContent>
                            </Card>
                          )}
                        </Draggable>
                      ))}
                      {provided.placeholder}
                    </div>
                  )}
                </Droppable>
              </div>
            </DragDropContext>
          </TabsContent>

          <TabsContent value="code" className="space-y-4">
            <div className="space-y-4">
              <div>
                <Label>Conditions (JSON)</Label>
                <Textarea
                  value={JSON.stringify(formData.conditions, null, 2)}
                  onChange={(e) => {
                    try {
                      const conditions = JSON.parse(e.target.value);
                      setFormData(prev => ({ ...prev, conditions }));
                    } catch {
                      // Invalid JSON, ignore
                    }
                  }}
                  rows={10}
                  className="font-mono text-sm"
                />
              </div>
              <div>
                <Label>Actions (JSON)</Label>
                <Textarea
                  value={JSON.stringify(formData.actions, null, 2)}
                  onChange={(e) => {
                    try {
                      const actions = JSON.parse(e.target.value);
                      setFormData(prev => ({ ...prev, actions }));
                    } catch {
                      // Invalid JSON, ignore
                    }
                  }}
                  rows={10}
                  className="font-mono text-sm"
                />
              </div>
            </div>
          </TabsContent>
        </Tabs>

        {/* Validation Errors */}
        {validationErrors.length > 0 && (
          <div className="space-y-2">
            <div className="flex items-center gap-2 text-destructive">
              <XCircle className="w-4 h-4" />
              <span className="font-medium">Validation Errors:</span>
            </div>
            <ul className="list-disc list-inside text-sm text-destructive space-y-1">
              {validationErrors.map((error, index) => (
                <li key={index}>{error}</li>
              ))}
            </ul>
          </div>
        )}
      </div>

      <div className="flex justify-end gap-2">
        <Button type="button" variant="outline" onClick={onCancel}>
          Cancel
        </Button>
        <Button type="submit" disabled={isValidating}>
          {isValidating ? 'Validating...' : (rule ? 'Update Rule' : 'Create Rule')}
        </Button>
      </div>
    </form>
  );
}