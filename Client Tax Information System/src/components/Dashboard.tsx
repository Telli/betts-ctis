import { useEffect, useMemo, useState } from "react";
import { PageHeader } from "./PageHeader";
import { MetricCard } from "./MetricCard";
import { Button } from "./ui/button";
import { Card, CardContent, CardHeader, CardTitle } from "./ui/card";
import { Tabs, TabsContent, TabsList, TabsTrigger } from "./ui/tabs";
import { Badge } from "./ui/badge";
import { Progress } from "./ui/progress";
import { Alert, AlertDescription } from "./ui/alert";
import { CheckCircle, Clock, AlertTriangle, FileText, DollarSign, Calendar } from "lucide-react";
import { LineChart, Line, PieChart, Pie, Cell, XAxis, YAxis, CartesianGrid, Tooltip, Legend, ResponsiveContainer } from "recharts";
import { fetchDashboardSummary, DashboardSummary } from "../lib/services/dashboard";
import { DeadlineDto, fetchOverdueDeadlines } from "../lib/services/deadlines";

interface DashboardProps {
  userRole?: "client" | "staff";
  clientId?: number | null;
}

type ViewType = "client" | "staff";

interface DisplayDeadline {
  id: number;
  title: string;
  client: string;
  taxType: string;
  dueDate: string;
  status: "pending" | "at-risk" | "urgent" | "overdue";
  daysLeft: number;
  assignedTo?: string;
  progress: number;
}

