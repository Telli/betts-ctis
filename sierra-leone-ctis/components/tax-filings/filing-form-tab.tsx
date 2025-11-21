'use client'

import { Card, CardContent, CardHeader, CardTitle } from '@/components/ui/card'
import { Input } from '@/components/ui/input'
import { Label } from '@/components/ui/label'
import { Textarea } from '@/components/ui/textarea'
import { Select, SelectContent, SelectItem, SelectTrigger, SelectValue } from '@/components/ui/select'

interface FilingFormTabProps {
  filing: any
  onUpdate: (filing: any) => void
}

export default function FilingFormTab({ filing, onUpdate }: FilingFormTabProps) {
  const handleFieldChange = (field: string, value: any) => {
    onUpdate({ ...filing, [field]: value })
  }

  return (
    <>
      <Card>
        <CardHeader>
          <CardTitle>Basic Information</CardTitle>
        </CardHeader>
        <CardContent className="space-y-4">
          <div className="grid grid-cols-1 md:grid-cols-2 gap-4">
            <div className="space-y-2">
              <Label>Tax Period</Label>
              <Select
                value={filing.period || filing.taxYear?.toString()}
                onValueChange={(value) => handleFieldChange('period', value)}
              >
                <SelectTrigger>
                  <SelectValue />
                </SelectTrigger>
                <SelectContent>
                  <SelectItem value="Q1-2025">Q1 2025 (Jan-Mar)</SelectItem>
                  <SelectItem value="Q2-2025">Q2 2025 (Apr-Jun)</SelectItem>
                  <SelectItem value="Q3-2025">Q3 2025 (Jul-Sep)</SelectItem>
                  <SelectItem value="Q4-2025">Q4 2025 (Oct-Dec)</SelectItem>
                  <SelectItem value="2025">Annual 2025</SelectItem>
                </SelectContent>
              </Select>
            </div>
            <div className="space-y-2">
              <Label>Filing Status</Label>
              <Select
                value={filing.status}
                onValueChange={(value) => handleFieldChange('status', value)}
              >
                <SelectTrigger>
                  <SelectValue />
                </SelectTrigger>
                <SelectContent>
                  <SelectItem value="Draft">Draft</SelectItem>
                  <SelectItem value="Submitted">Pending Review</SelectItem>
                  <SelectItem value="UnderReview">Under Review</SelectItem>
                  <SelectItem value="Approved">Approved</SelectItem>
                </SelectContent>
              </Select>
            </div>
          </div>
        </CardContent>
      </Card>

      {filing.taxType === 'GST' && (
        <Card>
          <CardHeader>
            <CardTitle>GST Details</CardTitle>
          </CardHeader>
          <CardContent className="space-y-4">
            <div className="grid grid-cols-1 md:grid-cols-2 gap-4">
              <div className="space-y-2">
                <Label>Total Sales (SLE)</Label>
                <Input
                  type="number"
                  placeholder="0.00"
                  value={filing.totalSales || ''}
                  onChange={(e) => handleFieldChange('totalSales', parseFloat(e.target.value) || 0)}
                />
              </div>
              <div className="space-y-2">
                <Label>Taxable Sales (SLE)</Label>
                <Input
                  type="number"
                  placeholder="0.00"
                  value={filing.taxableSales || ''}
                  onChange={(e) => handleFieldChange('taxableSales', parseFloat(e.target.value) || 0)}
                />
              </div>
              <div className="space-y-2">
                <Label>GST Rate (%)</Label>
                <Input type="number" placeholder="15" value={filing.gstRate || 15} disabled />
              </div>
              <div className="space-y-2">
                <Label>Output Tax (SLE)</Label>
                <Input
                  type="number"
                  placeholder="0.00"
                  value={filing.outputTax || (filing.taxableSales * (filing.gstRate || 15)) / 100}
                  disabled
                />
              </div>
              <div className="space-y-2">
                <Label>Input Tax Credit (SLE)</Label>
                <Input
                  type="number"
                  placeholder="0.00"
                  value={filing.inputTaxCredit || ''}
                  onChange={(e) => handleFieldChange('inputTaxCredit', parseFloat(e.target.value) || 0)}
                />
              </div>
              <div className="space-y-2">
                <Label>Net GST Payable (SLE)</Label>
                <Input
                  type="number"
                  placeholder="0.00"
                  value={
                    filing.netGstPayable ||
                    ((filing.taxableSales * (filing.gstRate || 15)) / 100 - (filing.inputTaxCredit || 0))
                  }
                  disabled
                  className="font-semibold"
                />
              </div>
            </div>
          </CardContent>
        </Card>
      )}

      <Card>
        <CardHeader>
          <CardTitle>Additional Information</CardTitle>
        </CardHeader>
        <CardContent>
          <div className="space-y-2">
            <Label>Notes / Comments</Label>
            <Textarea
              placeholder="Add any additional notes or explanations..."
              rows={4}
              value={filing.notes || ''}
              onChange={(e) => handleFieldChange('notes', e.target.value)}
            />
          </div>
        </CardContent>
      </Card>
    </>
  )
}

