'use client'

import { useState } from 'react'
import { Dialog, DialogContent, DialogHeader, DialogTitle, DialogFooter } from '@/components/ui/dialog'
import { Button } from '@/components/ui/button'
import { Label } from '@/components/ui/label'
import { Input } from '@/components/ui/input'
import { Upload, Loader2 } from 'lucide-react'
import { useToast } from '@/hooks/use-toast'
import { TaxFilingService } from '@/lib/services/tax-filing-service'

interface ScheduleImportDialogProps {
  open: boolean
  onClose: () => void
  filingId: number
  onImportComplete: () => void
}

export default function ScheduleImportDialog({
  open,
  onClose,
  filingId,
  onImportComplete,
}: ScheduleImportDialogProps) {
  const { toast } = useToast()
  const [file, setFile] = useState<File | null>(null)
  const [importing, setImporting] = useState(false)

  const handleFileChange = (e: React.ChangeEvent<HTMLInputElement>) => {
    if (e.target.files && e.target.files[0]) {
      setFile(e.target.files[0])
    }
  }

  const handleImport = async () => {
    if (!file) {
      toast({
        variant: 'destructive',
        title: 'Error',
        description: 'Please select a file to import',
      })
      return
    }

    try {
      setImporting(true)
      await TaxFilingService.importSchedules(filingId, file)
      toast({
        title: 'Success',
        description: 'Schedules imported successfully',
      })
      onImportComplete()
      setFile(null)
    } catch (error) {
      console.error('Error importing schedules:', error)
      toast({
        variant: 'destructive',
        title: 'Error',
        description: 'Failed to import schedules. Please check the file format.',
      })
    } finally {
      setImporting(false)
    }
  }

  return (
    <Dialog open={open} onOpenChange={onClose}>
      <DialogContent>
        <DialogHeader>
          <DialogTitle>Import Schedules from CSV/Excel</DialogTitle>
        </DialogHeader>
        <div className="space-y-4 py-4">
          <div className="space-y-2">
            <Label>File</Label>
            <Input type="file" accept=".csv,.xlsx,.xls" onChange={handleFileChange} />
            <p className="text-sm text-muted-foreground">
              Upload a CSV or Excel file with columns: Description, Amount, Taxable Amount
            </p>
          </div>
          {file && (
            <div className="text-sm text-muted-foreground">
              Selected file: <span className="font-medium">{file.name}</span>
            </div>
          )}
        </div>
        <DialogFooter>
          <Button variant="outline" onClick={onClose} disabled={importing}>
            Cancel
          </Button>
          <Button onClick={handleImport} disabled={!file || importing}>
            {importing ? (
              <>
                <Loader2 className="w-4 h-4 mr-2 animate-spin" />
                Importing...
              </>
            ) : (
              <>
                <Upload className="w-4 h-4 mr-2" />
                Import
              </>
            )}
          </Button>
        </DialogFooter>
      </DialogContent>
    </Dialog>
  )
}

