"use client"

import { Card, CardContent, CardHeader, CardTitle } from '@/components/ui/card';
import { Alert, AlertDescription } from '@/components/ui/alert';
import { CheckCircle } from 'lucide-react';
import type { TaxFilingDto } from '@/lib/services';

export interface AssessmentTabProps {
  filing?: TaxFilingDto;
  mode?: 'create' | 'edit' | 'view';
}

export function AssessmentTab({ filing, mode = 'edit' }: AssessmentTabProps) {
  // Mock calculation data - in real implementation, this would be calculated from form/schedules
  const assessmentData = {
    totalSales: filing?.taxLiability ? (filing.taxLiability / 0.15) : 250000,
    taxableSales: filing?.taxLiability ? (filing.taxLiability / 0.15) : 250000,
    taxRate: 15,
    outputTax: filing?.taxLiability || 37500,
    inputTaxCredit: 15000,
    penalties: 0,
    interest: 0,
    totalPayable: filing?.taxLiability || 22500,
  };

  const hasErrors = false;

  return (
    <>
      <Card>
        <CardHeader>
          <CardTitle>Tax Assessment Summary</CardTitle>
        </CardHeader>
        <CardContent>
          <div className="space-y-4">
            {/* Summary Items */}
            <div className="flex justify-between items-center py-3 border-b border-gray-200">
              <span className="text-gray-600">Total Sales</span>
              <span className="font-semibold">SLE {assessmentData.totalSales.toLocaleString()}</span>
            </div>

            <div className="flex justify-between items-center py-3 border-b border-gray-200">
              <span className="text-gray-600">Taxable Sales</span>
              <span className="font-semibold">SLE {assessmentData.taxableSales.toLocaleString()}</span>
            </div>

            <div className="flex justify-between items-center py-3 border-b border-gray-200">
              <span className="text-gray-600">Tax Rate</span>
              <span className="font-semibold">{assessmentData.taxRate}%</span>
            </div>

            <div className="flex justify-between items-center py-3 border-b border-gray-200">
              <span className="text-gray-600">Output Tax</span>
              <span className="font-semibold">SLE {assessmentData.outputTax.toLocaleString()}</span>
            </div>

            <div className="flex justify-between items-center py-3 border-b border-gray-200">
              <span className="text-gray-600">Input Tax Credit</span>
              <span className="font-semibold text-green-600">- SLE {assessmentData.inputTaxCredit.toLocaleString()}</span>
            </div>

            <div className="flex justify-between items-center py-3 border-b border-gray-200">
              <span className="text-gray-600">Penalties</span>
              <span className="font-semibold">{assessmentData.penalties > 0 ? `SLE ${assessmentData.penalties.toLocaleString()}` : 'SLE 0'}</span>
            </div>

            <div className="flex justify-between items-center py-3 border-b border-gray-200">
              <span className="text-gray-600">Interest</span>
              <span className="font-semibold">{assessmentData.interest > 0 ? `SLE ${assessmentData.interest.toLocaleString()}` : 'SLE 0'}</span>
            </div>

            {/* Total Payable - Highlighted */}
            <div className="flex justify-between items-center py-4 px-4 bg-blue-50 rounded-lg border-2 border-blue-200 mt-4">
              <span className="font-bold text-lg">Total Tax Payable</span>
              <span className="text-2xl font-bold text-blue-600">
                SLE {assessmentData.totalPayable.toLocaleString()}
              </span>
            </div>
          </div>
        </CardContent>
      </Card>

      {/* Validation Status */}
      {!hasErrors ? (
        <Alert className="border-green-200 bg-green-50">
          <CheckCircle className="w-4 h-4 text-green-600" />
          <AlertDescription className="text-green-800">
            <strong>No validation errors found.</strong> This filing is ready to submit.
          </AlertDescription>
        </Alert>
      ) : (
        <Alert variant="destructive">
          <AlertDescription>
            <strong>Validation errors found.</strong> Please review the form and schedules before submitting.
          </AlertDescription>
        </Alert>
      )}

      {/* Breakdown Card */}
      <Card>
        <CardHeader>
          <CardTitle>Calculation Breakdown</CardTitle>
        </CardHeader>
        <CardContent>
          <div className="space-y-3 text-sm">
            <div className="flex justify-between">
              <span className="text-gray-600">Base Calculation:</span>
              <span className="font-mono">
                SLE {assessmentData.taxableSales.toLocaleString()} × {assessmentData.taxRate}% = SLE {assessmentData.outputTax.toLocaleString()}
              </span>
            </div>
            <div className="flex justify-between">
              <span className="text-gray-600">Less Input Tax Credit:</span>
              <span className="font-mono">SLE {assessmentData.outputTax.toLocaleString()} - SLE {assessmentData.inputTaxCredit.toLocaleString()} = SLE {(assessmentData.outputTax - assessmentData.inputTaxCredit).toLocaleString()}</span>
            </div>
            {assessmentData.penalties > 0 && (
              <div className="flex justify-between">
                <span className="text-gray-600">Plus Penalties:</span>
                <span className="font-mono">SLE {assessmentData.penalties.toLocaleString()}</span>
              </div>
            )}
            {assessmentData.interest > 0 && (
              <div className="flex justify-between">
                <span className="text-gray-600">Plus Interest:</span>
                <span className="font-mono">SLE {assessmentData.interest.toLocaleString()}</span>
              </div>
            )}
            <div className="flex justify-between pt-2 border-t border-gray-200">
              <span className="font-semibold">Final Amount:</span>
              <span className="font-mono font-bold text-blue-600">
                SLE {assessmentData.totalPayable.toLocaleString()}
              </span>
            </div>
          </div>
        </CardContent>
      </Card>
    </>
  );
}
