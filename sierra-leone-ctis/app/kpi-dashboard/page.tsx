'use client';

import { Card, CardContent, CardDescription, CardHeader, CardTitle } from '@/components/ui/card';
import { Button } from '@/components/ui/button';
import { TrendingUp, Users, FileText, DollarSign, Activity } from 'lucide-react';
import Link from 'next/link';
import InternalKPIDashboard from '@/components/kpi/InternalKPIDashboard';

export default function KPIDashboardPage() {
  return (
    <div className="flex-1 space-y-4 p-4 md:p-8 pt-6">
      {/* Header */}
      <div className="flex items-center justify-between">
        <div>
          <h2 className="text-3xl font-bold tracking-tight text-sierra-blue">KPI Dashboard</h2>
          <p className="text-muted-foreground">
            Monitor key performance indicators and business metrics for Sierra Leone tax compliance
          </p>
        </div>
      </div>

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

      {/* Main Internal KPI Dashboard */}
      <div className="space-y-4">
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
      </div>
    </div>
  );
}