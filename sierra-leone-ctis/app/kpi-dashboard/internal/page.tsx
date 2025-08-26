'use client';

import { Button } from '@/components/ui/button';
import { ArrowLeft } from 'lucide-react';
import Link from 'next/link';
import InternalKPIDashboard from '@/components/kpi/InternalKPIDashboard';

export default function InternalKPIDashboardPage() {
  return (
    <div className="flex-1 space-y-4 p-4 md:p-8 pt-6">
      {/* Header */}
      <div className="flex items-center justify-between">
        <div className="flex items-center gap-4">
          <Link href="/kpi-dashboard">
            <Button variant="ghost" size="sm">
              <ArrowLeft className="h-4 w-4 mr-2" />
              Back to KPI Dashboard
            </Button>
          </Link>
          <div>
            <h2 className="text-3xl font-bold tracking-tight text-sierra-blue">Internal KPI Dashboard</h2>
            <p className="text-muted-foreground">
              Comprehensive firm performance metrics and analytics
            </p>
          </div>
        </div>
      </div>

      {/* Internal KPI Dashboard Component */}
      <InternalKPIDashboard />
    </div>
  );
}