'use client';

import React, { useState } from 'react';
import { Card, CardContent, CardDescription, CardHeader, CardTitle } from '@/components/ui/card';
import { Button } from '@/components/ui/button';
import { Badge } from '@/components/ui/badge';
import { Progress } from '@/components/ui/progress';
import { Alert, AlertDescription } from '@/components/ui/alert';
import { 
  Download, 
  Eye, 
  Trash2, 
  Clock, 
  CheckCircle, 
  XCircle, 
  AlertCircle,
  FileText,
  Loader2,
  Calendar,
  User,
  FileSpreadsheet,
  RefreshCw,
  MoreHorizontal,
  Filter
} from 'lucide-react';
import { format, formatDistanceToNow } from 'date-fns';
import { useToast } from '@/components/ui/use-toast';
import { ReportRequest } from '@/lib/services/report-service';
import {
  DropdownMenu,
  DropdownMenuContent,
  DropdownMenuItem,
  DropdownMenuTrigger,
  DropdownMenuSeparator,
} from '@/components/ui/dropdown-menu';
import {
  Dialog,
  DialogContent,
  DialogDescription,
  DialogHeader,
  DialogTitle,
  DialogTrigger,
} from '@/components/ui/dialog';

interface ReportHistoryProps {
  reports: ReportRequest[];
  loading?: boolean;
  onDownload: (reportId: string, title: string) => void;
  onDelete: (reportId: string) => void;
  onPreview?: (report: ReportRequest) => void;
  onRefresh?: () => void;
}

