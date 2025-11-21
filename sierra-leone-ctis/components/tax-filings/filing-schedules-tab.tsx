'use client'

import { useState, useEffect } from 'react'
import { Card, CardContent, CardHeader, CardTitle } from '@/components/ui/card'
import { Button } from '@/components/ui/button'
import { Table, TableBody, TableCell, TableHead, TableHeader, TableRow } from '@/components/ui/table'
import { Alert, AlertDescription } from '@/components/ui/alert'
import { Upload, AlertCircle, Plus, Trash } from 'lucide-react'
import { useToast } from '@/hooks/use-toast'
import { TaxFilingService } from '@/lib/services/tax-filing-service'
import ScheduleImportDialog from './schedule-import-dialog'

interface ScheduleRow {
  id?: number
  description: string
  amount: number
  taxable: number
}

interface FilingSchedulesTabProps {
  filingId: number
}

export default function FilingSchedulesTab({ filingId }: FilingSchedulesTabProps) {
  const { toast } = useToast()
  const [schedules, setSchedules] = useState<ScheduleRow[]>([])
  const [loading, setLoading] = useState(true)
  const [showImportDialog, setShowImportDialog] = useState(false)

  useEffect(() => {
    loadSchedules()
  }, [filingId])

  const loadSchedules = async () => {
    try {
      setLoading(true)
      const data = await TaxFilingService.getSchedules(filingId)
      setSchedules(data)
    } catch (error) {
      console.error('Error loading schedules:', error)
      toast({
        variant: 'destructive',
        title: 'Error',
        description: 'Failed to load schedules',
      })
    } finally {
      setLoading(false)
    }
  }

  const handleAddRow = () => {
    setSchedules([...schedules, { description: '', amount: 0, taxable: 0 }])
  }

  const handleDeleteRow = (index: number) => {
    setSchedules(schedules.filter((_, i) => i !== index))
  }

  const handleFieldChange = (index: number, field: keyof ScheduleRow, value: any) => {
    const updated = [...schedules]
    updated[index] = { ...updated[index], [field]: value }
    setSchedules(updated)
  }

  const handleSave = async () => {
    try {
      await TaxFilingService.saveSchedules(filingId, schedules)
      toast({
        title: 'Success',
        description: 'Schedules saved successfully',
      })
      loadSchedules()
    } catch (error) {
      console.error('Error saving schedules:', error)
      toast({
        variant: 'destructive',
        title: 'Error',
        description: 'Failed to save schedules',
      })
    }
  }

  const handleImportComplete = () => {
    setShowImportDialog(false)
    loadSchedules()
  }

  return (
    <Card>
      <CardHeader>
        <div className="flex items-center justify-between">
          <CardTitle>Import Schedule Data</CardTitle>
          <div className="flex gap-2">
            <Button variant="outline" onClick={handleAddRow}>
              <Plus className="w-4 h-4 mr-2" />
              Add Row
            </Button>
            <Button onClick={() => setShowImportDialog(true)}>
              <Upload className="w-4 h-4 mr-2" />
              Import CSV/Excel
            </Button>
          </div>
        </div>
      </CardHeader>
      <CardContent>
        <Alert className="mb-6">
          <AlertCircle className="w-4 h-4" />
          <AlertDescription>
            Upload a CSV or Excel file with columns: Description, Amount, Taxable Amount
          </AlertDescription>
        </Alert>

        <div className="border border-border rounded-lg overflow-auto">
          <Table>
            <TableHeader>
              <TableRow>
                <TableHead>Description</TableHead>
                <TableHead className="text-right">Amount (SLE)</TableHead>
                <TableHead className="text-right">Taxable (SLE)</TableHead>
                <TableHead className="w-[100px]">Actions</TableHead>
              </TableRow>
            </TableHeader>
            <TableBody>
              {schedules.length === 0 ? (
                <TableRow>
                  <TableCell colSpan={4} className="text-center text-muted-foreground">
                    No schedule data. Add rows or import from CSV/Excel.
                  </TableCell>
                </TableRow>
              ) : (
                schedules.map((row, index) => (
                  <TableRow key={index}>
                    <TableCell>
                      <input
                        type="text"
                        value={row.description}
                        onChange={(e) => handleFieldChange(index, 'description', e.target.value)}
                        className="w-full px-2 py-1 border rounded"
                      />
                    </TableCell>
                    <TableCell className="text-right font-mono">
                      <input
                        type="number"
                        value={row.amount}
                        onChange={(e) => handleFieldChange(index, 'amount', parseFloat(e.target.value) || 0)}
                        className="w-full px-2 py-1 border rounded text-right"
                      />
                    </TableCell>
                    <TableCell className="text-right font-mono">
                      <input
                        type="number"
                        value={row.taxable}
                        onChange={(e) => handleFieldChange(index, 'taxable', parseFloat(e.target.value) || 0)}
                        className="w-full px-2 py-1 border rounded text-right"
                      />
                    </TableCell>
                    <TableCell>
                      <Button variant="ghost" size="sm" onClick={() => handleDeleteRow(index)}>
                        <Trash className="w-4 h-4 text-red-500" />
                      </Button>
                    </TableCell>
                  </TableRow>
                ))
              )}
            </TableBody>
          </Table>
        </div>

        {schedules.length > 0 && (
          <div className="mt-4 flex justify-end">
            <Button onClick={handleSave}>Save Schedules</Button>
          </div>
        )}
      </CardContent>

      <ScheduleImportDialog
        open={showImportDialog}
        onClose={() => setShowImportDialog(false)}
        filingId={filingId}
        onImportComplete={handleImportComplete}
      />
    </Card>
  )
}

