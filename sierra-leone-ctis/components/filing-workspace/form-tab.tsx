"use client"

import { Card, CardContent, CardHeader, CardTitle } from '@/components/ui/card';
import { Input } from '@/components/ui/input';
import { Label } from '@/components/ui/label';
import { Select, SelectContent, SelectItem, SelectTrigger, SelectValue } from '@/components/ui/select';
import { Textarea } from '@/components/ui/textarea';
import { TaxFilingDto, TaxType } from '@/lib/services';

export interface FormTabProps {
  filing?: TaxFilingDto;
  mode?: 'create' | 'edit' | 'view';
}

export function FormTab({ filing, mode = 'edit' }: FormTabProps) {
  const isReadOnly = mode === 'view';

  return (
    <>
      {/* Basic Information */}
      <Card>
        <CardHeader>
          <CardTitle>Basic Information</CardTitle>
        </CardHeader>
        <CardContent className="space-y-4">
          <div className="grid grid-cols-1 md:grid-cols-2 gap-4">
            <div className="space-y-2">
              <Label htmlFor="taxPeriod">Tax Period</Label>
              <Select defaultValue={filing?.taxYear?.toString() || new Date().getFullYear().toString()} disabled={isReadOnly}>
                <SelectTrigger id="taxPeriod">
                  <SelectValue />
                </SelectTrigger>
                <SelectContent>
                  <SelectItem value="2025">FY 2025</SelectItem>
                  <SelectItem value="2024">FY 2024</SelectItem>
                  <SelectItem value="2023">FY 2023</SelectItem>
                </SelectContent>
              </Select>
            </div>

            <div className="space-y-2">
              <Label htmlFor="filingStatus">Filing Status</Label>
              <Select defaultValue={filing?.status || 'Draft'} disabled={isReadOnly}>
                <SelectTrigger id="filingStatus">
                  <SelectValue />
                </SelectTrigger>
                <SelectContent>
                  <SelectItem value="Draft">Draft</SelectItem>
                  <SelectItem value="Submitted">Submitted</SelectItem>
                  <SelectItem value="UnderReview">Under Review</SelectItem>
                  <SelectItem value="Approved">Approved</SelectItem>
                </SelectContent>
              </Select>
            </div>

            <div className="space-y-2">
              <Label htmlFor="taxType">Tax Type</Label>
              <Select defaultValue={filing?.taxType || TaxType.GST} disabled={isReadOnly}>
                <SelectTrigger id="taxType">
                  <SelectValue />
                </SelectTrigger>
                <SelectContent>
                  <SelectItem value={TaxType.GST}>GST</SelectItem>
                  <SelectItem value={TaxType.IncomeTax}>Income Tax</SelectItem>
                  <SelectItem value={TaxType.PAYE}>PAYE</SelectItem>
                  <SelectItem value={TaxType.WithholdingTax}>Withholding Tax</SelectItem>
                  <SelectItem value={TaxType.ExciseDuty}>Excise Duty</SelectItem>
                </SelectContent>
              </Select>
            </div>

            <div className="space-y-2">
              <Label htmlFor="dueDate">Due Date</Label>
              <Input
                id="dueDate"
                type="date"
                defaultValue={filing?.dueDate ? new Date(filing.dueDate).toISOString().split('T')[0] : ''}
                disabled={isReadOnly}
              />
            </div>
          </div>
        </CardContent>
      </Card>

      {/* Tax Details */}
      <Card>
        <CardHeader>
          <CardTitle>Tax Details</CardTitle>
        </CardHeader>
        <CardContent className="space-y-4">
          <div className="grid grid-cols-1 md:grid-cols-2 gap-4">
            <div className="space-y-2">
              <Label htmlFor="totalSales">Total Sales (SLE)</Label>
              <Input
                id="totalSales"
                type="number"
                placeholder="0.00"
                defaultValue={filing?.taxLiability ? (filing.taxLiability / 0.15).toFixed(2) : ''}
                disabled={isReadOnly}
              />
            </div>

            <div className="space-y-2">
              <Label htmlFor="taxableSales">Taxable Sales (SLE)</Label>
              <Input
                id="taxableSales"
                type="number"
                placeholder="0.00"
                defaultValue={filing?.taxLiability ? (filing.taxLiability / 0.15).toFixed(2) : ''}
                disabled={isReadOnly}
              />
            </div>

            <div className="space-y-2">
              <Label htmlFor="taxRate">Tax Rate (%)</Label>
              <Input
                id="taxRate"
                type="number"
                placeholder="15"
                defaultValue="15"
                disabled
                className="bg-gray-50"
              />
            </div>

            <div className="space-y-2">
              <Label htmlFor="outputTax">Output Tax (SLE)</Label>
              <Input
                id="outputTax"
                type="number"
                placeholder="0.00"
                defaultValue={filing?.taxLiability?.toFixed(2) || ''}
                disabled
                className="bg-gray-50"
              />
            </div>

            <div className="space-y-2">
              <Label htmlFor="inputTax">Input Tax Credit (SLE)</Label>
              <Input
                id="inputTax"
                type="number"
                placeholder="0.00"
                defaultValue="0.00"
                disabled={isReadOnly}
              />
            </div>

            <div className="space-y-2">
              <Label htmlFor="netTax">Net Tax Payable (SLE)</Label>
              <Input
                id="netTax"
                type="number"
                placeholder="0.00"
                defaultValue={filing?.taxLiability?.toFixed(2) || ''}
                disabled
                className="bg-gray-50 font-semibold"
              />
            </div>
          </div>
        </CardContent>
      </Card>

      {/* Additional Information */}
      <Card>
        <CardHeader>
          <CardTitle>Additional Information</CardTitle>
        </CardHeader>
        <CardContent>
          <div className="space-y-2">
            <Label htmlFor="notes">Notes / Comments</Label>
            <Textarea
              id="notes"
              placeholder="Add any additional notes or explanations..."
              rows={4}
              defaultValue={filing?.reviewComments || ''}
              disabled={isReadOnly}
            />
          </div>
        </CardContent>
      </Card>
    </>
  );
}
