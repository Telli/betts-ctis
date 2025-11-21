'use client'

import { useState } from 'react'
import { Dialog, DialogContent, DialogHeader, DialogTitle, DialogFooter } from '@/components/ui/dialog'
import { Button } from '@/components/ui/button'
import { Input } from '@/components/ui/input'
import { Label } from '@/components/ui/label'
import { TaxRate } from '@/lib/services/admin-service'

interface EditTaxRateDialogProps {
  open: boolean
  taxRate: TaxRate
  onClose: () => void
  onUpdate: (rate: number) => void
}

export default function EditTaxRateDialog({ open, taxRate, onClose, onUpdate }: EditTaxRateDialogProps) {
  const [rate, setRate] = useState(taxRate.rate)

  const handleSubmit = (e: React.FormEvent) => {
    e.preventDefault()
    onUpdate(rate)
  }

  return (
    <Dialog open={open} onOpenChange={onClose}>
      <DialogContent>
        <DialogHeader>
          <DialogTitle>Edit {taxRate.type} Rate</DialogTitle>
        </DialogHeader>
        <form onSubmit={handleSubmit}>
          <div className="space-y-4 py-4">
            <div className="space-y-2">
              <Label htmlFor="tax-type">Tax Type</Label>
              <Input id="tax-type" value={taxRate.type} disabled />
            </div>
            <div className="space-y-2">
              <Label htmlFor="current-rate">Current Rate</Label>
              <Input id="current-rate" value={`${taxRate.rate}%`} disabled />
            </div>
            <div className="space-y-2">
              <Label htmlFor="new-rate">New Rate (%)</Label>
              <Input
                id="new-rate"
                type="number"
                step="0.01"
                min="0"
                max="100"
                value={rate}
                onChange={(e) => setRate(parseFloat(e.target.value))}
                required
              />
            </div>
            <div className="text-sm text-muted-foreground">
              <p>This change will take effect immediately and apply to all future calculations.</p>
            </div>
          </div>
          <DialogFooter>
            <Button type="button" variant="outline" onClick={onClose}>
              Cancel
            </Button>
            <Button type="submit">Update Rate</Button>
          </DialogFooter>
        </form>
      </DialogContent>
    </Dialog>
  )
}

