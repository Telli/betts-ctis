'use client'

import { useState, useEffect } from 'react'
import { Card, CardContent, CardHeader, CardTitle } from '@/components/ui/card'
import { Loader2 } from 'lucide-react'
import { useToast } from '@/hooks/use-toast'
import { TaxFilingService } from '@/lib/services/tax-filing-service'

interface HistoryEntry {
  id: number
  timestamp: string
  user: string
  action: string
  changes: string
}

interface FilingHistoryTabProps {
  filingId: number
}

export default function FilingHistoryTab({ filingId }: FilingHistoryTabProps) {
  const { toast } = useToast()
  const [history, setHistory] = useState<HistoryEntry[]>([])
  const [loading, setLoading] = useState(true)

  useEffect(() => {
    loadHistory()
  }, [filingId])

  const loadHistory = async () => {
    try {
      setLoading(true)
      const data = await TaxFilingService.getFilingHistory(filingId)
      setHistory(data)
    } catch (error) {
      console.error('Error loading history:', error)
      toast({
        variant: 'destructive',
        title: 'Error',
        description: 'Failed to load history',
      })
    } finally {
      setLoading(false)
    }
  }

  return (
    <Card>
      <CardHeader>
        <CardTitle>Audit Trail</CardTitle>
      </CardHeader>
      <CardContent>
        {loading ? (
          <div className="flex items-center justify-center py-12">
            <Loader2 className="w-8 h-8 animate-spin" />
          </div>
        ) : history.length === 0 ? (
          <div className="text-center py-12 text-muted-foreground">No history available</div>
        ) : (
          <div className="space-y-4">
            {history.map((entry) => (
              <div key={entry.id} className="flex gap-4 pb-4 border-b last:border-b-0">
                <div className="w-2 h-2 bg-primary rounded-full mt-2" />
                <div className="flex-1">
                  <div className="flex items-start justify-between">
                    <div>
                      <p className="font-medium">{entry.action}</p>
                      <p className="text-sm text-muted-foreground">{entry.changes}</p>
                    </div>
                    <time className="text-sm text-muted-foreground">{entry.timestamp}</time>
                  </div>
                  <p className="text-sm text-muted-foreground mt-1">by {entry.user}</p>
                </div>
              </div>
            ))}
          </div>
        )}
      </CardContent>
    </Card>
  )
}

