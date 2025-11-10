"use client"

import { useEffect, useMemo, useState } from 'react';
import { Card, CardContent, CardHeader, CardTitle } from '@/components/ui/card';
import { Button } from '@/components/ui/button';
import { Table, TableBody, TableCell, TableHead, TableHeader, TableRow } from '@/components/ui/table';
import { Badge } from '@/components/ui/badge';
import { Alert, AlertDescription } from '@/components/ui/alert';
import { useToast } from '@/components/ui/use-toast';
import { Dialog, DialogContent, DialogHeader, DialogTitle, DialogFooter } from '@/components/ui/dialog';
import { Label } from '@/components/ui/label';
import { Input } from '@/components/ui/input';
import { Textarea } from '@/components/ui/textarea';
import { Select, SelectContent, SelectItem, SelectTrigger, SelectValue } from '@/components/ui/select';
import { FileUp, FileText, Eye, Download, Trash2, RefreshCw } from 'lucide-react';
import { Progress } from '@/components/ui/progress';
import { DocumentService, type DocumentDto } from '@/lib/services/document-service';
import { format } from 'date-fns';
import { TaxFilingService } from '@/lib/services/tax-filing-service';
import { ClientService } from '@/lib/services/client-service';

export interface DocumentsTabProps {
  filingId?: number;
  mode?: 'create' | 'edit' | 'view';
}

interface DocumentItem {
  id: number;
  name: string;
  version: number;
  uploadedBy: string;
  date: string;
  status: 'verified' | 'scanning' | 'pending';
}

