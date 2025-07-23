'use client'

import React, { useCallback, useState } from 'react'
import { useDropzone } from 'react-dropzone'
import { Card, CardContent } from '@/components/ui/card'
import { Button } from '@/components/ui/button'
import { Progress } from '@/components/ui/progress'
import { Badge } from '@/components/ui/badge'
import { 
  Upload, 
  File, 
  FileText, 
  Image, 
  FileSpreadsheet,
  X,
  Check,
  AlertCircle,
  Loader2
} from 'lucide-react'
import { cn } from '@/lib/utils'

export interface FileUploadFile {
  id: string
  file: File
  status: 'pending' | 'uploading' | 'success' | 'error'
  progress: number
  error?: string
}

interface FileUploadProps {
  onFilesSelected: (files: File[]) => void
  onFileRemove: (fileId: string) => void
  onUploadStart?: (file: FileUploadFile) => void
  onUploadComplete?: (fileId: string, result?: any) => void
  onUploadError?: (fileId: string, error: string) => void
  acceptedFileTypes?: string[]
  maxFileSize?: number // in MB
  maxFiles?: number
  files: FileUploadFile[]
  disabled?: boolean
  className?: string
}

const getFileIcon = (file: File) => {
  const type = file.type.toLowerCase()
  const extension = file.name.split('.').pop()?.toLowerCase()

  if (type.startsWith('image/')) {
    return <Image className="h-8 w-8 text-blue-500" />
  }
  
  if (type === 'application/pdf') {
    return <FileText className="h-8 w-8 text-red-500" />
  }
  
  if (type.includes('spreadsheet') || ['xlsx', 'xls', 'csv'].includes(extension || '')) {
    return <FileSpreadsheet className="h-8 w-8 text-green-500" />
  }
  
  return <File className="h-8 w-8 text-gray-500" />
}

const formatFileSize = (bytes: number): string => {
  if (bytes === 0) return '0 B'
  const k = 1024
  const sizes = ['B', 'KB', 'MB', 'GB']
  const i = Math.floor(Math.log(bytes) / Math.log(k))
  return `${parseFloat((bytes / Math.pow(k, i)).toFixed(1))} ${sizes[i]}`
}

const getStatusIcon = (status: FileUploadFile['status']) => {
  switch (status) {
    case 'pending':
      return <File className="h-4 w-4 text-gray-500" />
    case 'uploading':
      return <Loader2 className="h-4 w-4 text-blue-500 animate-spin" />
    case 'success':
      return <Check className="h-4 w-4 text-green-500" />
    case 'error':
      return <AlertCircle className="h-4 w-4 text-red-500" />
    default:
      return <File className="h-4 w-4 text-gray-500" />
  }
}

const getStatusColor = (status: FileUploadFile['status']) => {
  switch (status) {
    case 'pending':
      return 'bg-gray-100 text-gray-800'
    case 'uploading':
      return 'bg-blue-100 text-blue-800'
    case 'success':
      return 'bg-green-100 text-green-800'
    case 'error':
      return 'bg-red-100 text-red-800'
    default:
      return 'bg-gray-100 text-gray-800'
  }
}

