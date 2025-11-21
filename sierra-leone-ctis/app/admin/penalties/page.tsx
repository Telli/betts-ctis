'use client'

import { useState, useEffect } from 'react'
import { PageHeader } from '@/components/page-header'
import { Button } from '@/components/ui/button'
import { Card, CardContent, CardHeader, CardTitle } from '@/components/ui/card'
import { Table, TableBody, TableCell, TableHead, TableHeader, TableRow } from '@/components/ui/table'
import { useToast } from '@/hooks/use-toast'
import { AdminService, PenaltyRule } from '@/lib/services/admin-service'
import { Plus, Edit, Trash, Upload } from 'lucide-react'
import PenaltyDialog from '@/components/admin/penalty-dialog'
import ImportExciseDialog from '@/components/admin/import-excise-dialog'
import Loading from '@/app/loading'

export default function PenaltiesPage() {
  const { toast } = useToast()
  const [penalties, setPenalties] = useState<PenaltyRule[]>([])
  const [loading, setLoading] = useState(true)
  const [editingPenalty, setEditingPenalty] = useState<PenaltyRule | null>(null)
  const [showCreateDialog, setShowCreateDialog] = useState(false)
  const [showImportDialog, setShowImportDialog] = useState(false)

  useEffect(() => {
    loadPenalties()
  }, [])

  const loadPenalties = async () => {
    try {
      setLoading(true)
      const data = await AdminService.getPenalties()
      setPenalties(data)
    } catch (error) {
      console.error('Error loading penalties:', error)
      toast({
        variant: 'destructive',
        title: 'Error',
        description: 'Failed to load penalties',
      })
    } finally {
      setLoading(false)
    }
  }

  const handleCreatePenalty = async (data: Omit<PenaltyRule, 'id'>) => {
    try {
      await AdminService.createPenalty(data)
      toast({
        title: 'Success',
        description: 'Penalty rule created successfully',
      })
      setShowCreateDialog(false)
      loadPenalties()
    } catch (error) {
      console.error('Error creating penalty:', error)
      toast({
        variant: 'destructive',
        title: 'Error',
        description: 'Failed to create penalty rule',
      })
    }
  }

  const handleUpdatePenalty = async (id: number, data: Partial<PenaltyRule>) => {
    try {
      await AdminService.updatePenalty(id, data)
      toast({
        title: 'Success',
        description: 'Penalty rule updated successfully',
      })
      setEditingPenalty(null)
      loadPenalties()
    } catch (error) {
      console.error('Error updating penalty:', error)
      toast({
        variant: 'destructive',
        title: 'Error',
        description: 'Failed to update penalty rule',
      })
    }
  }

  const handleDeletePenalty = async (id: number) => {
    if (!confirm('Are you sure you want to delete this penalty rule?')) return

    try {
      await AdminService.deletePenalty(id)
      toast({
        title: 'Success',
        description: 'Penalty rule deleted successfully',
      })
      loadPenalties()
    } catch (error) {
      console.error('Error deleting penalty:', error)
      toast({
        variant: 'destructive',
        title: 'Error',
        description: 'Failed to delete penalty rule',
      })
    }
  }

  const handleImportExcise = async (file: File) => {
    try {
      await AdminService.importExciseTable(file)
      toast({
        title: 'Success',
        description: 'Excise table imported successfully',
      })
      setShowImportDialog(false)
      loadPenalties()
    } catch (error) {
      console.error('Error importing excise table:', error)
      toast({
        variant: 'destructive',
        title: 'Error',
        description: 'Failed to import excise table',
      })
    }
  }

  if (loading) {
    return <Loading />
  }

  return (
    <div className="flex-1 flex flex-col">
      <PageHeader
        title="Penalty Matrix Management"
        breadcrumbs={[{ label: 'Admin' }, { label: 'Penalties' }]}
        actions={
          <div className="flex gap-2">
            <Button variant="outline" onClick={() => setShowImportDialog(true)}>
              <Upload className="w-4 h-4 mr-2" />
              Import Excise Table
            </Button>
            <Button onClick={() => setShowCreateDialog(true)}>
              <Plus className="w-4 h-4 mr-2" />
              Add Penalty Rule
            </Button>
          </div>
        }
      />

      <div className="flex-1 p-6">
        <Card>
          <CardHeader>
            <CardTitle>Penalty Rules</CardTitle>
          </CardHeader>
          <CardContent>
            <div className="border rounded-lg">
              <Table>
                <TableHeader>
                  <TableRow>
                    <TableHead>Tax Type</TableHead>
                    <TableHead>Condition</TableHead>
                    <TableHead>Amount (SLE)</TableHead>
                    <TableHead>Percentage</TableHead>
                    <TableHead>Description</TableHead>
                    <TableHead className="text-right">Actions</TableHead>
                  </TableRow>
                </TableHeader>
                <TableBody>
                  {penalties.length === 0 ? (
                    <TableRow>
                      <TableCell colSpan={6} className="text-center py-8 text-muted-foreground">
                        No penalty rules configured
                      </TableCell>
                    </TableRow>
                  ) : (
                    penalties.map((penalty) => (
                      <TableRow key={penalty.id}>
                        <TableCell className="font-medium">{penalty.taxType}</TableCell>
                        <TableCell className="text-muted-foreground">{penalty.condition}</TableCell>
                        <TableCell className="font-mono">
                          {penalty.amount.toLocaleString()}
                        </TableCell>
                        <TableCell>
                          {penalty.percentage ? `${penalty.percentage}%` : '-'}
                        </TableCell>
                        <TableCell className="max-w-xs truncate">{penalty.description}</TableCell>
                        <TableCell className="text-right">
                          <div className="flex items-center justify-end gap-2">
                            <Button
                              variant="ghost"
                              size="sm"
                              onClick={() => setEditingPenalty(penalty)}
                            >
                              <Edit className="w-4 h-4" />
                            </Button>
                            <Button
                              variant="ghost"
                              size="sm"
                              onClick={() => handleDeletePenalty(penalty.id)}
                            >
                              <Trash className="w-4 h-4 text-red-500" />
                            </Button>
                          </div>
                        </TableCell>
                      </TableRow>
                    ))
                  )}
                </TableBody>
              </Table>
            </div>
          </CardContent>
        </Card>
      </div>

      {showCreateDialog && (
        <PenaltyDialog
          open={showCreateDialog}
          onClose={() => setShowCreateDialog(false)}
          onCreate={handleCreatePenalty}
        />
      )}

      {editingPenalty && (
        <PenaltyDialog
          open={!!editingPenalty}
          penalty={editingPenalty}
          onClose={() => setEditingPenalty(null)}
          onUpdate={(data) => handleUpdatePenalty(editingPenalty.id, data)}
        />
      )}

      {showImportDialog && (
        <ImportExciseDialog
          open={showImportDialog}
          onClose={() => setShowImportDialog(false)}
          onImport={handleImportExcise}
        />
      )}
    </div>
  )
}

