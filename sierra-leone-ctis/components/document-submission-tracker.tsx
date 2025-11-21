"use client"

import { Card, CardContent, CardHeader, CardTitle } from '@/components/ui/card';
import { FileText, CheckCircle } from 'lucide-react';
import { cn } from '@/lib/utils';
import type { DocumentRequirementSummary } from '@/lib/services/compliance-service';

export interface DocumentSubmissionTrackerProps {
  requirements?: DocumentRequirementSummary[];
}

export function DocumentSubmissionTracker({ requirements = [] }: DocumentSubmissionTrackerProps) {

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
        {requirements.map((doc, index) => (
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
            <p className="text-xs text-gray-500">
              {doc.approved} approved / {doc.submitted} submitted
            </p>
          </div>
        ))}

        {requirements.length === 0 && (
          <div className="text-center py-8 text-gray-500">
            <FileText className="w-12 h-12 text-gray-300 mx-auto mb-2" />
            <p>No document requirements to track.</p>
          </div>
        )}

        {/* Overall Summary */}
        {requirements.length > 0 && (
          <div className="mt-6 pt-4 border-t border-gray-200">
            <div className="flex items-center justify-between text-sm">
              <span className="font-semibold text-gray-700">Overall Completion</span>
              <span className="font-bold text-blue-600">
                {Math.round(
                  requirements.reduce((sum, doc) => sum + doc.progress, 0) /
                    requirements.length
                )}%
              </span>
            </div>
          </div>
        )}
      </CardContent>
    </Card>
  );
}
