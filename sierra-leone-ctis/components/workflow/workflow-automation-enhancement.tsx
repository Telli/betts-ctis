'use client';

import React, { useState, useEffect, useCallback } from 'react';
import { Card, CardContent, CardDescription, CardHeader, CardTitle } from '@/components/ui/card';
import { Tabs, TabsContent, TabsList, TabsTrigger } from '@/components/ui/tabs';
import { Badge } from '@/components/ui/badge';
import { Settings, BarChart3, Activity, CheckCircle, AlertCircle, Clock, Users } from 'lucide-react';
import { WorkflowTriggerManager } from './no-code-rule-builder';
import { WorkflowTemplateManager } from './workflow-templates';
import { WorkflowExecutionMonitor } from './workflow-execution-monitor';
import { toast } from 'sonner';
import {
  workflowService,
  WorkflowDefinition,
  WorkflowInstance,
  WorkflowMetrics
} from '@/lib/services/workflow-service';

interface WorkflowAutomationProps {
  className?: string;
}

const formatStatusLabel = (status: number | string): string => {
  if (typeof status === 'string') return status;

  switch (status) {
    case 0:
      return 'Not Started';
    case 1:
      return 'Running';
    case 2:
      return 'Waiting For Approval';
    case 3:
      return 'Paused';
    case 4:
      return 'Completed';
    case 5:
      return 'Failed';
    case 6:
      return 'Cancelled';
    default:
      return 'Unknown';
  }
};

