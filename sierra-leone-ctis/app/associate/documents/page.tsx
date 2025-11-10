'use client';

import { useState, useEffect } from 'react';
import { Card, CardContent, CardDescription, CardHeader, CardTitle } from '@/components/ui/card';
import { Button } from '@/components/ui/button';
import { Badge } from '@/components/ui/badge';
import { Input } from '@/components/ui/input';
import { Select, SelectContent, SelectItem, SelectTrigger, SelectValue } from '@/components/ui/select';
import { Textarea } from '@/components/ui/textarea';
import { Dialog, DialogContent, DialogDescription, DialogFooter, DialogHeader, DialogTitle, DialogTrigger } from '@/components/ui/dialog';
import { useToast } from '@/hooks/use-toast';
import { useAuth } from '@/context/auth-context';
import {
  FileText,
  Search,
  Filter,
  Eye,
  Check,
  X,
  Download,
  Calendar,
  User,
  Building2,
  Clock,
  AlertCircle,
  CheckCircle,
  XCircle
} from 'lucide-react';
import { apiClient } from '@/lib/api-client';

interface DocumentReviewItem {
  documentId: number;
  clientId: number;
  clientName: string;
  businessName: string;
  originalFileName: string;
  documentType: string;
  description: string;
  uploadedAt: string;
  fileSize: number;
  verificationStatus: 'NotRequested' | 'Requested' | 'Submitted' | 'UnderReview' | 'Rejected' | 'Verified' | 'Filed';
  verificationNotes?: string;
  rejectionReason?: string;
  reviewedAt?: string;
  reviewedBy?: string;
  taxYear?: number;
}

interface DocumentReviewDialogProps {
  document: DocumentReviewItem | null;
  isOpen: boolean;
  onClose: () => void;
  onReview: (documentId: number, status: string, notes: string, rejectionReason?: string) => void;
}

function DocumentReviewDialog({ document, isOpen, onClose, onReview }: DocumentReviewDialogProps) {
  const [status, setStatus] = useState('');
  const [notes, setNotes] = useState('');
  const [rejectionReason, setRejectionReason] = useState('');
  const [isSubmitting, setIsSubmitting] = useState(false);

  useEffect(() => {
    if (document) {
      setStatus('');
      setNotes('');
      setRejectionReason('');
    }
  }, [document]);

  const handleSubmit = async () => {
    if (!document || !status) return;

    setIsSubmitting(true);
    try {
      await onReview(document.documentId, status, notes, status === 'Rejected' ? rejectionReason : undefined);
      onClose();
    } catch (error) {
      console.error('Error reviewing document:', error);
    } finally {
      setIsSubmitting(false);
    }
  };

  if (!document) return null;

  return (
    <Dialog open={isOpen} onOpenChange={onClose}>
      <DialogContent className="max-w-2xl">
        <DialogHeader>
          <DialogTitle>Review Document</DialogTitle>
          <DialogDescription>
            Review and approve or reject the submitted document
          </DialogDescription>
        </DialogHeader>

        <div className="space-y-4">
          <div className="grid grid-cols-2 gap-4">
            <div>
              <label className="text-sm font-medium">Client</label>
              <p className="text-sm text-gray-600">{document.businessName}</p>
            </div>
            <div>
              <label className="text-sm font-medium">Document</label>
              <p className="text-sm text-gray-600">{document.originalFileName}</p>
            </div>
          </div>

          <div>
            <label className="text-sm font-medium">Description</label>
            <p className="text-sm text-gray-600">{document.description}</p>
          </div>

          <div className="grid grid-cols-2 gap-4">
            <div>
              <label className="text-sm font-medium">Uploaded</label>
              <p className="text-sm text-gray-600">
                {new Date(document.uploadedAt).toLocaleDateString()}
              </p>
            </div>
            <div>
              <label className="text-sm font-medium">File Size</label>
              <p className="text-sm text-gray-600">
                {(document.fileSize / 1024 / 1024).toFixed(2)} MB
              </p>
            </div>
          </div>

          <div className="space-y-4">
            <div>
              <label className="text-sm font-medium">Review Decision *</label>
              <Select value={status} onValueChange={setStatus}>
                <SelectTrigger>
                  <SelectValue placeholder="Select decision" />
                </SelectTrigger>
                <SelectContent>
                  <SelectItem value="Verified">Approve Document</SelectItem>
                  <SelectItem value="Rejected">Reject Document</SelectItem>
                  <SelectItem value="UnderReview">Mark as Under Review</SelectItem>
                </SelectContent>
              </Select>
            </div>

            <div>
              <label className="text-sm font-medium">Review Notes</label>
              <Textarea
                placeholder="Add notes about your review decision..."
                value={notes}
                onChange={(e) => setNotes(e.target.value)}
                rows={3}
              />
            </div>

            {status === 'Rejected' && (
              <div>
                <label className="text-sm font-medium">Rejection Reason *</label>
                <Textarea
                  placeholder="Explain why the document was rejected..."
                  value={rejectionReason}
                  onChange={(e) => setRejectionReason(e.target.value)}
                  rows={2}
                />
              </div>
            )}
          </div>
        </div>

        <DialogFooter>
          <Button variant="outline" onClick={onClose}>
            Cancel
          </Button>
          <Button
            onClick={handleSubmit}
            disabled={!status || isSubmitting || (status === 'Rejected' && !rejectionReason)}
          >
            {isSubmitting ? 'Submitting...' : 'Submit Review'}
          </Button>
        </DialogFooter>
      </DialogContent>
    </Dialog>
  );
}

