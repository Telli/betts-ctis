"use client"

import { Card, CardContent, CardHeader, CardTitle } from '@/components/ui/card';
import { Badge } from '@/components/ui/badge';
import { Button } from '@/components/ui/button';
import { AlertTriangle, ExternalLink } from 'lucide-react';
import type { PenaltyWarningSummary } from '@/lib/services/compliance-service';

export interface PenaltyWarningsCardProps {
  warnings?: PenaltyWarningSummary[];
}

export function PenaltyWarningsCard({ warnings = [] }: PenaltyWarningsCardProps) {
  const totalPenalties = warnings.reduce((sum, w) => sum + w.estimatedAmount, 0);

  return (
    <Card>
      <CardHeader>
        <CardTitle className="flex items-center gap-2">
          <AlertTriangle className="w-5 h-5 text-red-600" />
          Penalty Warnings
        </CardTitle>
      </CardHeader>
      <CardContent className="space-y-4">
        {warnings.map((warning, index) => (
          <div
            key={index}
            className="flex items-start gap-3 p-4 border border-red-200 bg-red-50 rounded-lg"
          >
            <AlertTriangle className="w-5 h-5 text-red-600 mt-0.5 flex-shrink-0" />
            <div className="flex-1 min-w-0">
              <p className="font-semibold text-gray-900">{warning.type}</p>
              <p className="text-sm text-gray-600 mt-1">{warning.reason}</p>
              {warning.clientName && (
                <p className="text-xs text-gray-500 mt-1">{warning.clientName}</p>
              )}
              <div className="flex items-center justify-between mt-3 flex-wrap gap-2">
                <span className="text-sm text-red-700 font-semibold">
                  Estimated: SLE {warning.estimatedAmount.toLocaleString()}
                </span>
                <Badge variant="destructive" className="text-xs">
                  {warning.daysOverdue} day{warning.daysOverdue !== 1 ? 's' : ''} overdue
                </Badge>
              </div>
              {(warning.filingId || warning.paymentId) && (
                <Button
                  variant="link"
                  size="sm"
                  className="mt-2 p-0 h-auto text-blue-600 hover:text-blue-700"
                  onClick={() => {
                    // Navigate to filing
                    if (warning.filingId) {
                      window.location.href = `/tax-filings/${warning.filingId}`;
                    } else if (warning.paymentId) {
                      window.location.href = `/payments/${warning.paymentId}`;
                    }
                  }}
                >
                  View Filing
                  <ExternalLink className="w-3 h-3 ml-1" />
                </Button>
              )}
            </div>
          </div>
        ))}

        {warnings.length === 0 && (
          <div className="text-center py-8 text-gray-500">
            <AlertTriangle className="w-12 h-12 text-gray-300 mx-auto mb-2" />
            <p>No penalty warnings at this time.</p>
            <p className="text-sm mt-1">Keep up the good work!</p>
          </div>
        )}

        {/* Total Penalties Summary */}
        {warnings.length > 0 && (
          <div className="p-4 border border-gray-300 bg-white rounded-lg mt-4">
            <div className="flex items-center justify-between">
              <div>
                <p className="font-semibold text-gray-900">Total Potential Penalties</p>
                <p className="text-sm text-gray-600">If not addressed</p>
              </div>
              <span className="text-2xl font-bold text-red-600">
                SLE {totalPenalties.toLocaleString()}
              </span>
            </div>
          </div>
        )}
      </CardContent>
    </Card>
  );
}
