"use client"

import { useState } from 'react'
import { Card, CardContent, CardDescription, CardHeader, CardTitle } from '@/components/ui/card'
import { Button } from '@/components/ui/button'
import { Badge } from '@/components/ui/badge'
import { Separator } from '@/components/ui/separator'
import { 
  Download, 
  FileText, 
  Mail, 
  Printer, 
  Share2, 
  CheckCircle,
  Building2,
  Smartphone,
  CreditCard,
  Calendar,
  Hash,
  Receipt as ReceiptIcon
} from 'lucide-react'
import { format } from 'date-fns'
import { useToast } from '@/components/ui/use-toast'
import { PaymentStatus, PaymentMethod } from './PaymentStatusTracker'

interface PaymentReceipt {
  id: string
  receiptNumber: string
  paymentReference: string
  transactionId?: string
  status: PaymentStatus
  amount: number
  fee: number
  totalAmount: number
  method: PaymentMethod
  currency: string
  paymentDate: Date
  processedDate?: Date
  description?: string
  taxType?: string
  taxYear?: number
  // Client information
  clientName: string
  clientNumber?: string
  clientEmail?: string
  // Payment details
  providerReference?: string
  providerName?: string
  accountNumber?: string // Last 4 digits for security
  // Firm information
  firmName: string
  firmAddress: string
  firmPhone: string
  firmEmail: string
  firmLicense?: string
}

interface PaymentReceiptGeneratorProps {
  receipt: PaymentReceipt
  onDownload?: (format: 'pdf' | 'png') => void
  onEmail?: (email: string) => void
  onPrint?: () => void
  showActions?: boolean
  variant?: 'full' | 'compact'
}

