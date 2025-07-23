"use client"

import { useState, useEffect } from "react"
import { Badge } from "@/components/ui/badge"
import { Button } from "@/components/ui/button"
import { Avatar, AvatarFallback } from "@/components/ui/avatar"
import { Building2, User, Eye, Edit, Trash2, Plus } from "lucide-react"
import { ClientService, ClientDto } from "@/lib/services"
import Link from "next/link"
import { useToast } from "@/hooks/use-toast"
import AdvancedDataTable, { Column, FilterOption } from "@/components/ui/advanced-data-table"
import { formatSierraLeones } from "@/lib/utils/currency"

export function ClientsTable() {
  const [clients, setClients] = useState<ClientDto[]>([])
  const [loading, setLoading] = useState<boolean>(true)
  const [error, setError] = useState<string | null>(null)
  const { toast } = useToast()

  const fetchClients = async () => {
    try {
      setLoading(true)
      setError(null)
      const data = await ClientService.getAll()
      setClients(data)
    } catch (err: any) {
      console.error("Failed to fetch clients:", err)
      setError(err.message || "Failed to load clients")
      toast({
        title: "Error",
        description: "Failed to load clients. Please try again.",
        variant: "destructive",
      })
    } finally {
      setLoading(false)
    }
  }

  useEffect(() => {
    fetchClients()
  }, [])


  const getStatusBadge = (status: string) => {
    const variants = {
      compliant: "bg-green-100 text-green-800",
      pending: "bg-amber-100 text-amber-800",
      warning: "bg-orange-100 text-orange-800",
      overdue: "bg-red-100 text-red-800",
    }

    return (
      <Badge className={variants[status as keyof typeof variants] || variants.pending}>
        {status.charAt(0).toUpperCase() + status.slice(1)}
      </Badge>
    )
  }

  const getCategoryBadge = (category: string) => {
    const variants = {
      "Large Taxpayer": "bg-purple-100 text-purple-800",
      "Medium Taxpayer": "bg-blue-100 text-blue-800",
      "Small Taxpayer": "bg-green-100 text-green-800",
      "Micro Taxpayer": "bg-gray-100 text-gray-800",
    }

    return (
      <Badge variant="outline" className={variants[category as keyof typeof variants] || variants["Micro Taxpayer"]}>
        {category}
      </Badge>
    )
  }

  const getComplianceColor = (score: number) => {
    if (score >= 90) return "text-green-600"
    if (score >= 70) return "text-amber-600"
    return "text-red-600"
  }

  const handleDeleteClient = async (clientId?: number) => {
    if (!clientId) return;
    
    if (!confirm('Are you sure you want to delete this client?')) {
      return;
    }
    
    try {
      await ClientService.delete(clientId);
      setClients(clients.filter(client => client.clientId !== clientId));
      toast({
        title: "Success",
        description: "Client deleted successfully",
      });
    } catch (err) {
      console.error("Failed to delete client:", err);
      toast({
        title: "Error",
        description: "Failed to delete client. Please try again.",
        variant: "destructive",
      });
    }
  };

  // Define columns for the advanced data table
  const columns: Column<ClientDto>[] = [
    {
      key: 'name',
      label: 'Client',
      sortable: true,
      render: (_, client) => (
        <div className="flex items-center space-x-3">
          <Avatar className="h-10 w-10">
            <AvatarFallback className="bg-sierra-blue-100 text-sierra-blue-600">
              {client.type === "Corporation" ? (
                <Building2 className="h-5 w-5" />
              ) : (
                <User className="h-5 w-5" />
              )}
            </AvatarFallback>
          </Avatar>
          <div>
            <h3 className="font-semibold text-gray-900">{client.name}</h3>
            <p className="text-sm text-gray-600">{client.tin}</p>
            <p className="text-xs text-gray-500">Contact: {client.contact}</p>
          </div>
        </div>
      )
    },
    {
      key: 'category',
      label: 'Category',
      sortable: true,
      filterable: true,
      render: (category) => getCategoryBadge(category)
    },
    {
      key: 'status',
      label: 'Status',
      sortable: true,
      filterable: true,
      render: (status) => getStatusBadge(status)
    },
    {
      key: 'taxLiability',
      label: 'Tax Liability',
      sortable: true,
      render: (amount) => (
        <span className="font-semibold text-gray-900">
          {typeof amount === 'number' ? formatSierraLeones(amount) : amount}
        </span>
      )
    },
    {
      key: 'complianceScore',
      label: 'Compliance',
      sortable: true,
      render: (score, client) => (
        <div className="flex items-center space-x-2">
          <span className={`font-semibold ${getComplianceColor(score || 0)}`}>
            {score || 0}%
          </span>
          <div className="w-16 h-2 bg-gray-200 rounded-full">
            <div
              className={`h-2 rounded-full ${
                (score || 0) >= 90
                  ? "bg-green-500"
                  : (score || 0) >= 70
                    ? "bg-amber-500"
                    : "bg-red-500"
              }`}
              style={{ width: `${score || 0}%` }}
            />
          </div>
        </div>
      )
    },
    {
      key: 'lastFiling',
      label: 'Last Filing',
      sortable: true,
      render: (filing) => (
        <span className="text-sm text-gray-600">{filing}</span>
      )
    }
  ]

  // Define filters
  const filters: Record<string, FilterOption[]> = {
    category: [
      { value: 'Large Taxpayer', label: 'Large Taxpayer', count: clients.filter(c => c.category === 'Large Taxpayer').length },
      { value: 'Medium Taxpayer', label: 'Medium Taxpayer', count: clients.filter(c => c.category === 'Medium Taxpayer').length },
      { value: 'Small Taxpayer', label: 'Small Taxpayer', count: clients.filter(c => c.category === 'Small Taxpayer').length },
      { value: 'Micro Taxpayer', label: 'Micro Taxpayer', count: clients.filter(c => c.category === 'Micro Taxpayer').length }
    ],
    status: [
      { value: 'compliant', label: 'Compliant', count: clients.filter(c => c.status === 'compliant').length },
      { value: 'pending', label: 'Pending', count: clients.filter(c => c.status === 'pending').length },
      { value: 'warning', label: 'Warning', count: clients.filter(c => c.status === 'warning').length },
      { value: 'overdue', label: 'Overdue', count: clients.filter(c => c.status === 'overdue').length }
    ]
  }

  return (
    <AdvancedDataTable
      data={clients}
      columns={columns}
      loading={loading}
      error={error}
      title="Client Directory"
      description={`Manage ${clients.length} client accounts and tax compliance status`}
      searchPlaceholder="Search clients by name, TIN, or contact..."
      filters={filters}
      defaultSort={{ column: 'name', direction: 'asc' }}
      pageSize={15}
      onRefresh={fetchClients}
      onExport={() => {
        // TODO: Implement export functionality
        toast({
          title: "Export feature coming soon",
          description: "Client data export will be available in the next update.",
        })
      }}
      actions={
        <Link href="/clients/new">
          <Button className="bg-sierra-blue-600 hover:bg-sierra-blue-700">
            <Plus className="h-4 w-4 mr-2" />
            Add Client
          </Button>
        </Link>
      }
      rowActions={(client) => (
        <div className="flex items-center space-x-1">
          <Link href={`/clients/${client.clientId || 0}`}>
            <Button size="sm" variant="outline" title="View Client">
              <Eye className="h-4 w-4" />
            </Button>
          </Link>
          <Link href={`/clients/${client.clientId || 0}/edit`}>
            <Button size="sm" variant="outline" title="Edit Client">
              <Edit className="h-4 w-4" />
            </Button>
          </Link>
          <Button 
            size="sm" 
            variant="outline" 
            onClick={() => handleDeleteClient(client.clientId)}
            title="Delete Client"
          >
            <Trash2 className="h-4 w-4 text-red-500" />
          </Button>
        </div>
      )}
      emptyStateMessage="No clients found"
      emptyStateAction={
        <Link href="/clients/new">
          <Button className="bg-sierra-blue-600 hover:bg-sierra-blue-700">
            <Plus className="h-4 w-4 mr-2" />
            Add Your First Client
          </Button>
        </Link>
      }
      className="backdrop-blur-sm bg-white/90 border-0 shadow-lg"
    />
  )
}
