'use client';

import React, { useState, useEffect } from 'react';
import { useForm } from 'react-hook-form';
import { zodResolver } from '@hookform/resolvers/zod';
import * as z from 'zod';
import { Card, CardContent, CardDescription, CardHeader, CardTitle } from '@/components/ui/card';
import { Button } from '@/components/ui/button';
import { Input } from '@/components/ui/input';
import { Label } from '@/components/ui/label';
import { Select, SelectContent, SelectItem, SelectTrigger, SelectValue } from '@/components/ui/select';
import { Checkbox } from '@/components/ui/checkbox';
import { Textarea } from '@/components/ui/textarea';
import { Badge } from '@/components/ui/badge';
import { Alert, AlertDescription } from '@/components/ui/alert';
import { Separator } from '@/components/ui/separator';
import { DatePicker } from '@/components/ui/date-picker';
import { 
  CalendarIcon, 
  FileText, 
  Download, 
  Settings, 
  Info,
  AlertCircle,
  Clock,
  Users,
  DollarSign,
  Shield,
  BarChart3,
  TrendingUp,
  FileSpreadsheet,
  PieChart
} from 'lucide-react';
import { format } from 'date-fns';
import { cn } from '@/lib/utils';
import { useToast } from '@/components/ui/use-toast';
import { reportService, GenerateReportRequest, ReportRequest, ReportTemplate } from '@/lib/services/report-service';
import ClientSearchSelect from '@/components/client-search-select';

const reportGenerationSchema = z.object({
  reportType: z.string().min(1, 'Report type is required'),
  title: z.string().min(1, 'Title is required'),
  description: z.string().optional(),
  format: z.enum(['PDF', 'Excel', 'CSV']),
  dateFrom: z.date().optional(),
  dateTo: z.date().optional(),
  clientId: z.string().optional(),
  includeCharts: z.boolean().default(true),
  includeDetails: z.boolean().default(true),
  templateId: z.string().optional(),
  parameters: z.record(z.any()).default({}),
});

type ReportGenerationFormData = z.infer<typeof reportGenerationSchema>;

