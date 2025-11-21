'use client';

import { useMemo, useState } from 'react';
import { Card, CardContent, CardDescription, CardHeader, CardTitle } from '@/components/ui/card';
import { Button } from '@/components/ui/button';
import { Tabs, TabsContent, TabsList, TabsTrigger } from '@/components/ui/tabs';
import { TrendingUp, Users, FileText, DollarSign, Activity, Download, Building2, UserCircle } from 'lucide-react';
import Link from 'next/link';
import InternalKPIDashboard from '@/components/kpi/InternalKPIDashboard';
import { PageHeader } from '@/components/page-header';
import { MetricCard } from '@/components/metric-card';
import Loading from '@/app/loading';
import { useKpiDashboardSummary } from '@/lib/hooks/useKPIs';

export default function KPIDashboardPage() {
  const [activeView, setActiveView] = useState<'internal' | 'client'>('internal');
  const { data: summary, isLoading: summaryLoading, error: summaryError } = useKpiDashboardSummary();

  const internalSummary = summary?.internal;
  const clientSummary = summary?.client;
  const summaryErrorMessage = summaryError instanceof Error
    ? summaryError.message
    : summaryError
    ? 'Failed to load KPI summary data.'
    : null;

  const formatCurrency = (value?: number, currency: string = 'SLE') => {
    if (value == null) return '—';
    try {
      return new Intl.NumberFormat('en-US', {
        style: 'currency',
        currency,
        maximumFractionDigits: value >= 1 ? 0 : 2,
      }).format(value);
    } catch {
      return `${currency} ${value.toLocaleString()}`;
    }
  };

  const formatPercent = (value?: number) => {
    if (value == null) return '—';
    return `${value.toFixed(1)}%`;
  };

  const formatDays = (value?: number) => {
    if (value == null) return '—';
    return `${value.toFixed(1)} days`;
  };

  const segmentColours = ['bg-green-600', 'bg-blue-600', 'bg-amber-500', 'bg-sky-500', 'bg-purple-500'];
  const segments = useMemo(() => clientSummary?.segments ?? [], [clientSummary?.segments]);

  if (summaryLoading && !summary) {
    return <Loading />;
  }

  return (
    <div className="flex-1 flex flex-col">
      <PageHeader
        title="KPI Dashboard"
        breadcrumbs={[{ label: 'KPI Dashboard' }]}
        description="Monitor key performance indicators and business metrics for Sierra Leone tax compliance"
        actions={
          <Button variant="outline">
            <Download className="mr-2 h-4 w-4" />
            Export Report
          </Button>
        }
      />
      
      <div className="flex-1 p-6 space-y-6">

      {/* Quick Navigation Cards */}
      <div className="grid gap-4 md:grid-cols-2 lg:grid-cols-4">
        <Card className="hover:shadow-lg transition-shadow">
          <CardHeader className="flex flex-row items-center justify-between space-y-0 pb-2">
            <CardTitle className="text-sm font-medium">Internal KPIs</CardTitle>
            <TrendingUp className="h-4 w-4 text-sierra-blue" />
          </CardHeader>
          <CardContent>
            <div className="text-2xl font-bold text-sierra-blue-800">Overview</div>
            <p className="text-xs text-muted-foreground">
              Firm performance metrics
            </p>
            <Link href="/kpi-dashboard/internal">
              <Button variant="outline" size="sm" className="mt-2 w-full">
                View Details
              </Button>
            </Link>
          </CardContent>
        </Card>

        <Card className="hover:shadow-lg transition-shadow">
          <CardHeader className="flex flex-row items-center justify-between space-y-0 pb-2">
            <CardTitle className="text-sm font-medium">Client Performance</CardTitle>
            <Users className="h-4 w-4 text-sierra-gold" />
          </CardHeader>
          <CardContent>
            <div className="text-2xl font-bold text-sierra-blue-800">Analytics</div>
            <p className="text-xs text-muted-foreground">
              Client-specific KPIs
            </p>
            <Link href="/kpi-dashboard/clients">
              <Button variant="outline" size="sm" className="mt-2 w-full">
                View Details
              </Button>
            </Link>
          </CardContent>
        </Card>

        <Card className="hover:shadow-lg transition-shadow">
          <CardHeader className="flex flex-row items-center justify-between space-y-0 pb-2">
            <CardTitle className="text-sm font-medium">Compliance Trends</CardTitle>
            <FileText className="h-4 w-4 text-sierra-green" />
          </CardHeader>
          <CardContent>
            <div className="text-2xl font-bold text-sierra-blue-800">Trends</div>
            <p className="text-xs text-muted-foreground">
              Compliance over time
            </p>
            <Link href="/kpi-dashboard/compliance">
              <Button variant="outline" size="sm" className="mt-2 w-full">
                View Details
              </Button>
            </Link>
          </CardContent>
        </Card>

        <Card className="hover:shadow-lg transition-shadow">
          <CardHeader className="flex flex-row items-center justify-between space-y-0 pb-2">
            <CardTitle className="text-sm font-medium">Revenue Metrics</CardTitle>
            <DollarSign className="h-4 w-4 text-purple-600" />
          </CardHeader>
          <CardContent>
            <div className="text-2xl font-bold text-sierra-blue-800">Finance</div>
            <p className="text-xs text-muted-foreground">
              Revenue and payments
            </p>
            <Link href="/kpi-dashboard/revenue">
              <Button variant="outline" size="sm" className="mt-2 w-full">
                View Details
              </Button>
            </Link>
          </CardContent>
        </Card>
      </div>

      {/* Internal/Client View Tabs */}
      <Tabs value={activeView} onValueChange={(v) => setActiveView(v as 'internal' | 'client')} className="space-y-6">
        <TabsList className="grid w-full max-w-md grid-cols-2">
          <TabsTrigger value="internal" className="flex items-center gap-2">
            <Building2 className="h-4 w-4" />
            Internal View
          </TabsTrigger>
          <TabsTrigger value="client" className="flex items-center gap-2">
            <UserCircle className="h-4 w-4" />
            Client View
          </TabsTrigger>
        </TabsList>

        {/* Internal View */}
        <TabsContent value="internal" className="space-y-6">
          <div className="grid gap-4 md:grid-cols-2 lg:grid-cols-5">
            <MetricCard
              title="Total Revenue"
              value={formatCurrency(internalSummary?.totalRevenue, internalSummary?.revenueCurrency)}
              trend={internalSummary?.revenueChangePercentage != null ? (internalSummary.revenueChangePercentage > 0 ? 'up' : internalSummary.revenueChangePercentage < 0 ? 'down' : 'neutral') : undefined}
              trendValue={internalSummary?.revenueChangePercentage != null ? `${internalSummary.revenueChangePercentage > 0 ? '+' : ''}${internalSummary.revenueChangePercentage.toFixed(1)}%` : undefined}
              subtitle={internalSummary?.referencePeriodLabel}
              icon={<DollarSign className="w-4 h-4" />}
              color="success"
            />
            <MetricCard
              title="Active Clients"
              value={internalSummary ? internalSummary.activeClients.toLocaleString() : '—'}
              subtitle={internalSummary ? `${internalSummary.totalClients.toLocaleString()} total` : undefined}
              icon={<Users className="w-4 h-4" />}
              color="primary"
            />
            <MetricCard
              title="Filing Timeliness"
              value={formatDays(internalSummary?.averageFilingTimelinessDays)}
              subtitle="avg before deadline"
              icon={<FileText className="w-4 h-4" />}
              color="info"
            />
            <MetricCard
              title="Compliance Rate"
              value={formatPercent(internalSummary?.complianceRate)}
              subtitle="current period"
              icon={<TrendingUp className="w-4 h-4" />}
              color="warning"
            />
            <MetricCard
              title="Avg Processing"
              value={formatDays(internalSummary?.averageProcessingTimeDays)}
              subtitle="filing review"
              icon={<Activity className="w-4 h-4" />}
              color="success"
            />
          </div>

          {summaryErrorMessage && (
            <Card className="border-destructive/40">
              <CardHeader>
                <CardTitle>Error loading summary</CardTitle>
                <CardDescription>{summaryErrorMessage}</CardDescription>
              </CardHeader>
            </Card>
          )}

          <Card>
            <CardHeader>
              <CardTitle className="flex items-center gap-2">
                <Activity className="h-5 w-5 text-sierra-blue" />
                Internal Performance Overview
              </CardTitle>
              <CardDescription>
                Real-time key performance indicators for firm operations
              </CardDescription>
            </CardHeader>
            <CardContent>
              <InternalKPIDashboard />
            </CardContent>
          </Card>
        </TabsContent>

        {/* Client View */}
        <TabsContent value="client" className="space-y-6">
          <div className="grid gap-4 md:grid-cols-2 lg:grid-cols-5">
            <MetricCard
              title="Total Clients"
              value={clientSummary ? clientSummary.totalClients.toLocaleString() : '—'}
              icon={<Users className="w-4 h-4" />}
              color="primary"
            />
            <MetricCard
              title="Active Clients"
              value={clientSummary ? clientSummary.activeClients.toLocaleString() : '—'}
              subtitle={clientSummary ? `${clientSummary.totalClients.toLocaleString()} total` : undefined}
              icon={<Users className="w-4 h-4" />}
              color="success"
            />
            <MetricCard
              title="Avg Compliance"
              value={formatPercent(clientSummary?.averageComplianceScore)}
              subtitle="client average"
              icon={<TrendingUp className="w-4 h-4" />}
              color="info"
            />
            <MetricCard
              title="Avg Filing Time"
              value={formatDays(clientSummary?.averageFilingTimeDays)}
              subtitle="before deadline"
              icon={<FileText className="w-4 h-4" />}
              color="success"
            />
            <MetricCard
              title="Top Performer"
              value={formatPercent(clientSummary?.topPerformerComplianceScore)}
              subtitle={clientSummary?.topPerformerName ?? 'No data'}
              icon={<Activity className="w-4 h-4" />}
              color="success"
            />
          </div>

          <Card>
            <CardHeader>
              <CardTitle>Client Performance Breakdown</CardTitle>
              <CardDescription>Compliance scores by client segment</CardDescription>
            </CardHeader>
            <CardContent>
              <div className="space-y-6">
                {segments.length > 0 ? (
                  segments.map((segment, index) => {
                    const colourClass = segmentColours[index % segmentColours.length];
                    const width = Math.max(0, Math.min(segment.complianceRate, 100));

                    return (
                      <div key={segment.segment}>
                        <div className="flex items-center justify-between mb-2">
                          <span className="text-sm font-medium">{segment.segment}</span>
                          <span className="text-sm text-muted-foreground">
                            {segment.complianceRate.toFixed(1)}% compliance · {segment.clientCount.toLocaleString()} clients
                          </span>
                        </div>
                        <div className="relative h-4 bg-gray-200 rounded-full overflow-hidden">
                          <div
                            className={`absolute h-full transition-all ${colourClass}`}
                            style={{ width: `${width}%` }}
                          />
                        </div>
                      </div>
                    );
                  })
                ) : (
                  <p className="text-sm text-muted-foreground">No segment performance data available.</p>
                )}
              </div>
            </CardContent>
          </Card>

          {/* Quick Navigation Cards for Client View */}
          <div className="grid gap-4 md:grid-cols-2 lg:grid-cols-4">
            <Card className="hover:shadow-lg transition-shadow">
              <CardHeader className="flex flex-row items-center justify-between space-y-0 pb-2">
                <CardTitle className="text-sm font-medium">Client Performance</CardTitle>
                <Users className="h-4 w-4 text-sierra-gold" />
              </CardHeader>
              <CardContent>
                <div className="text-2xl font-bold text-sierra-blue-800">Analytics</div>
                <p className="text-xs text-muted-foreground">
                  Client-specific KPIs
                </p>
                <Link href="/kpi-dashboard/clients">
                  <Button variant="outline" size="sm" className="mt-2 w-full">
                    View Details
                  </Button>
                </Link>
              </CardContent>
            </Card>

            <Card className="hover:shadow-lg transition-shadow">
              <CardHeader className="flex flex-row items-center justify-between space-y-0 pb-2">
                <CardTitle className="text-sm font-medium">Compliance Trends</CardTitle>
                <FileText className="h-4 w-4 text-sierra-green" />
              </CardHeader>
              <CardContent>
                <div className="text-2xl font-bold text-sierra-blue-800">Trends</div>
                <p className="text-xs text-muted-foreground">
                  Compliance over time
                </p>
                <Link href="/kpi-dashboard/compliance">
                  <Button variant="outline" size="sm" className="mt-2 w-full">
                    View Details
                  </Button>
                </Link>
              </CardContent>
            </Card>

            <Card className="hover:shadow-lg transition-shadow">
              <CardHeader className="flex flex-row items-center justify-between space-y-0 pb-2">
                <CardTitle className="text-sm font-medium">Revenue Metrics</CardTitle>
                <DollarSign className="h-4 w-4 text-purple-600" />
              </CardHeader>
              <CardContent>
                <div className="text-2xl font-bold text-sierra-blue-800">Finance</div>
                <p className="text-xs text-muted-foreground">
                  Revenue and payments
                </p>
                <Link href="/kpi-dashboard/revenue">
                  <Button variant="outline" size="sm" className="mt-2 w-full">
                    View Details
                  </Button>
                </Link>
              </CardContent>
            </Card>

            <Card className="hover:shadow-lg transition-shadow">
              <CardHeader className="flex flex-row items-center justify-between space-y-0 pb-2">
                <CardTitle className="text-sm font-medium">Top Clients</CardTitle>
                <TrendingUp className="h-4 w-4 text-sierra-blue" />
              </CardHeader>
              <CardContent>
                <div className="text-2xl font-bold text-sierra-blue">Rankings</div>
                <p className="text-xs text-muted-foreground">
                  Best performers
                </p>
                <Link href="/kpi-dashboard/top-clients">
                  <Button variant="outline" size="sm" className="mt-2 w-full">
                    View Details
                  </Button>
                </Link>
              </CardContent>
            </Card>
          </div>
        </TabsContent>
      </Tabs>
      </div>
    </div>
  );
}