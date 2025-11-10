"use client"

import { useEffect, useState } from 'react';
import { Tabs, TabsContent, TabsList, TabsTrigger } from '@/components/ui/tabs';
import { Button } from '@/components/ui/button';
import { PageHeader } from '@/components/page-header';
import { Save, Send } from 'lucide-react';
import { FormTab } from './filing-workspace/form-tab';
import { SchedulesTab } from './filing-workspace/schedules-tab';
import { AssessmentTab } from './filing-workspace/assessment-tab';
import { DocumentsTab } from './filing-workspace/documents-tab';
import { HistoryTab } from './filing-workspace/history-tab';
import type { TaxFilingDto } from '@/lib/services';
import { TaxFilingService } from '@/lib/services/tax-filing-service';

export interface FilingWorkspaceProps {
  filingId?: number;
  filing?: TaxFilingDto;
  onSave?: () => void;
  onSubmit?: () => void;
  mode?: 'create' | 'edit' | 'view';
}

export function FilingWorkspace({ 
  filingId, 
  filing, 
  onSave, 
  onSubmit,
  mode = 'edit' 
}: FilingWorkspaceProps) {
  const [activeTab, setActiveTab] = useState('form');
  const [isSaving, setIsSaving] = useState(false);
  const [isSubmitting, setIsSubmitting] = useState(false);
  const [loading, setLoading] = useState(false);
  const [fetchedFiling, setFetchedFiling] = useState<TaxFilingDto | undefined>(undefined);

  // Fetch filing when ID is provided and no filing prop is passed
  useEffect(() => {
    const load = async () => {
      if (!filing && filingId) {
        try {
          setLoading(true);
          const res = await TaxFilingService.getTaxFilingById(filingId);
          setFetchedFiling(res.data);
        } catch (err) {
          console.error('Failed to fetch tax filing', err);
        } finally {
          setLoading(false);
        }
      }
    };
    load();
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, [filingId]);

  const handleSaveDraft = async () => {
    setIsSaving(true);
    try {
      // Save logic will be implemented in form tab
      onSave?.();
    } finally {
      setIsSaving(false);
    }
  };

  const handleSubmit = async () => {
    if (!filingId) {
      onSubmit?.();
      return;
    }
    setIsSubmitting(true);
    try {
      await TaxFilingService.submitTaxFiling(filingId);
      onSubmit?.();
    } catch (err) {
      console.error('Submit filing failed', err);
    } finally {
      setIsSubmitting(false);
    }
  };

  const isReadOnly = mode === 'view';
  const currentFiling = filing ?? fetchedFiling;
  const filingTitle = currentFiling 
    ? `${currentFiling.taxType} Return - ${currentFiling.filingReference || `FY ${currentFiling.taxYear}`}`
    : 'New Tax Filing';

  return (
    <div className="flex-1 flex flex-col">
      <PageHeader
        title={filingTitle}
        breadcrumbs={[
          { label: 'Tax Filings', href: '/tax-filings' },
          { label: filing?.taxType || 'New Filing', href: filingId ? `/tax-filings/${filingId}` : undefined },
          { label: filing?.filingReference || 'Create' },
        ]}
        actions={
          !isReadOnly && (
            <div className="flex gap-2">
              <Button 
                variant="outline" 
                onClick={handleSaveDraft}
                disabled={isSaving || isSubmitting || loading}
              >
                <Save className="w-4 h-4 mr-2" />
                {isSaving ? 'Saving...' : 'Save Draft'}
              </Button>
              <Button 
                onClick={handleSubmit}
                disabled={isSaving || isSubmitting || loading}
              >
                <Send className="w-4 h-4 mr-2" />
                {isSubmitting ? 'Submitting...' : 'Submit Filing'}
              </Button>
            </div>
          )
        }
      />

      <div className="flex-1 p-6">
        <Tabs value={activeTab} onValueChange={setActiveTab} className="space-y-6">
          <TabsList className="grid w-full grid-cols-5 lg:w-auto lg:inline-grid">
            <TabsTrigger value="form">Form</TabsTrigger>
            <TabsTrigger value="schedules">Schedules</TabsTrigger>
            <TabsTrigger value="assessment">Assessment</TabsTrigger>
            <TabsTrigger value="documents">Documents</TabsTrigger>
            <TabsTrigger value="history">History</TabsTrigger>
          </TabsList>

          <TabsContent value="form" className="space-y-6">
            <FormTab filing={currentFiling} mode={mode} />
          </TabsContent>

          <TabsContent value="schedules" className="space-y-6">
            <SchedulesTab filing={currentFiling} mode={mode} />
          </TabsContent>

          <TabsContent value="assessment" className="space-y-6">
            <AssessmentTab filing={currentFiling} mode={mode} />
          </TabsContent>

          <TabsContent value="documents" className="space-y-6">
            <DocumentsTab filingId={filingId ?? currentFiling?.taxFilingId} mode={mode} />
          </TabsContent>

          <TabsContent value="history" className="space-y-6">
            <HistoryTab filingId={filingId ?? currentFiling?.taxFilingId} />
          </TabsContent>
        </Tabs>
      </div>
    </div>
  );
}
