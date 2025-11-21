'use client'

import { useState, useEffect } from 'react'
import { PageHeader } from '@/components/page-header'
import { Button } from '@/components/ui/button'
import { Card, CardContent, CardHeader, CardTitle } from '@/components/ui/card'
import { Table, TableBody, TableCell, TableHead, TableHeader, TableRow } from '@/components/ui/table'
import { Badge } from '@/components/ui/badge'
import { useToast } from '@/hooks/use-toast'
import { AdminService, TaxRate } from '@/lib/services/admin-service'
import { Edit, History, Percent } from 'lucide-react'
import EditTaxRateDialog from '@/components/admin/edit-tax-rate-dialog'
import TaxRateHistoryDialog from '@/components/admin/tax-rate-history-dialog'
import Loading from '@/app/loading'

export default function TaxRatesPage() {
  const { toast } = useToast()
  const [taxRates, setTaxRates] = useState<TaxRate[]>([])
  const [loading, setLoading] = useState(true)
  const [editingRate, setEditingRate] = useState<TaxRate | null>(null)
  const [viewingHistory, setViewingHistory] = useState<string | null>(null)

  useEffect(() => {
    loadTaxRates()
  }, [])

  const loadTaxRates = async () => {
    try {
      setLoading(true)
      const data = await AdminService.getTaxRates()
      setTaxRates(data)
    } catch (error) {
      console.error('Error loading tax rates:', error)
      toast({
        variant: 'destructive',
        title: 'Error',
        description: 'Failed to load tax rates',
      })
    } finally {
      setLoading(false)
    }
  }

  const handleUpdateRate = async (type: string, rate: number) => {
    try {
      await AdminService.updateTaxRate(type, rate)
      toast({
        title: 'Success',
        description: 'Tax rate updated successfully',
      })
      setEditingRate(null)
      loadTaxRates()
    } catch (error) {
      console.error('Error updating tax rate:', error)
      toast({
        variant: 'destructive',
        title: 'Error',
        description: 'Failed to update tax rate',
      })
    }
  }

  if (loading) {
    return <Loading />
  }

  return (
    <div className="flex-1 flex flex-col">
      <PageHeader
        title="Tax Rates Configuration"
        breadcrumbs={[{ label: 'Admin' }, { label: 'Tax Rates' }]}
      />

      <div className="flex-1 p-6">
        <Card>
          <CardHeader>
            <CardTitle>Current Tax Rates</CardTitle>
          </CardHeader>
          <CardContent>
            <div className="border rounded-lg">
              <Table>
                <TableHeader>
                  <TableRow>
                    <TableHead>Tax Type</TableHead>
                    <TableHead>Current Rate</TableHead>
                    <TableHead>Effective From</TableHead>
                    <TableHead>Applicable To</TableHead>
                    <TableHead className="text-right">Actions</TableHead>
                  </TableRow>
                </TableHeader>
                <TableBody>
                  {taxRates.length === 0 ? (
                    <TableRow>
                      <TableCell colSpan={5} className="text-center py-8 text-muted-foreground">
                        No tax rates configured
                      </TableCell>
                    </TableRow>
                  ) : (
                    taxRates.map((rate) => (
                      <TableRow key={rate.id}>
                        <TableCell>
                          <div className="flex items-center gap-2">
                            <Percent className="w-4 h-4 text-primary" />
                            <span className="font-medium">{rate.type}</span>
                          </div>
                        </TableCell>
                        <TableCell>
                          <Badge className="bg-primary/10 text-primary">
                            {rate.rate}%
                          </Badge>
                        </TableCell>
                        <TableCell>
                          {new Date(rate.effectiveFrom).toLocaleDateString()}
                        </TableCell>
                        <TableCell className="text-muted-foreground">
                          {rate.applicableTo}
                        </TableCell>
                        <TableCell className="text-right">
                          <div className="flex items-center justify-end gap-2">
                            <Button
                              variant="ghost"
                              size="sm"
                              onClick={() => setEditingRate(rate)}
                            >
                              <Edit className="w-4 h-4" />
                            </Button>
                            <Button
                              variant="ghost"
                              size="sm"
                              onClick={() => setViewingHistory(rate.type)}
                            >
                              <History className="w-4 h-4" />
                            </Button>
                          </div>
                        </TableCell>
                      </TableRow>
                    ))
                  )}
                </TableBody>
              </Table>
            </div>

            <div className="mt-6 p-4 bg-amber-50 border border-amber-200 rounded-lg">
              <div className="flex items-start gap-3">
                <div className="flex-shrink-0">
                  <svg className="w-5 h-5 text-amber-600" fill="currentColor" viewBox="0 0 20 20">
                    <path fillRule="evenodd" d="M8.257 3.099c.765-1.36 2.722-1.36 3.486 0l5.58 9.92c.75 1.334-.213 2.98-1.742 2.98H4.42c-1.53 0-2.493-1.646-1.743-2.98l5.58-9.92zM11 13a1 1 0 11-2 0 1 1 0 012 0zm-1-8a1 1 0 00-1 1v3a1 1 0 002 0V6a1 1 0 00-1-1z" clipRule="evenodd" />
                  </svg>
                </div>
                <div>
                  <h4 className="font-medium text-amber-900">Important</h4>
                  <p className="text-sm text-amber-800 mt-1">
                    Changes to tax rates will affect all future tax calculations. Historical calculations will remain unchanged.
                  </p>
                </div>
              </div>
            </div>
          </CardContent>
        </Card>
      </div>

      {editingRate && (
        <EditTaxRateDialog
          open={!!editingRate}
          taxRate={editingRate}
          onClose={() => setEditingRate(null)}
          onUpdate={(rate) => handleUpdateRate(editingRate.type, rate)}
        />
      )}

      {viewingHistory && (
        <TaxRateHistoryDialog
          open={!!viewingHistory}
          taxType={viewingHistory}
          onClose={() => setViewingHistory(null)}
        />
      )}
    </div>
  )
}

