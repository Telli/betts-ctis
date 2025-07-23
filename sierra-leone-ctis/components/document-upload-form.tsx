'use client'

import React, { useState } from 'react'
import { useForm } from 'react-hook-form'
import { zodResolver } from '@hookform/resolvers/zod'
import * as z from 'zod'
import { Card, CardContent, CardDescription, CardHeader, CardTitle } from '@/components/ui/card'
import { Button } from '@/components/ui/button'
import { Input } from '@/components/ui/input'
import { Label } from '@/components/ui/label'
import { Textarea } from '@/components/ui/textarea'
import { Select, SelectContent, SelectItem, SelectTrigger, SelectValue } from '@/components/ui/select'
import { Badge } from '@/components/ui/badge'
import { useToast } from '@/components/ui/use-toast'
import FileUpload, { FileUploadFile } from '@/components/ui/file-upload'
import { DocumentService, DocumentUploadRequest } from '@/lib/services/document-service'
import { X, Tag } from 'lucide-react'

// Document categories with descriptions
const DOCUMENT_CATEGORIES = [
  {
    value: 'tax-return' as const,
    label: 'Tax Return',
    description: 'Annual tax returns and related forms',
    icon: '📊'
  },
  {
    value: 'financial-statement' as const,
    label: 'Financial Statement',
    description: 'Balance sheets, income statements, cash flow statements',
    icon: '💰'
  },
  {
    value: 'supporting-document' as const,
    label: 'Supporting Document',
    description: 'Receipts, invoices, contracts, and other supporting materials',
    icon: '📄'
  },
  {
    value: 'receipt' as const,
    label: 'Receipt',
    description: 'Payment receipts and transaction records',
    icon: '🧾'
  },
  {
    value: 'correspondence' as const,
    label: 'Correspondence',
    description: 'Letters, emails, and official communications',
    icon: '✉️'
  }
]

const uploadSchema = z.object({
  category: z.enum(['tax-return', 'financial-statement', 'supporting-document', 'receipt', 'correspondence']),
  description: z.string().optional(),
  tags: z.array(z.string()).optional(),
  taxYearId: z.number().optional()
})

type UploadFormData = z.infer<typeof uploadSchema>

interface DocumentUploadFormProps {
  clientId?: number
  onUploadComplete?: (documents: any[]) => void
  onCancel?: () => void
  className?: string
}

