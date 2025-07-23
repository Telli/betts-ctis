"use client"

import { useEffect, useState } from 'react'
import { useToast } from '@/components/ui/use-toast'
import { Button } from '@/components/ui/button'
import { Input } from '@/components/ui/input'
import { Card, CardContent, CardDescription, CardHeader, CardTitle } from '@/components/ui/card'
import { Badge } from '@/components/ui/badge'
import { Tabs, TabsContent, TabsList, TabsTrigger } from '@/components/ui/tabs'
import { Select, SelectContent, SelectItem, SelectTrigger, SelectValue } from '@/components/ui/select'
import { Table, TableBody, TableCell, TableHead, TableHeader, TableRow } from '@/components/ui/table'
import { Dialog, DialogContent, DialogHeader, DialogTitle, DialogTrigger } from '@/components/ui/dialog'
import { TaxFilingService, TaxFilingDto, TaxType, FilingStatus, CreateTaxFilingDto } from '@/lib/services'
import { Plus, Search, FileText, Calendar, DollarSign, Filter, Eye, Edit, Trash } from 'lucide-react'
import Loading from '@/app/loading'
import TaxFilingForm from '@/components/tax-filing-form'

export default function TaxFilingsPage() {
  const { toast } = useToast()
  const [loading, setLoading] = useState(true)
  const [taxFilings, setTaxFilings] = useState<TaxFilingDto[]>([])
  const [filteredFilings, setFilteredFilings] = useState<TaxFilingDto[]>([])
  const [searchTerm, setSearchTerm] = useState('')
  const [selectedTaxType, setSelectedTaxType] = useState<TaxType | 'ALL'>('ALL')
  const [selectedStatus, setSelectedStatus] = useState<FilingStatus | 'ALL'>('ALL')
  const [showCreateDialog, setShowCreateDialog] = useState(false)
  const [selectedFiling, setSelectedFiling] = useState<TaxFilingDto | null>(null)
  const [currentPage, setCurrentPage] = useState(1)
  const [totalPages, setTotalPages] = useState(1)
  const [totalCount, setTotalCount] = useState(0)

  const pageSize = 20

  // Fetch tax filings
  const fetchTaxFilings = async () => {
    try {
      setLoading(true)
      const response = await TaxFilingService.getTaxFilings(
        currentPage,
        pageSize,
        searchTerm || undefined,
        selectedTaxType !== 'ALL' ? selectedTaxType : undefined,
        selectedStatus !== 'ALL' ? selectedStatus : undefined
      )
      
      if (response.success) {
        setTaxFilings(response.data)
        setFilteredFilings(response.data)
        setTotalPages(response.pagination.totalPages)
        setTotalCount(response.pagination.totalCount)
      }
    } catch (error) {
      console.error('Error fetching tax filings:', error)
      toast({
        variant: 'destructive',
        title: 'Error',
        description: 'Failed to load tax filings',
      })
    } finally {
      setLoading(false)
    }
  }

  useEffect(() => {
    fetchTaxFilings()
  }, [currentPage, selectedTaxType, selectedStatus])

  // Handle search
  const handleSearch = () => {
    setCurrentPage(1)
    fetchTaxFilings()
  }

  // Clear filters
  const clearFilters = () => {
    setSearchTerm('')
    setSelectedTaxType('ALL')
    setSelectedStatus('ALL')
    setCurrentPage(1)
    fetchTaxFilings()
  }

  // Get status badge variant
  const getStatusBadgeVariant = (status: FilingStatus) => {
    switch (status) {
      case FilingStatus.Draft:
        return 'secondary'
      case FilingStatus.Submitted:
        return 'default'
      case FilingStatus.UnderReview:
        return 'outline'
      case FilingStatus.Approved:
        return 'default'
      case FilingStatus.Rejected:
        return 'destructive'
      case FilingStatus.Filed:
        return 'default'
      default:
        return 'secondary'
    }
  }

  // Get tax type color
  const getTaxTypeColor = (taxType: TaxType) => {
    switch (taxType) {
      case TaxType.IncomeTax:
        return 'bg-sierra-blue text-white'
      case TaxType.GST:
        return 'bg-sierra-gold text-white'
      case TaxType.PayrollTax:
        return 'bg-sierra-green text-white'
      case TaxType.ExciseDuty:
        return 'bg-gray-600 text-white'
      default:
        return 'bg-gray-500 text-white'
    }
  }

  if (loading) {
    return <Loading />
  }

  return (
    <div className="flex-1 space-y-4 p-4 md:p-8 pt-6">
      {/* Header */}
      <div className="flex items-center justify-between">
        <div>
          <h2 className="text-3xl font-bold tracking-tight text-sierra-blue">Tax Filings</h2>
          <p className="text-muted-foreground">
            Manage and track tax filings for Sierra Leone compliance
          </p>
        </div>
        <Dialog open={showCreateDialog} onOpenChange={setShowCreateDialog}>
          <DialogTrigger asChild>
            <Button className="bg-sierra-blue hover:bg-sierra-blue/90">
              <Plus className="mr-2 h-4 w-4" />
              New Tax Filing
            </Button>
          </DialogTrigger>
          <DialogContent className="max-w-2xl">
            <DialogHeader>
              <DialogTitle>Create New Tax Filing</DialogTitle>
            </DialogHeader>
            <TaxFilingForm 
              onSuccess={() => {
                setShowCreateDialog(false)
                fetchTaxFilings()
              }}
            />
          </DialogContent>
        </Dialog>
      </div>

      {/* Filters */}
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
                  placeholder="Search by reference, client name, or client number..."
                  value={searchTerm}
                  onChange={(e) => setSearchTerm(e.target.value)}
                  className="pl-10"
                  onKeyPress={(e) => e.key === 'Enter' && handleSearch()}
                />
              </div>
            </div>
            <div className="w-full sm:w-48">
              <Select value={selectedTaxType} onValueChange={(value) => setSelectedTaxType(value as TaxType | 'ALL')}>
                <SelectTrigger>
                  <SelectValue placeholder="Tax Type" />
                </SelectTrigger>
                <SelectContent>
                  <SelectItem value="ALL">All Tax Types</SelectItem>
                  <SelectItem value={TaxType.IncomeTax}>Income Tax</SelectItem>
                  <SelectItem value={TaxType.GST}>GST</SelectItem>
                  <SelectItem value={TaxType.PayrollTax}>Payroll Tax</SelectItem>
                  <SelectItem value={TaxType.ExciseDuty}>Excise Duty</SelectItem>
                </SelectContent>
              </Select>
            </div>
            <div className="w-full sm:w-48">
              <Select value={selectedStatus} onValueChange={(value) => setSelectedStatus(value as FilingStatus | 'ALL')}>
                <SelectTrigger>
                  <SelectValue placeholder="Status" />
                </SelectTrigger>
                <SelectContent>
                  <SelectItem value="ALL">All Status</SelectItem>
                  <SelectItem value={FilingStatus.Draft}>Draft</SelectItem>
                  <SelectItem value={FilingStatus.Submitted}>Submitted</SelectItem>
                  <SelectItem value={FilingStatus.UnderReview}>Under Review</SelectItem>
                  <SelectItem value={FilingStatus.Approved}>Approved</SelectItem>
                  <SelectItem value={FilingStatus.Rejected}>Rejected</SelectItem>
                  <SelectItem value={FilingStatus.Filed}>Filed</SelectItem>
                </SelectContent>
              </Select>
            </div>
            <Button onClick={handleSearch} className="w-full sm:w-auto">
              Search
            </Button>
            <Button variant="outline" onClick={clearFilters} className="w-full sm:w-auto">
              Clear
            </Button>
          </div>
        </CardContent>
      </Card>

      {/* Results */}
      <Card>
        <CardHeader>
          <div className="flex items-center justify-between">
            <div>
              <CardTitle>Tax Filings ({totalCount})</CardTitle>
              <CardDescription>
                Showing {filteredFilings.length} of {totalCount} tax filings
              </CardDescription>
            </div>
          </div>
        </CardHeader>
        <CardContent>
          <div className="rounded-md border">
            <Table>
              <TableHeader>
                <TableRow>
                  <TableHead>Reference</TableHead>
                  <TableHead>Client</TableHead>
                  <TableHead>Tax Type</TableHead>
                  <TableHead>Tax Year</TableHead>
                  <TableHead>Due Date</TableHead>
                  <TableHead>Tax Liability</TableHead>
                  <TableHead>Status</TableHead>
                  <TableHead>Actions</TableHead>
                </TableRow>
              </TableHeader>
              <TableBody>
                {filteredFilings.map((filing) => (
                  <TableRow key={filing.taxFilingId}>
                    <TableCell className="font-mono text-sm">
                      {filing.filingReference}
                    </TableCell>
                    <TableCell>
                      <div>
                        <div className="font-medium">{filing.clientName}</div>
                        <div className="text-sm text-muted-foreground">{filing.clientNumber}</div>
                      </div>
                    </TableCell>
                    <TableCell>
                      <Badge className={getTaxTypeColor(filing.taxType)}>
                        {filing.taxType.replace(/([A-Z])/g, ' $1').trim()}
                      </Badge>
                    </TableCell>
                    <TableCell>{filing.taxYear}</TableCell>
                    <TableCell>
                      <div className="flex items-center gap-1">
                        <Calendar className="h-4 w-4 text-muted-foreground" />
                        {new Date(filing.dueDate).toLocaleDateString()}
                      </div>
                    </TableCell>
                    <TableCell>
                      <div className="flex items-center gap-1">
                        <DollarSign className="h-4 w-4 text-muted-foreground" />
                        {filing.taxLiability.toLocaleString('en-US', {
                          style: 'currency',
                          currency: 'SLE'
                        })}
                      </div>
                    </TableCell>
                    <TableCell>
                      <Badge variant={getStatusBadgeVariant(filing.status)}>
                        {filing.status.replace(/([A-Z])/g, ' $1').trim()}
                      </Badge>
                    </TableCell>
                    <TableCell>
                      <div className="flex items-center gap-2">
                        <Button
                          size="sm"
                          variant="outline"
                          onClick={() => setSelectedFiling(filing)}
                        >
                          <Eye className="h-4 w-4" />
                        </Button>
                        {filing.status === FilingStatus.Draft && (
                          <>
                            <Button size="sm" variant="outline">
                              <Edit className="h-4 w-4" />
                            </Button>
                            <Button size="sm" variant="outline">
                              <Trash className="h-4 w-4" />
                            </Button>
                          </>
                        )}
                      </div>
                    </TableCell>
                  </TableRow>
                ))}
              </TableBody>
            </Table>
          </div>

          {/* Pagination */}
          {totalPages > 1 && (
            <div className="flex items-center justify-between mt-4">
              <div className="text-sm text-muted-foreground">
                Page {currentPage} of {totalPages}
              </div>
              <div className="flex items-center gap-2">
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
                  onClick={() => setCurrentPage(prev => Math.min(totalPages, prev + 1))}
                  disabled={currentPage === totalPages}
                >
                  Next
                </Button>
              </div>
            </div>
          )}
        </CardContent>
      </Card>

      {/* Selected Filing Details Dialog */}
      {selectedFiling && (
        <Dialog open={!!selectedFiling} onOpenChange={() => setSelectedFiling(null)}>
          <DialogContent className="max-w-4xl">
            <DialogHeader>
              <DialogTitle>Tax Filing Details - {selectedFiling.filingReference}</DialogTitle>
            </DialogHeader>
            <div className="grid gap-6">
              <div className="grid grid-cols-2 gap-4">
                <div>
                  <label className="text-sm font-medium">Client</label>
                  <p className="text-sm text-muted-foreground">{selectedFiling.clientName} ({selectedFiling.clientNumber})</p>
                </div>
                <div>
                  <label className="text-sm font-medium">Tax Type</label>
                  <p className="text-sm text-muted-foreground">{selectedFiling.taxType}</p>
                </div>
                <div>
                  <label className="text-sm font-medium">Tax Year</label>
                  <p className="text-sm text-muted-foreground">{selectedFiling.taxYear}</p>
                </div>
                <div>
                  <label className="text-sm font-medium">Status</label>
                  <Badge variant={getStatusBadgeVariant(selectedFiling.status)}>
                    {selectedFiling.status}
                  </Badge>
                </div>
                <div>
                  <label className="text-sm font-medium">Due Date</label>
                  <p className="text-sm text-muted-foreground">{new Date(selectedFiling.dueDate).toLocaleDateString()}</p>
                </div>
                <div>
                  <label className="text-sm font-medium">Tax Liability</label>
                  <p className="text-sm text-muted-foreground">
                    {selectedFiling.taxLiability.toLocaleString('en-US', {
                      style: 'currency',
                      currency: 'SLE'
                    })}
                  </p>
                </div>
              </div>
              
              {selectedFiling.reviewComments && (
                <div>
                  <label className="text-sm font-medium">Review Comments</label>
                  <p className="text-sm text-muted-foreground mt-1">{selectedFiling.reviewComments}</p>
                </div>
              )}
              
              <div className="grid grid-cols-3 gap-4 text-center">
                <Card>
                  <CardContent className="pt-6">
                    <div className="text-2xl font-bold text-sierra-blue">{selectedFiling.documentCount}</div>
                    <p className="text-sm text-muted-foreground">Documents</p>
                  </CardContent>
                </Card>
                <Card>
                  <CardContent className="pt-6">
                    <div className="text-2xl font-bold text-sierra-gold">{selectedFiling.paymentCount}</div>
                    <p className="text-sm text-muted-foreground">Payments</p>
                  </CardContent>
                </Card>
                <Card>
                  <CardContent className="pt-6">
                    <div className="text-2xl font-bold text-sierra-green">
                      {selectedFiling.totalPaid.toLocaleString('en-US', {
                        style: 'currency',
                        currency: 'SLE'
                      })}
                    </div>
                    <p className="text-sm text-muted-foreground">Total Paid</p>
                  </CardContent>
                </Card>
              </div>
            </div>
          </DialogContent>
        </Dialog>
      )}
    </div>
  )
}