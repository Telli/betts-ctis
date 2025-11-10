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
      if (process.env.NODE_ENV !== 'production') {
        try {
          console.debug('Clients fetched:', { count: Array.isArray(data) ? data.length : 0, sample: (Array.isArray(data) ? data.slice(0, 3) : []) })
        } catch {}
      }
      const normalized = (Array.isArray(data) ? data : []).map(c => {
        const C = c as any
        const fn = c.firstName ?? C.FirstName ?? ''
        const ln = c.lastName ?? C.LastName ?? ''
        const nm = c.name ?? C.Name ?? ''
        const bn = c.businessName ?? C.BusinessName ?? C.CompanyName ?? ''
        const displayName = bn || nm || [fn, ln].filter(Boolean).join(' ')
        const contact = (c.contactPerson ?? C.ContactPerson ?? c.contact ?? C.Contact ?? [fn, ln].filter(Boolean).join(' '))

        return {
          // include all original fields first
          ...c,
          // then override with normalized/camelCase fields
          clientId: c.clientId ?? C.ClientId,
          clientNumber: c.clientNumber ?? C.ClientNumber,
          businessName: displayName,
          name: nm,
          firstName: fn,
          lastName: ln,
          contactPerson: contact,
          email: c.email ?? C.Email,
          phoneNumber: c.phoneNumber ?? C.PhoneNumber,
          address: c.address ?? C.Address,
          tin: c.tin ?? C.TIN ?? C.Tin,
          clientType: c.clientType ?? C.ClientType ?? C.type ?? '',
          taxpayerCategory: c.taxpayerCategory ?? C.TaxpayerCategory ?? C.category ?? '',
          annualTurnover: c.annualTurnover ?? C.AnnualTurnover,
          status: c.status ?? C.Status ?? C.kpiStatus ?? c.status,
          complianceScore: c.complianceScore ?? C.ComplianceScore,
        } as ClientDto
      })
      setClients(normalized)
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


  const getStatusBadge = (status: string | number) => {
    // Map numeric status values to string labels (matches backend ClientStatus enum)
    const statusMap: Record<number, string> = {
      0: "Active",
      1: "Inactive",
      2: "Suspended"
    }

    // Normalize status which may arrive as number, numeric-string, or text
    let statusText: string
    if (typeof status === 'number') {
      statusText = (statusMap[status] || 'Unknown').toLowerCase()
    } else if (typeof status === 'string') {
      const trimmed = status.trim()
      const numeric = /^\d+$/.test(trimmed) ? Number(trimmed) : null
      if (numeric !== null) {
        statusText = (statusMap[numeric] || 'Unknown').toLowerCase()
      } else {
        statusText = trimmed.toLowerCase()
      }
    } else {
      statusText = 'unknown'
    }

    const variants = {
      active: "bg-green-100 text-green-800",
      inactive: "bg-gray-100 text-gray-800",
      suspended: "bg-red-100 text-red-800",
      unknown: "bg-gray-100 text-gray-600",
    }

    return (
      <Badge className={variants[statusText as keyof typeof variants] || variants.unknown}>
        {statusText.charAt(0).toUpperCase() + statusText.slice(1)}
      </Badge>
    )
  }

  const getCategoryBadge = (category: string) => {
    const variants = {
      "Large Taxpayer": "bg-purple-100 text-purple-800",
      "Medium Taxpayer": "bg-blue-100 text-blue-800",
      "Small Taxpayer": "bg-green-100 text-green-800",
      "Micro Taxpayer": "bg-gray-100 text-gray-800",
      "Unknown": "bg-gray-100 text-gray-600",
    }

    return (
      <Badge variant="outline" className={variants[category as keyof typeof variants] || variants["Unknown"]}>
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

  // Map backend enum values to display strings
  const getClientTypeName = (type: string | number) => {
    const typeMap: Record<number, string> = { 0: "Individual", 1: "Partnership", 2: "Corporation", 3: "NGO" };
    if (type === null || type === undefined || type === '') return 'Unknown';
    if (typeof type === 'number') return typeMap[type] || 'Unknown'
    const trimmed = String(type).trim()
    if (/^\d+$/.test(trimmed)) {
      const n = Number(trimmed)
      return typeMap[n] || 'Unknown'
    }
    return trimmed || 'Unknown'
  };

  const getTaxpayerCategoryName = (category: string | number) => {
    const categoryMap: Record<number, string> = { 0: "Large", 1: "Medium", 2: "Small", 3: "Micro" };
    if (category === null || category === undefined || category === '') return 'Unknown';
    if (typeof category === 'number') {
      const base = categoryMap[category]
      return base ? `${base} Taxpayer` : 'Unknown'
    }
    const raw = String(category).trim()
    if (/^\d+$/.test(raw)) {
      const n = Number(raw)
      const base = categoryMap[n]
      return base ? `${base} Taxpayer` : 'Unknown'
    }
    const lower = raw.toLowerCase()
    if (lower.includes('large')) return 'Large Taxpayer'
    if (lower.includes('medium')) return 'Medium Taxpayer'
    if (lower.includes('small')) return 'Small Taxpayer'
    if (lower.includes('micro')) return 'Micro Taxpayer'
    return 'Unknown'
  };

  const getStatusCode = (status: string | number | undefined): number => {
    if (status === null || status === undefined || status === '') return -1
    if (typeof status === 'number') return status
    const trimmed = String(status).trim()
    if (/^\d+$/.test(trimmed)) return Number(trimmed)
    const lower = trimmed.toLowerCase()
    if (lower.startsWith('active')) return 0
    if (lower.startsWith('inactive')) return 1
    if (lower.startsWith('suspended')) return 2
    return -1
  }

  const getCategoryCode = (category: string | number | undefined): number => {
    if (category === null || category === undefined || category === '') return -1
    if (typeof category === 'number') return category
    const trimmed = String(category).trim()
    if (/^\d+$/.test(trimmed)) return Number(trimmed)
    const lower = trimmed.toLowerCase()
    if (lower.includes('large')) return 0
    if (lower.includes('medium')) return 1
    if (lower.includes('small')) return 2
    if (lower.includes('micro')) return 3
    return -1
  }

  // Define columns for the advanced data table
  const columns: Column<ClientDto>[] = [
    {
      key: 'businessName',
      label: 'Client',
      sortable: true,
      render: (_, client) => (
        <div className="flex items-center space-x-3">
          <Avatar className="h-10 w-10">
            <AvatarFallback className="bg-sierra-blue-100 text-sierra-blue-600">
              {getClientTypeName(client.clientType) === "Corporation" ? (
                <Building2 className="h-5 w-5" />
              ) : (
                <User className="h-5 w-5" />
              )}
            </AvatarFallback>
          </Avatar>
          <div>
            <h3 className="font-semibold text-gray-900">{client.businessName || client.name || [client.firstName, client.lastName].filter(Boolean).join(' ') || 'Unnamed Client'}</h3>
            <p className="text-sm text-gray-600">{client.tin || 'N/A'}</p>
            <p className="text-xs text-gray-500">
              Contact: {(client.contactPerson || [client.firstName, client.lastName].filter(Boolean).join(' ') || 'Not available')}
            </p>
          </div>
        </div>
      )
    },
    {
      key: 'taxpayerCategory',
      label: 'Category',
      sortable: true,
      filterable: true,
      render: (category) => getCategoryBadge(getTaxpayerCategoryName(category))
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
          <span className={`font-semibold ${score == null ? 'text-muted-foreground' : getComplianceColor(score)}`}>
            {score == null ? '—' : `${score}%`}
          </span>
          <div className="w-16 h-2 bg-gray-200 rounded-full">
            <div
              className={`h-2 rounded-full ${
                (typeof score === 'number' && score >= 90)
                  ? "bg-green-500"
                  : (typeof score === 'number' && score >= 70)
                    ? "bg-amber-500"
                    : "bg-red-500"
              }`}
              style={{ width: `${typeof score === 'number' ? score : 0}%` }}
            />
          </div>
        </div>
      )
    },
    {
      key: 'clientNumber',
      label: 'Client #',
      sortable: true,
      render: (clientNumber) => (
        <span className="text-sm font-mono text-gray-600">{clientNumber}</span>
      )
    }
  ]

  // Prepare normalized table data so filters work regardless of raw value types
  const tableData = clients.map(c => ({
    ...c,
    statusCode: getStatusCode(c.status),
    taxpayerCategoryCode: getCategoryCode(c.taxpayerCategory),
  }))

  // Define filters (using normalized codes)
  const filters: Record<string, FilterOption[]> = {
    taxpayerCategoryCode: [
      { value: '0', label: 'Large Taxpayer', count: clients.filter(c => getCategoryCode(c.taxpayerCategory) === 0).length },
      { value: '1', label: 'Medium Taxpayer', count: clients.filter(c => getCategoryCode(c.taxpayerCategory) === 1).length },
      { value: '2', label: 'Small Taxpayer', count: clients.filter(c => getCategoryCode(c.taxpayerCategory) === 2).length },
      { value: '3', label: 'Micro Taxpayer', count: clients.filter(c => getCategoryCode(c.taxpayerCategory) === 3).length }
    ],
    statusCode: [
      { value: '0', label: 'Active', count: clients.filter(c => getStatusCode(c.status) === 0).length },
      { value: '1', label: 'Inactive', count: clients.filter(c => getStatusCode(c.status) === 1).length },
      { value: '2', label: 'Suspended', count: clients.filter(c => getStatusCode(c.status) === 2).length }
    ]
  }

  return (
    <AdvancedDataTable
      data={tableData as any}
      columns={columns}
      loading={loading}
      error={error}
      title="Client Directory"
      description={`Manage ${clients.length} client accounts and tax compliance status`}
      searchPlaceholder="Search clients by name, TIN, or contact..."
      filters={filters}
      defaultSort={{ column: 'businessName', direction: 'asc' }}
      pageSize={15}
      onRefresh={fetchClients}
      onExport={() => {
        try {
          const headers = [
            'Client ID',
            'Client Number',
            'Business Name',
            'Contact Person',
            'Email',
            'Phone',
            'TIN',
            'Client Type',
            'Taxpayer Category',
            'Status',
            'Annual Turnover',
            'Compliance Score',
            'Tax Liability',
          ]
          const rows = clients.map(c => [
            c.clientId ?? '',
            c.clientNumber ?? '',
            c.businessName ?? '',
            c.contactPerson ?? '',
            c.email ?? '',
            c.phoneNumber ?? '',
            c.tin ?? '',
            getClientTypeName(c.clientType),
            getTaxpayerCategoryName(c.taxpayerCategory),
            typeof c.status === 'number' ? (['Active','Inactive','Suspended'][c.status] || 'Unknown') : c.status,
            c.annualTurnover ?? '',
            c.complianceScore ?? '',
            c.taxLiability ?? '',
          ])
          const csv = [headers, ...rows]
            .map(r => r.map(v => {
              const s = String(v ?? '')
              if (s.includes(',') || s.includes('"') || s.includes('\n')) {
                return '"' + s.replace(/"/g, '""') + '"'
              }
              return s
            }).join(','))
            .join('\n')

          const blob = new Blob([csv], { type: 'text/csv;charset=utf-8;' })
          const url = URL.createObjectURL(blob)
          const a = document.createElement('a')
          a.href = url
          a.download = `clients-${new Date().toISOString().slice(0,10)}.csv`
          document.body.appendChild(a)
          a.click()
          document.body.removeChild(a)
          URL.revokeObjectURL(url)

          toast({ title: 'Export complete', description: 'Client data exported as CSV.' })
        } catch (e) {
          toast({ variant: 'destructive', title: 'Export failed', description: 'Could not export client data.' })
        }
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
