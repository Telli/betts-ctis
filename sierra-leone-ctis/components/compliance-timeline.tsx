"use client"

import { Card, CardContent, CardHeader, CardTitle } from '@/components/ui/card';
import { CheckCircle, XCircle, Clock } from 'lucide-react';
import { cn } from '@/lib/utils';
import type { ComplianceTimelineEvent } from '@/lib/services/compliance-service';

export interface ComplianceTimelineProps {
  events?: ComplianceTimelineEvent[];
}

export function ComplianceTimeline({ events = [] }: ComplianceTimelineProps) {
  const displayEvents = events;

  const getStatusIcon = (status: string) => {
    switch (status) {
      case 'success':
        return <CheckCircle className="w-5 h-5 text-green-600" />;
      case 'warning':
        return <Clock className="w-5 h-5 text-amber-500" />;
      case 'error':
        return <XCircle className="w-5 h-5 text-red-600" />;
      default:
        return <CheckCircle className="w-5 h-5 text-gray-400" />;
    }
  };

  const getStatusColor = (status: string) => {
    switch (status) {
      case 'success':
        return 'bg-green-100 border-green-200';
      case 'warning':
        return 'bg-amber-50 border-amber-200';
      case 'error':
        return 'bg-red-50 border-red-200';
      default:
        return 'bg-gray-50 border-gray-200';
    }
  };

  return (
    <Card>
      <CardHeader>
        <CardTitle>Compliance Timeline</CardTitle>
      </CardHeader>
      <CardContent>
        <div className="space-y-4">
          {displayEvents.map((item, index) => (
            <div key={index} className="flex gap-4">
              {/* Timeline Icon & Line */}
              <div className="flex flex-col items-center">
                {getStatusIcon(item.status)}
                {index < displayEvents.length - 1 && (
                  <div className="w-0.5 h-full bg-gray-300 mt-2 min-h-[40px]" />
                )}
              </div>

              {/* Content */}
              <div className={cn(
                "flex-1 pb-4 px-4 py-3 rounded-lg border",
                getStatusColor(item.status)
              )}>
                <div className="flex items-start justify-between mb-1">
                  <p className="font-semibold text-gray-900">{item.event}</p>
                  <time className="text-sm text-gray-600 whitespace-nowrap ml-4">
                    {item.date}
                  </time>
                </div>
                {item.details && (
                  <p className="text-sm text-gray-600 mt-1">{item.details}</p>
                )}
              </div>
            </div>
          ))}

          {displayEvents.length === 0 && (
            <div className="text-center py-8 text-gray-500">
              <Clock className="w-12 h-12 text-gray-300 mx-auto mb-2" />
              <p>No compliance events to display.</p>
            </div>
          )}
        </div>
      </CardContent>
    </Card>
  );
}
