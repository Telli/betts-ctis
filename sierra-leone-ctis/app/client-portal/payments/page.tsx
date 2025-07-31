'use client'

import { useState, useEffect } from 'react'
import { Card, CardContent, CardDescription, CardHeader, CardTitle } from '@/components/ui/card'
import { Badge } from '@/components/ui/badge'
import { Button } from '@/components/ui/button'
import { Tabs, TabsContent, TabsList, TabsTrigger } from '@/components/ui/tabs'
import { 
  DollarSign, 
  CreditCard, 
  Calendar, 
  CheckCircle, 
  Clock,
  AlertTriangle,
  Download,
  Receipt,
  Filter,
  Plus
} from 'lucide-react'
import { format } from 'date-fns'
import { Dialog, DialogContent, DialogHeader, DialogTitle, DialogTrigger } from '@/components/ui/dialog'
import ClientPaymentForm from '@/components/client-portal/forms/payment-form'

interface Payment {
  id: string
  paymentReference: string
  taxType: string
  taxYear: number
  amount: number
  method: 'bank-transfer' | 'orange-money' | 'africell-money' | 'paypal' | 'stripe'
  status: 'pending' | 'processing' | 'confirmed' | 'failed' | 'refunded'
  paymentDate: Date
  processedDate?: Date
  transactionId?: string
  receiptNumber?: string
  notes?: string
  feeAmount?: number
  currency: string
  exchangeRate?: number
  originalAmount?: number
  originalCurrency?: string
}

