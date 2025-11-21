'use client'

import { useState, useEffect } from 'react'
import { useParams, useRouter } from 'next/navigation'
import { PageHeader } from '@/components/page-header'
import { Button } from '@/components/ui/button'
import { Tabs, TabsContent, TabsList, TabsTrigger } from '@/components/ui/tabs'
import { Save, Send, Loader2 } from 'lucide-react'
import { useToast } from '@/hooks/use-toast'
import { TaxFilingService } from '@/lib/services/tax-filing-service'
import FilingFormTab from '@/components/tax-filings/filing-form-tab'
import FilingSchedulesTab from '@/components/tax-filings/filing-schedules-tab'
import FilingAssessmentTab from '@/components/tax-filings/filing-assessment-tab'
import FilingDocumentsTab from '@/components/tax-filings/filing-documents-tab'
import FilingHistoryTab from '@/components/tax-filings/filing-history-tab'
import Loading from '@/app/loading'

export default function FilingWorkspacePage() {
  const params = useParams()
  const router = useRouter()
  const { toast } = useToast()
  const filingId = params.id as string

  const [activeTab, setActiveTab] = useState('form')
  const [loading, setLoading] = useState(true)
  const [saving, setSaving] = useState(false)
  const [submitting, setSubmitting] = useState(false)
  const [filing, setFiling] = useState<any>(null)

  useEffect(() => {
    loadFilingWorkspace()
  }, [filingId])

  const loadFilingWorkspace = async () => {
    try {
      setLoading(true)
      const data = await TaxFilingService.getFilingWorkspace(parseInt(filingId))
      setFiling(data)
    } catch (error) {
      console.error('Error loading filing workspace:', error)
      toast({
        variant: 'destructive',
        title: 'Error',
        description: 'Failed to load filing workspace',
      })
    } finally {
      setLoading(false)
    }
  }

  const handleSaveDraft = async () => {
    try {
      setSaving(true)
      await TaxFilingService.saveDraft(parseInt(filingId), filing)
      toast({
        title: 'Success',
        description: 'Draft saved successfully',
      })
    } catch (error) {
      console.error('Error saving draft:', error)
      toast({
        variant: 'destructive',
        title: 'Error',
        description: 'Failed to save draft',
      })
    } finally {
      setSaving(false)
    }
  }

  const handleSubmit = async () => {
    try {
      setSubmitting(true)
      await TaxFilingService.submitFiling(parseInt(filingId))
      toast({
        title: 'Success',
        description: 'Filing submitted successfully',
      })
      router.push('/tax-filings')
    } catch (error) {
      console.error('Error submitting filing:', error)
      toast({
        variant: 'destructive',
        title: 'Error',
        description: 'Failed to submit filing',
      })
    } finally {
      setSubmitting(false)
    }
  }

  if (loading) {
    return <Loading />
  }

  if (!filing) {
    return (
      <div className="p-6">
        <div className="text-center">
          <h2 className="text-xl font-semibold">Filing not found</h2>
        </div>
      </div>
    )
  }

  return (
    <div className="flex-1 flex flex-col">
      <PageHeader
        title={`${filing.taxType} - ${filing.period || filing.taxYear}`}
        breadcrumbs={[
          { label: 'Tax Filings', href: '/tax-filings' },
          { label: filing.taxType },
          { label: 'Workspace' },
        ]}
        actions={
          <div className="flex gap-2">
            <Button variant="outline" onClick={handleSaveDraft} disabled={saving}>
              {saving ? (
                <>
                  <Loader2 className="w-4 h-4 mr-2 animate-spin" />
                  Saving...
                </>
              ) : (
                <>
                  <Save className="w-4 h-4 mr-2" />
                  Save Draft
                </>
              )}
            </Button>
            <Button onClick={handleSubmit} disabled={submitting}>
              {submitting ? (
                <>
                  <Loader2 className="w-4 h-4 mr-2 animate-spin" />
                  Submitting...
                </>
              ) : (
                <>
                  <Send className="w-4 h-4 mr-2" />
                  Submit Filing
                </>
              )}
            </Button>
          </div>
        }
      />

      <div className="flex-1 p-6">
        <Tabs value={activeTab} onValueChange={setActiveTab}>
          <TabsList className="mb-6">
            <TabsTrigger value="form">Form</TabsTrigger>
            <TabsTrigger value="schedules">Schedules</TabsTrigger>
            <TabsTrigger value="assessment">Assessment</TabsTrigger>
            <TabsTrigger value="documents">Documents</TabsTrigger>
            <TabsTrigger value="history">History</TabsTrigger>
          </TabsList>

          <TabsContent value="form" className="space-y-6">
            <FilingFormTab filing={filing} onUpdate={setFiling} />
          </TabsContent>

          <TabsContent value="schedules" className="space-y-6">
            <FilingSchedulesTab filingId={parseInt(filingId)} />
          </TabsContent>

          <TabsContent value="assessment" className="space-y-6">
            <FilingAssessmentTab filingId={parseInt(filingId)} />
          </TabsContent>

          <TabsContent value="documents" className="space-y-6">
            <FilingDocumentsTab filingId={parseInt(filingId)} />
          </TabsContent>

          <TabsContent value="history" className="space-y-6">
            <FilingHistoryTab filingId={parseInt(filingId)} />
          </TabsContent>
        </Tabs>
      </div>
    </div>
  )
}