export default function ReportHistory({
  reports,
  loading = false,
  onDownload,
  onDelete,
  onPreview,
  onRefresh
}: ReportHistoryProps) {
  const { toast } = useToast();
  const [selectedReport, setSelectedReport] = useState<ReportRequest | null>(null);
  const [showDetailsDialog, setShowDetailsDialog] = useState(false);

  const getStatusIcon = (status: string) => {
    switch (status) {
      case 'Completed':
        return <CheckCircle className="h-4 w-4 text-green-600" />;
      case 'Processing':
        return <Loader2 className="h-4 w-4 text-blue-600 animate-spin" />;
      case 'Failed':
        return <XCircle className="h-4 w-4 text-red-600" />;
      case 'Cancelled':
        return <XCircle className="h-4 w-4 text-gray-600" />;
      default:
        return <Clock className="h-4 w-4 text-yellow-600" />;
    }
  };

  const getStatusColor = (status: string) => {
    switch (status) {
      case 'Completed':
        return 'bg-green-100 text-green-800 border-green-200';
      case 'Processing':
        return 'bg-blue-100 text-blue-800 border-blue-200';
      case 'Failed':
        return 'bg-red-100 text-red-800 border-red-200';
      case 'Cancelled':
        return 'bg-gray-100 text-gray-800 border-gray-200';
      default:
        return 'bg-yellow-100 text-yellow-800 border-yellow-200';
    }
  };

  const getFormatIcon = (reportType: string) => {
    return <FileText className="h-4 w-4" />; // Default icon
  };

  const formatFileSize = (bytes?: number) => {
    if (!bytes) return 'Unknown';
    const sizes = ['Bytes', 'KB', 'MB', 'GB'];
    if (bytes === 0) return '0 Bytes';
    const i = Math.floor(Math.log(bytes) / Math.log(1024));
    return Math.round(bytes / Math.pow(1024, i) * 100) / 100 + ' ' + sizes[i];
  };

  const formatDuration = (report: ReportRequest) => {
    if (report.status === 'Processing') {
      const elapsed = Date.now() - new Date(report.createdAt).getTime();
      const estimated = (report.estimatedDuration || 120) * 1000;
      const remaining = Math.max(0, estimated - elapsed);
      const minutes = Math.floor(remaining / (1000 * 60));
      const seconds = Math.floor((remaining % (1000 * 60)) / 1000);
      return minutes > 0 ? `~${minutes}m ${seconds}s remaining` : `~${seconds}s remaining`;
    }
    
    if (report.completedAt) {
      const duration = new Date(report.completedAt).getTime() - new Date(report.createdAt).getTime();
      const minutes = Math.floor(duration / (1000 * 60));
      const seconds = Math.floor((duration % (1000 * 60)) / 1000);
      return minutes > 0 ? `${minutes}m ${seconds}s` : `${seconds}s`;
    }
    
    return 'N/A';
  };

  const handleDelete = async (reportId: string, title: string) => {
    if (confirm(`Are you sure you want to delete "${title}"?`)) {
      try {
        onDelete(reportId);
        toast({
          title: 'Report Deleted',
          description: 'The report has been successfully deleted.',
        });
      } catch (error) {
        toast({
          variant: 'destructive',
          title: 'Delete Failed',
          description: 'Failed to delete the report. Please try again.',
        });
      }
    }
  };

  const showReportDetails = (report: ReportRequest) => {
    setSelectedReport(report);
    setShowDetailsDialog(true);
  };

  if (loading) {
    return (
      <Card>
        <CardContent className="p-8">
          <div className="text-center">
            <Loader2 className="h-8 w-8 animate-spin mx-auto mb-4" />
            <p className="text-muted-foreground">Loading reports...</p>
          </div>
        </CardContent>
      </Card>
    );
  }

  if (reports.length === 0) {
    return (
      <Card>
        <CardContent className="p-8">
          <div className="text-center">
            <FileText className="h-12 w-12 mx-auto mb-4 text-muted-foreground opacity-50" />
            <h3 className="text-lg font-medium mb-2">No Reports Found</h3>
            <p className="text-muted-foreground mb-4">
              You haven't generated any reports yet. Create your first report to get started.
            </p>
            <Button onClick={onRefresh}>
              <RefreshCw className="mr-2 h-4 w-4" />
              Refresh
            </Button>
          </div>
        </CardContent>
      </Card>
    );
  }

  return (
    <div className="space-y-4">
      {/* Header */}
      <div className="flex items-center justify-between">
        <div>
          <h3 className="text-lg font-medium">Report History</h3>
          <p className="text-sm text-muted-foreground">
            {reports.length} report{reports.length !== 1 ? 's' : ''} found
          </p>
        </div>
        {onRefresh && (
          <Button variant="outline" size="sm" onClick={onRefresh}>
            <RefreshCw className="mr-2 h-4 w-4" />
            Refresh
          </Button>
        )}
      </div>

      {/* Reports List */}
      <div className="space-y-3">
        {reports.map((report) => (
          <Card key={report.id} className="hover:shadow-md transition-shadow">
            <CardContent className="p-4">
              <div className="flex items-start justify-between">
                <div className="flex items-start gap-3 flex-1">
                  {/* Status Icon */}
                  <div className="flex-shrink-0 mt-1">
                    {getStatusIcon(report.status)}
                  </div>

                  {/* Report Info */}
                  <div className="flex-1 min-w-0">
                    <div className="flex items-start justify-between mb-2">
                      <div className="flex-1">
                        <h4 className="font-medium truncate">{report.title}</h4>
                        {report.description && (
                          <p className="text-sm text-muted-foreground mt-1">
                            {report.description}
                          </p>
                        )}
                      </div>
                      <Badge className={`ml-2 ${getStatusColor(report.status)}`}>
                        {report.status}
                      </Badge>
                    </div>

                    {/* Progress Bar (for processing reports) */}
                    {report.status === 'Processing' && (
                      <div className="mb-3">
                        <div className="flex items-center justify-between text-xs text-muted-foreground mb-1">
                          <span>Processing...</span>
                          <span>{report.progress || 0}%</span>
                        </div>
                        <Progress value={report.progress || 0} className="h-2" />
                      </div>
                    )}

                    {/* Metadata */}
                    <div className="flex items-center gap-4 text-xs text-muted-foreground">
                      <div className="flex items-center gap-1">
                        {getFormatIcon(report.reportType)}
                        <span>{report.reportType}</span>
                      </div>
                      <div className="flex items-center gap-1">
                        <Calendar className="h-3 w-3" />
                        <span>{format(new Date(report.createdAt), 'MMM d, yyyy')}</span>
                      </div>
                      <div className="flex items-center gap-1">
                        <Clock className="h-3 w-3" />
                        <span>{formatDuration(report)}</span>
                      </div>
                      {report.fileSize && (
                        <div className="flex items-center gap-1">
                          <FileText className="h-3 w-3" />
                          <span>{formatFileSize(report.fileSize)}</span>
                        </div>
                      )}
                    </div>

                    {/* Error Message */}
                    {report.status === 'Failed' && report.errorMessage && (
                      <Alert className="mt-3">
                        <AlertCircle className="h-4 w-4" />
                        <AlertDescription className="text-sm">
                          <strong>Error:</strong> {report.errorMessage}
                        </AlertDescription>
                      </Alert>
                    )}
                  </div>
                </div>

                {/* Actions */}
                <div className="flex items-center gap-2 ml-4">
                  {report.status === 'Completed' && (
                    <Button
                      size="sm"
                      onClick={() => onDownload(report.id, report.title)}
                    >
                      <Download className="h-4 w-4" />
                    </Button>
                  )}

                  <DropdownMenu>
                    <DropdownMenuTrigger asChild>
                      <Button variant="outline" size="sm">
                        <MoreHorizontal className="h-4 w-4" />
                      </Button>
                    </DropdownMenuTrigger>
                    <DropdownMenuContent align="end">
                      <DropdownMenuItem onClick={() => showReportDetails(report)}>
                        <Eye className="mr-2 h-4 w-4" />
                        View Details
                      </DropdownMenuItem>
                      {onPreview && report.status === 'Completed' && (
                        <DropdownMenuItem onClick={() => onPreview(report)}>
                          <FileText className="mr-2 h-4 w-4" />
                          Preview
                        </DropdownMenuItem>
                      )}
                      {report.status === 'Completed' && (
                        <DropdownMenuItem onClick={() => onDownload(report.id, report.title)}>
                          <Download className="mr-2 h-4 w-4" />
                          Download
                        </DropdownMenuItem>
                      )}
                      <DropdownMenuSeparator />
                      <DropdownMenuItem 
                        onClick={() => handleDelete(report.id, report.title)}
                        className="text-red-600 focus:text-red-600"
                      >
                        <Trash2 className="mr-2 h-4 w-4" />
                        Delete
                      </DropdownMenuItem>
                    </DropdownMenuContent>
                  </DropdownMenu>
                </div>
              </div>
            </CardContent>
          </Card>
        ))}
      </div>

      {/* Report Details Dialog */}
      <Dialog open={showDetailsDialog} onOpenChange={setShowDetailsDialog}>
        <DialogContent className="max-w-2xl">
          <DialogHeader>
            <DialogTitle>Report Details</DialogTitle>
            <DialogDescription>
              Detailed information about the selected report
            </DialogDescription>
          </DialogHeader>
          
          {selectedReport && (
            <div className="space-y-4">
              <div className="grid grid-cols-2 gap-4">
                <div>
                  <Label className="text-sm font-medium">Title</Label>
                  <p className="text-sm">{selectedReport.title}</p>
                </div>
                <div>
                  <Label className="text-sm font-medium">Type</Label>
                  <p className="text-sm">{selectedReport.reportType}</p>
                </div>
                <div>
                  <Label className="text-sm font-medium">Status</Label>
                  <div className="flex items-center gap-2">
                    {getStatusIcon(selectedReport.status)}
                    <Badge className={getStatusColor(selectedReport.status)}>
                      {selectedReport.status}
                    </Badge>
                  </div>
                </div>
                <div>
                  <Label className="text-sm font-medium">Created</Label>
                  <p className="text-sm">
                    {format(new Date(selectedReport.createdAt), 'PPP pp')}
                  </p>
                </div>
                {selectedReport.completedAt && (
                  <div>
                    <Label className="text-sm font-medium">Completed</Label>
                    <p className="text-sm">
                      {format(new Date(selectedReport.completedAt), 'PPP pp')}
                    </p>
                  </div>
                )}
                {selectedReport.fileSize && (
                  <div>
                    <Label className="text-sm font-medium">File Size</Label>
                    <p className="text-sm">{formatFileSize(selectedReport.fileSize)}</p>
                  </div>
                )}
              </div>

              {selectedReport.description && (
                <div>
                  <Label className="text-sm font-medium">Description</Label>
                  <p className="text-sm mt-1">{selectedReport.description}</p>
                </div>
              )}

              {selectedReport.parameters && Object.keys(selectedReport.parameters).length > 0 && (
                <div>
                  <Label className="text-sm font-medium">Parameters</Label>
                  <div className="mt-2 p-3 bg-gray-50 rounded-lg">
                    <pre className="text-xs text-gray-700 whitespace-pre-wrap">
                      {JSON.stringify(selectedReport.parameters, null, 2)}
                    </pre>
                  </div>
                </div>
              )}

              {selectedReport.status === 'Failed' && selectedReport.errorMessage && (
                <Alert>
                  <AlertCircle className="h-4 w-4" />
                  <AlertDescription>
                    <strong>Error Details:</strong>
                    <p className="mt-1">{selectedReport.errorMessage}</p>
                  </AlertDescription>
                </Alert>
              )}

              <div className="flex justify-end gap-2 pt-4">
                {selectedReport.status === 'Completed' && (
                  <Button onClick={() => onDownload(selectedReport.id, selectedReport.title)}>
                    <Download className="mr-2 h-4 w-4" />
                    Download
                  </Button>
                )}
                <Button variant="outline" onClick={() => setShowDetailsDialog(false)}>
                  Close
                </Button>
              </div>
            </div>
          )}
        </DialogContent>
      </Dialog>
    </div>
  );
}

function Label({ children, className }: { children: React.ReactNode; className?: string }) {
  return <div className={cn("font-medium", className)}>{children}</div>;
}