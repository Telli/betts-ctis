"use client";

import { useState, useEffect } from "react";
import { Button } from "@/components/ui/button";
import { Card, CardContent, CardHeader, CardTitle } from "@/components/ui/card";
import { useToast } from "@/hooks/use-toast";
import { DocumentService, DocumentDto } from "@/lib/services";
import { FileText, Download, Trash2 } from "lucide-react";
import { formatFileSize, formatDate } from "@/lib/utils";

interface DocumentListProps {
  clientId: number;
  refreshTrigger?: number; // Used to trigger refresh from parent
}

export function DocumentList({ clientId, refreshTrigger = 0 }: DocumentListProps) {
  const [documents, setDocuments] = useState<DocumentDto[]>([]);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);
  const { toast } = useToast();

  useEffect(() => {
    const fetchDocuments = async () => {
      try {
        setLoading(true);
        setError(null);
        const data = await DocumentService.getClientDocuments(clientId);
        setDocuments(data);
      } catch (err: any) {
        console.error("Failed to fetch documents:", err);
        setError(err.message || "Failed to load documents");
        toast({
          title: "Error",
          description: "Failed to load documents. Please try again.",
          variant: "destructive",
        });
      } finally {
        setLoading(false);
      }
    };

    fetchDocuments();
  }, [clientId, refreshTrigger, toast]);

  const handleDelete = async (documentId: number | undefined) => {
    if (!documentId) return;

    if (!confirm("Are you sure you want to delete this document?")) {
      return;
    }

    try {
      await DocumentService.delete(documentId.toString());
      setDocuments(documents.filter(doc => doc.documentId !== documentId));
      toast({
        title: "Success",
        description: "Document deleted successfully",
      });
    } catch (err) {
      console.error("Failed to delete document:", err);
      toast({
        title: "Error",
        description: "Failed to delete document. Please try again.",
        variant: "destructive",
      });
    }
  };

  return (
    <Card className="w-full shadow-md mt-6">
      <CardHeader>
        <CardTitle>Client Documents</CardTitle>
      </CardHeader>
      <CardContent>
        {loading ? (
          <div className="text-center py-4">Loading documents...</div>
        ) : error ? (
          <div className="text-center text-red-500 py-4">{error}</div>
        ) : documents.length === 0 ? (
          <div className="text-center text-gray-500 py-4">No documents found for this client.</div>
        ) : (
          <div className="divide-y">
            {documents.map((doc) => (
              <div key={doc.documentId} className="py-3 flex items-center justify-between">
                <div className="flex items-center space-x-3">
                  <div className="bg-blue-100 p-2 rounded">
                    <FileText className="h-5 w-5 text-blue-600" />
                  </div>
                  <div>
                    <h4 className="font-medium">{doc.filename}</h4>
                    <div className="text-sm text-gray-500">
                      {formatFileSize(doc.fileSize)} • {formatDate(doc.uploadDate)}
                    </div>
                  </div>
                </div>
                <div className="flex space-x-2">
                  <Button variant="ghost" size="sm">
                    <Download className="h-4 w-4 mr-1" />
                    Download
                  </Button>
                  <Button 
                    variant="ghost" 
                    size="sm" 
                    className="text-red-500 hover:text-red-700 hover:bg-red-50"
                    onClick={() => handleDelete(doc.documentId)}
                  >
                    <Trash2 className="h-4 w-4 mr-1" />
                    Delete
                  </Button>
                </div>
              </div>
            ))}
          </div>
        )}
      </CardContent>
    </Card>
  );
}