export default function ClientPaymentsPage() {
  const [payments, setPayments] = useState<Payment[]>([])
  const [loading, setLoading] = useState(true)
  const [activeTab, setActiveTab] = useState('recent')
  const [filterStatus, setFilterStatus] = useState<string>('all')
  const [showCreateDialog, setShowCreateDialog] = useState(false)

  useEffect(() => {
    // Mock data - replace with actual API call
    const mockPayments: Payment[] = [
      {
        id: '1',
        paymentReference: 'PAY-2025-001',
        taxType: 'Income Tax',
        taxYear: 2024,
        amount: 150000,
        method: 'bank-transfer',
        status: 'confirmed',
        paymentDate: new Date(2025, 0, 15),
        processedDate: new Date(2025, 0, 15),
        transactionId: 'TXN-BT-001234',
        receiptNumber: 'RCP-2025-001',
        notes: 'Annual Income Tax payment for 2024',
        currency: 'SLE'
      },
      {
        id: '2',
        paymentReference: 'PAY-2025-002',
        taxType: 'GST',
        taxYear: 2024,
        amount: 50000,
        method: 'orange-money',
        status: 'confirmed',
        paymentDate: new Date(2025, 0, 25),
        processedDate: new Date(2025, 0, 25),
        transactionId: 'OM-789456',
        receiptNumber: 'RCP-2025-002',
        feeAmount: 500,
        currency: 'SLE'
      },
      {
        id: '3',
        paymentReference: 'PAY-2025-003',
        taxType: 'Payroll Tax',
        taxYear: 2024,
        amount: 25000,
        method: 'paypal',
        status: 'processing',
        paymentDate: new Date(2025, 0, 28),
        transactionId: 'PP-ABC123XYZ',
        originalAmount: 125.50,
        originalCurrency: 'USD',
        exchangeRate: 199.20,
        currency: 'SLE'
      },
      {
        id: '4',
        paymentReference: 'PAY-2024-015',
        taxType: 'Income Tax',
        taxYear: 2023,
        amount: 125000,
        method: 'bank-transfer',
        status: 'confirmed',
        paymentDate: new Date(2024, 2, 20),
        processedDate: new Date(2024, 2, 20),
        transactionId: 'TXN-BT-005678',
        receiptNumber: 'RCP-2024-015',
        currency: 'SLE'
      },
      {
        id: '5',
        paymentReference: 'PAY-2025-004',
        taxType: 'Excise Duty',
        taxYear: 2024,
        amount: 75000,
        method: 'africell-money',
        status: 'failed',
        paymentDate: new Date(2025, 0, 30),
        transactionId: 'AM-DEF456',
        notes: 'Payment failed due to insufficient balance',
        currency: 'SLE'
      }
    ]
    
    setPayments(mockPayments)
    setLoading(false)
  }, [])

  const getStatusIcon = (status: string) => {
    switch (status) {
      case 'confirmed':
        return <CheckCircle className="h-4 w-4 text-green-500" />
      case 'processing':
        return <Clock className="h-4 w-4 text-yellow-500" />
      case 'pending':
        return <Clock className="h-4 w-4 text-blue-500" />
      case 'failed':
        return <AlertTriangle className="h-4 w-4 text-red-500" />
      case 'refunded':
        return <AlertTriangle className="h-4 w-4 text-orange-500" />
      default:
        return null
    }
  }

  const getStatusColor = (status: string) => {
    switch (status) {
      case 'confirmed':
        return 'bg-green-100 text-green-800'
      case 'processing':
        return 'bg-yellow-100 text-yellow-800'
      case 'pending':
        return 'bg-blue-100 text-blue-800'
      case 'failed':
        return 'bg-red-100 text-red-800'
      case 'refunded':
        return 'bg-orange-100 text-orange-800'
      default:
        return 'bg-gray-100 text-gray-800'
    }
  }

  const getMethodIcon = (method: string) => {
    switch (method) {
      case 'bank-transfer':
        return <CreditCard className="h-4 w-4" />
      case 'orange-money':
        return <DollarSign className="h-4 w-4 text-orange-500" />
      case 'africell-money':
        return <DollarSign className="h-4 w-4 text-red-500" />
      case 'paypal':
        return <DollarSign className="h-4 w-4 text-blue-500" />
      case 'stripe':
        return <CreditCard className="h-4 w-4 text-purple-500" />
      default:
        return <DollarSign className="h-4 w-4" />
    }
  }

  const getMethodName = (method: string) => {
    switch (method) {
      case 'bank-transfer':
        return 'Bank Transfer'
      case 'orange-money':
        return 'Orange Money'
      case 'africell-money':
        return 'Africell Money'
      case 'paypal':
        return 'PayPal'
      case 'stripe':
        return 'Credit Card'
      default:
        return method
    }
  }

  const currentYear = new Date().getFullYear()
  const recentPayments = payments.slice(0, 10)
  const thisYearPayments = payments.filter(payment => payment.taxYear === currentYear)
  const filteredPayments = filterStatus === 'all' 
    ? payments 
    : payments.filter(payment => payment.status === filterStatus)

  const stats = {
    total: payments.length,
    confirmed: payments.filter(p => p.status === 'confirmed').length,
    processing: payments.filter(p => p.status === 'processing').length,
    failed: payments.filter(p => p.status === 'failed').length,
    totalAmount: payments.filter(p => p.status === 'confirmed').reduce((sum, p) => sum + p.amount, 0),
    thisYearAmount: thisYearPayments.filter(p => p.status === 'confirmed').reduce((sum, p) => sum + p.amount, 0),
    averagePayment: payments.length > 0 
      ? payments.filter(p => p.status === 'confirmed').reduce((sum, p) => sum + p.amount, 0) / payments.filter(p => p.status === 'confirmed').length 
      : 0
  }

  if (loading) {
    return (
      <div className="flex items-center justify-center min-h-screen">
        <div className="animate-spin rounded-full h-32 w-32 border-b-2 border-sierra-blue"></div>
      </div>
    )
  }

  return (
    <div className="space-y-6">
      <div className="flex justify-between items-center">
        <div>
          <h1 className="text-3xl font-bold tracking-tight">My Payments</h1>
          <p className="text-muted-foreground mt-2">
            View your payment history and transaction details
          </p>
        </div>
        <div className="flex gap-2">
          <Dialog open={showCreateDialog} onOpenChange={setShowCreateDialog}>
            <DialogTrigger asChild>
              <Button className="bg-sierra-blue hover:bg-sierra-blue/90">
                <Plus className="mr-2 h-4 w-4" />
                New Payment
              </Button>
            </DialogTrigger>
            <DialogContent className="max-w-2xl">
              <DialogHeader>
                <DialogTitle>Submit New Payment</DialogTitle>
              </DialogHeader>
              <ClientPaymentForm 
                onSuccess={() => {
                  setShowCreateDialog(false)
                  // Refresh payments list when backend is ready
                }}
              />
            </DialogContent>
          </Dialog>
          <Button variant="outline">
            <Download className="mr-2 h-4 w-4" />
            Export Report
          </Button>
        </div>
      </div>

      {/* Summary Cards */}
      <div className="grid gap-4 md:grid-cols-2 lg:grid-cols-4">
        <Card>
          <CardHeader className="flex flex-row items-center justify-between space-y-0 pb-2">
            <CardTitle className="text-sm font-medium">Total Payments</CardTitle>
            <Receipt className="h-4 w-4 text-muted-foreground" />
          </CardHeader>
          <CardContent>
            <div className="text-2xl font-bold">{stats.total}</div>
            <p className="text-xs text-muted-foreground">
              all time transactions
            </p>
          </CardContent>
        </Card>

        <Card>
          <CardHeader className="flex flex-row items-center justify-between space-y-0 pb-2">
            <CardTitle className="text-sm font-medium">Successful Payments</CardTitle>
            <CheckCircle className="h-4 w-4 text-green-500" />
          </CardHeader>
          <CardContent>
            <div className="text-2xl font-bold text-green-600">{stats.confirmed}</div>
            <p className="text-xs text-muted-foreground">
              completed transactions
            </p>
          </CardContent>
        </Card>

        <Card>
          <CardHeader className="flex flex-row items-center justify-between space-y-0 pb-2">
            <CardTitle className="text-sm font-medium">Total Amount Paid</CardTitle>
            <DollarSign className="h-4 w-4 text-muted-foreground" />
          </CardHeader>
          <CardContent>
            <div className="text-2xl font-bold">
              Le {stats.totalAmount.toLocaleString()}
            </div>
            <p className="text-xs text-muted-foreground">
              all confirmed payments
            </p>
          </CardContent>
        </Card>

        <Card>
          <CardHeader className="flex flex-row items-center justify-between space-y-0 pb-2">
            <CardTitle className="text-sm font-medium">{currentYear} Payments</CardTitle>
            <Calendar className="h-4 w-4 text-muted-foreground" />
          </CardHeader>
          <CardContent>
            <div className="text-2xl font-bold">
              Le {stats.thisYearAmount.toLocaleString()}
            </div>
            <p className="text-xs text-muted-foreground">
              current tax year
            </p>
          </CardContent>
        </Card>
      </div>

      {/* Payment History */}
      <Tabs value={activeTab} onValueChange={setActiveTab}>
        <TabsList>
          <TabsTrigger value="recent">Recent Payments</TabsTrigger>
          <TabsTrigger value="current-year">This Year</TabsTrigger>
          <TabsTrigger value="all">All Payments</TabsTrigger>
          <TabsTrigger value="methods">Payment Methods</TabsTrigger>
        </TabsList>

        <TabsContent value="recent" className="space-y-4">
          <Card>
            <CardHeader>
              <CardTitle>Recent Payment Activity</CardTitle>
              <CardDescription>
                Your most recent payment transactions
              </CardDescription>
            </CardHeader>
            <CardContent>
              <div className="space-y-4">
                {recentPayments.map((payment) => (
                  <div key={payment.id} className="p-4 border rounded-lg">
                    <div className="flex justify-between items-start">
                      <div className="space-y-2">
                        <div className="flex items-center gap-2">
                          {getStatusIcon(payment.status)}
                          <span className="font-medium">{payment.paymentReference}</span>
                          <Badge className={getStatusColor(payment.status)}>
                            {payment.status}
                          </Badge>
                        </div>
                        
                        <div className="text-sm text-muted-foreground">
                          {payment.taxType} • Tax Year {payment.taxYear}
                        </div>
                        
                        <div className="flex items-center gap-4 text-sm">
                          <span className="font-medium">
                            Le {payment.amount.toLocaleString()}
                          </span>
                          <span className="flex items-center gap-1">
                            {getMethodIcon(payment.method)}
                            {getMethodName(payment.method)}
                          </span>
                          <span>
                            {format(payment.paymentDate, 'MMM d, yyyy')}
                          </span>
                        </div>
                        
                        {payment.originalAmount && payment.originalCurrency && (
                          <div className="text-sm text-muted-foreground">
                            Original: {payment.originalCurrency} {payment.originalAmount} 
                            (Rate: {payment.exchangeRate})
                          </div>
                        )}
                        
                        {payment.feeAmount && (
                          <div className="text-sm text-muted-foreground">
                            Transaction Fee: Le {payment.feeAmount.toLocaleString()}
                          </div>
                        )}
                        
                        {payment.notes && (
                          <div className="text-sm text-muted-foreground italic">
                            {payment.notes}
                          </div>
                        )}
                      </div>
                      
                      <div className="flex gap-2">
                        {payment.receiptNumber && (
                          <Button variant="outline" size="sm">
                            <Receipt className="mr-2 h-4 w-4" />
                            Receipt
                          </Button>
                        )}
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

        <TabsContent value="current-year" className="space-y-4">
          <Card>
            <CardHeader>
              <CardTitle>Payments for Tax Year {currentYear}</CardTitle>
              <CardDescription>
                All payments made for the current tax year
              </CardDescription>
            </CardHeader>
            <CardContent>
              <div className="space-y-4">
                {thisYearPayments.length === 0 ? (
                  <div className="text-center py-8">
                    <Receipt className="mx-auto h-12 w-12 text-muted-foreground" />
                    <p className="text-muted-foreground mt-2">
                      No payments for {currentYear} yet
                    </p>
                  </div>
                ) : (
                  thisYearPayments.map((payment) => (
                    <div key={payment.id} className="p-4 border rounded-lg">
                      <div className="flex justify-between items-start">
                        <div className="space-y-2">
                          <div className="flex items-center gap-2">
                            {getStatusIcon(payment.status)}
                            <span className="font-medium">{payment.paymentReference}</span>
                            <Badge className={getStatusColor(payment.status)}>
                              {payment.status}
                            </Badge>
                          </div>
                          
                          <div className="text-sm text-muted-foreground">
                            {payment.taxType} • {format(payment.paymentDate, 'MMMM d, yyyy')}
                          </div>
                          
                          <div className="flex items-center gap-4 text-sm">
                            <span className="font-medium">
                              Le {payment.amount.toLocaleString()}
                            </span>
                            <span className="flex items-center gap-1">
                              {getMethodIcon(payment.method)}
                              {getMethodName(payment.method)}
                            </span>
                          </div>
                        </div>
                        
                        <div className="flex gap-2">
                          {payment.receiptNumber && (
                            <Button variant="outline" size="sm">
                              <Receipt className="mr-2 h-4 w-4" />
                              Receipt
                            </Button>
                          )}
                        </div>
                      </div>
                    </div>
                  ))
                )}
              </div>
            </CardContent>
          </Card>
        </TabsContent>

        <TabsContent value="all" className="space-y-4">
          {/* Filter Controls */}
          <Card>
            <CardHeader>
              <CardTitle className="flex items-center gap-2">
                <Filter className="h-5 w-5" />
                Filter Payments
              </CardTitle>
            </CardHeader>
            <CardContent>
              <div className="flex gap-4">
                <div className="space-y-2">
                  <label className="text-sm font-medium">Status</label>
                  <select 
                    value={filterStatus} 
                    onChange={(e) => setFilterStatus(e.target.value)}
                    className="w-40 p-2 border rounded"
                  >
                    <option value="all">All Status</option>
                    <option value="confirmed">Confirmed</option>
                    <option value="processing">Processing</option>
                    <option value="pending">Pending</option>
                    <option value="failed">Failed</option>
                    <option value="refunded">Refunded</option>
                  </select>
                </div>
              </div>
            </CardContent>
          </Card>

          <Card>
            <CardHeader>
              <CardTitle>All Payment History</CardTitle>
              <CardDescription>
                Complete history of all your payments
              </CardDescription>
            </CardHeader>
            <CardContent>
              <div className="space-y-4">
                {filteredPayments
                  .sort((a, b) => b.paymentDate.getTime() - a.paymentDate.getTime())
                  .map((payment) => (
                    <div key={payment.id} className="p-4 border rounded-lg">
                      <div className="flex justify-between items-start">
                        <div className="space-y-2">
                          <div className="flex items-center gap-2">
                            {getStatusIcon(payment.status)}
                            <span className="font-medium">{payment.paymentReference}</span>
                            <Badge className={getStatusColor(payment.status)}>
                              {payment.status}
                            </Badge>
                          </div>
                          
                          <div className="text-sm text-muted-foreground">
                            {payment.taxType} • Tax Year {payment.taxYear}
                          </div>
                          
                          <div className="flex items-center gap-4 text-sm">
                            <span className="font-medium">
                              Le {payment.amount.toLocaleString()}
                            </span>
                            <span className="flex items-center gap-1">
                              {getMethodIcon(payment.method)}
                              {getMethodName(payment.method)}
                            </span>
                            <span>
                              {format(payment.paymentDate, 'MMM d, yyyy')}
                            </span>
                          </div>
                          
                          {payment.transactionId && (
                            <div className="text-xs text-muted-foreground">
                              Transaction ID: {payment.transactionId}
                            </div>
                          )}
                        </div>
                        
                        <div className="flex gap-2">
                          {payment.receiptNumber && (
                            <Button variant="outline" size="sm">
                              <Receipt className="mr-2 h-4 w-4" />
                              Receipt
                            </Button>
                          )}
                        </div>
                      </div>
                    </div>
                  ))}
              </div>
            </CardContent>
          </Card>
        </TabsContent>

        <TabsContent value="methods" className="space-y-4">
          <div className="grid gap-4 md:grid-cols-2">
            <Card>
              <CardHeader>
                <CardTitle>Payment Method Statistics</CardTitle>
              </CardHeader>
              <CardContent>
                <div className="space-y-4">
                  {['bank-transfer', 'orange-money', 'africell-money', 'paypal', 'stripe'].map((method) => {
                    const methodPayments = payments.filter(p => p.method === method && p.status === 'confirmed')
                    const totalAmount = methodPayments.reduce((sum, p) => sum + p.amount, 0)
                    
                    return (
                      <div key={method} className="flex items-center justify-between">
                        <div className="flex items-center gap-2">
                          {getMethodIcon(method)}
                          <span className="font-medium">{getMethodName(method)}</span>
                        </div>
                        <div className="text-right">
                          <div className="font-medium">
                            {methodPayments.length} payments
                          </div>
                          <div className="text-sm text-muted-foreground">
                            Le {totalAmount.toLocaleString()}
                          </div>
                        </div>
                      </div>
                    )
                  })}
                </div>
              </CardContent>
            </Card>

            <Card>
              <CardHeader>
                <CardTitle>Payment Preferences</CardTitle>
              </CardHeader>
              <CardContent>
                <div className="space-y-4">
                  <div>
                    <h4 className="font-medium mb-2">Preferred Payment Method</h4>
                    <p className="text-sm text-muted-foreground">
                      Based on your payment history, you prefer Bank Transfer
                    </p>
                  </div>
                  
                  <div>
                    <h4 className="font-medium mb-2">Average Payment Amount</h4>
                    <p className="text-lg font-bold">
                      Le {Math.round(stats.averagePayment).toLocaleString()}
                    </p>
                  </div>
                  
                  <div>
                    <h4 className="font-medium mb-2">Payment Success Rate</h4>
                    <p className="text-lg font-bold text-green-600">
                      {((stats.confirmed / stats.total) * 100).toFixed(1)}%
                    </p>
                  </div>
                </div>
              </CardContent>
            </Card>
          </div>
        </TabsContent>
      </Tabs>
    </div>
  )
}