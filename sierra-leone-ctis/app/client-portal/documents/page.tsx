"use client"

import React, { useState, useEffect } from 'react';
import { Card, CardContent, CardDescription, CardHeader, CardTitle } from '@/components/ui/card';
import { Button } from '@/components/ui/button';
import { Badge } from '@/components/ui/badge';
import { Input } from '@/components/ui/input';
import { Skeleton } from '@/components/ui/skeleton';
import { useToast } from '@/hooks/use-toast';
import { 
  FileText, 
  Upload, 
  Download, 
  Search, 
  Calendar,
  Filter,
  Plus,
  Eye,
  MoreHorizontal
} from 'lucide-react';
import { DocumentUploadForm } from '@/components/client-portal/forms/document-upload-form';
import { ClientPortalService, ClientDocument, PaginatedResponse } from '@/lib/services/client-portal-service';

export default function ClientDocumentsPage() {
  const [documents, setDocuments] = useState<PaginatedResponse<ClientDocument> | null>(null);
  const [loading, setLoading] = useState(true);
  const [showUploadForm, setShowUploadForm] = useState(false);
  const [searchTerm, setSearchTerm] = useState('');
  const [currentPage, setCurrentPage] = useState(1);
  const { toast } = useToast();

  const fetchDocuments = async (page: number = 1) => {
    try {
      setLoading(true);
      const data = await ClientPortalService.getDocuments(page, 10);
      setDocuments(data);
    } catch (error) {
      console.error('Error fetching documents:', error);
      toast({
        title: "Error",
        description: "Failed to load documents. Please try again.",
        variant: "destructive",
      });
    } finally {
      setLoading(false);
    }
  };

  useEffect(() => {
    fetchDocuments(currentPage);
  }, [currentPage]);

  const handleUploadSuccess = (newDocument: ClientDocument) => {
    setShowUploadForm(false);
    fetchDocuments(currentPage); // Refresh the list
  };

  const handleDownload = async (doc: ClientDocument) => {
    try {
      const blob = await ClientPortalService.downloadDocument(doc.documentId);
      const url = window.URL.createObjectURL(blob);
      const link = document.createElement('a');
      link.href = url;
      link.download = doc.originalFileName;
      document.body.appendChild(link);
      link.click();
      link.remove();
      window.URL.revokeObjectURL(url);
      
      toast({
        title: "Download Started",
        description: `${doc.originalFileName} is being downloaded.`,
      });
    } catch (error) {
      console.error('Error downloading document:', error);
      toast({
        title: "Download Failed",
        description: "Failed to download document. Please try again.",
        variant: "destructive",
      });
    }
  };

  const getDocumentIcon = (type: string) => {
    switch (type) {
      case 'tax_return': return '📄';
      case 'financial_statement': return '📊';
      case 'receipt': return '🧾';
      case 'supporting_document': return '📋';
      default: return '📁';
    }
  };

  const getVerificationStatusBadge = (status?: string) => {
    if (!status) return null;
    
    const statusConfig = {
      'NotRequested': { variant: 'secondary' as const, label: 'Not Requested' },
      'Requested': { variant: 'outline' as const, label: 'Requested' },
      'Submitted': { variant: 'default' as const, label: 'Submitted' },
      'UnderReview': { variant: 'secondary' as const, label: 'Under Review' },
      'Rejected': { variant: 'destructive' as const, label: 'Rejected' },
      'Verified': { variant: 'default' as const, label: 'Verified', className: 'bg-green-600' },
      'Filed': { variant: 'default' as const, label: 'Filed', className: 'bg-blue-600' }
    };
    
    const config = statusConfig[status as keyof typeof statusConfig];
    return config ? (
      <Badge variant={config.variant} className={'className' in config ? config.className : undefined}>
        {config.label}
      </Badge>
    ) : null;
  };

  const getDocumentTypeLabel = (type: string) => {
    switch (type) {
      case 'tax_return': return 'Tax Return';
      case 'financial_statement': return 'Financial Statement';
      case 'receipt': return 'Receipt/Invoice';
      case 'supporting_document': return 'Supporting Document';
      default: return 'Other';
    }
  };

  const formatFileSize = (bytes: number): string => {
    if (bytes === 0) return '0 Bytes';
    const k = 1024;
    const sizes = ['Bytes', 'KB', 'MB', 'GB'];
    const i = Math.floor(Math.log(bytes) / Math.log(k));
    return parseFloat((bytes / Math.pow(k, i)).toFixed(2)) + ' ' + sizes[i];
  };

  const filteredDocuments = documents?.items.filter(doc =>
    doc.originalFileName.toLowerCase().includes(searchTerm.toLowerCase()) ||
    doc.description.toLowerCase().includes(searchTerm.toLowerCase())
  );

  if (showUploadForm) {
    return (
      <div className="p-6">
        <div className="mb-6">
          <Button 
            variant="outline" 
            onClick={() => setShowUploadForm(false)}
            className="mb-4"
          >
            ← Back to Documents
          </Button>
        </div>
        <DocumentUploadForm 
          onUploadSuccess={handleUploadSuccess}
          onCancel={() => setShowUploadForm(false)}
        />
      </div>
    );
  }

  return (
    <div className="p-6 space-y-6">
      {/* Header */}
      <div className="flex items-center justify-between">
        <div>
          <h1 className="text-3xl font-bold text-gray-900">My Documents</h1>
          <p className="text-gray-600">Manage your tax documents and supporting files</p>
        </div>
        <Button onClick={() => setShowUploadForm(true)}>
          <Plus className="h-4 w-4 mr-2" />
          Upload Document
        </Button>
      </div>

      {/* Search and Filters */}
      <Card>
        <CardContent className="p-4">
          <div className="flex items-center space-x-4">
            <div className="flex-1 relative">
              <Search className="absolute left-3 top-1/2 transform -translate-y-1/2 text-gray-400 h-4 w-4" />
              <Input
                placeholder="Search documents..."
                value={searchTerm}
                onChange={(e) => setSearchTerm(e.target.value)}
                className="pl-10"
              />
            </div>
            <Button variant="outline" size="sm">
              <Filter className="h-4 w-4 mr-2" />
              Filter
            </Button>
          </div>
        </CardContent>
      </Card>

      {/* Documents List */}
      <Card>
        <CardHeader>
          <CardTitle className="flex items-center space-x-2">
            <FileText className="h-5 w-5" />
            <span>Documents</span>
            {documents && (
              <Badge variant="secondary">
                {documents.totalCount} total
              </Badge>
            )}
          </CardTitle>
          <CardDescription>
            Your uploaded tax documents and supporting files
          </CardDescription>
        </CardHeader>
        <CardContent>
          {loading ? (
            <div className="space-y-4">
              {[...Array(5)].map((_, i) => (
                <div key={i} className="flex items-center space-x-4 p-4 border rounded-lg">
                  <Skeleton className="h-10 w-10 rounded" />
                  <div className="flex-1 space-y-2">
                    <Skeleton className="h-4 w-3/4" />
                    <Skeleton className="h-3 w-1/2" />
                  </div>
                  <Skeleton className="h-8 w-20" />
                </div>
              ))}
            </div>
          ) : !filteredDocuments || filteredDocuments.length === 0 ? (
            <div className="text-center py-12">
              <FileText className="h-12 w-12 text-gray-400 mx-auto mb-4" />
              <h3 className="text-lg font-medium text-gray-900 mb-2">No documents found</h3>
              <p className="text-gray-600 mb-4">
                {searchTerm ? 'No documents match your search criteria.' : 'You haven\'t uploaded any documents yet.'}
              </p>
              <Button onClick={() => setShowUploadForm(true)}>
                <Upload className="h-4 w-4 mr-2" />
                Upload Your First Document
              </Button>
            </div>
          ) : (
            <div className="space-y-4">
              {filteredDocuments.map((document) => (
                <div key={document.documentId} className="flex items-center space-x-4 p-4 border rounded-lg hover:bg-gray-50 transition-colors">
                  <div className="text-2xl">
                    {getDocumentIcon(document.documentType)}
                  </div>
                  <div className="flex-1 min-w-0">
                    <div className="flex items-center space-x-2 mb-1">
                      <h3 className="font-medium text-gray-900 truncate">
                        {document.originalFileName}
                      </h3>
                      {getVerificationStatusBadge(document.verificationStatus)}
                      <Badge variant="outline" className="text-xs">
                        {getDocumentTypeLabel(document.documentType)}
                      </Badge>
                      {document.taxYear && (
                        <Badge variant="secondary" className="text-xs">
                          {document.taxYear}
                        </Badge>
                      )}
                    </div>
                    <p className="text-sm text-gray-600 truncate mb-1">
                      {document.description}
                    </p>
                    {document.verificationStatus === 'Rejected' && document.rejectionReason && (
                      <p className="text-sm text-red-600 mb-1">
                        <strong>Rejection Reason:</strong> {document.rejectionReason}
                      </p>
                    )}
                    {document.verificationNotes && (
                      <p className="text-sm text-blue-600 mb-1">
                        <strong>Review Notes:</strong> {document.verificationNotes}
                      </p>
                    )}
                    <div className="flex items-center space-x-4 text-xs text-gray-500">
                      <span className="flex items-center">
                        <Calendar className="h-3 w-3 mr-1" />
                        {new Date(document.uploadedAt).toLocaleDateString()}
                      </span>
                      <span>{formatFileSize(document.fileSize)}</span>
                      {document.reviewedAt && (
                        <span className="flex items-center">
                          <Eye className="h-3 w-3 mr-1" />
                          Reviewed {new Date(document.reviewedAt).toLocaleDateString()}
                        </span>
                      )}
                    </div>
                  </div>
                  <div className="flex items-center space-x-2">
                    <Button
                      variant="outline"
                      size="sm"
                      onClick={() => handleDownload(document)}
                    >
                      <Download className="h-4 w-4 mr-1" />
                      Download
                    </Button>
                    <Button variant="ghost" size="sm">
                      <MoreHorizontal className="h-4 w-4" />
                    </Button>
                  </div>
                </div>
              ))}
            </div>
          )}

          {/* Pagination */}
          {documents && documents.totalPages > 1 && (
            <div className="flex items-center justify-between mt-6 pt-6 border-t">
              <p className="text-sm text-gray-600">
                Showing {((currentPage - 1) * 10) + 1} to {Math.min(currentPage * 10, documents.totalCount)} of {documents.totalCount} documents
              </p>
              <div className="flex space-x-2">
                <Button
                  variant="outline"
                  size="sm"
                  onClick={() => setCurrentPage(prev => Math.max(1, prev - 1))}
                  disabled={currentPage === 1}
                >
                  Previous
                </Button>
                <Button
                  variant="outline"
                  size="sm"
                  onClick={() => setCurrentPage(prev => Math.min(documents.totalPages, prev + 1))}
                  disabled={currentPage === documents.totalPages}
                >
                  Next
                </Button>
              </div>
            </div>
          )}
        </CardContent>
      </Card>
    </div>
  );
}