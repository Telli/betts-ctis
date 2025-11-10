$ErrorActionPreference = 'Stop'
$path = 'c:\Users\telli\Desktop\Betts\Betts\sierra-leone-ctis\components\workflow\workflow-builder.tsx'
$content = Get-Content -Raw -LiteralPath $path -Encoding UTF8
$modified = $false

function EnsureImport([string]$t) {
  if ($t -match 'workflow-service') { return $t }
  $anchor = "import { WorkflowExecutionMonitor } from './workflow-execution-monitor';"
  if ($t.Contains($anchor)) {
    $insert = "import { workflowService, type WorkflowDefinition } from '@/lib/services/workflow-service';"
    return $t.Replace($anchor, $anchor + "`r`n" + $insert)
  }
  return $t
}

function EnsureDefinitionsState([string]$t) {
  if ($t -match 'setDefinitions\(') { return $t }
  $needle = 'const [workflows, setWorkflows] = useState<Workflow[]>([]);'
  if ($t.Contains($needle)) {
    $insert = $needle + "`r`n  const [definitions, setDefinitions] = useState<WorkflowDefinition[]>([]);"
    return $t.Replace($needle, $insert)
  }
  return $t
}

function ReplaceRegex([string]$t, [string]$pattern, [string]$replacement) {
  return [regex]::Replace($t, $pattern, $replacement, [System.Text.RegularExpressions.RegexOptions]::Singleline)
}

# Apply import and state
$newContent = EnsureImport $content
if ($newContent -ne $content) { $content = $newContent; $modified = $true }
$newContent = EnsureDefinitionsState $content
if ($newContent -ne $content) { $content = $newContent; $modified = $true }

# Replace functions
$content = ReplaceRegex $content '(?s)const\s+loadWorkflows\s*=\s*async\s*\(\)\s*=>\s*\{.*?\};' @"
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
"@

$content = ReplaceRegex $content '(?s)const\s+loadTemplates\s*=\s*async\s*\(\)\s*=>\s*\{.*?\};' @"
const loadTemplates = async () => {
  try {
    const defs = await workflowService.getDefinitions();
    setTemplates(defs.map(d => ({ id: d.id, name: d.name, description: d.description, type: String(d.type) })));
  } catch (error) {
    toast.error('Failed to load workflow templates');
  }
};
"@

$content = ReplaceRegex $content '(?s)const\s+handleCreateWorkflow\s*=\s*async\s*\(workflowData:\s*Partial<Workflow>\)\s*=>\s*\{.*?\};' @"
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
"@

$content = ReplaceRegex $content '(?s)const\s+handleUpdateWorkflow\s*=\s*async\s*\(workflowId:\s*string,\s*workflowData:\s*Partial<Workflow>\)\s*=>\s*\{.*?\};' @"
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
"@

$content = ReplaceRegex $content '(?s)const\s+handleDeleteWorkflow\s*=\s*async\s*\(workflowId:\s*string\)\s*=>\s*\{.*?\};' @"
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
"@

$content = ReplaceRegex $content '(?s)const\s+handleTriggerWorkflow\s*=\s*async\s*\(workflowId:\s*string\)\s*=>\s*\{.*?\};' @"
const handleTriggerWorkflow = async (workflowId: string) => {
  try {
    await workflowService.startInstance({ workflowId, variables: {} });
    toast.success('Workflow triggered successfully');
  } catch (error) {
    toast.error('Failed to trigger workflow');
  }
};
"@

# Pass definitions into WorkflowTemplateManager
if ($content -match 'definitions=\{\[\]\}') {
  $content = $content -replace 'definitions=\{\[\]\}', 'definitions={definitions}'
  $modified = $true
}

if ($modified) { Set-Content -LiteralPath $path -Value $content -Encoding UTF8 }
