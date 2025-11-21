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
import { Textarea } from '@/components/ui/textarea'
import { PaymentService, PaymentDto, PaymentStatus, PaymentMethod, CreatePaymentDto } from '@/lib/services'
import { Plus, Search, DollarSign, Filter, Eye, CheckCircle, XCircle, Clock, CreditCard } from 'lucide-react'
import { formatSierraLeones } from '@/lib/utils/currency'
import Loading from '@/app/loading'
import Link from 'next/link'

export default function PaymentsPage() {
  const { toast } = useToast()
  const [loading, setLoading] = useState(true)
  const [payments, setPayments] = useState<PaymentDto[]>([])
  const [pendingApprovals, setPendingApprovals] = useState<PaymentDto[]>([])
  const [searchTerm, setSearchTerm] = useState('')
  const [selectedStatus, setSelectedStatus] = useState<PaymentStatus | 'ALL'>('ALL')
  const [selectedPayment, setSelectedPayment] = useState<PaymentDto | null>(null)
  const [showApprovalDialog, setShowApprovalDialog] = useState(false)
  const [approvalAction, setApprovalAction] = useState<'approve' | 'reject'>('approve')
  const [approvalComments, setApprovalComments] = useState('')
  const [rejectionReason, setRejectionReason] = useState('')
  const [currentPage, setCurrentPage] = useState(1)
  const [totalPages, setTotalPages] = useState(1)
  const [totalCount, setTotalCount] = useState(0)

  const pageSize = 20

  // Fetch payments
  const fetchPayments = async () => {
    try {
      setLoading(true)
      const response = await PaymentService.getPayments(
        currentPage,
        pageSize,
        searchTerm || undefined,
        selectedStatus !== 'ALL' ? selectedStatus : undefined
      )
      
      if (response.success) {
        setPayments(response.data)
        setTotalPages(response.pagination.totalPages)
        setTotalCount(response.pagination.totalCount)
      }
    } catch (error) {
      console.error('Error fetching payments:', error)
      
      // Set empty state so UI still renders
      setPayments([])
      setTotalPages(1)
      setTotalCount(0)
      
      // Check if it's an authentication error
      const isAuthError = (error as any)?.response?.status === 401 || (error as any)?.message?.includes('401')
      
      toast({
        variant: 'destructive',
        title: isAuthError ? 'Authentication Required' : 'Error Loading Payments',
        description: isAuthError 
          ? 'Please log in to view payments. The page will still function for creating new payments.'
          : 'Failed to load payments data. You can still create new payments.',
      })
    } finally {
      // Always clear loading state so UI renders
      setLoading(false)
    }
  }

  // Fetch pending approvals
  const fetchPendingApprovals = async () => {
    try {
      const response = await PaymentService.getPendingApprovals()
      if (response.success) {
        setPendingApprovals(response.data)
      }
    } catch (error) {
      console.error('Error fetching pending approvals:', error)
      
      // Set empty state so UI still renders
      setPendingApprovals([])
      
      // Don't show toast for this error since we already show one for main payments
      // The user will see the authentication error from fetchPayments
    }
  }

  useEffect(() => {
    fetchPayments()
    fetchPendingApprovals()
  }, [currentPage, selectedStatus])

  // Handle search
  const handleSearch = () => {
    setCurrentPage(1)
    fetchPayments()
  }

  // Clear filters
  const clearFilters = () => {
    setSearchTerm('')
    setSelectedStatus('ALL')
    setCurrentPage(1)
    fetchPayments()
  }

  // Handle approval
  const handleApproval = async () => {
    if (!selectedPayment) return

    try {
      let result
      if (approvalAction === 'approve') {
        result = await PaymentService.approvePayment(selectedPayment.paymentId, {
          comments: approvalComments
        })
      } else {
        if (!rejectionReason.trim()) {
          toast({
            variant: 'destructive',
            title: 'Error',
            description: 'Rejection reason is required',
          })
          return
        }
        result = await PaymentService.rejectPayment(selectedPayment.paymentId, {
          rejectionReason
        })
      }

      if (result.success) {
        toast({
          title: 'Success',
          description: `Payment ${approvalAction === 'approve' ? 'approved' : 'rejected'} successfully`,
        })
        setShowApprovalDialog(false)
        setSelectedPayment(null)
        setApprovalComments('')
        setRejectionReason('')
        fetchPayments()
        fetchPendingApprovals()
      }
    } catch (error) {
      console.error('Error processing approval:', error)
      toast({
        variant: 'destructive',
        title: 'Error',
        description: `Failed to ${approvalAction} payment`,
      })
    }
  }

  // Get status badge variant
  const getStatusBadgeVariant = (status: PaymentStatus) => {
    switch (status) {
      case PaymentStatus.Pending:
        return 'outline'
      case PaymentStatus.Approved:
        return 'default'
      case PaymentStatus.Rejected:
        return 'destructive'
      case PaymentStatus.Processing:
        return 'secondary'
      case PaymentStatus.Completed:
        return 'default'
      case PaymentStatus.Failed:
        return 'destructive'
      default:
        return 'secondary'
    }
  }

  // Get payment method icon
  const getPaymentMethodIcon = (method: PaymentMethod) => {
    switch (method) {
      case PaymentMethod.BankTransfer:
        return <CreditCard className="h-4 w-4" />
      case PaymentMethod.Cash:
        return <DollarSign className="h-4 w-4" />
      case PaymentMethod.Check:
        return <CreditCard className="h-4 w-4" />
      case PaymentMethod.OnlinePayment:
        return <CreditCard className="h-4 w-4" />
      case PaymentMethod.MobileMoney:
        return <CreditCard className="h-4 w-4" />
      default:
        return <DollarSign className="h-4 w-4" />
    }
  }

  return (
    <div className="flex-1 space-y-4 p-4 md:p-8 pt-6">
      {/* Header - Always Visible */}
      <div className="flex items-center justify-between">
        <div>
          <h2 className="text-3xl font-bold tracking-tight text-sierra-blue-900">Payments</h2>
          <p className="text-muted-foreground">
            Manage payment records and approvals for Sierra Leone tax compliance
          </p>
        </div>
        <Button asChild data-testid="new-payment-button">
          <Link href="/payments/new">
            <Plus className="mr-2 h-4 w-4" />
            New Payment
          </Link>
        </Button>
      </div>

      {/* Loading Indicator */}
      {loading && (
        <Card>
          <CardContent className="py-8">
            <div className="flex items-center justify-center text-muted-foreground">
              Loading payments...
            </div>
          </CardContent>
        </Card>
      )}

      {/* Quick Stats */}
      <div className="grid gap-4 md:grid-cols-4">
        <Card>
          <CardContent className="pt-6">
            <div className="flex items-center justify-between">
              <div>
                <p className="text-sm font-medium text-muted-foreground">Total Payments</p>
                <p className="text-2xl font-bold text-sierra-blue">{totalCount}</p>
              </div>
            </div>
          </CardContent>
        </Card>
        <Card>
          <CardContent className="pt-6">
            <div className="flex items-center justify-between">
              <div>
                <p className="text-sm font-medium text-muted-foreground">Pending Approval</p>
                <p className="text-2xl font-bold text-sierra-gold">{pendingApprovals.length}</p>
              </div>
              <Clock className="h-8 w-8 text-sierra-gold" />
            </div>
          </CardContent>
        </Card>
        <Card>
          <CardContent className="pt-6">
            <div className="flex items-center justify-between">
              <div>
                <p className="text-sm font-medium text-muted-foreground">Approved Today</p>
                <p className="text-2xl font-bold text-sierra-green">
                  {payments.filter(p => 
                    p.status === PaymentStatus.Approved && 
                    p.approvedAt && 
                    new Date(p.approvedAt).toDateString() === new Date().toDateString()
                  ).length}
                </p>
              </div>
              <CheckCircle className="h-8 w-8 text-sierra-green" />
            </div>
          </CardContent>
        </Card>
        <Card>
          <CardContent className="pt-6">
            <div className="flex items-center justify-between">
              <div>
                <p className="text-sm font-medium text-muted-foreground">Total Value</p>
                <p className="text-2xl font-bold">
                  {formatSierraLeones(payments.reduce((sum, p) => sum + p.amount, 0))}
                </p>
              </div>
            </div>
          </CardContent>
        </Card>
      </div>

      <Tabs defaultValue="all-payments" className="space-y-4">
        <TabsList>
          <TabsTrigger value="all-payments">All Payments</TabsTrigger>
          <TabsTrigger value="pending-approval">
            Pending Approval ({pendingApprovals.length})
          </TabsTrigger>
        </TabsList>

        {/* All Payments Tab */}
        <TabsContent value="all-payments" className="space-y-4">
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
                      placeholder="Search by reference, client name..."
                      value={searchTerm}
                      onChange={(e) => setSearchTerm(e.target.value)}
                      className="pl-10"
                      onKeyPress={(e) => e.key === 'Enter' && handleSearch()}
                    />
                  </div>
                </div>
                <div className="w-full sm:w-48">
                  <Select value={selectedStatus} onValueChange={(value) => setSelectedStatus(value as PaymentStatus | 'ALL')}>
                    <SelectTrigger>
                      <SelectValue placeholder="Status" />
                    </SelectTrigger>
                    <SelectContent>
                      <SelectItem value="ALL">All Status</SelectItem>
                      <SelectItem value={PaymentStatus.Pending}>Pending</SelectItem>
                      <SelectItem value={PaymentStatus.Approved}>Approved</SelectItem>
                      <SelectItem value={PaymentStatus.Rejected}>Rejected</SelectItem>
                      <SelectItem value={PaymentStatus.Processing}>Processing</SelectItem>
                      <SelectItem value={PaymentStatus.Completed}>Completed</SelectItem>
                      <SelectItem value={PaymentStatus.Failed}>Failed</SelectItem>
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
                <CardTitle>Payments ({totalCount})</CardTitle>
              </div>
            </CardHeader>
            <CardContent>
              <div className="rounded-md border">
                <Table>
                  <TableHeader>
                    <TableRow>
                      <TableHead>Reference</TableHead>
                      <TableHead>Client</TableHead>
                      <TableHead>Amount</TableHead>
                      <TableHead>Method</TableHead>
                      <TableHead>Date</TableHead>
                      <TableHead>Status</TableHead>
                      <TableHead>Actions</TableHead>
                    </TableRow>
                  </TableHeader>
                  <TableBody>
                    {payments.length === 0 ? (
                      <TableRow>
                        <TableCell colSpan={7} className="text-center py-8">
                          <div className="flex flex-col items-center gap-2 text-muted-foreground">
                            <CreditCard className="h-8 w-8" />
                            <p>No payments found</p>
                            <p className="text-sm">
                              {totalCount === 0 ? 'Use the "New Payment" button above to create your first payment.' : 'Try adjusting your search filters.'}
                            </p>
                          </div>
                        </TableCell>
                      </TableRow>
                    ) : (
                      payments.map((payment) => (
                        <TableRow key={payment.paymentId}>
                        <TableCell className="font-mono text-sm">
                          {payment.paymentReference}
                        </TableCell>
                        <TableCell>
                          <div>
                            <div className="font-medium">{payment.clientName}</div>
                            <div className="text-sm text-muted-foreground">{payment.clientNumber}</div>
                          </div>
                        </TableCell>
                        <TableCell>
                          <div className="flex items-center gap-1 font-medium">
                            <DollarSign className="h-4 w-4 text-muted-foreground" />
                            {formatSierraLeones(payment.amount)}
                          </div>
                        </TableCell>
                        <TableCell>
                          <div className="flex items-center gap-2">
                            {getPaymentMethodIcon(payment.method)}
                            <span>{payment.method.replace(/([A-Z])/g, ' $1').trim()}</span>
                          </div>
                        </TableCell>
                        <TableCell>
                          {new Date(payment.paymentDate).toLocaleDateString()}
                        </TableCell>
                        <TableCell>
                          <Badge variant={getStatusBadgeVariant(payment.status)}>
                            {payment.status}
                          </Badge>
                        </TableCell>
                        <TableCell>
                          <div className="flex items-center gap-2">
                            <Button
                              size="sm"
                              variant="outline"
                              onClick={() => setSelectedPayment(payment)}
                            >
                              <Eye className="h-4 w-4" />
                            </Button>
                            {payment.status === PaymentStatus.Pending && (
                              <>
                                <Button
                                  size="sm"
                                  variant="outline"
                                  onClick={() => {
                                    setSelectedPayment(payment)
                                    setApprovalAction('approve')
                                    setShowApprovalDialog(true)
                                  }}
                                  className="text-green-600 hover:text-green-700"
                                >
                                  <CheckCircle className="h-4 w-4" />
                                </Button>
                                <Button
                                  size="sm"
                                  variant="outline"
                                  onClick={() => {
                                    setSelectedPayment(payment)
                                    setApprovalAction('reject')
                                    setShowApprovalDialog(true)
                                  }}
                                  className="text-red-600 hover:text-red-700"
                                >
                                  <XCircle className="h-4 w-4" />
                                </Button>
                              </>
                            )}
                          </div>
                        </TableCell>
                      </TableRow>
                    )))}
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
        </TabsContent>

        {/* Pending Approval Tab */}
        <TabsContent value="pending-approval" className="space-y-4">
          <Card>
            <CardHeader>
              <CardTitle>Pending Payment Approvals</CardTitle>
              <CardDescription>
                Review and approve payment submissions
              </CardDescription>
            </CardHeader>
            <CardContent>
              <div className="rounded-md border">
                <Table>
                  <TableHeader>
                    <TableRow>
                      <TableHead>Reference</TableHead>
                      <TableHead>Client</TableHead>
                      <TableHead>Amount</TableHead>
                      <TableHead>Method</TableHead>
                      <TableHead>Submitted</TableHead>
                      <TableHead>Actions</TableHead>
                    </TableRow>
                  </TableHeader>
                  <TableBody>
                    {pendingApprovals.length === 0 ? (
                      <TableRow>
                        <TableCell colSpan={6} className="text-center py-8">
                          <div className="flex flex-col items-center gap-2 text-muted-foreground">
                            <Clock className="h-8 w-8" />
                            <p>No pending approvals</p>
                            <p className="text-sm">All payments have been processed or no payments require approval.</p>
                          </div>
                        </TableCell>
                      </TableRow>
                    ) : (
                      pendingApprovals.map((payment) => (
                        <TableRow key={payment.paymentId}>
                        <TableCell className="font-mono text-sm">
                          {payment.paymentReference}
                        </TableCell>
                        <TableCell>
                          <div>
                            <div className="font-medium">{payment.clientName}</div>
                            <div className="text-sm text-muted-foreground">{payment.clientNumber}</div>
                          </div>
                        </TableCell>
                        <TableCell>
                          <div className="flex items-center gap-1 font-medium">
                            <DollarSign className="h-4 w-4 text-muted-foreground" />
                            {formatSierraLeones(payment.amount)}
                          </div>
                        </TableCell>
                        <TableCell>
                          <div className="flex items-center gap-2">
                            {getPaymentMethodIcon(payment.method)}
                            <span>{payment.method.replace(/([A-Z])/g, ' $1').trim()}</span>
                          </div>
                        </TableCell>
                        <TableCell>
                          {new Date(payment.createdAt).toLocaleDateString()}
                        </TableCell>
                        <TableCell>
                          <div className="flex items-center gap-2">
                            <Button
                              size="sm"
                              onClick={() => {
                                setSelectedPayment(payment)
                                setApprovalAction('approve')
                                setShowApprovalDialog(true)
                              }}
                              className="bg-green-600 hover:bg-green-700 text-white"
                            >
                              <CheckCircle className="h-4 w-4 mr-1" />
                              Approve
                            </Button>
                            <Button
                              size="sm"
                              variant="destructive"
                              onClick={() => {
                                setSelectedPayment(payment)
                                setApprovalAction('reject')
                                setShowApprovalDialog(true)
                              }}
                            >
                              <XCircle className="h-4 w-4 mr-1" />
                              Reject
                            </Button>
                          </div>
                        </TableCell>
                      </TableRow>
                    )))}
                  </TableBody>
                </Table>
              </div>
            </CardContent>
          </Card>
        </TabsContent>
      </Tabs>

      {/* Payment Details Dialog */}
      {selectedPayment && !showApprovalDialog && (
        <Dialog open={!!selectedPayment} onOpenChange={() => setSelectedPayment(null)}>
          <DialogContent className="max-w-2xl">
            <DialogHeader>
              <DialogTitle>Payment Details - {selectedPayment.paymentReference}</DialogTitle>
            </DialogHeader>
            <div className="space-y-4">
              <div className="grid grid-cols-2 gap-4">
                <div>
                  <label className="text-sm font-medium">Client</label>
                  <p className="text-sm text-muted-foreground">{selectedPayment.clientName} ({selectedPayment.clientNumber})</p>
                </div>
                <div>
                  <label className="text-sm font-medium">Amount</label>
                  <p className="text-sm text-muted-foreground">
                    {formatSierraLeones(selectedPayment.amount)}
                  </p>
                </div>
                <div>
                  <label className="text-sm font-medium">Payment Method</label>
                  <p className="text-sm text-muted-foreground">{selectedPayment.method}</p>
                </div>
                <div>
                  <label className="text-sm font-medium">Status</label>
                  <Badge variant={getStatusBadgeVariant(selectedPayment.status)}>
                    {selectedPayment.status}
                  </Badge>
                </div>
                <div>
                  <label className="text-sm font-medium">Payment Date</label>
                  <p className="text-sm text-muted-foreground">{new Date(selectedPayment.paymentDate).toLocaleDateString()}</p>
                </div>
                <div>
                  <label className="text-sm font-medium">Created Date</label>
                  <p className="text-sm text-muted-foreground">{new Date(selectedPayment.createdAt).toLocaleDateString()}</p>
                </div>
              </div>
              
              {selectedPayment.approvalWorkflow && (
                <div>
                  <label className="text-sm font-medium">Approval Workflow</label>
                  <p className="text-sm text-muted-foreground mt-1">{selectedPayment.approvalWorkflow}</p>
                </div>
              )}
              
              {selectedPayment.rejectionReason && (
                <div>
                  <label className="text-sm font-medium">Rejection Reason</label>
                  <p className="text-sm text-muted-foreground mt-1 text-red-600">{selectedPayment.rejectionReason}</p>
                </div>
              )}
            </div>
          </DialogContent>
        </Dialog>
      )}

      {/* Approval Dialog */}
      {showApprovalDialog && selectedPayment && (
        <Dialog open={showApprovalDialog} onOpenChange={setShowApprovalDialog}>
          <DialogContent>
            <DialogHeader>
              <DialogTitle>
                {approvalAction === 'approve' ? 'Approve' : 'Reject'} Payment
              </DialogTitle>
            </DialogHeader>
            <div className="space-y-4">
              <div>
                <p className="text-sm text-muted-foreground">
                  Payment: {selectedPayment.paymentReference}
                </p>
                <p className="text-sm text-muted-foreground">
                  Client: {selectedPayment.clientName}
                </p>
                <p className="text-sm text-muted-foreground">
                  Amount: {formatSierraLeones(selectedPayment.amount)}
                </p>
              </div>
              
              {approvalAction === 'approve' ? (
                <div>
                  <label className="text-sm font-medium">Comments (Optional)</label>
                  <Textarea
                    placeholder="Add any comments for this approval..."
                    value={approvalComments}
                    onChange={(e) => setApprovalComments(e.target.value)}
                    className="mt-1"
                  />
                </div>
              ) : (
                <div>
                  <label className="text-sm font-medium">Rejection Reason *</label>
                  <Textarea
                    placeholder="Please provide a reason for rejecting this payment..."
                    value={rejectionReason}
                    onChange={(e) => setRejectionReason(e.target.value)}
                    className="mt-1"
                    required
                  />
                </div>
              )}
              
              <div className="flex justify-end gap-2">
                <Button variant="outline" onClick={() => setShowApprovalDialog(false)}>
                  Cancel
                </Button>
                <Button
                  onClick={handleApproval}
                  className={approvalAction === 'approve' ? 'bg-green-600 hover:bg-green-700' : ''}
                  variant={approvalAction === 'reject' ? 'destructive' : 'default'}
                >
                  {approvalAction === 'approve' ? 'Approve Payment' : 'Reject Payment'}
                </Button>
              </div>
            </div>
          </DialogContent>
        </Dialog>
      )}
    </div>
  )
}