const reportTypes: Array<{
  value: string;
  label: string;
  description: string;
  icon: any;
  category: string;
  estimatedDuration: number;
  features: string[];
  formats: string[];
  requiredFields: string[];
  parameters: Array<{
    name: string;
    type: string;
    label: string;
    default?: any;
    options?: string[];
  }>;
}> = [
  {
    value: 'TaxCompliance',
    label: 'Tax Compliance Report',
    description: 'Comprehensive tax compliance status with Finance Act 2025 requirements',
    icon: FileText,
    category: 'Tax',
    estimatedDuration: 180,
    features: ['Compliance scoring', 'Deadline tracking', 'Penalty analysis', 'Risk assessment'],
    formats: ['PDF', 'Excel'],
    requiredFields: ['clientId'] as string[],
    parameters: [
      { name: 'includeHistory', type: 'boolean', label: 'Include Historical Data', default: false },
      { name: 'riskAssessment', type: 'boolean', label: 'Include Risk Assessment', default: true },
      { name: 'penaltyAnalysis', type: 'boolean', label: 'Include Penalty Analysis', default: true }
    ]
  },
  {
    value: 'PaymentHistory',
    label: 'Payment History Report',
    description: 'Complete payment transaction history with mobile money integration',
    icon: DollarSign,
    category: 'Financial',
    estimatedDuration: 120,
    features: ['Transaction details', 'Payment methods', 'Reconciliation', 'Trends analysis'],
    formats: ['PDF', 'Excel', 'CSV'],
    requiredFields: [],
    parameters: [
      { name: 'paymentMethod', type: 'select', label: 'Payment Method', options: ['All', 'Orange Money', 'Africell Money', 'Bank Transfer'] },
      { name: 'includeFailures', type: 'boolean', label: 'Include Failed Transactions', default: false },
      { name: 'groupByClient', type: 'boolean', label: 'Group by Client', default: true }
    ]
  },
  {
    value: 'ClientActivity',
    label: 'Client Activity Report',
    description: 'Client engagement and activity analysis with compliance metrics',
    icon: Users,
    category: 'Client Management',
    estimatedDuration: 150,
    features: ['Activity tracking', 'Login analytics', 'Document uploads', 'Filing status'],
    formats: ['PDF', 'Excel'],
    requiredFields: [],
    parameters: [
      { name: 'includeDormant', type: 'boolean', label: 'Include Dormant Clients', default: false },
      { name: 'activityThreshold', type: 'number', label: 'Activity Threshold (days)', default: 30 },
      { name: 'detailLevel', type: 'select', label: 'Detail Level', options: ['Summary', 'Detailed', 'Complete'] }
    ]
  },
  {
    value: 'KPISummary',
    label: 'KPI Summary Report',
    description: 'Key Performance Indicators dashboard with business metrics',
    icon: BarChart3,
    category: 'Analytics',
    estimatedDuration: 90,
    features: ['Revenue metrics', 'Client metrics', 'Compliance metrics', 'Trend analysis'],
    formats: ['PDF', 'Excel'],
    requiredFields: [],
    parameters: [
      { name: 'includeTargets', type: 'boolean', label: 'Include Targets', default: true },
      { name: 'compareWithPrevious', type: 'boolean', label: 'Compare with Previous Period', default: true },
      { name: 'breakdownLevel', type: 'select', label: 'Breakdown Level', options: ['Monthly', 'Quarterly', 'Yearly'] }
    ]
  },
  {
    value: 'PenaltyAnalysis',
    label: 'Penalty Analysis Report',
    description: 'Detailed penalty analysis with Finance Act 2025 calculations',
    icon: AlertCircle,
    category: 'Compliance',
    estimatedDuration: 120,
    features: ['Penalty calculations', 'Late filing analysis', 'Interest calculations', 'Mitigation strategies'],
    formats: ['PDF', 'Excel'],
    requiredFields: [],
    parameters: [
      { name: 'severityLevel', type: 'select', label: 'Severity Level', options: ['All', 'Minor', 'Major', 'Critical'] },
      { name: 'includeProjections', type: 'boolean', label: 'Include Projections', default: true },
      { name: 'mitigation', type: 'boolean', label: 'Include Mitigation Strategies', default: true }
    ]
  },
  {
    value: 'AuditTrail',
    label: 'Audit Trail Report',
    description: 'System audit trail with security and compliance monitoring',
    icon: Shield,
    category: 'Security',
    estimatedDuration: 200,
    features: ['User activities', 'Data changes', 'Access logs', 'Security events'],
    formats: ['PDF', 'Excel', 'CSV'],
    requiredFields: [],
    parameters: [
      { name: 'userId', type: 'select', label: 'User', options: ['All Users'] },
      { name: 'actionType', type: 'select', label: 'Action Type', options: ['All', 'Login', 'Data Modification', 'File Access'] },
      { name: 'riskLevel', type: 'select', label: 'Risk Level', options: ['All', 'Low', 'Medium', 'High', 'Critical'] }
    ]
  },
];

interface ReportGeneratorProps {
  onReportGenerated?: (report: ReportRequest) => void;
  templates?: ReportTemplate[];
  initialType?: string;
  initialParameters?: Record<string, any>;
}