export function DocumentsTab({ filingId, mode = 'edit' }: DocumentsTabProps) {
  const isReadOnly = mode === 'view';

  const [documents, setDocuments] = useState<DocumentDto[]>([]);
  const [loading, setLoading] = useState(false);
  const [error, setError] = useState<string | null>(null);
  const { toast } = useToast();
  const [isUploadOpen, setIsUploadOpen] = useState(false);
  const [uploading, setUploading] = useState(false);
  const [clientId, setClientId] = useState<number | null>(null);
  const [uploadFiles, setUploadFiles] = useState<File[]>([]);
  const [uploadCategory, setUploadCategory] = useState<string>('supporting-document');
  const [uploadDescription, setUploadDescription] = useState<string>('');
  const [uploadTags, setUploadTags] = useState<string>('');
  const [requirements, setRequirements] = useState<Array<{
    category: string;
    required: boolean;
    description: string;
    acceptedFormats: string[];
    maxSizeMb: number;
  }>>([]);

  const [uploadProgress, setUploadProgress] = useState<Record<string, 'pending'|'uploading'|'done'|'error'|'skipped'>>({});
  const fileKey = (f: File) => `${f.name}__${f.size}__${f.lastModified}`;
  const [uploadPercent, setUploadPercent] = useState<Record<string, number>>({});

  const [isReplaceOpen, setIsReplaceOpen] = useState(false);
  const [replaceTarget, setReplaceTarget] = useState<DocumentDto | null>(null);
  const [replaceFile, setReplaceFile] = useState<File | null>(null);
  const [replaceUploading, setReplaceUploading] = useState(false);

  const normalize = (s?: string) => (s || '').toLowerCase().replace(/[^a-z]/g, '');
  const findReqForCategory = (cat: string) => {
    const needle = normalize(cat);
    return requirements.find(r => {
      const hay = normalize(r.category);
      // loose match to account for naming differences
      if (needle === hay) return true;
      if (needle.includes(hay) || hay.includes(needle)) return true;
      // special cases
      if (needle.includes('tax') && needle.includes('return') && hay.includes('taxreturn')) return true;
      if (needle.includes('financial') && hay.includes('financial')) return true;
      if (needle.includes('bank') && hay.includes('bank')) return true;
      if (needle.includes('receipt') && hay.includes('receipt')) return true;
      if (needle.includes('invoice') && hay.includes('invoice')) return true;
      if (needle.includes('payment') && hay.includes('payment')) return true;
      if (needle.includes('correspondence') && hay.includes('correspondence')) return true;
      return false;
    });
  };

  const handleReplaceSubmit = async () => {
    try {
      if (!replaceTarget) {
        toast({ variant: 'destructive', title: 'No document selected', description: 'Select a document to replace.' });
        return;
      }
      if (!replaceFile) {
        toast({ variant: 'destructive', title: 'No file selected', description: 'Choose a new file to upload.' });
        return;
      }
      const allowed = getAllowedFormatsForCategory(replaceCategory);
      const ext = '.' + (replaceFile.name.split('.').pop() || '').toLowerCase();
      const sizeOk = replaceFile.size <= (replaceMaxSizeMb * 1024 * 1024);
      const typeOk = !allowed.length || allowed.includes(ext);
      if (!typeOk) {
        toast({ variant: 'destructive', title: 'Invalid file type', description: `Allowed: ${allowed.join(', ')}` });
        return;
      }
      if (!sizeOk) {
        toast({ variant: 'destructive', title: 'File too large', description: `Max size: ${replaceMaxSizeMb} MB` });
        return;
      }

      setReplaceUploading(true);
      const id = String(replaceTarget.documentId ?? replaceTarget.id);
      await DocumentService.replace(id, {
        file: replaceFile,
        taxFilingId: filingId,
        description: replaceTarget.description,
      });
      toast({ title: 'Document replaced', description: `${replaceTarget.filename} updated.` });
      if (filingId) {
        const docs = await DocumentService.getDocumentsByFiling(filingId);
        setDocuments(docs);
      }
      setIsReplaceOpen(false);
      setReplaceFile(null);
      setReplaceTarget(null);
    } catch (e) {
      toast({ variant: 'destructive', title: 'Replace failed', description: 'Could not replace the document.' });
    } finally {
      setReplaceUploading(false);
    }
  };

  const allowedFormatsForSelected = useMemo(() => {
    const req = findReqForCategory(uploadCategory);
    if (req && req.acceptedFormats?.length) return req.acceptedFormats;
    const set = new Set<string>();
    requirements.forEach(r => r.acceptedFormats?.forEach(f => set.add(f)));
    return Array.from(set);
  }, [requirements, uploadCategory]);

  const maxSizeMbForSelected = useMemo(() => {
    const req = findReqForCategory(uploadCategory);
    if (req?.maxSizeMb) return req.maxSizeMb;
    const maxAcross = Math.max(0, ...requirements.map(r => r.maxSizeMb || 0));
    return maxAcross || 15;
  }, [requirements, uploadCategory]);

  const getAllowedFormatsForCategory = (category: string) => {
    const req = findReqForCategory(category);
    if (req && req.acceptedFormats?.length) return req.acceptedFormats;
    const set = new Set<string>();
    requirements.forEach(r => r.acceptedFormats?.forEach(f => set.add(f)));
    return Array.from(set);
  };

  const getMaxSizeMbForCategory = (category: string) => {
    const req = findReqForCategory(category);
    if (req?.maxSizeMb) return req.maxSizeMb;
    const maxAcross = Math.max(0, ...requirements.map(r => r.maxSizeMb || 0));
    return maxAcross || 15;
  };

  const acceptAttr = useMemo(() => {
    return allowedFormatsForSelected.length ? allowedFormatsForSelected.join(',') : undefined;
  }, [allowedFormatsForSelected]);

  // Replace dialog derived values
  const replaceCategory = (replaceTarget?.category as string) || 'supporting-document';
  const replaceAccept = useMemo(() => {
    const list = getAllowedFormatsForCategory(replaceCategory);
    return list.length ? list.join(',') : undefined;
  }, [requirements, replaceCategory]);
  const replaceMaxSizeMb = useMemo(() => getMaxSizeMbForCategory(replaceCategory), [requirements, replaceCategory]);

  useEffect(() => {
    const load = async () => {
      if (!filingId) {
        setDocuments([]);
        return;
      }
      try {
        setLoading(true);
        setError(null);
        const docs = await DocumentService.getDocumentsByFiling(filingId);
        setDocuments(docs);
        // Fetch clientId from filing for uploads
        try {
          const filing = await TaxFilingService.getTaxFilingById(filingId);
          setClientId(filing.data.clientId);
          try {
            const client = await ClientService.getById(filing.data.clientId);
            const catIndexToName = ['Large', 'Medium', 'Small', 'Micro'];
            const catName = typeof client.taxpayerCategory === 'number'
              ? (catIndexToName[client.taxpayerCategory] || 'Micro')
              : String(client.taxpayerCategory);
            const reqs = await DocumentService.getDocumentRequirements(String(filing.data.taxType), catName);
            setRequirements(reqs);
          } catch {}
        } catch (e) {
          // Non-fatal if clientId cannot be fetched now
        }
      } catch (err) {
        console.error('Failed to fetch filing documents', err);
        setDocuments([]);
        setError('Failed to load documents');
        toast({ variant: 'destructive', title: 'Error', description: 'Failed to load documents' });
      } finally {
        setLoading(false);
      }
    };
    load();
  }, [filingId]);

  const handleUpload = () => {
    setIsUploadOpen(true);
  };

  const handleUploadSubmit = async () => {
    try {
      if (!uploadFiles.length) {
        toast({ variant: 'destructive', title: 'No files selected', description: 'Please choose at least one file to upload.' });
        return;
      }
      if (!clientId) {
        toast({ variant: 'destructive', title: 'Missing client', description: 'Unable to determine client for this filing.' });
        return;
      }
      // initialize progress map
      setUploadProgress(prev => {
        const next = { ...prev };
        uploadFiles.forEach(f => { next[fileKey(f)] = 'pending'; });
        return next;
      });
      setUploadPercent(prev => {
        const next = { ...prev } as Record<string, number>;
        uploadFiles.forEach(f => { next[fileKey(f)] = 0; });
        return next;
      });
      setUploading(true);
      const tags = uploadTags
        .split(',')
        .map(t => t.trim())
        .filter(Boolean);
      const maxBytes = maxSizeMbForSelected * 1024 * 1024;
      const allowed = allowedFormatsForSelected;
      let successCount = 0;
      let skippedCount = 0;
      for (const file of uploadFiles) {
        const key = fileKey(file);
        const ext = '.' + (file.name.split('.').pop() || '').toLowerCase();
        const typeOk = !allowed.length || allowed.includes(ext);
        const sizeOk = file.size <= maxBytes;
        if (!typeOk || !sizeOk) {
          skippedCount++;
          setUploadProgress(prev => ({ ...prev, [key]: 'skipped' }));
          toast({
            variant: 'destructive',
            title: 'File skipped',
            description: !typeOk
              ? `${file.name} has unsupported type (${ext}).`
              : `${file.name} exceeds size limit (${maxSizeMbForSelected} MB).`
          });
          continue;
        }
        setUploadProgress(prev => ({ ...prev, [key]: 'uploading' }));
        setUploadPercent(prev => ({ ...prev, [key]: 0 }));
        try {
          await DocumentService.uploadWithProgress(
            clientId,
            {
              file,
              category: uploadCategory as any,
              description: uploadDescription || undefined,
              tags,
              taxFilingId: filingId,
            },
            (percent) => {
              setUploadPercent(prev => ({ ...prev, [key]: percent }));
            }
          );
          successCount++;
          setUploadProgress(prev => ({ ...prev, [key]: 'done' }));
          setUploadPercent(prev => ({ ...prev, [key]: 100 }));
        } catch (e) {
          skippedCount++;
          setUploadProgress(prev => ({ ...prev, [key]: 'error' }));
          toast({ variant: 'destructive', title: 'Upload failed', description: `Could not upload ${file.name}.` });
        }
      }
      if (successCount) {
        toast({ title: 'Upload complete', description: `${successCount} file(s) uploaded successfully.` });
      }
      if (skippedCount && !successCount) {
        toast({ variant: 'destructive', title: 'No files uploaded', description: 'All files were skipped or failed.' });
      }
      // Refresh list
      if (filingId) {
        const docs = await DocumentService.getDocumentsByFiling(filingId);
        setDocuments(docs);
      }
      // Reset and close
      setIsUploadOpen(false);
      setUploadFiles([]);
      setUploadCategory('supporting-document');
      setUploadDescription('');
      setUploadTags('');
      setUploadProgress({});
      setUploadPercent({});
    } catch (e) {
      console.error('Upload failed', e);
      toast({ variant: 'destructive', title: 'Upload failed', description: 'Could not upload the document.' });
    } finally {
      setUploading(false);
    }
  };

  const getStatusBadge = (status: string) => {
    switch (status) {
      case 'verified':
        return <Badge className="bg-green-600">Verified</Badge>;
      case 'processed':
        return <Badge className="bg-blue-600">Processed</Badge>;
      case 'pending':
        return <Badge variant="outline">Pending</Badge>;
      case 'rejected':
        return <Badge className="bg-red-600">Rejected</Badge>;
      default:
        return <Badge variant="secondary">{status}</Badge>;
    }
  };

  return (
    <Card>
      <CardHeader>
        <div className="flex items-center justify-between">
          <CardTitle>Supporting Documents</CardTitle>
          {!isReadOnly && (
            <Button onClick={handleUpload}>
              <FileUp className="w-4 h-4 mr-2" />
              Upload Document
            </Button>
          )}
        </div>
      </CardHeader>
      <CardContent>
        {error && (
          <Alert className="mb-4">
            <AlertDescription>{error}</AlertDescription>
          </Alert>
        )}
        <div className="border border-gray-200 rounded-lg overflow-hidden">
          <Table>
            <TableHeader>
              <TableRow>
                <TableHead>Document Name</TableHead>
                <TableHead>Type</TableHead>
                <TableHead>Uploaded By</TableHead>
                <TableHead>Date</TableHead>
                <TableHead>Status</TableHead>
                <TableHead className="w-[120px]">Actions</TableHead>
              </TableRow>
            </TableHeader>
            <TableBody>
              {loading && (
                <TableRow>
                  <TableCell colSpan={6} className="text-center text-gray-500 py-6">Loading documents...</TableCell>
                </TableRow>
              )}
              {!loading && documents.map((doc) => (
                <TableRow key={doc.documentId ?? doc.id}>
                  <TableCell>
                    <div className="flex items-center gap-2">
                      <FileText className="w-4 h-4 text-blue-600" />
                      {doc.filename}
                    </div>
                  </TableCell>
                  <TableCell>
                    <Badge variant="outline">{doc.contentType?.split('/')?.[1]?.toUpperCase() || 'FILE'}</Badge>
                  </TableCell>
                  <TableCell>{doc.uploadedBy}</TableCell>
                  <TableCell>{doc.uploadDate ? format(new Date(doc.uploadDate), 'yyyy-MM-dd') : ''}</TableCell>
                  <TableCell>{getStatusBadge(doc.status)}</TableCell>
                  <TableCell>
                    <div className="flex gap-1">
                      <Button
                        variant="ghost"
                        size="sm"
                        title="View"
                        onClick={async () => {
                          const id = doc.documentId ?? parseInt(String(doc.id), 10);
                          if (!id || Number.isNaN(id)) return;
                          try {
                            const blob = await DocumentService.downloadDocument(String(id));
                            const url = window.URL.createObjectURL(blob);
                            window.open(url, '_blank');
                          } catch (e) {
                            toast({ variant: 'destructive', title: 'Error', description: 'Failed to open document' });
                          }
                        }}
                      >
                        <Eye className="w-4 h-4" />
                      </Button>
                      <Button
                        variant="ghost"
                        size="sm"
                        title="Download"
                        onClick={async () => {
                          const id = doc.documentId ?? parseInt(String(doc.id), 10);
                          if (!id || Number.isNaN(id)) return;
                          try {
                            const blob = await DocumentService.downloadDocument(String(id));
                            const url = window.URL.createObjectURL(blob);
                            const a = document.createElement('a');
                            a.href = url;
                            a.download = doc.originalName || doc.filename;
                            document.body.appendChild(a);
                            a.click();
                            window.URL.revokeObjectURL(url);
                            document.body.removeChild(a);
                          } catch (e) {
                            toast({ variant: 'destructive', title: 'Error', description: 'Failed to download document' });
                          }
                        }}
                      >
                        <Download className="w-4 h-4" />
                      </Button>
                      {!isReadOnly && (
                        <>
                          <Button
                            variant="ghost"
                            size="sm"
                            title="Replace"
                            onClick={() => {
                              setReplaceTarget(doc);
                              setReplaceFile(null);
                              setIsReplaceOpen(true);
                            }}
                          >
                            <RefreshCw className="w-4 h-4" />
                          </Button>
                          <Button
                            variant="ghost"
                            size="sm"
                            title="Delete"
                            onClick={async () => {
                              const id = doc.documentId ?? parseInt(String(doc.id), 10);
                              if (!id || Number.isNaN(id)) return;
                              if (!confirm('Delete this document?')) return;
                              try {
                                await DocumentService.delete(String(id));
                                toast({ title: 'Deleted', description: `${doc.filename} removed.` });
                                if (filingId) {
                                  const docs = await DocumentService.getDocumentsByFiling(filingId);
                                  setDocuments(docs);
                                }
                              } catch (e) {
                                toast({ variant: 'destructive', title: 'Error', description: 'Failed to delete document' });
                              }
                            }}
                          >
                            <Trash2 className="w-4 h-4" />
                          </Button>
                        </>
                      )}
                    </div>
                  </TableCell>
                </TableRow>
              ))}
              {!loading && documents.length === 0 && (
                <TableRow>
                  <TableCell colSpan={6} className="text-center text-gray-500 py-8">
                    No documents uploaded yet. Click "Upload Document" to add supporting documents.
                  </TableCell>
                </TableRow>
              )}
            </TableBody>
          </Table>
        </div>

        {/* Upload Dialog */}
        <Dialog open={isUploadOpen} onOpenChange={setIsUploadOpen}>
          <DialogContent>
            <DialogHeader>
              <DialogTitle>Upload Document</DialogTitle>
            </DialogHeader>
            <div className="space-y-4">
              <div className="space-y-2">
                <Label>Files</Label>
                <div
                  className="border-2 border-dashed rounded-md p-4 text-sm text-muted-foreground"
                  onDragOver={(e) => { e.preventDefault(); e.stopPropagation(); }}
                  onDrop={(e) => {
                    e.preventDefault();
                    const files = Array.from(e.dataTransfer.files || []);
                    if (files.length) setUploadFiles(prev => [...prev, ...files]);
                  }}
                >
                  Drag & drop files here, or use the picker below.
                </div>
                <Input
                  type="file"
                  multiple
                  accept={acceptAttr}
                  onChange={(e) => setUploadFiles(Array.from(e.target.files || []))}
                />
                {allowedFormatsForSelected.length > 0 && (
                  <div className="text-xs text-muted-foreground">Accepted: {allowedFormatsForSelected.join(', ')}. Max size: {maxSizeMbForSelected} MB.</div>
                )}
                {uploadFiles.length > 0 && (
                  <div className="mt-2 max-h-48 overflow-auto border rounded">
                    {uploadFiles.map((f, idx) => (
                      <div key={idx} className="flex items-center justify-between px-2 py-2 text-sm">
                        <div className="min-w-0 flex-1 pr-2">
                          <div className="flex items-center justify-between gap-2">
                            <span className="truncate">{f.name}</span>
                            <span className="text-xs text-muted-foreground">{(f.size/1024/1024).toFixed(2)} MB</span>
                          </div>
                          <div className="flex items-center gap-2 mt-1">
                            <Progress value={uploadPercent[fileKey(f)] || 0} className="w-40 h-2" />
                            <span className="text-xs w-10 text-right">{uploadPercent[fileKey(f)]?.toFixed?.(0) || 0}%</span>
                            <span className="text-xs text-muted-foreground">{(uploadProgress[fileKey(f)] ?? 'pending')}</span>
                          </div>
                        </div>
                        <Button
                          variant="ghost"
                          size="sm"
                          disabled={uploading && (uploadProgress[fileKey(f)] === 'uploading')}
                          onClick={() => setUploadFiles(uploadFiles.filter((_, i) => i !== idx))}
                        >
                          Remove
                        </Button>
                      </div>
                    ))}
                  </div>
                )}
              </div>
              <div className="space-y-2">
                <Label>Category</Label>
                <Select value={uploadCategory} onValueChange={setUploadCategory}>
                  <SelectTrigger>
                    <SelectValue placeholder="Select category" />
                  </SelectTrigger>
                  <SelectContent>
                    <SelectItem value="supporting-document">Supporting Document</SelectItem>
                    <SelectItem value="tax-return">Tax Return</SelectItem>
                    <SelectItem value="financial-statement">Financial Statement</SelectItem>
                    <SelectItem value="receipt">Receipt</SelectItem>
                    <SelectItem value="invoice">Invoice</SelectItem>
                    <SelectItem value="payment-evidence">Payment Evidence</SelectItem>
                    <SelectItem value="bank-statement">Bank Statement</SelectItem>
                    <SelectItem value="correspondence">Correspondence</SelectItem>
                  </SelectContent>
                </Select>
              </div>
              <div className="space-y-2">
                <Label>Description (optional)</Label>
                <Textarea
                  placeholder="Brief description"
                  value={uploadDescription}
                  onChange={(e) => setUploadDescription(e.target.value)}
                />
              </div>
              <div className="space-y-2">
                <Label>Tags (optional, comma-separated)</Label>
                <Input
                  placeholder="e.g. Q4, 2024, signed"
                  value={uploadTags}
                  onChange={(e) => setUploadTags(e.target.value)}
                />
              </div>
            </div>
            <DialogFooter>
              <Button variant="outline" onClick={() => setIsUploadOpen(false)} disabled={uploading}>Cancel</Button>
              <Button onClick={handleUploadSubmit} disabled={uploading || uploadFiles.length === 0}>
                {uploading ? 'Uploading...' : `Upload ${uploadFiles.length || ''}`}
              </Button>
            </DialogFooter>
          </DialogContent>
        </Dialog>

        {/* Replace Dialog */}
        <Dialog open={isReplaceOpen} onOpenChange={setIsReplaceOpen}>
          <DialogContent>
            <DialogHeader>
              <DialogTitle>Replace Document</DialogTitle>
            </DialogHeader>
            {replaceTarget && (
              <div className="space-y-4">
                <div className="grid grid-cols-2 gap-4">
                  <div>
                    <Label>Current Name</Label>
                    <div className="text-sm mt-1">{replaceTarget.originalName || replaceTarget.filename}</div>
                  </div>
                  <div>
                    <Label>Category</Label>
                    <div className="text-sm mt-1">{replaceCategory}</div>
                  </div>
                </div>

                <div className="space-y-2">
                  <Label>New File</Label>
                  <Input
                    type="file"
                    accept={replaceAccept}
                    onChange={(e) => setReplaceFile((e.target.files && e.target.files[0]) || null)}
                  />
                  {replaceAccept && (
                    <div className="text-xs text-muted-foreground">Accepted: {replaceAccept}. Max size: {replaceMaxSizeMb} MB.</div>
                  )}
                </div>
              </div>
            )}
            <DialogFooter>
              <Button variant="outline" onClick={() => setIsReplaceOpen(false)} disabled={replaceUploading}>Cancel</Button>
              <Button onClick={handleReplaceSubmit} disabled={replaceUploading || !replaceFile}>
                {replaceUploading ? 'Replacing...' : 'Replace'}
              </Button>
            </DialogFooter>
          </DialogContent>
        </Dialog>

        {/* Document Requirements Info */}
        <div className="mt-6 p-4 bg-blue-50 rounded-lg border border-blue-200">
          <h4 className="font-semibold text-blue-900 mb-2">Required Documents:</h4>
          {requirements.length ? (
            <ul className="text-sm text-blue-800 space-y-1">
              {requirements.map((r, i) => (
                <li key={i}>
                  <span className="font-medium">{r.category}</span> {r.required ? '(Required)' : '(Optional)'}
                  {r.description ? ` — ${r.description}` : ''}
                  {r.acceptedFormats?.length ? ` | Formats: ${r.acceptedFormats.join(', ')}` : ''}
                  {r.maxSizeMb ? ` | Max: ${r.maxSizeMb} MB` : ''}
                </li>
              ))}
            </ul>
          ) : (
            <ul className="text-sm text-blue-800 space-y-1">
              <li>• Financial Statements (Annual)</li>
              <li>• Bank Statements (for filing period)</li>
              <li>• Sales/Purchase Records</li>
              <li>• Previous Tax Return (if applicable)</li>
            </ul>
          )}
        </div>
      </CardContent>
    </Card>
  );
}