export function FileUpload({
  onFilesSelected,
  onFileRemove,
  onUploadStart,
  onUploadComplete,
  onUploadError,
  acceptedFileTypes = ['.pdf', '.xlsx', '.xls', '.csv', '.jpg', '.jpeg', '.png', '.doc', '.docx'],
  maxFileSize = 10, // 10MB default
  maxFiles = 10,
  files = [],
  disabled = false,
  className
}: FileUploadProps) {
  const [dragActive, setDragActive] = useState(false)

  const onDrop = useCallback((acceptedFiles: File[], rejectedFiles: any[]) => {
    // Handle rejected files
    if (rejectedFiles.length > 0) {
      rejectedFiles.forEach(rejection => {
        const { file, errors } = rejection
        const errorMessages = errors.map((e: any) => {
          switch (e.code) {
            case 'file-too-large':
              return `File is too large. Maximum size is ${maxFileSize}MB.`
            case 'file-invalid-type':
              return `File type not supported. Accepted types: ${acceptedFileTypes.join(', ')}`
            case 'too-many-files':
              return `Too many files. Maximum is ${maxFiles} files.`
            default:
              return e.message
          }
        }).join(' ')
        
        if (onUploadError) {
          onUploadError(file.name, errorMessages)
        }
      })
    }

    // Handle accepted files
    if (acceptedFiles.length > 0) {
      onFilesSelected(acceptedFiles)
    }
  }, [maxFileSize, maxFiles, acceptedFileTypes, onFilesSelected, onUploadError])

  const { getRootProps, getInputProps, isDragActive } = useDropzone({
    onDrop,
    accept: acceptedFileTypes.reduce((acc, type) => {
      acc[type.startsWith('.') ? `*${type}` : type] = [type]
      return acc
    }, {} as Record<string, string[]>),
    maxSize: maxFileSize * 1024 * 1024, // Convert MB to bytes
    maxFiles: maxFiles - files.length, // Subtract already selected files
    disabled: disabled || files.length >= maxFiles,
    onDragEnter: () => setDragActive(true),
    onDragLeave: () => setDragActive(false),
    onDropAccepted: () => setDragActive(false),
    onDropRejected: () => setDragActive(false)
  })

  return (
    <div className={cn("space-y-4", className)}>
      {/* Dropzone */}
      <Card 
        className={cn(
          "border-2 border-dashed transition-all duration-200 cursor-pointer",
          isDragActive || dragActive 
            ? "border-sierra-blue-400 bg-sierra-blue-50" 
            : "border-gray-300 hover:border-sierra-blue-300",
          disabled && "opacity-50 cursor-not-allowed",
          files.length >= maxFiles && "opacity-50 cursor-not-allowed"
        )}
      >
        <CardContent 
          {...getRootProps()} 
          className="p-8 text-center"
        >
          <input {...getInputProps()} />
          <div className="flex flex-col items-center space-y-4">
            <div className={cn(
              "p-4 rounded-full transition-colors",
              isDragActive || dragActive 
                ? "bg-sierra-blue-100" 
                : "bg-gray-100"
            )}>
              <Upload className={cn(
                "h-8 w-8",
                isDragActive || dragActive 
                  ? "text-sierra-blue-600" 
                  : "text-gray-400"
              )} />
            </div>
            
            <div className="space-y-2">
              <p className="text-lg font-medium">
                {isDragActive || dragActive ? (
                  'Drop files here'
                ) : (
                  'Drag & drop files here'
                )}
              </p>
              <p className="text-sm text-gray-500">
                or <Button variant="link" className="p-0 h-auto text-sierra-blue-600">browse files</Button>
              </p>
            </div>
            
            <div className="text-xs text-gray-400 space-y-1">
              <p>Accepted formats: {acceptedFileTypes.join(', ')}</p>
              <p>Maximum file size: {maxFileSize}MB</p>
              <p>Maximum files: {maxFiles} ({files.length} selected)</p>
            </div>
          </div>
        </CardContent>
      </Card>

      {/* File List */}
      {files.length > 0 && (
        <Card>
          <CardContent className="p-4">
            <div className="space-y-3">
              <h4 className="font-medium text-sm text-gray-700">
                Selected Files ({files.length}/{maxFiles})
              </h4>
              
              <div className="space-y-2">
                {files.map((fileUpload) => (
                  <div 
                    key={fileUpload.id}
                    className="flex items-center space-x-3 p-3 bg-gray-50 rounded-lg"
                  >
                    {/* File Icon */}
                    <div className="flex-shrink-0">
                      {getFileIcon(fileUpload.file)}
                    </div>
                    
                    {/* File Info */}
                    <div className="flex-1 min-w-0">
                      <div className="flex items-center space-x-2">
                        <p className="text-sm font-medium text-gray-900 truncate">
                          {fileUpload.file.name}
                        </p>
                        <Badge 
                          variant="outline" 
                          className={getStatusColor(fileUpload.status)}
                        >
                          <span className="flex items-center space-x-1">
                            {getStatusIcon(fileUpload.status)}
                            <span className="capitalize">{fileUpload.status}</span>
                          </span>
                        </Badge>
                      </div>
                      
                      <div className="flex items-center space-x-2 mt-1">
                        <p className="text-xs text-gray-500">
                          {formatFileSize(fileUpload.file.size)}
                        </p>
                        
                        {fileUpload.error && (
                          <p className="text-xs text-red-600">
                            {fileUpload.error}
                          </p>
                        )}
                      </div>
                      
                      {/* Progress Bar */}
                      {fileUpload.status === 'uploading' && (
                        <div className="mt-2">
                          <Progress value={fileUpload.progress} className="h-1" />
                        </div>
                      )}
                    </div>
                    
                    {/* Remove Button */}
                    <Button
                      variant="outline"
                      size="sm"
                      onClick={() => onFileRemove(fileUpload.id)}
                      className="flex-shrink-0 h-8 w-8 p-0"
                      disabled={fileUpload.status === 'uploading'}
                    >
                      <X className="h-4 w-4" />
                    </Button>
                  </div>
                ))}
              </div>
            </div>
          </CardContent>
        </Card>
      )}
    </div>
  )
}

export default FileUpload