'use client'

import { useState, useEffect } from 'react'
import { Dialog, DialogContent, DialogHeader, DialogTitle, DialogFooter } from '@/components/ui/dialog'
import { Button } from '@/components/ui/button'
import { Input } from '@/components/ui/input'
import { Label } from '@/components/ui/label'
import { Textarea } from '@/components/ui/textarea'
import { Select, SelectContent, SelectItem, SelectTrigger, SelectValue } from '@/components/ui/select'
import { PenaltyRule } from '@/lib/services/admin-service'

interface PenaltyDialogProps {
  open: boolean
  penalty?: PenaltyRule
  onClose: () => void
  onCreate?: (data: Omit<PenaltyRule, 'id'>) => void
  onUpdate?: (data: Partial<PenaltyRule>) => void
}

export default function PenaltyDialog({ open, penalty, onClose, onCreate, onUpdate }: PenaltyDialogProps) {
  const [formData, setFormData] = useState({
    taxType: penalty?.taxType || undefined,
    condition: penalty?.condition || undefined,
    amount: penalty?.amount || 0,
    percentage: penalty?.percentage || undefined,
    description: penalty?.description || '',
  })

  useEffect(() => {
    if (penalty) {
      setFormData({
        taxType: penalty.taxType,
        condition: penalty.condition,
        amount: penalty.amount,
        percentage: penalty.percentage,
        description: penalty.description,
      })
    } else {
      // Reset to undefined when creating new penalty
      setFormData({
        taxType: undefined,
        condition: undefined,
        amount: 0,
        percentage: undefined,
        description: '',
      })
    }
  }, [penalty])

  const handleSubmit = (e: React.FormEvent) => {
    e.preventDefault()
    if (penalty && onUpdate) {
      onUpdate(formData)
    } else if (onCreate) {
      onCreate(formData)
    }
  }

  return (
    <Dialog open={open} onOpenChange={onClose}>
      <DialogContent className="max-w-2xl">
        <DialogHeader>
          <DialogTitle>{penalty ? 'Edit Penalty Rule' : 'Create Penalty Rule'}</DialogTitle>
        </DialogHeader>
        <form onSubmit={handleSubmit}>
          <div className="space-y-4 py-4">
            <div className="grid grid-cols-2 gap-4">
              <div className="space-y-2">
                <Label htmlFor="tax-type">Tax Type</Label>
                <Select
                  value={formData.taxType || undefined}
                  onValueChange={(value) => setFormData({ ...formData, taxType: value })}
                >
                  <SelectTrigger>
                    <SelectValue placeholder="Select tax type" />
                  </SelectTrigger>
                  <SelectContent>
                    <SelectItem value="GST">GST</SelectItem>
                    <SelectItem value="CIT">Corporate Income Tax</SelectItem>
                    <SelectItem value="PIT">Personal Income Tax</SelectItem>
                    <SelectItem value="WHT">Withholding Tax</SelectItem>
                    <SelectItem value="PAYE">PAYE</SelectItem>
                    <SelectItem value="ExciseDuty">Excise Duty</SelectItem>
                  </SelectContent>
                </Select>
              </div>
              <div className="space-y-2">
                <Label htmlFor="condition">Condition</Label>
                <Select
                  value={formData.condition || undefined}
                  onValueChange={(value) => setFormData({ ...formData, condition: value })}
                >
                  <SelectTrigger>
                    <SelectValue placeholder="Select condition" />
                  </SelectTrigger>
                  <SelectContent>
                    <SelectItem value="Late Filing">Late Filing</SelectItem>
                    <SelectItem value="Late Payment">Late Payment</SelectItem>
                    <SelectItem value="Underreporting">Underreporting</SelectItem>
                    <SelectItem value="Non-Filing">Non-Filing</SelectItem>
                  </SelectContent>
                </Select>
              </div>
            </div>
            <div className="grid grid-cols-2 gap-4">
              <div className="space-y-2">
                <Label htmlFor="amount">Fixed Amount (SLE)</Label>
                <Input
                  id="amount"
                  type="number"
                  step="0.01"
                  min="0"
                  value={formData.amount}
                  onChange={(e) => setFormData({ ...formData, amount: parseFloat(e.target.value) || 0 })}
                  required
                />
              </div>
              <div className="space-y-2">
                <Label htmlFor="percentage">Percentage (Optional)</Label>
                <Input
                  id="percentage"
                  type="number"
                  step="0.01"
                  min="0"
                  max="100"
                  value={formData.percentage || ''}
                  onChange={(e) =>
                    setFormData({ ...formData, percentage: e.target.value ? parseFloat(e.target.value) : undefined })
                  }
                />
              </div>
            </div>
            <div className="space-y-2">
              <Label htmlFor="description">Description</Label>
              <Textarea
                id="description"
                value={formData.description}
                onChange={(e) => setFormData({ ...formData, description: e.target.value })}
                placeholder="Enter description..."
                rows={3}
                required
              />
            </div>
          </div>
          <DialogFooter>
            <Button type="button" variant="outline" onClick={onClose}>
              Cancel
            </Button>
            <Button type="submit">{penalty ? 'Update' : 'Create'}</Button>
          </DialogFooter>
        </form>
      </DialogContent>
    </Dialog>
  )
}

