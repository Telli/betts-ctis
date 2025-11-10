"use client"

import { Card, CardContent, CardHeader, CardTitle } from '@/components/ui/card';
import { Progress } from '@/components/ui/progress';
import { FileText, CheckCircle } from 'lucide-react';
import { cn } from '@/lib/utils';

export interface DocumentRequirement {
  name: string;
  required: number;
  submitted: number;
  progress: number;
}

export interface DocumentSubmissionTrackerProps {
  requirements?: DocumentRequirement[];
}

export function DocumentSubmissionTracker({ requirements }: DocumentSubmissionTrackerProps) {
  // Mock data - in real implementation, this would come from API
  const defaultRequirements: DocumentRequirement[] = [
    { name: 'Financial Statements', required: 12, submitted: 12, progress: 100 },
    { name: 'Bank Statements', required: 12, submitted: 11, progress: 92 },
    { name: 'Payroll Records', required: 12, submitted: 10, progress: 83 },
    { name: 'Sales Invoices', required: 4, submitted: 3, progress: 75 },
    { name: 'Tax Receipts', required: 8, submitted: 5, progress: 63 },
  ];

  const displayRequirements = requirements || defaultRequirements;

  const getProgressColor = (progress: number) => {
    if (progress === 100) return 'bg-green-600';
    if (progress >= 80) return 'bg-blue-600';
    if (progress >= 60) return 'bg-amber-500';
    return 'bg-red-600';
  };

  return (
    <Card>
      <CardHeader>
        <CardTitle>Document Submission Tracker</CardTitle>
      </CardHeader>
      <CardContent className="space-y-5">
        {displayRequirements.map((doc, index) => (
          <div key={index} className="space-y-2">
            <div className="flex items-center justify-between">
              <div className="flex items-center gap-2 flex-1 min-w-0">
                {doc.progress === 100 ? (
                  <CheckCircle className="w-4 h-4 text-green-600 flex-shrink-0" />
                ) : (
                  <FileText className="w-4 h-4 text-gray-400 flex-shrink-0" />
                )}
                <span className="font-medium text-gray-900 truncate">{doc.name}</span>
              </div>
              <div className="flex items-center gap-3 flex-shrink-0 ml-2">
                <span className="text-sm text-gray-600">
                  {doc.submitted}/{doc.required}
                </span>
                <span className={cn(
                  "text-sm font-semibold w-12 text-right",
                  doc.progress === 100 ? "text-green-600" :
                  doc.progress >= 80 ? "text-blue-600" :
                  doc.progress >= 60 ? "text-amber-600" :
                  "text-red-600"
                )}>
                  {doc.progress}%
                </span>
              </div>
            </div>
            <div className="relative h-2 bg-gray-200 rounded-full overflow-hidden">
              <div
                className={cn("h-full transition-all duration-300", getProgressColor(doc.progress))}
                style={{ width: `${doc.progress}%` }}
              />
            </div>
          </div>
        ))}

        {displayRequirements.length === 0 && (
          <div className="text-center py-8 text-gray-500">
            <FileText className="w-12 h-12 text-gray-300 mx-auto mb-2" />
            <p>No document requirements to track.</p>
          </div>
        )}

        {/* Overall Summary */}
        {displayRequirements.length > 0 && (
          <div className="mt-6 pt-4 border-t border-gray-200">
            <div className="flex items-center justify-between text-sm">
              <span className="font-semibold text-gray-700">Overall Completion</span>
              <span className="font-bold text-blue-600">
                {Math.round(
                  displayRequirements.reduce((sum, doc) => sum + doc.progress, 0) /
                    displayRequirements.length
                )}%
              </span>
            </div>
          </div>
        )}
      </CardContent>
    </Card>
  );
}
