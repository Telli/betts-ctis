'use client'

import { useState, useEffect } from 'react'
import { Card, CardContent, CardDescription, CardHeader, CardTitle } from '@/components/ui/card'
import { Badge } from '@/components/ui/badge'
import { Button } from '@/components/ui/button'
import { Tabs, TabsContent, TabsList, TabsTrigger } from '@/components/ui/tabs'
import { Progress } from '@/components/ui/progress'
import { 
  FileText, 
  Calendar, 
  DollarSign, 
  AlertTriangle, 
  CheckCircle, 
  Clock,
  Download,
  Upload,
  Eye,
  Plus
} from 'lucide-react'
import { format } from 'date-fns'
import { Dialog, DialogContent, DialogHeader, DialogTitle, DialogTrigger } from '@/components/ui/dialog'
import ClientTaxFilingForm from '@/components/client-portal/forms/tax-filing-form'

interface TaxFiling {
  id: string
  taxType: string
  taxYear: number
  status: 'draft' | 'submitted' | 'under-review' | 'approved' | 'filed' | 'overdue'
  submittedDate?: Date
  dueDate: Date
  filedDate?: Date
  taxLiability: number
  amountPaid: number
  outstandingBalance: number
  documents: string[]
  notes?: string
  reviewComments?: string
  completionPercentage: number
}

