'use client'

import { useState, useEffect } from 'react'
import { PageHeader } from '@/components/page-header'
import { Button } from '@/components/ui/button'
import { Card, CardContent, CardHeader, CardTitle } from '@/components/ui/card'
import { Table, TableBody, TableCell, TableHead, TableHeader, TableRow } from '@/components/ui/table'
import { Input } from '@/components/ui/input'
import { Label } from '@/components/ui/label'
import { useToast } from '@/hooks/use-toast'
import { AdminService, AuditLogEntry } from '@/lib/services/admin-service'
import { Download, Search } from 'lucide-react'
import Loading from '@/app/loading'

export default function AuditLogsPage() {
  const { toast } = useToast()
  const [logs, setLogs] = useState<AuditLogEntry[]>([])
  const [loading, setLoading] = useState(true)
  const [filters, setFilters] = useState({
    fromDate: '',
    toDate: '',
    actor: '',
    action: '',
  })

  useEffect(() => {
    loadLogs()
  }, [])

  const loadLogs = async () => {
    try {
      setLoading(true)
      const data = await AdminService.getAuditLogs(filters)
      setLogs(data)
    } catch (error) {
      console.error('Error loading audit logs:', error)
      toast({
        variant: 'destructive',
        title: 'Error',
        description: 'Failed to load audit logs',
      })
    } finally {
      setLoading(false)
    }
  }

  const handleSearch = () => {
    loadLogs()
  }

  const handleExport = async () => {
    try {
      await AdminService.exportAuditLogs()
      toast({
        title: 'Success',
        description: 'Audit logs exported successfully',
      })
    } catch (error) {
      console.error('Error exporting audit logs:', error)
      toast({
        variant: 'destructive',
        title: 'Error',
        description: 'Failed to export audit logs',
      })
    }
  }

  if (loading) {
    return <Loading />
  }

  return (
    <div className="flex-1 flex flex-col">
      <PageHeader
        title="Audit Logs"
        breadcrumbs={[{ label: 'Admin' }, { label: 'Audit Logs' }]}
        actions={
          <Button onClick={handleExport}>
            <Download className="w-4 h-4 mr-2" />
            Export to CSV
          </Button>
        }
      />

      <div className="flex-1 p-6 space-y-6">
        <Card>
          <CardHeader>
            <CardTitle>Search Filters</CardTitle>
          </CardHeader>
          <CardContent>
            <div className="grid grid-cols-1 md:grid-cols-4 gap-4">
              <div className="space-y-2">
                <Label htmlFor="from-date">From Date</Label>
                <Input
                  id="from-date"
                  type="date"
                  value={filters.fromDate}
                  onChange={(e) => setFilters({ ...filters, fromDate: e.target.value })}
                />
              </div>
              <div className="space-y-2">
                <Label htmlFor="to-date">To Date</Label>
                <Input
                  id="to-date"
                  type="date"
                  value={filters.toDate}
                  onChange={(e) => setFilters({ ...filters, toDate: e.target.value })}
                />
              </div>
              <div className="space-y-2">
                <Label htmlFor="actor">Actor</Label>
                <Input
                  id="actor"
                  placeholder="Search by user..."
                  value={filters.actor}
                  onChange={(e) => setFilters({ ...filters, actor: e.target.value })}
                />
              </div>
              <div className="space-y-2">
                <Label htmlFor="action">Action</Label>
                <Input
                  id="action"
                  placeholder="Search by action..."
                  value={filters.action}
                  onChange={(e) => setFilters({ ...filters, action: e.target.value })}
                />
              </div>
            </div>
            <div className="mt-4">
              <Button onClick={handleSearch}>
                <Search className="w-4 h-4 mr-2" />
                Search
              </Button>
            </div>
          </CardContent>
        </Card>

        <Card>
          <CardHeader>
            <CardTitle>Audit Log Entries</CardTitle>
          </CardHeader>
          <CardContent>
            <div className="border rounded-lg">
              <Table>
                <TableHeader>
                  <TableRow>
                    <TableHead>Timestamp</TableHead>
                    <TableHead>Actor</TableHead>
                    <TableHead>Role</TableHead>
                    <TableHead>Action</TableHead>
                    <TableHead>IP Address</TableHead>
                    <TableHead>Details</TableHead>
                  </TableRow>
                </TableHeader>
                <TableBody>
                  {logs.length === 0 ? (
                    <TableRow>
                      <TableCell colSpan={6} className="text-center py-8 text-muted-foreground">
                        No audit logs found
                      </TableCell>
                    </TableRow>
                  ) : (
                    logs.map((log) => (
                      <TableRow key={log.id}>
                        <TableCell className="font-mono text-sm">
                          {new Date(log.timestamp).toLocaleString()}
                        </TableCell>
                        <TableCell className="font-medium">{log.actor}</TableCell>
                        <TableCell>
                          <span className="px-2 py-1 bg-primary/10 text-primary text-xs rounded">
                            {log.role}
                          </span>
                        </TableCell>
                        <TableCell className="font-medium">{log.action}</TableCell>
                        <TableCell className="font-mono text-sm">{log.ipAddress}</TableCell>
                        <TableCell className="max-w-md truncate text-muted-foreground">
                          {log.details}
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
    </div>
  )
}