export default function AssociateDocumentReviewPage() {
  const [documents, setDocuments] = useState<DocumentReviewItem[]>([]);
  const [loading, setLoading] = useState(true);
  const [selectedDocument, setSelectedDocument] = useState<DocumentReviewItem | null>(null);
  const [reviewDialogOpen, setReviewDialogOpen] = useState(false);
  const [statusFilter, setStatusFilter] = useState<string>('Submitted');
  const [searchTerm, setSearchTerm] = useState('');
  const { toast } = useToast();

  const fetchDocuments = async () => {
    try {
      setLoading(true);
      const response = await apiClient.get<DocumentReviewItem[]>(`/api/document-verification/pending?status=${statusFilter}`);

      setDocuments(response.data);
    } catch (error) {
      console.error('Error fetching documents:', error);
      toast({
        title: "Error",
        description: "Failed to load documents for review.",
        variant: "destructive",
      });
    } finally {
      setLoading(false);
    }
  };

  useEffect(() => {
    fetchDocuments();
  }, [statusFilter]);

  const handleReviewDocument = async (documentId: number, status: string, notes: string, rejectionReason?: string) => {
    try {
      const reviewData = {
        documentId,
        status,
        reviewNotes: notes,
        rejectionReason
      };

      await apiClient.post('/api/document-verification/review', reviewData);

      toast({
        title: "Success",
        description: `Document ${status.toLowerCase()} successfully.`,
      });
      fetchDocuments(); // Refresh the list
    } catch (error) {
      console.error('Error reviewing document:', error);
      toast({
        title: "Error",
        description: "Failed to review document. Please try again.",
        variant: "destructive",
      });
    }
  };

  const handleDownload = async (doc: DocumentReviewItem) => {
    try {
      // Use fetch directly for blob downloads
      const token = localStorage.getItem('auth_token');
      const response = await fetch(`${process.env.NEXT_PUBLIC_API_URL || 'http://localhost:5001'}/api/documents/${doc.documentId}/download`, {
        headers: {
          'Authorization': `Bearer ${token}`
        }
      });

      if (!response.ok) {
        throw new Error('Download failed');
      }

      const blob = await response.blob();
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

  const getStatusIcon = (status: string) => {
    switch (status) {
      case 'Submitted': return <Clock className="h-4 w-4 text-blue-500" />;
      case 'UnderReview': return <Eye className="h-4 w-4 text-yellow-500" />;
      case 'Verified': return <CheckCircle className="h-4 w-4 text-green-500" />;
      case 'Rejected': return <XCircle className="h-4 w-4 text-red-500" />;
      default: return <FileText className="h-4 w-4" />;
    }
  };

  const getStatusBadge = (status: string) => {
    const statusConfig = {
      'Submitted': { variant: 'default' as const, label: 'Pending Review' },
      'UnderReview': { variant: 'secondary' as const, label: 'Under Review' },
      'Verified': { variant: 'default' as const, label: 'Verified', className: 'bg-green-600' },
      'Rejected': { variant: 'destructive' as const, label: 'Rejected' }
    };

    const config = statusConfig[status as keyof typeof statusConfig];
    return config ? (
      <Badge variant={config.variant} className={'className' in config ? config.className : undefined}>
        {config.label}
      </Badge>
    ) : null;
  };

  const filteredDocuments = documents.filter(doc =>
    doc.originalFileName.toLowerCase().includes(searchTerm.toLowerCase()) ||
    doc.businessName.toLowerCase().includes(searchTerm.toLowerCase()) ||
    doc.clientName.toLowerCase().includes(searchTerm.toLowerCase())
  );

  return (
    <div className="p-6 space-y-6">
      {/* Header */}
      <div className="flex items-center justify-between">
        <div>
          <h1 className="text-3xl font-bold text-gray-900">Document Review</h1>
          <p className="text-gray-600">Review and approve client documents</p>
        </div>
      </div>

      {/* Filters */}
      <Card>
        <CardContent className="p-4">
          <div className="flex items-center space-x-4">
            <div className="flex-1 relative">
              <Search className="absolute left-3 top-1/2 transform -translate-y-1/2 text-gray-400 h-4 w-4" />
              <Input
                placeholder="Search documents or clients..."
                value={searchTerm}
                onChange={(e) => setSearchTerm(e.target.value)}
                className="pl-10"
              />
            </div>
            <Select value={statusFilter} onValueChange={setStatusFilter}>
              <SelectTrigger className="w-48">
                <SelectValue />
              </SelectTrigger>
              <SelectContent>
                <SelectItem value="Submitted">Pending Review</SelectItem>
                <SelectItem value="UnderReview">Under Review</SelectItem>
                <SelectItem value="Verified">Verified</SelectItem>
                <SelectItem value="Rejected">Rejected</SelectItem>
              </SelectContent>
            </Select>
          </div>
        </CardContent>
      </Card>

      {/* Documents List */}
      <Card>
        <CardHeader>
          <CardTitle className="flex items-center space-x-2">
            <FileText className="h-5 w-5" />
            <span>Documents for Review</span>
            <Badge variant="secondary">
              {filteredDocuments.length} documents
            </Badge>
          </CardTitle>
          <CardDescription>
            Review documents submitted by clients
          </CardDescription>
        </CardHeader>
        <CardContent>
          {loading ? (
            <div className="space-y-4">
              {[...Array(5)].map((_, i) => (
                <div key={i} className="flex items-center space-x-4 p-4 border rounded-lg">
                  <div className="h-10 w-10 bg-gray-200 rounded animate-pulse" />
                  <div className="flex-1 space-y-2">
                    <div className="h-4 bg-gray-200 rounded animate-pulse w-3/4" />
                    <div className="h-3 bg-gray-200 rounded animate-pulse w-1/2" />
                  </div>
                  <div className="h-8 bg-gray-200 rounded animate-pulse w-20" />
                </div>
              ))}
            </div>
          ) : filteredDocuments.length === 0 ? (
            <div className="text-center py-12">
              <FileText className="h-12 w-12 text-gray-400 mx-auto mb-4" />
              <h3 className="text-lg font-medium text-gray-900 mb-2">No documents found</h3>
              <p className="text-gray-600">
                {searchTerm ? 'No documents match your search criteria.' : 'No documents require review at this time.'}
              </p>
            </div>
          ) : (
            <div className="space-y-4">
              {filteredDocuments.map((document) => (
                <div key={document.documentId} className="flex items-center space-x-4 p-4 border rounded-lg hover:bg-gray-50 transition-colors">
                  <div className="flex-shrink-0">
                    {getStatusIcon(document.verificationStatus)}
                  </div>
                  <div className="flex-1 min-w-0">
                    <div className="flex items-center space-x-2 mb-1">
                      <h3 className="font-medium text-gray-900 truncate">
                        {document.originalFileName}
                      </h3>
                      {getStatusBadge(document.verificationStatus)}
                      <Badge variant="outline" className="text-xs">
                        {document.documentType}
                      </Badge>
                    </div>
                    <div className="flex items-center space-x-2 text-sm text-gray-600 mb-1">
                      <Building2 className="h-3 w-3" />
                      <span>{document.businessName}</span>
                      <User className="h-3 w-3 ml-2" />
                      <span>{document.clientName}</span>
                    </div>
                    <p className="text-sm text-gray-600 truncate mb-1">
                      {document.description}
                    </p>
                    <div className="flex items-center space-x-4 text-xs text-gray-500">
                      <span className="flex items-center">
                        <Calendar className="h-3 w-3 mr-1" />
                        {new Date(document.uploadedAt).toLocaleDateString()}
                      </span>
                      <span>{(document.fileSize / 1024 / 1024).toFixed(2)} MB</span>
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
                    <Button
                      size="sm"
                      onClick={() => {
                        setSelectedDocument(document);
                        setReviewDialogOpen(true);
                      }}
                    >
                      <Eye className="h-4 w-4 mr-1" />
                      Review
                    </Button>
                  </div>
                </div>
              ))}
            </div>
          )}
        </CardContent>
      </Card>

      <DocumentReviewDialog
        document={selectedDocument}
        isOpen={reviewDialogOpen}
        onClose={() => {
          setReviewDialogOpen(false);
          setSelectedDocument(null);
        }}
        onReview={handleReviewDocument}
      />
    </div>
  );
}