export default function PaymentReceiptGenerator({
  receipt,
  onDownload,
  onEmail,
  onPrint,
  showActions = true,
  variant = 'full'
}: PaymentReceiptGeneratorProps) {
  const { toast } = useToast()
  const [isGenerating, setIsGenerating] = useState(false)
  const [emailSent, setEmailSent] = useState(false)

  const handleDownload = async (format: 'pdf' | 'png' = 'pdf') => {
    setIsGenerating(true)
    try {
      // Simulate PDF/PNG generation
      await new Promise(resolve => setTimeout(resolve, 2000))
      
      if (onDownload) {
        onDownload(format)
      } else {
        // Mock download
        const link = document.createElement('a')
        link.href = '#'
        link.download = `receipt-${receipt.receiptNumber}.${format}`
        link.click()
      }

      toast({
        title: 'Receipt Downloaded',
        description: `Receipt saved as ${format.toUpperCase()} file`,
      })
    } catch (error) {
      toast({
        variant: 'destructive',
        title: 'Download Failed',
        description: 'Could not generate receipt. Please try again.',
      })
    } finally {
      setIsGenerating(false)
    }
  }

  const handleEmail = async () => {
    if (!receipt.clientEmail) {
      toast({
        variant: 'destructive',
        title: 'No Email Address',
        description: 'Client email address is not available',
      })
      return
    }

    setIsGenerating(true)
    try {
      await new Promise(resolve => setTimeout(resolve, 1500))
      
      if (onEmail) {
        onEmail(receipt.clientEmail)
      }

      setEmailSent(true)
      toast({
        title: 'Receipt Emailed',
        description: `Receipt sent to ${receipt.clientEmail}`,
      })
    } catch (error) {
      toast({
        variant: 'destructive',
        title: 'Email Failed',
        description: 'Could not send receipt via email. Please try again.',
      })
    } finally {
      setIsGenerating(false)
    }
  }

  const handlePrint = () => {
    if (onPrint) {
      onPrint()
    } else {
      window.print()
    }
  }

  const getMethodIcon = (method: PaymentMethod) => {
    switch (method) {
      case PaymentMethod.OrangeMoney:
      case PaymentMethod.AfricellMoney:
        return <Smartphone className="h-4 w-4" />
      case PaymentMethod.BankTransfer:
        return <Building2 className="h-4 w-4" />
      case PaymentMethod.PayPal:
      case PaymentMethod.Stripe:
        return <CreditCard className="h-4 w-4" />
      default:
        return <CreditCard className="h-4 w-4" />
    }
  }

  const getMethodName = (method: PaymentMethod) => {
    switch (method) {
      case PaymentMethod.OrangeMoney:
        return 'Orange Money'
      case PaymentMethod.AfricellMoney:
        return 'Africell Money'
      case PaymentMethod.BankTransfer:
        return 'Bank Transfer'
      case PaymentMethod.PayPal:
        return 'PayPal'
      case PaymentMethod.Stripe:
        return 'Credit/Debit Card'
      default:
        return String(method).replace('-', ' ').replace(/\b\w/g, l => l.toUpperCase())
    }
  }

  if (variant === 'compact') {
    return (
      <Card className="w-full">
        <CardContent className="p-4">
          <div className="flex items-center justify-between">
            <div className="flex items-center gap-3">
              <div className="w-10 h-10 bg-sierra-blue/10 rounded-lg flex items-center justify-center">
                <ReceiptIcon className="h-5 w-5 text-sierra-blue" />
              </div>
              <div>
                <div className="font-medium">Receipt #{receipt.receiptNumber}</div>
                <div className="text-sm text-muted-foreground">
                  Le {receipt.totalAmount.toLocaleString()} • {format(receipt.paymentDate, 'MMM d, yyyy')}
                </div>
              </div>
            </div>
            
            <div className="flex gap-2">
              <Button 
                variant="outline" 
                size="sm"
                onClick={() => handleDownload('pdf')}
                disabled={isGenerating}
              >
                <Download className="h-4 w-4" />
              </Button>
              {receipt.clientEmail && (
                <Button 
                  variant="outline" 
                  size="sm"
                  onClick={handleEmail}
                  disabled={isGenerating || emailSent}
                >
                  <Mail className="h-4 w-4" />
                </Button>
              )}
            </div>
          </div>
        </CardContent>
      </Card>
    )
  }

  return (
    <div className="w-full max-w-2xl mx-auto space-y-4">
      {/* Actions Bar */}
      {showActions && (
        <Card>
          <CardContent className="p-4">
            <div className="flex flex-wrap gap-2">
              <Button 
                onClick={() => handleDownload('pdf')}
                disabled={isGenerating}
                className="bg-sierra-blue hover:bg-sierra-blue/90"
              >
                <Download className="h-4 w-4 mr-2" />
                Download PDF
              </Button>
              
              <Button 
                variant="outline"
                onClick={() => handleDownload('png')}
                disabled={isGenerating}
              >
                <FileText className="h-4 w-4 mr-2" />
                Save as Image
              </Button>
              
              {receipt.clientEmail && (
                <Button 
                  variant="outline"
                  onClick={handleEmail}
                  disabled={isGenerating || emailSent}
                >
                  <Mail className="h-4 w-4 mr-2" />
                  {emailSent ? 'Email Sent' : 'Email Receipt'}
                </Button>
              )}
              
              <Button 
                variant="outline"
                onClick={handlePrint}
              >
                <Printer className="h-4 w-4 mr-2" />
                Print
              </Button>
              
              <Button variant="outline">
                <Share2 className="h-4 w-4 mr-2" />
                Share
              </Button>
            </div>
          </CardContent>
        </Card>
      )}

      {/* Receipt */}
      <Card className="print:shadow-none">
        <CardHeader className="text-center border-b bg-sierra-blue/5">
          <div className="flex items-center justify-center gap-2 mb-2">
            <div className="w-12 h-12 bg-sierra-blue rounded-lg flex items-center justify-center">
              <ReceiptIcon className="h-6 w-6 text-white" />
            </div>
          </div>
          <CardTitle className="text-sierra-blue">Payment Receipt</CardTitle>
          <CardDescription>Official tax payment confirmation</CardDescription>
        </CardHeader>

        <CardContent className="p-6 space-y-6">
          {/* Firm Information */}
          <div className="text-center">
            <h2 className="text-xl font-bold text-sierra-blue">{receipt.firmName}</h2>
            <div className="text-sm text-muted-foreground mt-1">
              <div>{receipt.firmAddress}</div>
              <div>Phone: {receipt.firmPhone} • Email: {receipt.firmEmail}</div>
              {receipt.firmLicense && (
                <div>License: {receipt.firmLicense}</div>
              )}
            </div>
          </div>

          <Separator />

          {/* Receipt Header */}
          <div className="grid grid-cols-2 gap-4">
            <div>
              <div className="text-sm text-muted-foreground">Receipt Number</div>
              <div className="font-mono font-medium">{receipt.receiptNumber}</div>
            </div>
            <div>
              <div className="text-sm text-muted-foreground">Payment Reference</div>
              <div className="font-mono font-medium">{receipt.paymentReference}</div>
            </div>
            <div>
              <div className="text-sm text-muted-foreground">Date Issued</div>
              <div className="font-medium">{format(receipt.paymentDate, 'MMMM d, yyyy')}</div>
            </div>
            <div>
              <div className="text-sm text-muted-foreground">Status</div>
              <Badge className="bg-green-100 text-green-800 border-green-200">
                <CheckCircle className="h-3 w-3 mr-1" />
                {receipt.status === PaymentStatus.Confirmed ? 'Confirmed' : receipt.status}
              </Badge>
            </div>
          </div>

          <Separator />

          {/* Client Information */}
          <div>
            <h3 className="font-semibold mb-3 flex items-center gap-2">
              <Building2 className="h-4 w-4" />
              Client Information
            </h3>
            <div className="grid grid-cols-2 gap-4">
              <div>
                <div className="text-sm text-muted-foreground">Client Name</div>
                <div className="font-medium">{receipt.clientName}</div>
              </div>
              {receipt.clientNumber && (
                <div>
                  <div className="text-sm text-muted-foreground">Client Number</div>
                  <div className="font-medium">{receipt.clientNumber}</div>
                </div>
              )}
            </div>
          </div>

          <Separator />

          {/* Payment Details */}
          <div>
            <h3 className="font-semibold mb-3 flex items-center gap-2">
              {getMethodIcon(receipt.method)}
              Payment Details
            </h3>
            <div className="grid grid-cols-2 gap-4">
              <div>
                <div className="text-sm text-muted-foreground">Payment Method</div>
                <div className="font-medium">{getMethodName(receipt.method)}</div>
              </div>
              <div>
                <div className="text-sm text-muted-foreground">Currency</div>
                <div className="font-medium">{receipt.currency}</div>
              </div>
              {receipt.transactionId && (
                <div>
                  <div className="text-sm text-muted-foreground">Transaction ID</div>
                  <div className="font-mono text-sm">{receipt.transactionId}</div>
                </div>
              )}
              {receipt.providerReference && (
                <div>
                  <div className="text-sm text-muted-foreground">Provider Reference</div>
                  <div className="font-mono text-sm">{receipt.providerReference}</div>
                </div>
              )}
              {receipt.processedDate && (
                <div>
                  <div className="text-sm text-muted-foreground">Processed Date</div>
                  <div className="font-medium">{format(receipt.processedDate, 'MMMM d, yyyy HH:mm')}</div>
                </div>
              )}
            </div>
          </div>

          {/* Tax Information */}
          {(receipt.taxType || receipt.taxYear) && (
            <>
              <Separator />
              <div>
                <h3 className="font-semibold mb-3 flex items-center gap-2">
                  <FileText className="h-4 w-4" />
                  Tax Information
                </h3>
                <div className="grid grid-cols-2 gap-4">
                  {receipt.taxType && (
                    <div>
                      <div className="text-sm text-muted-foreground">Tax Type</div>
                      <div className="font-medium">{receipt.taxType}</div>
                    </div>
                  )}
                  {receipt.taxYear && (
                    <div>
                      <div className="text-sm text-muted-foreground">Tax Year</div>
                      <div className="font-medium">{receipt.taxYear}</div>
                    </div>
                  )}
                </div>
                {receipt.description && (
                  <div className="mt-2">
                    <div className="text-sm text-muted-foreground">Description</div>
                    <div className="font-medium">{receipt.description}</div>
                  </div>
                )}
              </div>
            </>
          )}

          <Separator />

          {/* Amount Breakdown */}
          <div>
            <h3 className="font-semibold mb-3">Amount Breakdown</h3>
            <div className="space-y-2">
              <div className="flex justify-between">
                <span>Payment Amount:</span>
                <span className="font-mono">Le {receipt.amount.toLocaleString()}</span>
              </div>
              <div className="flex justify-between">
                <span>Transaction Fee:</span>
                <span className="font-mono">Le {receipt.fee.toLocaleString()}</span>
              </div>
              <Separator />
              <div className="flex justify-between text-lg font-bold">
                <span>Total Amount:</span>
                <span className="font-mono">Le {receipt.totalAmount.toLocaleString()}</span>
              </div>
            </div>
          </div>

          <Separator />

          {/* Footer */}
          <div className="text-center text-sm text-muted-foreground space-y-2">
            <p>This is an official receipt for tax payment services</p>
            <p>Generated on {format(new Date(), 'MMMM d, yyyy \'at\' HH:mm')}</p>
            <div className="flex items-center justify-center gap-1 text-xs">
              <Hash className="h-3 w-3" />
              <span className="font-mono">Receipt ID: {receipt.id}</span>
            </div>
          </div>
        </CardContent>
      </Card>
    </div>
  )
}