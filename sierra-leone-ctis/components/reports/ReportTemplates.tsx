'use client'

import { useEffect, useMemo, useState } from 'react'
import { Card, CardContent, CardDescription, CardFooter, CardHeader, CardTitle } from '@/components/ui/card'
import { Button } from '@/components/ui/button'
import { Input } from '@/components/ui/input'
import { Textarea } from '@/components/ui/textarea'
import { Label } from '@/components/ui/label'
import { Select, SelectContent, SelectItem, SelectTrigger, SelectValue } from '@/components/ui/select'
import { Dialog, DialogContent, DialogDescription, DialogFooter as UIDialogFooter, DialogHeader, DialogTitle } from '@/components/ui/dialog'
import { Tabs, TabsContent, TabsList, TabsTrigger } from '@/components/ui/tabs'
import { Badge } from '@/components/ui/badge'
import { Eye, Plus, Play, RefreshCw, Pencil, Trash2, Download } from 'lucide-react'
import { useToast } from '@/components/ui/use-toast'
import { reportService, type ReportTemplate } from '@/lib/services/report-service'

type Props = {
  onTemplateSelected?: (template: ReportTemplate) => void
}

export default function ReportTemplates({ onTemplateSelected }: Props) {
  const { toast } = useToast()
  const [templates, setTemplates] = useState<ReportTemplate[]>([])
  const [loading, setLoading] = useState(true)
  const [creating, setCreating] = useState(false)
  const [previewUrl, setPreviewUrl] = useState<string | null>(null)
  const [previewContentType, setPreviewContentType] = useState<string>('application/pdf')
  const [previewOpen, setPreviewOpen] = useState(false)
  const [previewFormat, setPreviewFormat] = useState<'PDF' | 'Excel' | 'CSV'>('PDF')
  const [createOpen, setCreateOpen] = useState(false)
  const [editOpen, setEditOpen] = useState(false)
  const [editing, setEditing] = useState(false)
  const [selectedTemplate, setSelectedTemplate] = useState<ReportTemplate | null>(null)

  const reportTypes = useMemo(() => reportService.getReportTypes().map(rt => ({ value: rt.value, label: rt.label })), [])

  useEffect(() => {
    void loadTemplates()
    return () => {
      if (previewUrl) URL.revokeObjectURL(previewUrl)
    }
  }, [])

  const loadTemplates = async () => {
    setLoading(true)
    const res = await reportService.getTemplates()
    if (res.success && res.data) setTemplates(res.data)
    else {
      toast({ variant: 'destructive', title: 'Failed to load templates', description: res.error || 'Try again later.' })
      setTemplates([])
    }
    setLoading(false)
  }

  const handlePreview = async (tpl: ReportTemplate, preferredFormat: 'PDF' | 'Excel' | 'CSV' = 'PDF') => {
    try {
      setSelectedTemplate(tpl)
      if (previewUrl) URL.revokeObjectURL(previewUrl)
      const res = await reportService.previewTemplate(tpl.id, tpl.parameters || {}, { format: preferredFormat })
      if (res.success && res.data) {
        setPreviewUrl(res.data.url)
        setPreviewContentType(res.data.contentType)
        setPreviewFormat(preferredFormat)
        setPreviewOpen(true)
      } else {
        throw new Error(res.error || 'Preview failed')
      }
    } catch (err) {
      toast({ variant: 'destructive', title: 'Preview error', description: err instanceof Error ? err.message : 'Unable to preview template' })
    }
  }

  const handleEdit = (tpl: ReportTemplate) => {
    setSelectedTemplate(tpl)
    setEditOpen(true)
  }

  const handleDelete = async (tpl: ReportTemplate) => {
    if (!confirm('Delete this template? This action cannot be undone.')) return
    const res = await reportService.deleteTemplate(tpl.id)
    if (res.success) {
      toast({ title: 'Template deleted' })
      void loadTemplates()
    } else {
      toast({ variant: 'destructive', title: 'Delete failed', description: res.error || 'Please try again.' })
    }
  }

  return (
    <div className="space-y-4">
      <div className="flex items-center justify-between">
        <div>
          <h3 className="text-lg font-semibold">Templates</h3>
          <p className="text-sm text-muted-foreground">Save report configurations and reuse them anytime</p>
        </div>
        <div className="flex items-center gap-2">
          <Button variant="outline" size="sm" onClick={loadTemplates}>
            <RefreshCw className="h-4 w-4 mr-2" /> Refresh
          </Button>
          <Button size="sm" onClick={() => setCreateOpen(true)}>
            <Plus className="h-4 w-4 mr-2" /> New Template
          </Button>
        </div>
      </div>

      {loading ? (
        <Card>
          <CardContent className="p-6 text-sm text-muted-foreground">Loading templates…</CardContent>
        </Card>
      ) : templates.length === 0 ? (
        <Card>
          <CardHeader>
            <CardTitle>No templates yet</CardTitle>
            <CardDescription>Create your first template to speed up report generation.</CardDescription>
          </CardHeader>
          <CardFooter>
            <Button onClick={() => setCreateOpen(true)}>
              <Plus className="h-4 w-4 mr-2" /> Create Template
            </Button>
          </CardFooter>
        </Card>
      ) : (
        <div className="grid gap-4 md:grid-cols-2 lg:grid-cols-3">
          {templates.map((tpl) => (
            <Card key={tpl.id} className="flex flex-col">
              <CardHeader>
                <div className="flex items-start justify-between gap-2">
                  <div>
                    <CardTitle className="text-base">{tpl.name}</CardTitle>
                    <CardDescription className="line-clamp-2">{tpl.description}</CardDescription>
                  </div>
                  <Badge variant="secondary">{tpl.reportType}</Badge>
                </div>
              </CardHeader>
              <CardContent className="text-sm text-muted-foreground">
                <div className="flex items-center justify-between">
                  <span>Updated {new Date(tpl.updatedAt).toLocaleDateString()}</span>
                  {tpl.isDefault && <Badge>Default</Badge>}
                </div>
              </CardContent>
              <CardFooter className="mt-auto flex justify-end gap-2">
                <Button variant="outline" size="sm" onClick={() => handlePreview(tpl)}>
                  <Eye className="h-4 w-4 mr-2" /> Preview
                </Button>
                <Button variant="outline" size="sm" onClick={() => handleEdit(tpl)}>
                  <Pencil className="h-4 w-4 mr-2" /> Edit
                </Button>
                {!tpl.isDefault && (
                  <Button variant="destructive" size="sm" onClick={() => handleDelete(tpl)}>
                    <Trash2 className="h-4 w-4 mr-2" /> Delete
                  </Button>
                )}
                {onTemplateSelected && (
                  <Button size="sm" onClick={() => onTemplateSelected(tpl)}>
                    <Play className="h-4 w-4 mr-2" /> Use
                  </Button>
                )}
              </CardFooter>
            </Card>
          ))}
        </div>
      )}

      {/* Create Template Dialog */}
      <Dialog open={createOpen} onOpenChange={setCreateOpen}>
        <DialogContent className="sm:max-w-[640px]">
          <DialogHeader>
            <DialogTitle>Create Template</DialogTitle>
            <DialogDescription>Define a reusable report template</DialogDescription>
          </DialogHeader>
          <CreateTemplateForm
            reportTypes={reportTypes}
            submitting={creating}
            onCancel={() => setCreateOpen(false)}
            onSubmit={async (payload) => {
              setCreating(true)
              const res = await reportService.saveTemplate({
                name: payload.name,
                description: payload.description,
                reportType: payload.reportType,
                parameters: payload.parameters,
                isDefault: payload.isDefault ?? false,
              })
              setCreating(false)
              if (res.success) {
                toast({ title: 'Template saved' })
                setCreateOpen(false)
                loadTemplates()
              } else {
                toast({ variant: 'destructive', title: 'Save failed', description: res.error || 'Please try again.' })
              }
            }}
          />
          <UIDialogFooter>
            <p className="text-xs text-muted-foreground">You can override parameters later when generating a report.</p>
          </UIDialogFooter>
        </DialogContent>
      </Dialog>

      {/* Preview Dialog */}
      <Dialog open={previewOpen} onOpenChange={(open) => { setPreviewOpen(open); if (!open && previewUrl) { URL.revokeObjectURL(previewUrl); setPreviewUrl(null); setPreviewContentType('application/pdf'); setPreviewFormat('PDF') } }}>
        <DialogContent className="sm:max-w-[900px] h-[80vh]">
          <DialogHeader>
            <DialogTitle>Template Preview</DialogTitle>
            <DialogDescription>Rendered using current template parameters</DialogDescription>
          </DialogHeader>
          <div className="flex items-center justify-between pb-2">
            <div className="text-sm text-muted-foreground">Preview format</div>
            <div>
              <Select value={previewFormat} onValueChange={(val) => {
                const fmt = (val as 'PDF' | 'Excel' | 'CSV')
                setPreviewFormat(fmt)
                if (selectedTemplate) {
                  void handlePreview(selectedTemplate, fmt)
                }
              }}>
                <SelectTrigger className="w-[160px]"><SelectValue placeholder="Select format" /></SelectTrigger>
                <SelectContent>
                  <SelectItem value="PDF">PDF</SelectItem>
                  <SelectItem value="Excel">Excel</SelectItem>
                  <SelectItem value="CSV">CSV</SelectItem>
                </SelectContent>
              </Select>
            </div>
          </div>
          <div className="flex-1 h-full">
            {!previewUrl && (
              <div className="text-sm text-muted-foreground">Generating preview…</div>
            )}
            {previewUrl && previewContentType.includes('pdf') && (
              <iframe src={previewUrl} className="w-full h-[65vh] border rounded" />
            )}
            {previewUrl && !previewContentType.includes('pdf') && (
              <div className="text-sm text-muted-foreground flex items-center justify-between">
                <span>Preview is not a PDF ({previewContentType}). Download to view.</span>
                <a href={previewUrl} download className="inline-flex items-center gap-2 text-primary hover:underline">
                  <Download className="h-4 w-4" /> Download Preview
                </a>
              </div>
            )}
          </div>
        </DialogContent>
      </Dialog>

      {/* Edit Template Dialog */}
      <Dialog open={editOpen} onOpenChange={setEditOpen}>
        <DialogContent className="sm:max-w-[640px]">
          <DialogHeader>
            <DialogTitle>Edit Template</DialogTitle>
            <DialogDescription>Update template details</DialogDescription>
          </DialogHeader>
          {selectedTemplate && (
            <CreateTemplateForm
              reportTypes={reportTypes}
              submitting={editing}
              onCancel={() => setEditOpen(false)}
              onSubmit={async (payload) => {
                setEditing(true)
                const res = await reportService.updateTemplate(selectedTemplate.id, payload)
                setEditing(false)
                if (res.success) {
                  toast({ title: 'Template updated' })
                  setEditOpen(false)
                  setSelectedTemplate(null)
                  loadTemplates()
                } else {
                  toast({ variant: 'destructive', title: 'Update failed', description: res.error || 'Please try again.' })
                }
              }}
              initialData={{
                name: selectedTemplate.name,
                description: selectedTemplate.description,
                reportType: selectedTemplate.reportType,
                parameters: selectedTemplate.parameters,
                isDefault: selectedTemplate.isDefault
              }}
            />
          )}
        </DialogContent>
      </Dialog>
    </div>
  )
}

