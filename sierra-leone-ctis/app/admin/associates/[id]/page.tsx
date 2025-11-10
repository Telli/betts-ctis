'use client'

import { useEffect, useMemo, useState } from 'react'
import { useParams, useRouter } from 'next/navigation'
import { Button } from '@/components/ui/button'
import { Card, CardContent, CardHeader, CardTitle, CardDescription } from '@/components/ui/card'
import { Badge } from '@/components/ui/badge'
import { Table, TableBody, TableCell, TableHead, TableHeader, TableRow } from '@/components/ui/table'
import { Separator } from '@/components/ui/separator'
import { useToast } from '@/hooks/use-toast'
import { 
  AssociateDashboardService, 
  type AssociateDashboardData 
} from '@/lib/services'
import { ArrowLeft, Users, AlertTriangle, Clock, Activity, CheckCircle } from 'lucide-react'

export default function AssociateDetailsPage() {
  const params = useParams<{ id: string }>()
  const associateId = useMemo(() => (Array.isArray(params?.id) ? params.id[0] : params?.id), [params])
  const router = useRouter()
  const { toast } = useToast()

  const [loading, setLoading] = useState(true)
  const [dashboard, setDashboard] = useState<AssociateDashboardData | null>(null)
  const [clients, setClients] = useState<any[]>([])
  const [clientsPage, setClientsPage] = useState(1)
  const [clientsTotalPages, setClientsTotalPages] = useState(1)
  const [actions, setActions] = useState<any[]>([])
  const [alerts, setAlerts] = useState<any[]>([])

  useEffect(() => {
    if (!associateId) return
    const fetchAll = async () => {
      try {
        setLoading(true)
        const [dash, clientRes, actionsRes, alertsRes] = await Promise.all([
          AssociateDashboardService.getDashboard(associateId),
          AssociateDashboardService.getDelegatedClients(associateId, 1, 10),
          AssociateDashboardService.getRecentActions(associateId, 20),
          AssociateDashboardService.getPermissionAlerts(associateId),
        ])
        setDashboard(dash.data)
        setClients(clientRes.data)
        setClientsPage(clientRes.pagination?.currentPage || 1)
        setClientsTotalPages(clientRes.pagination?.totalPages || 1)
        setActions(actionsRes.data)
        setAlerts(alertsRes.data.alerts)
      } catch (e: any) {
        toast({ title: 'Failed to load associate details', description: e?.message || 'Unexpected error', variant: 'destructive' })
      } finally {
        setLoading(false)
      }
    }
    fetchAll()
  }, [associateId, toast])

  const loadClientsPage = async (page: number) => {
    if (!associateId) return
    try {
      const res = await AssociateDashboardService.getDelegatedClients(associateId, page, 10)
      setClients(res.data)
      setClientsPage(res.pagination?.currentPage || page)
      setClientsTotalPages(res.pagination?.totalPages || 1)
    } catch (e: any) {
      toast({ title: 'Failed to load clients', description: e?.message || 'Unexpected error', variant: 'destructive' })
    }
  }

  if (loading) {
    return (
      <div className="flex items-center justify-center min-h-[400px]">
        <Clock className="h-6 w-6 mr-2 animate-pulse" />
        Loading...
      </div>
    )
  }

  if (!dashboard) {
    return (
      <div className="p-6">
        <Button variant="outline" onClick={() => router.push('/admin/associates')}>
          <ArrowLeft className="mr-2 h-4 w-4" /> Back to Associates
        </Button>
        <div className="mt-6 text-muted-foreground">No data available</div>
      </div>
    )
  }

  return (
    <div className="container mx-auto p-6 space-y-6">
      <div className="flex items-center justify-between">
        <div className="flex items-center gap-2">
          <Users className="h-6 w-6" />
          <h1 className="text-2xl font-bold">Associate Details</h1>
        </div>
        <Button variant="outline" onClick={() => router.push('/admin/associates')}>
          <ArrowLeft className="mr-2 h-4 w-4" /> Back to Associates
        </Button>
      </div>

      {/* Summary */}
      <div className="grid gap-4 md:grid-cols-2 lg:grid-cols-5">
        <Card>
          <CardHeader className="pb-2"><CardTitle className="text-sm">Total Clients</CardTitle></CardHeader>
          <CardContent className="text-2xl font-bold">{dashboard.summary.totalClients}</CardContent>
        </Card>
        <Card>
          <CardHeader className="pb-2"><CardTitle className="text-sm">Total Permissions</CardTitle></CardHeader>
          <CardContent className="text-2xl font-bold">{dashboard.summary.totalPermissions}</CardContent>
        </Card>
        <Card>
          <CardHeader className="pb-2"><CardTitle className="text-sm">Expiring Permissions</CardTitle></CardHeader>
          <CardContent className="flex items-center gap-2">
            <AlertTriangle className="h-4 w-4 text-yellow-600" />
            <span className="text-2xl font-bold">{dashboard.summary.expiringPermissions}</span>
          </CardContent>
        </Card>
        <Card>
          <CardHeader className="pb-2"><CardTitle className="text-sm">Recent Actions</CardTitle></CardHeader>
          <CardContent className="text-2xl font-bold">{dashboard.summary.recentActions}</CardContent>
        </Card>
        <Card>
          <CardHeader className="pb-2"><CardTitle className="text-sm">Upcoming Deadlines</CardTitle></CardHeader>
          <CardContent className="text-2xl font-bold">{dashboard.summary.upcomingDeadlines}</CardContent>
        </Card>
      </div>

      {/* Delegated Clients */}
      <Card>
        <CardHeader>
          <CardTitle>Delegated Clients</CardTitle>
          <CardDescription>Clients this associate is permitted to act on.</CardDescription>
        </CardHeader>
        <CardContent>
          {clients.length === 0 ? (
            <div className="text-muted-foreground">No delegated clients.</div>
          ) : (
            <div className="rounded-md border">
              <Table>
                <TableHeader>
                  <TableRow>
                    <TableHead>Client</TableHead>
                    <TableHead>Taxpayer Category</TableHead>
                    <TableHead>Upcoming Deadlines</TableHead>
                  </TableRow>
                </TableHeader>
                <TableBody>
                  {clients.map((c) => (
                    <TableRow key={c.clientId}>
                      <TableCell className="font-medium">{c.businessName}</TableCell>
                      <TableCell>{c.taxpayerCategory ?? '-'}</TableCell>
                      <TableCell>
                        {c.upcomingDeadlinesCount > 0 ? (
                          <Badge className="bg-amber-100 text-amber-800">{c.upcomingDeadlinesCount}</Badge>
                        ) : (
                          <Badge variant="secondary">0</Badge>
                        )}
                      </TableCell>
                    </TableRow>
                  ))}
                </TableBody>
              </Table>
            </div>
          )}

          {/* Pagination */}
          <div className="flex items-center justify-end gap-2 mt-3">
            <Button variant="outline" size="sm" disabled={clientsPage <= 1} onClick={() => loadClientsPage(clientsPage - 1)}>Prev</Button>
            <span className="text-sm text-muted-foreground">Page {clientsPage} of {clientsTotalPages}</span>
            <Button variant="outline" size="sm" disabled={clientsPage >= clientsTotalPages} onClick={() => loadClientsPage(clientsPage + 1)}>Next</Button>
          </div>
        </CardContent>
      </Card>

      {/* Permission Alerts */}
      <Card>
        <CardHeader>
          <CardTitle>Permission Alerts</CardTitle>
          <CardDescription>Expiring permissions and other alerts.</CardDescription>
        </CardHeader>
        <CardContent>
          {alerts.length === 0 ? (
            <div className="text-muted-foreground">No alerts.</div>
          ) : (
            <div className="space-y-3">
              {alerts.map((a: any) => (
                <div key={`${a.type}-${a.permissionId ?? a.clientId ?? Math.random()}`} className="p-3 border rounded-md flex items-center justify-between">
                  <div>
                    <div className="font-medium">{a.title}</div>
                    <div className="text-sm text-muted-foreground">{a.message}</div>
                  </div>
                  <Badge variant={a.severity === 'critical' ? 'destructive' : a.severity === 'high' ? 'default' : 'secondary'}>
                    {a.severity}
                  </Badge>
                </div>
              ))}
            </div>
          )}
        </CardContent>
      </Card>

      {/* Recent Actions */}
      <Card>
        <CardHeader>
          <CardTitle>Recent Actions</CardTitle>
          <CardDescription>Last actions taken on behalf of clients.</CardDescription>
        </CardHeader>
        <CardContent>
          {actions.length === 0 ? (
            <div className="text-muted-foreground">No recent actions.</div>
          ) : (
            <div className="space-y-3">
              {actions.map((ra: any) => (
                <div key={ra.id} className="p-3 border rounded-md flex items-center justify-between">
                  <div className="space-y-1">
                    <div className="flex items-center gap-2">
                      <Activity className="h-4 w-4 text-slate-600" />
                      <span className="font-medium">{ra.action}</span>
                      <Badge variant="outline">{ra.entityType}</Badge>
                    </div>
                    <div className="text-sm text-muted-foreground">
                      {ra.clientName ? `${ra.clientName} • ` : ''}{new Date(ra.actionDate).toLocaleString()}
                    </div>
                  </div>
                  <CheckCircle className="h-4 w-4 text-emerald-500" />
                </div>
              ))}
            </div>
          )}
        </CardContent>
      </Card>
    </div>
  )
}
