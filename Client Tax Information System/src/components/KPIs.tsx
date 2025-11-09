import { useState } from "react";
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

export function KPIs() {
  const [dateRange, setDateRange] = useState("6months");
  const [viewType, setViewType] = useState<"internal" | "client">("internal");

  const monthlyTrend = [
    { month: "Apr", compliance: 88, timeliness: 12, payments: 85 },
    { month: "May", compliance: 90, timeliness: 15, payments: 87 },
    { month: "Jun", compliance: 92, timeliness: 14, payments: 90 },
    { month: "Jul", compliance: 91, timeliness: 16, payments: 88 },
    { month: "Aug", compliance: 93, timeliness: 18, payments: 91 },
    { month: "Sep", compliance: 94, timeliness: 17, payments: 92 },
  ];

  const clientPerformance = [
    { name: "ABC Corp", score: 95 },
    { name: "XYZ Trading", score: 88 },
    { name: "Tech Solutions", score: 92 },
    { name: "Global Imports", score: 78 },
    { name: "Local Cafe", score: 85 },
  ];

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

      <div className="p-6">
        <Tabs value={viewType} onValueChange={(v) => setViewType(v as "internal" | "client")}>
          <TabsList className="mb-6">
            <TabsTrigger value="internal">Internal KPIs</TabsTrigger>
            <TabsTrigger value="client">Client KPIs</TabsTrigger>
          </TabsList>

          {/* Internal KPIs */}
          <TabsContent value="internal" className="space-y-6">
            <div className="grid grid-cols-1 md:grid-cols-2 lg:grid-cols-5 gap-4">
              <MetricCard
                title="Compliance Rate"
                value="94%"
                trend="up"
                trendValue="+3%"
                subtitle="vs last period"
                icon={<CheckCircle className="w-4 h-4" />}
                color="success"
              />
              <MetricCard
                title="Avg Timeliness"
                value="17 days"
                trend="up"
                trendValue="+2 days"
                subtitle="before deadline"
                icon={<Clock className="w-4 h-4" />}
                color="primary"
              />
              <MetricCard
                title="Payment Completion"
                value="92%"
                trend="up"
                trendValue="+5%"
                subtitle="on-time payments"
                icon={<DollarSign className="w-4 h-4" />}
                color="info"
              />
              <MetricCard
                title="Doc Submission"
                value="91%"
                trend="up"
                trendValue="+6%"
                subtitle="compliance rate"
                icon={<FileText className="w-4 h-4" />}
                color="success"
              />
              <MetricCard
                title="Engagement Rate"
                value="87%"
                trend="up"
                trendValue="+2%"
                subtitle="active clients"
                icon={<Users className="w-4 h-4" />}
                color="info"
              />
            </div>

            <div className="grid grid-cols-1 lg:grid-cols-2 gap-6">
              <Card>
                <CardHeader>
                  <CardTitle>Compliance Trend</CardTitle>
                </CardHeader>
                <CardContent>
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
                </CardContent>
              </Card>

              <Card>
                <CardHeader>
                  <CardTitle>Filing Timeliness Trend</CardTitle>
                </CardHeader>
                <CardContent>
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
                </CardContent>
              </Card>

              <Card>
                <CardHeader>
                  <CardTitle>Payment Completion Rate</CardTitle>
                </CardHeader>
                <CardContent>
                  <ResponsiveContainer width="100%" height={300}>
                    <LineChart data={monthlyTrend}>
                      <CartesianGrid strokeDasharray="3 3" stroke="#e2e8f0" />
                      <XAxis dataKey="month" />
                      <YAxis />
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
                </CardContent>
              </Card>

              <Card>
                <CardHeader>
                  <CardTitle>Top Performing Clients</CardTitle>
                </CardHeader>
                <CardContent>
                  <ResponsiveContainer width="100%" height={300}>
                    <BarChart data={clientPerformance} layout="vertical">
                      <CartesianGrid strokeDasharray="3 3" stroke="#e2e8f0" />
                      <XAxis type="number" domain={[0, 100]} />
                      <YAxis dataKey="name" type="category" width={120} />
                      <Tooltip />
                      <Bar dataKey="score" fill="#3d5f7e" name="Compliance Score" />
                    </BarChart>
                  </ResponsiveContainer>
                </CardContent>
              </Card>
            </div>
          </TabsContent>

          {/* Client KPIs */}
          <TabsContent value="client" className="space-y-6">
            <div className="grid grid-cols-1 md:grid-cols-2 lg:grid-cols-4 gap-4">
              <MetricCard
                title="My Timeliness"
                value="18 days"
                trend="up"
                trendValue="+4 days"
                subtitle="avg before deadline"
                icon={<Clock className="w-4 h-4" />}
                color="primary"
              />
              <MetricCard
                title="On-Time Payments"
                value="100%"
                trend="neutral"
                trendValue="0%"
                subtitle="perfect record"
                icon={<DollarSign className="w-4 h-4" />}
                color="success"
              />
              <MetricCard
                title="Document Readiness"
                value="92%"
                trend="up"
                trendValue="+12%"
                subtitle="submission rate"
                icon={<FileText className="w-4 h-4" />}
                color="info"
              />
              <MetricCard
                title="Composite Score"
                value="94%"
                trend="up"
                trendValue="+3%"
                subtitle="overall compliance"
                icon={<TrendingUp className="w-4 h-4" />}
                color="success"
              />
            </div>

            <div className="grid grid-cols-1 lg:grid-cols-2 gap-6">
              <Card>
                <CardHeader>
                  <CardTitle>My Compliance Score History</CardTitle>
                </CardHeader>
                <CardContent>
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
                </CardContent>
              </Card>

              <Card>
                <CardHeader>
                  <CardTitle>Performance Breakdown</CardTitle>
                </CardHeader>
                <CardContent>
                  <div className="space-y-4">
                    {[
                      { metric: "Filing Timeliness", score: 95, color: "success" },
                      { metric: "Payment Compliance", score: 100, color: "success" },
                      { metric: "Document Submission", score: 92, color: "info" },
                      { metric: "Response Time", score: 88, color: "info" },
                    ].map((item, index) => (
                      <div key={index} className="space-y-2">
                        <div className="flex items-center justify-between">
                          <span className="text-sm font-medium">{item.metric}</span>
                          <span className="text-sm font-semibold">{item.score}%</span>
                        </div>
                        <div className="h-2 bg-muted rounded-full overflow-hidden">
                          <div
                            className={`h-full ${
                              item.color === "success" ? "bg-success" : "bg-info"
                            }`}
                            style={{ width: `${item.score}%` }}
                          />
                        </div>
                      </div>
                    ))}
                  </div>
                </CardContent>
              </Card>
            </div>
          </TabsContent>
        </Tabs>
      </div>
    </div>
  );
}
