"use client"

import { Card, CardContent, CardHeader, CardTitle } from '@/components/ui/card';
import { Badge } from '@/components/ui/badge';

export interface HistoryTabProps {
  filingId?: number;
}

interface HistoryEntry {
  date: string;
  user: string;
  action: string;
  changes: string;
  status?: string;
}

export function HistoryTab({ filingId }: HistoryTabProps) {
  // Mock data - in real implementation, this would come from audit trail API
  const history: HistoryEntry[] = [
    { 
      date: '2025-10-05 14:30', 
      user: 'John Doe', 
      action: 'Updated form data', 
      changes: 'Revenue figures updated from SLE 240,000 to SLE 250,000',
      status: 'modified'
    },
    { 
      date: '2025-10-04 10:15', 
      user: 'Jane Smith', 
      action: 'Uploaded document', 
      changes: 'Financial Statements v2 uploaded',
      status: 'uploaded'
    },
    { 
      date: '2025-10-03 16:45', 
      user: 'John Doe', 
      action: 'Created filing', 
      changes: 'GST Return Q3 2025 created',
      status: 'created'
    },
    { 
      date: '2025-10-02 09:00', 
      user: 'System', 
      action: 'Status changed', 
      changes: 'Status changed from Draft to Under Review',
      status: 'status_change'
    },
  ];

  const getStatusColor = (status?: string) => {
    switch (status) {
      case 'created':
        return 'bg-green-100 text-green-700';
      case 'modified':
        return 'bg-blue-100 text-blue-700';
      case 'uploaded':
        return 'bg-purple-100 text-purple-700';
      case 'status_change':
        return 'bg-amber-100 text-amber-700';
      default:
        return 'bg-gray-100 text-gray-700';
    }
  };

  return (
    <Card>
      <CardHeader>
        <CardTitle>Audit Trail</CardTitle>
      </CardHeader>
      <CardContent>
        <div className="space-y-6">
          {history.map((entry, index) => (
            <div key={index} className="flex gap-4 pb-6 border-b border-gray-200 last:border-b-0 last:pb-0">
              {/* Timeline Dot */}
              <div className="flex flex-col items-center">
                <div className="w-3 h-3 bg-blue-600 rounded-full mt-1.5" />
                {index < history.length - 1 && (
                  <div className="w-0.5 h-full bg-gray-300 mt-2" />
                )}
              </div>

              {/* Content */}
              <div className="flex-1">
                <div className="flex items-start justify-between mb-1">
                  <div>
                    <p className="font-semibold text-gray-900">{entry.action}</p>
                    <p className="text-sm text-gray-600 mt-1">{entry.changes}</p>
                  </div>
                  <time className="text-sm text-gray-500 whitespace-nowrap ml-4">{entry.date}</time>
                </div>
                <div className="flex items-center gap-2 mt-2">
                  <p className="text-sm text-gray-600">by <span className="font-medium">{entry.user}</span></p>
                  {entry.status && (
                    <Badge variant="outline" className={getStatusColor(entry.status)}>
                      {entry.status.replace('_', ' ')}
                    </Badge>
                  )}
                </div>
              </div>
            </div>
          ))}

          {history.length === 0 && (
            <div className="text-center text-gray-500 py-8">
              No history available for this filing yet.
            </div>
          )}
        </div>
      </CardContent>
    </Card>
  );
}
