'use client'

import { useState } from 'react'
import { Dialog, DialogContent, DialogHeader, DialogTitle, DialogFooter } from '@/components/ui/dialog'
import { Button } from '@/components/ui/button'
import { Input } from '@/components/ui/input'
import { Label } from '@/components/ui/label'
import { Upload, Loader2 } from 'lucide-react'

interface ImportExciseDialogProps {
  open: boolean
  onClose: () => void
  onImport: (file: File) => Promise<void>
}

export default function ImportExciseDialog({ open, onClose, onImport }: ImportExciseDialogProps) {
  const [file, setFile] = useState<File | null>(null)
  const [importing, setImporting] = useState(false)

  const handleFileChange = (e: React.ChangeEvent<HTMLInputElement>) => {
    if (e.target.files && e.target.files[0]) {
      setFile(e.target.files[0])
    }
  }

  const handleSubmit = async (e: React.FormEvent) => {
    e.preventDefault()
    if (!file) return

    try {
      setImporting(true)
      await onImport(file)
      setFile(null)
    } finally {
      setImporting(false)
    }
  }

  return (
    <Dialog open={open} onOpenChange={onClose}>
      <DialogContent>
        <DialogHeader>
          <DialogTitle>Import Excise Duty Table</DialogTitle>
        </DialogHeader>
        <form onSubmit={handleSubmit}>
          <div className="space-y-4 py-4">
            <div className="space-y-2">
              <Label htmlFor="excise-file">CSV/Excel File</Label>
              <Input
                id="excise-file"
                type="file"
                accept=".csv,.xlsx,.xls"
                onChange={handleFileChange}
                disabled={importing}
              />
              <p className="text-sm text-muted-foreground">
                Upload a CSV or Excel file with columns: Tax Type, Condition, Amount, Percentage, Description
              </p>
            </div>
            {file && (
              <div className="text-sm text-muted-foreground">
                Selected file: <span className="font-medium">{file.name}</span>
              </div>
            )}
          </div>
          <DialogFooter>
            <Button type="button" variant="outline" onClick={onClose} disabled={importing}>
              Cancel
            </Button>
            <Button type="submit" disabled={!file || importing}>
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
        </form>
      </DialogContent>
    </Dialog>
  )
}

