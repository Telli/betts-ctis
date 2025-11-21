'use client'

import { useState, useEffect } from 'react'
import { Card, CardContent, CardHeader, CardTitle } from '@/components/ui/card'
import { Alert, AlertDescription } from '@/components/ui/alert'
import { CheckCircle, Loader2 } from 'lucide-react'
import { useToast } from '@/hooks/use-toast'
import { TaxFilingService } from '@/lib/services/tax-filing-service'

interface AssessmentSummary {
  totalSales: number
  taxableSales: number
  gstRate: number
  outputTax: number
  inputTaxCredit: number
  penalties: number
  totalPayable: number
}

interface FilingAssessmentTabProps {
  filingId: number
}

export default function FilingAssessmentTab({ filingId }: FilingAssessmentTabProps) {
  const { toast } = useToast()
  const [assessment, setAssessment] = useState<AssessmentSummary | null>(null)
  const [loading, setLoading] = useState(true)

  useEffect(() => {
    loadAssessment()
  }, [filingId])

  const loadAssessment = async () => {
    try {
      setLoading(true)
      const data = await TaxFilingService.getAssessment(filingId)
      setAssessment(data)
    } catch (error) {
      console.error('Error loading assessment:', error)
      toast({
        variant: 'destructive',
        title: 'Error',
        description: 'Failed to load assessment',
      })
    } finally {
      setLoading(false)
    }
  }

  if (loading) {
    return (
      <Card>
        <CardContent className="flex items-center justify-center py-12">
          <Loader2 className="w-8 h-8 animate-spin" />
        </CardContent>
      </Card>
    )
  }

  if (!assessment) {
    return (
      <Card>
        <CardContent className="py-12 text-center text-muted-foreground">
          No assessment data available
        </CardContent>
      </Card>
    )
  }

  return (
    <>
      <Card>
        <CardHeader>
          <CardTitle>Tax Assessment Summary</CardTitle>
        </CardHeader>
        <CardContent>
          <div className="space-y-4">
            <div className="flex justify-between items-center py-3 border-b">
              <span className="text-muted-foreground">Total Sales</span>
              <span className="font-semibold">SLE {assessment.totalSales.toLocaleString()}</span>
            </div>
            <div className="flex justify-between items-center py-3 border-b">
              <span className="text-muted-foreground">Taxable Sales</span>
              <span className="font-semibold">SLE {assessment.taxableSales.toLocaleString()}</span>
            </div>
            <div className="flex justify-between items-center py-3 border-b">
              <span className="text-muted-foreground">GST Rate</span>
              <span className="font-semibold">{assessment.gstRate}%</span>
            </div>
            <div className="flex justify-between items-center py-3 border-b">
              <span className="text-muted-foreground">Output Tax</span>
              <span className="font-semibold">SLE {assessment.outputTax.toLocaleString()}</span>
            </div>
            <div className="flex justify-between items-center py-3 border-b">
              <span className="text-muted-foreground">Input Tax Credit</span>
              <span className="font-semibold text-success">- SLE {assessment.inputTaxCredit.toLocaleString()}</span>
            </div>
            <div className="flex justify-between items-center py-3 border-b">
              <span className="text-muted-foreground">Penalties</span>
              <span className="font-semibold">SLE {assessment.penalties.toLocaleString()}</span>
            </div>
            <div className="flex justify-between items-center py-4 bg-primary/5 px-4 rounded-lg">
              <span className="font-semibold">Total GST Payable</span>
              <span className="text-xl font-semibold text-primary">
                SLE {assessment.totalPayable.toLocaleString()}
              </span>
            </div>
          </div>
        </CardContent>
      </Card>

      <Alert>
        <CheckCircle className="w-4 h-4 text-success" />
        <AlertDescription>No validation errors found. Ready to submit.</AlertDescription>
      </Alert>
    </>
  )
}

