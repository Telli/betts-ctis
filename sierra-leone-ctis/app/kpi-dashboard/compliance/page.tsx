'use client';

import { Button } from '@/components/ui/button';
import { ArrowLeft } from 'lucide-react';
import Link from 'next/link';
import ComplianceTrendChart from '@/components/kpi/ComplianceTrendChart';
import ComplianceScoreCard from '@/components/kpi/ComplianceScoreCard';
import { ComplianceLevel } from '@/lib/types/kpi';

export default function ComplianceKPIDashboardPage() {
  // Sample data for the chart
  const trendData = [
    { date: '2024-01-01', value: 78, label: 'Jan' },
    { date: '2024-02-01', value: 82, label: 'Feb' },
    { date: '2024-03-01', value: 85, label: 'Mar' },
    { date: '2024-04-01', value: 88, label: 'Apr' },
    { date: '2024-05-01', value: 85, label: 'May' },
    { date: '2024-06-01', value: 90, label: 'Jun' },
  ];

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
            <h2 className="text-3xl font-bold tracking-tight text-sierra-green">Compliance Trends Dashboard</h2>
            <p className="text-muted-foreground">
              Historical compliance performance and trend analysis
            </p>
          </div>
        </div>
      </div>

      {/* Compliance KPI Dashboard */}
      <div className="grid gap-6 md:grid-cols-2">
        <ComplianceScoreCard
          score={85}
          level={ComplianceLevel.Green}
          trend={5}
          description="Overall compliance score based on filing timeliness and accuracy"
        />
        <ComplianceTrendChart data={trendData} />
      </div>
    </div>
  );
}