export function WorkflowAutomationEnhancement({ className }: WorkflowAutomationProps) {
  const [activeTab, setActiveTab] = useState('rules');
  const [definitions, setDefinitions] = useState<WorkflowDefinition[]>([]);
  const [metrics, setMetrics] = useState<WorkflowMetrics | null>(null);
  const [recentExecutions, setRecentExecutions] = useState<WorkflowInstance[]>([]);
  const [pendingApprovals, setPendingApprovals] = useState<number>(0);
  const [isLoading, setIsLoading] = useState(true);

  const loadDashboard = useCallback(async () => {
    try {
      setIsLoading(true);
      const [defs, metricResult, instances, approvals] = await Promise.all([
        workflowService.getDefinitions(),
        workflowService.getMetrics(),
        workflowService.getInstances(),
        workflowService.getPendingApprovals().catch(() => [])
      ]);

      setDefinitions(defs);
      setMetrics(metricResult);
      setRecentExecutions(instances.slice(0, 5));
      setPendingApprovals(Array.isArray(approvals) ? approvals.length : 0);
    } catch (error) {
      console.error('Failed to load workflow automation data', error);
      toast.error('Unable to load workflow automation data. Please try again.');
    } finally {
      setIsLoading(false);
    }
  }, []);

  useEffect(() => {
    loadDashboard();
  }, [loadDashboard]);

  const handleInstanceStarted = async () => {
    toast.success('Workflow instance started successfully');
    await loadDashboard();
  };

  const handleTriggersUpdated = async () => {
    await loadDashboard();
    toast.success('Workflow triggers updated');
  };

  if (isLoading) {
    return (
      <div className="flex items-center justify-center p-8">
        <div className="text-center">
          <div className="animate-spin rounded-full h-8 w-8 border-b-2 border-blue-600 mx-auto mb-2" />
          <p className="text-sm text-gray-600">Loading workflow automation...</p>
        </div>
      </div>
    );
  }

  const activeCount = definitions.filter((definition) => definition.isActive).length;
  const metricsSummary = metrics ?? {
    totalWorkflows: definitions.length,
    activeWorkflows: activeCount,
    totalInstances: recentExecutions.length,
    runningInstances: recentExecutions.filter((instance) => instance.status === 1).length,
    pendingApprovals,
    overallSuccessRate: 0,
    lastUpdated: new Date().toISOString()
  };

  return (
    <div className={`space-y-6 ${className}`}>
      <div className="space-y-6">
        <div>
          <h1 className="text-3xl font-bold text-gray-900">Workflow Automation</h1>
          <p className="text-gray-600 mt-1">
            Manage automation definitions, monitor executions, and review performance metrics in one place.
          </p>
        </div>

        <div className="grid md:grid-cols-4 gap-4">
          <Card>
            <CardContent className="p-4">
              <div className="flex items-center justify-between">
                <div>
                  <p className="text-sm font-medium text-gray-600">Total Workflows</p>
                  <p className="text-2xl font-bold text-gray-900">{definitions.length}</p>
                </div>
                <Settings className="h-8 w-8 text-blue-600" />
              </div>
            </CardContent>
          </Card>

          <Card>
            <CardContent className="p-4">
              <div className="flex items-center justify-between">
                <div>
                  <p className="text-sm font-medium text-gray-600">Active Workflows</p>
                  <p className="text-2xl font-bold text-green-600">{activeCount}</p>
                </div>
                <CheckCircle className="h-8 w-8 text-green-600" />
              </div>
            </CardContent>
          </Card>

          <Card>
            <CardContent className="p-4">
              <div className="flex items-center justify-between">
                <div>
                  <p className="text-sm font-medium text-gray-600">Running Instances</p>
                  <p className="text-2xl font-bold text-blue-600">{metricsSummary.runningInstances}</p>
                </div>
                <Activity className="h-8 w-8 text-blue-600" />
              </div>
            </CardContent>
          </Card>

          <Card>
            <CardContent className="p-4">
              <div className="flex items-center justify-between">
                <div>
                  <p className="text-sm font-medium text-gray-600">Pending Approvals</p>
                  <p className="text-2xl font-bold text-amber-600">{metricsSummary.pendingApprovals ?? pendingApprovals}</p>
                </div>
                <Users className="h-8 w-8 text-amber-600" />
              </div>
            </CardContent>
          </Card>
        </div>
      </div>

      <Tabs value={activeTab} onValueChange={setActiveTab} className="w-full">
        <TabsList className="grid w-full grid-cols-4">
          <TabsTrigger value="rules" className="flex items-center gap-2">
            <Settings className="h-4 w-4" />
            Triggers
          </TabsTrigger>
          <TabsTrigger value="templates" className="flex items-center gap-2">
            <Clock className="h-4 w-4" />
            Definitions
          </TabsTrigger>
          <TabsTrigger value="monitor" className="flex items-center gap-2">
            <Activity className="h-4 w-4" />
            Monitor
          </TabsTrigger>
          <TabsTrigger value="analytics" className="flex items-center gap-2">
            <BarChart3 className="h-4 w-4" />
            Analytics
          </TabsTrigger>
        </TabsList>

        <TabsContent value="rules" className="space-y-6">
          <WorkflowTriggerManager
            definitions={definitions}
            onChange={handleTriggersUpdated}
          />
        </TabsContent>

        <TabsContent value="templates" className="space-y-6">
          <WorkflowTemplateManager
            definitions={definitions}
            onInstanceStarted={handleInstanceStarted}
          />
        </TabsContent>

        <TabsContent value="monitor" className="space-y-6">
          <WorkflowExecutionMonitor
            definitions={definitions}
            onRefresh={loadDashboard}
          />
        </TabsContent>

        <TabsContent value="analytics" className="space-y-6">
          <Card>
            <CardHeader>
              <CardTitle className="flex items-center gap-2">
                <BarChart3 className="h-5 w-5" />
                Workflow Analytics Overview
              </CardTitle>
              <CardDescription>
                Snapshot of recent executions and success trends across all workflows.
              </CardDescription>
            </CardHeader>
            <CardContent className="space-y-6">
              <div className="grid md:grid-cols-3 gap-4">
                <Card className="border-dashed">
                  <CardContent className="p-4">
                    <p className="text-sm font-medium text-gray-600">Total Executions</p>
                    <p className="text-2xl font-bold text-gray-900">{metricsSummary.totalInstances}</p>
                  </CardContent>
                </Card>
                <Card className="border-dashed">
                  <CardContent className="p-4">
                    <p className="text-sm font-medium text-gray-600">Success Rate</p>
                    <div className="flex items-baseline gap-2">
                      <p className="text-2xl font-bold text-gray-900">
                        {metricsSummary.overallSuccessRate ? metricsSummary.overallSuccessRate.toFixed(1) : '0.0'}%
                      </p>
                      <Badge variant="outline">Last updated {new Date(metricsSummary.lastUpdated).toLocaleString()}</Badge>
                    </div>
                  </CardContent>
                </Card>
                <Card className="border-dashed">
                  <CardContent className="p-4">
                    <p className="text-sm font-medium text-gray-600">Active Definitions</p>
                    <p className="text-2xl font-bold text-gray-900">{metricsSummary.activeWorkflows}</p>
                  </CardContent>
                </Card>
              </div>

              <div>
                <h3 className="text-sm font-semibold text-gray-700 mb-3">Recent Workflow Executions</h3>
                {recentExecutions.length === 0 ? (
                  <div className="text-center py-6 text-gray-500">
                    <Clock className="h-8 w-8 mx-auto mb-2 opacity-50" />
                    <p className="text-sm">No recent workflow executions</p>
                  </div>
                ) : (
                  <div className="space-y-2">
                    {recentExecutions.map((execution) => (
                      <div key={execution.id} className="flex items-center justify-between p-3 border rounded-lg">
                        <div>
                          <p className="font-medium">{execution.name}</p>
                          <p className="text-xs text-gray-500">
                            Started {execution.startedAt ? new Date(execution.startedAt).toLocaleString() : new Date(execution.createdAt).toLocaleString()}
                          </p>
                        </div>
                        <Badge
                          variant={
                            execution.status === 4
                              ? 'default'
                              : execution.status === 1
                                ? 'secondary'
                                : execution.status === 5
                                  ? 'destructive'
                                  : 'outline'
                          }
                        >
                          {formatStatusLabel(execution.status)}
                        </Badge>
                      </div>
                    ))}
                  </div>
                )}
              </div>
            </CardContent>
          </Card>
        </TabsContent>
      </Tabs>
    </div>
  );
}