type CreateTemplateFormProps = {
  reportTypes: { value: string; label: string }[]
  submitting?: boolean
  onSubmit: (payload: { name: string; description: string; reportType: string; parameters: Record<string, any>; isDefault?: boolean }) => void
  onCancel: () => void
  initialData?: { name: string; description: string; reportType: string; parameters: Record<string, any>; isDefault?: boolean }
}

function CreateTemplateForm({ reportTypes, submitting, onSubmit, onCancel, initialData }: CreateTemplateFormProps) {
  const [name, setName] = useState(initialData?.name ?? '')
  const [description, setDescription] = useState(initialData?.description ?? '')
  const [reportType, setReportType] = useState<string>(initialData?.reportType ?? '')
  const [parametersText, setParametersText] = useState<string>(() => {
    try {
      return initialData?.parameters ? JSON.stringify(initialData.parameters, null, 2) : '{}'
    } catch {
      return '{}'
    }
  })
  const [isDefault, setIsDefault] = useState<boolean>(initialData?.isDefault ?? false)
  const { toast } = useToast()

  const handleSubmit = () => {
    try {
      const parsed = parametersText.trim() ? JSON.parse(parametersText) : {}
      onSubmit({ name, description, reportType, parameters: parsed, isDefault })
    } catch (e) {
      toast({ variant: 'destructive', title: 'Invalid parameters JSON', description: 'Please provide valid JSON for parameters.' })
    }
  }

  return (
    <div className="space-y-4">
      <div className="grid gap-4 md:grid-cols-2">
        <div className="space-y-2">
          <Label htmlFor="tpl-name">Name</Label>
          <Input id="tpl-name" value={name} onChange={(e) => setName(e.target.value)} placeholder="e.g. Monthly Payment History" />
        </div>
        <div className="space-y-2">
          <Label htmlFor="tpl-type">Report Type</Label>
          <Select value={reportType} onValueChange={setReportType}>
            <SelectTrigger id="tpl-type"><SelectValue placeholder="Select type" /></SelectTrigger>
            <SelectContent>
              {reportTypes.map(rt => (
                <SelectItem key={rt.value} value={rt.value}>{rt.label}</SelectItem>
              ))}
            </SelectContent>
          </Select>
        </div>
      </div>
      <div className="space-y-2">
        <Label htmlFor="tpl-desc">Description</Label>
        <Textarea id="tpl-desc" value={description} onChange={(e) => setDescription(e.target.value)} placeholder="What does this template generate?" />
      </div>
      <div className="flex items-center gap-2">
        <input id="tpl-default" type="checkbox" checked={isDefault} onChange={(e) => setIsDefault(e.target.checked)} />
        <Label htmlFor="tpl-default">Mark as default for this report type</Label>
      </div>
      <Tabs defaultValue="builder">
        <TabsList>
          <TabsTrigger value="builder">Parameters (JSON)</TabsTrigger>
        </TabsList>
        <TabsContent value="builder">
          <div className="space-y-2">
            <Label htmlFor="tpl-params">Parameters</Label>
            <Textarea id="tpl-params" className="font-mono text-xs" rows={10} value={parametersText} onChange={(e) => setParametersText(e.target.value)} />
            <p className="text-xs text-muted-foreground">Provide key/value pairs used during generation. Example: {`{"includeDetails":true,"paymentMethod":"All"}`}</p>
          </div>
        </TabsContent>
      </Tabs>

      <div className="flex justify-end gap-2 pt-2">
        <Button type="button" variant="outline" onClick={onCancel}>Cancel</Button>
        <Button type="button" onClick={handleSubmit} disabled={submitting || !name || !reportType}>
          {submitting ? 'Saving…' : 'Save Template'}
        </Button>
      </div>
    </div>
  )
}
