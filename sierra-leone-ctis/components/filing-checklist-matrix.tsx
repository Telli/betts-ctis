"use client"

import { Card, CardContent, CardHeader, CardTitle } from '@/components/ui/card';
import { CheckCircle, Clock, AlertTriangle, Minus } from 'lucide-react';

export interface FilingChecklistMatrixProps {
  year?: number;
}

interface FilingStatus {
  q1: 'filed' | 'pending' | 'overdue' | 'upcoming' | 'n/a';
  q2: 'filed' | 'pending' | 'overdue' | 'upcoming' | 'n/a';
  q3: 'filed' | 'pending' | 'overdue' | 'upcoming' | 'n/a';
  q4: 'filed' | 'pending' | 'overdue' | 'upcoming' | 'n/a';
}

interface TaxTypeRow {
  type: string;
  status: FilingStatus;
}

export function FilingChecklistMatrix({ year = 2025 }: FilingChecklistMatrixProps) {
  // Mock data - in real implementation, this would come from API
  const filingData: TaxTypeRow[] = [
    { type: 'GST Returns', status: { q1: 'filed', q2: 'filed', q3: 'pending', q4: 'upcoming' } },
    { type: 'PAYE Returns', status: { q1: 'filed', q2: 'filed', q3: 'filed', q4: 'upcoming' } },
    { type: 'Income Tax', status: { q1: 'filed', q2: 'n/a', q3: 'n/a', q4: 'n/a' } },
    { type: 'Excise Duty', status: { q1: 'filed', q2: 'filed', q3: 'overdue', q4: 'upcoming' } },
    { type: 'Withholding Tax', status: { q1: 'filed', q2: 'filed', q3: 'filed', q4: 'pending' } },
  ];

  const getStatusIcon = (status: string) => {
    switch (status) {
      case 'filed':
        return <div title="Filed"><CheckCircle className="w-5 h-5 text-green-600" /></div>;
      case 'pending':
        return <div title="Pending"><Clock className="w-5 h-5 text-amber-500" /></div>;
      case 'overdue':
        return <div title="Overdue"><AlertTriangle className="w-5 h-5 text-red-600" /></div>;
      case 'upcoming':
        return <div className="w-5 h-5 rounded-full border-2 border-gray-400" title="Upcoming" />;
      case 'n/a':
        return <div title="Not Applicable"><Minus className="w-5 h-5 text-gray-400" /></div>;
      default:
        return null;
    }
  };

  return (
    <Card>
      <CardHeader>
        <CardTitle>Filing Checklist - {year}</CardTitle>
      </CardHeader>
      <CardContent>
        <div className="space-y-4">
          {/* Header Row */}
          <div className="grid grid-cols-5 gap-4 pb-3 border-b border-gray-200">
            <div className="font-semibold text-gray-700">Tax Type</div>
            <div className="font-semibold text-gray-700 text-center">Q1</div>
            <div className="font-semibold text-gray-700 text-center">Q2</div>
            <div className="font-semibold text-gray-700 text-center">Q3</div>
            <div className="font-semibold text-gray-700 text-center">Q4</div>
          </div>

          {/* Data Rows */}
          {filingData.map((item, index) => (
            <div
              key={index}
              className="grid grid-cols-5 gap-4 items-center py-2 hover:bg-gray-50 rounded-lg transition-colors"
            >
              <div className="font-medium text-gray-900">{item.type}</div>
              <div className="flex justify-center">{getStatusIcon(item.status.q1)}</div>
              <div className="flex justify-center">{getStatusIcon(item.status.q2)}</div>
              <div className="flex justify-center">{getStatusIcon(item.status.q3)}</div>
              <div className="flex justify-center">{getStatusIcon(item.status.q4)}</div>
            </div>
          ))}
        </div>

        {/* Legend */}
        <div className="mt-6 pt-4 border-t border-gray-200">
          <div className="flex flex-wrap gap-4 text-sm">
            <div className="flex items-center gap-2">
              <CheckCircle className="w-4 h-4 text-green-600" />
              <span className="text-gray-600">Filed</span>
            </div>
            <div className="flex items-center gap-2">
              <Clock className="w-4 h-4 text-amber-500" />
              <span className="text-gray-600">Pending</span>
            </div>
            <div className="flex items-center gap-2">
              <AlertTriangle className="w-4 h-4 text-red-600" />
              <span className="text-gray-600">Overdue</span>
            </div>
            <div className="flex items-center gap-2">
              <div className="w-4 h-4 rounded-full border-2 border-gray-400" />
              <span className="text-gray-600">Upcoming</span>
            </div>
            <div className="flex items-center gap-2">
              <Minus className="w-4 h-4 text-gray-400" />
              <span className="text-gray-600">N/A</span>
            </div>
          </div>
        </div>
      </CardContent>
    </Card>
  );
}