export default function ReportGenerator({ 
  onReportGenerated, 
  templates = [], 
  initialType,
  initialParameters
}: ReportGeneratorProps) {
  const { toast } = useToast();
  const [loading, setLoading] = useState(false);
  const [selectedType, setSelectedType] = useState<typeof reportTypes[0] | null>(
    initialType ? reportTypes.find(t => t.value === initialType) || null : null
  );
  const [showAdvanced, setShowAdvanced] = useState(false);
  const [previewData, setPreviewData] = useState<any>(null);

  const form = useForm<ReportGenerationFormData>({
    resolver: zodResolver(reportGenerationSchema),
    defaultValues: {
      format: 'PDF',
      includeCharts: true,
      includeDetails: true,
      parameters: {},
    },
  });

  useEffect(() => {
    if (selectedType) {
      form.setValue('reportType', selectedType.value);
      form.setValue('title', `${selectedType.label} - ${format(new Date(), 'MMM yyyy')}`);
      form.setValue('format', selectedType.formats.includes('PDF') ? 'PDF' : selectedType.formats[0] as any);
      
      // Set default parameters
      const defaultParams = selectedType.parameters.reduce((acc, param) => {
        if (param.default !== undefined) {
          acc[param.name] = param.default;
        }
        return acc;
      }, {} as Record<string, any>);

      // Merge in any initialParameters passed from a template selection
      const merged = { ...defaultParams, ...(initialParameters || {}) };
      form.setValue('parameters', merged);
    }
  }, [selectedType, form, initialParameters]);

  const onSubmit = async (data: ReportGenerationFormData) => {
    if (!selectedType) return;

    setLoading(true);
    try {
      const request: GenerateReportRequest = {
        reportType: data.reportType,
        parameters: {
          ...data.parameters,
          clientId: data.clientId || undefined,
          dateFrom: data.dateFrom ? data.dateFrom.toISOString() : undefined,
          dateTo: data.dateTo ? data.dateTo.toISOString() : undefined,
          includeDetails: data.includeDetails,
          includeCharts: data.includeCharts,
          format: data.format,
          title: data.title,
          description: data.description,
        },
      };

      const response = await reportService.generateReport(request);
      
      if (response.success && response.data) {
        onReportGenerated?.(response.data);
        toast({
          title: 'Report Generation Started',
          description: `Your ${selectedType.label} is being generated and will be available shortly.`,
        });
        
        // Reset form
        form.reset();
        setSelectedType(null);
      } else {
        throw new Error(response.error || 'Failed to generate report');
      }
    } catch (error) {
      toast({
        variant: 'destructive',
        title: 'Generation Failed',
        description: error instanceof Error ? error.message : 'Failed to generate report',
      });
    } finally {
      setLoading(false);
    }
  };

  const loadPreviewData = async () => {
    if (!selectedType) return;
    
    try {
      // This would call an API to get preview data
      setPreviewData({
        recordCount: Math.floor(Math.random() * 1000) + 100,
        estimatedSize: `${Math.floor(Math.random() * 5) + 1}.${Math.floor(Math.random() * 9)}MB`,
        estimatedTime: `${selectedType.estimatedDuration} seconds`,
      });
    } catch (error) {
      console.error('Failed to load preview data:', error);
    }
  };

  useEffect(() => {
    if (selectedType) {
      loadPreviewData();
    }
  }, [selectedType, form.watch('dateFrom'), form.watch('dateTo')]);

  const formatDuration = (seconds: number) => {
    if (seconds < 60) return `${seconds}s`;
    const minutes = Math.floor(seconds / 60);
    const remainingSeconds = seconds % 60;
    return `${minutes}m ${remainingSeconds}s`;
  };

  return (
    <div className="space-y-6">
      {/* Report Type Selection */}
      {!selectedType ? (
        <Card>
          <CardHeader>
            <CardTitle>Select Report Type</CardTitle>
            <CardDescription>
              Choose the type of report you want to generate
            </CardDescription>
          </CardHeader>
          <CardContent>
            <div className="grid gap-4 md:grid-cols-2 lg:grid-cols-3">
              {reportTypes.map((type) => {
                const Icon = type.icon;
                return (
                  <Card 
                    key={type.value} 
                    className="cursor-pointer hover:shadow-md transition-shadow border-2 hover:border-sierra-blue-200"
                    onClick={() => setSelectedType(type)}
                  >
                    <CardContent className="p-4">
                      <div className="flex items-start gap-3 mb-3">
                        <div className="p-2 bg-sierra-blue-50 rounded-lg">
                          <Icon className="h-6 w-6 text-sierra-blue-600" />
                        </div>
                        <div className="flex-1">
                          <h4 className="font-medium">{type.label}</h4>
                          <Badge variant="outline" className="mt-1">
                            {type.category}
                          </Badge>
                        </div>
                      </div>
                      <p className="text-sm text-muted-foreground mb-3">
                        {type.description}
                      </p>
                      <div className="flex items-center justify-between text-xs text-muted-foreground">
                        <span className="flex items-center gap-1">
                          <Clock className="h-3 w-3" />
                          ~{formatDuration(type.estimatedDuration)}
                        </span>
                        <span>{type.formats.join(', ')}</span>
                      </div>
                      <div className="mt-3">
                        <div className="flex flex-wrap gap-1">
                          {type.features.slice(0, 3).map((feature, index) => (
                            <Badge key={index} variant="secondary" className="text-xs">
                              {feature}
                            </Badge>
                          ))}
                          {type.features.length > 3 && (
                            <Badge variant="secondary" className="text-xs">
                              +{type.features.length - 3}
                            </Badge>
                          )}
                        </div>
                      </div>
                    </CardContent>
                  </Card>
                );
              })}
            </div>
          </CardContent>
        </Card>
      ) : (
        <Card>
          <CardHeader>
            <div className="flex items-center justify-between">
              <div className="flex items-center gap-3">
                <selectedType.icon className="h-6 w-6 text-sierra-blue-600" />
                <div>
                  <CardTitle>{selectedType.label}</CardTitle>
                  <CardDescription>{selectedType.description}</CardDescription>
                </div>
              </div>
              <Button variant="outline" onClick={() => setSelectedType(null)}>
                Change Type
              </Button>
            </div>
          </CardHeader>
          <CardContent>
            <form onSubmit={form.handleSubmit(onSubmit)} className="space-y-6">
              {/* Basic Configuration */}
              <div className="grid gap-4 md:grid-cols-2">
                <div className="space-y-2">
                  <Label htmlFor="title">Report Title</Label>
                  <Input
                    id="title"
                    placeholder="Enter report title"
                    {...form.register('title')}
                  />
                  {form.formState.errors.title && (
                    <p className="text-sm text-red-600">{form.formState.errors.title.message}</p>
                  )}
                </div>

                <div className="space-y-2">
                  <Label htmlFor="format">Output Format</Label>
                  <Select
                    value={form.watch('format')}
                    onValueChange={(value) => form.setValue('format', value as any)}
                  >
                    <SelectTrigger>
                      <SelectValue />
                    </SelectTrigger>
                    <SelectContent>
                      {selectedType.formats.map((format) => (
                        <SelectItem key={format} value={format}>
                          {format === 'PDF' && <FileText className="mr-2 h-4 w-4" />}
                          {format === 'Excel' && <FileSpreadsheet className="mr-2 h-4 w-4" />}
                          {format === 'CSV' && <FileText className="mr-2 h-4 w-4" />}
                          {format}
                        </SelectItem>
                      ))}
                    </SelectContent>
                  </Select>
                </div>
              </div>

              <div className="space-y-2">
                <Label htmlFor="description">Description (Optional)</Label>
                <Textarea
                  id="description"
                  placeholder="Enter report description"
                  {...form.register('description')}
                />
              </div>

              {/* Date Range (optional) */}
              <div className="grid gap-4 md:grid-cols-2">
                <div className="space-y-2">
                  <Label>From Date (optional)</Label>
                  <DatePicker
                    value={form.watch('dateFrom') ?? null}
                    onChange={(d) => form.setValue('dateFrom', d || undefined)}
                    placeholder="Pick start date"
                  />
                </div>
                <div className="space-y-2">
                  <Label>To Date (optional)</Label>
                  <DatePicker
                    value={form.watch('dateTo') ?? null}
                    onChange={(d) => form.setValue('dateTo', d || undefined)}
                    placeholder="Pick end date"
                    minDate={form.watch('dateFrom') ?? undefined}
                  />
                  {form.watch('dateFrom') && form.watch('dateTo') && form.watch('dateTo')! < form.watch('dateFrom')! && (
                    <p className="text-xs text-red-600">End date should be after start date.</p>
                  )}
                </div>
              </div>

              {/* Client Selection (if required) */}
              {selectedType.requiredFields.includes('clientId') && (
                <div className="space-y-2">
                  <Label htmlFor="clientId">Client (Optional)</Label>
                  <ClientSearchSelect
                    value={form.watch('clientId') || ''}
                    onChange={(value) => form.setValue('clientId', value || undefined)}
                    placeholder="Select client (leave empty for all clients)"
                    allowEmpty
                    emptyLabel="All Clients"
                  />
                </div>
              )}

              {/* Report Options */}
              <div className="space-y-4">
                <h4 className="font-medium">Report Options</h4>
                <div className="grid gap-4 md:grid-cols-2">
                  <div className="flex items-center space-x-2">
                    <Checkbox
                      id="includeCharts"
                      checked={form.watch('includeCharts')}
                      onCheckedChange={(checked) => form.setValue('includeCharts', !!checked)}
                    />
                    <Label htmlFor="includeCharts">Include Charts and Graphs</Label>
                  </div>
                  <div className="flex items-center space-x-2">
                    <Checkbox
                      id="includeDetails"
                      checked={form.watch('includeDetails')}
                      onCheckedChange={(checked) => form.setValue('includeDetails', !!checked)}
                    />
                    <Label htmlFor="includeDetails">Include Detailed Data</Label>
                  </div>
                </div>
              </div>

              {/* Advanced Parameters */}
              <div className="space-y-4">
                <div className="flex items-center justify-between">
                  <h4 className="font-medium">Advanced Options</h4>
                  <Button
                    type="button"
                    variant="ghost"
                    size="sm"
                    onClick={() => setShowAdvanced(!showAdvanced)}
                  >
                    <Settings className="mr-2 h-4 w-4" />
                    {showAdvanced ? 'Hide' : 'Show'} Advanced
                  </Button>
                </div>

                {showAdvanced && (
                  <div className="space-y-4 p-4 border rounded-lg bg-gray-50">
                    {selectedType.parameters.map((param) => (
                      <div key={param.name} className="space-y-2">
                        <Label>{param.label}</Label>
                        {param.type === 'boolean' ? (
                          <div className="flex items-center space-x-2">
                            <Checkbox
                              checked={form.watch('parameters')?.[param.name] || param.default || false}
                              onCheckedChange={(checked) => {
                                const currentParams = form.watch('parameters') || {};
                                form.setValue('parameters', {
                                  ...currentParams,
                                  [param.name]: !!checked
                                });
                              }}
                            />
                            <span className="text-sm">{param.label}</span>
                          </div>
                        ) : param.type === 'select' && param.options ? (
                          <Select
                            value={form.watch('parameters')?.[param.name] || param.default || ''}
                            onValueChange={(value) => {
                              const currentParams = form.watch('parameters') || {};
                              form.setValue('parameters', {
                                ...currentParams,
                                [param.name]: value
                              });
                            }}
                          >
                            <SelectTrigger>
                              <SelectValue />
                            </SelectTrigger>
                            <SelectContent>
                              {param.options.map((option) => (
                                <SelectItem key={option} value={option}>
                                  {option}
                                </SelectItem>
                              ))}
                            </SelectContent>
                          </Select>
                        ) : param.type === 'number' ? (
                          <Input
                            type="number"
                            value={form.watch('parameters')?.[param.name] || param.default || ''}
                            onChange={(e) => {
                              const currentParams = form.watch('parameters') || {};
                              form.setValue('parameters', {
                                ...currentParams,
                                [param.name]: parseInt(e.target.value) || 0
                              });
                            }}
                          />
                        ) : (
                          <Input
                            value={form.watch('parameters')?.[param.name] || param.default || ''}
                            onChange={(e) => {
                              const currentParams = form.watch('parameters') || {};
                              form.setValue('parameters', {
                                ...currentParams,
                                [param.name]: e.target.value
                              });
                            }}
                          />
                        )}
                      </div>
                    ))}
                  </div>
                )}
              </div>

              {/* Preview Information */}
              {previewData && (
                <Alert>
                  <Info className="h-4 w-4" />
                  <AlertDescription>
                    <div className="grid grid-cols-3 gap-4 text-sm">
                      <div>
                        <strong>Records:</strong> {previewData.recordCount}
                      </div>
                      <div>
                        <strong>Est. Size:</strong> {previewData.estimatedSize}
                      </div>
                      <div>
                        <strong>Est. Time:</strong> {previewData.estimatedTime}
                      </div>
                    </div>
                  </AlertDescription>
                </Alert>
              )}

              <Separator />

              {/* Actions */}
              <div className="flex justify-end gap-2">
                <Button 
                  type="button" 
                  variant="outline" 
                  onClick={() => setSelectedType(null)}
                >
                  Cancel
                </Button>
                <Button type="submit" disabled={loading}>
                  {loading ? (
                    <>
                      <Clock className="mr-2 h-4 w-4 animate-spin" />
                      Generating...
                    </>
                  ) : (
                    <>
                      <Download className="mr-2 h-4 w-4" />
                      Generate Report
                    </>
                  )}
                </Button>
              </div>
            </form>
          </CardContent>
        </Card>
      )}
    </div>
  );
}