export function DocumentUploadForm({
  clientId = 1, // Default to first client for demo
  onUploadComplete,
  onCancel,
  className
}: DocumentUploadFormProps) {
  const { toast } = useToast()
  const [files, setFiles] = useState<FileUploadFile[]>([])
  const [uploading, setUploading] = useState(false)
  const [newTag, setNewTag] = useState('')
  const [tags, setTags] = useState<string[]>([])

  const form = useForm<UploadFormData>({
    resolver: zodResolver(uploadSchema),
    defaultValues: {
      category: 'supporting-document',
      description: '',
      tags: [],
      taxYearId: undefined
    }
  })

  const handleFilesSelected = (selectedFiles: File[]) => {
    const newFiles: FileUploadFile[] = selectedFiles.map(file => ({
      id: `${Date.now()}-${file.name}`,
      file,
      status: 'pending',
      progress: 0
    }))
    setFiles(prev => [...prev, ...newFiles])
  }

  const handleFileRemove = (fileId: string) => {
    setFiles(prev => prev.filter(f => f.id !== fileId))
  }

  const addTag = () => {
    if (newTag.trim() && !tags.includes(newTag.trim())) {
      setTags(prev => [...prev, newTag.trim()])
      setNewTag('')
    }
  }

  const removeTag = (tagToRemove: string) => {
    setTags(prev => prev.filter(tag => tag !== tagToRemove))
  }

  const handleKeyPress = (e: React.KeyboardEvent) => {
    if (e.key === 'Enter') {
      e.preventDefault()
      addTag()
    }
  }

  const updateFileStatus = (fileId: string, status: FileUploadFile['status'], progress?: number, error?: string) => {
    setFiles(prev => prev.map(f => 
      f.id === fileId 
        ? { ...f, status, progress: progress ?? f.progress, error } 
        : f
    ))
  }

  const onSubmit = async (data: UploadFormData) => {
    if (files.length === 0) {
      toast({
        variant: 'destructive',
        title: 'No files selected',
        description: 'Please select at least one file to upload.'
      })
      return
    }

    setUploading(true)
    const uploadedDocuments = []

    try {
      // Upload each file
      for (const fileUpload of files) {
        if (fileUpload.status === 'success') continue // Skip already uploaded files

        updateFileStatus(fileUpload.id, 'uploading', 0)

        try {
          const uploadRequest: DocumentUploadRequest = {
            file: fileUpload.file,
            category: data.category,
            description: data.description,
            tags: tags.length > 0 ? tags : undefined,
            taxYearId: data.taxYearId
          }

          // Simulate progress (since we don't have real progress from the API)
          const progressInterval = setInterval(() => {
            updateFileStatus(fileUpload.id, 'uploading', Math.min(90, fileUpload.progress + 10))
          }, 200)

          const result = await DocumentService.upload(clientId, uploadRequest)
          
          clearInterval(progressInterval)
          updateFileStatus(fileUpload.id, 'success', 100)
          uploadedDocuments.push(result)

          toast({
            title: 'File uploaded successfully',
            description: `${fileUpload.file.name} has been uploaded and is pending review.`
          })

        } catch (error: any) {
          const errorMessage = error.response?.data?.message || error.message || 'Upload failed'
          updateFileStatus(fileUpload.id, 'error', 0, errorMessage)
          
          toast({
            variant: 'destructive',
            title: 'Upload failed',
            description: `Failed to upload ${fileUpload.file.name}: ${errorMessage}`
          })
        }
      }

      // If all files uploaded successfully
      if (uploadedDocuments.length === files.length) {
        toast({
          title: 'All files uploaded',
          description: `Successfully uploaded ${uploadedDocuments.length} document${uploadedDocuments.length > 1 ? 's' : ''}.`
        })
        
        if (onUploadComplete) {
          onUploadComplete(uploadedDocuments)
        }
      }

    } catch (error) {
      toast({
        variant: 'destructive',
        title: 'Upload error',
        description: 'An unexpected error occurred during upload.'
      })
    } finally {
      setUploading(false)
    }
  }

  const selectedCategory = DOCUMENT_CATEGORIES.find(cat => cat.value === form.watch('category'))

  return (
    <Card className={className}>
      <CardHeader>
        <CardTitle>Upload Documents</CardTitle>
        <CardDescription>
          Upload tax documents, financial statements, and supporting materials for client review
        </CardDescription>
      </CardHeader>
      <CardContent>
        <form onSubmit={form.handleSubmit(onSubmit)} className="space-y-6">
          {/* Document Category */}
          <div className="space-y-2">
            <Label htmlFor="category">Document Category *</Label>
            <Select
              value={form.watch('category')}
              onValueChange={(value) => form.setValue('category', value as any)}
            >
              <SelectTrigger>
                <SelectValue placeholder="Select document type" />
              </SelectTrigger>
              <SelectContent>
                {DOCUMENT_CATEGORIES.map(category => (
                  <SelectItem key={category.value} value={category.value}>
                    <div className="flex items-center space-x-2">
                      <span>{category.icon}</span>
                      <div>
                        <p className="font-medium">{category.label}</p>
                        <p className="text-xs text-gray-500">{category.description}</p>
                      </div>
                    </div>
                  </SelectItem>
                ))}
              </SelectContent>
            </Select>
            {selectedCategory && (
              <p className="text-xs text-gray-500 mt-1">
                {selectedCategory.description}
              </p>
            )}
          </div>

          {/* Description */}
          <div className="space-y-2">
            <Label htmlFor="description">Description (Optional)</Label>
            <Textarea
              {...form.register('description')}
              placeholder="Add any additional notes about these documents..."
              rows={3}
            />
          </div>

          {/* Tags */}
          <div className="space-y-2">
            <Label htmlFor="tags">Tags (Optional)</Label>
            <div className="space-y-2">
              <div className="flex space-x-2">
                <Input
                  value={newTag}
                  onChange={(e) => setNewTag(e.target.value)}
                  onKeyPress={handleKeyPress}
                  placeholder="Add a tag..."
                />
                <Button 
                  type="button" 
                  variant="outline" 
                  onClick={addTag}
                  disabled={!newTag.trim()}
                >
                  <Tag className="h-4 w-4" />
                </Button>
              </div>
              
              {tags.length > 0 && (
                <div className="flex flex-wrap gap-2">
                  {tags.map(tag => (
                    <Badge key={tag} variant="secondary" className="flex items-center space-x-1">
                      <span>{tag}</span>
                      <button
                        type="button"
                        onClick={() => removeTag(tag)}
                        className="ml-1 hover:text-red-600"
                      >
                        <X className="h-3 w-3" />
                      </button>
                    </Badge>
                  ))}
                </div>
              )}
            </div>
          </div>

          {/* File Upload */}
          <div className="space-y-2">
            <Label>Files *</Label>
            <FileUpload
              files={files}
              onFilesSelected={handleFilesSelected}
              onFileRemove={handleFileRemove}
              acceptedFileTypes={['.pdf', '.xlsx', '.xls', '.csv', '.jpg', '.jpeg', '.png', '.doc', '.docx']}
              maxFileSize={10}
              maxFiles={10}
              disabled={uploading}
            />
          </div>

          {/* Actions */}
          <div className="flex justify-end space-x-2 pt-4">
            {onCancel && (
              <Button 
                type="button" 
                variant="outline" 
                onClick={onCancel}
                disabled={uploading}
              >
                Cancel
              </Button>
            )}
            <Button 
              type="submit" 
              disabled={files.length === 0 || uploading}
              className="bg-sierra-blue-600 hover:bg-sierra-blue-700"
            >
              {uploading ? 'Uploading...' : `Upload ${files.length} File${files.length !== 1 ? 's' : ''}`}
            </Button>
          </div>
        </form>
      </CardContent>
    </Card>
  )
}

export default DocumentUploadForm