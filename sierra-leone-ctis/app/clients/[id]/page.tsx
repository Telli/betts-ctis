"use client";

import { useState, useEffect } from "react";
import { useParams } from "next/navigation";
import { DashboardHeader } from "@/components/dashboard-header";
import { DocumentUpload } from "@/components/document-upload";
import { DocumentList } from "@/components/document-list";
import { Button } from "@/components/ui/button";
import { Card, CardContent, CardHeader, CardTitle } from "@/components/ui/card";
import { Badge } from "@/components/ui/badge";
import { ClientService, ClientDto } from "@/lib/services";
import { useToast } from "@/hooks/use-toast";
import Link from "next/link";
import { Edit, ArrowLeft } from "lucide-react";

export default function ClientDetailPage() {
  const params = useParams();
  const clientId = Number(params.id);
  
  const [client, setClient] = useState<ClientDto | null>(null);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);
  const [refreshDocuments, setRefreshDocuments] = useState(0);
  const { toast } = useToast();

  useEffect(() => {
    const fetchClient = async () => {
      try {
        setLoading(true);
        const data = await ClientService.getById(clientId);
        setClient(data);
        setError(null);
      } catch (err: any) {
        console.error("Failed to fetch client details:", err);
        setError(err.message || "Failed to load client details");
        toast({
          title: "Error",
          description: "Failed to load client details. Please try again.",
          variant: "destructive",
        });
      } finally {
        setLoading(false);
      }
    };

    if (clientId) {
      fetchClient();
    }
  }, [clientId, toast]);

  const getStatusBadge = (status: string | number | undefined) => {
    if (status === undefined || status === null) return null;
    
    // Map numeric status values to string labels
    const statusMap: Record<number, string> = {
      0: "active",
      1: "inactive",
      2: "suspended"
    };

    const statusText = typeof status === 'number' 
      ? (statusMap[status] || 'unknown')
      : status.toLowerCase();
    
    const variants: Record<string, string> = {
      active: "bg-green-100 text-green-800",
      compliant: "bg-green-100 text-green-800",
      pending: "bg-amber-100 text-amber-800",
      warning: "bg-orange-100 text-orange-800",
      inactive: "bg-gray-100 text-gray-800",
      suspended: "bg-red-100 text-red-800",
      overdue: "bg-red-100 text-red-800",
    };

    const style = variants[statusText] || "bg-gray-100 text-gray-800";
    const label = statusText.charAt(0).toUpperCase() + statusText.slice(1);

    return <Badge className={style}>{label}</Badge>;
  };

  const handleDocumentUploaded = () => {
    // Increment refreshDocuments to trigger a reload of the document list
    setRefreshDocuments(prev => prev + 1);
  };

  if (loading) {
    return (
      <div className="min-h-screen bg-gradient-to-br from-slate-50 to-blue-50">
        <DashboardHeader />
        <main className="container mx-auto px-4 py-6">
          <div className="text-center py-8">Loading client details...</div>
        </main>
      </div>
    );
  }

  if (error || !client) {
    return (
      <div className="min-h-screen bg-gradient-to-br from-slate-50 to-blue-50">
        <DashboardHeader />
        <main className="container mx-auto px-4 py-6">
          <div className="text-center py-8 text-red-500">
            {error || "Client not found"}
            <div className="mt-4">
              <Link href="/clients">
                <Button>Back to Clients</Button>
              </Link>
            </div>
          </div>
        </main>
      </div>
    );
  }

  return (
    <div className="min-h-screen bg-gradient-to-br from-slate-50 to-blue-50">
      <DashboardHeader />
      <main className="container mx-auto px-4 py-6">
        <div className="mb-6 flex items-center justify-between">
          <div className="flex items-center gap-2">
            <Link href="/clients">
              <Button variant="ghost" size="sm">
                <ArrowLeft className="mr-2 h-4 w-4" />
                Back to Clients
              </Button>
            </Link>
            <h1 className="text-2xl font-bold">{client.name}</h1>
            {client.status && getStatusBadge(client.status)}
          </div>
          <Link href={`/clients/${clientId}/edit`}>
            <Button>
              <Edit className="mr-2 h-4 w-4" />
              Edit Client
            </Button>
          </Link>
        </div>

        <div className="grid grid-cols-1 lg:grid-cols-3 gap-6">
          <div className="lg:col-span-2">
            <Card className="w-full shadow-md">
              <CardHeader>
                <CardTitle>Client Details</CardTitle>
              </CardHeader>
              <CardContent className="space-y-4">
                <div className="grid grid-cols-2 gap-4">
                  <div>
                    <h3 className="text-sm font-medium text-gray-500">Client Type</h3>
                    <p>{client.type}</p>
                  </div>
                  <div>
                    <h3 className="text-sm font-medium text-gray-500">Category</h3>
                    <p>{client.category}</p>
                  </div>
                </div>
                
                <div className="grid grid-cols-2 gap-4">
                  <div>
                    <h3 className="text-sm font-medium text-gray-500">TIN</h3>
                    <p>{client.tin}</p>
                  </div>
                  <div>
                    <h3 className="text-sm font-medium text-gray-500">Primary Contact</h3>
                    <p>{client.contact}</p>
                  </div>
                </div>

                {client.taxLiability && (
                  <div>
                    <h3 className="text-sm font-medium text-gray-500">Current Tax Liability</h3>
                    <p className="text-lg font-semibold">{client.taxLiability}</p>
                  </div>
                )}

                {client.complianceScore !== undefined && (
                  <div>
                    <h3 className="text-sm font-medium text-gray-500">Compliance Score</h3>
                    <div className="flex items-center space-x-2">
                      <span className={`font-semibold ${
                        client.complianceScore >= 90 ? "text-green-600" : 
                        client.complianceScore >= 70 ? "text-amber-600" : 
                        "text-red-600"
                      }`}>
                        {client.complianceScore}%
                      </span>
                      <div className="w-24 h-2 bg-gray-200 rounded-full">
                        <div
                          className={`h-2 rounded-full ${
                            client.complianceScore >= 90 ? "bg-green-500" : 
                            client.complianceScore >= 70 ? "bg-amber-500" : 
                            "bg-red-500"
                          }`}
                          style={{ width: `${client.complianceScore}%` }}
                        />
                      </div>
                    </div>
                  </div>
                )}
              </CardContent>
            </Card>

            <DocumentList clientId={clientId} refreshTrigger={refreshDocuments} />
          </div>
          
          <div>
            <DocumentUpload clientId={clientId} onUploadComplete={handleDocumentUploaded} />
          </div>
        </div>
      </main>
    </div>
  );
}
