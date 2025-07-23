"use client"

import React, { useState, useRef } from 'react';
import { useForm } from 'react-hook-form';
import { zodResolver } from '@hookform/resolvers/zod';
import { Card, CardContent, CardDescription, CardHeader, CardTitle } from '@/components/ui/card';
import { Button } from '@/components/ui/button';
import { Input } from '@/components/ui/input';
import { Label } from '@/components/ui/label';
import { Textarea } from '@/components/ui/textarea';
import { Select, SelectContent, SelectItem, SelectTrigger, SelectValue } from '@/components/ui/select';
import { useToast } from '@/hooks/use-toast';
import { Loader2, Upload, FileText, X, CheckCircle } from 'lucide-react';
import { documentUploadSchema, DocumentUploadFormData } from '@/lib/validations/client-portal';
import { ClientPortalService, ClientDocument } from '@/lib/services/client-portal-service';

interface DocumentUploadFormProps {
  onUploadSuccess?: (document: ClientDocument) => void;
  onCancel?: () => void;
}

export function DocumentUploadForm({ onUploadSuccess, onCancel }: DocumentUploadFormProps) {
  const [isLoading, setIsLoading] = useState(false);
  const [selectedFile, setSelectedFile] = useState<File | null>(null);
  const [uploadProgress, setUploadProgress] = useState(0);
  const fileInputRef = useRef<HTMLInputElement>(null);
  const { toast } = useToast();

  const {
    register,
    handleSubmit,
    formState: { errors },
    setValue,
    watch,
    reset
  } = useForm<DocumentUploadFormData>({
    resolver: zodResolver(documentUploadSchema)
  });

  const documentType = watch('documentType');

  const handleFileSelect = (event: React.ChangeEvent<HTMLInputElement>) => {
    const file = event.target.files?.[0];
    if (file) {
      setSelectedFile(file);
      setValue('file', file);
    }
  };

  const removeFile = () => {
    setSelectedFile(null);
    setValue('file', undefined as any);
    if (fileInputRef.current) {
      fileInputRef.current.value = '';
    }
  };

  const formatFileSize = (bytes: number): string => {
    if (bytes === 0) return '0 Bytes';
    const k = 1024;
    const sizes = ['Bytes', 'KB', 'MB', 'GB'];
    const i = Math.floor(Math.log(bytes) / Math.log(k));
    return parseFloat((bytes / Math.pow(k, i)).toFixed(2)) + ' ' + sizes[i];
  };

  const getFileIcon = (fileType: string) => {
    if (fileType.includes('pdf')) return '📄';
    if (fileType.includes('image')) return '🖼️';
    if (fileType.includes('word') || fileType.includes('document')) return '📝';
    if (fileType.includes('excel') || fileType.includes('sheet')) return '📊';
    return '📁';
  };

  const onSubmit = async (data: DocumentUploadFormData) => {
    try {
      setIsLoading(true);
      setUploadProgress(0);

      const formData = new FormData();
      formData.append('file', data.file);
      formData.append('documentType', data.documentType);
      formData.append('description', data.description);
      if (data.taxYear) {
        formData.append('taxYear', data.taxYear.toString());
      }

      // Simulate upload progress
      const progressInterval = setInterval(() => {
        setUploadProgress(prev => {
          if (prev >= 90) {
            clearInterval(progressInterval);
            return prev;
          }
          return prev + 10;
        });
      }, 200);

      const uploadedDocument = await ClientPortalService.uploadDocument(formData);
      
      clearInterval(progressInterval);
      setUploadProgress(100);
      
      toast({
        title: "Document Uploaded",
        description: `${uploadedDocument.originalFileName} has been successfully uploaded.`,
      });

      onUploadSuccess?.(uploadedDocument);
      reset();
      setSelectedFile(null);
    } catch (error) {
      console.error('Error uploading document:', error);
      toast({
        title: "Upload Failed",
        description: "Failed to upload document. Please try again.",
        variant: "destructive",
      });
      setUploadProgress(0);
    } finally {
      setIsLoading(false);
    }
  };

  const currentYear = new Date().getFullYear();

  return (
    <Card>
      <CardHeader>
        <CardTitle className="flex items-center space-x-2">
          <Upload className="h-5 w-5" />
          <span>Upload Document</span>
        </CardTitle>
        <CardDescription>
          Upload tax documents, receipts, and supporting files for your organization
        </CardDescription>
      </CardHeader>
      <CardContent>
        <form onSubmit={handleSubmit(onSubmit)} className="space-y-6">
          {/* File Upload */}
          <div className="space-y-2">
            <Label className="flex items-center space-x-2">
              <FileText className="h-4 w-4" />
              <span>Select File *</span>
            </Label>
            <div className="border-2 border-dashed border-gray-300 rounded-lg p-6 text-center hover:border-sierra-blue-400 transition-colors">
              {!selectedFile ? (
                <div>
                  <Upload className="h-8 w-8 text-gray-400 mx-auto mb-2" />
                  <p className="text-gray-600 mb-2">Click to select a file or drag and drop</p>
                  <p className="text-sm text-gray-500">PDF, Images, Word, Excel (Max 10MB)</p>
                  <Input
                    ref={fileInputRef}
                    type="file"
                    onChange={handleFileSelect}
                    accept=".pdf,.jpg,.jpeg,.png,.doc,.docx,.xls,.xlsx"
                    className="hidden"
                  />
                  <Button
                    type="button"
                    variant="outline"
                    className="mt-3"
                    onClick={() => fileInputRef.current?.click()}
                  >
                    Choose File
                  </Button>
                </div>
              ) : (
                <div className="flex items-center justify-between p-3 bg-gray-50 rounded border">
                  <div className="flex items-center space-x-3">
                    <span className="text-2xl">{getFileIcon(selectedFile.type)}</span>
                    <div className="text-left">
                      <p className="font-medium text-gray-900">{selectedFile.name}</p>
                      <p className="text-sm text-gray-600">{formatFileSize(selectedFile.size)}</p>
                    </div>
                  </div>
                  <Button
                    type="button"
                    variant="ghost"
                    size="sm"
                    onClick={removeFile}
                    className="text-red-600 hover:text-red-700"
                  >
                    <X className="h-4 w-4" />
                  </Button>
                </div>
              )}
            </div>
            {errors.file && (
              <p className="text-sm text-red-600">{errors.file.message}</p>
            )}
          </div>

          {/* Document Type */}
          <div className="space-y-2">
            <Label htmlFor="documentType">Document Type *</Label>
            <Select onValueChange={(value) => setValue('documentType', value as any)}>
              <SelectTrigger className={errors.documentType ? "border-red-500" : ""}>
                <SelectValue placeholder="Select document type" />
              </SelectTrigger>
              <SelectContent>
                <SelectItem value="tax_return">Tax Return</SelectItem>
                <SelectItem value="financial_statement">Financial Statement</SelectItem>
                <SelectItem value="supporting_document">Supporting Document</SelectItem>
                <SelectItem value="receipt">Receipt/Invoice</SelectItem>
                <SelectItem value="other">Other</SelectItem>
              </SelectContent>
            </Select>
            {errors.documentType && (
              <p className="text-sm text-red-600">{errors.documentType.message}</p>
            )}
          </div>

          {/* Tax Year (conditional) */}
          {(documentType === 'tax_return' || documentType === 'financial_statement') && (
            <div className="space-y-2">
              <Label htmlFor="taxYear">Tax Year</Label>
              <Select onValueChange={(value) => setValue('taxYear', parseInt(value))}>
                <SelectTrigger>
                  <SelectValue placeholder="Select tax year" />
                </SelectTrigger>
                <SelectContent>
                  {Array.from({ length: 6 }, (_, i) => currentYear - i).map(year => (
                    <SelectItem key={year} value={year.toString()}>{year}</SelectItem>
                  ))}
                </SelectContent>
              </Select>
              {errors.taxYear && (
                <p className="text-sm text-red-600">{errors.taxYear.message}</p>
              )}
            </div>
          )}

          {/* Description */}
          <div className="space-y-2">
            <Label htmlFor="description">Description *</Label>
            <Textarea
              id="description"
              {...register('description')}
              placeholder="Provide a brief description of the document"
              rows={3}
              className={errors.description ? "border-red-500" : ""}
            />
            {errors.description && (
              <p className="text-sm text-red-600">{errors.description.message}</p>
            )}
          </div>

          {/* Upload Progress */}
          {isLoading && (
            <div className="space-y-2">
              <div className="flex items-center justify-between">
                <span className="text-sm text-gray-600">Uploading...</span>
                <span className="text-sm text-gray-600">{uploadProgress}%</span>
              </div>
              <div className="w-full bg-gray-200 rounded-full h-2">
                <div 
                  className="bg-sierra-blue-600 h-2 rounded-full transition-all duration-300"
                  style={{ width: `${uploadProgress}%` }}
                />
              </div>
            </div>
          )}

          {/* Form Actions */}
          <div className="flex items-center justify-between pt-6 border-t">
            <p className="text-sm text-gray-600">
              * Required fields
            </p>
            <div className="flex space-x-3">
              {onCancel && (
                <Button
                  type="button"
                  variant="outline"
                  onClick={onCancel}
                  disabled={isLoading}
                >
                  Cancel
                </Button>
              )}
              <Button
                type="submit"
                disabled={!selectedFile || isLoading}
                className="min-w-32"
              >
                {isLoading ? (
                  <>
                    <Loader2 className="h-4 w-4 mr-2 animate-spin" />
                    Uploading...
                  </>
                ) : (
                  <>
                    <Upload className="h-4 w-4 mr-2" />
                    Upload Document
                  </>
                )}
              </Button>
            </div>
          </div>
        </form>
      </CardContent>
    </Card>
  );
}