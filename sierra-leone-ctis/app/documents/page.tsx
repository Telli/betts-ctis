'use client'

import { useState, useEffect } from 'react'
import { Card, CardContent, CardDescription, CardHeader, CardTitle } from '@/components/ui/card'
import { Badge } from '@/components/ui/badge'
import { Button } from '@/components/ui/button'
import { Tabs, TabsContent, TabsList, TabsTrigger } from '@/components/ui/tabs'
import { Input } from '@/components/ui/input'
import { Label } from '@/components/ui/label'
import { 
  Upload, 
  FileText, 
  Download, 
  Eye, 
  Trash2,
  Search,
  Filter,
  FolderOpen,
  CheckCircle,
  Clock,
  XCircle,
  File,
  Image,
  FileSpreadsheet,
  Plus
} from 'lucide-react'
import { format } from 'date-fns'
import { DocumentService, DocumentDto, DocumentStats } from '@/lib/services/document-service'
import { Dialog, DialogContent, DialogHeader, DialogTitle, DialogTrigger } from '@/components/ui/dialog'
import DocumentUploadForm from '@/components/document-upload-form'


export default function DocumentsPage() {
  const [documents, setDocuments] = useState<DocumentDto[]>([])
  const [stats, setStats] = useState<DocumentStats | null>(null)
  const [loading, setLoading] = useState(true)
  const [error, setError] = useState<string | null>(null)
  const [activeTab, setActiveTab] = useState('all')
  const [searchTerm, setSearchTerm] = useState('')
  const [filterCategory, setFilterCategory] = useState<string>('all')
  const [filterStatus, setFilterStatus] = useState<string>('all')
  const [showUploadDialog, setShowUploadDialog] = useState(false)

  const fetchDocumentData = async () => {
    try {
      setLoading(true)
      setError(null)
      
      const [documentList, documentStats] = await Promise.all([
        DocumentService.getDocuments({ 
          status: filterStatus === 'all' ? undefined : filterStatus as any,
          category: filterCategory === 'all' ? undefined : filterCategory as any,
          search: searchTerm || undefined
        }),
        DocumentService.getDocumentStats()
      ])
      
      setDocuments(documentList)
      setStats(documentStats)
    } catch (err) {
      console.error('Error fetching document data:', err)
      setError('Failed to load document data. Please try again later.')
    } finally {
      setLoading(false)
    }
  }

  useEffect(() => {
    fetchDocumentData()
  }, [filterStatus, filterCategory, searchTerm])

  const handleUploadComplete = (uploadedDocuments: any[]) => {
    setShowUploadDialog(false)
    fetchDocumentData() // Refresh the document list
  }

  const getFileIcon = (contentType: string) => {
    const type = contentType.split('/')[1] || contentType
    switch (type.toLowerCase()) {
      case 'pdf':
        return <FileText className="h-4 w-4 text-red-500" />
      case 'xlsx':
      case 'xls':
        return <FileSpreadsheet className="h-4 w-4 text-green-500" />
      case 'jpg':
      case 'jpeg':
      case 'png':
        return <Image className="h-4 w-4 text-blue-500" />
      default:
        return <File className="h-4 w-4 text-gray-500" />
    }
  }

  const getStatusIcon = (status: string) => {
    switch (status) {
      case 'verified':
        return <CheckCircle className="h-4 w-4 text-green-500" />
      case 'pending':
        return <Clock className="h-4 w-4 text-yellow-500" />
      case 'rejected':
        return <XCircle className="h-4 w-4 text-red-500" />
      case 'processed':
        return <CheckCircle className="h-4 w-4 text-blue-500" />
      default:
        return null
    }
  }

  const getStatusColor = (status: string) => {
    switch (status) {
      case 'verified':
        return 'bg-green-100 text-green-800'
      case 'pending':
        return 'bg-yellow-100 text-yellow-800'
      case 'rejected':
        return 'bg-red-100 text-red-800'
      case 'processed':
        return 'bg-blue-100 text-blue-800'
      default:
        return 'bg-gray-100 text-gray-800'
    }
  }

  const getCategoryColor = (category: string) => {
    switch (category) {
      case 'tax-return':
        return 'bg-sierra-blue-100 text-sierra-blue-800'
      case 'financial-statement':
        return 'bg-green-100 text-green-800'
      case 'supporting-document':
        return 'bg-orange-100 text-orange-800'
      case 'receipt':
        return 'bg-purple-100 text-purple-800'
      case 'correspondence':
        return 'bg-gray-100 text-gray-800'
      default:
        return 'bg-gray-100 text-gray-800'
    }
  }

  const formatFileSize = (bytes: number) => {
    if (bytes === 0) return '0 Bytes'
    const k = 1024
    const sizes = ['Bytes', 'KB', 'MB', 'GB']
    const i = Math.floor(Math.log(bytes) / Math.log(k))
    return parseFloat((bytes / Math.pow(k, i)).toFixed(2)) + ' ' + sizes[i]
  }

  const filteredDocuments = (documents || []).filter(doc => {
    const matchesSearch = doc.filename.toLowerCase().includes(searchTerm.toLowerCase()) ||
                         doc.description?.toLowerCase().includes(searchTerm.toLowerCase()) ||
                         doc.clientName?.toLowerCase().includes(searchTerm.toLowerCase())
    const matchesCategory = filterCategory === 'all' || doc.category === filterCategory
    const matchesStatus = filterStatus === 'all' || doc.status === filterStatus
    
    return matchesSearch && matchesCategory && matchesStatus
  })

  const displayStats = stats || {
    total: documents?.length || 0,
    pending: documents?.filter(d => d.status === 'pending').length || 0,
    verified: documents?.filter(d => d.status === 'verified').length || 0,
    rejected: documents?.filter(d => d.status === 'rejected').length || 0,
    totalSize: documents?.reduce((sum, doc) => sum + doc.fileSize, 0) || 0
  }

  if (loading) {
    return (
      <div className="flex items-center justify-center min-h-screen">
        <div className="animate-spin rounded-full h-32 w-32 border-b-2 border-sierra-blue"></div>
      </div>
    )
  }

  if (error) {
    return (
      <div className="flex items-center justify-center min-h-screen">
        <Card className="w-full max-w-md">
          <CardHeader>
            <CardTitle>Error Loading Documents</CardTitle>
          </CardHeader>
          <CardContent>
            <p className="text-muted-foreground mb-4">{error}</p>
            <Button onClick={() => window.location.reload()}>
              Retry
            </Button>
          </CardContent>
        </Card>
      </div>
    )
  }

  return (
    <div className="space-y-6">
      <div className="flex justify-between items-center">
        <div>
          <h1 className="text-3xl font-bold tracking-tight">Document Management</h1>
          <p className="text-muted-foreground mt-2">
            Manage tax documents, financial statements, and supporting files
          </p>
        </div>
        <Dialog open={showUploadDialog} onOpenChange={setShowUploadDialog}>
          <DialogTrigger asChild>
            <Button className="bg-sierra-blue-600 hover:bg-sierra-blue-700">
              <Plus className="mr-2 h-4 w-4" />
              Upload Documents
            </Button>
          </DialogTrigger>
          <DialogContent className="max-w-4xl max-h-[90vh] overflow-y-auto">
            <DialogHeader>
              <DialogTitle>Upload New Documents</DialogTitle>
            </DialogHeader>
            <DocumentUploadForm
              onUploadComplete={handleUploadComplete}
              onCancel={() => setShowUploadDialog(false)}
            />
          </DialogContent>
        </Dialog>
      </div>

      {/* Stats Cards */}
      <div className="grid gap-4 md:grid-cols-2 lg:grid-cols-5">
        <Card>
          <CardHeader className="flex flex-row items-center justify-between space-y-0 pb-2">
            <CardTitle className="text-sm font-medium">Total Documents</CardTitle>
            <FolderOpen className="h-4 w-4 text-muted-foreground" />
          </CardHeader>
          <CardContent>
            <div className="text-2xl font-bold">{displayStats.total}</div>
          </CardContent>
        </Card>

        <Card>
          <CardHeader className="flex flex-row items-center justify-between space-y-0 pb-2">
            <CardTitle className="text-sm font-medium">Pending Review</CardTitle>
            <Clock className="h-4 w-4 text-yellow-500" />
          </CardHeader>
          <CardContent>
            <div className="text-2xl font-bold text-yellow-600">{displayStats.pending}</div>
          </CardContent>
        </Card>

        <Card>
          <CardHeader className="flex flex-row items-center justify-between space-y-0 pb-2">
            <CardTitle className="text-sm font-medium">Verified</CardTitle>
            <CheckCircle className="h-4 w-4 text-green-500" />
          </CardHeader>
          <CardContent>
            <div className="text-2xl font-bold text-green-600">{displayStats.verified}</div>
          </CardContent>
        </Card>

        <Card>
          <CardHeader className="flex flex-row items-center justify-between space-y-0 pb-2">
            <CardTitle className="text-sm font-medium">Rejected</CardTitle>
            <XCircle className="h-4 w-4 text-red-500" />
          </CardHeader>
          <CardContent>
            <div className="text-2xl font-bold text-red-600">{displayStats.rejected}</div>
          </CardContent>
        </Card>

        <Card>
          <CardHeader className="flex flex-row items-center justify-between space-y-0 pb-2">
            <CardTitle className="text-sm font-medium">Storage Used</CardTitle>
            <FileText className="h-4 w-4 text-muted-foreground" />
          </CardHeader>
          <CardContent>
            <div className="text-2xl font-bold">{formatFileSize(displayStats.totalSize)}</div>
          </CardContent>
        </Card>
      </div>

      {/* Search and Filter */}
      <Card>
        <CardHeader>
          <CardTitle className="text-lg flex items-center gap-2">
            <Filter className="h-5 w-5" />
            Search & Filter
          </CardTitle>
        </CardHeader>
        <CardContent className="space-y-4">
          <div className="flex flex-col sm:flex-row gap-4">
            <div className="flex-1">
              <div className="relative">
                <Search className="absolute left-3 top-3 h-4 w-4 text-muted-foreground" />
                <Input
                  placeholder="Search documents..."
                  value={searchTerm}
                  onChange={(e) => setSearchTerm(e.target.value)}
                  className="pl-10"
                />
              </div>
            </div>
            <div className="w-full sm:w-48">
              <select
                value={filterStatus}
                onChange={(e) => setFilterStatus(e.target.value)}
                className="w-full px-3 py-2 border border-gray-300 rounded-md"
              >
                <option value="all">All Status</option>
                <option value="pending">Pending</option>
                <option value="verified">Verified</option>
                <option value="rejected">Rejected</option>
                <option value="processed">Processed</option>
              </select>
            </div>
            <div className="w-full sm:w-48">
              <select
                value={filterCategory}
                onChange={(e) => setFilterCategory(e.target.value)}
                className="w-full px-3 py-2 border border-gray-300 rounded-md"
              >
                <option value="all">All Categories</option>
                <option value="tax-return">Tax Return</option>
                <option value="financial-statement">Financial Statement</option>
                <option value="supporting-document">Supporting Document</option>
                <option value="receipt">Receipt</option>
                <option value="correspondence">Correspondence</option>
              </select>
            </div>
            <Button variant="outline" onClick={() => {setSearchTerm(''); setFilterStatus('all'); setFilterCategory('all')}}>
              Clear
            </Button>
          </div>
        </CardContent>
      </Card>

      {/* Documents List */}
      <Tabs defaultValue="list" className="space-y-4">
        <TabsList>
          <TabsTrigger value="list">List View</TabsTrigger>
          <TabsTrigger value="pending">Pending Review ({displayStats.pending})</TabsTrigger>
          <TabsTrigger value="recent">Recent Uploads</TabsTrigger>
        </TabsList>

        <TabsContent value="list">
          <Card>
            <CardHeader>
              <div className="flex items-center justify-between">
                <CardTitle>All Documents ({filteredDocuments.length})</CardTitle>
              </div>
            </CardHeader>
            <CardContent>
              {filteredDocuments.length === 0 ? (
                <div className="text-center py-12">
                  <FolderOpen className="h-12 w-12 text-gray-400 mx-auto mb-4" />
                  <h3 className="text-lg font-medium text-gray-900 mb-2">No documents found</h3>
                  <p className="text-gray-500 mb-6">Get started by uploading your first document.</p>
                  <Button onClick={() => setShowUploadDialog(true)} className="bg-sierra-blue-600 hover:bg-sierra-blue-700">
                    <Plus className="mr-2 h-4 w-4" />
                    Upload Document
                  </Button>
                </div>
              ) : (
                <div className="space-y-4">
                  {filteredDocuments.map((document) => (
              <div key={document.id} className="p-4 border rounded-lg">
                <div className="flex justify-between items-start">
                  <div className="space-y-2 flex-1">
                    <div className="flex items-center gap-2">
                      {getFileIcon(document.contentType)}
                      <span className="font-medium">{document.filename}</span>
                      <Badge className={getStatusColor(document.status)}>
                        {getStatusIcon(document.status)}
                        {document.status}
                      </Badge>
                      <Badge className={getCategoryColor(document.category)}>
                        {document.category.replace('-', ' ')}
                      </Badge>
                    </div>
                    
                    {document.description && (
                      <p className="text-sm text-muted-foreground">
                        {document.description}
                      </p>
                    )}
                    
                    <div className="flex items-center gap-4 text-sm text-muted-foreground">
                      <span>Size: {formatFileSize(document.fileSize)}</span>
                      <span>Uploaded: {format(new Date(document.uploadDate), 'MMM d, yyyy')}</span>
                      <span>By: {document.uploadedBy}</span>
                      {document.clientName && (
                        <span>Client: {document.clientName}</span>
                      )}
                      {document.taxYear && (
                        <span>Tax Year: {document.taxYear}</span>
                      )}
                    </div>
                    
                    <div className="flex gap-1">
                      {document.tags.map((tag, index) => (
                        <Badge key={index} variant="outline" className="text-xs">
                          {tag}
                        </Badge>
                      ))}
                    </div>
                  </div>
                  
                  <div className="flex gap-2 ml-4">
                    <Button variant="outline" size="sm">
                      <Eye className="mr-2 h-4 w-4" />
                      View
                    </Button>
                    <Button variant="outline" size="sm">
                      <Download className="mr-2 h-4 w-4" />
                      Download
                    </Button>
                    </div>
                  </div>
                </div>
              ))}
                </div>
              )}
            </CardContent>
          </Card>
        </TabsContent>

        <TabsContent value="pending">
          <Card>
            <CardHeader>
              <CardTitle>Pending Review ({(documents || []).filter(d => d.status === 'pending').length})</CardTitle>
              <CardDescription>Documents waiting for verification</CardDescription>
            </CardHeader>
            <CardContent>
              <div className="space-y-4">
                {(documents || []).filter(d => d.status === 'pending').map((document) => (
                  <div key={document.id} className="p-4 border border-yellow-200 rounded-lg bg-yellow-50">
                    <div className="flex justify-between items-start">
                      <div className="space-y-2 flex-1">
                        <div className="flex items-center gap-2">
                          {getFileIcon(document.contentType)}
                          <span className="font-medium">{document.filename}</span>
                          <Badge className={getStatusColor(document.status)}>
                            {getStatusIcon(document.status)}
                            {document.status}
                          </Badge>
                        </div>
                        <div className="text-sm text-muted-foreground">
                          Uploaded: {format(new Date(document.uploadDate), 'MMM d, yyyy')} • Size: {formatFileSize(document.fileSize)}
                        </div>
                      </div>
                      <div className="flex gap-2 ml-4">
                        <Button variant="outline" size="sm" className="text-green-600 hover:text-green-700">
                          <CheckCircle className="mr-2 h-4 w-4" />
                          Verify
                        </Button>
                        <Button variant="outline" size="sm" className="text-red-600 hover:text-red-700">
                          <XCircle className="mr-2 h-4 w-4" />
                          Reject
                        </Button>
                      </div>
                    </div>
                  </div>
                ))}
                {(documents || []).filter(d => d.status === 'pending').length === 0 && (
                  <div className="text-center py-8">
                    <CheckCircle className="h-12 w-12 text-green-400 mx-auto mb-4" />
                    <h3 className="text-lg font-medium text-gray-900 mb-2">No pending documents</h3>
                    <p className="text-gray-500">All documents have been reviewed.</p>
                  </div>
                )}
              </div>
            </CardContent>
          </Card>
        </TabsContent>

        <TabsContent value="recent">
          <Card>
            <CardHeader>
              <CardTitle>Recent Uploads</CardTitle>
              <CardDescription>Documents uploaded in the last 30 days</CardDescription>
            </CardHeader>
            <CardContent>
              <div className="space-y-4">
                {(documents || [])
                  .filter(d => {
                    const uploadDate = new Date(d.uploadDate)
                    const thirtyDaysAgo = new Date()
                    thirtyDaysAgo.setDate(thirtyDaysAgo.getDate() - 30)
                    return uploadDate >= thirtyDaysAgo
                  })
                  .sort((a, b) => new Date(b.uploadDate).getTime() - new Date(a.uploadDate).getTime())
                  .map((document) => (
                    <div key={document.id} className="p-4 border rounded-lg">
                      <div className="flex justify-between items-start">
                        <div className="space-y-2 flex-1">
                          <div className="flex items-center gap-2">
                            {getFileIcon(document.contentType)}
                            <span className="font-medium">{document.filename}</span>
                            <Badge className={getStatusColor(document.status)}>
                              {getStatusIcon(document.status)}
                              {document.status}
                            </Badge>
                          </div>
                          <div className="text-sm text-muted-foreground">
                            Uploaded: {format(new Date(document.uploadDate), 'MMM d, yyyy')} • Size: {formatFileSize(document.fileSize)}
                          </div>
                        </div>
                        <div className="flex gap-2 ml-4">
                          <Button variant="outline" size="sm">
                            <Eye className="mr-2 h-4 w-4" />
                            View
                          </Button>
                          <Button variant="outline" size="sm">
                            <Download className="mr-2 h-4 w-4" />
                            Download
                          </Button>
                        </div>
                      </div>
                    </div>
                  ))}
              </div>
            </CardContent>
          </Card>
        </TabsContent>
      </Tabs>
    </div>
  )
}