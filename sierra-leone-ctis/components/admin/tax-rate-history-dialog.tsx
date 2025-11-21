'use client'

import { useState, useEffect } from 'react'
import { Dialog, DialogContent, DialogHeader, DialogTitle } from '@/components/ui/dialog'
import { Table, TableBody, TableCell, TableHead, TableHeader, TableRow } from '@/components/ui/table'
import { AdminService, TaxRate } from '@/lib/services/admin-service'
import { Loader2 } from 'lucide-react'

interface TaxRateHistoryDialogProps {
  open: boolean
  taxType: string
  onClose: () => void
}

export default function TaxRateHistoryDialog({ open, taxType, onClose }: TaxRateHistoryDialogProps) {
  const [history, setHistory] = useState<TaxRate[]>([])
  const [loading, setLoading] = useState(true)

  useEffect(() => {
    if (open) {
      loadHistory()
    }
  }, [open, taxType])

  const loadHistory = async () => {
    try {
      setLoading(true)
      const data = await AdminService.getTaxRateHistory(taxType)
      setHistory(data)
    } catch (error) {
      console.error('Error loading tax rate history:', error)
    } finally {
      setLoading(false)
    }
  }

  return (
    <Dialog open={open} onOpenChange={onClose}>
      <DialogContent className="max-w-2xl">
        <DialogHeader>
          <DialogTitle>{taxType} Rate History</DialogTitle>
        </DialogHeader>
        <div className="max-h-96 overflow-auto">
          {loading ? (
            <div className="flex items-center justify-center py-8">
              <Loader2 className="w-8 h-8 animate-spin" />
            </div>
          ) : history.length === 0 ? (
            <div className="text-center py-8 text-muted-foreground">
              No history available
            </div>
          ) : (
            <Table>
              <TableHeader>
                <TableRow>
                  <TableHead>Rate</TableHead>
                  <TableHead>Effective From</TableHead>
                  <TableHead>Effective To</TableHead>
                </TableRow>
              </TableHeader>
              <TableBody>
                {history.map((entry, index) => (
                  <TableRow key={index}>
                    <TableCell className="font-medium">{entry.rate}%</TableCell>
                    <TableCell>{new Date(entry.effectiveFrom).toLocaleDateString()}</TableCell>
                    <TableCell>
                      {entry.effectiveTo ? new Date(entry.effectiveTo).toLocaleDateString() : 'Current'}
                    </TableCell>
                  </TableRow>
                ))}
              </TableBody>
            </Table>
          )}
        </div>
      </DialogContent>
    </Dialog>
  )
}

