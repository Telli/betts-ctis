"use client"

import { useEffect, useState } from 'react';
import { Card, CardContent, CardHeader, CardTitle } from '@/components/ui/card';
import { Alert, AlertDescription } from '@/components/ui/alert';
import { CheckCircle } from 'lucide-react';
import { useToast } from '@/components/ui/use-toast';
import type { TaxFilingDto, TaxFilingValidationResultDto } from '@/lib/services';
import { FilingStatus } from '@/lib/services';
import { TaxFilingService } from '@/lib/services/tax-filing-service';

export interface AssessmentTabProps {
  filing?: TaxFilingDto;
  mode?: 'create' | 'edit' | 'view';
}

interface AssessmentData {
  totalSales: number;
  taxableSales: number;
  taxRate: number;
  outputTax: number;
  inputTaxCredit: number;
  penalties: number;
  interest: number;
  totalPayable: number;
}

const buildDefaultAssessment = (filing?: TaxFilingDto): AssessmentData => {
  const baseLiability = filing?.taxLiability;
  return {
    totalSales: baseLiability ? baseLiability / 0.15 : 250000,
    taxableSales: baseLiability ? baseLiability / 0.15 : 250000,
    taxRate: 15,
    outputTax: baseLiability ?? 37500,
    inputTaxCredit: 15000,
    penalties: 0,
    interest: 0,
    totalPayable: baseLiability ?? 22500,
  };
};

export function AssessmentTab({ filing, mode = 'edit' }: AssessmentTabProps) {
  const filingId = filing?.taxFilingId;
  const { toast } = useToast();
  const [assessmentData, setAssessmentData] = useState<AssessmentData>(() => buildDefaultAssessment(filing));
  const [loading, setLoading] = useState(false);
  const [error, setError] = useState<string | null>(null);
  const [validation, setValidation] = useState<TaxFilingValidationResultDto | null>(null);
  const [validationLoading, setValidationLoading] = useState(false);
  const [validationError, setValidationError] = useState<string | null>(null);

  const errorCount = validation?.errors?.length ?? 0;
  const warningCount = validation?.warnings?.length ?? 0;
  const hasErrors = !!validation && !validation.isValid && errorCount > 0;

  useEffect(() => {
    const loadAssessment = async () => {
      if (!filingId) {
        setAssessmentData(buildDefaultAssessment(filing));
        return;
      }

      try {
        setLoading(true);
        setError(null);
        const dto = await TaxFilingService.getAssessmentSummary(filingId);
        setAssessmentData({
          totalSales: dto.totalSales,
          taxableSales: dto.taxableSales,
          taxRate: dto.gstRate,
          outputTax: dto.outputTax,
          inputTaxCredit: dto.inputTaxCredit,
          penalties: dto.penalties,
          interest: 0,
          totalPayable: dto.totalPayable,
        });
      } catch (err) {
        console.error('Failed to load assessment summary', err);
        setError('Failed to load assessment summary. Showing estimated values instead.');
        toast({
          variant: 'destructive',
          title: 'Error',
          description: 'Failed to load assessment summary. Showing estimated values instead.',
        });
        setAssessmentData(buildDefaultAssessment(filing));
      } finally {
        setLoading(false);
      }
    };

    loadAssessment();
  }, [filingId, filing, toast]);

  useEffect(() => {
    const validate = async () => {
      if (!filingId) {
        setValidation(null);
        setValidationError(null);
        return;
      }

      try {
        setValidationLoading(true);
        setValidationError(null);
        const result = await TaxFilingService.validateTaxFilingForSubmission(filingId);
        setValidation(result);
      } catch (err) {
        console.error('Failed to validate tax filing before submission', err);
        setValidationError('Failed to validate filing before submission.');
        toast({
          variant: 'destructive',
          title: 'Validation error',
          description: 'Failed to validate filing before submission.',
        });
      } finally {
        setValidationLoading(false);
      }
    };

    validate();
  }, [filingId, toast]);

  return (
    <>
      <Card>
        <CardHeader>
          <CardTitle>Tax Assessment Summary</CardTitle>
        </CardHeader>
        <CardContent>
          {loading && (
            <div className="mb-2 text-sm text-gray-500">
              Loading assessment from server...
            </div>
          )}

          {!loading && error && (
            <div className="mb-2 text-sm text-red-600">
              {error}
            </div>
          )}

          {!loading && !error && validationLoading && (
            <div className="mb-2 text-sm text-gray-500">
              Checking filing for validation issues...
            </div>
          )}

          {!loading && !error && validationError && (
            <div className="mb-2 text-sm text-red-600">
              {validationError}
            </div>
          )}

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
            <strong>No blocking validation issues detected.</strong>{' '}
            {filing?.status === FilingStatus.Approved
              ? 'This filing has been approved.'
              : 'This filing appears ready to submit based on the current assessment.'}
            {validation && (validation.warnings?.length ?? 0) > 0 && (
              <ul className="mt-2 list-disc list-inside text-sm text-yellow-800">
                {(validation.warnings ?? []).map((warning, idx) => (
                  <li key={idx}>{warning}</li>
                ))}
              </ul>
            )}
          </AlertDescription>
        </Alert>
      ) : (
        <Alert variant="destructive">
          <AlertDescription>
            <strong>Validation issues detected.</strong>{' '}
            {filing?.status === FilingStatus.Rejected
              ? 'This filing was rejected during review. Please address the reviewer comments and update the schedules or form as needed.'
              : 'Please review the form and schedules before submitting.'}
            {filing?.reviewComments && (
              <p className="mt-2 text-sm">
                <span className="font-semibold">Reviewer comments:</span> {filing.reviewComments}
              </p>
            )}
            {validation && (validation.errors?.length ?? 0) > 0 && (
              <ul className="mt-2 list-disc list-inside text-sm">
                {(validation.errors ?? []).map((errMsg, idx) => (
                  <li key={idx}>{errMsg}</li>
                ))}
              </ul>
            )}
            {validation && (validation.warnings?.length ?? 0) > 0 && (
              <ul className="mt-2 list-disc list-inside text-sm text-yellow-800">
                {(validation.warnings ?? []).map((warning, idx) => (
                  <li key={idx}>{warning}</li>
                ))}
              </ul>
            )}
            {assessmentData.penalties > 0 && (
              <p className="mt-1 text-sm">
                <span className="font-semibold">Penalties applied:</span> SLE{' '}
                {assessmentData.penalties.toLocaleString()}
              </p>
            )}
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
