import { useState } from "react";
import { PageHeader } from "./PageHeader";
import { MetricCard } from "./MetricCard";
import { Card, CardContent, CardHeader, CardTitle } from "./ui/card";
import { Tabs, TabsContent, TabsList, TabsTrigger } from "./ui/tabs";
import { Badge } from "./ui/badge";
import { Progress } from "./ui/progress";
import {
  CheckCircle,
  Clock,
  AlertTriangle,
  FileText,
  TrendingUp,
  Users,
  DollarSign,
  Calendar,
} from "lucide-react";
import {
  LineChart,
  Line,
  BarChart,
  Bar,
  PieChart,
  Pie,
  Cell,
  XAxis,
  YAxis,
  CartesianGrid,
  Tooltip,
  Legend,
  ResponsiveContainer,
} from "recharts";

interface DashboardProps {
  userRole?: "client" | "staff";
}

export function Dashboard({ userRole = "staff" }: DashboardProps) {
  const [viewType, setViewType] = useState<"client" | "staff">(userRole);

  // Mock data for charts
  const filingTrendsData = [
    { month: "Jan", onTime: 85, late: 15 },
    { month: "Feb", onTime: 88, late: 12 },
    { month: "Mar", onTime: 92, late: 8 },
    { month: "Apr", onTime: 87, late: 13 },
    { month: "May", onTime: 90, late: 10 },
    { month: "Jun", onTime: 94, late: 6 },
  ];

  const complianceDistribution = [
    { name: "Fully Compliant", value: 65, color: "#38a169" },
    { name: "Pending", value: 20, color: "#d69e2e" },
    { name: "At Risk", value: 10, color: "#e53e3e" },
    { name: "Non-Compliant", value: 5, color: "#1a202c" },
  ];

  const upcomingDeadlines = [
    { client: "ABC Corp", type: "GST Return", dueDate: "2025-10-15", daysLeft: 8, status: "pending" },
    { client: "XYZ Ltd", type: "Income Tax", dueDate: "2025-10-20", daysLeft: 13, status: "pending" },
    { client: "Tech Solutions", type: "Payroll Tax", dueDate: "2025-10-12", daysLeft: 5, status: "at-risk" },
    { client: "Global Trade", type: "Excise Duty", dueDate: "2025-10-10", daysLeft: 3, status: "urgent" },
  ];

  const recentActivity = [
    { time: "2 hours ago", action: "GST Return filed for ABC Corp", user: "Jane Smith" },
    { time: "4 hours ago", action: "Document uploaded: Financial Statements", user: "John Doe" },
    { time: "Yesterday", action: "Payment processed: SLE 15,000", user: "Sarah Johnson" },
    { time: "2 days ago", action: "New client onboarded: Tech Innovations Ltd", user: "Mike Brown" },
  ];

  return (
    <div>
      {userRole === "staff" && (
        <PageHeader
          title="Dashboard"
          breadcrumbs={[{ label: "Dashboard" }]}
          actions={
            <Tabs value={viewType} onValueChange={(v) => setViewType(v as "client" | "staff")}>
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

      <div className="p-6">
        {viewType === "staff" ? (
          <>
            {/* Staff Metrics */}
            <div className="grid grid-cols-1 md:grid-cols-2 lg:grid-cols-4 gap-6 mb-6">
              <MetricCard
                title="Client Compliance Rate"
                value="92%"
                trend="up"
                trendValue="+5%"
                subtitle="vs last month"
                icon={<CheckCircle className="w-4 h-4" />}
                color="success"
              />
              <MetricCard
                title="Filing Timeliness"
                value="15 days"
                trend="up"
                trendValue="+2 days"
                subtitle="avg before deadline"
                icon={<Clock className="w-4 h-4" />}
                color="primary"
              />
              <MetricCard
                title="Payment Completion"
                value="87%"
                trend="down"
                trendValue="-3%"
                subtitle="on-time payments"
                icon={<DollarSign className="w-4 h-4" />}
                color="info"
              />
              <MetricCard
                title="Document Compliance"
                value="94%"
                trend="up"
                trendValue="+8%"
                subtitle="submitted on time"
                icon={<FileText className="w-4 h-4" />}
                color="success"
              />
            </div>

            {/* Charts */}
            <div className="grid grid-cols-1 lg:grid-cols-2 gap-6 mb-6">
              <Card>
                <CardHeader>
                  <CardTitle>Filing Timeliness Trend</CardTitle>
                </CardHeader>
                <CardContent>
                  <ResponsiveContainer width="100%" height={300}>
                    <LineChart data={filingTrendsData}>
                      <CartesianGrid strokeDasharray="3 3" stroke="#e2e8f0" />
                      <XAxis dataKey="month" />
                      <YAxis />
                      <Tooltip />
                      <Legend />
                      <Line type="monotone" dataKey="onTime" stroke="#38a169" strokeWidth={2} name="On Time %" />
                      <Line type="monotone" dataKey="late" stroke="#e53e3e" strokeWidth={2} name="Late %" />
                    </LineChart>
                  </ResponsiveContainer>
                </CardContent>
              </Card>

              <Card>
                <CardHeader>
                  <CardTitle>Compliance Distribution</CardTitle>
                </CardHeader>
                <CardContent>
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
                </CardContent>
              </Card>
            </div>

            {/* At-Risk Clients & Recent Activity */}
            <div className="grid grid-cols-1 lg:grid-cols-2 gap-6">
              <Card>
                <CardHeader>
                  <CardTitle>At-Risk Clients</CardTitle>
                </CardHeader>
                <CardContent>
                  <div className="space-y-4">
                    {upcomingDeadlines
                      .filter((d) => d.status !== "pending")
                      .map((deadline, index) => (
                        <div key={index} className="flex items-center justify-between p-3 border border-border rounded-lg">
                          <div>
                            <p className="font-medium">{deadline.client}</p>
                            <p className="text-sm text-muted-foreground">{deadline.type}</p>
                          </div>
                          <div className="text-right">
                            <Badge variant={deadline.status === "urgent" ? "destructive" : "default"}>
                              {deadline.daysLeft} days left
                            </Badge>
                            <p className="text-xs text-muted-foreground mt-1">Due {deadline.dueDate}</p>
                          </div>
                        </div>
                      ))}
                  </div>
                </CardContent>
              </Card>

              <Card>
                <CardHeader>
                  <CardTitle>Recent Activity</CardTitle>
                </CardHeader>
                <CardContent>
                  <div className="space-y-4">
                    {recentActivity.map((activity, index) => (
                      <div key={index} className="flex gap-3">
                        <div className="w-2 h-2 bg-primary rounded-full mt-2" />
                        <div>
                          <p className="text-sm">{activity.action}</p>
                          <p className="text-xs text-muted-foreground">
                            {activity.user} • {activity.time}
                          </p>
                        </div>
                      </div>
                    ))}
                  </div>
                </CardContent>
              </Card>
            </div>
          </>
        ) : (
          <>
            {/* Client Metrics */}
            <div className="grid grid-cols-1 md:grid-cols-2 lg:grid-cols-4 gap-6 mb-6">
              <MetricCard
                title="Compliance Score"
                value="94%"
                trend="up"
                trendValue="+3%"
                subtitle="excellent standing"
                icon={<CheckCircle className="w-4 h-4" />}
                color="success"
              />
              <MetricCard
                title="Filing Timeliness"
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
                subtitle="all paid on time"
                icon={<DollarSign className="w-4 h-4" />}
                color="success"
              />
              <MetricCard
                title="Document Readiness"
                value="92%"
                trend="up"
                trendValue="+12%"
                subtitle="submitted on time"
                icon={<FileText className="w-4 h-4" />}
                color="info"
              />
            </div>

            {/* Upcoming Deadlines & Calendar */}
            <div className="grid grid-cols-1 lg:grid-cols-2 gap-6 mb-6">
              <Card>
                <CardHeader>
                  <CardTitle>Upcoming Deadlines</CardTitle>
                </CardHeader>
                <CardContent>
                  <div className="space-y-4">
                    {upcomingDeadlines.slice(0, 3).map((deadline, index) => (
                      <div key={index} className="flex items-center justify-between p-3 border border-border rounded-lg">
                        <div>
                          <p className="font-medium">{deadline.type}</p>
                          <p className="text-sm text-muted-foreground">Due {deadline.dueDate}</p>
                        </div>
                        <Badge variant={deadline.daysLeft < 7 ? "destructive" : "default"}>
                          {deadline.daysLeft} days
                        </Badge>
                      </div>
                    ))}
                  </div>
                </CardContent>
              </Card>

              <Card>
                <CardHeader>
                  <CardTitle>Penalty Warnings</CardTitle>
                </CardHeader>
                <CardContent>
                  <div className="space-y-4">
                    <div className="flex items-start gap-3 p-3 border border-warning/20 bg-warning/5 rounded-lg">
                      <AlertTriangle className="w-5 h-5 text-warning mt-0.5" />
                      <div>
                        <p className="font-medium">GST Late Filing Risk</p>
                        <p className="text-sm text-muted-foreground">Estimated penalty: SLE 2,500</p>
                        <p className="text-xs text-muted-foreground mt-1">File by Oct 15 to avoid</p>
                      </div>
                    </div>
                    <div className="p-3 border border-border rounded-lg">
                      <div className="flex items-center justify-between mb-2">
                        <p className="text-sm font-medium">Overall Tax Compliance</p>
                        <span className="text-sm text-success">94%</span>
                      </div>
                      <Progress value={94} className="h-2" />
                    </div>
                  </div>
                </CardContent>
              </Card>
            </div>

            {/* Recent Activity Timeline */}
            <Card>
              <CardHeader>
                <CardTitle>Recent Activity</CardTitle>
              </CardHeader>
              <CardContent>
                <div className="space-y-4">
                  {recentActivity.slice(0, 5).map((activity, index) => (
                    <div key={index} className="flex gap-3">
                      <div className="w-2 h-2 bg-primary rounded-full mt-2" />
                      <div>
                        <p className="text-sm">{activity.action}</p>
                        <p className="text-xs text-muted-foreground">{activity.time}</p>
                      </div>
                    </div>
                  ))}
                </div>
              </CardContent>
            </Card>
          </>
        )}
      </div>
    </div>
  );
}