export function Dashboard({ userRole = "staff", clientId }: DashboardProps) {
  const [viewType, setViewType] = useState<ViewType>(userRole);
  const [summary, setSummary] = useState<DashboardSummary | null>(null);
  const [overdueDeadlines, setOverdueDeadlines] = useState<DeadlineDto[]>([]);
  const [isLoading, setIsLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);

  useEffect(() => {
    setViewType(userRole);
  }, [userRole]);

  useEffect(() => {
    let cancelled = false;

    async function loadDashboardData() {
      setIsLoading(true);
      try {
        const [summaryData, overdue] = await Promise.all([
          fetchDashboardSummary({ clientId: clientId ?? undefined }),
          fetchOverdueDeadlines(clientId ?? undefined),
        ]);

        if (!cancelled) {
          setSummary(summaryData);
          setOverdueDeadlines(overdue);
          setError(null);
        }
      } catch (err) {
        if (!cancelled) {
          setError(err instanceof Error ? err.message : "Failed to load dashboard data.");
        }
      } finally {
        if (!cancelled) {
          setIsLoading(false);
        }
      }
    }

    loadDashboardData();
    return () => {
      cancelled = true;
    };
  }, [clientId]);

  const metrics = summary?.metrics ?? [];

  const clientMetricLabelOverrides: Record<string, string> = {
    clientCompliance: "Compliance Score",
    filingTimeliness: "Filing Timeliness",
    paymentCompletion: "On-Time Payments",
    documentCompliance: "Document Readiness",
  };

  const clientMetrics = useMemo(
    () =>
      metrics
        .filter((metric) => clientMetricLabelOverrides[metric.key])
        .map((metric) => ({
          ...metric,
          title: clientMetricLabelOverrides[metric.key] ?? metric.title,
        })),
    [metrics],
  );

  const filingTrends = summary?.filingTrends ?? [];
  const complianceDistribution = summary?.complianceDistribution ?? [];
  const recentActivity = summary?.recentActivity ?? [];

  const calculateDaysLeft = (dueDate: string) => {
    if (!dueDate) {
      return 0;
    }
    const due = new Date(dueDate);
    if (Number.isNaN(due.getTime())) {
      return 0;
    }
    const today = new Date();
    today.setHours(0, 0, 0, 0);
    due.setHours(0, 0, 0, 0);
    return Math.ceil((due.getTime() - today.getTime()) / (1000 * 60 * 60 * 24));
  };

  const formatDate = (value: string) =>
    value
      ? new Date(value).toLocaleDateString(undefined, {
          year: "numeric",
          month: "short",
          day: "numeric",
        })
      : "Not available";

  const computeProgress = (daysLeft: number) => {
    if (daysLeft < 0) return 0;
    const percentage = 100 - (Math.min(daysLeft, 30) / 30) * 100;
    return Math.max(5, Math.min(100, Math.round(percentage)));
  };

  const determineDeadlineStatus = (daysLeft: number) => {
    if (daysLeft < 0) return "overdue" as const;
    if (daysLeft <= 3) return "urgent" as const;
    if (daysLeft <= 7) return "at-risk" as const;
    return "pending" as const;
  };

  const upcomingDisplay = useMemo<DisplayDeadline[]>(() => {
    const data = summary?.upcomingDeadlines ?? [];
    return data.map((deadline) => {
      const daysLeft = calculateDaysLeft(deadline.dueDate);
      const status = determineDeadlineStatus(daysLeft);
      return {
        id: deadline.id,
        title: `${deadline.taxType ?? "Not available"} • ${deadline.client ?? "Not available"}`,
        client: deadline.client ?? "Not available",
        taxType: deadline.taxType ?? "Not available",
        dueDate: formatDate(deadline.dueDate),
        status,
        daysLeft,
        progress: computeProgress(daysLeft),
      };
    });
  }, [summary]);

  const atRiskDeadlines = useMemo(
    () => upcomingDisplay.filter((deadline) => deadline.status === "at-risk" || deadline.status === "urgent"),
    [upcomingDisplay],
  );

  const penaltyWarnings = useMemo(() => {
    return overdueDeadlines.map((deadline) => {
      const daysLeft = calculateDaysLeft(deadline.dueDate);
      return {
        id: deadline.id,
        type: `${deadline.taxTypeName ?? "Not available"} • ${deadline.clientName ?? "Not available"}`,
        reason: `${deadline.priority ?? "Not available"} priority overdue task`,
        daysOverdue: Math.abs(daysLeft),
      };
    });
  }, [overdueDeadlines]);

  const hasRecentActivity = recentActivity.length > 0;

  return (
    <div>
      {userRole === "staff" && (
        <PageHeader
          title="Dashboard"
          breadcrumbs={[{ label: "Dashboard" }]}
          actions={
            <Tabs value={viewType} onValueChange={(value) => setViewType(value as ViewType)}>
              <TabsList>
                <TabsTrigger value="staff">Staff View</TabsTrigger>
                <TabsTrigger value="client">Client View</TabsTrigger>
              </TabsList>
            </Tabs>
          }
        />
      )}

      {!userRole || userRole === "client" ? (
        <PageHeader title="Dashboard" breadcrumbs={[{ label: "Dashboard" }]} />
      ) : null}

      <div className="p-6 space-y-6">
        {error && (
          <Alert variant="destructive">
            <AlertDescription>{error}</AlertDescription>
          </Alert>
        )}

        {isLoading ? (
          <div className="text-sm text-muted-foreground">Loading dashboard data...</div>
        ) : viewType === "staff" ? (
          <>
            <div className="grid grid-cols-1 md:grid-cols-2 lg:grid-cols-4 gap-6">
              {metrics.length === 0 ? (
                <p className="text-sm text-muted-foreground">No metrics available.</p>
              ) : (
                metrics.map((metric) => (
                  <MetricCard
                    key={metric.key}
                    title={metric.title}
                    value={metric.value || "Not available"}
                    trend={metric.trendDirection}
                    trendValue={metric.trendValue || "N/A"}
                    subtitle={metric.subtitle || ""}
                    icon={
                      metric.key === "clientCompliance" ? (
                        <CheckCircle className="w-4 h-4" />
                      ) : metric.key === "filingTimeliness" ? (
                        <Clock className="w-4 h-4" />
                      ) : metric.key === "paymentCompletion" ? (
                        <DollarSign className="w-4 h-4" />
                      ) : (
                        <FileText className="w-4 h-4" />
                      )
                    }
                    color={
                      metric.color === "success" || metric.color === "primary" || metric.color === "info"
                        ? (metric.color as "success" | "primary" | "info")
                        : "default"
                    }
                  />
                ))
              )}
            </div>

            <div className="grid grid-cols-1 lg:grid-cols-2 gap-6">
              <Card>
                <CardHeader>
                  <CardTitle>Filing Timeliness Trend</CardTitle>
                </CardHeader>
                <CardContent>
                  {filingTrends.length === 0 ? (
                    <p className="text-sm text-muted-foreground">No trend data available.</p>
                  ) : (
                    <ResponsiveContainer width="100%" height={300}>
                      <LineChart data={filingTrends}>
                        <CartesianGrid strokeDasharray="3 3" stroke="#e2e8f0" />
                        <XAxis dataKey="month" />
                        <YAxis />
                        <Tooltip />
                        <Legend />
                        <Line type="monotone" dataKey="onTime" stroke="#38a169" strokeWidth={2} name="On Time %" />
                        <Line type="monotone" dataKey="late" stroke="#e53e3e" strokeWidth={2} name="Late %" />
                      </LineChart>
                    </ResponsiveContainer>
                  )}
                </CardContent>
              </Card>

              <Card>
                <CardHeader>
                  <CardTitle>Compliance Distribution</CardTitle>
                </CardHeader>
                <CardContent>
                  {complianceDistribution.length === 0 ? (
                    <p className="text-sm text-muted-foreground">No compliance distribution available.</p>
                  ) : (
                    <ResponsiveContainer width="100%" height={300}>
                      <PieChart>
                        <Pie
                          data={complianceDistribution}
                          cx="50%"
                          cy="50%"
                          labelLine={false}
                          label={({ name, percent }) => `${name}: ${(percent * 100).toFixed(0)}%`}
                          outerRadius={80}
                          fill="#8884d8"
                          dataKey="value"
                        >
                          {complianceDistribution.map((entry, index) => (
                            <Cell key={`cell-${index}`} fill={entry.color} />
                          ))}
                        </Pie>
                        <Tooltip />
                      </PieChart>
                    </ResponsiveContainer>
                  )}
                </CardContent>
              </Card>
            </div>

            <div className="grid grid-cols-1 lg:grid-cols-2 gap-6">
              <Card>
                <CardHeader>
                  <CardTitle>At-Risk Clients</CardTitle>
                </CardHeader>
                <CardContent>
                  <div className="space-y-4">
                    {atRiskDeadlines.length === 0 ? (
                      <p className="text-sm text-muted-foreground">No at-risk deadlines detected.</p>
                    ) : (
                      atRiskDeadlines.map((deadline) => (
                        <div key={deadline.id} className="flex items-center justify-between p-3 border border-border rounded-lg">
                          <div>
                            <p className="font-medium">{deadline.client}</p>
                            <p className="text-sm text-muted-foreground">{deadline.taxType}</p>
                          </div>
                          <div className="text-right">
                            <Badge variant={deadline.status === "urgent" ? "destructive" : "default"}>
                              {deadline.daysLeft >= 0 ? `${deadline.daysLeft} days left` : `${Math.abs(deadline.daysLeft)} days overdue`}
                            </Badge>
                            <p className="text-xs text-muted-foreground mt-1">Due {deadline.dueDate}</p>
                          </div>
                        </div>
                      ))
                    )}
                  </div>
                </CardContent>
              </Card>

              <Card>
                <CardHeader>
                  <CardTitle>Recent Activity</CardTitle>
                </CardHeader>
                <CardContent>
                  <div className="space-y-4">
                    {hasRecentActivity ? (
                      recentActivity.map((activity, index) => (
                        <div key={`${activity.action}-${index}`} className="flex gap-3">
                          <div className="w-2 h-2 bg-primary rounded-full mt-2" />
                          <div>
                            <p className="text-sm">{activity.action ?? "Activity"}</p>
                            <p className="text-xs text-muted-foreground">
                              {activity.user ?? "Not available"} • {activity.timeDescription ?? "Not available"}
                            </p>
                          </div>
                        </div>
                      ))
                    ) : (
                      <p className="text-sm text-muted-foreground">No recent activity recorded.</p>
                    )}
                  </div>
                </CardContent>
              </Card>
            </div>
          </>
        ) : (
          <>
            <div className="grid grid-cols-1 md:grid-cols-2 lg:grid-cols-4 gap-6">
              {clientMetrics.length === 0 ? (
                <p className="text-sm text-muted-foreground col-span-full">No client metrics available.</p>
              ) : (
                clientMetrics.map((metric) => (
                  <MetricCard
                    key={metric.key}
                    title={metric.title}
                    value={metric.value || "Not available"}
                    trend={metric.trendDirection}
                    trendValue={metric.trendValue || "N/A"}
                    subtitle={metric.subtitle || ""}
                    icon={
                      metric.key === "clientCompliance" ? (
                        <CheckCircle className="w-4 h-4" />
                      ) : metric.key === "filingTimeliness" ? (
                        <Clock className="w-4 h-4" />
                      ) : metric.key === "paymentCompletion" ? (
                        <DollarSign className="w-4 h-4" />
                      ) : (
                        <FileText className="w-4 h-4" />
                      )
                    }
                    color={
                      metric.color === "success" || metric.color === "primary" || metric.color === "info"
                        ? (metric.color as "success" | "primary" | "info")
                        : "default"
                    }
                  />
                ))
              )}
            </div>

            <div className="grid grid-cols-1 lg:grid-cols-2 gap-6">
              <Card>
                <CardHeader>
                  <CardTitle>Upcoming Deadlines</CardTitle>
                </CardHeader>
                <CardContent>
                  <div className="space-y-4">
                    {upcomingDisplay.length === 0 ? (
                      <p className="text-sm text-muted-foreground">No upcoming deadlines.</p>
                    ) : (
                      upcomingDisplay.slice(0, 3).map((deadline) => (
                        <div key={deadline.id} className="flex items-center justify-between p-3 border border-border rounded-lg">
                          <div>
                            <p className="font-medium">{deadline.taxType}</p>
                            <p className="text-sm text-muted-foreground">Due {deadline.dueDate}</p>
                          </div>
                          <Badge variant={deadline.status === "urgent" ? "destructive" : "default"}>
                            {deadline.daysLeft >= 0 ? `${deadline.daysLeft} days` : `${Math.abs(deadline.daysLeft)} days overdue`}
                          </Badge>
                        </div>
                      ))
                    )}
                  </div>
                </CardContent>
              </Card>

              <Card>
                <CardHeader>
                  <CardTitle>Penalty & Risk Alerts</CardTitle>
                </CardHeader>
                <CardContent>
                  <div className="space-y-4">
                    {penaltyWarnings.length === 0 ? (
                      <p className="text-sm text-muted-foreground">No overdue filings. Great work!</p>
                    ) : (
                      penaltyWarnings.map((warning) => (
                        <div key={warning.id} className="flex items-start gap-3 p-3 border border-destructive/20 bg-destructive/5 rounded-lg">
                          <AlertTriangle className="w-5 h-5 text-destructive mt-0.5" />
                          <div className="flex-1">
                            <p className="font-medium">{warning.type}</p>
                            <p className="text-sm text-muted-foreground">{warning.reason}</p>
                            <div className="flex items-center justify-between mt-2">
                              <Badge variant="destructive" className="text-xs">
                                {warning.daysOverdue} days overdue
                              </Badge>
                              <Button variant="link" className="h-auto p-0 text-sm">
                                Resolve now
                              </Button>
                            </div>
                          </div>
                        </div>
                      ))
                    )}
                  </div>
                </CardContent>
              </Card>
            </div>

            <Card>
              <CardHeader>
                <CardTitle>Recent Activity</CardTitle>
              </CardHeader>
              <CardContent>
                <div className="space-y-4">
                  {hasRecentActivity ? (
                    recentActivity.slice(0, 5).map((activity, index) => (
                      <div key={`${activity.action}-${index}`} className="flex gap-3">
                        <div className="w-2 h-2 bg-primary rounded-full mt-2" />
                        <div>
                          <p className="text-sm">{activity.action ?? "Activity"}</p>
                          <p className="text-xs text-muted-foreground">{activity.timeDescription ?? "Not available"}</p>
                        </div>
                      </div>
                    ))
                  ) : (
                    <p className="text-sm text-muted-foreground">No recent activity.</p>
                  )}
                </div>
              </CardContent>
            </Card>
          </>
        )}
      </div>
    </div>
  );
}
