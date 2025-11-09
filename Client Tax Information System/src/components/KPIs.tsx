import { useEffect, useMemo, useState } from "react";
import { PageHeader } from "./PageHeader";
import { Card, CardContent, CardHeader, CardTitle } from "./ui/card";
import { Tabs, TabsContent, TabsList, TabsTrigger } from "./ui/tabs";
import { MetricCard } from "./MetricCard";
import {
  Select,
  SelectContent,
  SelectItem,
  SelectTrigger,
  SelectValue,
} from "./ui/select";
import { CheckCircle, Clock, DollarSign, FileText, TrendingUp, Users } from "lucide-react";
import {
  LineChart,
  Line,
  BarChart,
  Bar,
  XAxis,
  YAxis,
  CartesianGrid,
  Tooltip,
  Legend,
  ResponsiveContainer,
} from "recharts";
import { Alert, AlertDescription } from "./ui/alert";
import { fetchKpiSummary, KpiMetric, KpiSummary } from "../lib/services/kpis";

interface KPIsProps {
  clientId?: number | null;
  userRole?: string;
}

type ViewType = "internal" | "client";

export function KPIs({ clientId, userRole }: KPIsProps) {
  const [dateRange, setDateRange] = useState("6months");
  const [viewType, setViewType] = useState<ViewType>(userRole?.toLowerCase() === "client" ? "client" : "internal");
  const [summary, setSummary] = useState<KpiSummary | null>(null);
  const [isLoading, setIsLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);

  useEffect(() => {
    setViewType(userRole?.toLowerCase() === "client" ? "client" : "internal");
  }, [userRole]);

  useEffect(() => {
    let cancelled = false;

    async function loadKpis() {
      setIsLoading(true);
      try {
        const data = await fetchKpiSummary(clientId ?? undefined);
        if (!cancelled) {
          setSummary(data);
          setError(null);
        }
      } catch (err) {
        if (!cancelled) {
          setError(err instanceof Error ? err.message : "Failed to load KPI data.");
        }
      } finally {
        if (!cancelled) {
          setIsLoading(false);
        }
      }
    }

    loadKpis();
    return () => {
      cancelled = true;
    };
  }, [clientId]);

  const internalMetrics = summary?.internalMetrics ?? [];
  const clientMetrics = summary?.clientMetrics ?? [];
  const monthlyTrend = summary?.monthlyTrend ?? [];
  const clientPerformance = summary?.clientPerformance ?? [];
  const performanceBreakdown = summary?.performanceBreakdown ?? [];

  const getMetricIcon = (metric: KpiMetric) => {
    switch (metric.key) {
      case "complianceRate":
      case "clientCompliance":
        return <CheckCircle className="w-4 h-4" />;
      case "avgTimeliness":
      case "myTimeliness":
        return <Clock className="w-4 h-4" />;
      case "paymentCompletion":
      case "onTimePayments":
        return <DollarSign className="w-4 h-4" />;
      case "docSubmission":
      case "documentReadiness":
        return <FileText className="w-4 h-4" />;
      case "engagementRate":
        return <Users className="w-4 h-4" />;
      case "compositeScore":
        return <TrendingUp className="w-4 h-4" />;
      default:
        return <TrendingUp className="w-4 h-4" />;
    }
  };

  const renderMetrics = (metrics: KpiMetric[]) =>
    metrics.length === 0 ? (
      <p className="text-sm text-muted-foreground">No metrics available for this view.</p>
    ) : (
      metrics.map((metric) => (
        <MetricCard
          key={metric.key}
          title={metric.title}
          value={metric.value || "Not available"}
          trend={metric.trendDirection}
          trendValue={metric.trendValue || "N/A"}
          subtitle={metric.subtitle || ""}
          icon={getMetricIcon(metric)}
          color={
            metric.color === "success" || metric.color === "primary" || metric.color === "info"
              ? (metric.color as "success" | "primary" | "info")
              : "default"
          }
        />
      ))
    );

  return (
    <div>
      <PageHeader
        title="Key Performance Indicators"
        breadcrumbs={[{ label: "KPIs" }]}
        actions={
          <div className="flex gap-3">
            <Select value={dateRange} onValueChange={setDateRange}>
              <SelectTrigger className="w-[180px]">
                <SelectValue />
              </SelectTrigger>
              <SelectContent>
                <SelectItem value="30days">Last 30 Days</SelectItem>
                <SelectItem value="3months">Last 3 Months</SelectItem>
                <SelectItem value="6months">Last 6 Months</SelectItem>
                <SelectItem value="1year">Last Year</SelectItem>
              </SelectContent>
            </Select>
          </div>
        }
      />

      <div className="p-6 space-y-6">
        {error && (
          <Alert variant="destructive">
            <AlertDescription>{error}</AlertDescription>
          </Alert>
        )}

        {isLoading ? (
          <p className="text-sm text-muted-foreground">Loading KPI data...</p>
        ) : (
          <Tabs value={viewType} onValueChange={(value) => setViewType(value as ViewType)}>
            <TabsList className="mb-6">
              <TabsTrigger value="internal">Internal KPIs</TabsTrigger>
              <TabsTrigger value="client">Client KPIs</TabsTrigger>
            </TabsList>

            <TabsContent value="internal" className="space-y-6">
              <div className="grid grid-cols-1 md:grid-cols-2 lg:grid-cols-5 gap-4">
                {renderMetrics(internalMetrics)}
              </div>

              <div className="grid grid-cols-1 lg:grid-cols-2 gap-6">
                <Card>
                  <CardHeader>
                    <CardTitle>Compliance Trend</CardTitle>
                  </CardHeader>
                  <CardContent>
                    {monthlyTrend.length === 0 ? (
                      <p className="text-sm text-muted-foreground">No trend data available.</p>
                    ) : (
                      <ResponsiveContainer width="100%" height={300}>
                        <LineChart data={monthlyTrend}>
                          <CartesianGrid strokeDasharray="3 3" stroke="#e2e8f0" />
                          <XAxis dataKey="month" />
                          <YAxis />
                          <Tooltip />
                          <Legend />
                          <Line
                            type="monotone"
                            dataKey="compliance"
                            stroke="#3d5f7e"
                            strokeWidth={2}
                            name="Compliance %"
                          />
                        </LineChart>
                      </ResponsiveContainer>
                    )}
                  </CardContent>
                </Card>

                <Card>
                  <CardHeader>
                    <CardTitle>Filing Timeliness Trend</CardTitle>
                  </CardHeader>
                  <CardContent>
                    {monthlyTrend.length === 0 ? (
                      <p className="text-sm text-muted-foreground">No timeliness data available.</p>
                    ) : (
                      <ResponsiveContainer width="100%" height={300}>
                        <LineChart data={monthlyTrend}>
                          <CartesianGrid strokeDasharray="3 3" stroke="#e2e8f0" />
                          <XAxis dataKey="month" />
                          <YAxis />
                          <Tooltip />
                          <Legend />
                          <Line
                            type="monotone"
                            dataKey="timeliness"
                            stroke="#4299e1"
                            strokeWidth={2}
                            name="Days Before Deadline"
                          />
                        </LineChart>
                      </ResponsiveContainer>
                    )}
                  </CardContent>
                </Card>

                <Card>
                  <CardHeader>
                    <CardTitle>Payment Completion Rate</CardTitle>
                  </CardHeader>
                  <CardContent>
                    {monthlyTrend.length === 0 ? (
                      <p className="text-sm text-muted-foreground">No payment completion data available.</p>
                    ) : (
                      <ResponsiveContainer width="100%" height={300}>
                        <LineChart data={monthlyTrend}>
                          <CartesianGrid strokeDasharray="3 3" stroke="#e2e8f0" />
                          <XAxis dataKey="month" />
                          <YAxis domain={[0, 100]} />
                          <Tooltip />
                          <Legend />
                          <Line
                            type="monotone"
                            dataKey="payments"
                            stroke="#38a169"
                            strokeWidth={2}
                            name="Payment %"
                          />
                        </LineChart>
                      </ResponsiveContainer>
                    )}
                  </CardContent>
                </Card>

                <Card>
                  <CardHeader>
                    <CardTitle>Top Performing Clients</CardTitle>
                  </CardHeader>
                  <CardContent>
                    {clientPerformance.length === 0 ? (
                      <p className="text-sm text-muted-foreground">No client performance data available.</p>
                    ) : (
                      <ResponsiveContainer width="100%" height={300}>
                        <BarChart data={clientPerformance} layout="vertical">
                          <CartesianGrid strokeDasharray="3 3" stroke="#e2e8f0" />
                          <XAxis type="number" domain={[0, 100]} />
                          <YAxis dataKey="name" type="category" width={120} />
                          <Tooltip />
                          <Bar dataKey="score" fill="#3d5f7e" name="Compliance Score" />
                        </BarChart>
                      </ResponsiveContainer>
                    )}
                  </CardContent>
                </Card>
              </div>
            </TabsContent>

            <TabsContent value="client" className="space-y-6">
              <div className="grid grid-cols-1 md:grid-cols-2 lg:grid-cols-4 gap-4">
                {renderMetrics(clientMetrics)}
              </div>

              <div className="grid grid-cols-1 lg:grid-cols-2 gap-6">
                <Card>
                  <CardHeader>
                    <CardTitle>My Compliance Score History</CardTitle>
                  </CardHeader>
                  <CardContent>
                    {monthlyTrend.length === 0 ? (
                      <p className="text-sm text-muted-foreground">No compliance history available.</p>
                    ) : (
                      <ResponsiveContainer width="100%" height={300}>
                        <LineChart data={monthlyTrend}>
                          <CartesianGrid strokeDasharray="3 3" stroke="#e2e8f0" />
                          <XAxis dataKey="month" />
                          <YAxis domain={[0, 100]} />
                          <Tooltip />
                          <Legend />
                          <Line
                            type="monotone"
                            dataKey="compliance"
                            stroke="#38a169"
                            strokeWidth={3}
                            name="My Score"
                          />
                        </LineChart>
                      </ResponsiveContainer>
                    )}
                  </CardContent>
                </Card>

                <Card>
                  <CardHeader>
                    <CardTitle>Performance Breakdown</CardTitle>
                  </CardHeader>
                  <CardContent>
                    <div className="space-y-4">
                      {performanceBreakdown.length === 0 ? (
                        <p className="text-sm text-muted-foreground">No performance breakdown available.</p>
                      ) : (
                        performanceBreakdown.map((item, index) => (
                          <div key={index} className="space-y-2">
                            <div className="flex items-center justify-between">
                              <span className="text-sm font-medium">{item.metric}</span>
                              <span className="text-sm font-semibold">{item.score}%</span>
                            </div>
                            <div className="h-2 bg-muted rounded-full overflow-hidden">
                              <div
                                className={`h-full ${
                                  item.color === "success" ? "bg-success" : item.color === "info" ? "bg-info" : "bg-primary"
                                }`}
                                style={{ width: `${Math.min(100, Math.max(0, item.score))}%` }}
                              />
                            </div>
                          </div>
                        ))
                      )}
                    </div>
                  </CardContent>
                </Card>
              </div>
            </TabsContent>
          </Tabs>
        )}
      </div>
    </div>
  );
}
