'use client';

import { useState } from 'react';
import { Card, CardContent, CardDescription, CardHeader, CardTitle } from '@/components/ui/card';
import { Button } from '@/components/ui/button';
import { Tabs, TabsContent, TabsList, TabsTrigger } from '@/components/ui/tabs';
import { TrendingUp, Users, FileText, DollarSign, Activity, Download, Building2, UserCircle } from 'lucide-react';
import Link from 'next/link';
import InternalKPIDashboard from '@/components/kpi/InternalKPIDashboard';
import { PageHeader } from '@/components/page-header';
import { MetricCard } from '@/components/metric-card';

export default function KPIDashboardPage() {
  const [activeView, setActiveView] = useState<'internal' | 'client'>('internal');

  // Mock data for client performance
  const clientKPIData = {
    totalClients: 145,
    activeClients: 132,
    avgComplianceScore: 87,
    avgFilingTime: 12,
    topPerformer: 'Koroma Industries Ltd.',
  };

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
            <div className="text-2xl font-bold text-sierra-blue">Overview</div>
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
            <div className="text-2xl font-bold text-sierra-gold">Analytics</div>
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
            <div className="text-2xl font-bold text-sierra-green">Trends</div>
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
            <div className="text-2xl font-bold text-purple-600">Finance</div>
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
              value="SLE 2.5M"
              trend="up"
              trendValue="+15%"
              subtitle="vs last quarter"
              icon={<DollarSign className="w-4 h-4" />}
              color="success"
            />
            <MetricCard
              title="Active Clients"
              value="132"
              trend="up"
              trendValue="+8"
              subtitle="vs last month"
              icon={<Users className="w-4 h-4" />}
              color="primary"
            />
            <MetricCard
              title="Filing Timeliness"
              value="92%"
              trend="up"
              trendValue="+5%"
              subtitle="on-time filings"
              icon={<FileText className="w-4 h-4" />}
              color="info"
            />
            <MetricCard
              title="Compliance Rate"
              value="87%"
              trend="down"
              trendValue="-2%"
              subtitle="vs target"
              icon={<TrendingUp className="w-4 h-4" />}
              color="warning"
            />
            <MetricCard
              title="Avg Processing"
              value="3.2 days"
              trend="down"
              trendValue="-0.8 days"
              subtitle="faster"
              icon={<Activity className="w-4 h-4" />}
              color="success"
            />
          </div>

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
              value={clientKPIData.totalClients}
              icon={<Users className="w-4 h-4" />}
              color="primary"
            />
            <MetricCard
              title="Active Clients"
              value={clientKPIData.activeClients}
              trend="up"
              trendValue="+8"
              subtitle="this month"
              icon={<Users className="w-4 h-4" />}
              color="success"
            />
            <MetricCard
              title="Avg Compliance"
              value={`${clientKPIData.avgComplianceScore}%`}
              trend="up"
              trendValue="+3%"
              subtitle="client average"
              icon={<TrendingUp className="w-4 h-4" />}
              color="info"
            />
            <MetricCard
              title="Avg Filing Time"
              value={`${clientKPIData.avgFilingTime} days`}
              trend="down"
              trendValue="-2 days"
              subtitle="before deadline"
              icon={<FileText className="w-4 h-4" />}
              color="success"
            />
            <MetricCard
              title="Top Performer"
              value="98%"
              subtitle={clientKPIData.topPerformer}
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
                {/* Large Taxpayers */}
                <div>
                  <div className="flex items-center justify-between mb-2">
                    <span className="text-sm font-medium">Large Taxpayers</span>
                    <span className="text-sm text-muted-foreground">95% compliance</span>
                  </div>
                  <div className="relative h-4 bg-gray-200 rounded-full overflow-hidden">
                    <div className="absolute h-full bg-green-600 transition-all" style={{ width: '95%' }} />
                  </div>
                </div>

                {/* Medium Taxpayers */}
                <div>
                  <div className="flex items-center justify-between mb-2">
                    <span className="text-sm font-medium">Medium Taxpayers</span>
                    <span className="text-sm text-muted-foreground">87% compliance</span>
                  </div>
                  <div className="relative h-4 bg-gray-200 rounded-full overflow-hidden">
                    <div className="absolute h-full bg-blue-600 transition-all" style={{ width: '87%' }} />
                  </div>
                </div>

                {/* Small Businesses */}
                <div>
                  <div className="flex items-center justify-between mb-2">
                    <span className="text-sm font-medium">Small Businesses</span>
                    <span className="text-sm text-muted-foreground">78% compliance</span>
                  </div>
                  <div className="relative h-4 bg-gray-200 rounded-full overflow-hidden">
                    <div className="absolute h-full bg-amber-500 transition-all" style={{ width: '78%' }} />
                  </div>
                </div>

                {/* Individuals */}
                <div>
                  <div className="flex items-center justify-between mb-2">
                    <span className="text-sm font-medium">Individual Taxpayers</span>
                    <span className="text-sm text-muted-foreground">82% compliance</span>
                  </div>
                  <div className="relative h-4 bg-gray-200 rounded-full overflow-hidden">
                    <div className="absolute h-full bg-sky-500 transition-all" style={{ width: '82%' }} />
                  </div>
                </div>
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
                <div className="text-2xl font-bold text-sierra-gold">Analytics</div>
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
                <div className="text-2xl font-bold text-sierra-green">Trends</div>
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
                <div className="text-2xl font-bold text-purple-600">Finance</div>
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