export default function ClientTaxFilingsPage() {
  const [filings, setFilings] = useState<TaxFiling[]>([])
  const [loading, setLoading] = useState(true)
  const [activeTab, setActiveTab] = useState('current')
  const [showCreateDialog, setShowCreateDialog] = useState(false)

  useEffect(() => {
    // Mock data - replace with actual API call
    const mockFilings: TaxFiling[] = [
      {
        id: '1',
        taxType: 'Income Tax',
        taxYear: 2024,
        status: 'filed',
        submittedDate: new Date(2024, 2, 15),
        dueDate: new Date(2024, 2, 31),
        filedDate: new Date(2024, 2, 20),
        taxLiability: 150000,
        amountPaid: 150000,
        outstandingBalance: 0,
        documents: ['Annual Accounts', 'Tax Computation', 'Payment Receipt'],
        notes: 'Filed successfully with full payment',
        completionPercentage: 100
      },
      {
        id: '2',
        taxType: 'GST',
        taxYear: 2024,
        status: 'under-review',
        submittedDate: new Date(2025, 0, 25),
        dueDate: new Date(2025, 0, 31),
        taxLiability: 75000,
        amountPaid: 50000,
        outstandingBalance: 25000,
        documents: ['GST Return', 'Supporting Documents'],
        reviewComments: 'Requires additional supporting documentation for input tax claims',
        completionPercentage: 85
      },
      {
        id: '3',
        taxType: 'Payroll Tax',
        taxYear: 2024,
        status: 'draft',
        dueDate: new Date(2025, 1, 15),
        taxLiability: 45000,
        amountPaid: 0,
        outstandingBalance: 45000,
        documents: ['Payroll Register'],
        notes: 'Draft prepared, pending final review',
        completionPercentage: 60
      },
      {
        id: '4',
        taxType: 'Income Tax',
        taxYear: 2023,
        status: 'filed',
        submittedDate: new Date(2023, 2, 20),
        dueDate: new Date(2023, 2, 31),
        filedDate: new Date(2023, 2, 25),
        taxLiability: 125000,
        amountPaid: 125000,
        outstandingBalance: 0,
        documents: ['Annual Accounts', 'Tax Computation', 'Payment Receipt'],
        completionPercentage: 100
      }
    ]
    
    setFilings(mockFilings)
    setLoading(false)
  }, [])

  const getStatusIcon = (status: string) => {
    switch (status) {
      case 'filed':
        return <CheckCircle className="h-4 w-4 text-green-500" />
      case 'approved':
        return <CheckCircle className="h-4 w-4 text-green-500" />
      case 'under-review':
        return <Clock className="h-4 w-4 text-yellow-500" />
      case 'submitted':
        return <Clock className="h-4 w-4 text-blue-500" />
      case 'draft':
        return <FileText className="h-4 w-4 text-gray-500" />
      case 'overdue':
        return <AlertTriangle className="h-4 w-4 text-red-500" />
      default:
        return null
    }
  }

  const getStatusColor = (status: string) => {
    switch (status) {
      case 'filed':
        return 'bg-green-100 text-green-800'
      case 'approved':
        return 'bg-green-100 text-green-800'
      case 'under-review':
        return 'bg-yellow-100 text-yellow-800'
      case 'submitted':
        return 'bg-blue-100 text-blue-800'
      case 'draft':
        return 'bg-gray-100 text-gray-800'
      case 'overdue':
        return 'bg-red-100 text-red-800'
      default:
        return 'bg-gray-100 text-gray-800'
    }
  }

  const currentYear = new Date().getFullYear()
  const currentFilings = filings.filter(filing => filing.taxYear === currentYear)
  const previousFilings = filings.filter(filing => filing.taxYear < currentYear)

  const stats = {
    total: filings.length,
    filed: filings.filter(f => f.status === 'filed').length,
    pending: filings.filter(f => ['draft', 'submitted', 'under-review'].includes(f.status)).length,
    totalLiability: filings.reduce((sum, f) => sum + f.taxLiability, 0),
    totalPaid: filings.reduce((sum, f) => sum + f.amountPaid, 0),
    outstanding: filings.reduce((sum, f) => sum + f.outstandingBalance, 0)
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
          <h1 className="text-3xl font-bold tracking-tight">My Tax Filings</h1>
          <p className="text-muted-foreground mt-2">
            View and manage your tax filing history and status
          </p>
        </div>
        <div className="flex gap-2">
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
              <ClientTaxFilingForm 
                onSuccess={() => {
                  setShowCreateDialog(false)
                  // Refresh filings list when backend is ready
                }}
              />
            </DialogContent>
          </Dialog>
          <Button variant="outline">
            <Upload className="mr-2 h-4 w-4" />
            Upload Documents
          </Button>
        </div>
      </div>

      {/* Summary Cards */}
      <div className="grid gap-4 md:grid-cols-2 lg:grid-cols-4">
        <Card>
          <CardHeader className="flex flex-row items-center justify-between space-y-0 pb-2">
            <CardTitle className="text-sm font-medium">Total Filings</CardTitle>
            <FileText className="h-4 w-4 text-muted-foreground" />
          </CardHeader>
          <CardContent>
            <div className="text-2xl font-bold">{stats.total}</div>
            <p className="text-xs text-muted-foreground">
              across all tax years
            </p>
          </CardContent>
        </Card>

        <Card>
          <CardHeader className="flex flex-row items-center justify-between space-y-0 pb-2">
            <CardTitle className="text-sm font-medium">Successfully Filed</CardTitle>
            <CheckCircle className="h-4 w-4 text-green-500" />
          </CardHeader>
          <CardContent>
            <div className="text-2xl font-bold text-green-600">{stats.filed}</div>
            <p className="text-xs text-muted-foreground">
              completed filings
            </p>
          </CardContent>
        </Card>

        <Card>
          <CardHeader className="flex flex-row items-center justify-between space-y-0 pb-2">
            <CardTitle className="text-sm font-medium">Total Tax Liability</CardTitle>
            <DollarSign className="h-4 w-4 text-muted-foreground" />
          </CardHeader>
          <CardContent>
            <div className="text-2xl font-bold">
              Le {stats.totalLiability.toLocaleString()}
            </div>
            <p className="text-xs text-muted-foreground">
              across all filings
            </p>
          </CardContent>
        </Card>

        <Card>
          <CardHeader className="flex flex-row items-center justify-between space-y-0 pb-2">
            <CardTitle className="text-sm font-medium">Outstanding Balance</CardTitle>
            <AlertTriangle className="h-4 w-4 text-orange-500" />
          </CardHeader>
          <CardContent>
            <div className="text-2xl font-bold text-orange-600">
              Le {stats.outstanding.toLocaleString()}
            </div>
            <p className="text-xs text-muted-foreground">
              pending payment
            </p>
          </CardContent>
        </Card>
      </div>

      {/* Tax Filings */}
      <Tabs value={activeTab} onValueChange={setActiveTab}>
        <TabsList>
          <TabsTrigger value="current">Current Year ({currentYear})</TabsTrigger>
          <TabsTrigger value="previous">Previous Years</TabsTrigger>
          <TabsTrigger value="all">All Filings</TabsTrigger>
        </TabsList>

        <TabsContent value="current" className="space-y-4">
          <Card>
            <CardHeader>
              <CardTitle>Tax Year {currentYear} Filings</CardTitle>
              <CardDescription>
                Your tax filings for the current tax year
              </CardDescription>
            </CardHeader>
            <CardContent>
              <div className="space-y-4">
                {currentFilings.length === 0 ? (
                  <div className="text-center py-8">
                    <FileText className="mx-auto h-12 w-12 text-muted-foreground" />
                    <p className="text-muted-foreground mt-2">
                      No tax filings for {currentYear} yet
                    </p>
                  </div>
                ) : (
                  currentFilings.map((filing) => (
                    <div key={filing.id} className="p-4 border rounded-lg">
                      <div className="flex justify-between items-start">
                        <div className="space-y-2">
                          <div className="flex items-center gap-2">
                            {getStatusIcon(filing.status)}
                            <span className="font-medium">{filing.taxType}</span>
                            <Badge className={getStatusColor(filing.status)}>
                              {filing.status.replace('-', ' ')}
                            </Badge>
                          </div>
                          
                          <div className="text-sm text-muted-foreground">
                            Due: {format(filing.dueDate, 'MMMM d, yyyy')}
                            {filing.submittedDate && (
                              <span className="ml-4">
                                Submitted: {format(filing.submittedDate, 'MMMM d, yyyy')}
                              </span>
                            )}
                          </div>
                          
                          <div className="flex gap-4 text-sm">
                            <span>Tax Liability: Le {filing.taxLiability.toLocaleString()}</span>
                            <span>Paid: Le {filing.amountPaid.toLocaleString()}</span>
                            {filing.outstandingBalance > 0 && (
                              <span className="text-orange-600">
                                Outstanding: Le {filing.outstandingBalance.toLocaleString()}
                              </span>
                            )}
                          </div>
                          
                          <div className="space-y-1">
                            <div className="flex justify-between text-sm">
                              <span>Completion Progress</span>
                              <span>{filing.completionPercentage}%</span>
                            </div>
                            <Progress value={filing.completionPercentage} />
                          </div>
                          
                          {filing.reviewComments && (
                            <div className="p-2 bg-yellow-50 border border-yellow-200 rounded text-sm">
                              <strong>Review Comments:</strong> {filing.reviewComments}
                            </div>
                          )}
                          
                          <div className="flex gap-2">
                            {filing.documents.map((doc, index) => (
                              <Badge key={index} variant="outline">
                                {doc}
                              </Badge>
                            ))}
                          </div>
                        </div>
                        
                        <div className="flex gap-2">
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
                  ))
                )}
              </div>
            </CardContent>
          </Card>
        </TabsContent>

        <TabsContent value="previous" className="space-y-4">
          <Card>
            <CardHeader>
              <CardTitle>Previous Tax Years</CardTitle>
              <CardDescription>
                Historical tax filings from previous years
              </CardDescription>
            </CardHeader>
            <CardContent>
              <div className="space-y-4">
                {previousFilings.map((filing) => (
                  <div key={filing.id} className="p-4 border rounded-lg">
                    <div className="flex justify-between items-start">
                      <div className="space-y-2">
                        <div className="flex items-center gap-2">
                          {getStatusIcon(filing.status)}
                          <span className="font-medium">{filing.taxType} - {filing.taxYear}</span>
                          <Badge className={getStatusColor(filing.status)}>
                            {filing.status.replace('-', ' ')}
                          </Badge>
                        </div>
                        
                        <div className="text-sm text-muted-foreground">
                          {filing.filedDate && (
                            <span>Filed: {format(filing.filedDate, 'MMMM d, yyyy')}</span>
                          )}
                        </div>
                        
                        <div className="flex gap-4 text-sm">
                          <span>Tax Liability: Le {filing.taxLiability.toLocaleString()}</span>
                          <span>Paid: Le {filing.amountPaid.toLocaleString()}</span>
                        </div>
                        
                        <div className="flex gap-2">
                          {filing.documents.map((doc, index) => (
                            <Badge key={index} variant="outline">
                              {doc}
                            </Badge>
                          ))}
                        </div>
                      </div>
                      
                      <div className="flex gap-2">
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

        <TabsContent value="all" className="space-y-4">
          <Card>
            <CardHeader>
              <CardTitle>All Tax Filings</CardTitle>
              <CardDescription>
                Complete history of all your tax filings
              </CardDescription>
            </CardHeader>
            <CardContent>
              <div className="space-y-4">
                {filings
                  .sort((a, b) => b.taxYear - a.taxYear || a.taxType.localeCompare(b.taxType))
                  .map((filing) => (
                    <div key={filing.id} className="p-4 border rounded-lg">
                      <div className="flex justify-between items-start">
                        <div className="space-y-2">
                          <div className="flex items-center gap-2">
                            {getStatusIcon(filing.status)}
                            <span className="font-medium">{filing.taxType} - {filing.taxYear}</span>
                            <Badge className={getStatusColor(filing.status)}>
                              {filing.status.replace('-', ' ')}
                            </Badge>
                          </div>
                          
                          <div className="text-sm text-muted-foreground">
                            Due: {format(filing.dueDate, 'MMMM d, yyyy')}
                            {filing.filedDate && (
                              <span className="ml-4">
                                Filed: {format(filing.filedDate, 'MMMM d, yyyy')}
                              </span>
                            )}
                          </div>
                          
                          <div className="flex gap-4 text-sm">
                            <span>Tax Liability: Le {filing.taxLiability.toLocaleString()}</span>
                            <span>Paid: Le {filing.amountPaid.toLocaleString()}</span>
                            {filing.outstandingBalance > 0 && (
                              <span className="text-orange-600">
                                Outstanding: Le {filing.outstandingBalance.toLocaleString()}
                              </span>
                            )}
                          </div>
                          
                          {filing.status !== 'filed' && (
                            <div className="space-y-1">
                              <div className="flex justify-between text-sm">
                                <span>Completion Progress</span>
                                <span>{filing.completionPercentage}%</span>
                              </div>
                              <Progress value={filing.completionPercentage} />
                            </div>
                          )}
                        </div>
                        
                        <div className="flex gap-2">
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