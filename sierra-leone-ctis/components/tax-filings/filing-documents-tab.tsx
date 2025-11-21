'use client'

import { useState, useEffect } from 'react'
import { Card, CardContent, CardHeader, CardTitle } from '@/components/ui/card'
import { Button } from '@/components/ui/button'
import { Table, TableBody, TableCell, TableHead, TableHeader, TableRow } from '@/components/ui/table'
import { Badge } from '@/components/ui/badge'
import { FileUp, FileText, Download, Loader2 } from 'lucide-react'
import { useToast } from '@/hooks/use-toast'
import { TaxFilingService } from '@/lib/services/tax-filing-service'

interface FilingDocument {
  id: number
  name: string
  version: number
  uploadedBy: string
  uploadedAt: string
}

interface FilingDocumentsTabProps {
  filingId: number
}

export default function FilingDocumentsTab({ filingId }: FilingDocumentsTabProps) {
  const { toast } = useToast()
  const [documents, setDocuments] = useState<FilingDocument[]>([])
  const [loading, setLoading] = useState(true)
  const [uploading, setUploading] = useState(false)

  useEffect(() => {
    loadDocuments()
  }, [filingId])

  const loadDocuments = async () => {
    try {
      setLoading(true)
      const data = await TaxFilingService.getFilingDocuments(filingId)
      setDocuments(data)
    } catch (error) {
      console.error('Error loading documents:', error)
      toast({
        variant: 'destructive',
        title: 'Error',
        description: 'Failed to load documents',
      })
    } finally {
      setLoading(false)
    }
  }

  const handleUpload = async (e: React.ChangeEvent<HTMLInputElement>) => {
    const file = e.target.files?.[0]
    if (!file) return

    try {
      setUploading(true)
      await TaxFilingService.uploadFilingDocument(filingId, file)
      toast({
        title: 'Success',
        description: 'Document uploaded successfully',
      })
      loadDocuments()
    } catch (error) {
      console.error('Error uploading document:', error)
      toast({
        variant: 'destructive',
        title: 'Error',
        description: 'Failed to upload document',
      })
    } finally {
      setUploading(false)
    }
  }

  const handleDownload = async (doc: FilingDocument) => {
    try {
      await TaxFilingService.downloadFilingDocument(filingId, doc.id)
      toast({
        title: 'Success',
        description: 'Document downloaded',
      })
    } catch (error) {
      console.error('Error downloading document:', error)
      toast({
        variant: 'destructive',
        title: 'Error',
        description: 'Failed to download document',
      })
    }
  }

  return (
    <Card>
      <CardHeader>
        <div className="flex items-center justify-between">
          <CardTitle>Supporting Documents</CardTitle>
          <div>
            <input
              type="file"
              id="document-upload"
              className="hidden"
              onChange={handleUpload}
              disabled={uploading}
            />
            <Button asChild disabled={uploading}>
              <label htmlFor="document-upload" className="cursor-pointer">
                {uploading ? (
                  <>
                    <Loader2 className="w-4 h-4 mr-2 animate-spin" />
                    Uploading...
                  </>
                ) : (
                  <>
                    <FileUp className="w-4 h-4 mr-2" />
                    Upload Document
                  </>
                )}
              </label>
            </Button>
          </div>
        </div>
      </CardHeader>
      <CardContent>
        <div className="border border-border rounded-lg">
          <Table>
            <TableHeader>
              <TableRow>
                <TableHead>Document Name</TableHead>
                <TableHead>Version</TableHead>
                <TableHead>Uploaded By</TableHead>
                <TableHead>Date</TableHead>
                <TableHead className="w-[100px]">Actions</TableHead>
              </TableRow>
            </TableHeader>
            <TableBody>
              {loading ? (
                <TableRow>
                  <TableCell colSpan={5} className="text-center py-8">
                    <Loader2 className="w-6 h-6 animate-spin mx-auto" />
                  </TableCell>
                </TableRow>
              ) : documents.length === 0 ? (
                <TableRow>
                  <TableCell colSpan={5} className="text-center py-8 text-muted-foreground">
                    No documents uploaded yet
                  </TableCell>
                </TableRow>
              ) : (
                documents.map((doc) => (
                  <TableRow key={doc.id}>
                    <TableCell>
                      <div className="flex items-center gap-2">
                        <FileText className="w-4 h-4 text-primary" />
                        {doc.name}
                      </div>
                    </TableCell>
                    <TableCell>
                      <Badge variant="outline">v{doc.version}</Badge>
                    </TableCell>
                    <TableCell>{doc.uploadedBy}</TableCell>
                    <TableCell>{new Date(doc.uploadedAt).toLocaleDateString()}</TableCell>
                    <TableCell>
                      <Button variant="ghost" size="sm" onClick={() => handleDownload(doc)}>
                        <Download className="w-4 h-4" />
                      </Button>
                    </TableCell>
                  </TableRow>
                ))
              )}
            </TableBody>
          </Table>
        </div>
      </CardContent>
    </Card>
  )
}

